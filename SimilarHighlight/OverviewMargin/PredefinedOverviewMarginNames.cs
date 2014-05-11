// Copyright (c) Microsoft Corporation
// All rights reserved

namespace SimilarHighlight.OverviewMargin
{
    public static class PredefinedOverviewMarginNames
    {
        /// <summary>
        /// The margin to the right of the text view, contained in the right margin and positioned after the vertical scrollbar, 
        /// that implements mouse handlers that allow the user to jump to positions in the buffer by left-clicking.
        /// </summary>
        /// <remarks>The OverviewMargin implements the <see cref="IOverviewMargin"/> interface, which can be used to get an <see cref="IVerticalScrollBar"/>
        /// to map between buffer positions and y-coordinates.</remarks>
        public const string Overview = "Overview";
    }
}
