using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Data;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
﻿using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;
using System.Web.Services.Description;
using System.Web.Services.Discovery;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Microsoft.CSharp;
using PHP.Core;
using SDD = System.Data.Design;

namespace PHP.Library.Soap
{

    internal class DynamicWebServiceProxy
    {
        private Assembly ass;
        private object proxyInstance;
        private string wsdl;
        private string protocolName = "Soap";
        private string proxySource;
        private ServiceDescriptionImporter sdi;
        private XmlSchemas schemas;
        private bool enableMessageAccess;
        private static bool pipelineProperlyConfigured;
        private ArrayList outParams = new ArrayList();
        private ServiceCache serviceCache;
        private readonly X509Certificate2 certificate;

        /// <summary>
        /// Creates a new <see cref="DynamicWebServiceProxy"/> instance.
        /// </summary>
        /// <param name="wsdlLocation">Location of WSDL file</param>
        /// <param name="enableMessageAccess">Enables access to SOAP messages</param>
        /// <param name="wsdlCache">Type of caching to be used</param>
        /// <param name="certificate">Certificate to use.</param>
        internal DynamicWebServiceProxy(string wsdlLocation, bool enableMessageAccess = false, WsdlCache wsdlCache = WsdlCache.Both, X509Certificate2 certificate = null)
        {
            this.wsdl = wsdlLocation;
            this.enableMessageAccess = enableMessageAccess;
            this.serviceCache = new ServiceCache(wsdlLocation, wsdlCache, new ServiceCache.CacheMissEvent(BuildAssemblyFromWsdl));
            this.certificate = certificate;
            BuildProxy();
        }

        /// <summary>
        /// Invokes the call.
        /// </summary>
        /// <returns></returns>
        public object InvokeCall(string methodName ,PhpArray parameters)
        {
            var soapProxy = (SoapHttpClientProtocolExtended)proxyInstance;
            MethodInfo mi = soapProxy.GetType().GetMethod(methodName);

            bool wrappedArgs = true;

            object[] attr = mi.GetCustomAttributes(typeof(System.Web.Services.Protocols.SoapDocumentMethodAttribute), false);
            if (attr.Length > 0 && attr[0].GetType() == typeof(System.Web.Services.Protocols.SoapDocumentMethodAttribute))
            {
                var soapMethodAttr = (System.Web.Services.Protocols.SoapDocumentMethodAttribute)attr[0];
                if (soapMethodAttr.ParameterStyle == System.Web.Services.Protocols.SoapParameterStyle.Bare)
                {
                    wrappedArgs = false;
                }
            }


            var paramBinder = new ParameterBinder();
            object[] transformedParameters = paramBinder.BindParams(mi, parameters, wrappedArgs);


            object[] resArray = soapProxy.Invoke(methodName, transformedParameters);

            if (resArray[0] != null)
            {
                resArray[0] = ResultBinder.BindResult( 
                    resArray[0],
                    mi.Name,
                    wrappedArgs);
            }

            //object result = mi.Invoke(proxyInstance, (object[])methodParams.ToArray(typeof(object)));

            //foreach (ParameterInfo pi in mi.GetParameters())
            //{
            //    if (pi.IsOut) outParams.Add(methodParams[i]);

            //    i++;
            //}
                
            return resArray[0];
        }

        #region Async invoke (not supported now)

        ///// <summary>
        ///// Begins the invoke call.
        ///// </summary>
        ///// <param name="callback">Callback.</param>
        ///// <param name="asyncState">State of the async.</param>
        ///// <returns></returns>
        //public IAsyncResult BeginInvokeCall(AsyncCallback callback, object asyncState)
        //{
        //    try
        //    {
        //        ArrayList parameters = new ArrayList(methodParams);
        //        parameters.Add(callback);
        //        parameters.Add(asyncState);

        //        MethodInfo mi = proxyInstance.GetType().GetMethod(CodeConstants.BEGIN + methodName);
        //        IAsyncResult result = (IAsyncResult)mi.Invoke(proxyInstance, (object[])parameters.ToArray(typeof(object)));

        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //        throw new MessageStorageException("Problem asynchronously calling the Web service.", ex);
        //    }
        //}

