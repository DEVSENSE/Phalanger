using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.IO;
using System.Net;
using System.Web.Services.Protocols;
using System.Xml.Serialization;
using PHP.Core;

namespace PHP.Library.Soap
{
    /// <summary>
    /// 
    /// </summary>
    internal class WsdlHelper
    {
        /// <summary>
        /// Gets the content of WSDL file
        /// </summary>
        /// <param name="source">Source.</param>
        /// <param name="fullPath">Full path to Wsdl file</param>
        internal static string GetWsdlContent(string source, out string fullPath)
        {
            Uri uri = new Uri(new Uri(ScriptContext.CurrentContext.WorkingDirectory), source);
            fullPath = uri.AbsoluteUri;
            string wsdlSourceValue = String.Empty;

            WebRequest req = WebRequest.Create(uri);
            using (WebResponse result = req.GetResponse())
            {
                Stream ReceiveStream = result.GetResponseStream();
            
                using (StreamReader sr = new StreamReader(ReceiveStream, Configuration.Application.Globalization.PageEncoding))
                {
                    wsdlSourceValue = sr.ReadToEnd();
                }
            }

            return wsdlSourceValue;
        }

        internal static MethodInfo GetMethodBySoapName(string methodName, Type type)
        {
            MethodInfo mi = type.GetMethod(methodName);
            if (mi == null)
            {
                foreach (var methodInfo in type.GetMethods())
                {
                    var at = (SoapDocumentMethodAttribute)methodInfo.GetCustomAttributes(typeof(SoapDocumentMethodAttribute)).SingleOrDefault();
                    if (at != null && at.RequestElementName == methodName)
                    {
                        mi = methodInfo;
                        break;
                    }
                }
            }
            return mi;
        }

        internal static string GetParameterSoapName(ParameterInfo parameter)
        {
            var name = parameter.Name ?? parameter.Member.Name + "Result";
            var at = (XmlElementAttribute)parameter.GetCustomAttribute(typeof(XmlElementAttribute));
            if (at != null && !string.IsNullOrWhiteSpace(at.ElementName))
                name = at.ElementName;
            return name;
        }
    }
}
