namespace AngleSharp.Core.Tests.Html
{
    using AngleSharp.Dom;
    using AngleSharp.Html.Parser;
    using AngleSharp.Text;
    using NUnit.Framework;
    using System;

    /// <summary>
    /// Tests for whitespace-only text node handling in custom (generic)
    /// tree construction, controlled by
    /// <see cref="HtmlParserOptions.IsKeepingWhitespaceTextNodes"/>.
    /// </summary>
    [TestFixture]
    public class CustomTreeWhitespaceTests
    {
        private const String Source = "<html><body><div>5 days</div> \n <div>Physiotherapy</div></body></html>";

        [Test]
        public void GenericTreeConstructionDropsWhitespaceOnlyTextNodesByDefault()
        {
            var parser = new HtmlParser(new HtmlParserOptions(), BrowsingContext.New(Configuration.Default));

            using (var document = parser.ParseDocument<Document, Element>(new TextSource(Source)))
            {
                var body = document.Body;

                Assert.AreEqual(2, body.ChildNodes.Length);
                Assert.AreEqual("5 daysPhysiotherapy", body.TextContent);
            }
        }

        [Test]
        public void GenericTreeConstructionKeepsWhitespaceOnlyTextNodesWithOption()
        {
            var options = new HtmlParserOptions { IsKeepingWhitespaceTextNodes = true };
            var parser = new HtmlParser(options, BrowsingContext.New(Configuration.Default));

            using (var document = parser.ParseDocument<Document, Element>(new TextSource(Source)))
            {
                var body = document.Body;

                Assert.AreEqual(3, body.ChildNodes.Length);
                Assert.AreEqual(NodeType.Text, body.ChildNodes[1].NodeType);
                Assert.AreEqual("5 days \n Physiotherapy", body.TextContent);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StandardDomAlwaysKeepsWhitespaceOnlyTextNodes(Boolean isKeepingWhitespaceTextNodes)
        {
            var options = new HtmlParserOptions { IsKeepingWhitespaceTextNodes = isKeepingWhitespaceTextNodes };
            var parser = new HtmlParser(options);

            using (var document = parser.ParseDocument(Source))
            {
                var body = document.Body;

                Assert.AreEqual(3, body.ChildNodes.Length);
                Assert.AreEqual(NodeType.Text, body.ChildNodes[1].NodeType);
                Assert.AreEqual("5 days \n Physiotherapy", body.TextContent);
            }
        }
    }
}
