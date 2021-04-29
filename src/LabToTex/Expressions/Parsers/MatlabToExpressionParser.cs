using LabToTex.Expressions.Elements;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LabToTex.Expressions.Parsers
{
    public class MatlabToExpressionParser
    {
        public List<ExpressionElement> ParseToExpression(List<string> rawLines)
        {
            var lines = new List<Line>();

            for (int i = 0; i < rawLines.Count; i++)
            {
                lines.Add(new Line
                {
                    Value = rawLines[i],
                    Index = i + 1
                });
            }

            lines = this.CollapseMultiLineStatements(lines);

            return lines.Select(f => ParseLine(f)).ToList();
        }

        private List<Line> CollapseMultiLineStatements(List<Line> lines)
        {
            var collapsedLines = new List<Line>();

            for (int i = 0; i < lines.Count; i++)
            {
                var currentLine = lines.ElementAt(i);

                // multiline array declerations
                if (currentLine.Value.Count(f => f == '[') != currentLine.Value.Count(f => f == ']'))
                {
                    var includedLineCount = 1;

                    for (int j = i + 1; j < lines.Count; j++)
                    {
                        includedLineCount++;

                        if (lines[j].Value.Contains("]"))
                            break;
                    }

                    collapsedLines.Add(new Line
                    {
                        Value = string.Join(" ", lines.Skip(i).Take(includedLineCount).Select(f => f.Value).ToList()),
                        Index = currentLine.Index
                    });
                    i += includedLineCount - 1;
                }
                else
                {
                    collapsedLines.Add(currentLine);
                }
            }

            return collapsedLines;
        }

        private class Line
        {
            public string Value { get; set; }
            public int Index { get; set; }
        }

        private IList<string> ExtractLineParts(string line)
        {
            var parts = new List<string>();

            var i = 0;
            while (i < line.Length)
            {
                var currentChar = line[i];

                if (char.IsLetterOrDigit(currentChar))
                {
                    var wholethingy = string.Join("", line[i..].TakeWhile(f => char.IsLetterOrDigit(f) || f == '.' || f == '_'));
                    i += wholethingy.Length;
                    parts.Add(wholethingy);
                    continue;
                }

                if (MatlabSpecification.IsKeyWord(currentChar.ToString()))
                    parts.Add(currentChar.ToString());

                if (currentChar == '%')
                    break;

                i++;
            }

            return parts.Where(f => string.IsNullOrWhiteSpace(f) == false).ToList();
        }

        private ExpressionElement ParseLine(Line line)
        {
            var parts = this.ExtractLineParts(line.Value);

            if (parts.Any() == false)
                return new ExpressionEmptyElement()
                {
                    LineReference = line.Index
                };

            var expressionParts = this.ParseExpressionElements(parts, line.Index);

            if (expressionParts.Count == 1)
                return new ExpressionElement
                {
                    LineReference = line.Index,
                    RawValue = expressionParts.First().RawValue
                };

            if (expressionParts[0] is ExpressionVariableElement variableElement && expressionParts[1] is ExpressionAssignmentOperatorElement)
            {
                var variableDeclaration = new ExpressionVariableDeclarationElement
                {
                    LineReference = line.Index,
                    Name = variableElement,
                    RawValue = line.Value
                };

                variableElement.Parent = variableDeclaration;

                if (expressionParts[2] is ExpressionArrayDeclarationElement)
                    variableDeclaration.ValueExpression = this.ParseArrayDeclaration(expressionParts.Skip(2).ToList(), variableDeclaration);
                else
                    variableDeclaration.ValueExpression = this.ParseTerm(expressionParts.Skip(2).ToList(), variableDeclaration);

                return variableDeclaration;
            }

            return new ExpressionElement
            {
                LineReference = line.Index,
                RawValue = string.Join(" ", expressionParts.Select(f => f.RawValue))
            };
        }

        private IList<ExpressionElement> ParseExpressionElements(IList<string> parts, int index)
        {
            var expressionParts = new List<ExpressionElement>();

            for (var i = 0; i < parts.Count; i++)
            {
                var currentPart = parts.ElementAtOrDefault(i);

                if (currentPart == null)
                    break;

                try
                {
                    ExpressionElement element;

                    if (MatlabSpecification.IsOperator(currentPart))
                    {
                        element = new ExpressionOperatorElement
                        {
                            Operator = currentPart,
                            IsUnary = MatlabSpecification.IsUnaryOperator(currentPart)
                        };
                    }
                    else if (MatlabSpecification.IsValue(currentPart))
                    {
                        element = new ExpressionValueElement
                        {
                            Value = decimal.Parse(currentPart)
                        };
                    }
                    else if (MatlabSpecification.IsVariable(currentPart))
                    {
                        element = new ExpressionVariableElement
                        {
                            Name = currentPart
                        };
                    }
                    else if (MatlabSpecification.IsAssignmentOperator(currentPart))
                    {
                        element = new ExpressionAssignmentOperatorElement();
                    }
                    else if (MatlabSpecification.IsArrayOperator(currentPart))
                    {
                        element = new ExpressionArrayDeclarationElement();
                    }
                    else if (MatlabSpecification.IsParenthesisOperators(currentPart))
                    {
                        element = new ExpressionParenthesisElement
                        {
                            Type = MatlabSpecification.GetParenthesisType(currentPart)
                        };
                    }
                    else
                    {
                        throw new Exception($"Could not parse value '{currentPart}'");
                    }

                    element.LineReference = index;
                    element.RawValue = currentPart;

                    expressionParts.Add(element);
                }
                catch (Exception e)
                {
                    expressionParts.Add(new ExpressionErrorElement
                    {
                        LineReference = index,
                        Exception = e,
                        RawValue = currentPart
                    });
                }
            }

            return expressionParts;
        }

        private ExpressionElement ParseArrayDeclaration(IList<ExpressionElement> expressionParts, ExpressionElement parent)
        {
            return new ExpressionArrayDeclarationElement
            {
                RawValue = string.Join(", ", expressionParts),
                Parent = parent
            };
        }

        private ExpressionElement ParseTerm(IList<ExpressionElement> expressionParts, ExpressionElement parent)
        {
            try
            {
                ExpressionElement element;

                if (expressionParts.Count == 1 && (expressionParts[0] is ExpressionValueElement || expressionParts[0] is ExpressionVariableElement))
                {
                    element = expressionParts.First();
                }
                else
                {
                    var workInProgressExpressionParts = expressionParts.ToList();

                    // first pass
                    // collapse paranthesis
                    var i = 0;
                    while (true)
                    {
                        var currentElement = workInProgressExpressionParts.ElementAtOrDefault(i);

                        if (currentElement == null)
                            break;

                        if (currentElement is ExpressionParenthesisElement paranthesisElement)
                        {
                            var closingParenthesis = this.FindClosingParenthesis(workInProgressExpressionParts.Skip(i).ToList());
                            var innerElements = workInProgressExpressionParts.Skip(i + 1).TakeWhile(f => object.ReferenceEquals(f, closingParenthesis) == false).ToList();

                            var previousElement = workInProgressExpressionParts.ElementAtOrDefault(i - 1);
                            ExpressionElement innerExpression;

                            if (previousElement == null || !(previousElement is ExpressionVariableElement))
                            {
                                innerExpression = this.ParseTerm(innerElements, currentElement);
                            }
                            else
                            {
                                innerExpression = new ExpressionArrayAccesorElement()
                                {
                                    LineReference = previousElement.LineReference,
                                    RawValue = $"{previousElement}({string.Join(",", innerElements)})"
                                };

                                workInProgressExpressionParts.Remove(previousElement);
                                i--;
                            }

                            workInProgressExpressionParts.Remove(paranthesisElement);

                            foreach (var currentInnerElement in innerElements)
                                workInProgressExpressionParts.Remove(currentInnerElement);

                            workInProgressExpressionParts.Remove(closingParenthesis);

                            workInProgressExpressionParts.Insert(i, innerExpression);
                        }

                        i++;
                    }

                    // second pass 
                    // handle unary operators
                    i = 0;
                    while (true)
                    {
                        var currentElement = workInProgressExpressionParts.ElementAtOrDefault(i);

                        if (currentElement == null)
                            break;

                        if (currentElement is ExpressionOperatorElement operatorExpression && operatorExpression.IsSealed == false && operatorExpression.IsUnary)
                        {
                            var element1 = workInProgressExpressionParts.ElementAt(i + 1);

                            element1.Parent = currentElement;

                            operatorExpression.Operand1 = element1;
                            workInProgressExpressionParts.Remove(operatorExpression.Operand1);

                            operatorExpression.IsSealed = true;
                        }

                        i++;
                    }

                    // third pass 
                    // handle binary operators
                    i = workInProgressExpressionParts.Count - 1;
                    while (true)
                    {
                        var currentElement = workInProgressExpressionParts.ElementAtOrDefault(i);

                        if (currentElement == null)
                            break;

                        if (currentElement is ExpressionOperatorElement operatorExpression && operatorExpression.IsSealed == false && operatorExpression.IsUnary == false)
                        {
                            var element1 = workInProgressExpressionParts.ElementAtOrDefault(i - 1);
                            var element2 = workInProgressExpressionParts.ElementAtOrDefault(i + 1);

                            if (element1 != null)
                                element1.Parent = currentElement;
                            if (element2 != null)
                                element2.Parent = currentElement;

                            if (MatlabSpecification.CanBinaryOperatorBeUsedAsUnary(operatorExpression.Operator) &&
                                (element1 == null || element1 is ExpressionOperatorElement))
                            {
                                operatorExpression.Operand1 = element2;
                                operatorExpression.IsUnary = true;
                                workInProgressExpressionParts.Remove(operatorExpression.Operand1);
                            }
                            else
                            {
                                operatorExpression.Operand1 = element1;
                                operatorExpression.Operand2 = element2;

                                workInProgressExpressionParts.Remove(operatorExpression.Operand1);
                                workInProgressExpressionParts.Remove(operatorExpression.Operand2);
                            }

                            operatorExpression.IsSealed = true;
                        }

                        i--;
                    }

                    if (workInProgressExpressionParts.Count != 1)
                        throw new Exception("Expression could not be reduced to a single element");

                    element = workInProgressExpressionParts.First();
                }

                element.Parent = parent;

                return element;
            }
            catch (Exception e)
            {
                return new ExpressionErrorElement()
                {
                    Exception = e,
                    Children = expressionParts.ToList(),
                    LineReference = expressionParts.First().LineReference,
                    Parent = parent
                };
            }
        }

        private ExpressionElement FindClosingParenthesis(IList<ExpressionElement> expressionParts)
        {
            var innerOpeningParanthesis = 0;

            for (int j = 1; j < expressionParts.Count; j++)
            {
                if (expressionParts[j] is ExpressionParenthesisElement innerClosingElement)
                {
                    if (innerClosingElement.Type == ParenthesisType.Open)
                    {
                        innerOpeningParanthesis++;
                    }
                    else
                    {
                        if (innerOpeningParanthesis != 0)
                            innerOpeningParanthesis--;
                        else
                            return innerClosingElement;
                    }
                }
            }

            throw new ArgumentOutOfRangeException("Could not find closing paranthesis");
        }
    }
}
