using LabToTex.Expressions.Elements;
using System.Collections.Generic;
using System.Linq;

namespace LabToTex
{
    public static class MatlabSpecification
    {
        public static List<string> UnaryOperators = new List<string>
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
}
