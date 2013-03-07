using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
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

    }
}
