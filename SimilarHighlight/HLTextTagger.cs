using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using EnvDTE;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Code2Xml.Core;
using Code2Xml.Core.Location;
using System.IO;
using System.Diagnostics;
using System.Xml.Linq;
using System.Xml;
using Code2Xml.Languages.ANTLRv3.Processors.CSharp;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Code2Xml.Languages.ANTLRv3.Processors.Java;
using System.Collections;
using Code2Xml.Core.Generators;
using SimilarHighlight.OutputWindow;
using SimilarHighlight.ContainerMargin;
using System.Windows.Threading;
using SimilarHighlight.Option;

namespace SimilarHighlight
{
    public struct LocationInfo
    {
        public XElement XElement;
        public CodeRange CodeRange;
        public bool IsNeedFix;
    }

    internal class HLTextTagger : ITagger<HLTextTag>, IDisposable
    {
        // the collecton of highlighted elements
        public static IList<SnapshotSpan> NewSpanAll = new List<SnapshotSpan>();
        public static int PaneLineCnt { get; set; }
        public static OptionPage OptionPage { get; set; }
        public static string FileName { get; set; }

        #region Private Members
    //    private SimilarMarginFactory SimilarMarginFactory; //static
        public  EnvDTE.Document Document { get; set; } //static
        private IWpfTextView View { get; set; }//static
        // the order number of current selection in highlighted elements
        private static int CurrentSelectNum { get; set; }
        // the temp order number of current selection in highlighted elements
        private static int TMPCurrentSelectNum { get; set; }
        // The line break. Cobol only has /n.
        private static bool hasSR { get; set; }

        private ITextBuffer SourceBuffer { get; set; }
        private NormalizedSnapshotSpanCollection WordSpans { get; set; }
        private SnapshotSpan? CurrentWord { get; set; }
        
        private object updateLock = new object();
        private object buildLock = new object();

        // Make the highlight operation to background.
        private System.Threading.Thread listenThread;
        // The highlighted elements will be saved when the text is changed.
        private NormalizedSnapshotSpanCollection TmpWordSpans { get; set; }
        // location datas 
        private List<LocationInfo> Locations { get; set; }
        
        // Count the number of left mouse button clicks.
        private int CntLeftClick { get; set; }
        // current selection
        private TextSelection RequestSelection { get; set; }
        // current word for Repeat check   
        private SnapshotSpan CurrentWordForCheck { get; set; }

        // the SyntaxTreeGenerator of the current editor
        private SyntaxTreeGenerator SyntaxTreeGenerator;
        // the source code of the current editor
        private string SourceCode { get; set; }
        // the root element of the source code
        private XElement RootElement { get; set; }
        // the token list of the source code
   //     List<XElement> TokenElements { get; set; }
        // Whether the shift key is pressed.
        private bool IsShiftDown = false;
        // Whether the similar elements are needed to fix.
        private bool IsNeedFix { get; set; }
        // the regex wheather need fix
        private Regex RegexNeedFix { get; set; }
        // the forward offset when fixing
        private int startOffset { get; set; }
        // the backward offset when fixing
        private int endOffset { get; set; }
        private Tuple<Regex, Tuple<int, int>> FixKit = null;
        // LocationInfo of the element selected before current two element
        private LocationInfo PreLocationInfo { get; set; }
        // Whether have the similar elements.
        private bool HaveSimilarElements = false;
        // CST : 0;  AST : 1;
        private int treeType = 0;
        private static IOutputWindowPane OutputWindow;
        private int SelectionNo { get; set; }
        private int highlightNo { get; set; }
        
        private bool m_disposed;
        public static List<Tuple<int, string, CodeRange>> OutputDatas = new List<Tuple<int, string, CodeRange>>();
        private SimilarMarginElement MarginElement { get; set; }
        #endregion