        ///// <summary>
        ///// Ends the invoke call.
        ///// </summary>
        ///// <param name="asyncResult">Async result.</param>
        ///// <returns></returns>
        //public object EndInvokeCall(IAsyncResult asyncResult)
        //{
        //    try
        //    {
        //        MethodInfo mi = proxyInstance.GetType().GetMethod(CodeConstants.END + methodName);
        //        object result = mi.Invoke(proxyInstance, new object[] { asyncResult });

        //        return result;
        //    }
        //    catch (MessageStorageException e)
        //    {
        //        Console.WriteLine(e.Message);
        //        return null;
        //    }
        //}

        #endregion

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value></value>
        public object Instance
        {
            get
            {
                return proxyInstance;
            }
        }


        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        /// <value></value>
        public Uri Url
        {
            get
            {
                PropertyInfo propInfo = proxyInstance.GetType().GetProperty("Url");
                object result = propInfo.GetValue(proxyInstance, null);

                return new Uri((string)result);
            }
            set
            {
                string urlValue = value.AbsoluteUri;
                PropertyInfo propInfo = proxyInstance.GetType().GetProperty("Url");
                propInfo.SetValue(proxyInstance, urlValue,
                    BindingFlags.NonPublic | BindingFlags.Static |
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetField,
                    null, null, null
                    );
            }
        }

        /// <summary>
        /// Gets or sets the WSDL.
        /// </summary>
        /// <value></value>
        public string Wsdl
        {
            // TODO: move the init process to an explicit method Init() ...
            get
            {
                return wsdl;
            }
            //set
            //{
            //    wsdl = value;
            //    ResetInternalState();
            //    BuildProxy();
            //}
        }

        /// <summary>
        /// Gets or sets the name of the protocol.
        /// </summary>
        /// <value></value>
        public Protocol ProtocolName
        {
            get
            {
                switch (protocolName)
                {
                    case "HttpGet":
                        return Protocol.HttpGet;
                    case "HttpPost":
                        return Protocol.HttpPost;
                    case "Soap":
                        return Protocol.HttpSoap;
                    default:
                        return Protocol.HttpSoap;
                }
            }
            set
            {
                switch (value)
                {
                    case Protocol.HttpGet:
                        protocolName = "HttpGet";
                        break;
                    case Protocol.HttpPost:
                        protocolName = "HttpPost";
                        break;
                    case Protocol.HttpSoap:
                        protocolName = "Soap";
                        break;
                }
            }
        }

        ///// <summary>
        ///// Clears the cache.
        ///// </summary>
        ///// <param name="wsdlLocation">WSDL location.</param>
        //public static void ClearCache(string wsdlLocation)
        //{
        //    CompiledAssemblyCache.ClearCache(wsdlLocation);
        //}

        ///// <summary>
        ///// Clear all cached DLLs.
        ///// </summary>
        //public static void ClearAllCached()
        //{
        //    CompiledAssemblyCache.ClearAllCached();
        //}

