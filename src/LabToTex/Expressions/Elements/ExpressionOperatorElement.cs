namespace LabToTex.Expressions.Elements
{
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
}
