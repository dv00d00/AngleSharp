#if NET10_0
using System;
using System.IO;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.Text;
using AngleSharp.Text.Experimental;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace AngleSharp.Benchmarks;

/// <summary>
/// Measures the complete path from raw UTF-8 network bytes through DOM construction.
/// </summary>
[MemoryDiagnoser, ShortRunJob]
public class Utf8NetworkParserBenchmark
{
    private const Int32 NetworkBufferSize = 4096;
    private IBrowsingContext _context = null!;
    private HtmlParser _legacyParser = null!;
    private Byte[] _utf8 = null!;

    [GlobalSetup]
    public void Setup()
    {
        _utf8 = File.ReadAllBytes("page.html");
        _context = BrowsingContext.New(Configuration.Default);
        _legacyParser = new HtmlParser(_context);
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Network4K")]
    public async Task<Int32> LegacyNetwork4K()
    {
        using var stream = new NetworkReadStream(_utf8, NetworkBufferSize);
        using var document = await _legacyParser.ParseDocumentAsync(stream).ConfigureAwait(false);
        return document.DocumentElement.ChildElementCount;
    }

    [Benchmark, BenchmarkCategory("Network4K")]
    public async Task<Int32> BoundedUtf16Network4K()
    {
        using var stream = new NetworkReadStream(_utf8, NetworkBufferSize);
        var source = new TextSource(new Utf8StreamingTextSource(stream, NetworkBufferSize));
        using var document = await _legacyParser.ParseDocumentAsync(source, default).ConfigureAwait(false);
        return document.DocumentElement.ChildElementCount;
    }

    private sealed class NetworkReadStream(Byte[] source, Int32 maxReadSize) : Stream
    {
        private Int32 _position;

        public override Boolean CanRead => true;
        public override Boolean CanSeek => false;
        public override Boolean CanWrite => false;
        public override Int64 Length => source.Length;
        public override Int64 Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }

        public override Int32 Read(Byte[] buffer, Int32 offset, Int32 count) =>
            Read(buffer.AsSpan(offset, count));

        public override Int32 Read(Span<Byte> buffer)
        {
            var length = Math.Min(Math.Min(buffer.Length, maxReadSize), source.Length - _position);
            if (length <= 0)
                return 0;

            source.AsSpan(_position, length).CopyTo(buffer);
            _position += length;
            return length;
        }

        public override Task<Int32> ReadAsync(
            Byte[] buffer,
            Int32 offset,
            Int32 count,
            CancellationToken cancellationToken) =>
            Task.FromResult(Read(buffer, offset, count));

        public override ValueTask<Int32> ReadAsync(
            Memory<Byte> buffer,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Read(buffer.Span));

        public override void Flush() { }
        public override Int64 Seek(Int64 offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(Int64 value) => throw new NotSupportedException();
        public override void Write(Byte[] buffer, Int32 offset, Int32 count) => throw new NotSupportedException();
    }
}
#endif