        /// <summary>
        /// Builds the assembly from WSDL.
        /// </summary>
        /// <param name="absoluteWsdlLocation">Absolute path to wsdl file.</param>
        /// /// <param name="wsdlContent">Actual content of wsdl file</param>
        /// <returns>Assembly containg proxy for service defined in <paramref name="absoluteWsdlLocation"/></returns>
        private Assembly BuildAssemblyFromWsdl(string absoluteWsdlLocation, string wsdlContent)
        {
            // Use an XmlTextReader to get the Web Service description
            StringReader wsdlStringReader = new StringReader(wsdlContent);
            XmlTextReader tr = new XmlTextReader(wsdlStringReader);
            ServiceDescription.Read(tr);
            tr.Close();

            // WSDL service description importer 
            CodeNamespace cns = new CodeNamespace(CodeConstants.CODENAMESPACE);
            sdi = new ServiceDescriptionImporter();
            //sdi.AddServiceDescription(sd, null, null);

            // check for optional imports in the root WSDL
            CheckForImports(absoluteWsdlLocation);

            sdi.ProtocolName = protocolName;
            sdi.Import(cns, null);

            // change the base class
            // get all available Service classes - not only the default one
            ArrayList newCtr = new ArrayList();

            foreach (CodeTypeDeclaration ctDecl in cns.Types)
            {
                if (ctDecl.BaseTypes.Count > 0)
                {
                    if (ctDecl.BaseTypes[0].BaseType == CodeConstants.DEFAULTBASETYPE)
                    {
                        newCtr.Add(ctDecl);
                    }
                }
            }

            foreach (CodeTypeDeclaration ctDecl in newCtr)
            {
                cns.Types.Remove(ctDecl);
                ctDecl.BaseTypes[0] = new CodeTypeReference(CodeConstants.CUSTOMBASETYPE);
                cns.Types.Add(ctDecl);
            }

            // source code generation
            CSharpCodeProvider cscp = new CSharpCodeProvider();
            StringBuilder srcStringBuilder = new StringBuilder();
            StringWriter sw = new StringWriter(srcStringBuilder, CultureInfo.CurrentCulture);

            if (schemas != null)
            {
                foreach (XmlSchema xsd in schemas)
                {
                    if (XmlSchemas.IsDataSet(xsd))
                    {
                        MemoryStream mem = new MemoryStream();
                        mem.Position = 0;
                        xsd.Write(mem);
                        mem.Position = 0;
                        DataSet dataSet1 = new DataSet();
                        dataSet1.Locale = CultureInfo.InvariantCulture;
                        dataSet1.ReadXmlSchema(mem);
                        SDD.TypedDataSetGenerator.Generate(dataSet1, cns, cscp);
                    }
                }
            }

            cscp.GenerateCodeFromNamespace(cns, sw, null);
            proxySource = srcStringBuilder.ToString();
            sw.Close();

            // assembly compilation
            string location = "";

            if (HttpContext.Current != null)
            {
                location = HttpContext.Current.Server.MapPath(".");
                location += @"\bin\";
            }

            CompilerParameters cp = new CompilerParameters();
            cp.ReferencedAssemblies.Add("System.dll");
            cp.ReferencedAssemblies.Add("System.Xml.dll");
            cp.ReferencedAssemblies.Add("System.Web.Services.dll");
            cp.ReferencedAssemblies.Add("System.Data.dll");
            cp.ReferencedAssemblies.Add(Assembly.GetExecutingAssembly().Location);

            cp.GenerateExecutable = false;
            cp.GenerateInMemory = false;
            cp.IncludeDebugInformation = false;
            cp.TempFiles = new TempFileCollection(CompiledAssemblyCache.GetLibTempPath());

            CompilerResults cr = cscp.CompileAssemblyFromSource(cp, proxySource);

            if (cr.Errors.Count > 0)
                throw new DynamicCompilationException(string.Format(CultureInfo.CurrentCulture, @"Building dynamic assembly failed: {0} errors", cr.Errors.Count));

            Assembly compiledAssembly = cr.CompiledAssembly;
            
            return compiledAssembly;
        }

        /// <summary>
        /// Creates the instance.
        /// </summary>
        /// <returns></returns>
        private object CreateProxyInstance()
        {
            string objTypeName = null;

            try
            {
                foreach (Type ty in ProxyAssembly.GetTypes())
                {
                    if (ty.BaseType == typeof(SoapHttpClientProtocolExtended))
                    {
                        objTypeName = ty.Name;
                        break;
                    }
                }

                Type t = ass.GetType(CodeConstants.CODENAMESPACE + "." + objTypeName);

                return Activator.CreateInstance(t);
            }
            catch (Exception ex)
            {
                throw new ProxyTypeInstantiationException("An error occured while instantiating the proxy type: " + ex.Message + ", " + ex.StackTrace, ex);
            }
        }

        /// <summary>
        /// Resets the state of the internal.
        /// </summary>
        private void ResetInternalState()
        {
            protocolName = "Soap";
            sdi = null;
        }

        /// <summary>
        /// Builds the proxy.
        /// </summary>
        private void BuildProxy()
        {
            if (enableMessageAccess)
            {
                PipelineConfiguration.InjectExtension(typeof(SoapMessageAccessClientExtension));
                pipelineProperlyConfigured = true;
            }

            ass = serviceCache.GetOrAdd();
            proxyInstance = CreateProxyInstance();

            if (certificate != null)
            {
                var proxy = (SoapHttpClientProtocolExtended)proxyInstance;
                proxy.ClientCertificates.Add(certificate);
            }
        }

