
using Microsoft.Win32;
using Newtonsoft.Json;
using NHttp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Management
{

    public class Server : IDisposable
    {
        private HttpServer httpServer;

        public IPEndPoint EndPoint
        {
            get
            {
                return httpServer.EndPoint;
            }
        }

        public void Start()
        {
            httpServer.Start();
        }
        public void Stop()
        {
            httpServer.Stop();
        }
        public void Dispose()
        {
            httpServer.Dispose();
            httpServer = null;
        }

        public Server(int port)
        {
            httpServer = new HttpServer();
            httpServer.EndPoint = new IPEndPoint(new IPAddress(new byte[] { 127, 0, 0, 1 }), port);
            httpServer.RequestReceived += (s, e) =>
            {
                string name = "ManagementServer" + e.Request.Path.Replace('/', '.');
                ManifestResourceInfo resourceInfo = Assembly.GetExecutingAssembly().GetManifestResourceInfo(name);
                if (resourceInfo != null)
                {
                    e.Response.ContentType = MimeTypes.GetMimeType(new FileInfo(name));
                    using (Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
                    {
                        resourceStream.CopyTo(e.Response.OutputStream);
                    }
                    return;
                }

                using (var writer = new StreamWriter(e.Response.OutputStream))
                {
                    if (e.Request.Path == "/")
                    {
                        e.Response.Redirect("/index.html");
                    }
                    else if (e.Request.Path == "/localization.js")
                    {
                        name = "ManagementServer.Scripts.lang-" + CultureInfo.CurrentCulture.ThreeLetterISOLanguageName + ".js";
                        resourceInfo = Assembly.GetExecutingAssembly().GetManifestResourceInfo(name);
                        if (resourceInfo != null)
                        {
                            e.Response.ContentType = MimeTypes.GetMimeType(new FileInfo(name));
                            using (Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
                            {
                                resourceStream.CopyTo(e.Response.OutputStream);
                            }
                            return;
                        }
                        else
                        {
                            name = "ManagementServer.Scripts.lang-neutral.js";
                            e.Response.ContentType = MimeTypes.GetMimeType(new FileInfo(name));
                            using (Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
                            {
                                resourceStream.CopyTo(e.Response.OutputStream);
                            }
                            return;
                        }
                    }
                    else if (e.Request.Path == "/tree")
                    {
                        e.Response.ContentType = "application/json";
                        Node.SerializeTree(writer);
                    }
                    else if (e.Request.Path == "/mbean")
                    {
                        string id = e.Request.Params["id"];
                        IMBean obj = Node.GetObject(id);
                        if (obj == null)
                        {
                            e.Response.StatusCode = 404;
                            e.Response.StatusDescription = "Object not found on this server";
                        }
                        else
                        {
                            e.Response.ContentType = "application/json";
                            obj.Serialize(id, writer);
                        }
                    }
                    else if (e.Request.Path == "/set-prop")
                    {
                        try
                        {
                            string id = e.Request.Params["id"];
                            string propName = e.Request.Params["propName"];
                            string value = e.Request.Params["val"];

                            IMBean obj = Node.GetObject(id);
                            obj.SetProperty(propName, value);

                            e.Response.ContentType = "application/json";
                            writer.WriteLine("{ \"result\": " + JsonConvert.ToString("ok")
                                + ", \"message\": " + JsonConvert.ToString(ManagementServer.strings.PROP_SET_SUCCESSFULLY) + "}");
                        }
                        catch(Exception ex)
                        {
                            e.Response.ContentType = "application/json";
                            writer.WriteLine("{ \"result\": " + JsonConvert.ToString("error")
                                + ", \"message\": " + JsonConvert.ToString(ex.Message) + "}");
                        }
                    }
                    else if (e.Request.Path == "/invoke")
                    {
                        try
                        {
                            string id = e.Request.Params["object-id"];
                            string methodName = e.Request.Params["method-name"];
                            string methodSignature = e.Request.Params["method-signature"];

                            IMBean obj = Node.GetObject(id);
                            MethodInfo methodInfo = obj.FindMethod(methodName, methodSignature);
                            if (methodInfo == null)
                            {
                                throw new ArgumentException(ManagementServer.strings.ERR_METHOD_NOT_FOUND);
                            }
                            object val = obj.InvokeMethod(methodInfo, e.Request.Params);

                            e.Response.ContentType = "application/json";
                            if (methodInfo.ReturnType == typeof(void))
                            {
                                writer.WriteLine("{ \"result\": " + JsonConvert.ToString("ok")
                                    + ", \"message\": " + JsonConvert.ToString(ManagementServer.strings.VOID_INVOKED_SUCCESSFULLY)
                                    + "}");
                            }
                            else
                            {
                                writer.WriteLine("{ \"result\": " + JsonConvert.ToString("ok")
                                    + ", \"message\": " + JsonConvert.ToString(
                                        string.Format(ManagementServer.strings.METHOD_INVOKED_SUCCESSFULLY, (val == null ? "null" : val.ToString()))
                                    ) + "}");
                            }
                        }
                        catch (Exception ex)
                        {
                            e.Response.ContentType = "application/json";
                            writer.WriteLine("{ \"result\": " + JsonConvert.ToString("error")
                                + ", \"message\": " + JsonConvert.ToString(ex.Message) + "}");
                        }
                    }
                    else
                    {
                        e.Response.StatusCode = 404;
                        e.Response.StatusDescription = "File not found on this server";
                    }
                }
            };
        }
    }

    internal static class MimeTypes
    {
        private static Dictionary<string, string> MimeMap;

        public static string GetMimeType(FileInfo fileInfo)
        {
            string ext = fileInfo.Extension.ToLower();
            if (MimeMap.ContainsKey(ext))
            {
                return MimeMap[ext];
            }
            string mimeType = "application/unknown";
            RegistryKey regKey = Registry.ClassesRoot.OpenSubKey(ext);
            if (regKey != null)
            {
                object contentType = regKey.GetValue("Content Type");
                if (contentType != null)
                {
                    mimeType = contentType.ToString();
                }
            }
            MimeMap[ext] = mimeType;
            return mimeType;
        }

        static MimeTypes()
        {
            MimeMap = new Dictionary<string, string>();
            MimeMap.Add("html", "text/html");
            MimeMap.Add(".htm", "text/html");
            MimeMap.Add(".js", "application/javascript");
            MimeMap.Add(".png", "image/png");
            MimeMap.Add(".css", "text/css");
        }
    }
}
