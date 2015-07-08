using CPI.DirectoryServices;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Management
{
    [Serializable]
    public class MBean : IMBean
    {
        private readonly Type t;

        public virtual string GetLabel(PropertyInfo info)
        {
            LabelAttribute attr = Attribute.GetCustomAttribute(info, typeof(LabelAttribute), true) as LabelAttribute;
            return attr == null ? null : attr.text;
        }
        public virtual string GetLabel(ParameterInfo info)
        {
            LabelAttribute attr = Attribute.GetCustomAttribute(info, typeof(LabelAttribute), true) as LabelAttribute;
            return attr == null ? null : attr.text;
        }
        public virtual string GetDescription()
        {
            DescriptionAttribute attr = Attribute.GetCustomAttribute(GetType(), typeof(DescriptionAttribute), true) as DescriptionAttribute;
            if (attr != null)
            {
                return attr.text;
            }
            attr = Attribute.GetCustomAttribute(t, typeof(DescriptionAttribute), true) as DescriptionAttribute;
            if (attr != null)
            {
                return attr.text;
            }
            return null;
        }
        public virtual string GetDescription(MemberInfo info)
        {
            DescriptionAttribute attr = Attribute.GetCustomAttribute(info, typeof(DescriptionAttribute), true) as DescriptionAttribute;
            return attr == null ? null : attr.text;
        }
        public virtual string GetDescription(ParameterInfo info)
        {
            DescriptionAttribute attr = Attribute.GetCustomAttribute(info, typeof(DescriptionAttribute), true) as DescriptionAttribute;
            return attr == null ? null : attr.text;
        }
        public virtual DN GetMBeanName()
        {
            MBeanNameAttribute attr = Attribute.GetCustomAttribute(GetType(), typeof(MBeanNameAttribute)) as MBeanNameAttribute;
            if (attr != null)
            {
                return attr.dn;
            }
            else
            {
                attr = Attribute.GetCustomAttribute(t, typeof(MBeanNameAttribute)) as MBeanNameAttribute;
                if (attr != null)
                {
                    return attr.dn;
                }
            }
            return null;
        }
        public virtual void Serialize(string id, TextWriter writer)
        {
            writer.WriteLine("{");
            writer.WriteLine("\"id\": " + JsonConvert.ToString(id) + ",");
            writer.WriteLine("\"mbeanName\": " + JsonConvert.ToString(GetMBeanName().ToString()) + ",");
            writer.Write("\"description\": " + JsonConvert.ToString(GetDescription()));

            List<PropertyInfo> properties = new List<PropertyInfo>();
            List<MethodInfo> methods = new List<MethodInfo>();
            foreach (MemberInfo info in t.GetMembers())
            {
                if (info is PropertyInfo)
                {
                    properties.Add(info as PropertyInfo);
                }
                else if (info is MethodInfo)
                {
                    if (info.Name.StartsWith("get_") || info.Name.StartsWith("set_"))
                    {
                        if (t.GetProperty(info.Name.Substring(4)) != null)
                        {
                            // Not a user method, but a property accessor
                            continue;
                        }
                    }
                    methods.Add(info as MethodInfo);
                }
            }
            if (properties.Count > 0)
            {
                writer.WriteLine(",\"properties\": [");
                int i = 0;
                foreach (PropertyInfo info in properties)
                {
                    if (i++ > 0)
                    {
                        writer.WriteLine(",");
                    }
                    writer.WriteLine("{");
                    writer.Write("\"name\": " + JsonConvert.ToString(info.Name));
                    writer.Write(",\n\"writable\": " + JsonConvert.ToString(info.CanWrite));
                    writer.Write(",\n\"type\": " + JsonConvert.ToString(info.PropertyType.ToString()));
                    string label = GetLabel(info);
                    if (label != null)
                    {
                        writer.Write(",\n\"label\": " + JsonConvert.ToString(label));
                    }
                    string desc = GetDescription(info);
                    if (desc != null)
                    {
                        writer.Write(",\n\"description\": " + JsonConvert.ToString(desc));
                    }
                    if (info.CanRead)
                    {
                        PropertyInfo propertyInfo = GetType().GetProperty(info.Name);
                        var val = propertyInfo.GetValue(this);
                        if (val != null && val.GetType().IsArray)
                        {
                            writer.Write(",\n\"value\": " + JsonConvert.ToString(string.Join("\n", val as object[])));
                        }
                        else
                        {
                            writer.Write(",\n\"value\": " + JsonConvert.ToString(val));
                        }
                    }
                    writer.Write("}");
                }
                writer.Write("]");
            }

            if (methods.Count > 0)
            {
                writer.Write(",\n\"methods\": [");
                int i = 0;
                foreach (MethodInfo info in methods)
                {
                    if (i++ > 0)
                    {
                        writer.WriteLine(",");
                    }
                    writer.WriteLine("{");
                    writer.Write("\"name\": " + JsonConvert.ToString(info.Name));
                    writer.Write(",\n\"returnType\": " + JsonConvert.ToString(info.ReturnType.ToString()));
                    writer.Write(",\n\"signature\": " + JsonConvert.ToString(GetMethodSignature(info)));
                    string desc = GetDescription(info);
                    if (desc != null)
                    {
                        writer.Write(",\n\"description\": " + JsonConvert.ToString(desc));
                    }

                    ParameterInfo[] paramInfos = info.GetParameters();
                    if (paramInfos.Length > 0)
                    {
                        writer.Write(",\n\"parameters\": [");
                        int j = 0;
                        foreach (ParameterInfo paramInfo in paramInfos)
                        {
                            if (j++ > 0)
                            {
                                writer.WriteLine(",");
                            }
                            writer.WriteLine("{");
                            writer.Write("\"name\": " + JsonConvert.ToString(paramInfo.Name));
                            writer.Write(",\n\"type\": " + JsonConvert.ToString(paramInfo.ParameterType.ToString()));
                            string label = GetLabel(paramInfo);
                            if (label != null)
                            {
                                writer.Write(",\n\"label\": " + JsonConvert.ToString(label));
                            }
                            desc = GetDescription(paramInfo);
                            if (desc != null)
                            {
                                writer.Write(",\n\"description\": " + JsonConvert.ToString(desc));
                            }
                            writer.Write("}");
                        }
                        writer.WriteLine("]");
                    }
                    writer.WriteLine("}");
                }
                writer.WriteLine("]");
            }
            writer.WriteLine("}");
        }
        public virtual void Register()
        {
            Node.Register(GetMBeanName(), this);
        }
        public virtual void Unregister()
        {
            Node.Unregister(GetMBeanName());
        }

        private static string GetMethodSignature(MethodInfo info)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(info.ReturnType.ToString());
            foreach (ParameterInfo param in info.GetParameters())
            {
                sb.AppendLine(param.ParameterType.ToString());
            }
            using (var md5 = MD5.Create())
            {
                return new Guid(md5.ComputeHash(Encoding.Default.GetBytes(sb.ToString()))).ToString();
            }
        }
        public virtual MethodInfo FindMethod(string name, string signature)
        {
            foreach (MethodInfo info in GetType().GetMethods())
            {
                if (info.Name == name && GetMethodSignature(info) == signature)
                {
                    return info;
                }
            }
            return null;
        }
        public virtual void SetProperty(string name, string value)
        {
            PropertyInfo propertyInfo = this.GetType().GetProperty(name);
            propertyInfo.SetValue(this, Convert.ChangeType(value, propertyInfo.PropertyType), null);
        }
        public virtual object InvokeMethod(MethodInfo info, NameValueCollection parameters)
        {
            if (info.GetParameters().Length == 0)
            {
                return info.Invoke(this, null);
            }
            else
            {
                List<object> par = new List<object>();
                foreach (ParameterInfo paramInfo in info.GetParameters())
                {
                    string val = parameters[paramInfo.Name];
                    if (val == null)
                    {
                        par.Add(null);
                    }
                    else
                    {
                        par.Add(Convert.ChangeType(val, paramInfo.ParameterType));
                    }
                }
                //if (methodInfo.ReturnType == typeof(void))
                //{
                //    methodInfo.Invoke(this, par.ToArray());
                //    return null;
                //}
                //else
                //{
                    return info.Invoke(this, par.ToArray());
                //}
            }
        }

        public MBean(Type t)
        {
            this.t = t;
        }
    }
}
