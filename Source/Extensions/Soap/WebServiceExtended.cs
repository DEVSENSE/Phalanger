using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Services;
using PHP.Core;
using PHP.Core.Reflection;
using Convert = PHP.Core.Convert;

namespace PHP.Library.Soap
{
    public class WebServiceExtended : WebService
    {
        protected object[] Invoke(string name,  params object[] parameters)
        {
            var type = (DTypeDesc) Context.Items["SoapServerType"];
            var method = type.GetMethod(new Name(name));
            var clrMethod = WsdlHelper.GetMethodBySoapName(name, GetType());
            var p = ResultBinder.BindServerParameters(parameters, clrMethod);
            var context = ScriptContext.CurrentContext;
            context.Stack.AddFrame(p);
            var result = Convert.ObjectToPhpArray(method.Invoke(null, context.Stack));
            var binder = new ParameterBinder();
            var results = binder.BindServerResult(clrMethod, result, true);
            return results;
        }
    }
}
