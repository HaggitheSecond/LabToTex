using LabToTex.Expressions.Elements;
using System.Collections.Generic;
using System.Linq;

namespace LabToTex.Expressions.Parsers
{
    public class ExpressionFile
    {
        public List<ExpressionElement> Expressions { get; set; }

        public ExpressionVariableDeclarationElement TryFindVariableDeclarationByName(string name)
        {
            var element = this.Expressions.OfType<ExpressionVariableDeclarationElement>().FirstOrDefault(f => f.Name.Name == name);
            return element;
        }
    }
}
