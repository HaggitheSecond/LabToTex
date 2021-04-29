using LabToTex.Expressions.Elements;
using LabToTex.Specifications;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LabToTex.Writer
{
    public class LatexWriter
    {
        public void WriteFile(string fileName, string template, List<ExpressionElement> expressions)
        {
            var specification = new LatexSpecification();

            var outputLines = File.ReadAllLines(template).ToList();

            outputLines.InsertRange(this.FindStartTagIndex(outputLines, specification), expressions.Select(f => this.WriteExpression(f, specification)));

            File.WriteAllLines(fileName, outputLines);
        }

        private int FindStartTagIndex(List<string> lines, LatexSpecification specification)
        {
            var startTag = lines.First(f => string.Equals(f.Trim(), specification.StartTag, StringComparison.InvariantCultureIgnoreCase));
            var startTagIndex = lines.IndexOf(startTag);

            if (startTagIndex == -1)
                throw new Exception("Could not find starttagindex");

            return startTagIndex;
        }

        private string WriteExpression(ExpressionElement expression, LatexSpecification specification)
        {
            if (expression == null)
                return string.Empty;

            var text = string.Empty;

            switch (expression)
            {
                case ExpressionEmptyElement _:
                    return specification.MediumLineBreak;

                case ExpressionVariableElement variableElement:
                    text = this.WriteVariableExpressionElement(variableElement, specification);
                    break;

                case ExpressionVariableDeclarationElement variableDeclarationElement:
                    text = this.WriteVariableDeclarationElement(variableDeclarationElement, specification);
                    break;

                case ExpressionOperatorElement operatorElement:
                    text = this.WriteOperatorElement(operatorElement, specification);
                    break;

                case ExpressionValueElement valueElement:
                    text = this.WriteValueElement(valueElement, specification);
                    break;

                case ExpressionArrayDeclarationElement arrayDeclarationElement:
                    text = this.WriteArrayDeclarationElement(arrayDeclarationElement, specification);
                    break;

                case ExpressionArrayAccesorElement arrayAccesorElement:
                    text = this.WriteArrayAccessorElement(arrayAccesorElement, specification);
                    break;

                default:
                    text = expression.ToString();
                    break;
            }

            return expression.Parent == null ? $"${text}$ {specification.LineBreak}" : text;
        }

        private string WriteArrayAccessorElement(ExpressionArrayAccesorElement arrayAccesorElement, LatexSpecification specification)
        {
            var index = arrayAccesorElement.Index.XIndex.ToString();

            if (arrayAccesorElement.Index.YIndex.HasValue)
                index += "," + arrayAccesorElement.Index.YIndex.Value;

            return $"{this.WriteExpression(arrayAccesorElement.Name, specification)}({index})";
        }

        private string WriteArrayDeclarationElement(ExpressionArrayDeclarationElement element, LatexSpecification specification)
        {
            var dimensions = element.Elements.Select(f => f.Index.YIndex).Distinct().Count();

            var output = @"\begin{" + specification.DesieredMatrixType + "}";

            if (dimensions == 1)
            {
                output += string.Join("&", element.Elements.Select(f => this.WriteExpression(f, specification)));
            }
            else
            {
                for (int i = 0; i < dimensions; i++)
                {
                    var parts = element.Elements.Where(f => f.Index.YIndex == i);
                    output += string.Join("&", parts.Select(f => this.WriteExpression(f, specification))) + @"\\";
                }
            }

            return output += @"\end{" + specification.DesieredMatrixType + "}";
        }

        private string WriteValueElement(ExpressionValueElement valueElement, LatexSpecification specification)
        {
            return valueElement.Value.ToString();
        }

        private string WriteOperatorElement(ExpressionOperatorElement element, LatexSpecification specification)
        {
            var operand1 = this.WriteExpression(element.Operand1, specification);
            var operand2 = this.WriteExpression(element.Operand2, specification);

            if (element.Operator == "/")
                return string.Format("\\frac{{{0}}}{{{1}}}", operand1, operand2);

            var @operator = element.Operator;

            if (@operator == "*")
                @operator = specification.DesiredMultiplication;

            return element.Type switch
            {
                OperatorType.Binary => $"{operand1} {@operator} {operand2}",
                OperatorType.Unary => $"{@operator}({operand1})",
                OperatorType.BinaryAsUnary => $"{@operator}{operand1}",
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        private string WriteVariableDeclarationElement(ExpressionVariableDeclarationElement element, LatexSpecification specification)
        {
            return $"{this.WriteExpression(element.Name, specification)} = {this.WriteExpression(element.ValueExpression, specification)}";
        }

        private string WriteVariableExpressionElement(ExpressionVariableElement element, LatexSpecification specification)
        {
            var outputName = element.Name;

            foreach (var currentLetter in specification.SpecialChars)
            {
                outputName = outputName.Replace(currentLetter, "\\" + currentLetter + " ");
            }

            if (element.Name.Contains("_") == false)
                return outputName;

            var parts = outputName.Split("_").ToList();

            outputName = parts[0] + "_{" + string.Join(',', parts.Skip(1)) + "}";
            return outputName;
        }
    }
}
