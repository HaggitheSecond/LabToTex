namespace LabToTex.Expressions.Elements
{
    public class ExpressionElement
    {
        public virtual string RawValue { get; set; }
        public int LineReference { get; set; }
        public bool IsSealed { get; set; }

        public override string ToString()
        {
            return RawValue;
        }
    }
}
