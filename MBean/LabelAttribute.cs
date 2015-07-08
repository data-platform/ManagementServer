using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Management
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public class LabelAttribute : System.Attribute
    {
        internal string text;

        public LabelAttribute(string text)
        {
            this.text = text;
        }
    }
}
