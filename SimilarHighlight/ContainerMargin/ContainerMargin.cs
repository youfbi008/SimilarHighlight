using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;
using SimilarHighlight.ContainerwMargin;
using DColor = System.Drawing.Color;

namespace SimilarHighlight.ContainerMargin
{
    /// <summary>
    /// Manages the logical content of the ContainerMargin, which displays information
    /// relative to the entire document (optionally including elided regions) and supports
    /// click navigation.
    /// </summary>
    internal class ContainerMargin : BaseMargin, IContainerMargin
    {
        #region Private Members
        const double VerticalPadding = 1.0;
        const double MinViewportHeight = 5.0; // smallest that viewport extent will be drawn

        private Brush backScreenBrush;
        private Brush scollBrush;

        private readonly IOutliningManager _outliningManager;

        private SimpleScrollBar _scrollBar;

        private ContainerMarginProvider _provider;
        private DColor backScreenColor;
        private DColor scollColor;
        private SimilarMarginElement MarginElement { get; set; }
        /// <summary>
        /// Constructor for the ContainerMargin.
        /// </summary>
        /// <param name="textViewHost">The IWpfTextViewHost in which this margin will be displayed.</param>
        private ContainerMargin(IWpfTextViewHost textViewHost, IWpfTextViewMargin containerMargin, ContainerMarginProvider myProvider)
            : base(PredefinedContainerMargin.Container, Orientation.Vertical, textViewHost, myProvider.OrderedMarginProviders)
        {
            if (HLTextTagger.OptionPage == null) return;
            _provider = myProvider;

            _outliningManager = myProvider.OutliningManagerService.GetOutliningManager(textViewHost.TextView);

            _scrollBar = new SimpleScrollBar(textViewHost, containerMargin, myProvider._scrollMapFactory, this, false);

            backScreenColor = HLTextTagger.OptionPage.BackScreenColor;
            scollColor = HLTextTagger.OptionPage.ScrollColor;
            this.backScreenBrush = GetBrush(backScreenColor, Color.FromArgb(0x30, 0x00, 0x00, 0x00));
            this.scollBrush = GetBrush(scollColor, Color.FromArgb(0x00, 0xff, 0xff, 0xff));

            base.Background = Brushes.Transparent;
            base.ClipToBounds = true;

//            base.TextViewHost.TextView.Options.OptionChanged += this.OnOptionsChanged;
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

        protected override void Close()
        {
       //     base.TextViewHost.TextView.Options.OptionChanged -= this.OnOptionsChanged;
            UnregisterEvents();

            base.Close();
        }

        #endregion

        /// <summary>
        /// Factory for the ContainerMargin.
        /// </summary>
        /// <param name="textViewHost">The IWpfTextViewHost in which this margin will be displayed.</param>
        /// <param name="myProvider">Will be queried for various imported components.</param>
        public static ContainerMargin Create(IWpfTextViewHost textViewHost, IWpfTextViewMargin containerMargin, ContainerMarginProvider myProvider)
        {
            ContainerMargin margin = new ContainerMargin(textViewHost, containerMargin, myProvider);
            margin.Initialize();
            
            return margin;
        }

        #region IContainerMargin members
        public IVerticalScrollBar ScrollBar
        {
            get
            {
                base.ThrowIfDisposed();
                return _scrollBar;
            }
        }
        #endregion

        // RegisterEvents() will be called for the first time from Initialize()
        protected override void RegisterEvents()
        {
            base.RegisterEvents();
            if (MarginElement == null)
                MarginElement = base.SimilarMargin.similarMarginElement;

            if (_scrollBar == null) return;
            base.TextViewHost.TextView.LayoutChanged += OnLayoutChanged;
            _scrollBar.TrackSpanChanged += OnTrackSpanChanged;

            this.MouseLeftButtonDown += OnMouseLeftButtonDown;

            this.MouseMove += OnMouseMove;
            this.MouseLeftButtonUp += OnMouseLeftButtonUp;

            if (this.backScreenColor != HLTextTagger.OptionPage.BackScreenColor)
            {
                backScreenColor = HLTextTagger.OptionPage.BackScreenColor;
                backScreenBrush = GetBrush(backScreenColor, Color.FromArgb(0x30, 0x00, 0x00, 0x00));
            }

            if (this.scollColor != HLTextTagger.OptionPage.ScrollColor)
            {
                scollColor = HLTextTagger.OptionPage.ScrollColor;
                scollBrush = GetBrush(scollColor, Color.FromArgb(0x00, 0xff, 0xff, 0xff));
            }
        }

        protected override void UnregisterEvents()
        {
            base.UnregisterEvents();
            if (_scrollBar == null) return;
            base.TextViewHost.TextView.LayoutChanged -= OnLayoutChanged;
            _scrollBar.TrackSpanChanged -= OnTrackSpanChanged;

            this.MouseLeftButtonDown -= OnMouseLeftButtonDown;

            this.MouseMove -= OnMouseMove;
            this.MouseLeftButtonUp -= OnMouseLeftButtonUp;
        }

        #region Event Handlers

        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            this.InvalidateVisual();
        }

