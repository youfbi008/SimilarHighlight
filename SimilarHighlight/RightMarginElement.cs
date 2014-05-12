using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using SimilarHighlight.OverviewMargin;
using System.Diagnostics;

namespace SimilarHighlight
{
    [Export(typeof(EditorOptionDefinition))]
    internal sealed class Enabled : EditorOptionDefinition<bool>
    {
        public override bool Default { get { return true; } }
        public override EditorOptionKey<bool> Key { get { return RightMarginElement.EnabledOptionId; } }
    }

    [Export(typeof(EditorOptionDefinition))]
    internal sealed class CaretColor : EditorOptionDefinition<Color>
    {
        public override Color Default { get { return Colors.Black; } }//Colors.MediumBlue
        public override EditorOptionKey<Color> Key { get { return RightMarginElement.CaretColorId; } }
    }

    [Export(typeof(EditorOptionDefinition))]
    internal sealed class MatchColor : EditorOptionDefinition<Color>
    {
        public override Color Default { get { return Colors.MediumPurple; } }
        public override EditorOptionKey<Color> Key { get { return RightMarginElement.MatchColorId; } }
    }

    [Export(typeof(EditorOptionDefinition))]
    internal sealed class AdornmentMatchColor : EditorOptionDefinition<Color>
    {
        //Off by default
        public override Color Default { get { return Color.FromArgb(0x40, 0x93, 0x70, 0xDB); } }
        public override EditorOptionKey<Color> Key { get { return RightMarginElement.AdornmentMatchColorId; } }
    }

    [Export(typeof(EditorOptionDefinition))]
    internal sealed class MarginWidth : EditorOptionDefinition<double>
    {
        public override double Default { get { return 10.0; } }
        public override bool IsValid(ref double proposedValue)
        {
            return (proposedValue >= 3.0) && (proposedValue <= 60.0);
        }
        public override EditorOptionKey<double> Key { get { return RightMarginElement.MarginWidthId; } }
    }

    [Export(typeof(FrameworkElement))]
    /// <summary>
    /// Helper class to handle the rendering of the caret margin.
    /// </summary>
    class RightMarginElement : FrameworkElement
    {
        private IList<SnapshotSpan> SpanAll { get; set; }
        public static int OldHighlightNo { get { return oldHighlightNo; } set { oldHighlightNo = value; } }
        private static int oldHighlightNo { get; set; }
        private readonly IWpfTextView textView;
   //     private readonly IAdornmentLayer layer;
        private readonly IVerticalScrollBar scrollBar;

        private BackgroundSearch search = null;
        private string highlight = null;
        private SnapshotSpan? highlightSpan = null;

        private Brush caretBrush;
        private Brush matchBrush;
        private SnapshotPoint currentPoint;

        private bool hasEvents = false;

        const double MarkPadding = 0.5;
        const double MarkThickness = 4.0;

        public static readonly EditorOptionKey<bool> EnabledOptionId = new EditorOptionKey<bool>("RightMargin/Enabled");
        public static readonly EditorOptionKey<Color> CaretColorId = new EditorOptionKey<Color>("RightMargin/CaretColor");
        public static readonly EditorOptionKey<Color> MatchColorId = new EditorOptionKey<Color>("RightMargin/MatchColor");
        public static readonly EditorOptionKey<Color> AdornmentMatchColorId = new EditorOptionKey<Color>("RightMargin/AdornmentMatchColor");
        public static readonly EditorOptionKey<double> MarginWidthId = new EditorOptionKey<double>("RightMargin/MarginWidth");
        public static readonly string CaretMarginRoot = "RightMargin/CaretMarginRoot";

