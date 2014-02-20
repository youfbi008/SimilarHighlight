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


namespace HighlightAndMove
{
    internal class HighlightWordTagger : ITagger<HighlightWordTag>
    {
        IWpfTextView View { get; set; }
        ITextBuffer SourceBuffer { get; set; }
        ITextSearchService TextSearchService { get; set; }
        ITextStructureNavigator TextStructureNavigator { get; set; }
        NormalizedSnapshotSpanCollection WordSpans { get; set; }
        SnapshotSpan? CurrentWord { get; set; }
        EnvDTE.Document document { get; set; }
        object updateLock = new object();

        // location datas 
        IEnumerable<LocationInfo> locations { get; set; }
        // the source code of current file
        string source_code { get; set; }
        // the collecton of highlighted elements
        ICollection<SnapshotSpan> newSpanAll { get; set; }
        // Count the number of left mouse button clicks
        int cntLeftClick { get; set; }
        // current selection
        TextSelection RequestSelection { get; set; }
        // the order number of current selection in highlighted elements
        int CurrentSelectNum { get; set; }
        // the position data collecton of highlighted elements
        ICollection<Tuple<int, int>> newSelectionAll { get; set; }
        CSharpProcessorUsingAntlr3 processor;
        FileInfo currentFile { get; set; }
        List<XElement> tokenElements { get; set; }

        public HighlightWordTagger(IWpfTextView view, ITextBuffer sourceBuffer, ITextSearchService textSearchService,
ITextStructureNavigator textStructureNavigator, EnvDTE.Document document)
        {
            this.document = document;
            if (this.document != null && this.currentFile == null)
            {
                this.processor = new Code2Xml.Languages.ANTLRv3.Processors.CSharp.CSharpProcessorUsingAntlr3();
                this.currentFile = new FileInfo(this.document.FullName);
                var xml = processor.GenerateXml(currentFile);
                //var elements = xml.Descendants("identifier").ToList();
                //// Get the data list of TOKEN
                //this.tokenElements = elements.Descendants("TOKEN").ToList();
            }

            this.cntLeftClick = 0;
            this.View = view;
            
            this.View.VisualElement.PreviewMouseLeftButtonUp += VisualElement_PreviewMouseLeftButtonUp;
            this.View.VisualElement.PreviewMouseDown += VisualElement_PreviewMouseDown;
            this.View.VisualElement.PreviewKeyUp += VisualElement_PreviewKeyUp;
            this.SourceBuffer = sourceBuffer;
            this.TextSearchService = textSearchService;
            this.TextStructureNavigator = textStructureNavigator;
            this.WordSpans = new NormalizedSnapshotSpanCollection();
            this.CurrentWord = null;
   //         this.View.Caret.PositionChanged += CaretPositionChanged;
       //     this.View.LayoutChanged += ViewLayoutChanged;
        }

        // TODO 1. the selection by pressing keys.
        // TODO 2. If we've selected something not worth highlighting, we might have missed a "word" 
        void VisualElement_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            
            if (cntLeftClick == 2)
            {
                cntLeftClick = 0;
                RequestSelection = this.document.Selection;
            }
            else
            {
                RequestSelection = this.document.Selection;
            }

