/*

 Copyright (c) 2005-2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Linq;
using System.Runtime.Serialization;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

using PHP.Core;
using PHP.Core.Reflection;
using System.Collections.Generic;
using System.Text;

namespace PHP.Library.SPL
{
    /// <summary>
    /// The ReflectionClass class reports information about a class.
    /// </summary>
    [Serializable]
    [ImplementsType]
    public class ReflectionClass : PhpObject, Reflector
    {
        /// <summary>
        /// Resolved <see cref="DTypeDesc"/> of reflected type.
        /// </summary>
        internal DTypeDesc typedesc;

        #region Constants

        /// <summary>
        /// Indicates class that is abstract because it has some abstract methods.
        /// </summary>
        public const int IS_IMPLICIT_ABSTRACT = 16;

        /// <summary>
        /// Indicates class that is abstract because of its definition.
        /// </summary>
        public const int IS_EXPLICIT_ABSTRACT = 32;

        /// <summary>
        /// Indicates final class.
        /// </summary>
        public const int IS_FINAL = 64;

        #endregion

        #region Properties

        /// <summary>
        /// Name of the class. Read-only, throws <see cref="ReflectionException"/> in attempt to write.
        /// </summary>
        [PhpVisible]
        public string name
        {
            get
            {
                if (this._name == null && typedesc != null)
                    this._name = typedesc.MakeFullName();

                return this._name;
            }
            //set   // DPhpFieldDesc.Set does not support properties properly yet
            //{
            //    Exception.ThrowSplException(
            //        _ctx => new ReflectionException(_ctx, true),
            //        ScriptContext.CurrentContext,
            //        string.Format(CoreResources.readonly_property_written, "ReflectionClass", "name"), 0, null);
            //}
        }
        private string _name = null;

        #endregion

        #region Constructor
        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ReflectionClass(ScriptContext/*!*/context, bool newInstance)
            : base(context, newInstance)
        { }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ReflectionClass(ScriptContext/*!*/context, DTypeDesc caller)
            : base(context, caller)
        { }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object __construct(object instance, PhpStack stack)
        {
            object argument = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ReflectionClass)instance).__construct(stack.Context, argument);
        }

        [ImplementsMethod]
        public virtual object __construct(ScriptContext context, object arg)
        {
            DObject dobj;

            if ((dobj = arg as DObject) != null)
            {
                typedesc = dobj.TypeDesc;
            }
            else
            {
                // namespaces are ignored in runtime
                // any value except DObject is converted to string
                typedesc = ResolveType(context, PHP.Core.Convert.ObjectToString(arg));
            }

            return null;
        }

        /// <summary>
        /// Resolves the <paramref name="typeName"/> and provides corresponding <see cref="DTypeDesc"/> or <c>null</c> reference.
        /// </summary>
        private static DTypeDesc ResolveType(ScriptContext/*!*/context, string typeName)
        {
            return context.ResolveType(typeName, null, null, null, ResolveTypeFlags.ThrowErrors | ResolveTypeFlags.UseAutoload);
        }

        #endregion

        #region ReflectionClass

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object newInstance(object instance, PhpStack stack)
        {
            var self = (ReflectionClass)instance;
            if (self.typedesc == null)
            {
                stack.RemoveFrame();
                return null;
            }

            // preserve arguments on stack for New

            // instantiate the object, checks whether typedesc is an abstract type:
            return Operators.New(self.typedesc, null, stack.Context, null);
        }

        /// <summary>
        /// Creates a new instance of the class. The given arguments are passed to the class constructor.
        /// </summary>
        /// <param name="context">Current context.</param>
        /// <returns>Returns a new instance of the class.</returns>
        [ImplementsMethod]
        [NeedsArgless]
        public virtual object newInstance(ScriptContext/*!*/context)
        {
            // this method should not be called, its argless should.
            throw new InvalidOperationException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object newInstanceArgs(object instance, PhpStack stack)
        {
            object args = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ReflectionClass)instance).newInstanceArgs(stack.Context, args);
        }

        /// <summary>
        /// Creates a new instance of the class, the given arguments are passed to the class constructor.
        /// </summary>
        /// <param name="context">Current context.</param>
        /// <param name="arg">The parameters to be passed to the class constructor as an <see cref="PhpArray"/>.</param>
        /// <returns>Returns a new instance of the class.</returns>
        [ImplementsMethod]
        public object newInstanceArgs(ScriptContext/*!*/context, object arg)
        {
            if (this.typedesc == null)
                return null;

            // push arguments onto the stack:
            var array = PhpArray.AsPhpArray(arg);

            if (array != null)
            {
                var args = new object[array.Count];
                array.CopyValuesTo(args, 0);
                context.Stack.AddFrame(args);
            }
            else
            {
                PhpException.InvalidArgumentType("arg", PhpArray.PhpTypeName);
                return null;
            }

            //
            return Operators.New(typedesc, null, context, null);
        }

        #endregion

        #region getName, inNamespace, getNamespaceName, getShortName

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getName(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionClass)instance).getName(stack.Context);
        }

        /// <summary>
        /// Gets the class name.
        /// </summary>
        [ImplementsMethod]
        public virtual object getName(ScriptContext/*!*/context)
        {
            return this.name;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object inNamespace(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionClass)instance).inNamespace(stack.Context);
        }

        /// <summary>
        /// Checks if this class is defined in a namespace.
        /// </summary>
        [ImplementsMethod]
        public virtual object inNamespace(ScriptContext/*!*/context)
        {
            var name = this.name;
            return name != null && name.IndexOf(QualifiedName.Separator) != -1;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getNamespaceName(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionClass)instance).getNamespaceName(stack.Context);
        }

        /// <summary>
        /// Gets the namespace name or an empty string if the class is not defined in a namespace.
        /// </summary>
        [ImplementsMethod]
        public virtual object getNamespaceName(ScriptContext/*!*/context)
        {
            var name = this.name;
            int lastSeparatorIndex;
            if (name != null && (lastSeparatorIndex = name.LastIndexOf(QualifiedName.Separator)) != -1)
            {
                return name.Remove(lastSeparatorIndex);
            }

            return string.Empty;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getShortName(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionClass)instance).getShortName(stack.Context);
        }

        /// <summary>
        /// Gets the short name of the class, the part without the namespace.
        /// </summary>
        [ImplementsMethod]
        public virtual object getShortName(ScriptContext/*!*/context)
        {
            var name = this.name;
            int lastSeparatorIndex;
            if (name != null && (lastSeparatorIndex = name.LastIndexOf(QualifiedName.Separator)) != -1)
            {
                return name.Substring(lastSeparatorIndex + 1);
            }

            return name;
        }

        #endregion

        #region Reflector

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object __toString(object instance, PhpStack stack)
        {
            return ((ReflectionClass)instance).__toString(stack.Context);
        }

        [ImplementsMethod]
        public virtual object __toString(ScriptContext/*!*/context)
        {
            if (this.typedesc == null)
                return false;

            StringBuilder result = new StringBuilder();

            // Interface|Class
            result.Append(this.typedesc.IsInterface ? "Interface [ interface " : "Class [ class ");
            result.Append(this.name);
            result.Append(' ');
            if (this.typedesc.Base != null)
            {
                result.Append("extends ");
                result.Append(this.typedesc.Base.MakeFullName());
                result.Append(' ');
            }
            if (this.typedesc.Interfaces.Length > 0)
            {
                result.Append("implements ");
                result.Append(string.Join(", ", this.typedesc.Interfaces.Select(x => x.MakeFullName())));
                result.Append(' ');
            }
            result.AppendLine("] {");

            // @@ filename
            var fname = this.getFileName(context);
            if (fname is string)
                result.AppendFormat("  @@ {0}\n", (string)fname);

            // Constants
            result.AppendLine();
            result.AppendFormat("  - Constants [{0}] {{\n", this.typedesc.Constants.Count);
            foreach (var cnst in this.typedesc.Constants)
            {
                var cnst_value = cnst.Value.GetValue(context);
                result.AppendFormat("    Constant [ {0} {1} ] {{ {2} }}\n",
                    PhpVariable.GetTypeName(cnst_value),
                    cnst.Key.Value,
                    Core.Convert.ObjectToString(cnst_value));
            }
            result.AppendLine("  }");

            // Static properties
            var static_properties = this.typedesc.Properties.Where(x => x.Value.IsStatic).ToList();
            result.AppendLine();
            result.AppendFormat("  - Static properties [{0}] {{\n", static_properties.Count);
            foreach (var prop in static_properties)
            {
                result.AppendFormat("    Property [ {0} static ${1} ]\n",
                    VisibilityString(prop.Value.MemberAttributes),
                    prop.Key.Value);
            }
            result.AppendLine("  }");

            // Static methods
            var static_methods = this.typedesc.Methods.Where(x => x.Value.IsStatic).ToList();
            result.AppendLine();
            result.AppendFormat("  - Static methods [{0}] {{\n", static_methods.Count);
            foreach (var mtd in static_methods)
            {
                result.AppendFormat("    Method [ static {0} method {1} ] {{}}\n",
                    VisibilityString(mtd.Value.MemberAttributes),
                    mtd.Key.Value);
                // TODO: @@ fname position
            }
            result.AppendLine("  }");

            // Properties
            var properties = this.typedesc.Properties.Where(x => !x.Value.IsStatic).ToList();
            result.AppendLine();
            result.AppendFormat("  - Properties [{0}] {{\n", properties.Count);
            foreach (var prop in properties)
            {
                result.AppendFormat("    Property [ {0} ${1} ]\n",
                    VisibilityString(prop.Value.MemberAttributes),
                    prop.Key.Value);
            }
            result.AppendLine("  }");

            // Methods
            var methods = this.typedesc.Methods.Where(x => !x.Value.IsStatic).ToList();
            result.AppendLine();
            result.AppendFormat("  - Methods [{0}] {{\n", methods.Count);
            foreach (var mtd in static_methods)
            {
                result.AppendFormat("    Method [ {0} method {1} ] {{}}\n",
                    VisibilityString(mtd.Value.MemberAttributes),
                    mtd.Key.Value);
                // TODO: @@ fname position
            }
            result.AppendLine("  }");

            // }
            result.AppendLine("}");
            
            //
            return result.ToString();
        }

        private static string VisibilityString(PhpMemberAttributes attrs)
        {
            var visibility = attrs & PhpMemberAttributes.VisibilityMask;
            switch (visibility)
            {
                case PhpMemberAttributes.Public:
                    return "public";
                default:
                    return visibility.ToString().ToLowerInvariant();
            }
        }

        #endregion

        #region hasMethod, hasConstant, hasProperty

        [ImplementsMethod]
        public virtual object hasMethod(ScriptContext/*!*/context, object argName)
        {
            var name = new Name(PHP.Core.Convert.ObjectToString(argName));

            for (var type = this.typedesc; type != null; type = type.Base)
                if (type.Methods.ContainsKey(name))
                    return true;

            return false;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object hasMethod(object instance, PhpStack stack)
        {
            object args = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ReflectionClass)instance).hasMethod(stack.Context, args);
        }

        [ImplementsMethod]
        public virtual object hasConstant(ScriptContext/*!*/context, object argName)
        {
            var name = new VariableName(PHP.Core.Convert.ObjectToString(argName));

            for (var type = this.typedesc; type != null; type = type.Base)
                if (type.Constants.ContainsKey(name))
                    return true;

            return false;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object hasConstant(object instance, PhpStack stack)
        {
            object args = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ReflectionClass)instance).hasConstant(stack.Context, args);
        }

        [ImplementsMethod]
        public virtual object hasProperty(ScriptContext/*!*/context, object argName)
        {
            if (this.typedesc == null)
                return false;

            var name = new VariableName(PHP.Core.Convert.ObjectToString(argName));

            for (var type = this.typedesc; type != null; type = type.Base)
                if (type.Properties.ContainsKey(name))
                    return true;

            return false;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object hasProperty(object instance, PhpStack stack)
        {
            object args = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ReflectionClass)instance).hasProperty(stack.Context, args);
        }

        #endregion

        #region getFileName

        [ImplementsMethod]
        public virtual object getFileName(ScriptContext/*!*/context)
        {
            int id;
            string typename;
            string src;
            ReflectionUtils.ParseTypeId(this.typedesc.RealType.FullName, out id, out src, out typename);
            if (src == null)
                return false;

            return System.IO.Path.Combine(
                Configuration.Application.Compiler.SourceRoot.FullFileName,
                src);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getFileName(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionClass)instance).getFileName(stack.Context);
        }

        #endregion

        #region getStaticPropertyValue, setStaticPropertyValue, getConstant, getConstants

        [ImplementsMethod]
        public virtual object getStaticPropertyValue(ScriptContext/*!*/context, object argName, [Optional]object argDefault)
        {
            string name = PHP.Core.Convert.ObjectToString(argName);
            return Operators.GetStaticProperty(this.typedesc, argName, this.TypeDesc, context, false);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getStaticPropertyValue(object instance, PhpStack stack)
        {
            object argName = stack.PeekValue(1);
            object argDefault = stack.PeekValueOptional(2);
            stack.RemoveFrame();
            return ((ReflectionClass)instance).getStaticPropertyValue(stack.Context, argName, argDefault);
        }

        [ImplementsMethod]
        public virtual object setStaticPropertyValue(ScriptContext/*!*/context, object argName, object value)
        {
            string name = PHP.Core.Convert.ObjectToString(argName);
            Operators.SetStaticProperty(this.typedesc, argName, value, null, context);
            return null;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object setStaticPropertyValue(object instance, PhpStack stack)
        {
            object argName = stack.PeekValue(1);
            object argValue = stack.PeekValue(2);
            stack.RemoveFrame();
            return ((ReflectionClass)instance).setStaticPropertyValue(stack.Context, argName, argValue);
        }

        [ImplementsMethod]
        public virtual object getConstant(ScriptContext context, object argName)
        {
            string name = PHP.Core.Convert.ObjectToString(argName);
            return Operators.GetClassConstant(this.typedesc, name, this.TypeDesc, context);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getConstant(object instance, PhpStack stack)
        {
            object argName = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ReflectionClass)instance).getConstant(stack.Context, argName);
        }

        [ImplementsMethod]
        public virtual object getConstants(ScriptContext context)
        {
            PhpArray arr = new PhpArray(this.typedesc.Constants.Count);
            foreach (var c in this.typedesc.Constants)
                arr.Add(c.Key.Value, c.Value.GetValue(context));
            
            return arr;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getConstants(object instance, PhpStack stack)
        {
            object argName = stack.PeekValue(1);
            return ((ReflectionClass)instance).getConstants(stack.Context);
        }

        #endregion

        #region getInterfaceNames, getParentClass, getInterfaces, implementsInterface

        [ImplementsMethod]
        public virtual object getInterfaceNames(ScriptContext/*!*/context)
        {
            if (typedesc == null)
                return false;

            return new PhpArray(typedesc.Interfaces.Select(x => x.MakeFullName()));
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getInterfaceNames(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionClass)instance).getInterfaceNames(stack.Context);
        }

        [ImplementsMethod]
        public virtual object getParentClass(ScriptContext/*!*/context)
        {
            if (typedesc == null || typedesc.Base == null)
                return false;

            // construct new ReflectionClass with resolved TypeDesc
            return new ReflectionClass(context, true)
            {
                typedesc = this.typedesc.Base
            };
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getParentClass(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionClass)instance).getParentClass(stack.Context);
        }

        [ImplementsMethod]
        public virtual object getInterfaces(ScriptContext/*!*/context)
        {
            if (typedesc == null)
                return false;

            var ifaces = typedesc.Interfaces;

            PhpArray result = new PhpArray(ifaces.Length);
            foreach (var ifacedesc in ifaces)
            {
                result.Add(
                    ifacedesc.MakeFullName(),
                    new ReflectionClass(context, true)
                    {
                        typedesc = ifacedesc,
                    });
            }
            return result;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getInterfaces(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionClass)instance).getInterfaces(stack.Context);
        }

        [ImplementsMethod]
        public virtual object implementsInterface(ScriptContext/*!*/context, object ifacename)
        {
            if (typedesc == null)
                return false;

            var ifacenamestr = Core.Convert.ObjectToString(ifacename);
            if (string.IsNullOrEmpty(ifacenamestr))
            {
                //PhpException.InvalidArgument("ifacename"); // ?
                return false;
            }

            var ifaces = typedesc.Interfaces;

            foreach (var ifacedesc in ifaces)
                if (ifacedesc.MakeFullName().EqualsOrdinalIgnoreCase(ifacenamestr))
                    return true;

            return false;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object implementsInterface(object instance, PhpStack stack)
        {
            var ifacename = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ReflectionClass)instance).implementsInterface(stack.Context, ifacename);
        }

        #endregion

        #region getConstructor, getMethods, getProperties, getProperty, getStaticProperties

        [ImplementsMethod]
        public virtual object getConstructor(ScriptContext/*!*/context)
        {
            if (typedesc == null)
                return false;

            DRoutineDesc method;

            if (typedesc.GetMethod(DObject.SpecialMethodNames.Construct, null, out method) == GetMemberResult.NotFound)
                return false;

            // construct new ReflectionClass with resolved TypeDesc
            return new ReflectionMethod(context, true)
            {
                dtype = typedesc,
                method = method,
            };
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getConstructor(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionClass)instance).getConstructor(stack.Context);
        }

        [ImplementsMethod]
        public virtual object getMethod(ScriptContext/*!*/context, object name)
        {
            if (typedesc == null)
                return false;

            var nameStr = PhpVariable.AsString(name);
            if (string.IsNullOrEmpty(nameStr))
                return false;

            DRoutineDesc method;
            if (typedesc.GetMethod(new Name(nameStr), typedesc, out method) == GetMemberResult.NotFound)
                return false;

            return new ReflectionMethod(context, true)
            {
                dtype = method.DeclaringType,
                method = method,
            };
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getMethod(object instance, PhpStack stack)
        {
            var name = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ReflectionClass)instance).getMethod(stack.Context, name);
        }

        [ImplementsMethod]
        public virtual object getMethods(ScriptContext/*!*/context, object filter = null)
        {
            if (typedesc == null)
                return false;

            if (filter != null && filter != Arg.Default)
                PhpException.ArgumentValueNotSupported("filter", filter);

            PhpArray result = new PhpArray();
            foreach (KeyValuePair<Name, DRoutineDesc> method in typedesc.EnumerateMethods())
            {
                result.Add(new ReflectionMethod(context, true)
                {
                    dtype = method.Value.DeclaringType,
                    method = method.Value,
                });
            }
            return result;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getMethods(object instance, PhpStack stack)
        {
            var filter = stack.PeekValueOptional(1);
            stack.RemoveFrame();
            return ((ReflectionClass)instance).getMethods(stack.Context, filter);
        }

        [ImplementsMethod]
        public virtual object getStaticProperties(ScriptContext/*!*/context)
        {
            if (typedesc == null)
                return false;

            PhpArray result = new PhpArray();

            foreach (var prop in typedesc.Properties.Where(x => x.Value.IsStatic))
                result.Add(prop.Key.Value, prop.Value.Get(null));
            
            return result;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getStaticProperties(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionClass)instance).getStaticProperties(stack.Context);
        }

        [ImplementsMethod]
        public virtual object getProperties(ScriptContext/*!*/context, object filter = null)
        {
            if (typedesc == null)
                return false;

            PhpArray result = new PhpArray(typedesc.Properties.Count);

            foreach (var prop in typedesc.Properties)
            {
                result.Add(new ReflectionProperty(context, true)
                {
                    dtype = prop.Value.DeclaringType,
                    property = prop.Value,
                });
            }

            return result;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getProperties(object instance, PhpStack stack)
        {
            var filter = stack.PeekValueOptional(1);
            stack.RemoveFrame();
            return ((ReflectionClass)instance).getProperties(stack.Context, filter);
        }

        [ImplementsMethod]
        public virtual object getProperty(ScriptContext/*!*/context, object name)
        {
            if (typedesc == null)
                return false;

            DPropertyDesc prop;
            var namestr = Core.Convert.ObjectToString(name);
            if (typedesc.Properties.TryGetValue(new VariableName(namestr), out prop))
            {
                return new ReflectionProperty(context, true)
                {
                    dtype = prop.DeclaringType,
                    property = prop,
                };
            }
            else
            {
                return false;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getProperty(object instance, PhpStack stack)
        {
            var name = stack.PeekValueOptional(1);
            stack.RemoveFrame();
            return ((ReflectionClass)instance).getProperty(stack.Context, name);
        }

        #endregion

        #region getModifiers

        [ImplementsMethod]
        public virtual object/*int*/getModifiers(ScriptContext context)
        {
            if (typedesc == null)
                return false;

            int result = 0;

            if (typedesc.IsAbstract) result |= IS_EXPLICIT_ABSTRACT;
            if (typedesc.IsFinal) result |= IS_FINAL;
            
            return result;
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getModifiers(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionClass)instance).getModifiers(stack.Context);
        }

        #endregion

        #region isAbstract, isFinal, isCloneable, isInstance, isInstantiable, isInterface, isInternal, isIterateable, isSubclassOf, isTrait, isUserDefined

        [ImplementsMethod]
        public virtual object isAbstract(ScriptContext/*!*/context)
        {
            return this.typedesc != null && this.typedesc.IsAbstract;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object isAbstract(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionClass)instance).isAbstract(stack.Context);
        }

        [ImplementsMethod]
        public virtual object isFinal(ScriptContext/*!*/context)
        {
            return this.typedesc != null && this.typedesc.IsFinal;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object isFinal(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionClass)instance).isFinal(stack.Context);
        }

        [ImplementsMethod]
        public virtual object isCloneable(ScriptContext/*!*/context)
        {
            var desc = this.typedesc;
            DRoutineDesc m;
            return
                desc != null &&
                ((m = desc.GetMethod(DObject.SpecialMethodNames.Clone)) == null || !m.IsPrivate);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object isCloneable(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionClass)instance).isCloneable(stack.Context);
        }

        [ImplementsMethod]
        public virtual object isInstance(ScriptContext/*!*/context, object obj)
        {
            var dobj = obj as DObject;
            if (dobj == null)
                PhpException.InvalidArgument("obj");

            return this.typedesc != null && dobj != null && dobj.TypeDesc.IsAssignableFrom(this.typedesc);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object isInstance(object instance, PhpStack stack)
        {
            var obj = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ReflectionClass)instance).isInstance(stack.Context, obj);
        }

        [ImplementsMethod]
        public virtual object isInstantiable(ScriptContext/*!*/context)
        {
            var desc = this.typedesc;
            DRoutineDesc ctor;

            return desc != null &&
                !desc.IsInterface &&
                !desc.IsAbstract &&
                ((ctor = desc.GetMethod(DObject.SpecialMethodNames.Construct)) == null || !ctor.IsPrivate);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object isInstantiable(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionClass)instance).isInstantiable(stack.Context);
        }

        [ImplementsMethod]
        public virtual object isInterface(ScriptContext/*!*/context)
        {
            return this.typedesc != null && this.typedesc.IsInterface;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object isInterface(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionClass)instance).isInterface(stack.Context);
        }

        [ImplementsMethod]
        public virtual object isInternal(ScriptContext/*!*/context)
        {
            return this.typedesc != null && this.typedesc.RealType.Assembly == typeof(ReflectionClass).Assembly;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object isInternal(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionClass)instance).isInternal(stack.Context);
        }

        [ImplementsMethod]
        public virtual object isIterateable(ScriptContext/*!*/context)
        {
            var iteratordesc = DTypeDesc.Create(typeof(SPL.Iterator));
            return this.typedesc != null && iteratordesc.IsAssignableFrom(this.typedesc);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object isIterateable(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionClass)instance).isIterateable(stack.Context);
        }

        [ImplementsMethod]
        public virtual object isUserDefined(ScriptContext/*!*/context)
        {
            return !(bool)isInternal(context);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object isUserDefined(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionClass)instance).isUserDefined(stack.Context);
        }

        [ImplementsMethod]
        public virtual object isTrait(ScriptContext/*!*/context)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object isTrait(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionClass)instance).isTrait(stack.Context);
        }

        [ImplementsMethod]
        public virtual object isSubclassOf(ScriptContext/*!*/context, object @class)
        {
            var classname = PhpVariable.AsString(@class);

            if (!string.IsNullOrEmpty(classname) && this.typedesc != null)
            {
                var dtype = context.ResolveType(classname, null, null, null, ResolveTypeFlags.ThrowErrors | ResolveTypeFlags.UseAutoload);
                return dtype != null && this.typedesc.IsAssignableFrom(dtype);
            }

            return false;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object isSubclassOf(object instance, PhpStack stack)
        {
            var @class = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ReflectionClass)instance).isSubclassOf(stack.Context, @class);
        }

        #endregion
    }
}