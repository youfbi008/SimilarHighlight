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
using System.Threading;

namespace SimilarHighlight
{
    public static class InferrerSelector
    {
        public static IEnumerable<Tuple<int, CodeRange>> GetSimilarElements(
                IEnumerable<LocationInfo> locations, CstNode rootNode, TreeType treeType, string candidateNodeType)
        {
            try
            {
                if (treeType == TreeType.CST)
                {
                    return CstInferrer.GetSimilarElements(locations,
                            rootNode, candidateNodeType);
                }
                else if (treeType == TreeType.AST)
                {   // TODO: The class will be fix in future.
                    return AstInferrer.GetSimilarElements(locations,
                            rootNode, candidateNodeType);
                }
            }
            catch (ThreadAbortException tae)
            {
                HLTextTagger.OutputMsgForExc("Background thread of highlighting is stopping.[GetSimilarElements method]");
            }
            catch (Exception exc)
            {
                HLTextTagger.OutputMsgForExc(exc.ToString());
            }
            return null;
        }
    }
}