        /// <summary>
        /// Checks the for imports.
        /// </summary>
        /// <param name="baseWSDLUrl">Base WSDL URL.</param>
        private void CheckForImports(string baseWSDLUrl)
        {
            DiscoveryClientProtocol dcp = new DiscoveryClientProtocol();
            //DEBUG code
            try
            {
                dcp.DiscoverAny(baseWSDLUrl);
                dcp.ResolveAll();
            }
            catch(UriFormatException ex)
            {
                throw new ApplicationException("Not a valid wsdl location: " + baseWSDLUrl, ex);
            }

            foreach (object osd in dcp.Documents.Values)
            {
                if (osd is ServiceDescription) sdi.AddServiceDescription((ServiceDescription)osd, null, null);
                if (osd is XmlSchema)
                {
                    // store in global schemas variable
                    if (schemas == null) schemas = new XmlSchemas();
                    schemas.Add((XmlSchema)osd);

                    sdi.Schemas.Add((XmlSchema)osd);
                }
            }
        }

        /// <summary>
        /// Gets the SOAP request.
        /// </summary>
        /// <value></value>
        public string SoapRequest
        {
            get
            {
                if (enableMessageAccess && pipelineProperlyConfigured)
                {
                    PropertyInfo propInfo = proxyInstance.GetType().GetProperty("SoapRequestString");
                    object result = propInfo.GetValue(proxyInstance, null);

                    return (string)result;
                }
                else
                    return String.Empty;
            }
        }

        /// <summary>
        /// Gets the SOAP response.
        /// </summary>
        /// <value></value>
        public string SoapResponse
        {
            get
            {
                if (enableMessageAccess && pipelineProperlyConfigured)
                {
                    PropertyInfo propInfo = proxyInstance.GetType().GetProperty("SoapResponseString");
                    object result = propInfo.GetValue(proxyInstance, null);

                    return (string)result;
                }
                else
                    return String.Empty;
            }
        }

        /// <summary>
        /// Gets or sets the credentials.
        /// </summary>
        /// <value></value>
        public ICredentials Credentials
        {
            set
            {
                PropertyInfo propInfo = proxyInstance.GetType().GetProperty("Credentials");
                propInfo.SetValue(proxyInstance, value, null);
            }

            get
            {
                PropertyInfo propInfo = proxyInstance.GetType().GetProperty("Credentials");
                ICredentials result = (ICredentials)propInfo.GetValue(proxyInstance, null);

                return result;
            }
        }

        /// <summary>
        /// Gets or sets the timeout.
        /// </summary>
        /// <value></value>
        public int Timeout
        {
            set
            {
                PropertyInfo propInfo = proxyInstance.GetType().GetProperty("Timeout");
                propInfo.SetValue(proxyInstance, value, null);
            }

            get
            {
                PropertyInfo propInfo = proxyInstance.GetType().GetProperty("Timeout");
                int result = (int)propInfo.GetValue(proxyInstance, null);

                return result;
            }
        }

        /// <summary>
        /// Gets or sets the proxy.
        /// </summary>
        /// <value></value>
        public IWebProxy Proxy
        {
            set
            {
                PropertyInfo propInfo = proxyInstance.GetType().GetProperty("Proxy");
                propInfo.SetValue(proxyInstance, value, null);
            }

            get
            {
                PropertyInfo propInfo = proxyInstance.GetType().GetProperty("Proxy");
                IWebProxy result = (IWebProxy)propInfo.GetValue(proxyInstance, null);

                return result;
            }
        }

        /// <summary>
        /// Gets the proxy assembly.
        /// </summary>
        /// <value></value>
        public Assembly ProxyAssembly
        {
            get
            {
                return ass;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [enable message access].
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [enable message access]; otherwise, <c>false</c>.
        /// </value>
        public bool EnableMessageAccess
        {
            get
            {
                return enableMessageAccess && pipelineProperlyConfigured;
            }

            //set
            //{
            //    PipelineConfiguration.InjectExtension(typeof(SoapMessageAccessClientExtension));
            //    enableMessageAccess = value;
            //}
        }

        /// <summary>
        /// Gets or sets the dynamic and cached assembly's temporary path.
        /// </summary>
        /// <value></value>
        public string AssemblyTemporaryPath
        {
            get
            {
                return CompiledAssemblyCache.GetLibTempPath();
            }

            set
            {
                CompiledAssemblyCache.SetLibTempPath(value);
            }
        }

        /// <summary>
        /// Gets the out params.
        /// </summary>
        /// <value></value>
        public ArrayList OutParameters
        {
            get { return outParams; }
        }
    }
}
