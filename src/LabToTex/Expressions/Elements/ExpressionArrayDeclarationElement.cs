using System.Collections.Generic;
using System.Linq;

namespace LabToTex.Expressions.Elements
{
    public class ExpressionArrayDeclarationElement : ExpressionElement
    {
        public List<ExpressionArrayElementElement> Elements { get; set; }
    }
}
