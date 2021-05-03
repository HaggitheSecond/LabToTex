using LabToTex.Common;
using LabToTex.Expressions.Elements;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LabToTex.Expressions.Parsers
{

    public partial class MatlabToExpressionParser
    {
        private ExpressionFile _expressionFile;

        public ExpressionFile ParseToExpression(List<string> rawLines)
        {
            var matLabSpecification = new MatlabSpecification();
            var labToTextSpecification = new LabToTextSpecification();

            this._expressionFile = new ExpressionFile
            {
                Expressions = new List<ExpressionElement>()
            };

            var lines = this.ConvertToLines(rawLines);

            var (declarationLines, otherLines) = this.ExtractDeclarationLines(lines, labToTextSpecification);
            var expressions = otherLines.Select(f => ParseLine(f, matLabSpecification)).ToList();

            var labToTextDeclarationParser = new LabToTextDeclarationsParser();
            labToTextDeclarationParser.ParseDeclarations(expressions, declarationLines, labToTextSpecification);

            return this._expressionFile;
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
                    element = new ExpressionUnknownElement
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

                    if (expressionParts[2] is ExpressionArrayDeclarationElement)
                    {
                        variableDeclaration.Type = ExpressionVariableDeclarationType.ArrayDeclaration;
                        variableDeclaration.ValueExpression = this.ParseArrayDeclaration(expressionParts.Skip(2).ToList(), variableDeclaration);
                    }
                    else if (expressionParts[2] is ExpressionAnnoynmousFunctionElement)
                    {
                        variableDeclaration.Type = ExpressionVariableDeclarationType.AnnonymousFunction;
                        variableDeclaration.ValueExpression = this.ParseAnnoynmousFunction(expressionParts.Skip(2).ToList(), variableDeclaration);
                    }
                    else
                    {
                        variableDeclaration.Type = ExpressionVariableDeclarationType.Unknown;
                        variableDeclaration.ValueExpression = this.ParseTerm(expressionParts.Skip(2).ToList(), variableDeclaration);
                    }

                    element = variableDeclaration;
                }
                else
                {
                    element = new ExpressionUnknownElement
                    {
                        RawValue = string.Join(" ", expressionParts.Select(f => f.RawValue))
                    };
                }
            }

            this.SetParentAndLineReference(element, line.Index);
            this._expressionFile.Expressions.Add(element);
            return element;
        }

        private void SetParentAndLineReference(ExpressionElement element, int lineReference, ExpressionElement parent = null)
        {
            if (element == null || element.Parent != null)
                return;

            element.LineReference = lineReference;
            element.Parent = parent;
            var type = element.GetType();

            foreach (var currentProperty in type.GetProperties())
            {
                if (currentProperty.Name == nameof(element.Parent))
                    continue;

                if (currentProperty.PropertyType == typeof(ExpressionElement) || currentProperty.PropertyType.BaseType == typeof(ExpressionElement))
                {
                    this.SetParentAndLineReference((ExpressionElement)currentProperty.GetValue(element), lineReference, element);
                }

                if (currentProperty.PropertyType.IsGenericType && currentProperty.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var listType = currentProperty.PropertyType.GetGenericArguments()[0];

                    if (listType == typeof(ExpressionElement) || listType.BaseType == typeof(ExpressionElement))
                    {
                        var value = (currentProperty.GetValue(element) as IEnumerable<object>).Cast<object>().ToList();

                        foreach (var currentItem in value)
                        {
                            this.SetParentAndLineReference((ExpressionElement)currentItem, lineReference, element);
                        }
                    }
                }
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
                        else if (i == 0 || expressionParts.Last() is ExpressionOperatorElement)
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
                    else if (specification.IsAnnoymousFunction(currentPart))
                    {
                        element = new ExpressionAnnoynmousFunctionElement();
                    }
                    else if (specification.IsSeparator(currentPart))
                    {
                        element = new ExpressionSeparatorElement();
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

        private ExpressionElement ParseArrayDeclaration(IList<ExpressionElement> expressionElements, ExpressionVariableDeclarationElement variableDeclaration)
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
                Elements = elements,
                IsSealed = true
            };
        }

        private ExpressionElement ParseAnnoynmousFunction(List<ExpressionElement> expressionElements, ExpressionVariableDeclarationElement variableDeclaration)
        {
            // remove leading @ and (
            expressionElements.RemoveAt(0);
            expressionElements.RemoveAt(0);

            var closingParenthesis = this.FindClosingParenthesis(expressionElements.ToList());
            var innerElements = expressionElements.TakeWhile(f => object.ReferenceEquals(f, closingParenthesis) == false).ToList();

            foreach (var current in innerElements)
                expressionElements.Remove(current);
            expressionElements.Remove(closingParenthesis);

            var element = new ExpressionAnnoynmousFunctionElement()
            {
                Parameters = innerElements.OfType<ExpressionVariableElement>().ToList(),
            };

            // fill valuexpression with its paramters first so we can work with it while parsing the actual term
            variableDeclaration.ValueExpression = element;
            element.Expression = this.ParseTerm(expressionElements, variableDeclaration);
            element.IsSealed = true;
            return element;
        }

        private ExpressionElement ParseTerm(List<ExpressionElement> expressionElements, ExpressionVariableDeclarationElement variableDeclaration)
        {
            try
            {
                ExpressionElement element;

                if (expressionElements.Count == 1 && (expressionElements[0] is ExpressionValueElement || expressionElements[0] is ExpressionVariableElement))
                {
                    element = expressionElements.First();
                }
                else
                {
                    var workInProgressExpressionElements = expressionElements.ToList();

                    // first pass
                    // collapse paranthesis
                    var i = 0;
                    while (true)
                    {
                        var currentElement = workInProgressExpressionElements.ElementAtOrDefault(i);

                        if (currentElement == null)
                            break;

                        if (currentElement is ExpressionParenthesisElement paranthesisElement)
                        {
                            var closingParenthesis = this.FindClosingParenthesis(workInProgressExpressionElements.Skip(i).ToList());
                            var innerExpressions = workInProgressExpressionElements.Skip(i + 1).TakeWhile(f => object.ReferenceEquals(f, closingParenthesis) == false).ToList();

                            var previousElement = workInProgressExpressionElements.ElementAtOrDefault(i - 1);
                            ExpressionElement innerExpression;

                            if (previousElement == null || !(previousElement is ExpressionVariableElement))
                            {
                                innerExpression = this.ParseTerm(innerExpressions, variableDeclaration);
                            }
                            else
                            {
                                innerExpression = this.ParseFunctionCall(innerExpressions, (ExpressionVariableElement)previousElement, variableDeclaration);
                                workInProgressExpressionElements.Remove(previousElement);
                                i--;
                            }

                            workInProgressExpressionElements.Remove(paranthesisElement);

                            foreach (var currentInnerElement in innerExpressions)
                                workInProgressExpressionElements.Remove(currentInnerElement);

                            workInProgressExpressionElements.Remove(closingParenthesis);

                            workInProgressExpressionElements.Insert(i, innerExpression);
                        }

                        i++;
                    }

                    // second pass 
                    // handle unary operators
                    i = 0;
                    while (true)
                    {
                        var currentElement = workInProgressExpressionElements.ElementAtOrDefault(i);

                        if (currentElement == null)
                            break;

                        if (currentElement is ExpressionOperatorElement operatorExpression && operatorExpression.IsSealed == false && operatorExpression.Type == OperatorType.Unary)
                        {
                            var element1 = workInProgressExpressionElements.ElementAt(i + 1);

                            operatorExpression.Operand1 = element1;
                            workInProgressExpressionElements.Remove(operatorExpression.Operand1);

                            operatorExpression.IsSealed = true;
                        }

                        i++;
                    }

                    // third pass 
                    // handle binary operators
                    i = workInProgressExpressionElements.Count - 1;
                    while (true)
                    {
                        var currentElement = workInProgressExpressionElements.ElementAtOrDefault(i);

                        if (currentElement == null)
                            break;

                        if (currentElement is ExpressionOperatorElement operatorExpression && operatorExpression.IsSealed == false && operatorExpression.Type != OperatorType.Unary)
                        {
                            var element1 = workInProgressExpressionElements.ElementAtOrDefault(i - 1);
                            var element2 = workInProgressExpressionElements.ElementAtOrDefault(i + 1);

                            if (operatorExpression.Type == OperatorType.BinaryAsUnary)
                            {
                                operatorExpression.Operand1 = element2;

                                workInProgressExpressionElements.Remove(operatorExpression.Operand1);
                            }
                            else
                            {
                                operatorExpression.Operand1 = element1;
                                operatorExpression.Operand2 = element2;

                                workInProgressExpressionElements.Remove(operatorExpression.Operand1);
                                workInProgressExpressionElements.Remove(operatorExpression.Operand2);
                            }

                            operatorExpression.IsSealed = true;
                        }

                        i--;
                    }

                    if (workInProgressExpressionElements.Count != 1)
                        throw new Exception("Expression could not be reduced to a single element");

                    element = workInProgressExpressionElements.First();
                }

                return element;
            }
            catch (Exception e)
            {
                return new ExpressionErrorElement()
                {
                    Exception = e,
                    Children = expressionElements.ToList()
                };
            }
        }

        private ExpressionElement ParseFunctionCall(List<ExpressionElement> expressionElements, ExpressionVariableElement nameElement, ExpressionVariableDeclarationElement variableDeclaration)
        {
            // collapse all parameters separated by ExpressionSeparatorElement
            var parameters = new List<ExpressionElement>();
            List<ExpressionElement> currentParameterParts = null;
            var i = 0;
            while (true)
            {
                var currentElement = expressionElements.ElementAtOrDefault(i);

                if (currentElement == null)
                {
                    parameters.Add(this.ParseTerm(currentParameterParts, variableDeclaration));
                    break;
                }

                if (currentElement is ExpressionSeparatorElement)
                {
                    if (currentParameterParts != null)
                    {
                        parameters.Add(this.ParseTerm(currentParameterParts, variableDeclaration));
                    }
                    currentParameterParts = new List<ExpressionElement>();
                }
                else if (currentParameterParts == null)
                {
                    currentParameterParts = new List<ExpressionElement>
                    {
                        currentElement
                    };
                }
                else
                {
                    currentParameterParts.Add(currentElement);
                }

                i++;
            }

            var referencedElement = this._expressionFile.TryFindVariableDeclarationByName(nameElement.Name) ?? variableDeclaration;

            switch (referencedElement.Type)
            {
                case ExpressionVariableDeclarationType.Unknown:
                    {
                        return new ExpressionUnknownElement
                        {
                            RawValue = $"{nameElement.Name}({string.Join("", expressionElements.Select(f => f.RawValue))})"
                        };
                    }
                case ExpressionVariableDeclarationType.ArrayDeclaration:
                    {
                        return new ExpressionArrayAccesorElement()
                        {
                            RawValue = $"{nameElement}({string.Join(",", parameters)})",
                            Name = nameElement,
                            Indexes = new List<ExpressionElement>(parameters)
                        };
                    }
                case ExpressionVariableDeclarationType.AnnonymousFunction:
                    {
                        var valueExpression = (ExpressionAnnoynmousFunctionElement)referencedElement.ValueExpression;

                        if (variableDeclaration.IsSealed)
                        {
                            return new ExpressionFunctionCallElement
                            {
                                RawValue = $"@({string.Join(",", parameters)})",
                                Function = valueExpression,
                                Parameters = new List<ExpressionElement>(parameters)
                            };
                        }
                        else
                        {
                            var matchingParameter = valueExpression.Parameters.FirstOrDefault(f => f.Name == nameElement.Name);

                            if (matchingParameter != null)
                            {
                                return new ExpressionArrayAccesorElement()
                                {
                                    RawValue = $"{nameElement}({string.Join(",", parameters)})",
                                    Name = nameElement,
                                    Indexes = new List<ExpressionElement>(parameters)
                                };
                            }
                            else
                            {
                                return new ExpressionFunctionCallElement
                                {
                                    RawValue = $"@({string.Join(",", parameters)})",
                                    Function = valueExpression,
                                    Parameters = new List<ExpressionElement>(parameters)
                                };
                            }
                        }
                    }
                default:
                    throw new ArgumentOutOfRangeException();
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
