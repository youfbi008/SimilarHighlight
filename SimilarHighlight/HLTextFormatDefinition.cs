using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SimilarHighlight.Option;
using System.Runtime.InteropServices;

namespace SimilarHighlight
{
    [Export(typeof(EditorFormatDefinition))]
    [Name(HLTextFormatDefinition.FormatName)]
    [UserVisible(true)]
    internal class HLTextFormatDefinition : MarkerFormatDefinition
    {
        public const string FormatName = "MarkerFormatDefinition/HLTextFormatDefinition";

        public HLTextFormatDefinition()
        {
            this.BackgroundColor = Colors.LightGreen;
            this.ForegroundColor = Colors.DarkBlue;
            this.DisplayName = "Highlight Word";
            this.ZOrder = 5;
        }
    }
}
