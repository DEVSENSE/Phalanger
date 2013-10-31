﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Services.Protocols;

namespace PHP.Library.Soap
{
    /// <summary>
    /// Extended SoapHttpClientProtocol implementing a custom behaviour as place for
    /// SOAP messages by <see cref="SoapMessageAccessClientExtension"/>
    /// </summary>
    public class SoapHttpClientProtocolExtended : SoapHttpClientProtocol
    {
        private byte[] m_SoapRequestMsg;
        private byte[] m_SoapResponseMsg;
        private WebRequest lastRequest;
        private WebResponse lastResponse;

        /// <summary>
        /// Creates a new <see cref="SoapHttpClientProtocolExtended"/> instance.
        /// </summary>
        public SoapHttpClientProtocolExtended()
        {
        }

        /// <summary>
        /// Sets the SOAP request internal.
        /// </summary>
        /// <value></value>
        internal byte[] SoapRequestInternal
        {
            set
            {
                m_SoapRequestMsg = value;
            }
        }

        /// <summary>
        /// Sets the SOAP response internal.
        /// </summary>
        /// <value></value>
        internal byte[] SoapResponseInternal
        {
            set
            {
                m_SoapResponseMsg = value;
            }
        }

        /// <summary>
        /// Gets the SOAP request.
        /// </summary>
        /// <value></value>
        public byte[] SoapRequest
        {
            get
            {
                return m_SoapRequestMsg;
            }
        }

        /// <summary>
        /// Gets the SOAP response.
        /// </summary>
        /// <value></value>
        public byte[] SoapResponse
        {
            get
            {
                return m_SoapResponseMsg;
            }
        }

        /// <summary>
        /// Gets the SOAP request string.
        /// </summary>
        /// <value></value>
        public string SoapRequestString
        {
            get
            {
                byte[] result = m_SoapRequestMsg;
                UTF8Encoding enc = new UTF8Encoding();

                return enc.GetString(result);
            }
        }

        /// <summary>
        /// Gets the SOAP response string.
        /// </summary>
        /// <value></value>
        public string SoapResponseString
        {
            get
            {
                byte[] result = m_SoapResponseMsg;
                UTF8Encoding enc = new UTF8Encoding();

                return enc.GetString(result);
            }
        }

        /// <summary>
        /// Invokes an XML Web service method synchronously using SOAP.
        /// </summary>
        /// <param name="methodName">The name of the XML Web service method.</param>
        /// <param name="parameters">An array of objects that contains the parameters to pass to the XML Web service. The order of the values in the array corresponds to the order of the parameters in the calling method of the derived class.</param>
        /// <returns>An array of objects that contains the return value and any reference or out parameters of the derived class method.</returns>
        public new object[] Invoke(string methodName, object[] parameters)
        {
            return base.Invoke(methodName, parameters);
        }

        protected override WebRequest GetWebRequest(Uri uri)
        {
            return lastRequest = base.GetWebRequest(uri);
        }

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            return lastResponse = base.GetWebResponse(request);
        }

        public string RequestHeaders
        {
            get
            {
                var req = (HttpWebRequest) lastRequest;
                var sb = new StringBuilder();
                sb.AppendFormat("{0} {1} HTTP/{2}{3}", req.Method, req.RequestUri.PathAndQuery, req.ProtocolVersion.ToString(2), Environment.NewLine);
                sb.Append(req.Headers);
                return sb.ToString();
            }
        }

        public string ResponseHeaders
        {
            get
            {
                var res = ((HttpWebResponse) lastResponse);
                var sb = new StringBuilder();
                sb.AppendFormat("HTTP/{0} {1} {2}{3}", res.ProtocolVersion.ToString(2), (int)res.StatusCode, res.StatusDescription, Environment.NewLine);
                sb.Append(res.Headers);
                return sb.ToString();
            }
        }
    }
}
