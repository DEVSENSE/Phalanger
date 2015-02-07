using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Reflection;

namespace PHP.Core.CodeDom
{
    /// <summary>Conntains helper members for PHP-to-CodeDOM translator</summary>
    /// <remarks>
    /// These members may appear in CodeDOM which originates from user-typed PHP code.
    /// It is because PHP-to-CodeDOM translator is not designed to be universal.
    /// It is primary designed to translate CodeDOM-to-PHP-generator-generated code back to PHP and it supports only very few PHP spoecialities.  
    /// These helper members usually stands for somethink that cannot be repserented in CodeDOM but in PHP can.
    /// The aim of these members is NOT to substitute PHP behavior and implement it for CodeDOM.
    /// The aim of these members is to translate CodeDOM-unsupported feature from PHP to CodeDOM somehow.
    /// In the future it may allow CodeDOM-to-PHP translator to translate such construct to PHP to the same code that have generated it.
    /// In order to avoid usage of these members by users members are hidden and obsolete.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class Helper
    {
        /// <summary>Translates type specification used by CodeDOM to string that can be used for <see cref="Type.GetType"/>.</summary>
        /// <param name="t">A <see cref="System.CodeDom.CodeTypeReference"/></param>
        /// <param name="GenericArgs">True for include generic args in string (otherwise only grave and number will be included)</param>
        /// <returns>Type name</returns>
        private static string GetTypeName(System.CodeDom.CodeTypeReference t, bool GenericArgs)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(t.ArrayRank == 0 ? t.BaseType : GetTypeName(t.ArrayElementType, GenericArgs));
            if (t.TypeArguments.Count > 0 && GenericArgs)
            {
                sb.Append('[');
                foreach (System.CodeDom.CodeTypeReference arg in t.TypeArguments)
                {
                    sb.Append('[');
                    sb.Append(GetTypeName(arg, true));
                    sb.Append(']');
                }
                sb.Append(']');
            }
            if (t.ArrayRank == 1) sb.Append("[]");
            if (t.ArrayRank > 1) sb.Append('[' + new string(',', t.ArrayRank - 1) + ']');
            return sb.ToString();
        }

        /// <summary>Tries to get <see cref="Type"/> from <see cref="System.CodeDom.CodeTypeReference"/></summary>
        /// <param name="t">A <see cref="System.CodeDom.CodeTypeReference"/></param>
        /// <param name="aliases"></param>
        /// <param name="imports"></param>
        /// <param name="references"></param>
        /// <returns><see cref="Type"/> if found or null.</returns>
        internal static Type GetType(System.CodeDom.CodeTypeReference t, IDictionary<string, string> aliases, string[] imports, IList<string> references)
        {
            string tn = GetTypeName(t, false);
            Type type = null;
            //try { type = Type.GetType(tn); }catch{}

            string[] namespaces = new string[imports.Length + 1];
            Array.Copy(imports, 0, namespaces, 1, imports.Length);
            namespaces[0] = string.Empty;

            if (references == null)references = new string[0];

            string[] assemblies = new string[references.Count + 1];
            references.CopyTo(assemblies, 0);
            assemblies[references.Count] = string.Empty;
            //if(type == null)

            // translate aliases
            if (aliases != null)
                foreach (var alias in aliases)
                {
                    string TypeName = tn;
                    if (alias.Key == TypeName) { TypeName = alias.Value; break; }
                    if (TypeName.StartsWith(alias.Key + ".")) { TypeName = alias.Value + TypeName.Substring(alias.Key.Length); break; }
                }

            // resolve type name
            foreach (string Assembly in assemblies)
            {
                foreach (string Namespace in namespaces)
                {
                    string TypeName = tn;
                    if (!string.IsNullOrEmpty(Namespace))
                        TypeName = Namespace + "." + tn;

                    try
                    {
                        if (!string.IsNullOrEmpty(Assembly))
                        {
                            if (System.IO.File.Exists(Assembly))    // assembly path
                            {
                                var ass = System.Reflection.Assembly.ReflectionOnlyLoadFrom(Assembly);
                                if (ass != null)
                                {
                                    type = ass.GetType(TypeName);
                                }
                            }
                            else
                            {
                                TypeName += "," + Assembly; // full assembly name
                                type = Type.GetType(TypeName);
                            }
                        }
                        else
                        {
                            type = Type.GetType(TypeName);  // try to load from current app domain
                        }
                    }
                    catch { }

                    if (type != null) break;
                }
                if (type != null) break;
            }
            return type;
        }

