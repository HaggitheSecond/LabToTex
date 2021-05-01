using LabToTex.Expressions.Elements;
using LabToTex.Expressions.Parsers;
using LabToTex.Writer;
using System;
using System.IO;
using System.Linq;

namespace LabToTex.Parsers
{
    public class MatlabParser
    {
        public void Parse(string sourceFilePath, string targetFilePath, string texTemplateFilePath)
        {
            var expressionFile = new MatlabToExpressionParser().ParseToExpression(File.ReadAllLines(sourceFilePath).ToList());
            new LatexWriter().WriteFile(targetFilePath, texTemplateFilePath, expressionFile);       
        }
    }
}
