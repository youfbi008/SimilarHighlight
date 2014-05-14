using Microsoft.VisualStudio.Text.Editor;

namespace SimilarHighlight.ContainerwMargin
{
    /// <summary>
    /// Represents an container margin.
    /// </summary>
    public interface IContainerMargin
    {
        /// <summary>
        /// Gets the <see cref="IVerticalScrollBar"/> used to map between buffer positions and y-coordinates.
        /// </summary>
        IVerticalScrollBar ScrollBar
        {
            get;
        }
    }
}
