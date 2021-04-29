using LabToTex.Expressions.Elements;
using LabToTex.Expressions.Parsers;
using System;
using System.IO;
using System.Linq;

namespace LabToTex.Parsers
{
    public class MatlabParser
    {
        private const string _texStartMarker = "%labtotextstart";

        public void Parse(string sourceFilePath, string targetFilePath, string texTemplateFilePath)
        {
            var expressions = new MatlabToExpressionParser().ParseToExpression(File.ReadAllLines(sourceFilePath).ToList());

            var outputLines = File.ReadAllLines(texTemplateFilePath).ToList();

            var startMarker = outputLines.First(f => string.Equals(f.Trim(), _texStartMarker, StringComparison.InvariantCultureIgnoreCase));
            var startMarkerIndex = outputLines.IndexOf(startMarker);

            outputLines.InsertRange(startMarkerIndex, expressions.Select(f => f.ToString() + " " + (f is ExpressionEmptyElement ? @"\medskip" : @"\par")).ToList());

            File.WriteAllLines(targetFilePath, outputLines);
        }
    }
}
