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
    public static class AstInferrer
    {
        // TODO: The class will be fixed in future. 
        // similarity range
        public static int SimilarityRange { get; set; }

        private static int keysCount { get; set; }

        public static HashSet<string> GetSurroundingKeys(
                this AstNode node, int length, bool inner = true, bool outer = true)
        {
            //inner = outer = true; // TODO: for debug

            var ret = new HashSet<string>();
            var childElements = new List<Tuple<AstNode, string>>();
            if (inner)
            {
                childElements.Add(Tuple.Create(node, node.Name));
                var ancestorStr = "";
                foreach (var e in node.AncestorsWithSingleChildAndSelf())
                {
                    // null?
                    if (e == null) {
                        continue;
                    }
                    ancestorStr = ancestorStr + "<" + e.Name;
                    ret.Add(ancestorStr);
                }
            }
            var i = 1;
            if (outer)
            {
                var parentElement = Tuple.Create(node, node.Name);
                var descendantStr = "";
                foreach (var e in node.DescendantsOfSingleAndSelf())
                {
                    descendantStr = descendantStr + "<" + e.Name;
                    ret.Add(descendantStr);
                }
                // 自分自身の位置による区別も考慮する
                ret.Add(node.Name);//.NameOrTokenWithId()
                for (; i <= length; i++)
                {
                    var newChildElements = new List<Tuple<AstNode, string>>();
                    foreach (var t in childElements)
                    {
                        foreach (var e in t.Item1.Elements())
                        {
                            var key = t.Item2 + ">" + e.Name;
                            newChildElements.Add(Tuple.Create(e, key));
                            // トークンが存在するかチェックする弱い条件
                            // for Preconditions.checkArguments()
                            ret.Add(t.Item2 + ">'" + e.Token.Text + "'");
                        }
                        foreach (var e in t.Item1.Descendants())
                        {
                            // トークンが存在するかチェックする弱い条件
                            //ret.Add(t.Item2 + ">>'" + e.TokenText() + "'");
                        }
                    }
                    foreach (var e in parentElement.Item1.Siblings(10))
                    {
                        var key = parentElement.Item2 + "-" + e.Name;
                        newChildElements.Add(Tuple.Create(e, key));
                        // トークンが存在するかチェックする弱い条件
                        // for Preconditions.checkArguments()
                        ret.Add(parentElement.Item2 + "-'" + e.Token.Text + "'");
                        //// 先祖に存在するかチェックする弱い条件
                        //var iLastName = parentElement.Item2.LastIndexOf("<");
                        //var weakKey = "<<" + parentElement.Item2.Substring(iLastName + 1) + "-" + e.NameOrTokenWithId();
                        //newChildElements.Add(Tuple.Create(e, weakKey));
                    }
                    ret.UnionWith(newChildElements.Select(t => t.Item2));
                    childElements = newChildElements;

                    var newParentElement = parentElement.Item1.Parent;
                    if (newParentElement == null)
                    {
                        break;
                    }
                    parentElement = Tuple.Create(
                            newParentElement,
                            parentElement.Item2 + "<" + newParentElement.Name);
                    ret.Add(parentElement.Item2);
                }
            }
            for (; i <= length; i++)
            {
                var newChildElements = new List<Tuple<AstNode, string>>();
                foreach (var t in childElements)
                {
                    foreach (var e in t.Item1.Elements())
                    {
                        var key = t.Item2 + ">" + e.Name;
                        newChildElements.Add(Tuple.Create(e, key));
                        // トークンが存在するかチェックする弱い条件
                        // for Preconditions.checkArguments()
                        ret.Add(t.Item2 + ">'" + e.Token.Text + "'");
                    }
                }
                ret.UnionWith(newChildElements.Select(t => t.Item2));
                childElements = newChildElements;
            }
            return ret;
        }

        public static HashSet<string> GetUnionKeys(
                this IEnumerable<AstNode> elements, int length, bool inner = true, bool outer = true)
        {
            var commonKeys = new HashSet<string>();
            foreach (var element in elements) {
                var keys = element.GetSurroundingKeys(length, inner, outer);
                commonKeys.UnionWith(keys);
            }
            return commonKeys;
        }

        public static HashSet<string> GetCommonKeys(
                this IEnumerable<AstNode> elements, int length, bool inner = true, bool outer = true)
        {
            HashSet<string> commonKeys = null;
            keysCount = 0;
            
            foreach (var element in elements) {
                // Get the data collection of the surrounding nodes.
                var keys = element.GetSurroundingKeys(length, inner, outer);
                //int i = 0;
                //foreach (var k in keys) {
                //    Debug.WriteLine("[" + i + "]:" + k); i++;
                //}

                // Accumulate the number of the surrounding nodes.
                keysCount += keys.Count();
                if (commonKeys == null)
                {
                    commonKeys = keys;
                }
                else
                {
                    commonKeys.IntersectWith(keys);
                }
            }
            return commonKeys;
        }

        private static ISet<string> AdoptNodeNames(ICollection<AstNode> outermosts)
        {
            var name2Count = new Dictionary<string, int>();
            var candidates = outermosts.AsParallel().SelectMany(
                    e => e.DescendantsOfSingleAndSelf());
            foreach (var e in candidates)
            {
                var count = name2Count.GetValueOrDefault(e.Name);
                name2Count[e.Name] = count + 1;
            }
            return outermosts.AsParallel().Select(
                    e => e.DescendantsOfSingleAndSelf()
                            .Select(e2 => e2.Name)
                            .MaxElementOrDefault(name => name2Count[name]))
                    .ToHashSet();
        }

        public static IEnumerable<Tuple<int, CodeRange>> GetSimilarElements(
                IEnumerable<LocationInfo> locations, XElement root, ref ISet<string> nodeNames,
                int range = 5, bool inner = true, bool outer = true)
        {
            try
            {
                var similarityRange = 0;

                // Convert the location informatoin (CodeRange) to the node (XElement) in the ASTs
                var elements = new List<AstNode>();

                foreach (var location in locations)
                {
                    elements.Add(AstNode.FromXml(location.XElement));
                }

                // Determine the node names to extract candidate nodes from the ASTs
                nodeNames = AdoptNodeNames(elements);

                var names = nodeNames;

                // Extract candidate nodes that has one of the determined names
                var candidates = new List<IEnumerable<AstNode>>();

                TimeWatch.Start();

                AstNode node = AstNode.FromXml(root);
                candidates.Add(
                        node.Descendants().AsParallel()
                                .Where(e => names.Contains(e.Name)).ToList());

                // Extract common surrounding nodes from the selected elements.
                var commonKeys = elements.GetCommonKeys(range, true, true);
                //int i = 0;
                //foreach (var k in commonKeys)
                //{
                //    Debug.WriteLine("[" + i + "]:" + k); i++;
                //}
                TimeWatch.Stop("FindOutCandidateElements");

                // Get the similarity range.
                if (SimilarityRange == 0 && keysCount != 0)
                {
                    // TODO The better algorithm is expected.
                    similarityRange = keysCount / 4;
                }
                else
                {
                    // If the similarity is setted.
                    similarityRange = SimilarityRange;
                }

                int minSimilarity = 0;
                // If the similarity is too small. 
                if (commonKeys.Count <= similarityRange)
                {
                    return Enumerable.Empty<Tuple<int, CodeRange>>();
                }

                // Get the similarity threshold.
                minSimilarity = commonKeys.Count - similarityRange;
                //}
                //else
                //{
                //    minSimilarity = commonKeys.Count * 2 / 3;
                //}

                TimeWatch.Start();

                // Get the similar nodes collection. 
                var ret = candidates.AsParallel().SelectMany(
                        kv =>
                        {
                            return kv.Select(
                                    e => Tuple.Create(
                                        // Count how many common surrounding nodes each candidate node has 
                                        e.GetSurroundingKeys(range, inner, outer)
                                            .Count(commonKeys.Contains),
                                            e))
                                // The candidate node will be taken as similar node 
                                // when the number of common surrounding nodes is bigger than the similarity threshold.
                                     .Where(e => e.Item1 > minSimilarity
                                     )
                                     .Select(
                                            t => Tuple.Create(
                                                    t.Item1,	// Indicates the simlarity
                                                    CodeRange.Nil
                                              //      CodeRange.Locate(t.Item2) TODO: this will be fixed in future.
                                                    ));
                        })
                        // Sort candidate nodes using the similarities
                        .OrderByDescending(t => t.Item1).ToList();

                TimeWatch.Stop("FindOutSimilarElements");
                return ret;
            }
            catch (Exception exc)
            {
                HLTextTagger.OutputMsgForExc(exc.ToString());
            }
            return null;
        }
    }
}