using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LabToTex
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new MatlabParser2();

            parser.Parse(@"C:\Users\Yannik\Desktop\Unistuff\latex\labtotex\messtechnik_ubeung5.m",
                @"C:\Users\Yannik\Desktop\Unistuff\latex\labtotex\test01.tex",
                @"C:\Users\Yannik\Desktop\Unistuff\latex\helloworld.tex");
        }
    }

    public class MatlabParser
    {
        private const string _texStartMarker = "%labtotextstart";

        public void EnsureValidFile(string filePath)
        {
            if (File.Exists(filePath) == false)
                throw new FileNotFoundException($"File with path '{filePath}' not found:");
        }

        public void Parse(string sourceFilePath, string targetFilePath, string texTemplateFilePath)
        {
            this.EnsureValidFile(sourceFilePath);

            var lines = this.ParseLines(File.ReadAllLines(sourceFilePath).ToList());

            var outputLines = File.ReadAllLines(texTemplateFilePath).ToList();

            var startMarker = outputLines.First(f => string.Equals(f.Trim(), _texStartMarker, StringComparison.InvariantCultureIgnoreCase));
            var startMarkerIndex = outputLines.IndexOf(startMarker);

            outputLines.InsertRange(startMarkerIndex, lines);

            File.WriteAllLines(targetFilePath, outputLines);
        }

        private List<string> ParseLines(List<string> lines)
        {
            var outputLines = new List<string>();

            foreach (var currentLine in lines)
            {
                outputLines.Add(this.ParseLine(currentLine));
            }

            return outputLines;
        }

        private string _endOfLine = @"\par";

        private string ParseLine(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return @"\medskip";

            if (input.StartsWith('%'))
                return input.Replace("%", "") + this._endOfLine;

            if (input.Contains("="))
                return this.ParseFormula(input) + this._endOfLine;

            return input + _endOfLine;
        }

        private string ParseFormula(string line)
        {
            line = line.Replace(";", "").Replace(" ", "");

            return "$" + this.ParseFormulaRightSide(line) + "$" + this._endOfLine;
        }

        private string ParseFormulaRightSide(string line)
        {
            if (line.TrimStart().StartsWith("["))
                return line;

            return this.ParseFormlaBlock(line);
        }

        private string ParseFormlaBlock(string block)
        {
            var parts = this.ExtractParts(block);

            return string.Join(' ', parts.Select(f => f.ToString()));
        }

        private IList<FormulaBlock> ExtractParts(string line)
        {
            var blockParts = new List<string>();

            var i = 0;
            while (i < line.Length)
            {
                var currentChar = line[i];

                if (char.IsLetterOrDigit(currentChar))
                {
                    var wholethingy = string.Join("", line[i..].TakeWhile(f => char.IsLetterOrDigit(f) || f == '.' || f == '_'));
                    i += wholethingy.Length;
                    blockParts.Add(wholethingy);
                    continue;
                }

                if (this.IsOperator(currentChar.ToString()))
                    blockParts.Add(currentChar.ToString());

                if (currentChar == '%')
                    break;

                i++;
            }

            var parts = new List<FormulaBlock>();

            foreach (var currentBlockPart in blockParts)
            {
                var block = new FormulaBlock(this)
                {
                    Value = currentBlockPart
                };

                if (this.IsValue(currentBlockPart))
                {
                    block.Type = FormulaBlockType.Value;
                }
                else if (this.IsVariable(currentBlockPart))
                {
                    block.Type = FormulaBlockType.Variable;
                }
                else if (this.IsOperator(currentBlockPart))
                {

                }
                else
                {
                    block.Type = FormulaBlockType.Other;
                }

                parts.Add(block);
            }

            return parts;
        }

        private class FormulaBlock
        {
            private readonly MatlabParser _parser;

            public string Value { get; set; }

            public FormulaBlockType Type { get; set; }

            public FormulaBlock(MatlabParser parser)
            {
                this._parser = parser;
            }

            public override string ToString()
            {
                switch (this.Type)
                {
                    case FormulaBlockType.Operator:
                        return this.Value;
                    case FormulaBlockType.Variable:
                        return this._parser.ParseVariableName(this.Value);
                    case FormulaBlockType.Other:
                    case FormulaBlockType.Value:
                    default:
                        return this.Value;
                }
            }
        }

        private enum FormulaBlockType
        {
            Operator,
            Value,
            Variable,
            Other
        }

        private bool IsOperator(string @string) => this._nonStickyOperators.Any(f => f == @string);
        private bool IsValue(string @string) => @string.Any(f => char.IsDigit(f) == false && f != '.') == false;
        private bool IsVariable(string @string) => this.IsValue(@string) == false && @string.All(f => this.IsOperator(f.ToString()) == false);

        private List<string> _nonStickyOperators = new List<string>
        {
            "+",
            "-",
            "*",
            "^",
            "/",
            "="
        };

        private string ParseVariableName(string variableName)
        {
            foreach (var currentLetter in this._specialChars)
            {
                variableName = variableName.Replace(currentLetter, "\\" + currentLetter + " ");
            }

            if (variableName.Contains("_") == false)
                return variableName;

            var parts = variableName.Split("_").ToList();

            return parts[0] + "_{" + string.Join(',', parts.Skip(1)) + "}";
        }

        private List<string> _specialChars = new List<string>
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
            "tan"
        };

        public static List<string> BinaryOperators = new List<string>
        {
            "+",
            "-",
            "*",
            "^",
            "/"
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
        public static bool IsBinaryOperator(string @string) => BinaryOperators.Any(f => f == @string);
        public static bool IsUnaryOperator(string @string) => UnaryOperators.Any(f => f == @string);
        public static bool IsValue(string @string) => @string.Any(f => char.IsDigit(f) == false && f != '.') == false;
        public static bool IsVariable(string @string) => IsValue(@string) == false && @string.All(f => IsKeyWord(f.ToString()) == false);
        public static bool IsAssignmentOperator(string @string) => AssignmentOperators.Any(f => f == @string);
        public static bool IsArrayOperator(string @string) => ArrayDeclarationOperators.Any(f => f == @string);
        public static bool IsParenthesisOperators(string @string) => ParenthesisOperators.Any(f => f == @string);
        public static ParenthesisType GetParenthesisType(string @string) => @string == ")" ? ParenthesisType.Close : ParenthesisType.Open;
        public static bool IsKeyWord(string @string) => IsOperator(@string) || IsAssignmentOperator(@string) || IsArrayOperator(@string) || IsParenthesisOperators(@string);
    }

    public class MatlabParser2
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

        private List<string> ParseLines(List<string> lines)
        {
            var outputLines = new List<ExpressionElement>();

            for (var i = 0; i < lines.Count; i++)
            {
                var expression = this.ParseLine(lines[i], i + 1);
                outputLines.Add(expression);
            }

            var stuff = outputLines.Select(f => f.ToString() + (f is ExpressionEmptyElement ? @"\medskip" : @"\par")).ToList();
            return stuff;
        }

        private ExpressionElement ParseLine(string line, int index)
        {
            var parts = this.ExtractLineParts(line);

            if (parts.Any() == false)
                return new ExpressionEmptyElement()
                {
                    LineReference = index
                };

            var expressionParts = this.ParseExpressionElements(parts, index);

            if (expressionParts.Count == 1)
                return new ExpressionElement
                {
                    LineReference = index,
                    RawValue = expressionParts.First().RawValue
                };

            if (expressionParts[0] is ExpressionVariableElement variableElement && expressionParts[1] is ExpressionAssignmentOperatorElement)
                return new ExpressionVariableDeclarationElement
                {
                    LineReference = index,
                    Name = variableElement,
                    RawValue = line,
                    ValueExpression = this.ParseTerm(expressionParts.Skip(2).ToList())
                };

            return new ExpressionElement
            {
                LineReference = index,
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
            if (expressionParts.Count == 1 && (expressionParts[0] is ExpressionValueElement || expressionParts[1] is ExpressionVariableElement))
                return expressionParts.First();

            var actual = expressionParts.ToList();

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
                    var innerExpression = this.ParseTerm(innerElements);

                    actual.Remove(paranthesisElement);

                    foreach (var currentInnerElement in innerElements)
                        actual.Remove(currentInnerElement);

                    actual.Remove(closingParenthesis);

                    actual.Insert(i, innerExpression);
                }

                i++;
            }

            i = 0;
            while (true)
            {
                var current = actual.ElementAtOrDefault(i);

                if (current == null)
                    break;

                if (current is ExpressionOperatorElement operatorExpression)
                {
                    if (operatorExpression.IsUnary)
                    {
                        var element2 = actual.ElementAt(i + 1);
                        operatorExpression.Operand2 = element2;
                        actual.Remove(operatorExpression.Operand2);
                        i++;
                    }
                    else
                    {
                        var element1 = actual.ElementAt(i - 1);
                        var element2 = actual.ElementAt(i + 1);

                        operatorExpression.Operand1 = element1;
                        operatorExpression.Operand2 = element2;

                        actual.Remove(operatorExpression.Operand1);
                        actual.Remove(operatorExpression.Operand2);
                    }
                }
                else
                {
                    i++;
                }
            }

            var firstElement = actual.First();

            return firstElement as ExpressionOperatorElement;
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
            return this.IsUnary
                ? $"{this.Operator} {this.Operand1}"
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
            return $"{this.Name} = {this.ValueExpression}";
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

    }

    public class ExpressionParenthesisElement : ExpressionElement
    {
        public ParenthesisType Type { get; set; }
    }

    public enum ParenthesisType
    {
        Open,
        Close
    }
}
