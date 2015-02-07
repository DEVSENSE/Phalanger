using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PHP.Core.Emit;

namespace PHP.Core
{
    public abstract class PluginHandler
    {
        /// <summary>
        /// Called before emitting statements.
        /// </summary>
        public static Action<ILEmitter, List<PHP.Core.AST.Statement>> BeforeBodyEmitter;
        
        /// <summary>
        /// If not <c>null</c>, called instead of <b>Emit(OpCodes.Ldstr, value)</b>.
        /// </summary>
        public static Action<ILEmitter, string> StringLiteralEmitter;
        
        /// <summary>
        /// Called on every function or method parameter name to allow changing its value.
        /// </summary>
        public static Converter<string, string> ParameterNameConverter;
        public static string ConvertParameterName(string name) { return (ParameterNameConverter != null) ? ParameterNameConverter(name) : name; }
    }
}
