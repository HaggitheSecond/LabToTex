using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LabToTex
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new MatlabParser();

            //parser.Parse(@"C:\Users\haggi\Documents\Uni\semester 1\latex\labtotex\messtechnik_ubeung5.m",
            //    @"C:\Users\haggi\Documents\Uni\semester 1\latex\labtotex\test01.tex",
            //    @"C:\Users\haggi\Documents\Uni\semester 1\latex\helloworld.tex");

            var directory = new DirectoryInfo(@"C:\Users\haggi\Documents\Uni\semester 1\latex\labtotexv2");

            foreach (var currentFile in directory.GetFiles())
            {
                if (currentFile.Extension != ".m")
                    File.Delete(currentFile.FullName);
            }

            foreach (var currentFile in directory.GetFiles("*.m"))
            {
                var outputFileName = Path.Combine(directory.FullName, Path.GetFileNameWithoutExtension(currentFile.FullName) + ".tex");

                parser.Parse(currentFile.FullName,
                    outputFileName,
                    @"C:\Users\haggi\Documents\Uni\semester 1\latex\helloworld.tex");
            }
        }
    }

    public static class MatlabKeywords
    {
        public static List<string> SpecialChars = new List<string>
        {
            "alpha",
            "Alpha",

            "Beta",
            "beta",

            "Gamma",
            "gamma",

            "Delta",
            "delta",

            "Epsilon",
            "epsilon",
            "varepsilon",

            "Lambda",
            "lambda",

            "pi",
            "Pi"
        };

        public static List<string> UnaryOperators = new List<string>
        {
            "sin",
            "cos",
            "cot",
            "tan",
            "atan",
            "sqrt",
            "log",
            "log10",
            "log2"
        };

        public static List<string> BinaryOperators = new List<string>
        {
            "*",
            "^",
            "/"
        };

        public static List<string> DualPurposeOperators = new List<string>
        {
            "+",
            "-"
        };

        public static List<string> AssignmentOperators = new List<string>
        {
            "="
        };

        public static List<string> ArrayDeclarationOperators = new List<string>
        {
            "[",
            "]"
        };

        public static List<string> ParenthesisOperators = new List<string>
        {
            "(",
            ")"
        };

        public static bool IsOperator(string @string) => IsBinaryOperator(@string) || IsUnaryOperator(@string);
        public static bool IsBinaryOperator(string @string) => BinaryOperators.Union(DualPurposeOperators).Any(f => f == @string);
        public static bool IsUnaryOperator(string @string) => UnaryOperators.Any(f => f == @string);
        public static bool CanBinaryOperatorBeUsedAsUnary(string @string) => DualPurposeOperators.Any(f => f == @string);
        public static bool IsValue(string @string) => @string.Any(f => char.IsDigit(f) == false && f != '.') == false;
        public static bool IsVariable(string @string) => IsValue(@string) == false && @string.All(f => IsKeyWord(f.ToString()) == false);
        public static bool IsAssignmentOperator(string @string) => AssignmentOperators.Any(f => f == @string);
        public static bool IsArrayOperator(string @string) => ArrayDeclarationOperators.Any(f => f == @string);
        public static bool IsParenthesisOperators(string @string) => ParenthesisOperators.Any(f => f == @string);
        public static ParenthesisType GetParenthesisType(string @string) => @string == ")" ? ParenthesisType.Close : ParenthesisType.Open;
        public static bool IsKeyWord(string @string) => IsOperator(@string) || IsAssignmentOperator(@string) || IsArrayOperator(@string) || IsParenthesisOperators(@string);
    }

    public class MatlabParser
    {
        private const string _texStartMarker = "%labtotextstart";

        public void Parse(string sourceFilePath, string targetFilePath, string texTemplateFilePath)
        {
            var lines = this.ParseLines(File.ReadAllLines(sourceFilePath).ToList());

            var outputLines = File.ReadAllLines(texTemplateFilePath).ToList();

            var startMarker = outputLines.First(f => string.Equals(f.Trim(), _texStartMarker, StringComparison.InvariantCultureIgnoreCase));
            var startMarkerIndex = outputLines.IndexOf(startMarker);

            outputLines.InsertRange(startMarkerIndex, lines);

            File.WriteAllLines(targetFilePath, outputLines);
        }

        private List<string> ParseLines(List<string> rawLines)
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

            var outputLines = lines.Select(f => ParseLine(f));

            var stuff = outputLines.Select(f => f.ToString() + " " + (f is ExpressionEmptyElement ? @"\medskip" : @"\par")).ToList();
            return stuff;
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
                    RawValue = line.Value,
                };

                if (expressionParts[2] is ExpressionArrayOperatorElement)
                    variableDeclaration.ValueExpression = new ExpressionArrayOperatorElement
                    {
                        RawValue = string.Join(" ", expressionParts.Skip(2))
                    };
                else
                    variableDeclaration.ValueExpression = this.ParseTerm(expressionParts.Skip(2).ToList());

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
                var currentPart = parts.ElementAt(i);

                ExpressionElement element;

                if (MatlabKeywords.IsOperator(currentPart))
                {
                    element = new ExpressionOperatorElement
                    {
                        Operator = currentPart,
                        IsUnary = MatlabKeywords.IsUnaryOperator(currentPart)
                    };
                }
                else if (MatlabKeywords.IsValue(currentPart))
                {
                    element = new ExpressionValueElement
                    {
                        Value = decimal.Parse(currentPart)
                    };
                }
                else if (MatlabKeywords.IsVariable(currentPart))
                {
                    element = new ExpressionVariableElement
                    {
                        Name = currentPart
                    };
                }
                else if (MatlabKeywords.IsAssignmentOperator(currentPart))
                {
                    element = new ExpressionAssignmentOperatorElement();
                }
                else if (MatlabKeywords.IsArrayOperator(currentPart))
                {
                    element = new ExpressionArrayOperatorElement();
                }
                else if (MatlabKeywords.IsParenthesisOperators(currentPart))
                {
                    element = new ExpressionParenthesisElement
                    {
                        Type = MatlabKeywords.GetParenthesisType(currentPart)
                    };
                }
                else
                {
                    throw new Exception();
                }

                element.LineReference = index;
                element.RawValue = currentPart;

                expressionParts.Add(element);
            }

            return expressionParts;
        }

        private ExpressionElement ParseTerm(IList<ExpressionElement> expressionParts)
        {
            if (expressionParts.Count == 1 && (expressionParts[0] is ExpressionValueElement || expressionParts[0] is ExpressionVariableElement))
                return expressionParts.First();

            try
            {

                var actual = expressionParts.ToList();

                // first pass
                // collapse paranthesis
                var i = 0;
                while (true)
                {
                    var current = actual.ElementAtOrDefault(i);

                    if (current == null)
                        break;

                    if (current is ExpressionParenthesisElement paranthesisElement)
                    {
                        var closingParenthesis = this.FindClosingParenthesis(actual.Skip(i).ToList());
                        var innerElements = actual.Skip(i + 1).TakeWhile(f => object.ReferenceEquals(f, closingParenthesis) == false).ToList();

                        var previousElement = actual.ElementAtOrDefault(i - 1);
                        ExpressionElement innerExpression;

                        if (previousElement == null || !(previousElement is ExpressionVariableElement))
                        {
                            innerExpression = this.ParseTerm(innerElements);
                        }
                        else
                        {
                            innerExpression = new ExpressionArrayAccesorElement()
                            {
                                LineReference = previousElement.LineReference,
                                RawValue = $"{previousElement}({string.Join(",", innerElements)})"
                            };

                            actual.Remove(previousElement);
                            i--;
                        }

                        actual.Remove(paranthesisElement);

                        foreach (var currentInnerElement in innerElements)
                            actual.Remove(currentInnerElement);

                        actual.Remove(closingParenthesis);

                        actual.Insert(i, innerExpression);
                    }

                    i++;
                }

                // second pass 
                // handle unary operators
                i = 0;
                while (true)
                {
                    var current = actual.ElementAtOrDefault(i);

                    if (current == null)
                        break;

                    if (current is ExpressionOperatorElement operatorExpression && operatorExpression.IsSealed == false && operatorExpression.IsUnary)
                    {
                        var element2 = actual.ElementAt(i + 1);
                        operatorExpression.Operand1 = element2;
                        actual.Remove(operatorExpression.Operand1);

                        operatorExpression.IsSealed = true;
                    }

                    i++;
                }

                // third pass 
                // handle binary operators
                i = actual.Count - 1;
                while (true)
                {
                    var current = actual.ElementAtOrDefault(i);

                    if (current == null)
                        break;

                    if (current is ExpressionOperatorElement operatorExpression && operatorExpression.IsSealed == false && operatorExpression.IsUnary == false)
                    {
                        var element1 = actual.ElementAtOrDefault(i - 1);
                        var element2 = actual.ElementAtOrDefault(i + 1);

                        if (MatlabKeywords.CanBinaryOperatorBeUsedAsUnary(operatorExpression.Operator) &&
                            (element1 == null || element1 is ExpressionOperatorElement))
                        {
                            operatorExpression.Operand1 = element2;
                            operatorExpression.IsUnary = true;
                            actual.Remove(operatorExpression.Operand1);
                        }
                        else
                        {
                            operatorExpression.Operand1 = element1;
                            operatorExpression.Operand2 = element2;

                            actual.Remove(operatorExpression.Operand1);
                            actual.Remove(operatorExpression.Operand2);
                        }

                        operatorExpression.IsSealed = true;
                    }

                    i--;
                }

                var firstElement = actual.First();
                return firstElement as ExpressionOperatorElement;
            }
            catch (Exception e)
            {
                return new ExpressionErrorElement()
                {
                    Exception = e,
                    Children = expressionParts.ToList(),
                    LineReference = expressionParts.First().LineReference
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

            throw new ArgumentException();
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

                if (MatlabKeywords.IsKeyWord(currentChar.ToString()))
                    parts.Add(currentChar.ToString());

                if (currentChar == '%')
                    break;

                i++;
            }

            return parts.Where(f => string.IsNullOrWhiteSpace(f) == false).ToList();
        }
    }

    public class ExpressionElement
    {
        public virtual string RawValue { get; set; }
        public int LineReference { get; set; }
        public bool IsSealed { get; set; }

        public override string ToString()
        {
            return this.RawValue;
        }
    }

    public class ExpressionEmptyElement : ExpressionElement
    {

    }

    public class ExpressionOperatorElement : ExpressionElement
    {
        public string Operator { get; set; }
        public bool IsUnary { get; set; }

        public ExpressionElement Operand1 { get; set; }
        public ExpressionElement Operand2 { get; set; }

        public override string ToString()
        {
            if (Operator == "/")
                return string.Format("\\frac{{{0}}}{{{1}}}", this.Operand1, this.Operand2);

            return this.IsUnary
                ? $"{this.Operator}({this.Operand1})"
                : $"{this.Operand1} {this.Operator} {this.Operand2}";
        }
    }

    public class ExpressionValueElement : ExpressionElement
    {
        public decimal Value { get; set; }
    }

    public class ExpressionVariableDeclarationElement : ExpressionElement
    {
        public ExpressionVariableElement Name { get; set; }

        public ExpressionElement ValueExpression { get; set; }

        public override string ToString()
        {
            return $"$ {this.Name} = {this.ValueExpression} $";
        }
    }

    public class ExpressionVariableElement : ExpressionElement
    {
        public string Name { get; set; }

        public override string ToString()
        {
            var outputName = this.Name;

            foreach (var currentLetter in MatlabKeywords.SpecialChars)
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

    public class ExpressionAssignmentOperatorElement : ExpressionElement
    {

    }

    public class ExpressionArrayOperatorElement : ExpressionElement
    {
        public ExpressionVariableElement Name { get; set; }
    }

    public class ExpressionArrayAccesorElement : ExpressionElement
    {
        public ExpressionArrayOperatorElement Parent { get; set; }
    }

    public class ExpressionParenthesisElement : ExpressionElement
    {
        public ParenthesisType Type { get; set; }
    }

    public class ExpressionErrorElement : ExpressionElement
    {
        public Exception Exception { get; set; }
        public List<ExpressionElement> Children { get; set; }

        public override string ToString()
        {
            return string.Join(" ", this.Children) + " -- Exception: " + this.Exception.Message;
        }
    }

    public enum ParenthesisType
    {
        Open,
        Close
    }
}
