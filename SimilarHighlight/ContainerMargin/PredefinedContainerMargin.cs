namespace SimilarHighlight.ContainerMargin
{
    public static class PredefinedContainerMargin
    {
        /// <summary>
        /// The margin to the right of the text view, contained in the right margin and positioned after the vertical scrollbar, 
        /// that implements mouse handlers that allow the user to jump to positions in the buffer by left-clicking.
        /// </summary>
        /// <remarks>The ContainerMargin implements the <see cref="IContainerMargin"/> interface, which can be used to get an <see cref="IVerticalScrollBar"/>
        /// to map between buffer positions and y-coordinates.</remarks>
        public const string Container = "Container";
    }
}
