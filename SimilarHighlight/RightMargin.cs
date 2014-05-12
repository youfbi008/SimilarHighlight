using System;
using System.ComponentModel.Composition;
using System.Windows;
using Microsoft.VisualStudio.Text.Editor;

namespace SimilarHighlight
{
    /// <summary>
    /// Implementation of an IWpfTextViewMargin that highlights the location of the caret
    /// and all instances of words that match the word under the caret.
    /// </summary>
    [Export(typeof(IWpfTextViewMargin))]
    internal class RightMargin : IWpfTextViewMargin
    {
        /// <summary>
        /// Name of this margin.
        /// </summary>
        public const string Name = "RightMargin";

        #region Private Members
        public RightMarginElement rightMarginElement;
        bool _isDisposed = false;
        #endregion

        /// <summary>
        /// Constructor for the RightMargin.
        /// </summary>
        /// <param name="textViewHost">The IWpfTextViewHost in which this margin will be displayed.</param>
        /// <param name="navigator">Instance of an ITextStructureNavigator used to define words in the host's TextView. Created from the
        /// ITextStructureNavigatorFactory service.</param>
        public RightMargin(IWpfTextViewHost textViewHost, IVerticalScrollBar scrollBar, RightMarginFactory factory)
        {
            // Validate
            if (textViewHost == null)
                throw new ArgumentNullException("textViewHost");

            this.rightMarginElement = new RightMarginElement(textViewHost.TextView, factory, scrollBar);
        }

        #region IWpfTextViewMargin Members
        /// <summary>
        /// The FrameworkElement that renders the margin.
        /// </summary>
        public FrameworkElement VisualElement
        {
            get
            {
                ThrowIfDisposed();
                return this.rightMarginElement;
            }
        }
        #endregion

        #region ITextViewMargin Members
        /// <summary>
        /// For a horizontal margin, this is the height of the margin (since the width will be determined by the ITextView. For a vertical margin, this is the width of the margin (since the height will be determined by the ITextView.
        /// </summary>
        public double MarginSize
        {
            get
            {
                ThrowIfDisposed();
                return this.rightMarginElement.ActualWidth;
            }
        }

        /// <summary>
        /// The visible property, true if the margin is visible, false otherwise.
        /// </summary>
        public bool Enabled
        {
            get
            {
                ThrowIfDisposed();
                return this.rightMarginElement.Enabled;
            }
        }

        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            return string.Compare(marginName, RightMargin.Name, StringComparison.OrdinalIgnoreCase) == 0 ? this : (ITextViewMargin)null;
        }

        /// <summary>
        /// In our dipose, stop listening for events.
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                this.rightMarginElement.Dispose();
                GC.SuppressFinalize(this);
                _isDisposed = true;
            }
        }
        #endregion

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(Name);
        }

    }
}
