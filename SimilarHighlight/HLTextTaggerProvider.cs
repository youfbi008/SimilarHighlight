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

        
   
        private IOutputWindowPane outputWindow { get; set; }

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            //provide highlighting only on the top buffer
            if (textView.TextBuffer != buffer)
                return null;
            //  DTE2 dte = Package.GetGlobalService(typeof(DTE)) as DTE2;

            ITextDocument textDocument = null;

            TextDocumentFactoryService.TryGetTextDocument(buffer, out textDocument);
            if (outputWindow == null)
            {
                outputWindow = OutputWindowService.TryGetPane("Similar");
            }
            //    outputWindow.WriteLine("12312312312");
            //EnvDTE80.DTE2 dte2;
            //dte2 = (EnvDTE80.DTE2)System.Runtime.InteropServices.Marshal.
            //GetActiveObject("VisualStudio.DTE.11.0");

            EnvDTE.Document nowDocument = null;

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


            return new HLTextTagger(textView as IWpfTextView, buffer, nowDocument, outputWindow) as ITagger<T>;
        }
    }
}
