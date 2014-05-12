using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using EnvDTE;
using EnvDTE80;
using System.Diagnostics;
using SimilarHighlight.OutputWindow;
using SimilarHighlight.OverviewMargin.Implementation;

namespace SimilarHighlight
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("text")]
    [TagType(typeof(TextMarkerTag))]
    internal class HLTextTaggerProvider : IViewTaggerProvider
    {
        [Import]
        internal ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        [Import]
        internal IOutputWindowService OutputWindowService { get; set; }

        [Import]
        internal SVsServiceProvider ServiceProvider = null;

        [ImportMany]
        internal List<Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>> marginProviders;

        private IWpfTextViewMarginProvider rightMarginFactory { get; set; }
        private IOutputWindowPane outputWindow { get; set; }
        
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            //provide highlighting only on the top buffer
            if (textView.TextBuffer != buffer)
                return null;

            ITextDocument textDocument = null;
            EnvDTE.Document nowDocument = null;

            TextDocumentFactoryService.TryGetTextDocument(buffer, out textDocument);
            DTE dte = (DTE)ServiceProvider.GetService(typeof(DTE));

            if (dte == null)
                Trace.WriteLine("did not get dte reference");
            else
            {
                foreach (var item in dte.Documents)
                {
                    var doc = item as EnvDTE.Document;
                    var name = doc.FullName;
                    if (name == textDocument.FilePath)
                    {
                        nowDocument = doc;
                        break;
                    }
                }
            }

            // A margin factory to add a right marigin which mark highlighted elements' points.
            if (rightMarginFactory == null)
            {
                foreach (var marginProvider in marginProviders)
                {
                    if (String.Compare(marginProvider.Metadata.Name, "RightMargin", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        rightMarginFactory = marginProvider.Value;
                    }
                }
            }

            // A output pane named "Similar" will be added to display some information.
            if (outputWindow == null)
            {
                outputWindow = OutputWindowService.TryGetPane("Similar");
            }

            return new HLTextTagger(textView as IWpfTextView, buffer, nowDocument, outputWindow, rightMarginFactory) as ITagger<T>;
        }
    }
}
