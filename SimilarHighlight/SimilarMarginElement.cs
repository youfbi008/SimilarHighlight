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

namespace SimilarHighlight
{
    [Export(typeof(EditorOptionDefinition))]
    internal sealed class Enabled : EditorOptionDefinition<bool>
    {
        public override bool Default { get { return true; } }
        public override EditorOptionKey<bool> Key { get { return SimilarMarginElement.EnabledOptionId; } }
    }

    [Export(typeof(EditorOptionDefinition))]
    internal sealed class MarginWidth : EditorOptionDefinition<double>
    {
        public override double Default { get { return 10.0; } }
        public override bool IsValid(ref double proposedValue)
        {
            return (proposedValue >= 3.0) && (proposedValue <= 60.0);
        }
        public override EditorOptionKey<double> Key { get { return SimilarMarginElement.MarginWidthId; } }
    }

    [Export(typeof(FrameworkElement))]
    /// <summary>
    /// Helper class to handle the rendering of the caret margin.
    /// </summary>
    class SimilarMarginElement : FrameworkElement
    {
        private IList<SnapshotSpan> SpanAll { get; set; }
        private readonly IWpfTextView textView;
        private readonly IVerticalScrollBar scrollBar;

        private string caretColorName;
        private string matchColorName;
        private Brush caretBrush;
        private Brush matchBrush;
        private SnapshotPoint currentPoint;

        double MarkPadding = 1.0;
        double MarkThickness = 4.0;

        public static readonly EditorOptionKey<bool> EnabledOptionId = new EditorOptionKey<bool>("SimilarM/Enabled");
        public static readonly EditorOptionKey<double> MarginWidthId = new EditorOptionKey<double>("SimilarM/MarginWidth");

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

            factory.LoadOption(textView.Options, SimilarMarginElement.EnabledOptionId.Name);
            factory.LoadOption(textView.Options, SimilarMarginElement.MarginWidthId.Name);

            this.scrollBar = verticalScrollbar;

            //Make our width big enough to see, but not so big that it consumes a lot of
            //real-estate.
            this.Width = textView.Options.GetOptionValue(SimilarMarginElement.MarginWidthId);

            caretColorName = HLTextTagger.OptionPage.CaretColor.Name;
            matchColorName = HLTextTagger.OptionPage.MatchColor.Name;
            this.caretBrush = GetBrush(caretColorName, Colors.Red);
            this.matchBrush = GetBrush(matchColorName, Colors.Blue);

            this.textView.Closed += OnClosed;
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

        private Brush GetBrush(string colorName, Color defColor)
        {
            Brush brush = null;
            try
            {
                if (colorName != "")
                {
                    brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorName));
                    brush.Freeze();
                }
            }
            catch (Exception exc) {
                HLTextTagger.OutputMsg("The setting of Color is invalid.");
                brush = new SolidColorBrush(defColor);
                brush.Freeze();
            }
            return brush;
        }

        public bool Enabled
        {
            get
            {
                return this.textView.Options.GetOptionValue<bool>(SimilarMarginElement.EnabledOptionId);
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

            if (HLTextTagger.NewSpanAll != null && HLTextTagger.NewSpanAll.Count > 0) //&& HLTextTagger.IsChecked == true
            {
                //There is a word that should be highlighted. It doesn't matter whether or not the search has completed or
                //is still in progress: draw red marks for each match found so far (the completion callback on the search
                //will guarantee that the highlight display gets invalidated once the search has completed).

                //Take a snapshot of the matches found to date (this could still be changing
                //if the search has not completed yet).
                IList<SnapshotSpan> matches = HLTextTagger.NewSpanAll;
                if (matchColorName != HLTextTagger.OptionPage.MatchColor.Name)
                {
                    matchColorName = HLTextTagger.OptionPage.MatchColor.Name;
                    this.matchBrush = GetBrush(matchColorName, Colors.Blue);
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
                    Debug.Write(exc.ToString());
                }
            }

            if (this.caretBrush != null && this.currentPoint.Position != 0)
            {
                if (caretColorName != HLTextTagger.OptionPage.CaretColor.Name)
                {
                    caretColorName = HLTextTagger.OptionPage.CaretColor.Name;
                    this.caretBrush = GetBrush(caretColorName, Colors.Red);
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