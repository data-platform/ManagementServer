using CPI.DirectoryServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Management
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class MBeanNameAttribute : Attribute
    {
        internal DN dn;

        public MBeanNameAttribute(string dn)
        {
            this.dn = new DN(dn);
        }
    }
}
