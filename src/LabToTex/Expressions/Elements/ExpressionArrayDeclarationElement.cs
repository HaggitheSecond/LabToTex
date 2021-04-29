using System.Collections.Generic;
using System.Linq;

namespace LabToTex.Expressions.Elements
{
    public class ExpressionArrayDeclarationElement : ExpressionElement
    {
        public List<ExpressionArrayElementElement> Elements { get; set; }

        public override IEnumerable<ExpressionElement> GetChildren()
        {
            var elements = new List<ExpressionElement>();

            if (this.Elements != null)
                elements.AddRange(this.Elements);

            return elements;
        }
    }
}
