﻿namespace AngleSharp
{
    using AngleSharp.Services.Default;

    /// <summary>
    /// Bundles the available factories.
    /// </summary>
    static class Factory
    {
        /// <summary>
        /// The HTML element factory.
        /// </summary>
        public static readonly HtmlElementFactory HtmlElements = new HtmlElementFactory();

        /// <summary>
        /// The MathML element factory.
        /// </summary>
        public static readonly MathElementFactory MathElements = new MathElementFactory();

        /// <summary>
        /// The SVG element factory.
        /// </summary>
        public static readonly SvgElementFactory SvgElements = new SvgElementFactory();

        /// <summary>
        /// The DOM events factory.
        /// </summary>
        public static readonly EventFactory Events = new EventFactory();

        /// <summary>
        /// The HTML input type provider factory.
        /// </summary>
        public static readonly InputTypeFactory InputTypes = new InputTypeFactory();

        /// <summary>
        /// The CSS attribute selector factory.
        /// </summary>
        public static readonly AttributeSelectorFactory AttributeSelector = new AttributeSelectorFactory();

        /// <summary>
        /// The CSS pseudo element selector factory.
        /// </summary>
        public static readonly PseudoElementSelectorFactory PseudoElementSelector = new PseudoElementSelectorFactory();

        /// <summary>
        /// The CSS pseudo class selector factory.
        /// </summary>
        public static readonly PseudoClassSelectorFactory PseudoClassSelector = new PseudoClassSelectorFactory();

        /// <summary>
        /// The link rel type factory.
        /// </summary>
        public static readonly LinkRelationFactory LinkRelations = new LinkRelationFactory();

        /// <summary>
        /// The document factory.
        /// </summary>
        public static readonly DocumentFactory Document = new DocumentFactory();

        /// <summary>
        /// The browsing context factory.
        /// </summary>
        public static readonly ContextFactory BrowsingContext = new ContextFactory();

        /// <summary>
        /// The service factory.
        /// </summary>
        public static readonly ServiceFactory Service = new ServiceFactory();
    }
}