using System.Collections.Generic;

namespace LabToTex.Expressions.Elements
{
    public class ExpressionVariableDeclarationElement : ExpressionElement
    {
        public ExpressionVariableElement Name { get; set; }

        public ExpressionElement ValueExpression { get; set; }
    }
}
