using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using System.Runtime.InteropServices;
﻿using System.Security.Cryptography.X509Certificates;
using System.IO;

namespace PHP.Library.Soap
{
    /// <summary>
    /// The SoapClient class provides a client for » SOAP 1.1, » SOAP 1.2 servers. It can be used in WSDL or non-WSDL mode.
    /// </summary>
    /// <remarks>
    /// Phalanger supports only WSDL mode and there isn't plan for supporting non-WSDL mode.
    /// </remarks>
    [ImplementsType()]
    public partial class SoapClient : PhpObject
    {
        private DynamicWebServiceProxy wsp = null;
        private bool exceptions = true;

        /// <summary>
        /// Calls a SOAP function
        /// </summary> 
        [PhpVisible, ImplementsMethod]
        public object __call(string function_name, PhpArray arguments/*, PhpArray options, PhpArray input_headers, PhpArray output_headers*/)
        {
            try
            {
                if (arguments.Count > 0)
                {
                    var item = arguments.GetArrayItem(0, true);

                    if (item != null && item.GetType() == typeof(PhpArray))
                    {
                        PhpArray arr = (PhpArray)item;
                        return wsp.InvokeCall(function_name, arr);
                    }
                }
            }
            catch (Exception exception)
            {
                SoapFault.Throw(ScriptContext.CurrentContext, "SOAP-ERROR", exception.Message, exceptions);
            }

            return null;
        }


        /// <summary>
        /// SoapClient::__doRequest()
        /// </summary> 
        [PhpVisible, ImplementsMethod]
        public string __doRequest()
        {
            PhpException.FunctionNotSupported(PhpError.Warning);
            return null;
        }


        /// <summary>
        /// Returns list of SOAP functions
        /// </summary> 
        [PhpVisible, ImplementsMethod]
        public PhpArray __getFunctions()
        {
            PhpException.FunctionNotSupported(PhpError.Warning);
            return null;
        }


        /// <summary>
        /// Returns last SOAP request
        /// </summary> 
        [PhpVisible, ImplementsMethod]
        public string __getLastRequest()
        {
            try
            {
                return wsp.SoapRequest;
            }
            catch (Exception exception)
            {
                SoapFault.Throw(ScriptContext.CurrentContext, "SOAP-ERROR", exception.Message, exceptions);
                return null;
            }

        }

        /// <summary>
        /// Returns last SOAP request headers
        /// </summary> 
        [PhpVisible, ImplementsMethod]
        public string __getLastRequestHeaders()
        {
            PhpException.FunctionNotSupported(PhpError.Warning);
            return null;
        }


        /// <summary>
        /// Returns last SOAP response
        /// </summary> 
        [PhpVisible, ImplementsMethod]
        public object __getLastResponse()
        {
            try
            {
                return wsp.SoapResponse;
            }
            catch (Exception exception)
            {
                SoapFault.Throw(ScriptContext.CurrentContext, "SOAP-ERROR", exception.Message, exceptions);
                return null;
            }
        }


        /// <summary>
        /// Returns last SOAP response headers
        /// </summary> 
        [PhpVisible, ImplementsMethod]
        public string __getLastResponseHeaders()
        {
            PhpException.FunctionNotSupported(PhpError.Warning);
            return null;
        }


        /// <summary>
        /// Returns list of SOAP types
        /// </summary> 
        [PhpVisible, ImplementsMethod]
        public PhpArray __getTypes()
        {
            PhpException.FunctionNotSupported(PhpError.Warning);
            return null;
        }


        /// <summary>
        /// Sets cookie thet will sent with SOAP request.
        /// </summary> 
        [PhpVisible, ImplementsMethod]
        public void __setCookie(string name, string value)
        {
            PhpException.FunctionNotSupported(PhpError.Warning);

        }


        /// <summary>
        /// Sets the location option (the endpoint URL that will be touched by the
        /// </summary> 
        [PhpVisible, ImplementsMethod]
        public string __setLocation(string new_location)
        {
            PhpException.FunctionNotSupported(PhpError.Warning);
            return null;
        }



        /// <summary>
        /// Sets SOAP headers for subsequent calls (replaces any previous
        /// </summary> 
        [PhpVisible, ImplementsMethod]
        public void __setSoapHeaders(PhpArray SoapHeaders)
        {
            PhpException.FunctionNotSupported(PhpError.Warning);

        }



        /// <summary>
        /// SoapClient constructor
        /// </summary> 
        public SoapClient(string wsdl, PhpArray options = null)
            : base(ScriptContext.CurrentContext, true)
        {
            __construct(wsdl, options);
        }

        /// <summary>
        /// SoapClient constructor
        /// </summary>
        /// <param name="wsdl">URI of the WSDL file or NULL if working in non-WSDL mode.</param>
        /// <param name="options">An array of options. If working in WSDL mode, this parameter is optional.
        /// If working in non-WSDL mode, the location and uri options must be set, where location is the URL
        /// of the SOAP server to send the request to, and uri is the target namespace of the SOAP service. </param>
        [PhpVisible, ImplementsMethod]
        public void __construct(string wsdl, [Optional] PhpArray options)
        {
            bool enableMessageAccess = false;
            WsdlCache wsdlCache = WsdlCache.Both;
            X509Certificate2 certificate = null;

            if (options != null)
            {
                object value;
                
                if (options.TryGetValue("trace", out value))
                {
                    enableMessageAccess = PHP.Core.Convert.ObjectToBoolean(value);
                }

                if (options.TryGetValue("cache_wsdl", out value))
                {
                    wsdlCache = (WsdlCache)value;//PHP.Core.Convert.ObjectToBoolean(value);//WsdlCache.None == 0, anything else is true
                }

                if (options.TryGetValue("exceptions", out value))
                {
                    exceptions = PHP.Core.Convert.ObjectToBoolean(value);
                }

                // certificate:
                string pass = null;

                if (options.TryGetValue("passphrase", out value))
                {
                    pass = Core.Convert.ObjectToString(value);
                }

                if (options.TryGetValue("local_cert", out value))
                {
                    var cert = Core.Convert.ObjectToString(value);
                    if (cert != null)
                        certificate = new X509Certificate2(cert, pass);
                }
            }

            try
            {
                wsp = new DynamicWebServiceProxy(wsdl, enableMessageAccess, wsdlCache, certificate);
            }
            catch (Exception exception)
            {
                SoapFault.Throw(ScriptContext.CurrentContext, "SOAP-ERROR", exception.Message, exceptions);
            }

        }


    }
}