            if (RequestSelection.Text != "")// || RequestSelection.Text.Length > 100
            {
                TextSelection CurrentSelection = RequestSelection;;
                source_code = File.ReadAllText(this.document.FullName);

                CodeRange currentRange = new CodeRange();

                // Validation Check
                //if (!IsValidSelection(ref currentRange) || currentRange == null)
                //{
                //    return;
                //}

                SnapshotPoint currentStart = ConvertToPosition(RequestSelection.TopPoint);
                SnapshotPoint currentEnd = ConvertToPosition(RequestSelection.BottomPoint);
                SnapshotSpan currentWord = new SnapshotSpan(currentStart, currentEnd);
                
                List<SnapshotSpan> wordSpans = new List<SnapshotSpan>();
                currentRange = GetCodeRangeBySelection(currentWord);                
                
                // It will compare two elements by default.
                if (locations == null || locations.Count<LocationInfo>() == 2)
                {
                    locations = new[] {new LocationInfo {
                        CodeRange = currentRange,
				        FileInfo = currentFile,
			        }};
                }
                else
                {
                    locations = locations.Concat(new[] {new LocationInfo {
                        CodeRange = currentRange,
				        FileInfo = currentFile,
			        }});

                    Inferrer.SimilarityRange = 5;
                    var ret = Inferrer.GetSimilarElements(processor, locations,
                        new[] { currentFile });

                    // There is no similar element.
                    if (ret.Count() == 0 || ret.First().Item1 == 0)
                    { 
                        return;
                    }

                    newSpanAll = new List<SnapshotSpan>();
                    newSelectionAll = new List<Tuple<int, int>>();
                    WordSpans = null;
                    CurrentSelectNum = 0;
             
                    foreach (var tuple in ret)
                    {
                        // if the similarity is less than the max similarity, it will be stopped
                        if (HightlightSimilarElements(tuple) < 0) {
                            break;
                        }

                        var score = tuple.Item1;
                        var location = tuple.Item2;                        
                        var startAndEnd = location.CodeRange.ConvertToIndicies(source_code);
                        var fragment = source_code.Substring(startAndEnd.Item1, startAndEnd.Item2 - startAndEnd.Item1);
                        Debug.WriteLine("Similarity: " + score + ", code: " + fragment);
                        Console.WriteLine("Similarity: " + score + ", code: " + fragment);
                    }

                    if (newSpanAll.Count == 0) {
                        return;
                    }

                    wordSpans.AddRange(newSpanAll);
                    newSelectionAll = newSelectionAll.OrderBy(sel => sel.Item1).ToList();

                    // Get the order number of current selection in the highlighted elements
                    var curSelection = newSelectionAll.Select((item, index) => new { Item = item, Index = index })
                        .First(sel => sel.Item.Item1 <= RequestSelection.TopPoint.AbsoluteCharOffset &&
                        sel.Item.Item2 >= RequestSelection.BottomPoint.AbsoluteCharOffset
                        );

                    if (curSelection != null)
                    {
                        CurrentSelectNum = curSelection.Index;
                    }
                    // If another change hasn't happened, do a real update
                    if (CurrentSelection == RequestSelection)
                        SynchronousUpdate(CurrentSelection, new NormalizedSnapshotSpanCollection(wordSpans), currentWord);
                }
            }
        }

