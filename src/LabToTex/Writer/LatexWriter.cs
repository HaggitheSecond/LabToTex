using LabToTex.Expressions.Elements;
using LabToTex.Expressions.Parsers;
using LabToTex.Specifications;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LabToTex.Writer
{
    public class LatexWriter
    {
        public void WriteFile(string fileName, string template, ExpressionFile expressionFile)
        {
            var specification = new LatexSpecification();
            var labtotexspecification = new LabToTextSpecification();

            var outputLines = File.ReadAllLines(template).ToList();

            outputLines.InsertRange(this.FindStartTagIndex(outputLines, specification), expressionFile.Expressions.Select(f => this.WriteExpression(f, specification, labtotexspecification)));

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

        private string WriteExpression(ExpressionElement expression, LatexSpecification specification, LabToTextSpecification labtotexspecification)
        {
            try
            {
                if (expression == null)
                    return string.Empty;

                var text = string.Empty;

                switch (expression)
                {
                    case ExpressionEmptyElement _:
                        return specification.MediumLineBreak;

                    case ExpressionVariableElement variableElement:
                        text = this.WriteVariableExpressionElement(variableElement, specification, labtotexspecification);
                        break;

                    case ExpressionVariableDeclarationElement variableDeclarationElement:
                        text = this.WriteVariableDeclarationElement(variableDeclarationElement, specification, labtotexspecification);
                        break;

                    case ExpressionOperatorElement operatorElement:
                        text = this.WriteOperatorElement(operatorElement, specification, labtotexspecification);
                        break;

                    case ExpressionValueElement valueElement:
                        text = this.WriteValueElement(valueElement, specification, labtotexspecification);
                        break;

                    case ExpressionArrayDeclarationElement arrayDeclarationElement:
                        text = this.WriteArrayDeclarationElement(arrayDeclarationElement, specification, labtotexspecification);
                        break;

                    case ExpressionArrayAccesorElement arrayAccesorElement:
                        text = this.WriteArrayAccessorElement(arrayAccesorElement, specification, labtotexspecification);
                        break;

                    case ExpressionAnnoynmousFunctionElement annoynmousFunctionElement:
                        text = this.WriteAnnoynmousFunctionElement(annoynmousFunctionElement, specification, labtotexspecification);
                        break;

                    case ExpressionFunctionCallElement functionCallElement:
                        text = this.WriteFunctionCallElement(functionCallElement, specification, labtotexspecification);
                        break;

                    case ExpressionArrayElementElement arrayElementElement:
                        text = this.WriteExpressionArrayElementElement(arrayElementElement, specification, labtotexspecification);
                        break;

                    default:
                        text = expression.ToString();
                        break;
                }

                return expression.Parent == null ? $"${text}$ {specification.LineBreak}" : text;
            }
            catch(Exception e)
            {
                return "faulty line ";
            }
        }

        private string WriteFunctionCallElement(ExpressionFunctionCallElement functionCallElement, LatexSpecification specification, LabToTextSpecification labtotexspecification)
        {
            if (labtotexspecification.InlineAnnoynmousFunctions)
            {
                var expression = functionCallElement.Function.Expression;

                this.ReplaceParameters(expression, functionCallElement.Parameters, functionCallElement.Function.Parameters.OfType<ExpressionVariableElement>().ToList());

                return this.WriteExpression(expression, specification, labtotexspecification);
            }
            else
            {
                var name = ((ExpressionVariableDeclarationElement)functionCallElement.Function.Parent).Name;

                return $"{name}({string.Join(", ", functionCallElement.Parameters.Select(f => this.WriteExpression(f, specification, labtotexspecification)))})";
            }
        }

        private void ReplaceParameters(ExpressionElement element, List<ExpressionElement> parameters, List<ExpressionVariableElement> functionParamters)
        {
            if (!(element is ExpressionOperatorElement operatorElement))            
                return;
            
            if (operatorElement.Operand1 != null)
            {
                if (operatorElement.Operand1 is ExpressionVariableElement expressionVariable)
                {
                    var templateParameter = functionParamters.FirstOrDefault(f => f.Name == expressionVariable.Name);
                    var templateParameterIndex = functionParamters.IndexOf(templateParameter);
                    var parameter = parameters.ElementAtOrDefault(templateParameterIndex);

                    if (parameter != null)
                    {
                        operatorElement.Operand1 = parameter;
                    }
                }
                else if (operatorElement.Operand1 is ExpressionArrayAccesorElement arrayAccesorElement)
                {

                }
                else
                {
                    this.ReplaceParameters(operatorElement.Operand1, parameters, functionParamters);
                }
            }

            if (operatorElement.Operand2 != null)
            {
                if (operatorElement.Operand2 is ExpressionVariableElement expressionVariable)
                {
                    var templateParameter = functionParamters.FirstOrDefault(f => f.Name == expressionVariable.Name);
                    var templateParameterIndex = functionParamters.IndexOf(templateParameter);
                    var parameter = parameters.ElementAtOrDefault(templateParameterIndex);

                    if (parameter != null)
                    {
                        operatorElement.Operand2 = parameter;
                    }
                }
                else if (operatorElement.Operand2 is ExpressionArrayAccesorElement arrayAccesorElement)
                {

                }
                else
                {
                    this.ReplaceParameters(operatorElement.Operand2, parameters, functionParamters);
                }
            }
        }

        private string WriteExpressionArrayElementElement(ExpressionArrayElementElement arrayElementElement, LatexSpecification specification, LabToTextSpecification labtotexspecification)
        {

            return "";
        }

        private string WriteAnnoynmousFunctionElement(ExpressionAnnoynmousFunctionElement annoynmousFunctionElement, LatexSpecification specification, LabToTextSpecification labtotexspecification)
        {
            return $"@({string.Join(",", annoynmousFunctionElement.Parameters.Select(f => this.WriteExpression(f, specification, labtotexspecification)))}) {this.WriteExpression(annoynmousFunctionElement.Expression, specification, labtotexspecification)}";
        }

        private string WriteArrayAccessorElement(ExpressionArrayAccesorElement arrayAccesorElement, LatexSpecification specification, LabToTextSpecification labtotexspecification)
        {
            return $"{this.WriteExpression(arrayAccesorElement.Name, specification, labtotexspecification)}({string.Join(",", arrayAccesorElement.Indexes.Select(f => this.WriteExpression(f, specification, labtotexspecification)))})";
        }

        private string WriteArrayDeclarationElement(ExpressionArrayDeclarationElement element, LatexSpecification specification, LabToTextSpecification labtotexspecification)
        {
            var dimensions = element.Elements.Select(f => f.Index.YIndex).Distinct().Count();

            var output = @"\begin{" + specification.DesieredMatrixType + "}";

            if (dimensions == 1)
            {
                output += string.Join("&", element.Elements.Select(f => this.WriteExpression(f, specification, labtotexspecification)));
            }
            else
            {
                for (int i = 0; i < dimensions; i++)
                {
                    var parts = element.Elements.Where(f => f.Index.YIndex == i);
                    output += string.Join("&", parts.Select(f => this.WriteExpression(f, specification, labtotexspecification))) + @"\\";
                }
            }

            return output += @"\end{" + specification.DesieredMatrixType + "}";
        }

        private string WriteValueElement(ExpressionValueElement valueElement, LatexSpecification specification, LabToTextSpecification labtotexspecification)
        {
            return valueElement.Value.ToString();
        }

        private string WriteOperatorElement(ExpressionOperatorElement element, LatexSpecification specification, LabToTextSpecification labtotexspecification)
        {
            var operand1 = this.WriteExpression(element.Operand1, specification, labtotexspecification);
            var operand2 = this.WriteExpression(element.Operand2, specification, labtotexspecification);

            if (element.Operator == "/")
                return string.Format("\\frac{{{0}}}{{{1}}}", operand1, operand2);

            var @operator = element.Operator;

            if (@operator == "*")
                @operator = specification.DesiredMultiplication;

            var shouldHaveParanthesis = element.Parent is ExpressionOperatorElement;

            return element.Type switch
            {
                OperatorType.Binary => shouldHaveParanthesis ? $"({operand1} {@operator} {operand2})" : $"{operand1} {@operator} {operand2}",
                OperatorType.Unary => $"{@operator}({operand1})",
                OperatorType.BinaryAsUnary => $"{@operator}{operand1}",
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        private string WriteVariableDeclarationElement(ExpressionVariableDeclarationElement element, LatexSpecification specification, LabToTextSpecification labtotexspecification)
        {
            return $"{this.WriteExpression(element.Name, specification, labtotexspecification)} = {this.WriteExpression(element.ValueExpression, specification, labtotexspecification)}";
        }

        private string WriteVariableExpressionElement(ExpressionVariableElement element, LatexSpecification specification, LabToTextSpecification labtotexspecification)
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