        /// <summary>
        /// Constructor for the CaretMarginElement.
        /// </summary>
        /// <param name="textView">ITextView to which this CaretMargenElement will be attached.</param>
        /// <param name="factory">Instance of the CaretMarginFactory that is creating the margin.</param>
        /// <param name="verticalScrollbar">Vertical scrollbar of the ITextViewHost that contains <paramref name="textView"/>.</param>
        [ImportingConstructor]
        public RightMarginElement(IWpfTextView textView, RightMarginFactory factory, IVerticalScrollBar verticalScrollbar)
        {
            this.textView = textView;
      //      this.layer = textView.GetAdornmentLayer("CaretAdornmentLayer");
            oldHighlightNo = 0;
            factory.LoadOption(textView.Options, RightMarginElement.EnabledOptionId.Name);
            factory.LoadOption(textView.Options, RightMarginElement.CaretColorId.Name);
            factory.LoadOption(textView.Options, RightMarginElement.MatchColorId.Name);
            factory.LoadOption(textView.Options, RightMarginElement.AdornmentMatchColorId.Name);
            factory.LoadOption(textView.Options, RightMarginElement.MarginWidthId.Name);

            this.scrollBar = verticalScrollbar;

            //Make our width big enough to see, but not so big that it consumes a lot of
            //real-estate.
            this.Width = textView.Options.GetOptionValue(RightMarginElement.MarginWidthId);

            this.caretBrush = GetBrush(RightMarginElement.CaretColorId);
            this.matchBrush = GetBrush(RightMarginElement.MatchColorId);

            this.textView.Closed += OnClosed;

            this.OnOptionsChanged(null, null);
            this.textView.Options.OptionChanged += this.OnOptionsChanged;
         //   this.textView.Selection.SelectionChanged += Selection_SelectionChanged;

            this.UpdateEventHandlers(null);

            this.IsVisibleChanged += delegate(object sender, DependencyPropertyChangedEventArgs e)
            {
                this.hasEvents = false;
               
                this.UpdateEventHandlers(null);
            };
        }

        void Selection_SelectionChanged(object sender, EventArgs e)
        {
   //         throw new NotImplementedException();
            if (this.textView.Selection.Start != this.textView.Selection.End && HLTextTagger.IsChecked)
            {
                
          //      this.UpdateMarginMatches(false);
            }
        }

        public void SetCurrentPoint(SnapshotPoint currentPoint)
        {
            this.currentPoint = currentPoint;
            this.InvalidateVisual();
        }

        private void UpdateEventHandlers(bool? forceEvents)
        {
            bool needEvents = forceEvents.HasValue ? forceEvents.Value : (this.IsVisible); //|| (this.adornmentMatchBrush != null)

            if (needEvents != this.hasEvents)
            {
                this.textView.Selection.SelectionChanged += Selection_SelectionChanged;
                this.hasEvents = needEvents;
                if (needEvents)
                {
                    
              //      this.textView.Caret.PositionChanged += OnPositionChanged;
           //         this.scrollBar.TrackSpanChanged += OnTrackSpanChanged;

                    //Rescan everything since things might have changed.
         //           this.UpdateMarginMatches(true);
                }
                else
                {
              //      this.textView.Caret.PositionChanged -= OnPositionChanged;
              //      this.scrollBar.TrackSpanChanged -= OnTrackSpanChanged;

                    if (this.search != null)
                    {
                        this.search.Abort();
                        this.search = null;
                    }
                    this.highlight = null;
                    this.highlightSpan = null;
                }
            }
        }

        private Brush GetBrush(EditorOptionKey<Color> key)
        {
            Brush brush = null;

            Color color = this.textView.Options.GetOptionValue(key);
            if (color.A != 0)
            {
                brush = new SolidColorBrush(color);
                brush.Freeze();
            }

            return brush;
        }

        public void Dispose()
        {
            this.textView.Options.OptionChanged -= this.OnOptionsChanged;
        }

        public bool Enabled
        {
            get
            {
                return this.textView.Options.GetOptionValue<bool>(RightMarginElement.EnabledOptionId);
            }
        }

