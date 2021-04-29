using System.Linq;

namespace LabToTex.Expressions.Elements
{
    public class ExpressionVariableElement : ExpressionElement
    {
        public string Name { get; set; }

        public override string ToString()
        {
            var outputName = this.Name;

            foreach (var currentLetter in MatlabSpecification.SpecialChars)
            {
                outputName = outputName.Replace(currentLetter, "\\" + currentLetter + " ");
            }

            if (this.Name.Contains("_") == false)
                return outputName;

            var parts = outputName.Split("_").ToList();

            outputName = parts[0] + "_{" + string.Join(',', parts.Skip(1)) + "}";
            return outputName;
        }
    }
}
