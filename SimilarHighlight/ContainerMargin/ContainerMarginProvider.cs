using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Tagging;
using SimilarHighlight.ContainerMargin;

namespace SimilarHighlight.ContainerMargin
{
    [Export(typeof(IWpfTextViewMarginProvider))]
    [Name(PredefinedContainerMargin.Container)]
    [MarginContainer(PredefinedMarginNames.VerticalScrollBarContainer)]
    [Order(After = PredefinedMarginNames.VerticalScrollBar)]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class ContainerMarginProvider : IWpfTextViewMarginProvider
    {
        [ImportMany]
        internal List<Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>> _marginProviders { get; private set; }

        [Import]
        internal IScrollMapFactoryService _scrollMapFactory { get; private set; }

        [Import]
        internal IOutliningManagerService OutliningManagerService { get; private set; }
                
        private IList<Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>> _orderedMarginProviders;
        internal IList<Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>> OrderedMarginProviders
        {
            get
            {
                if (_orderedMarginProviders == null)
                {
                    _orderedMarginProviders = Orderer.Order<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>(_marginProviders);
                }

                return _orderedMarginProviders;
            }
        }

        /// <summary>
        /// Create an instance of the ContainerMargin in the specified <see cref="IWpfTextViewHost"/>.
        /// </summary>
        /// <param name="textViewHost">The <see cref="IWpfTextViewHost"/> in which the ContainerMargin will be displayed.</param>
        /// <returns>The newly created ContainerMargin.</returns>
        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost textViewHost, IWpfTextViewMargin containerMargin)
        {
            if (!HLTextTagger.OptionPage.MarginEnabled) return null;
            return ContainerMargin.Create(textViewHost, containerMargin, this);
        }
    }
}