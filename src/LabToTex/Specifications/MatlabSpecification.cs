using LabToTex.Expressions.Elements;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LabToTex
{
    public class MatlabSpecification
    {
        public List<string> UnaryOperators = new List<string>
        {
            "abs",
            "acos",
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

        public List<string> BinaryOperators = new List<string>
        {
            "*",
            "^",
            "/"
        };

        public List<string> DualPurposeOperators = new List<string>
        {
            "+",
            "-"
        };

        public List<string> AssignmentOperators = new List<string>
        {
            "="
        };

        public List<string> ArrayDeclarationOperators = new List<string>
        {
            "[",
            "]"
        };

        public List<string> ParenthesisOperators = new List<string>
        {
            "(",
            ")"
        };

        public List<string> EndStatementOperators = new List<string>
        {
            ";"
        };


        public List<string> AnnoymousFunctionOperators = new List<string>
        {
            "@"
        };

        public bool IsOperator(string @string) => IsBinaryOperator(@string) || IsUnaryOperator(@string);
        public bool IsBinaryOperator(string @string) => BinaryOperators.Union(DualPurposeOperators).Any(f => f == @string);
        public bool IsUnaryOperator(string @string) => UnaryOperators.Any(f => f == @string);
        public bool CanBinaryOperatorBeUsedAsUnary(string @string) => DualPurposeOperators.Any(f => f == @string);
        public bool IsValue(string @string) => @string.Any(f => char.IsDigit(f) == false && f != '.') == false;
        public bool IsVariable(string @string) => IsValue(@string) == false && @string.All(f => IsKeyWord(f.ToString()) == false);
        public bool IsAssignmentOperator(string @string) => AssignmentOperators.Any(f => f == @string);
        public bool IsArrayOperator(string @string) => ArrayDeclarationOperators.Any(f => f == @string);
        public bool IsParenthesisOperators(string @string) => ParenthesisOperators.Any(f => f == @string);
        public ParenthesisType GetParenthesisType(string @string) => @string == ")" ? ParenthesisType.Close : ParenthesisType.Open;
        public bool IsKeyWord(string @string) => IsOperator(@string) || IsAssignmentOperator(@string) || IsArrayOperator(@string) || IsParenthesisOperators(@string) || IsEndStatement(@string) || IsAnnoymousFunction(@string);
        public bool IsEndStatement(string @string) => EndStatementOperators.Any(f => f == @string);
        public bool IsAnnoymousFunction(string @string) => AnnoymousFunctionOperators.Any(f => f == @string);
    }
}
