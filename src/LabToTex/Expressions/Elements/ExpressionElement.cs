using System.Collections.Generic;

namespace LabToTex.Expressions.Elements
{
    public abstract class ExpressionElement
    {
        public virtual string RawValue { get; set; } = "";
        public int LineReference { get; set; }
        public bool IsSealed { get; set; }

        public ExpressionElement Parent { get; set; }

        public override string ToString()
        {
            return this.RawValue + " | " + this.GetType();
        }
    }
}