        public HLTextTagger(IWpfTextView view, ITextBuffer sourceBuffer, EnvDTE.Document document,
            IOutputWindowPane outputWindow, IWpfTextViewMarginProvider similarMarginFactory,
            OptionPage optionPage, IEditorFormatMap format)
        {
            try
            {
                MarginElement = null;
                if (document == null)
                    return;
                if (this.SyntaxTreeGenerator == null)
                {
                    hasSR = true;
                    switch (Path.GetExtension(document.FullName).ToUpper())
                    {
                        case ".C":
                            this.SyntaxTreeGenerator = new Code2Xml.Languages.ANTLRv3.Generators.C.CCstGeneratorUsingAntlr3();
                            break;
                        case ".PHP":
                            this.SyntaxTreeGenerator = new Code2Xml.Languages.ANTLRv3.Generators.Php.PhpCstGeneratorUsingAntlr3();
                            break;
                        case ".JAVA":
                            this.SyntaxTreeGenerator = new Code2Xml.Languages.ANTLRv3.Generators.Java.JavaCstGeneratorUsingAntlr3();
                            break;
                        case ".JS":
                            this.SyntaxTreeGenerator = new Code2Xml.Languages.ANTLRv3.Generators.JavaScript.JavaScriptCstGeneratorUsingAntlr3();
                            break;                        
                        case ".CS":
                            this.SyntaxTreeGenerator = new Code2Xml.Languages.ANTLRv3.Generators.CSharp.CSharpCstGeneratorUsingAntlr3();
                            break;
                        // TODO: ExternalGenerators will be fixed.
                        case ".PY": //TODO: python 2 and 3 has the same extension that is "py".
                            this.SyntaxTreeGenerator = new Code2Xml.Languages.ExternalGenerators.Generators.Python.Python2CstGenerator();
                            break;
                        case ".RB": //TODO: ruby 18, 19, 20 has the same extension that is "rb".
                            this.SyntaxTreeGenerator = new Code2Xml.Languages.ExternalGenerators.Generators.Ruby.Ruby18AstGenerator();
                            break;
                        case ".CBL":
                            this.SyntaxTreeGenerator = new Code2Xml.Languages.ExternalGenerators.Generators.Cobol.Cobol85CstGenerator();
                            break;
                    }

                    if (SyntaxTreeGenerator != null)
                    {

                        var currentTextDoc = document.Object("TextDocument");
                        SourceCode = currentTextDoc.StartPoint.CreateEditPoint().GetText(currentTextDoc.EndPoint);
                        // Check the line break type of the file.
                        CheckLineBreakType();
                        Document = document;
                        RootElement = SyntaxTreeGenerator.GenerateXmlFromCodeText(SourceCode, true);
                        RegexNeedFix = new Regex("^\"(.*)\"$");
                        // the forward offset when fixing
                        startOffset = 1;
                        // the backward offset when fixing
                        endOffset = 1;

                        this.Locations = new List<LocationInfo>();
                        this.CntLeftClick = 0;
                        
                        this.SourceBuffer = sourceBuffer;
                        this.WordSpans = new NormalizedSnapshotSpanCollection();
                        this.TmpWordSpans = new NormalizedSnapshotSpanCollection();
                        this.CurrentWord = null;
                        
                        OutputWindow = outputWindow;
                        OptionPage = optionPage;
                        View = view;
                        View.VisualElement.IsVisibleChanged += delegate(object sender, DependencyPropertyChangedEventArgs e)
                        {
                            if ((bool)e.NewValue)
                            {
                                FileName = document.ActiveWindow.Caption;

                                if (MarginElement == null)
                                {
                                    MarginElement = (similarMarginFactory as SimilarMarginFactory).similarMargin.similarMarginElement;
                                    MarginElement.CurFileName = FileName;
                                    MarginElement.TextTaggerElement = this;
                                }
                                
                                //Hook up to the various events we need to keep the caret margin current.
                                View.VisualElement.PreviewMouseLeftButtonUp += VisualElement_PreviewMouseLeftButtonUp;
                                View.VisualElement.PreviewKeyDown += VisualElement_PreviewKeyDown;
                                View.VisualElement.PreviewKeyUp += VisualElement_PreviewKeyUp;
                                //Force the margin to be rerendered since things might have changed while the margin was hidden.
                                View.VisualElement.InvalidateVisual();
                            }
                            else
                            {
                                CurrentWord = null;
                                WordSpans = new NormalizedSnapshotSpanCollection();
                                TmpWordSpans = new NormalizedSnapshotSpanCollection();
                                NewSpanAll.Clear();
                                CurrentSelectNum = 0;
                                var tempEvent = TagsChanged;
                                if (tempEvent != null)
                                {
                                    Locations.Clear();
                                    // Refresh the text of the current editor window.
                                    tempEvent(this, new SnapshotSpanEventArgs(new SnapshotSpan(
                                        SourceBuffer.CurrentSnapshot, 0, SourceBuffer.CurrentSnapshot.Length)));
                                }

                                View.VisualElement.PreviewMouseLeftButtonUp -= VisualElement_PreviewMouseLeftButtonUp;
                                View.VisualElement.PreviewKeyDown -= VisualElement_PreviewKeyDown;
                                View.VisualElement.PreviewKeyUp -= VisualElement_PreviewKeyUp;
                            }
                        };
                    }
                }
            }
            catch (Exception exc)
            {
                OutputMsgForExc(exc.ToString());
            }
        }