        private void OnTrackSpanChanged(object sender, EventArgs e)
        {
            this.InvalidateVisual();
        }

        void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.CaptureMouse();

            Point pt = e.GetPosition(this);
            SnapshotPoint currentPoint = this.ScrollViewToYCoordinate(pt.Y, e.ClickCount == 2);
            // Set the current selection by selecting point from scroll margin.
            this.MarginElement.TextTaggerElement.SetCurrentScrollPointLine(currentPoint);
        }

        void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.ReleaseMouseCapture();
        }

        void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && this.IsMouseCaptured)
            {
                Point pt = e.GetPosition(this);
                this.ScrollViewToYCoordinate(pt.Y, false);
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            // Don't bother drawing if we have no content or no area to draw in (implying there are no children)
            if (ActualWidth > 0.0)
            {
                RenderViewportExtent(drawingContext);
            }
        }
        #endregion

        /// <summary>
        /// Scroll the view so that the location corresponding to the specified coordinate
        /// is at the center of the screen.
        /// </summary>
        /// <param name="y">A pixel coordinate relative to the top of the margin.</param>
        /// <remarks>
        /// The corresponding buffer position will be displayed at the center of the viewport.
        /// If the pixel coordinate corresponds to a position beyond the end of the buffer,
        /// the last line of text will be scrolled proportionally higher than center, until
        /// only one line of text is visible.
        /// </remarks>
        internal SnapshotPoint ScrollViewToYCoordinate(double y, bool expand)
        {
            double yLastLine = _scrollBar.TrackSpanBottom - _scrollBar.ThumbHeight;
            if (y < yLastLine)
            {
                SnapshotPoint position = _scrollBar.GetBufferPositionOfYCoordinate(y);

                if (expand)
                    this.Expand(position);
                base.TextViewHost.TextView.ViewScroller.EnsureSpanVisible(new SnapshotSpan(position, 0), EnsureSpanVisibleOptions.AlwaysCenter);
                return position;
            }
            else
            {
                // Place the last line of the document somewhere between the top of the view and the center of the view,
                // depending on how far below the last mapped coordinate the user clicked.  The lowest point of interest
                // is with the thumb at the bottom of the track, corresponding to a click half a thumbheight above bottom.
                y = Math.Min(y, yLastLine + (_scrollBar.ThumbHeight / 2.0));
                double fraction = (y - yLastLine) / _scrollBar.ThumbHeight; // 0 to 0.5 
                double dyDistanceFromTopOfViewport = base.TextViewHost.TextView.ViewportHeight * (0.5 - fraction);
                SnapshotPoint end = new SnapshotPoint(base.TextViewHost.TextView.TextSnapshot, base.TextViewHost.TextView.TextSnapshot.Length);

                if (expand)
                    this.Expand(end);
                base.TextViewHost.TextView.DisplayTextLineContainingBufferPosition(end, dyDistanceFromTopOfViewport, ViewRelativePosition.Top);
                return end;
            }
        }

        private void Expand(SnapshotPoint position)
        {
            if (_outliningManager != null)
            {
                _outliningManager.ExpandAll(new SnapshotSpan(position, 0), (collapsible) =>
                {
                    Span s = collapsible.Extent.GetSpan(position.Snapshot);
                    return (position > s.Start) && (position < s.End);
                });
            }
        }

        #region Rendering

        /// <summary>
        /// Shade the visible/offScreen portion of the buffer
        /// </summary>
        private void RenderViewportExtent(DrawingContext drawingContext)
        {
            var tvl = base.TextViewHost.TextView.TextViewLines;
            SnapshotPoint start = new SnapshotPoint(tvl.FirstVisibleLine.Snapshot, tvl.FirstVisibleLine.Start);

            double viewportTop = Math.Floor(_scrollBar.GetYCoordinateOfBufferPosition(start));
            double viewportBottom = Math.Ceiling(Math.Max(GetYCoordinateOfLineBottom(tvl.LastVisibleLine), viewportTop + MinViewportHeight));

            if (this.backScreenColor != HLTextTagger.OptionPage.BackScreenColor)
            {
                backScreenColor = HLTextTagger.OptionPage.BackScreenColor;
                backScreenBrush = GetBrush(backScreenColor, Color.FromArgb(0x30, 0x00, 0x00, 0x00));
            }

            if (this.scollColor != HLTextTagger.OptionPage.ScrollColor)
            {
                scollColor = HLTextTagger.OptionPage.ScrollColor;
                scollBrush = GetBrush(scollColor, Color.FromArgb(0x00, 0xff, 0xff, 0xff));
            }
            DrawRectangle(drawingContext, backScreenBrush, this.ActualWidth, _scrollBar.TrackSpanTop, viewportTop);

            DrawRectangle(drawingContext, scollBrush, this.ActualWidth, viewportTop, viewportBottom);

            DrawRectangle(drawingContext, backScreenBrush, this.ActualWidth, viewportBottom, _scrollBar.TrackSpanBottom);
        }

        private static void DrawRectangle(DrawingContext drawingContext, Brush brush, double width, double yTop, double yBottom)
        {
            if ((brush != null) && (yBottom - VerticalPadding > yTop))
                drawingContext.DrawRectangle(brush, null, new Rect(0.0, yTop, width, yBottom - yTop));
        }

        /// <summary>
        /// Get the scrollbar y coordinate of the bottom of the line.  Generally that will
        /// be the top of the next line, but if there's no next line, fake it
        /// based on the proportion of empty space below the last line.
        /// </summary>
        /// <param name="line">snapshot line number; the line must be visible</param>
        private double GetYCoordinateOfLineBottom(ITextViewLine line)
        {
            var snapshot = base.TextViewHost.TextView.TextSnapshot;
            if (line.EndIncludingLineBreak.Position < snapshot.Length)
            {
                // line is not the last line; get the Y coordinate of the next line.
                return _scrollBar.GetYCoordinateOfBufferPosition(new SnapshotPoint(snapshot, line.EndIncludingLineBreak.Position + 1));
            }
            else
            {
                // last line.
                var tvl = base.TextViewHost.TextView.TextViewLines;
                double empty = 1 - ((tvl.LastVisibleLine.Bottom - tvl.FirstVisibleLine.Bottom) / base.TextViewHost.TextView.ViewportHeight);
                return _scrollBar.GetYCoordinateOfScrollMapPosition(_scrollBar.Map.End + _scrollBar.Map.ThumbSize * empty);
            }
        }

        #endregion

        /// <summary>
        /// A scrollbar that can be switched to either delegate to the real (view-based) scrollbar or
        /// to use a specified scroll map.
        /// </summary>
        private class SimpleScrollBar : IVerticalScrollBar
        {
            IScrollMapFactoryService _scrollMapFactory;
            private ScrollMapWrapper _scrollMap = new ScrollMapWrapper();
            private IWpfTextView _textView;
            private IWpfTextViewMargin _realScrollBarMargin;
            private IVerticalScrollBar _realScrollBar;
            private bool _useElidedCoordinates = true;

            double _trackSpanTop;
            double _trackSpanBottom;

            private class ScrollMapWrapper : IScrollMap
            {
                private IScrollMap _scrollMap;

                public ScrollMapWrapper()
                {
                }

                public IScrollMap ScrollMap
                {
                    get { return _scrollMap; }
                    set
                    {
                        if (_scrollMap != null)
                        {
                            _scrollMap.MappingChanged -= OnMappingChanged;
                        }

                        _scrollMap = value;

                        _scrollMap.MappingChanged += OnMappingChanged;

                        this.OnMappingChanged(this, new EventArgs());
                    }
                }

                void OnMappingChanged(object sender, EventArgs e)
                {
                    EventHandler handler = this.MappingChanged;
                    if (handler != null)
                        handler(sender, e);
                }

                public double GetCoordinateAtBufferPosition(SnapshotPoint bufferPosition)
                {
                    return _scrollMap.GetCoordinateAtBufferPosition(bufferPosition);
                }

                public bool AreElisionsExpanded
                {
                    get { return _scrollMap.AreElisionsExpanded; }
                }

                public SnapshotPoint GetBufferPositionAtCoordinate(double coordinate)
                {
                    return _scrollMap.GetBufferPositionAtCoordinate(coordinate);
                }

                public double Start
                {
                    get { return _scrollMap.Start; }
                }

                public double End
                {
                    get { return _scrollMap.End; }
                }

                public double ThumbSize
                {
                    get { return _scrollMap.ThumbSize; }
                }

                public ITextView TextView
                {
                    get { return _scrollMap.TextView; }
                }

                public double GetFractionAtBufferPosition(SnapshotPoint bufferPosition)
                {
                    return _scrollMap.GetFractionAtBufferPosition(bufferPosition);
                }

                public SnapshotPoint GetBufferPositionAtFraction(double fraction)
                {
                    return _scrollMap.GetBufferPositionAtFraction(fraction);
                }

                public event EventHandler MappingChanged;
            }

            /// <summary>
            /// If true, map to the view's scrollbar; else map to the scrollMap.
            /// </summary>
            public bool UseElidedCoordinates
            {
                get { return _useElidedCoordinates; }
                set
                {
                    if (value != _useElidedCoordinates)
                    {
                        _useElidedCoordinates = value;
                        this.ResetScrollMap();
                    }
                }
            }

            private void ResetScrollMap()
            {
                if (_useElidedCoordinates && this.UseRealScrollBarTrackSpan)
                {
                    _scrollMap.ScrollMap = _realScrollBar.Map;
                }
                else
                {
                    _scrollMap.ScrollMap = _scrollMapFactory.Create(_textView, !_useElidedCoordinates);
                }
            }

            private void ResetTrackSpan()
            {
                if (this.UseRealScrollBarTrackSpan)
                {
                    _trackSpanTop = _realScrollBar.TrackSpanTop;
                    _trackSpanBottom = _realScrollBar.TrackSpanBottom;
                }
                else
                {
                    _trackSpanTop = 0.0;
                    _trackSpanBottom = _textView.ViewportHeight;
                }

                //Ensure that the length of the track span is never 0.
                _trackSpanBottom = Math.Max(_trackSpanTop + 1.0, _trackSpanBottom);
            }

            private bool UseRealScrollBarTrackSpan
            {
                get
                {
                    try
                    {
                        return (_realScrollBar != null) && (_realScrollBarMargin != null) && (_realScrollBarMargin.VisualElement.Visibility == Visibility.Visible);
                    }
                    catch
                    {
                        return false;
                    }
                }
            }

            void OnMappingChanged(object sender, EventArgs e)
            {
                this.RaiseTrackChangedEvent();
            }

            private void RaiseTrackChangedEvent()
            {
                EventHandler handler = this.TrackSpanChanged;
                if (handler != null)
                    handler(this, new EventArgs());
            }

            public SimpleScrollBar(IWpfTextViewHost host, IWpfTextViewMargin containerMargin, IScrollMapFactoryService scrollMapFactory, FrameworkElement container, bool useElidedCoordinates)
            {
                _textView = host.TextView;

                _realScrollBarMargin = containerMargin.GetTextViewMargin(PredefinedMarginNames.VerticalScrollBar) as IWpfTextViewMargin;
                if (_realScrollBarMargin != null)
                {
                    _realScrollBar = _realScrollBarMargin as IVerticalScrollBar;
                    if (_realScrollBar != null)
                    {
                        _realScrollBar.TrackSpanChanged += OnScrollBarTrackSpanChanged;
                    }
                }
                this.ResetTrackSpan();

                _scrollMapFactory = scrollMapFactory;
                _useElidedCoordinates = useElidedCoordinates;
                this.ResetScrollMap();

                _scrollMap.MappingChanged += delegate { this.RaiseTrackChangedEvent(); };

                container.SizeChanged += OnContainerSizeChanged;
            }

            void OnContainerSizeChanged(object sender, EventArgs e)
            {
                if (!this.UseRealScrollBarTrackSpan)
                {
                    this.ResetTrackSpan();
                    this.RaiseTrackChangedEvent();
                }
            }

            void OnScrollBarTrackSpanChanged(object sender, EventArgs e)
            {
                if (this.UseRealScrollBarTrackSpan)
                {
                    this.ResetTrackSpan();
                    this.RaiseTrackChangedEvent();
                }
            }

            #region IVerticalScrollBar Members
            public IScrollMap Map
            {
                get { return _scrollMap; }
            }

            public double GetYCoordinateOfBufferPosition(SnapshotPoint bufferPosition)
            {
                try
                {
                    double scrollMapPosition = _scrollMap.GetCoordinateAtBufferPosition(bufferPosition);
                    return this.GetYCoordinateOfScrollMapPosition(scrollMapPosition);
                }
                catch (Exception exc) {
                    HLTextTagger.OutputMsgForExc(exc.ToString());
                }
                return 0.0;
            }

            public double GetYCoordinateOfScrollMapPosition(double scrollMapPosition)
            {
                double minimum = _scrollMap.Start;
                double maximum = _scrollMap.End;
                double height = maximum - minimum;

                return this.TrackSpanTop + ((scrollMapPosition - minimum) * this.TrackSpanHeight) / (height + _scrollMap.ThumbSize);
            }

            public SnapshotPoint GetBufferPositionOfYCoordinate(double y)
            {
                double minimum = _scrollMap.Start;
                double maximum = _scrollMap.End;
                double height = maximum - minimum;

                double scrollCoordinate = minimum + (y - this.TrackSpanTop) * (height + _scrollMap.ThumbSize) / this.TrackSpanHeight;

                return _scrollMap.GetBufferPositionAtCoordinate(scrollCoordinate);
            }

            public double TrackSpanTop
            {
                get { return _trackSpanTop; }
            }

            public double TrackSpanBottom
            {
                get { return _trackSpanBottom; }
            }

            public double TrackSpanHeight
            {
                get { return _trackSpanBottom - _trackSpanTop; }
            }

            public double ThumbHeight
            {
                get
                {
                    double minimum = _scrollMap.Start;
                    double maximum = _scrollMap.End;
                    double height = maximum - minimum;

                    return _scrollMap.ThumbSize / (height + _scrollMap.ThumbSize) * this.TrackSpanHeight;
                }
            }

            public event EventHandler TrackSpanChanged;
            #endregion
        }
    }
}