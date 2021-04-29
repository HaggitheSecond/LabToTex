using System;
using System.Collections.Generic;

namespace LabToTex.Expressions.Elements
{
    public class ExpressionErrorElement : ExpressionElement
    {
        public Exception Exception { get; set; }
        public List<ExpressionElement> Children { get; set; }

        public override string ToString()
        {
            return string.Join(" ", this.Children) + " -- Exception: " + this.Exception.Message;
        }
    }
}