        private void OnOptionsChanged(object sender, EventArgs e)
        {
            this.Visibility = this.Enabled ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Handler for either the caret position changing or a change
        /// in the mapping of the scroll bar.
        /// </summary>
        private void OnPositionChanged(object sender, EventArgs e)
        {
            //if (this.textView.Selection. != "")
            //{
            //    this.UpdateMarginMatches(false);
            //}
        }

        /// <summary>
        /// Handler for the scrollbar changing its coordinate mapping.
        /// </summary>
        private void OnTrackSpanChanged(object sender, EventArgs e)
        {
            //Simply invalidate the visual: the positions of the various highlights haven't changed.
            this.InvalidateVisual();
        }

        void OnClosed(object sender, EventArgs e)
        {
            this.UpdateEventHandlers(false);
            this.textView.Closed -= OnClosed;
        }

        public void RedrawRightMargin() {
            this.InvalidateVisual();
        }

        /// <summary>
        /// Start a background search for all instances of this.highlight.
        /// </summary>
        public void UpdateMarginMatches(bool force)
        {
            if (((this.matchBrush != null) && this.IsVisible)) // || (this.adornmentMatchBrush != null)
            {
             //   SpanAll = HLTextTagger.NewSpanAll;
                //SnapshotSpan? oldHighlightSpan = this.highlightSpan;
                //string oldHighlight = this.highlight;

                //this.highlightSpan = BackgroundSearch.GetExtentOfWord(this.textView.Caret.Position.BufferPosition);
                //this.highlight = this.highlightSpan.HasValue ? this.highlightSpan.Value.GetText() : null;

                //if ((this.highlight != oldHighlight) || force)
                //{
                    //The text of the highlight changes ... restart the search.
                    if (this.search != null)
                    {
                        //Stop and blow away the old search (even if it didn't finish, the results are not interesting anymore).
                        this.search.Abort();
                        this.search = null;
                    }

                    if (HLTextTagger.NewSpanAll.Count > 0 || oldHighlightNo == 0)//this.highlight != null && 
                    {
                        if (RightMarginElement.OldHighlightNo != HLTextTagger.HighlightNo)
                        {
                            RightMarginElement.OldHighlightNo = HLTextTagger.HighlightNo;
                            this.SpanAll = HLTextTagger.NewSpanAll;
                            //break;
                        }

                     //   oldHighlightNo = HLTextTagger.HighlightNo;

                        //The underlying buffer could be very large, meaning that doing the search for all matches on the UI thread
                        //is a bad idea. Do the search on the background thread and use a callback to invalidate the visual when
                        //the entire search has completed.
                        //this.search = new BackgroundSearch(this.textView.TextSnapshot, this.highlight,
                        //                                    delegate
                        //                                    {
                        //                                        //Force the invalidate to happen on the UI thread to satisfy WPF
                        //                                        this.Dispatcher.Invoke(DispatcherPriority.Normal,
                        //                                                                new DispatcherOperationCallback(delegate
                        //                                                                {
                        //                                                                    this.InvalidateVisual();
                        //                                                               //     this.RedrawAdornments(null);
                        //                                                                    return null;
                        //                                                                }),
                        //                                                                null);
                        //                                    });
               //         this.search.Matches = new List<SnapshotSpan>(HLTextTagger.NewSpanAll);
                    }
                    else
                    {
                        //no highlight == no adornments or marks.
                 //       this.layer.RemoveAllAdornments();
                        this.InvalidateVisual();
                    }
                //}
                //else if (oldHighlight != null)
                //{
                //    //The highlight didn't change and isn't null ... therefore both old & new highlight spans have values.
                //    SnapshotSpan translatedOldHighlightSpan = oldHighlightSpan.Value.TranslateTo(this.textView.TextSnapshot, SpanTrackingMode.EdgeInclusive);
                //    if (translatedOldHighlightSpan != this.highlightSpan.Value)
                //    {
                //        if (this.adornmentMatchBrush != null)
                //        {
                //            //The spans moved (e.g. the user moved from this on one line to this on another).
                //            //Remove the adornment from the new highlight.
                //            this.layer.RemoveAdornmentsByVisualSpan(this.highlightSpan.Value);

                //            //Add an adornment at the old source of the highlight span.
                //            Geometry g = this.textView.TextViewLines.GetMarkerGeometry(translatedOldHighlightSpan);
                //            if (g != null)
                //            {
                //                this.layer.AddAdornment(translatedOldHighlightSpan, null, new GeometryAdornment(this.adornmentMatchBrush, g));
                //            }
                //        }

                //        //We also need to update the caret position in the margin.
                //        this.InvalidateVisual();
                //    }
                //}
            }
            //else if (this.IsVisible && (this.caretBrush != null))
            //{
            //    //Neither the match brush nor the adornment brush exists so we won't be doing a background search.
            //    //But we are visible and the caret brush exists, so invalidate the visual so that we can update the location of the caret.
            //    this.InvalidateVisual();
            //}
        }

        /// <summary>
        /// Override for the FrameworkElement's OnRender. When called, redraw
        /// all of the markers 
        /// </summary>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (HLTextTagger.NewSpanAll != null)
            {
                //There is a word that should be highlighted. It doesn't matter whether or not the search has completed or
                //is still in progress: draw red marks for each match found so far (the completion callback on the search
                //will guarantee that the highlight display gets invalidated once the search has completed).

                //Take a snapshot of the matches found to date (this could still be changing
                //if the search has not completed yet).
                IList<SnapshotSpan> matches = HLTextTagger.NewSpanAll;

                try
                {
                    double lastY = double.MinValue;
                    int markerCount = Math.Min(1000, matches.Count);
                    for (int i = 0; (i < markerCount); ++i)
                    {
                        //Get (for small lists) the index of every match or, for long lists, the index of every
                        //(count / 1000)th entry. Use longs to avoid any possible integer overflow problems.
                        int index = (int)(((long)(i) * (long)(matches.Count)) / ((long)markerCount));
                        SnapshotPoint match = matches[index].Start;

                        //Translate the match from its snapshot to the view's current snapshot (the versions should be the same,
                        //but this will handle it if -- for some reason -- they are not).
                        double y = this.scrollBar.GetYCoordinateOfBufferPosition(match.TranslateTo(this.textView.TextSnapshot, PointTrackingMode.Negative));
                        //if (y + MarkThickness > lastY)
                        //{
                        //    lastY = y;
                            this.DrawMark(drawingContext, this.matchBrush, y);
                        //}
                    }
                }
                catch (Exception exc)
                {
                    Debug.Write(exc.ToString());
                }
            }

            if (this.caretBrush != null && this.currentPoint.Position != 0)
            {
                //Draw a blue mark at the caret's location (on top of the mark at the caret's location).
                this.DrawMark(drawingContext, this.caretBrush, this.scrollBar.GetYCoordinateOfBufferPosition(currentPoint));
            }
        }

