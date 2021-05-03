using System.Collections.Generic;

namespace LabToTex.Expressions.Elements
{
    public class ExpressionArrayAccesorElement : ExpressionElement
    {
        public ExpressionVariableElement Name { get; set; }

        public List<ExpressionElement> Indexes { get; set; }
    }
}
