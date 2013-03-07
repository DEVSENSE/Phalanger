using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PHP.Library.Soap
{
    /// <summary>
    /// Summary description for CodeConstants.
    /// </summary>
    internal struct CodeConstants
    {
        internal const string BEGIN = "Begin";
        internal const string END = "End";

        internal const string CODENAMESPACE = "PHP.Library.Soap.DynamicProxy";
        internal const string DEFAULTBASETYPE = "System.Web.Services.Protocols.SoapHttpClientProtocol";
        internal const string CUSTOMBASETYPE = "PHP.Library.Soap.SoapHttpClientProtocolExtended";

        internal const string LIBTEMPDIR = "DynamicProxyTempDir";
        internal const string TEMPDLLEXTENSION = "_soapclient_tmp.dll";
    }
}
