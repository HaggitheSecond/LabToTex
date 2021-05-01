namespace LabToTex.Common
{
    public class Line
    {
        public string Value { get; set; }
        public int Index { get; set; }

        public override string ToString()
        {
            return $"{this.Index}: {this.Value}";
        }
    }
}