        private void DrawMark(DrawingContext drawingContext, Brush brush, double y)
        {
            drawingContext.DrawRectangle(brush, null,
                                         new Rect(MarkPadding, y - (MarkThickness * 0.5), this.Width - MarkPadding * 2.0, MarkThickness));
        }

        /// <summary>
        /// Helper class to do a search for matches on a background thread while
        /// providing thread-safe access to intermediate results.
        /// </summary>
        private class BackgroundSearch
        {
            private bool _abort = false;
            private IList<SnapshotSpan> matches = new List<SnapshotSpan>();
      //      public IList<SnapshotSpan> Matches { get; set; }
            public static SnapshotSpan? GetExtentOfWord(SnapshotPoint position)
            {
                int start = position.Position;
                while (--start > 0)
                {
                    if (!IsWordCharacter(position.Snapshot[start]))
                        break;
                }
                ++start;

                int end = position.Position;
                while (end < position.Snapshot.Length)
                {
                    if (!IsWordCharacter(position.Snapshot[end]))
                        break;
                    ++end;
                }

                if (start != end)
                    return new SnapshotSpan(position.Snapshot, start, end - start);
                else
                    return null;
            }

            public static bool IsWordCharacter(char c)
            {
                return (c == '_') | char.IsLetterOrDigit(c);
            }

            public static bool IsCompleteWord(int match, int length, string source)
            {
                return ((match == 0) || !IsWordCharacter(source[match - 1])) &&
                       ((match + length == source.Length) || !IsWordCharacter(source[match + length]));
            }

            public static int? GetMatch(string searchText, string text, int offset)
            {
                while (true)
                {
                    int match = text.IndexOf(searchText, offset, StringComparison.Ordinal);
                    if (match == -1)
                        break;

                    if (BackgroundSearch.IsCompleteWord(match, searchText.Length, text))
                    {
                        return match;
                    }

                    //Continue searching where the old search left off.
                    offset = match + 1;
                }

                return null;
            }

