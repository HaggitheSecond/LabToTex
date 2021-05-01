using System.Collections.Generic;
using System.Linq;

namespace LabToTex.Expressions.Elements
{
    public class ExpressionAnnoynmousFunctionElement : ExpressionElement
    {
        public List<ExpressionVariableElement> Parameters { get; set; }
        public ExpressionElement Expression { get; set; }

        public override IEnumerable<ExpressionElement> GetChildren()
        {
            var children = this.Parameters.Cast<ExpressionElement>().ToList();
            children.Add(this.Expression);
            return children;
        }
    }
}
