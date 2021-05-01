using System.Collections.Generic;

namespace LabToTex.Expressions.Elements
{
    public class ExpressionFunctionCallElement : ExpressionElement
    {
        public List<ExpressionElement> Parameters { get; set; }

        public ExpressionAnnoynmousFunctionElement Function { get; set; }
    }
}