            /// <summary>
            /// Search for all instances of <paramref name="searchText"/> in <paramref name="snapshot"/>. Call
            /// <paramref name="completionCallback"/> once the search has completed.
            /// </summary>
            /// <param name="snapshot">Text snapshot in which to search.</param>
            /// <param name="searchText">Test to search for.</param>
            /// <param name="completionCallback">Delegate to call if the search is completed (will be called on the UI thread).</param>
            /// <remarks>The constructor must be called from the UI thread.</remarks>
            public BackgroundSearch(ITextSnapshot snapshot, string searchText, Action completionCallback)
            {
                ThreadPool.QueueUserWorkItem(delegate(object state)
                {
                    //Lower our priority so that we do not compete with the rendering.
                    System.Threading.Thread.CurrentThread.Priority = ThreadPriority.Lowest;
                    System.Threading.Thread.CurrentThread.IsBackground = true;

                    //for (int i = 0; i < 10; i++)
                    //{
    
                    //    System.Threading.Thread.Sleep(200);
                        if (RightMarginElement.OldHighlightNo != HLTextTagger.HighlightNo)
                        {
                            RightMarginElement.OldHighlightNo = HLTextTagger.HighlightNo;
                            matches = HLTextTagger.NewSpanAll;
                            //break;
                        }
                        //else {

                        //    System.Threading.Thread.Sleep(200);
                        //    if (CaretMarginElement.OldHighlightNo != HLTextTagger.HighlightNo)
                        //    {
                        //        CaretMarginElement.OldHighlightNo = HLTextTagger.HighlightNo;
                        //        matches = HLTextTagger.NewSpanAll;
                        //        //break;
                        //    }
                        //    else
                        //    {
                        //        System.Threading.Thread.Sleep(200);
                        //        CaretMarginElement.OldHighlightNo = HLTextTagger.HighlightNo;
                        //        matches = HLTextTagger.NewSpanAll;
                        //    }
                        //}
                    //}
                    //while (true) {
                    //    if (CaretMarginElement.oldHighlightNo != HLTextTagger.HighlightNo)
                    //    {
                    //        CaretMarginElement.oldHighlightNo = HLTextTagger.HighlightNo;
                    //        break;
                    //    }
                    //}
            //        Matches = HLTextTagger.NewSpanAll;
                    //Check each line in the buffer.
                    //foreach (ITextSnapshotLine line in snapshot.Lines)
                    //{
                    //    string text = line.Extent.GetText();
                    //    int offset = 0;
                    //    while (true)
                    //    {
                    //        int? match = GetMatch(searchText, text, offset);
                    //        if (match.HasValue)
                    //        {
                    //            //It does, add it (thread-safely) to the list of matches.
                    //            SnapshotSpan matchSpan = new SnapshotSpan(snapshot, match.Value + line.Extent.Start, searchText.Length);
                    //            lock (this.matches)
                    //            {
                    //                this.matches.Add(matchSpan);
                    //            }

                    //            //Search for matches on the rest of the line.
                    //            offset = match.Value + searchText.Length;
                    //        }
                    //        else
                    //            break;

                    //        //Check to see if the search should be aborted because no one cares about the result any more.
                    //        if (_abort)
                    //            return;
                    //    }
                    //}

                    completionCallback();
                });
            }

            //private void GetDatas(int ms, out bool isGetted)
            //{
            //    isGetted = false;
            //    System.Threading.Thread.Sleep(ms);
            //    if (CaretMarginElement.OldHighlightNo != HLTextTagger.HighlightNo)
            //    {
            //        CaretMarginElement.OldHighlightNo = HLTextTagger.HighlightNo;
            //        matches = HLTextTagger.NewSpanAll;
            //        isGetted = true;
            //    }
            //}

            /// <summary>
            /// About the current search.
            /// </summary>
            public void Abort()
            {
                _abort = true;
            }

            /// <summary>
            /// Get a copy of the matches found so far.
            /// </summary>
            /// /<remarks>
            /// <para>This method can be called from any thread, even if the search
            /// has not completed.</para>
            /// <para>This returns a snapshot of the results found so far. It may not
            /// be complete.</para>
            /// </remarks>
            public IList<SnapshotSpan> Matches
            {
                get
                {
                    lock (this.matches)
                    {
                        return new List<SnapshotSpan>(this.matches);
                    }
                }
            }
        }
    }
}