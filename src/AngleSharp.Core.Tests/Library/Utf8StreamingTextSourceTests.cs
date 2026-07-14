#if NET10_0
namespace AngleSharp.Core.Tests.Library;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using AngleSharp.Text;
using AngleSharp.Text.Experimental;
using NUnit.Framework;

[TestFixture]
public sealed class Utf8StreamingTextSourceTests
{
    [TestCase(1)]
    [TestCase(3)]
    [TestCase(4096)]
    public async Task ParserMatchesStringInputAcrossUtf8Boundaries(Int32 maxReadSize)
    {
        const String html = "<!doctype html><p title='hé😀終'>alpha &amp; Ω</p><script>const x = '</nope>';</script>";
        var parser = new HtmlParser();
        using var expected = parser.ParseDocument(html);
        using var stream = new ChunkedReadStream(Encoding.UTF8.GetBytes(html), maxReadSize);
        var source = new TextSource(new Utf8StreamingTextSource(stream, 128));
        using var actual = await parser.ParseDocumentAsync(source, CancellationToken.None);

        Assert.That(actual.DocumentElement.OuterHtml, Is.EqualTo(expected.DocumentElement.OuterHtml));
    }

    [Test]
    public async Task ParserSkipsUtf8BomSplitAcrossReads()
    {
        const String html = "<p>ok</p>";
        var payload = new Byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes(html)).ToArray();
        using var stream = new ChunkedReadStream(payload, 1);
        var source = new TextSource(new Utf8StreamingTextSource(stream, 128));
        using var actual = await new HtmlParser().ParseDocumentAsync(source, CancellationToken.None);

        Assert.That(actual.Body.TextContent, Is.EqualTo("ok"));
    }

    [Test]
    public void SourceRetainsOnlyBoundedLookback()
    {
        var payload = Encoding.UTF8.GetBytes(new String('x', 32_000));
        using var stream = new MemoryStream(payload);
        using var source = new Utf8StreamingTextSource(stream, 128, 64);

        for (var index = 0; index < 20_000; index++)
            Assert.That(source.ReadCharacter(), Is.EqualTo('x'));

        Assert.That(source.Index, Is.EqualTo(20_000));
        Assert.Throws<ArgumentOutOfRangeException>(() => source.Index = 0);
        source.Index -= 64;
        Assert.That(source.ReadCharacter(), Is.EqualTo('x'));
    }

    private sealed class ChunkedReadStream(Byte[] source, Int32 maxReadSize) : Stream
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

        public override Int32 Read(Byte[] buffer, Int32 offset, Int32 count) => Read(buffer.AsSpan(offset, count));

        public override Int32 Read(Span<Byte> buffer)
        {
            var length = Math.Min(Math.Min(buffer.Length, maxReadSize), source.Length - _position);
            if (length <= 0)
                return 0;
            source.AsSpan(_position, length).CopyTo(buffer);
            _position += length;
            return length;
        }

        public override ValueTask<Int32> ReadAsync(
            Memory<Byte> buffer,
            CancellationToken cancellationToken = default) => ValueTask.FromResult(Read(buffer.Span));

        public override void Flush() { }
        public override Int64 Seek(Int64 offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(Int64 value) => throw new NotSupportedException();
        public override void Write(Byte[] buffer, Int32 offset, Int32 count) => throw new NotSupportedException();
    }
}
#endif
