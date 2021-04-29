using System.Collections.Generic;

namespace LabToTex.Expressions.Elements
{
    public class ExpressionOperatorElement : ExpressionElement
    {
        public string Operator { get; set; }
        public bool IsUnary { get; set; }

        public ExpressionElement Operand1 { get; set; }
        public ExpressionElement Operand2 { get; set; }

        public override IEnumerable<ExpressionElement> GetChildren()
        {
            yield return this.Operand1;
            yield return this.Operand2;
        }
    }
}
