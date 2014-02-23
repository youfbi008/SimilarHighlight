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
using System.Collections.Generic;
using System.Diagnostics;
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
    public struct LocationInfo
    {
        public XElement XElement;
        public CodeRange CodeRange;
    }

    public static class Inferrer
    {
        // similarity range
        public static int SimilarityRange { get; set; }

        private static int keysCount { get; set; }

        public static HashSet<string> GetSurroundingKeys(
                this XElement element, int length, bool inner = true, bool outer = true)
        {
            //inner = outer = true;

            var ret = new HashSet<string>();
            var childElements = new List<Tuple<XElement, string>>();
            if (inner)
            {
                childElements.Add(Tuple.Create(element, element.Name()));
                var ancestorStr = "";
                foreach (var e in element.AncestorsOfOnlyChildAndSelf())
                {
                    ancestorStr = ancestorStr + "<" + e.NameWithId();
                    ret.Add(ancestorStr);
                }
            }
            var i = 1;
            if (outer)
            {
                var parentElement = Tuple.Create(element, element.Name());
                var descendantStr = "";
                foreach (var e in element.DescendantsOfOnlyChildAndSelf())
                {
                    descendantStr = descendantStr + "<" + e.NameWithId();
                    ret.Add(descendantStr);
                }
                // 自分自身の位置による区別も考慮する
                ret.Add(element.NameOrTokenWithId());
                for (; i <= length; i++)
                {
                    var newChildElements = new List<Tuple<XElement, string>>();
                    foreach (var t in childElements.Where(t2 => !t2.Item1.IsTokenSet()))
                    {
                        foreach (var e in t.Item1.Elements())
                        {
                            var key = t.Item2 + ">" + e.NameOrTokenWithId();
                            newChildElements.Add(Tuple.Create(e, key));
                            // トークンが存在するかチェックする弱い条件
                            // for Preconditions.checkArguments()
                            ret.Add(t.Item2 + ">'" + e.TokenText() + "'");
                        }
                        foreach (var e in t.Item1.Descendants().Where(e => e.IsTokenSet()))
                        {
                            // トークンが存在するかチェックする弱い条件
                            //ret.Add(t.Item2 + ">>'" + e.TokenText() + "'");
                        }
                    }
                    foreach (var e in parentElement.Item1.Siblings(10))
                    {
                        var key = parentElement.Item2 + "-" + e.NameOrTokenWithId();
                        newChildElements.Add(Tuple.Create(e, key));
                        // トークンが存在するかチェックする弱い条件
                        // for Preconditions.checkArguments()
                        ret.Add(parentElement.Item2 + "-'" + e.TokenText() + "'");
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
                            parentElement.Item2 + "<" + newParentElement.NameOrTokenWithId());
                    ret.Add(parentElement.Item2);
                }
            }
            for (; i <= length; i++)
            {
                var newChildElements = new List<Tuple<XElement, string>>();
                foreach (var t in childElements.Where(t2 => !t2.Item1.IsTokenSet()))
                {
                    foreach (var e in t.Item1.Elements())
                    {
                        var key = t.Item2 + ">" + e.NameOrTokenWithId();
                        newChildElements.Add(Tuple.Create(e, key));
                        // トークンが存在するかチェックする弱い条件
                        // for Preconditions.checkArguments()
                        ret.Add(t.Item2 + ">'" + e.TokenText() + "'");
                    }
                }
                ret.UnionWith(newChildElements.Select(t => t.Item2));
                childElements = newChildElements;
            }
            return ret;
        }

        public static HashSet<string> GetUnionKeys(
                this IEnumerable<XElement> elements, int length, bool inner = true, bool outer = true)
        {
            var commonKeys = new HashSet<string>();
            foreach (var element in elements)
            {
                var keys = element.GetSurroundingKeys(length, inner, outer);
                commonKeys.UnionWith(keys);
            }
            return commonKeys;
        }

        public static HashSet<string> GetCommonKeys(
                this IEnumerable<XElement> elements, int length, bool inner = true, bool outer = true)
        {
            HashSet<string> commonKeys = null;
            keysCount = 0;
            foreach (var element in elements)
            {
                var keys = element.GetSurroundingKeys(length, inner, outer);
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

        private static ISet<string> AdoptNodeNames(ICollection<XElement> outermosts)
        {
            var name2Count = new Dictionary<string, int>();
            var candidates = outermosts.AsParallel().SelectMany(
                    e => e.DescendantsOfOnlyChildAndSelf());
            foreach (var e in candidates)
            {
                var count = name2Count.GetValueOrDefault(e.Name());
                name2Count[e.Name()] = count + 1;
            }
            return outermosts.AsParallel().Select(
                    e => e.DescendantsOfOnlyChildAndSelf()
                            .Select(e2 => e2.Name())
                            .MaxElementOrDefault(name => name2Count[name]))
                    .ToHashSet();
        }

        public static IEnumerable<Tuple<int, LocationInfo>> GetSimilarElements(
                Processor processor, IEnumerable<LocationInfo> locations, XElement root,
                int range = 5, bool inner = true, bool outer = true)
        {
            try
            {
                var similarityRange = 0;

                // Convert the location informatoin (CodeRange) to the node (XElement) in the ASTs
                var elements = new List<XElement>();

                foreach (var location in locations)
                {
                    elements.Add(location.XElement);
                }

                // Determine the node names to extract candidate nodes from the ASTs
                var names = AdoptNodeNames(elements);

                // Extract candidate nodes that has one of the determined names
                var candidates = new List<IEnumerable<XElement>>();

                TimeWatch.Start();

                candidates.Add(
                        root.Descendants().AsParallel()
                                .Where(e => names.Contains(e.Name())).ToList());

                // Extract surrounding nodes from each candidate node
                var commonKeys = elements.GetCommonKeys(range, true, true);

                TimeWatch.Stop("FindOutCandidateElements");

                if (SimilarityRange == 0 && keysCount != 0)
                {
                    similarityRange = keysCount / 10;
                }
                else
                {
                    similarityRange = SimilarityRange;
                }

                if (commonKeys.Count <= similarityRange)
                {
                    return Enumerable.Empty<Tuple<int, LocationInfo>>();
                }

                int minSimilarity = commonKeys.Count - similarityRange;

                TimeWatch.Start();

                var aa = candidates.AsParallel().SelectMany(
                        kv =>
                        {
                            return kv.Select(
                                    e => Tuple.Create(
                                        // Count how many common surrounding nodes each candidate node has
                                        e.GetSurroundingKeys(range, inner, outer)
                                            .Count(commonKeys.Contains),
                                            e))
                                    .Where(e => e.Item1 > minSimilarity
                                    )
                                    .Select(
                                            t => Tuple.Create(
                                                    t.Item1,	// Indicates the simlarity
                                                    new LocationInfo
                                                    {
                                                        //       XElement xx = 
                                                        CodeRange = CodeRange.Locate(t.Item2),
                                                    }));
                        })
                    // Sort candidate nodes using the similarities
                        .OrderByDescending(t => t.Item1).ToList();
                TimeWatch.Stop("FindOutSimilarElements");
                return aa;
            }
            catch (Exception exc)
            {
                Debug.Write(exc.ToString());
            }
            return null;
        }
    }
}