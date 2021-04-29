namespace LabToTex.Expressions.Elements
{
    public class ExpressionVariableDeclarationElement : ExpressionElement
    {
        public ExpressionVariableElement Name { get; set; }

        public ExpressionElement ValueExpression { get; set; }

        public override string ToString()
        {
            return $"$ {this.Name} = {this.ValueExpression} $";
        }
    }
}
