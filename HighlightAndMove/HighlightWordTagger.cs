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
        SnapshotPoint RequestedPoint { get; set; }
        EnvDTE.Document document { get; set; }
        object updateLock = new object();

        // location datas 
        IEnumerable<LocationInfo> locations { get; set; }
        // the source code of current file
        string source_code { get; set; }
        // the max similarity of the similar Elements
        int Max_similarity { get; set; }
        // the collecton of highlighted elements
        ICollection<SnapshotSpan> newSpanAll { get; set; }        
        CodeRange cur_word { get; set; }
        // Count the number of left mouse button clicks
        int cntLeftClick { get; set; }
        // current selection
        TextSelection RequestSelection { get; set; }
        // the order number of current selection in highlighted elements
        int CurrentSelectNum { get; set; }
        // the position data collecton of highlighted elements
        ICollection<Tuple<int, int>> newSelectionAll { get; set; }


        public HighlightWordTagger(IWpfTextView view, ITextBuffer sourceBuffer, ITextSearchService textSearchService,
ITextStructureNavigator textStructureNavigator, EnvDTE.Document document)
        {
            this.cntLeftClick = 0;
            this.Max_similarity = 0;
            this.View = view;
            this.document = document;
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

            if (RequestSelection.Text != "" )
            {
                TextSelection CurrentSelection = RequestSelection;;
                FileInfo cur_file = new FileInfo(this.document.FullName);
                source_code = File.ReadAllText(this.document.FullName);
                var processor = new Code2Xml.Languages.ANTLRv3.Processors.CSharp.CSharpProcessorUsingAntlr3();
                var xml = processor.GenerateXml(cur_file);
                
                //IEnumerable<XElement> tests =
                //    from item in xml.Element("TOKEN")
                //    where (string)item == RequestSelection.Text
                //    select item;
                //foreach (XElement el in tests) { 
                    
                //}
                //    Console.WriteLine((string)el.Attribute("TestId"));

                List<SnapshotSpan> wordSpans = new List<SnapshotSpan>();
                GetCodeRangeBySelection(RequestSelection);

                SnapshotPoint tmpStart = new SnapshotPoint(this.View.TextSnapshot, ConvertToPosition(RequestSelection.TopPoint));
                SnapshotPoint tmpEnd = new SnapshotPoint(this.View.TextSnapshot, ConvertToPosition(RequestSelection.BottomPoint));

                SnapshotSpan currentWord = new SnapshotSpan(tmpStart, tmpEnd);
                
                //If we couldn't find a word, clear out the existing markers
                if (cur_word == null)
                {
                    return;
                }

                // It will compare two elements by default.
                if (locations == null || locations.Count<LocationInfo>() == 2)
                {
                    locations = new[] {new LocationInfo {
                        CodeRange = cur_word,
				        FileInfo = cur_file,
			        }};
                }
                else
                {
                    locations = locations.Concat(new[] {new LocationInfo {
                        CodeRange = cur_word,
				        FileInfo = cur_file,
			        }});

                    var ret = Inferrer.GetSimilarElements(processor, locations,
                        new[] { cur_file });

                    newSpanAll = new List<SnapshotSpan>();
                    newSelectionAll = new List<Tuple<int, int>>();
                    Max_similarity = 0;
                    WordSpans = null;
                    CurrentSelectNum = 0;

                    foreach (var tuple in ret.Take(10))
                    {
                        // if the similarity is 0
                       if (tuple.Item1 == 0) {
                            break;
                        }

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
                    newSelectionAll.OrderBy(sel => sel.Item1);

                    // get the order number of current selection in the highlighted elements
                    CurrentSelectNum = newSelectionAll.Select( (item, index) => new {Item = item, Index = index})
                        .First(sel => sel.Item.Item1 == RequestSelection.TopPoint.AbsoluteCharOffset).Index;                    
                  
                    //If another change hasn't happened, do a real update
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

        //private void OnKeyUp(object sender, RoutedEventArgs e)
        //{
        //    if ((e as KeyEventArgs).ctrl == true)
        //    {
        //        if ((e as KeyEventArgs).key == Key.Left) {
        //            WlSelect("fwd");
        //        }
        //        else if ((e as KeyEventArgs).Key == Key.Right)
        //        {
        //            WlSelect("bwd");
        //        }
        //    }
        //}

        //void ViewLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        //{
        //    // If a new snapshot wasn't generated, then skip this layout
        //    if (e.NewSnapshot != e.OldSnapshot)
        //    {
        //        UpdateAtCaretPosition(View.Caret.Position);
        //    }
        //}

        //void CaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
        //{
            
        //        UpdateAtCaretPosition(e.NewPosition);
        //}
                

        void GetCodeRangeBySelection(TextSelection select)
        {
            
            //bool foundWord = true;
            ////If we've selected something not worth highlighting, we might have missed a "word" by a little bit
            //if (!WordExtentIsValid(currentRequest, word))
            //{
            //    //Before we retry, make sure it is worthwhile
            //    if (word.Span.Start != currentRequest
            //         || currentRequest == currentRequest.GetContainingLine().Start
            //         || char.IsWhiteSpace((currentRequest - 1).GetChar()))
            //    {
            //        foundWord = false;
            //    }
            //    else
            //    {
            //        // Try again, one character previous. 
            //        //If the caret is at the end of a word, pick up the word.
            //        word = TextStructureNavigator.GetExtentOfWord(currentRequest - 1);

            //        //If the word still isn't valid, we're done
            //        if (!WordExtentIsValid(currentRequest, word))
            //            foundWord = false;
            //    }
            //}

            //if (!foundWord)
            //{
            //    //If we couldn't find a word, clear out the existing markers
            //    SynchronousUpdate(currentRequest, new NormalizedSnapshotSpanCollection(), null);
            //    return;
            //}

            //CurrentWord = word.Span;

            cur_word = CodeRange.ConvertFromIndicies(source_code, ConvertToPosition(select.TopPoint) - 1, ConvertToPosition(select.BottomPoint));
        }

        // the position data will be converted from TextSelection to SnapshotPoint
        internal static int ConvertToPosition(TextPoint point)
        {
            return point.AbsoluteCharOffset + point.Line - 2;
        }
        // the position data will be converted from SnapshotPoint to TextSelection
        internal static int ConvertToCharOffset(SnapshotPoint point, int line)
        {
            return point.Position - line + 2;
        }


        int HightlightSimilarElements(Tuple<int, LocationInfo> tuple)
        {
            if (Max_similarity == 0)
            {
                // set the max similarity
                Max_similarity = tuple.Item1;
            }
            else {
                // if the similarity is less than the max similarity
                if (tuple.Item1 != Max_similarity) {
                    return -1;
                }
            }

            // build the collecton of highlighted elements
            int newInclusiveStart = 0;
            int newExclusiveEnd = 0;
            tuple.Item2.CodeRange.ConvertToIndicies(source_code, out newInclusiveStart, out newExclusiveEnd);

            SnapshotPoint tmpStart = new SnapshotPoint(this.View.TextSnapshot, newInclusiveStart + 1);
            SnapshotPoint tmpEnd = new SnapshotPoint(this.View.TextSnapshot, newExclusiveEnd);
            SnapshotSpan s_span = new SnapshotSpan(tmpStart, tmpEnd);

            newSpanAll.Add(s_span);

            // build the position data collecton of highlighted elements
            int line = tuple.Item2.CodeRange.StartLine;
            Tuple<int, int> tmpSelection = new Tuple<int,int>(
                ConvertToCharOffset(tmpStart, line), ConvertToCharOffset(tmpEnd, line));
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
