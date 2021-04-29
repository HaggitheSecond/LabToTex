using System.Collections.Generic;

namespace LabToTex.Expressions.Elements
{
    public class ExpressionVariableDeclarationElement : ExpressionElement
    {
        public ExpressionVariableElement Name { get; set; }

        public ExpressionElement ValueExpression { get; set; }

        public override IEnumerable<ExpressionElement> GetChildren()
        {
            yield return this.ValueExpression;
            yield return this.Name;
        }
    }
}
