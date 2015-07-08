using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Management
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ServiceNameAttribute : System.Attribute
    {
        private string name;

        public ServiceNameAttribute(string name)
        {
            this.name = name;
        }
    }
}