        private void ViewLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            //foreach (var item in Document.Collection)
            //{
            //  var doc = item as EnvDTE.Document;
            //  var name = doc.FullName;
            //    //if (name == textDocument.FilePath)
            //    //{
            //    //    nowDocument = doc;
            //    //    break;
            //    //}
            //}
            // If a new snapshot wasn't generated, then skip this layout
            if (e.NewViewState.EditSnapshot != e.OldViewState.EditSnapshot)
            {
          //      UpdateAtCaretPosition(View.Caret.Position);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this.m_disposed)
            {
                if (disposing)
                {
                    View = null;
                }

                m_disposed = true;
            }
        }

        private void FireTagsChanged()
        {
            if (TagsChanged != null)
            {
                var snapshot = View.TextSnapshot;
                TagsChanged(this, new SnapshotSpanEventArgs(new SnapshotSpan(snapshot, 0, snapshot.Length)));
            }
        }

        void VisualElement_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // When the shift key is pressed, maybe the select operation is going to happens.
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)
                || e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                IsShiftDown = true;
                GetRootElement();
            }
        }

        void VisualElement_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.LeftAlt))
            {
                if (TmpWordSpans != null && TmpWordSpans.Count() == 0)
                {
                    return;
                }
                if (e.Key == Key.Left)
                {
                    // Make the previous similar element highlighted.
                    MoveSelection("PREV");
                }
                else if (e.Key == Key.Right)
                {
                    // Make the next similar element highlighted.
                    MoveSelection("NEXT");
                }
            }
            else if (Keyboard.IsKeyDown(Key.Escape) || e.Key == Key.Escape)
            {
                // When the ESC key is pressed, the highlighting will be canceled.
                if (WordSpans.Count() != 0)
                {
                    WordSpans = new NormalizedSnapshotSpanCollection();
                    TmpWordSpans = new NormalizedSnapshotSpanCollection();
                    CurrentSelectNum = 0;
                    var tempEvent = TagsChanged;
                    if (tempEvent != null)
                    {
                        Locations.Clear();
                        // Refresh the text of the current editor window.
                        tempEvent(this, new SnapshotSpanEventArgs(new SnapshotSpan(
                            SourceBuffer.CurrentSnapshot, 0, SourceBuffer.CurrentSnapshot.Length)));
                        if (OptionPage.MarginEnabled)
                        {
                            NewSpanAll.Clear();
                            MarginElement.SetCurrentPoint(
                                View.TextSnapshot.GetLineFromLineNumber(0).Start);
                        }
                    }
                }
            }
            else if (IsShiftDown && !Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
            {
                // When the select opeartion by keyboard is over.
                RequestSelection = Document.Selection;
                var tmpTxt = RequestSelection.Text.Trim();
                if (tmpTxt != "")
                {
                    // Highlight by background thread.
                    ThreadStartHighlighting();
                }
            }
        }

        //public static void SetMargin(SimilarMarginElement marginElement) {
        //    MarginElement = marginElement;
        //}

        void VisualElement_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (OptionPage.Enabled == false) return;

            if (e.ClickCount == 2)
            {
                RequestSelection = Document.Selection;
            }
            else
            {
                RequestSelection = Document.Selection;
            }

            if (RequestSelection.Text.Trim() != "")
            {
                if (OptionPage.MarginEnabled)
                {
                    //IsChecked = true;
                    MarginElement.SetCurrentPoint(
                        ConvertToPosition(RequestSelection.TopPoint));
                }
                // Output the selection logs.
                OutputSelectionLogs(RequestSelection);
                // Highlight by background thread.
                ThreadStartHighlighting();
            }
        }

        void GetRootElement()
        {
            try
            {
                // When the target file is edited and not saved, the position of selection is different from befrore.
                var currentTextDoc = Document.Object("TextDocument");
                var tmpSource = currentTextDoc.StartPoint.CreateEditPoint().GetText(currentTextDoc.EndPoint);

                TimeWatch.Init();
                // If the source code was changed, the new one must be used in the next processing.
                if (SourceCode == null || SourceCode != tmpSource)
                {
                    SourceCode = tmpSource;
                    TimeWatch.Start();
                    // If the converting errors, the exception will be catched.
                    RootElement = SyntaxTreeGenerator.GenerateXmlFromCodeText(SourceCode, true);
                    TimeWatch.Stop("GenerateXml");

                    //       TokenElements = RootElement.Descendants("TOKEN").ToList();
                }
            }
            catch (Exception exc)
            {
                OutputMsgForExc(exc.ToString());
            }
        }

        void HighlightSimilarElements() {
            
            try
            {
                if (IsShiftDown)
                {
                    // Reset the key state.
                    IsShiftDown = false;
                }
                    
                // Get the current source code.
                // Even the shift key is pressed down, the source code maybe edited.
                GetRootElement();
                                
                CodeRange currentRange;
                // Validation Check about selected range  and get the CodeRange 
                if (!GetCodeRange(out currentRange))
                {
                    return;
                }

                var currentSelection = RequestSelection;
                TimeWatch.Start();
                var currentElement = currentRange.FindOutermostElement(RootElement);
                TimeWatch.Stop("FindOutermostElement");

                // The selected element is same with before.
                if (!BuildLocationsFromTwoElements(currentRange, currentElement))
                    return;

                if (Locations.Count == 2)
                {
                    // Get the similar Elements.
                    var ret = InferrerSelector.GetSimilarElements(Locations,
                            RootElement, treeType);

                    PaneLineCnt = RequestSelection.TextPane.Height;
                    TimeWatch.Start();
                    // If no similar element is found then nothing will be highlighted.
                    if (ret.Count() == 0 || ret.First().Item1 == 0)
                    {
                        HaveSimilarElements = false;
                        // The element selected before current two element will be added to compare.
                        if (PreLocationInfo.XElement != null)
                        {
                            var tmpLocationInfo = Locations[0];
                            Locations[0] = PreLocationInfo;

                            // Get the similar Elements.
                            ret = InferrerSelector.GetSimilarElements(Locations,
                                    RootElement, treeType);
                            
                            // If no similar element is found then nothing will be highlighted.
                            if (ret.Count() == 0 || ret.First().Item1 == 0)
                            {
                                Locations[0] = tmpLocationInfo;
                                return;
                            }
                        }
                        else
                        {
                            return;
                        }
                    }

                    // Highlight operation.
                    Highlight(currentSelection, ret);
                }
            }
            catch (Exception exc)
            {
                OutputMsgForExc(exc.ToString());
            }
        }

        bool BuildLocationsFromTwoElements(CodeRange currentRange, XElement currentElement) {

            try
            {
                if (Locations.Where(ln => ln.XElement == currentElement).Count() > 0)
                {
                    return false;
                }

                var tmpLocationInfo = new LocationInfo
                {
                    CodeRange = currentRange,
                    XElement = currentElement,
                    IsNeedFix = NeedFixCheck(currentElement),
                }; 

                if (Locations.Count == 2 && HaveSimilarElements)
                {
                    // Save the second element when the current two elements have similar element.
                    PreLocationInfo = Locations[1];
                    Locations.Clear();
                }
                else if (Locations.Count == 2 && !HaveSimilarElements) {
                    // Save the first element when the current two elements have not similar element.
                    PreLocationInfo = Locations[0];

                    Locations[0] = tmpLocationInfo;
                    Locations.Reverse();
                    return true;
                }

                Locations.Add(tmpLocationInfo);
            }
            catch (Exception exc)
            {
                OutputMsgForExc(exc.ToString());
            }
            return true;
        }

        void Highlight(TextSelection currentSelection, IEnumerable<Tuple<int, CodeRange>> ret)
        {
            try
            {
                // Have the similar elements.
                HaveSimilarElements = true;

                var currentStart = CurrentWordForCheck.Start;
                var currentEnd = CurrentWordForCheck.End;

                if (Locations[0].IsNeedFix || Locations[1].IsNeedFix)
                {
                    FixKit = Tuple.Create(RegexNeedFix,
                                        Tuple.Create(startOffset, endOffset));
                    if (RegexNeedFix.IsMatch(CurrentWordForCheck.GetText()))
                    {
                        currentStart += 1;
                        currentEnd -= 1;
                    }
                }
                else
                {
                    FixKit = null;
                }

                if (NewSpanAll.Count > 0)
                {
                    NewSpanAll.Clear();
                }
                CurrentSelectNum = 0;
                highlightNo++;

                Parallel.ForEach(ret, tuple =>
                {
                    lock (buildLock)
                    {
                        // Build the data collecton of the similar elements.
                        BuildSimilarElementsCollection(tuple);
                    }
                });

                TimeWatch.Stop("BuildSimilarElementsCollection");
                if (NewSpanAll.Count == 0)
                {
                    return;
                }

                // Display the margin marks.
                if (OptionPage.MarginEnabled)
                {
                    this.RedrawMargin();
                }

                // Display the output datas.
                if (OutputWindow != null)
                {
                    System.Threading.Thread outputThread = new System.Threading.Thread(this.OutputSimilarDatas);
                    outputThread.IsBackground = true;
                    outputThread.Start();
                }

                NormalizedSnapshotSpanCollection wordSpan = new NormalizedSnapshotSpanCollection(NewSpanAll);

                // TODO if the elements of a line is bigger than 1, the position need to fix.
                var curSelection = wordSpan.AsParallel().Select((item, index) => new { Item = item, Index = index })
                        .FirstOrDefault(sel => (sel.Item.Start <= currentStart &&
                        sel.Item.End >= currentEnd));

                if (curSelection != null)
                {
                    CurrentSelectNum = curSelection.Index;
                    // TODO temp  added
                    TMPCurrentSelectNum = CurrentSelectNum;
                }
                
                // If another change hasn't happened, do a real update
                if (currentSelection == RequestSelection)
                    SynchronousUpdate(currentSelection, wordSpan, CurrentWordForCheck);
            }
            catch (Exception exc)
            {
                OutputMsgForExc(exc.ToString());
            }
        }

        private void OutputSimilarDatas() {

            OutputMsg("=================================Start:" + highlightNo + "==============================");

            var outputDataList = OutputDatas.OrderByDescending(t => t.Item1).ToList();
            outputDataList.ForEach(t => OutputSimilarElementData(t)); // To guarantee the outputing on the order. I give up "AsParallel().AsOrdered()".
            OutputDatas.Clear();
            outputDataList.Clear();
            OutputMsg("==================================End:" + highlightNo + "===============================");
        }

        public void SetCurrentScrollPointLine(SnapshotPoint currentPoint)
        {
            NormalizedSnapshotSpanCollection wordSpan = new NormalizedSnapshotSpanCollection(NewSpanAll);
            // TODO if the elements of a line is bigger than 1, the position need to fix.
            int linenum = this.View.TextSnapshot.GetLineNumberFromPosition(currentPoint.Position);
            try
            {
                var curSelection = wordSpan.Select((item, index) => new { Item = item, Index = index })
                        .FirstOrDefault(sel => (View.TextSnapshot.GetLineNumberFromPosition(sel.Item.Start.Position) == linenum));
                if (curSelection != null)
                {
                    CurrentSelectNum = curSelection.Index;
                    TMPCurrentSelectNum = CurrentSelectNum;
                    var newSpan = wordSpan.ElementAt(CurrentSelectNum);
                    var newStartOffset = ConvertToCharOffset(newSpan.Start);
                    Document.Selection.MoveToAbsoluteOffset(newStartOffset, false);
                    Document.Selection.MoveToAbsoluteOffset(newStartOffset + newSpan.Length, true);
                    if (OptionPage.MarginEnabled)
                    {
                        // Set the new element.
                        MarginElement.SetCurrentPoint(newSpan.Start);
                    }
                }
            }
            catch (Exception exc)
            {
                OutputMsgForExc(exc.ToString());
            }
        }
        
        private void RedrawMargin()
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                MarginElement.RedrawSimilarMargin();
            }));
        }

        // When the selected element does not contain the double quotation marks, 
        // but the token node contains them, 
        // the double quotation marks will be cutted when highlighting.
        bool NeedFixCheck(XElement currentElement)
        {
            try
            {
                // When selected word is between double quotation marks.
                var tokenElements = currentElement.DescendantsAndSelf().Where(el => el.IsToken()).ToList();
                if (tokenElements.Count() == 1 && tokenElements[0].TokenText().Length != RequestSelection.Text.Length
                        && RegexNeedFix.IsMatch(tokenElements[0].TokenText()))
                {
                    return true;
                }
            }
            catch (Exception exc)
            {
                OutputMsgForExc(exc.ToString());
            }
            return false;
        }

        bool GetCodeRange(out CodeRange newCodeRange)
        {
            CurrentWordForCheck = new SnapshotSpan(ConvertToPosition(RequestSelection.TopPoint),
                    ConvertToPosition(RequestSelection.BottomPoint));

            newCodeRange = CodeRange.ConvertFromIndicies(SourceCode,
                CurrentWordForCheck.Start.Position,
                CurrentWordForCheck.End.Position);

            if (CurrentWordForCheck.Start.Position == CurrentWordForCheck.End.Position)
                return false;

            // Validation Check
            if (this.CurrentWordForCheck != null)
            {
                var tmpCodeRange = newCodeRange;
                try
                {
                    // The select operation will be ignored if the selection is same with before.
                    if (Locations.Where(ln => ln.CodeRange == tmpCodeRange).Count() > 0)
                    {
                        return false;
                    }
                }
                catch (Exception exc)
                {
                    OutputMsgForExc(exc.ToString());
                }
            }
            
            return true;
        }

        // Convert TextSelection to SnapshotPoint
        // AbsoluteCharOffset count line break as 1 character.
        // Line and LineCharOffset begin at one.
        internal SnapshotPoint ConvertToPosition(TextPoint selectPoint)
        {
            int lineNum = selectPoint.Line - 1;
            int offset = selectPoint.LineCharOffset - 1;
            return View.TextSnapshot.GetLineFromLineNumber(lineNum).Start + offset;
        }

        // The position data will be converted from SnapshotPoint to TextSelection.
        internal int ConvertToCharOffset(SnapshotPoint point)
        {
            if (hasSR)
            {
                int lineNum = View.TextSnapshot.GetLineNumberFromPosition(point.Position);
                return point.Position - lineNum + 1;
            }
            return point.Position + 1;
        }

        void BuildSimilarElementsCollection(Tuple<int, CodeRange> tuple)
        {
            try{
                // Build the collecton of similar elements.
                var startAndEnd = tuple.Item2.ConvertToIndicies(SourceCode);

                SnapshotPoint tmpStart;
                SnapshotPoint tmpEnd;
                var fragment = SourceCode.Substring(startAndEnd.Item1, startAndEnd.Item2 - startAndEnd.Item1);
                
                if (FixKit != null && FixKit.Item1.IsMatch(fragment))
                {
                    tmpStart = new SnapshotPoint(View.TextSnapshot, startAndEnd.Item1 + FixKit.Item2.Item1);
                    tmpEnd = new SnapshotPoint(View.TextSnapshot, startAndEnd.Item2 - FixKit.Item2.Item2);
                }
                else {
                    tmpStart = new SnapshotPoint(View.TextSnapshot, startAndEnd.Item1);
                    tmpEnd = new SnapshotPoint(View.TextSnapshot, startAndEnd.Item2);
                }

                if (OutputWindow != null)
                {
                    // Add the output datas.
                    OutputDatas.Add(Tuple.Create(tuple.Item1, fragment, tuple.Item2));
                }
                //OutputSimilarElementLogs(tuple.Item1, fragment, tuple.Item2.StartLine + 1, 
                //    tuple.Item2.StartPosition + 1, tuple.Item2.EndPosition + 1);
                var tmpSpan = new SnapshotSpan(tmpStart, tmpEnd);

                NewSpanAll.Add(tmpSpan);
            }
            catch (Exception exc)
            {
                OutputMsgForExc(exc.ToString());
            }
        }

        private void OutputSelectionLogs(TextSelection textSelection)
        {
            if (OutputWindow != null)
            {
                OutputMsg("Selection: " + (++SelectionNo) + ", Line: " + textSelection.TopPoint.Line + ", Range: (" +
                    textSelection.TopPoint.LineCharOffset + ", " + textSelection.BottomPoint.LineCharOffset + "), Code: " +
                    textSelection.Text.Trim());
            }
        }

        private void OutputSimilarElementData(Tuple<int, string, CodeRange> tuple)
        {
            OutputMsg("Line: " + (tuple.Item3.StartLine + 1) + ", Range: (" + (tuple.Item3.StartPosition + 1) + ", " + (tuple.Item3.EndPosition + 1) + "), " +
                "Similarity: " + tuple.Item1 + ", Code: " + tuple.Item2);
        }

        private void OutputGeneralMsg(string strMsg)
        {
            if (OutputWindow != null)
            {
                OutputMsg(strMsg);
            }
        }

        public static void OutputMsg(string strMsg) {
            OutputWindow.WriteLine(strMsg);
        }

        public static void OutputMsgForExc(string strMsg)
        {
            if (OutputWindow != null)
            {
                OutputWindow.WriteLine(strMsg);
            }
            else {
                Debug.WriteLine(strMsg);
            }
        }

        void SynchronousUpdate(TextSelection CurrentSelection, NormalizedSnapshotSpanCollection newSpans, 
            SnapshotSpan? newCurrentWord)
        {
            lock (updateLock)
            {
                try { 
                    if (CurrentSelection != RequestSelection)
                        return;

                    WordSpans = newSpans;
                    CurrentWord = newCurrentWord;

                    TmpWordSpans = WordSpans;
                    var tempEvent = TagsChanged;
                    if (tempEvent != null)
                        tempEvent(this, new SnapshotSpanEventArgs(new SnapshotSpan(
                            SourceBuffer.CurrentSnapshot, 0, SourceBuffer.CurrentSnapshot.Length)));
                }
                catch (Exception exc)
                {
                    OutputMsgForExc(exc.ToString());
                }
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public IEnumerable<ITagSpan<HLTextTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            
            if (CurrentWord == null)
                yield break;

            // Hold on to a "snapshot" of the word spans and current word, so that we maintain the same
            // collection throughout
            var currentWord = CurrentWord.Value;
            var wordSpans = WordSpans;

            if (spans.Count == 0 || WordSpans.Count == 0)
                yield break;

            try
            {
                // If the requested snapshot isn't the same as the one our words are on, translate our spans to the expected snapshot
                if (spans[0].Snapshot != wordSpans[0].Snapshot)
                {
                    wordSpans = new NormalizedSnapshotSpanCollection(
                        wordSpans.Select(span => span.TranslateTo(spans[0].Snapshot, SpanTrackingMode.EdgeExclusive)));

                    TmpWordSpans = wordSpans;
                    currentWord = currentWord.TranslateTo(spans[0].Snapshot, SpanTrackingMode.EdgeExclusive);
                }
            }
            catch (Exception exc)
            {
                OutputMsgForExc(exc.ToString());
            }

            // First, yield back the word the cursor is under (if it overlaps)
            // Note that we'll yield back the same word again in the wordspans collection;
            // the duplication here is expected.
            // It's not necessary for current needs.
            bool blOverlapsWith = false;
            try
            {
                blOverlapsWith = spans.OverlapsWith(new NormalizedSnapshotSpanCollection(currentWord));
            }
            catch (Exception exc)
            {
                OutputMsgForExc(exc.ToString());
            }

            if (blOverlapsWith) {
                yield return new TagSpan<HLTextTag>(currentWord, new HLTextTag());
            }

            // Second, yield all the other words in the file
            foreach (SnapshotSpan span in NormalizedSnapshotSpanCollection.Overlap(spans, wordSpans))
            {
                // TODO: Maybe the next element will be determined by the cursor in new screen.                
                //var curSelection = wordSpans.AsParallel().Select((item, index) => new { Item = item, Index = index })
                //                .First(sel => (sel.Item.Start <= span.Start &&
                //                sel.Item.End >= span.End));

                //if (curSelection != null)
                //{
                //    var tmpindex = curSelection.Index;
                //    // If the new span which will be displayed is a long distance from the last selected element.
                //    if (tmpindex - CurrentSelectNum > 10 || CurrentSelectNum - tmpindex > 10)
                //        TMPCurrentSelectNum = tmpindex;
                //    else if ( tmpindex - CurrentSelectNum < 3 || CurrentSelectNum - tmpindex < 3) {
                //        CurrentSelectNum = TMPCurrentSelectNum;
                //    }
                //}
                yield return new TagSpan<HLTextTag>(span, new HLTextTag());
            }
        }

        private void MoveSelection(string selectType)
        {
            try{
                bool blEdge = false;
                if (TMPCurrentSelectNum != CurrentSelectNum)
                {
                    CurrentSelectNum = TMPCurrentSelectNum;
                }
                if (selectType == "NEXT")
                {
                    CurrentSelectNum += 1;
                    // When the current selected element is the last one, the NEXT move operation will be ignored.
                    if (TmpWordSpans.Count() <= CurrentSelectNum)
                    {
                        CurrentSelectNum = TmpWordSpans.Count() - 1;
                        blEdge = true;
                    }
                }
                else if (selectType == "PREV")
                {
                    CurrentSelectNum -= 1;
                    // When the current selected element is the first one, the PREV move operation will be ignored.
                    if (CurrentSelectNum < 0)
                    {
                        CurrentSelectNum = 0;
                        blEdge = true;
                    }
                }
                TMPCurrentSelectNum = CurrentSelectNum;

                if (blEdge) return;

                var selected = Document.Selection;
                if (selected != null)
                {
                    // Get the position data of the similar element.
                    var newSpan = TmpWordSpans.ElementAt(CurrentSelectNum);
                    var newStartOffset = ConvertToCharOffset(newSpan.Start);
                    // Make the similar element highlighted.
                    selected.MoveToAbsoluteOffset(newStartOffset, false);
                    selected.MoveToAbsoluteOffset(newStartOffset + newSpan.Length, true);
                    //IsChecked = true;
                    if (OptionPage.MarginEnabled)
                    {
                        // Set the new element.
                        MarginElement.SetCurrentPoint(newSpan.Start);
                    }
                }
            }
            catch (Exception exc)
            {
                OutputMsgForExc(exc.ToString());
            }
        }

        void ThreadStartHighlighting(bool isBackground = true)
        {
            try
            {
                if (isBackground)
                {
                    this.listenThread = new System.Threading.Thread(this.HighlightSimilarElements);
                    this.listenThread.IsBackground = true;
                    this.listenThread.Start();
                }
                else
                {
                    HighlightSimilarElements();
                }
            }
            catch (Exception exc)
            {
                OutputMsgForExc(exc.ToString());
            }
        }

        private void CheckLineBreakType()
        {
            int count = 0;
            int startIndex = 0;
            while (true)
            {
                int newIndex = SourceCode.IndexOf("\r\n", startIndex);
                if (newIndex >= 0)
                {
                    count++;
                    startIndex = newIndex + 1;
                    if (count > 10)
                    {
                        hasSR = true;
                        break;
                    }
                }
            }
        }
    }
}