        void VisualElement_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.LeftAlt))
            {
                if (e.Key == Key.Left)
                {
                    // go to the previous highlighted element
                    MoveSelection("bwd");
                }
                else if (e.Key == Key.Right)
                {
                    // go to the next highlighted element
                    MoveSelection("fwd");
                }
            }
        }

        void VisualElement_PreviewMouseDown(object sender, RoutedEventArgs e)
        {
            // Double click
            if ((e as MouseButtonEventArgs).ClickCount == 2 && (e as MouseButtonEventArgs).LeftButton == MouseButtonState.Pressed)
            {
                cntLeftClick = 2;
                return;          
            }
        }

        bool IsValidSelection(ref CodeRange currentRange)
        {
            // Repeat check   
            if (this.CurrentWord != null)
            {
                SnapshotPoint currentStart = ConvertToPosition(RequestSelection.TopPoint);
                SnapshotPoint currentEnd = ConvertToPosition(RequestSelection.BottomPoint);

                if (currentStart.Position == ((SnapshotSpan)this.CurrentWord).Start.Position &&
                    currentEnd.Position == ((SnapshotSpan)this.CurrentWord).End.Position)
                {
                    return false;
                }
            }

            // Exist check
            var check = tokenElements.FindAll(el => el.Value == RequestSelection.Text &&
                el.Attribute("startline").Value == RequestSelection.TopPoint.Line.ToString() &&
                el.Attribute("startpos").Value == (RequestSelection.TopPoint.LineCharOffset - 1).ToString());

            // if the selected word is not the whole word. it will be ignored.
            if (check.Count != 1)
            {
                return false;
            }
            else {
                var anceIdentifiers = check[0].Ancestors("identifier").ToList();
                if (anceIdentifiers.Count == 1) {
                    currentRange = CodeRange.Locate(anceIdentifiers[0]);
                }                
            }

            return true;
        }

        CodeRange GetCodeRangeBySelection(SnapshotSpan currentWord)
        {

          //  return null;
            return CodeRange.ConvertFromIndicies(source_code, currentWord.Start.Position , currentWord.End.Position);
        }

        // convert TextSelection to SnapshotPoint
        // AbsoluteCharOffset count line break as 1 character.
        // Line and LineCharOffset begin at one.
        internal SnapshotPoint ConvertToPosition(TextPoint selectPoint)
        {
            int lineNum = selectPoint.Line - 1;
            int offset = selectPoint.LineCharOffset - 1;
            return this.View.TextSnapshot.GetLineFromLineNumber(lineNum).Start + offset;
            //return point.AbsoluteCharOffset + point.Line - 2;
        }

        // the position data will be converted from SnapshotPoint to TextSelection
        internal int ConvertToCharOffset(SnapshotPoint point)
        {
            int lineNum = this.View.TextSnapshot.GetLineNumberFromPosition(point.Position);
            return point.Position - lineNum + 1;
        }

        int HightlightSimilarElements(Tuple<int, LocationInfo> tuple)
        {
            
            // build the collecton of highlighted elements
            int newInclusiveStart = 0;
            int newExclusiveEnd = 0;
            tuple.Item2.CodeRange.ConvertToIndicies(source_code, out newInclusiveStart, out newExclusiveEnd);

            SnapshotPoint tmpStart = new SnapshotPoint(this.View.TextSnapshot, newInclusiveStart);
            SnapshotPoint tmpEnd = new SnapshotPoint(this.View.TextSnapshot, newExclusiveEnd);
            SnapshotSpan s_span = new SnapshotSpan(tmpStart, tmpEnd);

            newSpanAll.Add(s_span);

            // build the position data collecton of highlighted elements
            Tuple<int, int> tmpSelection = new Tuple<int,int>(
                ConvertToCharOffset(tmpStart), ConvertToCharOffset(tmpEnd));
            newSelectionAll.Add(tmpSelection);
            return 0;
        }
        static bool WordExtentIsValid(SnapshotPoint currentRequest, TextExtent word)
        {
            return word.IsSignificant
                && currentRequest.Snapshot.GetText(word.Span).Any(c => char.IsLetter(c));
        }

        private SnapshotSpan? GetExpectedSpan(SnapshotSpan tmpSpan, FindData findData)
        {

            SnapshotSpan? endSpan = ((ITextSearchService2)TextSearchService).Find(tmpSpan.End, findData.SearchString, findData.FindOptions);

            if (endSpan == null) {

                return null;
            }
            SnapshotSpan expectedSpan = new SnapshotSpan(tmpSpan.End, ((SnapshotSpan)endSpan).Start);

            return expectedSpan;
        }

        void SynchronousUpdate(TextSelection CurrentSelection, NormalizedSnapshotSpanCollection newSpans, SnapshotSpan? newCurrentWord)
        {
            lock (updateLock)
            {
                if (CurrentSelection != RequestSelection)
                    return;

                WordSpans = newSpans;
                CurrentWord = newCurrentWord;

                var tempEvent = TagsChanged;
                if (tempEvent != null)
                    tempEvent(this, new SnapshotSpanEventArgs(new SnapshotSpan(SourceBuffer.CurrentSnapshot, 0, SourceBuffer.CurrentSnapshot.Length)));
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public IEnumerable<ITagSpan<HighlightWordTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (CurrentWord == null)
                yield break;

            // Hold on to a "snapshot" of the word spans and current word, so that we maintain the same
            // collection throughout
            SnapshotSpan currentWord = CurrentWord.Value;
            NormalizedSnapshotSpanCollection wordSpans = WordSpans;

            if (spans.Count == 0 || WordSpans.Count == 0)
                yield break;

            // If the requested snapshot isn't the same as the one our words are on, translate our spans to the expected snapshot
            if (spans[0].Snapshot != wordSpans[0].Snapshot)
            {
                wordSpans = new NormalizedSnapshotSpanCollection(
                    wordSpans.Select(span => span.TranslateTo(spans[0].Snapshot, SpanTrackingMode.EdgeExclusive)));

                currentWord = currentWord.TranslateTo(spans[0].Snapshot, SpanTrackingMode.EdgeExclusive);
            }

            // First, yield back the word the cursor is under (if it overlaps)
            // Note that we'll yield back the same word again in the wordspans collection;
            // the duplication here is expected.
            // It's not necessary for current needs.
            //if (spans.OverlapsWith(new NormalizedSnapshotSpanCollection(currentWord)))
            //    yield return new TagSpan<HighlightWordTag>(currentWord, new HighlightWordTag());

            // Second, yield all the other words in the file
            foreach (SnapshotSpan span in NormalizedSnapshotSpanCollection.Overlap(spans, wordSpans))
            {
                yield return new TagSpan<HighlightWordTag>(span, new HighlightWordTag());
            }
        }

        private void MoveSelection(string selectType)
        {

            TextSelection selected = this.document.Selection;
            
            if (selected != null)
            {
                if (selectType == "fwd")
                {
                    CurrentSelectNum = CurrentSelectNum + 1; 

                    Tuple<int, int> newSelection = newSelectionAll.ElementAt(CurrentSelectNum);

                    selected.MoveToAbsoluteOffset(newSelection.Item1, false);
                    selected.MoveToAbsoluteOffset(newSelection.Item2, true);
                }
                else if (selectType == "bwd")
                {

                    CurrentSelectNum = CurrentSelectNum - 1;

                    if (CurrentSelectNum < 0) { 
                        CurrentSelectNum = 0;
                        return;
                    }

                    Tuple<int, int> newSelection = newSelectionAll.ElementAt(CurrentSelectNum);

                    selected.MoveToAbsoluteOffset(newSelection.Item1, false);
                    selected.MoveToAbsoluteOffset(newSelection.Item2, true);
                }
            }
        }
    }
}
