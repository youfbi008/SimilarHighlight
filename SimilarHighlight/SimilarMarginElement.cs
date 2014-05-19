using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using SimilarHighlight.ContainerMargin;
using System.Diagnostics;
using System.Windows.Media;
using DColor = System.Drawing.Color;

namespace SimilarHighlight
{
    [Export(typeof(FrameworkElement))]
    /// <summary>
    /// Helper class to handle the rendering of the caret margin.
    /// </summary>
    class SimilarMarginElement : FrameworkElement
    {
        public string CurFileName { get; set; }
        public HLTextTagger TextTaggerElement { get; set; }

        private IList<SnapshotSpan> SpanAll { get; set; }
        private readonly IWpfTextView textView;
        private readonly IVerticalScrollBar scrollBar;
        private DColor caretColor;
        private DColor matchColor;
        private Brush caretBrush;
        private Brush matchBrush;
        private SnapshotPoint currentPoint;
        double MarkPadding = 1.0;
        double MarkThickness = 4.0;

        /// <summary>
        /// Constructor for the CaretMarginElement.
        /// </summary>
        /// <param name="textView">ITextView to which this CaretMargenElement will be attached.</param>
        /// <param name="factory">Instance of the CaretMarginFactory that is creating the margin.</param>
        /// <param name="verticalScrollbar">Vertical scrollbar of the ITextViewHost that contains <paramref name="textView"/>.</param>
        [ImportingConstructor]
        public SimilarMarginElement(IWpfTextView textView, SimilarMarginFactory factory, IVerticalScrollBar verticalScrollbar)
        {
            this.textView = textView;
            this.scrollBar = verticalScrollbar;

            //Make our width big enough to see, but not so big that it consumes a lot of
            //real-estate.
            this.Width = HLTextTagger.OptionPage.MarginWidth;

            this.textView.Closed += OnClosed;
       
            this.IsVisibleChanged += delegate(object sender, DependencyPropertyChangedEventArgs e)
            {
                if ((bool)e.NewValue)
                {
                    if (caretColor != HLTextTagger.OptionPage.CaretColor)
                    {
                        caretColor = HLTextTagger.OptionPage.CaretColor;
                        this.caretBrush = GetBrush(caretColor, Colors.Red);
                    }
                    if (matchColor != HLTextTagger.OptionPage.MatchColor)
                    {
                        matchColor = HLTextTagger.OptionPage.MatchColor;
                        this.matchBrush = GetBrush(matchColor, Colors.Blue);
                    }

                    //Force the margin to be rerendered since things might have changed while the margin was hidden.
                    this.InvalidateVisual();
                }
                else
                {
               //     currentPoint = new SnapshotPoint(this.textView.TextSnapshot, 0);
           //         this.
                    //View.VisualElement.PreviewMouseLeftButtonUp -= VisualElement_PreviewMouseLeftButtonUp;
                    //View.VisualElement.PreviewKeyDown -= VisualElement_PreviewKeyDown;
                    //View.VisualElement.PreviewKeyUp -= VisualElement_PreviewKeyUp;
                }
            };
        }

        public void SetCurrentPoint(SnapshotPoint currentPoint)
        {
            this.currentPoint = currentPoint;
            this.InvalidateVisual();
        }

        public void RedrawSimilarMargin()
        {
            this.InvalidateVisual();
        }

        private Brush GetBrush(DColor color, Color defColor)
        {
            Brush brush = null;
            try
            {
                brush = new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
                brush.Freeze();
            }
            catch (Exception exc)
            {
                color = DColor.FromArgb(defColor.A, defColor.R, defColor.G, defColor.B);
                brush = new SolidColorBrush(defColor);
                brush.Freeze();
                HLTextTagger.OutputMsgForExc(exc.ToString());
            }
            return brush;
        }

        public bool Enabled
        {
            get
            {
                return HLTextTagger.OptionPage.MarginEnabled;
            }
        }

        void OnClosed(object sender, EventArgs e)
        {
            this.textView.Closed -= OnClosed;
        }

        /// <summary>
        /// Override for the FrameworkElement's OnRender. When called, redraw
        /// all of the markers 
        /// </summary>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            //this.drawingContext = drawingContext; && corFileName == HLTextTagger.FileName
            if (HLTextTagger.NewSpanAll != null && HLTextTagger.NewSpanAll.Count > 0) //&& HLTextTagger.IsChecked == true
            {
                //There is a word that should be highlighted. It doesn't matter whether or not the search has completed or
                //is still in progress: draw red marks for each match found so far (the completion callback on the search
                //will guarantee that the highlight display gets invalidated once the search has completed).

                //Take a snapshot of the matches found to date (this could still be changing
                //if the search has not completed yet).
                SpanAll = HLTextTagger.NewSpanAll;
                IList<SnapshotSpan> matches = HLTextTagger.NewSpanAll;
                if (matchColor != HLTextTagger.OptionPage.MatchColor)
                {
                    matchColor = HLTextTagger.OptionPage.MatchColor;
                    this.matchBrush = GetBrush(matchColor, Colors.Blue);
                }

                try
                {
                    int markerCount = Math.Min(1000, matches.Count);
                    for (int i = 0; i < markerCount; ++i)
                    {
                        //Get (for small lists) the index of every match or, for long lists, the index of every
                        //(count / 1000)th entry. Use longs to avoid any possible integer overflow problems.
                        int index = (int)(((long)(i) * (long)(matches.Count)) / ((long)markerCount));
                        SnapshotPoint match = matches[index].Start;
                        
                        //Translate the match from its snapshot to the view's current snapshot (the versions should be the same,
                        //but this will handle it if -- for some reason -- they are not).
                        double y = this.scrollBar.GetYCoordinateOfBufferPosition(match.TranslateTo(this.textView.TextSnapshot, PointTrackingMode.Negative));
                        MarkThickness = this.ActualHeight / (match.Snapshot.LineCount + HLTextTagger.PaneLineCnt + 1);
                        
                        this.DrawMark(drawingContext, this.matchBrush, y);
                    }
                }
                catch (Exception exc)
                {
                    HLTextTagger.OutputMsgForExc(exc.ToString());
                }
            }

            if (this.caretBrush != null && this.currentPoint.Position != 0 && CurFileName == HLTextTagger.FileName)
            {
                if (caretColor != HLTextTagger.OptionPage.CaretColor)
                {
                    caretColor = HLTextTagger.OptionPage.CaretColor;
                    this.caretBrush = GetBrush(caretColor, Colors.Red);
                }
                //Draw a blue mark at the caret's location (on top of the mark at the caret's location).
                this.DrawMark(drawingContext, this.caretBrush, this.scrollBar.GetYCoordinateOfBufferPosition(currentPoint));
            }
        }

        private void DrawMark(DrawingContext drawingContext, Brush brush, double y)
        {
            drawingContext.DrawRectangle(brush, null,
                new Rect(MarkPadding, y - (MarkThickness * 0.5), this.Width - MarkPadding * 2.0, MarkThickness));
        }
    }
}