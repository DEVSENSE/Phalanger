using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Web;
using System.Web.Services.Protocols;
using PHP.Core;
using PHP.Core.Reflection;

namespace PHP.Library.Soap
{
    [ImplementsType()]
    public partial class SoapServer : PhpObject
    {
        private DynamicWebServiceProxy wsp;
        private DTypeDesc type;

        public SoapServer(string wsdl) : this(wsdl, null)
        {
        }

        public SoapServer(string wsdl, PhpArray options) : base(ScriptContext.CurrentContext, true)
        {
            
        }

        [PhpVisible, ImplementsMethod]
        public void __construct(string wsdl, [Optional] PhpArray options)
        {
            wsp = new DynamicWebServiceProxy(wsdl);
        }

        [PhpVisible, ImplementsMethod]
        public void setClass(string class_name)
        {
            var context = ScriptContext.CurrentContext;
            type = context.DeclaredTypes[class_name];
        }

        private static MethodInfo coreGetHandler = typeof(WebServiceHandlerFactory).GetMethod("CoreGetHandler",
            BindingFlags.NonPublic | BindingFlags.Instance);
        [PhpVisible, ImplementsMethod]
        public void handle()
        {
            var clrType = wsp.ProxyAssembly.GetTypes().First(a => a.BaseType == typeof (WebServiceExtended));
            var factory = new WebServiceHandlerFactory();
            var context = HttpContext.Current;
            context.Items["SoapServerType"] = type;
            context.Items["SoapServerDynamicWebServiceProxy"] = wsp;
            var handler = (IHttpHandler) coreGetHandler.Invoke(factory, new object[] {clrType, context, context.Request, context.Response});
            handler.ProcessRequest(context);
        }
    }
}