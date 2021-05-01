using System.Collections.Generic;

namespace LabToTex.Expressions.Elements
{
    public class ExpressionOperatorElement : ExpressionElement
    {
        public string Operator { get; set; }
        public OperatorType Type { get; set; }

        public ExpressionElement Operand1 { get; set; }
        public ExpressionElement Operand2 { get; set; }
    }

    public enum OperatorType
    {
        Binary,
        Unary,
        BinaryAsUnary
    }
}
