using System.Collections.Generic;

namespace LabToTex.Expressions.Elements
{
    public class ExpressionArrayElementElement : ExpressionElement
    {
        public ExpressionElement Value { get; set; }

        public ArrayIndex Index { get; set; }

        public override IEnumerable<ExpressionElement> GetChildren()
        {
            yield return this.Value;
        }
    }

    public class ArrayIndex
    {
        public int XIndex { get; set; }
        public int? YIndex { get; set; }
    }
}
