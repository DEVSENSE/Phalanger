/*

 Copyright (c) 2005-2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Runtime.Serialization;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

using PHP.Core;
using PHP.Core.Reflection;

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
        protected DTypeDesc typedesc;

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
                return (typedesc != null) ? typedesc.MakeFullName() : null;
            }
            //set   // DPhpFieldDesc.Set does not support properties properly yet
            //{
            //    Exception.ThrowSplException(
            //        _ctx => new ReflectionException(_ctx, true),
            //        ScriptContext.CurrentContext,
            //        string.Format(CoreResources.readonly_property_written, "ReflectionClass", "name"), 0, null);
            //}
        }

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
        public object __construct(ScriptContext context, object arg)
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
        public object newInstance(ScriptContext/*!*/context)
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
        public object getName(ScriptContext/*!*/context)
        {
            return this.name;
        }

        #endregion

        #region Reflector

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object __toString(object instance, PhpStack stack)
        {
            return ((ReflectionClass)instance).__toString(stack.Context);
        }

        [ImplementsMethod]
        public object __toString(ScriptContext/*!*/context)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region hasMethod, hasConstant

        [ImplementsMethod]
        public object hasMethod(ScriptContext/*!*/context, object argName)
        {
            var name = new Name(PHP.Core.Convert.ObjectToString(argName));

            for (var type = this.typedesc; type != null; type = type.Base)
                if (type.Methods.ContainsKey(name))
                    return true;;

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
        public object hasConstant(ScriptContext/*!*/context, object argName)
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

        #endregion

        #region getFileName

        [ImplementsMethod]
        public object getFileName(ScriptContext/*!*/context)
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

        #region getStaticPropertyValue, getConstant, getConstants

        [ImplementsMethod]
        public object getStaticPropertyValue(ScriptContext/*!*/context, object argName, [Optional]object argDefault)
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
        public object getConstant(ScriptContext context, object argName)
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

        public object getConstants(ScriptContext context)
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
    }
}

