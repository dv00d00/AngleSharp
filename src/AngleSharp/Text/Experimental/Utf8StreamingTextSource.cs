#if NET10_0
namespace AngleSharp.Text.Experimental;

using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Common;

/// <summary>
/// Experimental forward-only UTF-8 source that keeps a bounded decoded UTF-16 window.
/// </summary>
/// <remarks>
/// This source is intended for parsing explicitly UTF-8 input. It preserves a small lookback window for tokenizer
/// reconsumption, but does not retain or expose the complete source text and does not support encoding restarts.
/// </remarks>
public sealed class Utf8StreamingTextSource : IReadOnlyTextSource
{
    private const Int32 DefaultBufferSize = 4096;
    private const Int32 DefaultLookback = 64;

    private readonly Stream _stream;
    private readonly Decoder _decoder;
    private readonly Byte[] _bytes;
    private readonly Char[] _chars;
    private readonly Int32 _decodeChunkSize;
    private readonly Int32 _lookback;
    private Int32 _byteOffset;
    private Int32 _byteCount;
    private Int32 _bufferStart;
    private Int32 _bufferLength;
    private Int32 _index;
    private Boolean _streamEnded;
    private Boolean _finished;
    private Boolean _disposed;
    private Boolean _atStart = true;

    /// <summary>
    /// Creates a bounded UTF-8 streaming source.
    /// </summary>
    public Utf8StreamingTextSource(
        Stream stream,
        Int32 bufferSize = DefaultBufferSize,
        Int32 lookback = DefaultLookback)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentOutOfRangeException.ThrowIfLessThan(bufferSize, 128);
        ArgumentOutOfRangeException.ThrowIfLessThan(lookback, 32);
        if (!stream.CanRead)
            throw new ArgumentException("The stream must be readable.", nameof(stream));

        _stream = stream;
        _decoder = new UTF8Encoding(false, false).GetDecoder();
        _bytes = ArrayPool<Byte>.Shared.Rent(bufferSize);
        _chars = ArrayPool<Char>.Shared.Rent(checked(bufferSize * 2 + lookback));
        _decodeChunkSize = bufferSize;
        _lookback = lookback;
    }

    /// <summary>
    /// The full text is intentionally not retained by this source.
    /// </summary>
    public String Text => throw new NotSupportedException(
        "A bounded streaming source does not retain the complete input text.");

    /// <summary>
    /// Gets the global end position of the currently decoded window.
    /// </summary>
    public Int32 Length => _bufferStart + _bufferLength;

    /// <summary>
    /// Gets the fixed UTF-8 input encoding.
    /// </summary>
    public Encoding CurrentEncoding
    {
        get => Encoding.UTF8;
        set
        {
            if (value?.CodePage != Encoding.UTF8.CodePage)
                throw new NotSupportedException("The streaming source is explicitly UTF-8.");
        }
    }

    /// <summary>
    /// Gets or sets the global read position inside the retained window.
    /// </summary>
    public Int32 Index
    {
        get => _index;
        set
        {
            if (value < _bufferStart || value > Length)
                throw new ArgumentOutOfRangeException(nameof(value), "The requested position is outside the retained window.");
            _index = value;
        }
    }

    /// <inheritdoc />
    public Char this[Int32 index]
    {
        get
        {
            if (index < _bufferStart || index >= Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _chars[index - _bufferStart];
        }
    }

    /// <inheritdoc />
    public Char ReadCharacter()
    {
        ThrowIfDisposed();
        EnsureAvailable(1);
        if (_index >= Length)
            return Symbols.EndOfFile;
        return _chars[_index++ - _bufferStart];
    }

    /// <inheritdoc />
    public String ReadCharacters(Int32 characters)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(characters);
        if (characters == 0)
            return String.Empty;

        var result = new Char[characters];
        var written = 0;
        while (written < result.Length)
        {
            var value = ReadCharacter();
            if (value == Symbols.EndOfFile)
                break;
            result[written++] = value;
        }
        return new String(result, 0, written);
    }

    /// <inheritdoc />
    public StringOrMemory ReadMemory(Int32 characters) => new(ReadCharacters(characters));

    /// <inheritdoc />
    public async Task PrefetchAsync(Int32 length, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        await EnsureAvailableAsync(Math.Min(length, _decodeChunkSize * 2), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// A bounded source cannot prefetch and retain the complete stream.
    /// </summary>
    public Task PrefetchAllAsync(CancellationToken cancellationToken) =>
        Task.FromException(new NotSupportedException(
            "A bounded streaming source cannot retain the complete input text."));

    /// <inheritdoc />
    public Boolean TryGetContentLength(out Int32 length)
    {
        length = 0;
        return false;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        ArrayPool<Byte>.Shared.Return(_bytes);
        ArrayPool<Char>.Shared.Return(_chars);
        _stream.Dispose();
    }

    private void EnsureAvailable(Int32 count)
    {
        var target = checked(_index + count);
        while (Length < target && !_finished)
        {
            if (_byteOffset >= _byteCount && !_streamEnded)
            {
                _byteCount = _stream.Read(_bytes, 0, _decodeChunkSize);
                _byteOffset = 0;
                _streamEnded = _byteCount == 0;
            }
            DecodeAvailable();
        }
    }

    private async Task EnsureAvailableAsync(Int32 count, CancellationToken cancellationToken)
    {
        var target = checked(_index + count);
        while (Length < target && !_finished)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_byteOffset >= _byteCount && !_streamEnded)
            {
                _byteCount = await _stream.ReadAsync(
                    _bytes.AsMemory(0, _decodeChunkSize),
                    cancellationToken).ConfigureAwait(false);
                _byteOffset = 0;
                _streamEnded = _byteCount == 0;
            }
            DecodeAvailable();
        }
    }

    private void DecodeAvailable()
    {
        MakeRoom();
        var destination = _chars.AsSpan(_bufferLength);
        _decoder.Convert(
            _bytes.AsSpan(_byteOffset, _byteCount - _byteOffset),
            destination,
            _streamEnded,
            out var bytesUsed,
            out var charsUsed,
            out var completed);

        _byteOffset += bytesUsed;
        _bufferLength += charsUsed;
        if (_atStart && _bufferLength > 0)
        {
            _atStart = false;
            if (_chars[0] == '\uFEFF')
            {
                _chars.AsSpan(1, _bufferLength - 1).CopyTo(_chars);
                _bufferLength--;
            }
        }
        if (_streamEnded && _byteOffset >= _byteCount && completed)
            _finished = true;

        if (bytesUsed == 0 && charsUsed == 0 && !_finished)
            throw new InvalidOperationException("The UTF-8 decoder made no progress.");
    }

    private void MakeRoom()
    {
        if (_chars.Length - _bufferLength >= 2)
            return;

        var retainFrom = Math.Max(_bufferStart, _index - _lookback);
        var discard = retainFrom - _bufferStart;
        if (discard <= 0)
            throw new InvalidOperationException("The requested forward window exceeds the streaming buffer capacity.");

        _chars.AsSpan(discard, _bufferLength - discard).CopyTo(_chars);
        _bufferStart += discard;
        _bufferLength -= discard;
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}
#endif
