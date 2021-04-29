using System.Collections.Generic;

namespace LabToTex.Specifications
{
    public class LatexSpecification
    {
        public string StartTag { get; set; } = "%labtotextstart";

        public string LineBreak { get; set; } = @"\par";
        public string MediumLineBreak { get; set; } = @"\medskip";

        public string DesieredMatrixType { get; set; } = @"bmatrix";

        public string DesiredMultiplication { get; set; } = @"\cdot";

        public List<string> SpecialChars { get; set; } = new List<string>
        {
            "alpha",
            "Alpha",

            "Beta",
            "beta",

            "Gamma",
            "gamma",

            "Delta",
            "delta",

            "Epsilon",
            "epsilon",
            "varepsilon",

            "Lambda",
            "lambda",

            "pi",
            "Pi"
        };
    }
}
