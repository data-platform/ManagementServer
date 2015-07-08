using CPI.DirectoryServices;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Management
{
    public interface IMBean
    {
        void Serialize(string id, TextWriter writer);
        void SetProperty(string name, string value);
        MethodInfo FindMethod(string name, string signature);
        object InvokeMethod(MethodInfo info, NameValueCollection parameters);
    }

    internal class Node
    {
        public string Id
        {
            get;
            private set;
        }
        public string Text
        {
            get;
            private set;
        }
        public Node Parent
        {
            get;
            private set;
        }
        public SortedList<string,Node> Children
        {
            get;
            private set;
        }
        public IMBean Payload
        {
            get;
            set;
        }

        private void Serialize(TextWriter writer)
        {
            writer.WriteLine("{");
            writer.WriteLine("\"id\": " + JsonConvert.ToString(Id) + ",");
            writer.WriteLine("\"text\": " + JsonConvert.ToString(Text) + ",");

            if (Children.Count > 0)
            {
                writer.WriteLine("\"icon\": " + JsonConvert.ToString("glyphicon glyphicon-folder-open") + ",");
                writer.WriteLine("\"nodes\": [");
                int i = 0;
                foreach (Node child in Children.Values)
                {
                    if (i++ > 0)
                    {
                        writer.WriteLine(",");
                    }
                    child.Serialize(writer);
                }
                writer.WriteLine("],");
            }
            else
            {
                writer.WriteLine("\"icon\": " + JsonConvert.ToString("glyphicon glyphicon-list-alt") + ",");
            }
            writer.WriteLine("\"tags\": [");
            if (Payload != null)
            {
                writer.WriteLine(JsonConvert.ToString("mbean"));
            }
            writer.WriteLine("]");
            writer.WriteLine("}");
        }
        private void Remove()
        {
            foreach (Node node in Children.Values)
            {
                node.Remove();
            }
            Nodes.Remove(Id);
        }

        private Node(string text, Node parent)
        {
            this.Id = Guid.NewGuid().ToString();
            this.Text = text;
            this.Parent = parent;
            this.Children = new SortedList<string, Node>();
        }

        private static Node Root
        {
            get;
            set;
        }
        private static Dictionary<string, Node> Nodes
        {
            get;
            set;
        }
        static Node()
        {
            Root = new Node("Root", null);
            Root.Id = "#";
            Nodes = new Dictionary<string, Node>();
        }

        public static Node Register(DN dn, IMBean obj)
        {
            Node node = Node.Root;
            lock (Nodes)
            {
                foreach (RDN rdn in dn.RDNs)
                {
                    string nodeText = null;
                    foreach (RDNComponent comp in rdn.Components)
                    {
                        nodeText = comp.ComponentType + "=" + comp.ComponentValue;
                        break;
                    }
                    if (node.Children.ContainsKey(nodeText))
                    {
                        node = node.Children[nodeText];
                    }
                    else
                    {
                        Node newNode = new Node(nodeText, node);
                        node.Children.Add(newNode.Text, newNode);
                        Node.Nodes.Add(newNode.Id, newNode);
                        node = newNode;
                    }
                }
            }
            node.Payload = obj;
            return node;
        }
        public static void Unregister(DN dn)
        {
            lock (Nodes)
            {
                Node node = Node.Root;
                foreach (RDN rdn in dn.RDNs)
                {
                    string nodeText = null;
                    foreach (RDNComponent comp in rdn.Components)
                    {
                        nodeText = comp.ComponentType + "=" + comp.ComponentValue;
                        break;
                    }
                    if (node.Children.ContainsKey(nodeText))
                    {
                        node = node.Children[nodeText];
                    }
                    else
                    {
                        return; // Not found
                    }
                }

                // Remove the node
                node.Remove();
            }
        }
        public static void SerializeTree(TextWriter writer)
        {
            writer.WriteLine("[");
            foreach (Node node in Root.Children.Values)
            {
                node.Serialize(writer);
            }
            writer.WriteLine("]");
        }
        public static IMBean GetObject(string id)
        {
            try
            {
                Node node = Nodes[id];
                return node == null ? null : node.Payload;
            }
            catch
            {
                return null;
            }
        }
    }
}
