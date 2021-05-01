using System.Collections.Generic;
using System.Linq;

namespace LabToTex.Expressions.Elements
{
    public class ExpressionAnnoynmousFunctionElement : ExpressionElement
    {
        public List<ExpressionVariableElement> Parameters { get; set; }
        public ExpressionElement Expression { get; set; }
    }
}
