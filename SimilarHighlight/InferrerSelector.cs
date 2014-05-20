using System;
using System.Diagnostics;
using System.Collections.Generic;
using Code2Xml.Core.Generators;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Code2Xml.Core;
using Code2Xml.Core.Location;
using Paraiba.Collections.Generic;
using Paraiba.Linq;
using System.Threading.Tasks;

namespace SimilarHighlight
{
    public static class InferrerSelector
    {
        public static IEnumerable<Tuple<int, CodeRange>> GetSimilarElements(
                IEnumerable<LocationInfo> locations, XElement root, int treeType)
        {
            try
            {
                if (treeType == 0) {
                    return CstInferrer.GetSimilarElements(locations,
                            root);
                }
                else if (treeType == 1) {
                    return AstInferrer.GetSimilarElements(locations,
                            root);
                }
            }
            catch (Exception exc)
            {
                HLTextTagger.OutputMsgForExc(exc.ToString());
            }
            return null;
        }
    }
}