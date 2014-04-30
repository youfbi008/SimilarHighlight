#region License

// Copyright (C) 2011-2014 Kazunori Sakamoto
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

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
                IEnumerable<LocationInfo> locations, XElement root, bool isStrict, int treeType)
        {
            try
            {
                if (treeType == 0) {
                    return CstInferrer.GetSimilarElements(locations,
                            root, isStrict);
                }
                else if (treeType == 1) {
                    return AstInferrer.GetSimilarElements(locations,
                            root, isStrict);
                }
            }
            catch (Exception exc)
            {
                Debug.Write(exc.ToString());
            }
            return null;
        }
    }
}