using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Management
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Parameter)]
    public class DescriptionAttribute : System.Attribute
    {
        internal string text;

        public DescriptionAttribute(string text)
        {
            this.text = text;
        }
    }
}
