namespace LabToTex.Expressions.Elements
{
    public class ExpressionParenthesisElement : ExpressionElement
    {
        public ParenthesisType Type { get; set; }
    }

    public enum ParenthesisType
    {
        Open,
        Close
    }
}
