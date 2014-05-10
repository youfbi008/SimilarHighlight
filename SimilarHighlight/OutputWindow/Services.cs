using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace SimilarHighlight.OutputWindow
{
    public static class Services
    {
        [Export]
        [Name("Similar")]
        internal static OutputWindowDefinition SimilarOutputWindowDefinition;
    }
}