        /// <summary>unset PHP pseudo-function is translated as assignment of this field ot a variable (with exception of local variables for which ünset is translated to assign of null because it is what PHP actually does)</summary>
        /// <remarks>Note: assigning null to anything else than local variable is not the same think as unsetting it!</remarks>
        [Obsolete("Do not use this member! This is only CodeDOM replacement for PHP unset pseudo-function")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        readonly public static object unset = null;
        /// <summary>Creating instance of this class and using it's <see cref="IndirectStFldAccess.Access">Access</see> property stands in CodeDOM for PHP indirect access to static field (MyClass::$$field)</summary>
        /// <remarks>The <see cref="IndirectStFldAccess.Access">Access</see> is currently not implemented and throws <see cref="NotImplementedException"/>.</remarks>
        [Obsolete("Do not use this class! This is only CodeDOM replacement for PHP indirect access to static field (MyClass::$$var)")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public sealed class IndirectStFldAccess
        {
            /// <summary>Type <see cref="field"/> is field of</summary>
            private Type type;
            /// <summary>Name of field to access to</summary>
            private string field;
            /// <summary>CTor</summary>
            /// <param name="type">Type <paramref name="field"/> is field of</param>
            /// <param name="field">Name of field to be accessed</param>
            public IndirectStFldAccess(Type type, string field)
            {
                this.type = type;
                this.field = field;
            }
            /// <summary>If implemented provides access to field given in constructor</summary>
            /// <remarks>Actually neither setter nor getter is implemented and both throw <see cref="NotImplementedException"/></remarks>
            /// <exception cref="NotImplementedException">Always</exception>
            public object Access
            {
                get
                {
                    throw new NotImplementedException("IndirectStFldAccess.Access's getter is not implemented. This class is only helper for PHP-to-CodeDOM translation");
                }
                set
                {
                    throw new NotImplementedException("IndirectStFldAccess.Access's getter is not implemented. This class is only helper for PHP-to-CodeDOM translation");
                }
            }
        }
        /// <summary>Creating instance of this class and using it's <see cref="IndirectInstFldAccess.Access">Access</see> property stands in CodeDOM for PHP indirect access to instance field ($instance->$field)</summary>
        /// <remarks>The <see cref="IndirectInstFldAccess.Access">Access</see> is currently not implemented and throws <see cref="NotImplementedException"/>.</remarks>
        [Obsolete("Do not use this class! This is only CodeDOM replacement for PHP indirect access to instance field ($obj->$var).")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public sealed class IndirectInstFldAccess
        {
            /// <summary>Instance <see cref="field"/> is field of</summary>
            private object instance;
            /// <summary>Name of field to access to</summary>
            private string field;
            /// <summary>CTor</summary>
            /// <param name="instance">Instance <paramref name="field"/> is field of</param>
            /// <param name="field">Name of field to be accessed</param>
            public IndirectInstFldAccess(object instance, string field)
            {
                this.instance = instance;
                this.field = field;
            }
            /// <summary>If implemented provides access to field given in constructor</summary>
            /// <remarks>Actually neither setter nor getter is implemented and both throw <see cref="NotImplementedException"/></remarks>
            /// <exception cref="NotImplementedException">Always</exception>
            public object Access
            {
                get
                {
                    throw new NotImplementedException("IndirectInstFldAccess.Access's getter is not implemented. This class is only helper for PHP-to-CodeDOM translation");
                }
                set
                {
                    throw new NotImplementedException("IndirectInstFldAccess.Access's getter is not implemented. This class is only helper for PHP-to-CodeDOM translation");
                }
            }
        }
        /// <summary>Creating instance of this class and using it's <see cref="IndirectVarAccess.Access">Access</see> property stands in CodeDOM for PHP indirect access variable ($$var)</summary>
        /// <remarks>The <see cref="IndirectVarAccess.Access">Access</see> is currently not implemented and throws <see cref="NotImplementedException"/>.</remarks>
        [Obsolete("Do not use this class! This is only CodeDOM replacement for PHP indirect access to variable ($$var).")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public sealed class IndirectVarAccess
        {
            /// <summary>Name of variable to access to</summary>
            private string field;
            /// <summary>CTor</summary>
            /// <param name="field">Name of variable to be accessed</param>
            public IndirectVarAccess(string field)
            {
                this.field = field;
            }
            /// <summary>If implemented provides access to variable with name given in constructor</summary>
            /// <remarks>Actually neither setter nor getter is implemented and both throw <see cref="NotImplementedException"/></remarks>
            /// <exception cref="NotImplementedException">Always</exception>
            public object Access
            {
                get
                {
                    throw new NotImplementedException("IndirectVarAccess.Access's getter is not implemented. This class is only helper for PHP-to-CodeDOM translation");
                }
                set
                {
                    throw new NotImplementedException("IndirectVarAccess.Access's setter is not implemented. This class is only helper for PHP-to-CodeDOM translation");
                }
            }
        }
        /// <summary>Indirectly calls static method. Intended only to be CodeDOM replacement for PHP indirect static method call</summary>
        /// <param name="type">Type to invoke method on</param>
        /// <param name="name">Name of method</param>
        /// <param name="params">Parameters of method</param>
        /// <returns>Retrn value of invoked method</returns>
        [Obsolete("Do not use this method! This is only CodeDOM replacement for PHP indirect call of static method.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object CallIndirectStatic(Type type, string name, params object[] @params)
        {
            return type.InvokeMember(name, BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Static, null, null, @params);
        }
        /// <summary>Indirectly calls instance method. Intended only to be CodeDOM replacement for PHP indirect instance method call</summary>
        /// <param name="instance">Instance to invoke method on</param>
        /// <param name="name">Name of method</param>
        /// <param name="params">Parameters of method</param>
        /// <returns>Retrn value of invoked method</returns>
        [Obsolete("Do not use this method! This is only CodeDOM replacement for PHP indirect call of instance method.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object CallIndirectInstance(object instance, string name, params object[] @params)
        {
            return instance.GetType().InvokeMember(name, BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Instance, null, instance, @params);
        }
        /// <summary>CodeDOM replacement for PHP conditional operator ?:</summary>
        /// <param name="Condition">Boolean expression</param>
        /// <param name="True">Returned whne <paramref name="Condition"/> is True</param>
        /// <param name="False">Returned when <paramref name="Condition"/> is False</param>
        /// <returns><paramref name="True"/> or <paramref name="False"/> depending on <paramref name="Condition"/></returns>
        [Obsolete("Do not use this method! This is only CodeDOM replacement for PHP conditional operator ?:")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object iif(bool Condition, Object True, Object False)
        {
            return Condition ? True : False;
        }
        /// <summary>Returns first argument</summary>
        /// <param name="First">Argument being returned</param>
        /// <param name="Second">No meaning</param>
        /// <returns><paramref name="First"/></returns>
        [Obsolete("Do not use this method! This is only CodeDOM helper method for fimulation of PHP post-incrementation / post-decrementation.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object ReturnFirst(object First, object Second)
        {
            return First;
        }
        /// <summary>Returns null</summary>
        /// <param name="anything">Anything, no meaning</param>
        /// <returns>null</returns>
        [Obsolete("Do not use this method! This is only CodeDOM helper for unset cast")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object ReturnNull(object anything)
        {
            return null;
        }
        /// <summary>Echos parameter and returns 1</summary>
        /// <param name="ToPrint">String to echo</param>
        /// <param name="PHPContext">COntext to echo <paramref name="ToPrint"/> through</param>
        /// <returns>1</returns>
        [Obsolete("Do not use this method! This is only CodeDOM helper for print operator.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static int Print(string ToPrint, ScriptContext /*!*/ PHPContext)
        {
            ScriptContext.Echo(ToPrint, PHPContext);
            return 1;
        }
        /// <summary>Returns its parameter</summary>
        /// <param name="obj">Parameter to return</param>
        /// <returns><paramref name="obj"/></returns>
        [Obsolete("Do not use this method! This is only CodeDOM placeholder for PHP @ operator")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object NoError(object obj)
        {
            return obj;
        }
    }
}