using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Code2Xml.Core;
using Code2Xml.Core.Location;
using NUnit.Framework;

namespace HighlightAndMove.Tests
{
	[TestFixture]
    public class InferenceTest
    {
		[Test]
		[TestCase(@"../../../HighlightAndMove.Tests/InferenceTest.cs")]
		public void TestGetSimilarElements(string path) {
			var processor = ProcessorLoader.CSharpUsingAntlr3;	// processorIdentifier indicates here
			var fileInfo = new FileInfo(path);					// fileInfoIdentifier indicates here
			var code = File.ReadAllText(path);
			var xml = processor.GenerateXml(fileInfo);
			var elements = xml.Descendants("identifier").ToList();
			var processorIdentifier = new LocationInfo {
				CodeRange = CodeRange.Locate(elements.First(e => e.TokenText() == "processor")),
				FileInfo = fileInfo,
			};
			var fileInfoIdentifier = new LocationInfo {
				CodeRange = CodeRange.Locate(elements.First(e => e.TokenText() == "fileInfo")),
				FileInfo = fileInfo,
			};
			var xml2 = processor.GenerateXml(new FileInfo(fileInfo.FullName));
			var ret = Inferrer.GetSimilarElements(processor, new[] { processorIdentifier, fileInfoIdentifier },
					new[] { fileInfo });
			foreach (var tuple in ret.Take(10)) {
				var score = tuple.Item1;
				var location = tuple.Item2;
				var startAndEnd = location.CodeRange.ConvertToIndicies(code);
				var fragment = code.Substring(startAndEnd.Item1, startAndEnd.Item2 - startAndEnd.Item1);
				Console.WriteLine(fragment);
			}
		}
    }
}
