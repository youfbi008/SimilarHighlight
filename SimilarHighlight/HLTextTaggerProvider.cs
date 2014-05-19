using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.CompilerServices;
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
using SimilarHighlight.ContainerMargin;
using Microsoft.VisualStudio.Shell.Interop;
using System.Runtime.InteropServices;
using SimilarHighlight.Option;
using System.Windows;

namespace SimilarHighlight
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("text")]
    [TagType(typeof(TextMarkerTag))]
    public class HLTextTaggerProvider : IViewTaggerProvider
    {
        [Import]
        internal ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        [Import]
        internal IOutputWindowService OutputWindowService { get; set; }
        
        [Import]
        internal SVsServiceProvider ServiceProvider = null;

        [ImportMany]
        internal List<Lazy<IWpfTextViewMarginProvider, IWpfTextViewMarginMetadata>> marginProviders { get; private set; }

        [Import]
        internal IEditorFormatMapService FormatMapService { get; set; }

        private IWpfTextViewMarginProvider similarMarginFactory { get; set; }
        private IOutputWindowPane outputWindow { get; set; }
        private OptionPage optionPage { get; set; }

        public ITagger<ITag> HLTextTaggerElement;

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            if (optionPage == null)
            {
                var shell = (IVsShell)ServiceProvider.GetService(typeof(SVsShell));
                IVsPackage package;
                Marshal.ThrowExceptionForHR(shell.LoadPackage(GuidList.guidSimilarOptionCmdSet, out package));

                optionPage = ((SimilarOptionPackage)package).GetOptionPage();
            }

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

                if (nowDocument == null) {
                    return null;
                }
            }

            // A margin factory to add a right marigin which mark highlighted elements' points.
            if (similarMarginFactory == null && optionPage.MarginEnabled)
            {
                foreach (var marginProvider in marginProviders)
                {
                    if (String.Compare(marginProvider.Metadata.Name, "SimilarMargin", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        similarMarginFactory = marginProvider.Value;
                    }
                }
            }

            // A output pane named "Similar" will be added to display some information.
            if (outputWindow == null && optionPage.OutputEnabled)
            {
                outputWindow = OutputWindowService.TryGetPane("Similar");
            }

            var format = FormatMapService.GetEditorFormatMap(textView);
            //format.BeginBatchUpdate();

            //ResourceDictionary properties = format.GetProperties(HLTextFormatDefinition.FormatName);

            //Color forecolor = Utils.CreateColor(_view.Options.GetOptionValue(OptionsKeys.TextMarkerForegroundColor));
            //Color backcolor = Utils.CreateColor(_view.Options.GetOptionValue(OptionsKeys.TextMarkerBackgroundColor));
       //     EditorFormatDefinition.BackgroundColorId
            //properties[EditorFormatDefinition.ForegroundBrushId] = GetBrush(optionPage.ForegroundColor.Name, Colors.DarkBlue);
            //properties[EditorFormatDefinition.BackgroundBrushId] = GetBrush(optionPage.BackgroundColor.Name, Colors.LightGreen);
            //properties[EditorFormatDefinition.ForegroundColorId] = optionPage.ForegroundColor;
            //properties[EditorFormatDefinition.BackgroundColorId] = optionPage.BackgroundColor;

            //format.EndBatchUpdate();
          //  FireTagsChanged();

            return new HLTextTagger(textView as IWpfTextView, buffer, nowDocument, outputWindow, similarMarginFactory, optionPage, format) as ITagger<T>;
        }

        //private Brush GetBrush(string colorName, Color defColor)
        //{
        //    Brush brush = null;
        //    try
        //    {
        //        if (colorName != "")
        //        {
        //            brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorName));
        //            brush.Freeze();
        //        }
        //    }
        //    catch (Exception exc)
        //    {
        //        if (outputWindow != null)
        //        {
        //            outputWindow.WriteLine("The setting of CaretColor is invalid.");
        //        }
        //        brush = new SolidColorBrush(defColor);
        //        brush.Freeze();
        //    }
        //    return brush;
        //}
    }
}