/*
namespace PHP.Library.SPL
{
/// <summary>
/// 
/// </summary>
/// <remarks>
/// <para>
/// <code>
/// class ReflectionClass implements Reflector
/// { 
///   
///   public name;
///   
///   final private __clone();
///   public static export();
///   public __construct(string name);
///   public __toString();
///   public getName();
///   public isInternal();
///   public isUserDefined();
///   public isInstantiable();
///   public getFileName();
///   public getStartLine();
///   public getEndLine();
///   public getDocComment();
///   public getConstructor();
///   public hasMethod(string name);
///   public getMethod(string name);
///   public getMethods();
///   public hasProperty(string name);
///   public getProperty(string name);
///   public getProperties();
///   public hasConstant(string name);
///   public getConstants();
///   public getConstant(string name);
///   public getInterfaces();
///   public isInterface();
///   public isAbstract();
///   public isFinal();
///   public getModifiers();
///   public isInstance(stdclass object);
///   public newInstance(mixed* args);
///   public getParentClass();
///   public isSubclassOf(ReflectionClass class);
///   public getStaticProperties();
///   public getStaticPropertyValue(string name [, mixed default]);
///   public setStaticPropertyValue(string name, mixed value);
///   public getDefaultProperties();
///   public isIterateable();
///   public implementsInterface(string name);
///   public getExtension();
///   public getExtensionName();  
/// }
/// </code>
/// </para>
/// </remarks>
[Serializable, ImplementsType]
class ReflectionClass : PhpObject, Reflector
{
 #region PHP Fields

 /// <summary>
 /// 
 /// </summary>
 public PhpReference name = new PhpSmartReference();

 #endregion
    
 #region PHP Methods

 /// <summary>
 /// 
 /// </summary>
 private object __clone()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public static object export()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public object __construct()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public object __toString()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public object getName()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public object isInternal()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public object isUserDefined()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public object isInstantiable()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public object getFileName()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public object getStartLine()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public object getEndLine()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public object getDocComment()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public object getConstructor()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public object hasMethod()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public object getMethod()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public object getMethods()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public object hasProperty()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public object getProperty()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public object getProperties()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public object hasConstant()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public object getConstants()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public object getConstant()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public object getInterfaces()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public object isInterface()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public object isAbstract()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public object isFinal()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public object getModifiers()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public object isInstance()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public object newInstance()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public object getParentClass()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public object isSubclassOf()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public object getStaticProperties()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public object getStaticPropertyValue()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public object setStaticPropertyValue()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public object getDefaultProperties()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public object isIterateable()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public object implementsInterface()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public object getExtension()
 {
	 // TODO: write body
	 return null;
 }

 /// <summary>
 /// 
 /// </summary>
 public object getExtensionName()
 {
	 // TODO: write body
	 return null;
 }

 #endregion
    
 #region Implementation Details
    
 /// <summary>
 /// The method table.
 /// </summary>
 private static volatile PhpMethodTable methodTable;

 /// <summary>
 /// The field table.
 /// </summary>
 private static volatile PhpFieldTable fieldTable;

 /// <summary>
 /// Returns the method table.
 /// </summary>
 public override IPhpMemberTable __GetMethodTable()
 {
	 if (methodTable == null)
	 {
		 Type type = typeof(PHP.Library.SPL.ReflectionClass);
		 lock (type)
		 {
			 if (methodTable == null)
			 {
				 methodTable = new PhpMethodTable(type, base.__GetMethodTable());
				 methodTable.AddMethod(type.GetMethod("__clone", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("export", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("__construct", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("__toString", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("getName", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("isInternal", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("isUserDefined", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("isInstantiable", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("getFileName", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("getStartLine", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("getEndLine", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("getDocComment", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("getConstructor", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("hasMethod", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("getMethod", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("getMethods", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("hasProperty", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("getProperty", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("getProperties", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("hasConstant", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("getConstants", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("getConstant", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("getInterfaces", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("isInterface", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("isAbstract", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("isFinal", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("getModifiers", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("isInstance", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("newInstance", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("getParentClass", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("isSubclassOf", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("getStaticProperties", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("getStaticPropertyValue", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("setStaticPropertyValue", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("getDefaultProperties", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("isIterateable", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("implementsInterface", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("getExtension", Emit.Types.PhpStack));
				 methodTable.AddMethod(type.GetMethod("getExtensionName", Emit.Types.PhpStack));
			 }
		 }
	 }
	 return methodTable;
 }

 /// <summary>
 /// Returns the field table.
 /// </summary>
 public override IPhpMemberTable __GetFieldTable()
 {
	 if (fieldTable == null)
	 {
		 Type type = typeof(PHP.Library.SPL.ReflectionClass);
		 lock (type)
		 {
			 if (fieldTable == null)
			 {
				 fieldTable = new PhpFieldTable(type, base.__GetFieldTable());
				 fieldTable.AddField(type.GetField("name", BindingFlags.Instance | BindingFlags.Public));
			 }
		 }
	 }      
	 return fieldTable;
 }

 /// <summary>
 /// For internal purposes only.
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public ReflectionClass(ScriptContext context, bool newInstance) : base(context, newInstance)
 {       
 }

 /// <summary>
 /// For internal purposes only.
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public ReflectionClass(ScriptContext context, RuntimeTypeHandle callingTypeHandle) : base(context, callingTypeHandle)
 { 		  
 }

 /// <summary>
 /// Deserializing constructor.
 /// </summary>
 protected ReflectionClass(SerializationInfo info, StreamingContext context) : base(info, context)
 { 
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 private object __clone(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return __clone();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public static object export(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return export();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public virtual object __construct(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return __construct();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public virtual object __toString(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return __toString();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public virtual object getName(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return getName();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public virtual object isInternal(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return isInternal();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public virtual object isUserDefined(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return isUserDefined();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public virtual object isInstantiable(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return isInstantiable();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public virtual object getFileName(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return getFileName();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public virtual object getStartLine(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return getStartLine();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public virtual object getEndLine(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return getEndLine();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public virtual object getDocComment(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return getDocComment();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public virtual object getConstructor(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return getConstructor();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public virtual object hasMethod(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return hasMethod();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public virtual object getMethod(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return getMethod();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public virtual object getMethods(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return getMethods();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public virtual object hasProperty(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return hasProperty();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public virtual object getProperty(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return getProperty();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public virtual object getProperties(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return getProperties();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public virtual object hasConstant(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return hasConstant();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public virtual object getConstants(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return getConstants();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public virtual object getConstant(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return getConstant();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public virtual object getInterfaces(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return getInterfaces();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public virtual object isInterface(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return isInterface();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public virtual object isAbstract(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return isAbstract();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public virtual object isFinal(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return isFinal();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public virtual object getModifiers(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return getModifiers();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public virtual object isInstance(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return isInstance();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public virtual object newInstance(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return newInstance();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public virtual object getParentClass(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return getParentClass();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public virtual object isSubclassOf(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return isSubclassOf();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public virtual object getStaticProperties(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return getStaticProperties();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public virtual object getStaticPropertyValue(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return getStaticPropertyValue();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public virtual object setStaticPropertyValue(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return setStaticPropertyValue();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public virtual object getDefaultProperties(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return getDefaultProperties();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public virtual object isIterateable(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return isIterateable();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public virtual object implementsInterface(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return implementsInterface();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public virtual object getExtension(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return getExtension();
 }

 /// <summary>
 /// 
 /// </summary>
 [EditorBrowsable(EditorBrowsableState.Never)]
 public virtual object getExtensionName(PhpStack stack)
 {
	 stack.RemoveFrame();
	 return getExtensionName();
 }

 #endregion
}
}
*/