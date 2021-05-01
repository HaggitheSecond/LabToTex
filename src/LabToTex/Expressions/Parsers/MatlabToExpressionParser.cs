using LabToTex.Common;
using LabToTex.Expressions.Elements;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LabToTex.Expressions.Parsers
{

    public partial class MatlabToExpressionParser
    {
        public ExpressionFile ParseToExpression(List<string> rawLines)
        {
            var matLabSpecification = new MatlabSpecification();
            var labToTextSpecification = new LabToTextSpecification();

            var lines = this.ConvertToLines(rawLines);

            var (declarationLines, otherLines) = this.ExtractDeclarationLines(lines, labToTextSpecification);
            var expressions = otherLines.Select(f => ParseLine(f, matLabSpecification)).ToList();

            var labToTextDeclarationParser = new LabToTextDeclarationsParser();
            labToTextDeclarationParser.ParseDeclarations(expressions, declarationLines, labToTextSpecification);

            return new ExpressionFile
            {
                Expressions = expressions
            };
        }

        private (List<Line> declarationLines, List<Line> otherLines) ExtractDeclarationLines(List<Line> lines, LabToTextSpecification specification)
        {
            int? declarationStartIndex = null;
            int? declarationsEndIndex = null;

            for (int i = 0; i < lines.Count; i++)
            {
                var currentLine = lines[i];

                if (currentLine.Value == $"% begin - {specification.LabToTexDeclarations}")
                    declarationStartIndex = i;

                if (currentLine.Value == $"% end")
                    declarationsEndIndex = i;
            }

            var actualLines = new List<Line>();
            var declarationLines = new List<Line>();

            if (declarationStartIndex.HasValue && declarationsEndIndex.HasValue)
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    if (i >= declarationStartIndex.Value && i <= declarationsEndIndex.Value)
                        declarationLines.Add(lines[i]);
                    else
                        actualLines.Add(lines[i]);
                }
            }
            else
            {
                actualLines = lines;
            }

            return (declarationLines, actualLines);
        }

        private List<Line> ConvertToLines(List<string> rawLines)
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

            return lines;
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

        private IList<string> ExtractLineParts(string line, MatlabSpecification specification)
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

                if (specification.IsKeyWord(currentChar.ToString()))
                    parts.Add(currentChar.ToString());

                if (currentChar == '%')
                    break;

                i++;
            }

            return parts.Where(f => string.IsNullOrWhiteSpace(f) == false).ToList();
        }

        private ExpressionElement ParseLine(Line line, MatlabSpecification specification)
        {
            ExpressionElement element;

            var parts = this.ExtractLineParts(line.Value, specification);

            if (parts.Any() == false)
            {
                element = new ExpressionEmptyElement();
            }
            else
            {
                var expressionParts = this.ParseExpressionElements(parts, specification);

                if (expressionParts.Last() is ExpressionEndStatementElement)
                    expressionParts.Remove(expressionParts.Last());

                if (expressionParts.Count == 1)
                {
                    element = new ExpressionElement
                    {
                        RawValue = expressionParts.First().RawValue
                    };
                }
                else if (expressionParts[0] is ExpressionVariableElement variableElement && expressionParts[1] is ExpressionAssignmentOperatorElement)
                {
                    var variableDeclaration = new ExpressionVariableDeclarationElement
                    {
                        Name = variableElement,
                        RawValue = line.Value
                    };

                    variableElement.Parent = variableDeclaration;

                    if (expressionParts[2] is ExpressionArrayDeclarationElement)
                        variableDeclaration.ValueExpression = this.ParseArrayDeclaration(expressionParts.Skip(2).ToList());
                    else if (expressionParts[2] is ExpressionAnnoynmousFunctionElement)
                        variableDeclaration.ValueExpression = this.AnnoynmousFunction(expressionParts.Skip(2).ToList());
                    else
                        variableDeclaration.ValueExpression = this.ParseTerm(expressionParts.Skip(2).ToList());

                    element = variableDeclaration;
                }
                else
                {
                    element = new ExpressionElement
                    {
                        RawValue = string.Join(" ", expressionParts.Select(f => f.RawValue))
                    };
                }
            }

            this.SetParentAndLineReference(element, line.Index);
            return element;
        }

        private void SetParentAndLineReference(ExpressionElement element, int lineReference, ExpressionElement parent = null)
        {
            if (element == null)
                return;

            if (parent != null)
                element.Parent = parent;

            element.LineReference = lineReference;

            foreach (var currentChild in element.GetChildren())
            {
                this.SetParentAndLineReference(currentChild, lineReference, element);
            }
        }

        private IList<ExpressionElement> ParseExpressionElements(IList<string> parts, MatlabSpecification specification)
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

                    if (specification.IsOperator(currentPart))
                    {
                        OperatorType type = OperatorType.Binary;

                        if (specification.IsUnaryOperator(currentPart))
                            type = OperatorType.Unary;
                        else if ( i == 0 || expressionParts.Last() is ExpressionOperatorElement)
                            type = OperatorType.BinaryAsUnary;

                        element = new ExpressionOperatorElement
                        {
                            Operator = currentPart,
                            Type = type
                        };
                    }
                    else if (specification.IsValue(currentPart))
                    {
                        element = new ExpressionValueElement
                        {
                            Value = decimal.Parse(currentPart)
                        };
                    }
                    else if (specification.IsVariable(currentPart))
                    {
                        element = new ExpressionVariableElement
                        {
                            Name = currentPart
                        };
                    }
                    else if (specification.IsAssignmentOperator(currentPart))
                    {
                        element = new ExpressionAssignmentOperatorElement();
                    }
                    else if (specification.IsArrayOperator(currentPart))
                    {
                        element = new ExpressionArrayDeclarationElement();
                    }
                    else if (specification.IsParenthesisOperators(currentPart))
                    {
                        element = new ExpressionParenthesisElement
                        {
                            Type = specification.GetParenthesisType(currentPart)
                        };
                    }
                    else if (specification.IsEndStatement(currentPart))
                    {
                        element = new ExpressionEndStatementElement();
                    }
                    else if(specification.IsAnnoymousFunction(currentPart))
                    {
                        element = new ExpressionAnnoynmousFunctionElement();
                    }
                    else
                    {
                        throw new Exception($"Could not parse value '{currentPart}'");
                    }

                    element.RawValue = currentPart;

                    expressionParts.Add(element);
                }
                catch (Exception e)
                {
                    expressionParts.Add(new ExpressionErrorElement
                    {
                        Exception = e,
                        RawValue = currentPart
                    });
                }
            }

            return expressionParts;
        }

        private ExpressionElement ParseArrayDeclaration(IList<ExpressionElement> expressionElements)
        {
            expressionElements.RemoveAt(0);
            expressionElements.RemoveAt(expressionElements.Count - 1);

            var elements = new List<ExpressionArrayElementElement>();

            var currentYDimension = 0;
            var currentXDimension = 0;

            for (int i = 0; i < expressionElements.Count; i++)
            {
                var currentElement = expressionElements[i];

                if (currentElement is ExpressionEndStatementElement)
                {
                    currentXDimension = 0;
                    currentYDimension++;
                }
                else
                {
                    currentXDimension++;
                    elements.Add(new ExpressionArrayElementElement
                    {
                        Index = new ArrayIndex
                        {
                            XIndex = currentXDimension,
                            YIndex = currentYDimension
                        },
                        Value = currentElement,
                        RawValue = currentElement.RawValue
                    });
                }
            }

            return new ExpressionArrayDeclarationElement
            {
                RawValue = string.Join(", ", expressionElements),
                Elements = elements
            };
        }

        private ExpressionElement AnnoynmousFunction(List<ExpressionElement> expressionElements)
        {
            // remove leading @ and (
            expressionElements.RemoveAt(0);
            expressionElements.RemoveAt(0);

            var closingParenthesis = this.FindClosingParenthesis(expressionElements.ToList());
            var innerElements = expressionElements.TakeWhile(f => object.ReferenceEquals(f, closingParenthesis) == false).ToList();

            foreach (var current in innerElements)
                expressionElements.Remove(current);
            expressionElements.Remove(closingParenthesis);

            return new ExpressionAnnoynmousFunctionElement()
            {
                Parameters = innerElements.OfType<ExpressionVariableElement>().ToList(),
                Expression = this.ParseTerm(expressionElements)
            };
         }

        private ExpressionElement ParseTerm(IList<ExpressionElement> expressionParts)
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
                                innerExpression = this.ParseTerm(innerElements);
                            }
                            else
                            {
                                var xIndex = 0;
                                var xIndexElement = innerElements.ElementAtOrDefault(0);

                                if (xIndexElement != null && int.TryParse(xIndexElement.RawValue, out var xIndexOut))
                                    xIndex = xIndexOut;

                                int? yIndex = null;
                                var yIndexElement = innerElements.ElementAtOrDefault(1);

                                if (yIndexElement != null && int.TryParse(yIndexElement.RawValue, out var yIndexOut))
                                    yIndex = yIndexOut;

                                innerExpression = new ExpressionArrayAccesorElement()
                                {
                                    RawValue = $"{previousElement}({string.Join(",", innerElements)})",
                                    Name = previousElement as ExpressionVariableElement,
                                    Index = new ArrayIndex
                                    {
                                        XIndex = xIndex,
                                        YIndex = yIndex
                                    }
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

                        if (currentElement is ExpressionOperatorElement operatorExpression && operatorExpression.IsSealed == false && operatorExpression.Type == OperatorType.Unary)
                        {
                            var element1 = workInProgressExpressionParts.ElementAt(i + 1);

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

                        if (currentElement is ExpressionOperatorElement operatorExpression && operatorExpression.IsSealed == false && operatorExpression.Type != OperatorType.Unary)
                        {
                            var element1 = workInProgressExpressionParts.ElementAtOrDefault(i - 1);
                            var element2 = workInProgressExpressionParts.ElementAtOrDefault(i + 1);

                            if (operatorExpression.Type == OperatorType.BinaryAsUnary)
                            {
                                operatorExpression.Operand1 = element2;

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

                return element;
            }
            catch (Exception e)
            {
                return new ExpressionErrorElement()
                {
                    Exception = e,
                    Children = expressionParts.ToList()
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
