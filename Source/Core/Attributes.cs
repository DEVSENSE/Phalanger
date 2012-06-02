/*

 Copyright (c) 2004-2006 Tomas Matousek and Ladislav Prosek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections;
using System.Diagnostics;
using PHP.Core.Reflection;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using System.Linq;
using PHP.Core.Emit;

#if SILVERLIGHT
using PHP.CoreCLR;
#endif

namespace PHP.Core
{
	#region Assemblies

	[Serializable]
	public abstract class DAssemblyAttribute : Attribute
	{
		internal static DAssemblyAttribute Reflect(Assembly/*!*/ assembly)
		{
#if !SILVERLIGHT
			Debug.Assert(!assembly.ReflectionOnly);
#endif
			object[] attrs = assembly.GetCustomAttributes(typeof(DAssemblyAttribute), false);
			return (attrs.Length == 1) ? (DAssemblyAttribute)attrs[0] : null;
		}
	}

	[Serializable]
	public abstract class PhpAssemblyAttribute : DAssemblyAttribute
	{
	}

	/// <summary>
	/// Identifies PHP library assembly or extension.
	/// </summary>
	[Serializable]
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
	public class PhpLibraryAttribute : DAssemblyAttribute
	{
		/// <summary>
		/// Name of the type describing the assembly. 
		/// Either a name of the type in the declaring assembly, a fully qualified name containing an assembly name
		/// or a <B>null</B> reference if a default descriptor can be used.
		/// </summary>
		public Type Descriptor { get { return descriptor; } }
        private readonly Type descriptor;

		public bool IsPure { get { return isPure; } }
		private readonly bool isPure;

		public bool ContainsDynamicStubs { get { return containsDynamicStubs; } }
        private readonly bool containsDynamicStubs;

		public string/*!*/ Name { get { return name; } }
        private readonly string/*!*/ name;

        public string[]/*!*/ ImplementsExtensions { get { return implementsExtensions; } }
        private readonly string[]/*!*/ implementsExtensions;

        /// <summary>
        /// Used by hand-written libraries in PHP.
        /// </summary>
        /// <param name="descriptor">The type derived from <c>PhpLibraryDescriptor</c> class. Can be null to use default implementation.</param>
        /// <param name="name">The human readable name of the extension.</param>
        /// <remarks>List of implemented extensions <c>ImplementsExtensions</c> is an empty array. Extensions using
        /// this attribute does not populate any list of implemented PHP extensions.</remarks>
        public PhpLibraryAttribute(Type descriptor, string/*!*/ name)
            : this(descriptor, name, ArrayUtils.EmptyStrings, false, false)
        {

        }

        /// <summary>
        /// Used by hand-written libraries.
        /// </summary>
        public PhpLibraryAttribute(Type descriptor, string/*!*/ name, string[]/*!*/implementsExtensions)
			: this(descriptor, name, implementsExtensions, false, false)
		{
			
		}

        /// <summary>
        /// Used by hand-written libraries.
        /// </summary>
        public PhpLibraryAttribute(Type descriptor, string/*!*/ name, string[]/*!*/implementsExtensions, bool isPure, bool containsDynamicStubs)
		{
            // descriptor can be null, default descriptor is used then, needed at least for the Extension PHP project
            //if (descriptor == null)
            //    throw new ArgumentNullException("descriptorName");
            if (descriptor != null && !typeof(PhpLibraryDescriptor).IsAssignableFrom(descriptor))
                throw new ArgumentNullException("descriptor", "The type must be derived from PHP.Core.PhpLibraryDescriptor class.");
			if (name == null)
				throw new ArgumentNullException("name");
            if (implementsExtensions == null)
                throw new ArgumentNullException("implementsExtensions");

			this.descriptor = descriptor;
			this.isPure = isPure;
			this.containsDynamicStubs = containsDynamicStubs;
			this.name = name;
            this.implementsExtensions = implementsExtensions;
		}

		/*
		 *  TODO: Not needed???
		 * 
		public static PhpLibraryAttribute/*!* / Reflect(CustomAttributeData/*!* / data)
		{
			if (data == null) 
				throw new ArgumentNullException("data");
				
			switch (data.ConstructorArguments.Count)
			{
				case 2: return new PhpLibraryAttribute(
					(Type)data.ConstructorArguments[0].Value,
					(string)data.ConstructorArguments[1].Value);
				case 3: return new PhpLibraryAttribute(
					(Type)data.ConstructorArguments[0].Value,
					(string)data.ConstructorArguments[1].Value,
					(bool)data.ConstructorArguments[2].Value, 
					(bool)data.ConstructorArguments[3].Value);
			}
			
			throw new ArgumentException();
		}*/
	}

    /// <summary>
	/// Identifies PHP extension written in PHP pure mode.
    /// It has only PHP literals as parameters and it has dynamic stubs contained already.
	/// </summary>
	[Serializable]
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
	public class PhpExtensionAttribute : PhpLibraryAttribute
    {
        public PhpExtensionAttribute(string/*!*/name)
            :base(null, name, ArrayUtils.EmptyStrings, false, true)
        {

        }
    }

    /// <summary>
	/// Marks Phalanger compiled PHP script assemblies.
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
	public sealed class ScriptAssemblyAttribute : PhpAssemblyAttribute
	{
		/// <summary>
		/// Determines whether there are multiple scripts stored in the assembly.
		/// </summary>
		public bool IsMultiScript { get { return isMultiScript; } }
		private readonly bool isMultiScript;

        /// <summary>
        /// <see cref="Type"/> of a <c>&lt;Script&gt;</c> class in case of SingleScriptAssembly.
        /// </summary>
        public Type SSAScriptType { get { return ssaScriptType; } }
        private readonly Type ssaScriptType;

		public ScriptAssemblyAttribute(bool isMultiScript, Type ssaScriptType)
		{
			this.isMultiScript = isMultiScript;
            this.ssaScriptType = ssaScriptType;
		}

		internal new static ScriptAssemblyAttribute Reflect(Assembly/*!*/ assembly)
		{
#if !SILVERLIGHT
			Debug.Assert(!assembly.ReflectionOnly);
#endif
			object[] attrs = assembly.GetCustomAttributes(typeof(ScriptAssemblyAttribute), false);
			return (attrs.Length == 1) ? (ScriptAssemblyAttribute)attrs[0] : null;
		}
	}

	/// <summary>
	/// Marks Phalanger compiled pure assemblies.
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
	public sealed class PurePhpAssemblyAttribute : PhpAssemblyAttribute
	{
		public string[]/*!*/ ReferencedAssemblies { get { return referencedAssemblies; } }
		private string[]/*!*/ referencedAssemblies;

		public PurePhpAssemblyAttribute(string[]/*!*/ referencedAssemblies)
		{
			this.referencedAssemblies = referencedAssemblies;
		}

		/* TODO: Not needed?
		public static PurePhpAssemblyAttribute/*!* / Reflect(CustomAttributeData/*!* / data)
		{
			if (data == null)
				throw new ArgumentNullException("data");

			switch (data.ConstructorArguments.Count)
			{
				case 1: return new PurePhpAssemblyAttribute((string[])data.ConstructorArguments[0].Value);
			}

			throw new ArgumentException();
		}*/

		internal new static PurePhpAssemblyAttribute Reflect(Assembly/*!*/ assembly)
		{
#if !SILVERLIGHT
			Debug.Assert(!assembly.ReflectionOnly);
#endif
			object[] attrs = assembly.GetCustomAttributes(typeof(PurePhpAssemblyAttribute), false);
			return (attrs.Length == 1) ? (PurePhpAssemblyAttribute)attrs[0] : null;
		}
	}

    /// <summary>
    /// Attribute marks an assembly as a plugin extending set of compiler and runtime features.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public sealed class PluginAssemblyAttribute : Attribute
    {
        public readonly Type/*!*/LoaderType;

        public PluginAssemblyAttribute(Type/*!*/loaderType)
        {
            Debug.Assert(loaderType != null);
            Debug.Assert(loaderType.GetMethod(PluginAssembly.LoaderMethod, BindingFlags.Public | BindingFlags.Static, null, PluginAssembly.LoaderMethodParameters, null) != null, "Plugin loader method cannot be found!");

            this.LoaderType = loaderType;
        }

        internal static IEnumerable<PluginAssemblyAttribute> Reflect(Assembly/*!*/ assembly)
        {
#if !SILVERLIGHT
            Debug.Assert(!assembly.ReflectionOnly);
#endif
            object[] attrs = assembly.GetCustomAttributes(typeof(PluginAssemblyAttribute), false);
            return (attrs != null && attrs.Length > 0) ? attrs.Cast<PluginAssemblyAttribute>() : null;
        }
    }


	#endregion

	#region Language

	/// <summary>
	/// Marks types of the Class Library which should be viewed as PHP classes or interfaces.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
	public sealed class ImplementsTypeAttribute : Attribute
	{
        /// <summary>
        /// If not <c>null</c>, defines the PHP type name instead of the reflected name. CLR notation of namespaces.
        /// </summary>
        public readonly string PHPTypeName;

        /// <summary>
        /// Initialized new instance of <see cref="ImplementsTypeAttribute"/> specifying that
        /// the type is visible in PHP code and the type is named using the reflected <see cref="Type.FullName"/>.
        /// </summary>
		public ImplementsTypeAttribute()
		{
		}

        /// <summary>
        /// Initialized new instance of <see cref="ImplementsTypeAttribute"/> with PHP type name specified.
        /// </summary>
        /// <param name="PHPTypeName">If not <c>null</c>, defines the PHP type name instead of the reflected name. Uses CLR notation of namespaces.</param>
        /// <remarks>This overload is only valid within class library types.</remarks>
        public ImplementsTypeAttribute(string PHPTypeName)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(PHPTypeName));
            Debug.Assert(!PHPTypeName.Contains(QualifiedName.Separator));
            
            this.PHPTypeName = PHPTypeName;
        }

        internal static ImplementsTypeAttribute Reflect(Type/*!*/type)
        {
            Debug.Assert(type != null);
            var attrs = type.GetCustomAttributes(typeof(ImplementsTypeAttribute), false);
            return (attrs != null && attrs.Length == 1) ? (ImplementsTypeAttribute)attrs[0] : null;
        }
	}

	/// <summary>
	/// An attibute storing PHP formal argument type hints.
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.GenericParameter, AllowMultiple = false, Inherited = false)]
	public sealed class DTypeSpecAttribute : Attribute
	{
		internal DTypeSpec TypeSpec { get { return typeSpec; } }
		private DTypeSpec typeSpec;

		public DTypeSpecAttribute(int data0, int data1)
		{
			typeSpec = new DTypeSpec(new int[] { data0, data1 });
		}

		public DTypeSpecAttribute(int[]/*!*/ data)
		{
			typeSpec = new DTypeSpec(data);
		}

		public DTypeSpecAttribute(int data0, int data1, byte[]/*!*/ strings)
		{
			typeSpec = new DTypeSpec(new int[] { data0, data1 }, strings);
		}

		public DTypeSpecAttribute(int[]/*!*/ data, byte[]/*!*/ strings)
		{
			typeSpec = new DTypeSpec(data, strings);
		}

		internal static DTypeSpecAttribute Reflect(ICustomAttributeProvider/*!*/ parameterInfo)
		{
			object[] attrs = parameterInfo.GetCustomAttributes(typeof(DTypeSpecAttribute), false);
			return (attrs.Length == 1) ? (DTypeSpecAttribute)attrs[0] : null;
		}
	}

	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public sealed class RoutineAttribute : Attribute
	{
		private RoutineProperties properties;

		public RoutineAttribute(RoutineProperties properties)
		{
			this.properties = properties;
		}

		public RoutineProperties Properties { get { return properties; } }
	}

	/// <summary>
	/// Marks namespace-private PHP types and functions.
	/// </summary>
	/// <remarks>Attribute is used by <see cref="Reflection"/>.</remarks>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public sealed class PhpNamespacePrivateAttribute : Attribute
	{
	}

	/// <summary>
	/// CLI does not allow static final methods. If a static method is declared as
	/// final, it is marked with this attribute.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public sealed class PhpFinalAttribute : Attribute
	{
	}

	/// <summary>
	/// CLI does not allow static abstract methods. If a  static method is declared as
	/// abstract, it is marked with this attribute.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public sealed class PhpAbstractAttribute : Attribute
	{
	}

	/// <summary>
	/// Class field that have an init value is marked with this attribute.
	/// </summary>
	/// <remarks>Attribute is used by <see cref="Reflection"/>.</remarks>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
	public sealed class PhpHasInitValueAttribute : Attribute
	{
	}

	/// <summary>
	/// Interface method marked by this attribute can be implemented without adhering to its return type.
	/// </summary>
	/// <remarks>
	/// An interface method returning by reference (<B>&amp;</B>) that is decorated by this attribute
	/// can be implemented by a method that does not return by reference.
	/// Attribute is used by <see cref="Reflection"/>.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public sealed class AllowReturnValueOverrideAttribute : Attribute
	{
	}

	/// <summary>
	/// Interface method marked by this attribute can be implemented without adhering to its parameters.
	/// </summary>
	/// <remarks>Attribute is used by <see cref="Reflection"/>.</remarks>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public sealed class AllowParametersOverrideAttribute : Attribute
	{
	}

	/// <summary>
	/// PHP allows increasing visibility of fields that are declared as protected
	/// in ancestor class. A class that increases the visibility declares no field
	/// but is marked with that attribute.
	/// </summary>
	/// <remarks>Attribute is used by <see cref="Reflection"/>.</remarks>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
	public sealed class PhpPublicFieldAttribute : Attribute
	{
		private string fieldName;
		private bool isStatic;
		private bool hasInitValue;

		public PhpPublicFieldAttribute(string fieldName, bool isStatic, bool hasInitValue)
		{
			this.fieldName = fieldName;
			this.isStatic = isStatic;
			this.hasInitValue = hasInitValue;
		}

		public string FieldName { get { return fieldName; } }
		public bool IsStatic { get { return isStatic; } }
		public bool HasInitValue { get { return hasInitValue; } }
	}

	public abstract class PseudoCustomAttribute : Attribute
	{
	}

	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class |
	  AttributeTargets.Interface | AttributeTargets.Constructor | AttributeTargets.Method |
	  AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
	public sealed class ExportAttribute : PseudoCustomAttribute
	{
		internal static readonly ExportAttribute/*!*/ Default = new ExportAttribute();

		public ExportAttribute()
		{
		}
	}

	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
	public sealed class AppStaticAttribute : PseudoCustomAttribute
	{
		public AppStaticAttribute()
		{
		}
	}

	#endregion

	#region Class Library and Extensions

	/// <summary>
	/// Options of the function implementation.
	/// </summary>
	[Flags]
	public enum FunctionImplOptions : ushort
	{
		/// <summary>
		/// No options defined.
		/// </summary>
		None = 0,

		/// <summary>
		/// <see cref="IDictionary"/> of declared variables will be passed to the first argument.
		/// </summary>
		NeedsVariables = 1,

		/// <summary>
		/// Whether the function accesses currently executed PHP function arguments.
		/// </summary>
		NeedsFunctionArguments = 2,

		/// <summary>
		/// Whether the function needs to access instance of the object calling the function ($this reference)
		/// </summary>
		NeedsThisReference = 4,

        ///// <summary>
        ///// Whether the function is special for compiler. Setting this flag implies changes in compiler so
        ///// only compiler writers should set it. 
        ///// </summary>
        //Special = 8,

		/// <summary>
		/// Function is not supported.
		/// </summary>
		NotSupported = 16,

		/// <summary>
		/// Function is internal.
		/// </summary>
		Internal = 32,

		/// <summary>
		/// Captures eval to the current <see cref="ScriptContext"/>. 
		/// The captured values has to be reset immediately before the method returns.
		/// </summary>
		CaptureEvalInfo = 64,

		/// <summary>
		/// Whether the function uses the current naming context.
		/// </summary>
		NeedsNamingContext = 128,

        /// <summary>
        /// Needs DTypeDesc class context of the caller.
        /// </summary>
        NeedsClassContext = 256,
	}

	/// <summary>
	/// Marks static methods of the Class Library which implements PHP functions.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public sealed class ImplementsFunctionAttribute : Attribute
	{
		/// <summary>
		/// Creates an instance of the <see cref="ImplementsFunctionAttribute"/> attribute.
		/// </summary>
		/// <param name="name">The name of the PHP function implemented by marked method.</param>
		public ImplementsFunctionAttribute(string name)
			: this(name, FunctionImplOptions.None)
		{
		}

		/// <summary>
		/// Creates an instance of the <see cref="ImplementsFunctionAttribute"/> attribute.
		/// </summary>
		/// <param name="name">The name of the PHP function implemented by marked method.</param>
		/// <param name="options">Options.</param>
		public ImplementsFunctionAttribute(string name, FunctionImplOptions options)
		{
			this.name = name;
			this.options = options;
		}

		/// <summary>
		/// The name of the PHP function.
		/// </summary>
		public string Name { get { return name; } }
		private string name;

		/// <summary>
		/// Options.
		/// </summary>
		public FunctionImplOptions Options { get { return options; } }
		private FunctionImplOptions options;

		internal static ImplementsFunctionAttribute Reflect(MethodBase/*!*/ method)
		{
			object[] attributes = method.GetCustomAttributes(Emit.Types.ImplementsFunctionAttribute, false);
			return (attributes.Length == 1) ? (ImplementsFunctionAttribute)attributes[0] : null;
		}

		/// <summary>
		/// Reflects an assembly, but also supports a case where Phalanger is reflecting
		/// Silverlight version of the assembly (and so the type of attribute is different)
		/// </summary>
        /// <param name="attrType"></param>
		/// <param name="method"></param>
		/// <returns></returns>
		internal static ImplementsFunctionAttribute ReflectDynamic(Type/*!*/ attrType, MethodBase/*!*/ method)
		{
			object[] attributes = method.GetCustomAttributes(attrType, false);
			if (attributes.Length == 1)
			{
                string name = (string)attrType.GetProperty("Name").GetValue(attributes[0], ArrayUtils.EmptyObjects);
                object options = attrType.GetProperty("Options").GetValue(attributes[0], ArrayUtils.EmptyObjects);
				int opt = System.Convert.ToInt32(options);
				return new ImplementsFunctionAttribute(name, (FunctionImplOptions)opt);
			}
			else
				return null;
		}
	}

    /// <summary>
    /// Marks class library function that the specified method is pure. Therefore it can be evaluated at the compilation time.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public sealed class PureFunctionAttribute : Attribute
    {
        /// <summary>
        /// True if special method must be called for compile-time evaluation.
        /// </summary>
        public bool CallSpecialMethod { get { return SpecialMethodType != null && SpecialMethodName != null; } }

        /// <summary>
        /// MethodInfo of the method to be used during the compile-time evaluation.
        /// </summary>
        public MethodInfo SpecialMethod
        {
            get
            {
                Debug.Assert(CallSpecialMethod);
                return SpecialMethodType.GetMethod(SpecialMethodName, BindingFlags.Static | BindingFlags.Public);
            }
        }

        /// <summary>
        /// Type containing special method to be called for compile-time evaluation.
        /// </summary>
        private Type SpecialMethodType { get; set; }

        /// <summary>
        /// Special method name to be called for compile-time evaluation.
        /// </summary>
        private string SpecialMethodName { get; set; }

        /// <summary>
        /// Creates an instance of the <see cref="PureFunctionAttribute"/> attribute. Used if the method can be called during the compile-time evaluation.
        /// </summary>
        public PureFunctionAttribute()
            : this(null, null)
        {

        }

        /// <summary>
        /// Creates an instance of the <see cref="PureFunctionAttribute"/> attribute. Used if another method must be called during the compile-time evaluation.
        /// </summary>
        /// <param name="specialMethodType">Type containing special method to be called for compile-time evaluation.</param>
        /// <param name="specialMethodName">Special method name to be called for compile-time evaluation.</param>
        public PureFunctionAttribute(Type specialMethodType, string specialMethodName)
        {
            this.SpecialMethodType = specialMethodType;
            this.SpecialMethodName = specialMethodName;
        }

        /// <summary>
        /// Reflect the given MethodBase to fund the PureFunctionAttribute.
        /// </summary>
        /// <param name="method">The method where to find PureFunctionAttribute.</param>
        /// <returns>PureFunctionAttribute of the <c>method</c> or null if the attribute was not found.</returns>
        internal static PureFunctionAttribute Reflect(MethodBase/*!*/ method)
        {
            object[] attributes = method.GetCustomAttributes(typeof(PureFunctionAttribute), false);
            return (attributes.Length == 1) ? (PureFunctionAttribute)attributes[0] : null;
        }
    }

	/// <summary>
	/// Marks the pseudo-this parameter of a class library method.
	/// </summary>
	/// <remarks>
	/// The method should be static and the parameter marked by this attribute should have the
	/// enclosing type. Use this attribute when the method must be callable both using an instance
	/// and statically (e.g. <c>DOMDocument::load</c>).
	/// </remarks>
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
	public sealed class ThisAttribute : Attribute
	{
		public ThisAttribute()
		{
		}
	}

	/// <summary>
	/// Marks a nullable parameter of a class library method.
	/// </summary>
	/// <remarks>
	/// When a parameter of a reference type is marked by this attribute, <B>null</B> is also a legal
	/// argument value.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
	public sealed class NullableAttribute : Attribute
	{
		public NullableAttribute()
		{
		}
	}

	/// <summary>
	/// Marks methods of the Class Library which implement PHP methods.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public sealed class ImplementsMethodAttribute : Attribute
	{
		public ImplementsMethodAttribute()
		{
		}
	}

	/// <summary>
	/// Marks properties/methods of Class Library types which should be exposed to PHP.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public sealed class PhpVisibleAttribute : Attribute
	{
		public PhpVisibleAttribute()
		{
		}

        internal static PhpVisibleAttribute Reflect(MemberInfo/*!*/ info)
        {
            object[] attributes = info.GetCustomAttributes(typeof(PhpVisibleAttribute), false);
            return (attributes.Length == 1) ? (PhpVisibleAttribute)attributes[0] : null;
        }
	}

	/// <summary>
	/// Marks constants and items of enumerations in the Class Library which represent PHP constants.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
	public sealed class ImplementsConstantAttribute : Attribute
	{
		/// <summary>
		/// Creates an instance of the <see cref="ImplementsConstantAttribute"/> attribute.
		/// </summary>
		/// <param name="name">The name of the PHP constant implemented by marked constant or enum item.</param>
		public ImplementsConstantAttribute(string name)
		{
			this.name = name;
			this.caseInsensitive = false;
		}

		/// <summary>
		/// The name of the PHP constant.
		/// </summary>
		public string Name { get { return name; } }
		private string name;

		/// <summary>
		/// Whether the constant name is not case sensitive.
		/// </summary>
		public bool CaseInsensitive { get { return caseInsensitive; } set { caseInsensitive = value; } }
		private bool caseInsensitive;

		internal static ImplementsConstantAttribute Reflect(FieldInfo/*!*/ field)
		{
			object[] attributes = field.GetCustomAttributes(typeof(ImplementsConstantAttribute), false);
			return (attributes.Length == 1) ? (ImplementsConstantAttribute)attributes[0] : null;
		}
	}

	/// <summary>
	/// Marks classes in the Class Library which implements a part or entire PHP extension.
	/// </summary>
	/// <remarks>
	/// Libraries which implements more than one extension should use the attribute 
	/// to distinguish which types belongs to which extension. If the library implements a single extension
	/// it is not required to use the attribute.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
	public sealed class ImplementsExtensionAttribute : Attribute
	{
		/// <summary>
		/// Creates an instance of the <see cref="ImplementsExtensionAttribute"/> attribute.
		/// </summary>
		/// <param name="name">The name of the PHP extension.</param>
		public ImplementsExtensionAttribute(string name)
		{
			this.name = name;
		}

		/// <summary>
		/// The name of the PHP extension.
		/// </summary>
		public string Name { get { return name; } }
		private string name;
	}

	/// <summary>
	/// Marks return values of methods implementing PHP functions which returns <B>false</B> on error
	/// but has other return type than <see cref="bool"/> or <see cref="object"/>.
	/// </summary>
	/// <remarks>
	/// Compiler takes care of converting a return value of a method into <B>false</B> if necessary.
	/// An attribute can be applied only on return values of type <see cref="int"/> (-1 is converted to <B>false</B>)
	/// or of a reference type (<B>null</B> is converted to <B>false</B>).
	/// </remarks>
	[AttributeUsage(AttributeTargets.ReturnValue, AllowMultiple = false, Inherited = false)]
	public sealed class CastToFalseAttribute : Attribute
	{
		public CastToFalseAttribute()
		{
		}
        
        /// <summary>
        /// Determine wheter the attribute is defined for given <paramref name="method"/>.
        /// </summary>
        /// <param name="method"><see cref="MethodInfo"/> to check for the attribute.</param>
        /// <returns>True iff given <paramref name="method"/> has <see cref="CastToFalseAttribute"/>.</returns>
        internal static bool IsDefined(MethodInfo/*!*/ method)
        {
            return method.ReturnTypeCustomAttributes.IsDefined(typeof(CastToFalseAttribute), false);
        }
	}

	/// <summary>
	/// If a parameter or a return value is marked by this attribute compiler should 
	/// generate deep-copy code before or after the method's call respectively.
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false, Inherited = false)]
	public sealed class PhpDeepCopyAttribute : Attribute
	{

	}

	/// <summary>
	/// Marks arguments having by-value argument pass semantics and data of the value can be changed by a callee.
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
	public sealed class PhpRwAttribute : Attribute
	{

	}

	/// <summary>
	/// Marks argless stubs in (dynamic) wrappers which consumes <see cref="PhpStack.Variables"/>.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public sealed class NeedsVariablesAttribute : Attribute
	{
	}

	/// <summary>
	/// ExternalCallbackAttribute marks methods which are intended to be called by some PHP extension module.
	/// </summary>
	/// <remarks>
	/// Informative only. Not used so far.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public sealed class ExternalCallbackAttribute : Attribute
	{
		/// <summary>
		/// Creates an instance of the <see cref="ImplementsConstantAttribute"/> attribute.
		/// </summary>
		/// <param name="callbackName">The name the callback.</param>
		public ExternalCallbackAttribute(string callbackName)
		{
			name = callbackName;
		}

		/// <summary>The name of the callback.</summary>
		public string Name { get { return name; } }

		/// <summary>The name of the callback.</summary>
		private string name;
	}

	#endregion

	#region Scripts

	/// <summary>
	/// An attribute associated with the persistent script type.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public sealed class ScriptAttribute : Attribute
	{
        /// <summary>
		/// A timestamp of the source file when the script builder is created.
		/// </summary>
		public DateTime SourceTimestamp  { get { return new DateTime(sourceTimestamp); } }
        private readonly long sourceTimestamp;

        /// <summary>
        /// Source file relative path.
        /// </summary>
        public string/*!*/RelativePath { get { return relativePath; } }
        private readonly string/*!*/relativePath;
        
        /// <summary>
        /// Used in SSA/MSA (target web and dll). Contains info needed for compile-time/runtime reflection.
        /// </summary>
        /// <param name="sourceTimestamp">A timestamp of the source file when the script builder is created.</param>
        /// <param name="relativePath">Source file relative path.</param>
        public ScriptAttribute(long sourceTimestamp, string relativePath)
		{
            this.sourceTimestamp = sourceTimestamp;
			this.relativePath = relativePath;
		}

        /// <summary>
        /// Determine the [ScriptInfoAttribute] attribute of given script type.
        /// </summary>
        /// <param name="type">The script type to reflect from.</param>
        /// <returns>Script attribute associated with the given <c>type</c> or null.</returns>
        internal static ScriptAttribute Reflect(Type/*!*/ type)
		{
            object[] attrs = type.GetCustomAttributes(typeof(ScriptAttribute), false);
            return (attrs.Length == 1) ? (ScriptAttribute)attrs[0] : null;
		}
	}

    /// <summary>
	/// An attribute associated with the persistent script type.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public sealed class ScriptIncludersAttribute : Attribute
	{
        /// <summary>
        /// Array of Scripts that statically include this script within current assembly.
        /// Script is represented as type token to the Script type resolved within current module.
        /// </summary>
        public int[]/*!*/ Includers { get { return includers ?? ArrayUtils.EmptyIntegers; } }
        private readonly int[] includers;

        /// <summary>
        /// Used in SSA/MSA (target web and dll). Contains info needed for compile-time/runtime reflection.
        /// </summary>
        /// <param name="includers">
        /// Array of Scripts that statically include this script within current assembly.
        /// Script is represented as type token to the Script type.
        /// </param>
        public ScriptIncludersAttribute(int[] includers)
		{
            this.includers = includers;
		}

        /// <summary>
        /// Determine the [ScriptIncludersAttribute] attribute of given script type.
        /// </summary>
        /// <param name="type">The script type to reflect from.</param>
        /// <returns>Script attribute associated with the given <c>type</c> or null.</returns>
        internal static ScriptIncludersAttribute Reflect(Type/*!*/ type)
		{
            object[] attrs = type.GetCustomAttributes(typeof(ScriptIncludersAttribute), false);
            return (attrs.Length == 1) ? (ScriptIncludersAttribute)attrs[0] : null;
		}
	}

    /// <summary>
	/// An attribute associated with the persistent script type.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public sealed class ScriptIncludeesAttribute : Attribute
	{
        /// <summary>
        /// Array of statically included Scripts.
        /// Script is represented as type token to the Script type resolved within current module.
        /// </summary>
        public int[]/*!*/ Inclusions { get { return inclusions ?? ArrayUtils.EmptyIntegers; } }
		private readonly int[] inclusions;

        /// <summary>
        /// Get bit array with flags determining if static inclusion on specific index is included conditionally.
        /// </summary>
        public BitArray/*!*/InclusionsConditionalFlags { get { return new BitArray(inclusionsConditionalFlag ?? ArrayUtils.EmptyBytes); } }
        /// <summary>
        /// Bit array. Bit is set to 1 if inclusion on specified bit index is conditional.
        /// </summary>
        private readonly byte[] inclusionsConditionalFlag;

        /// <summary>
        /// Used in SSA/MSA (target web and dll). Contains info needed for compile-time/runtime reflection.
        /// </summary>
        /// <param name="inclusions">
        /// Array of statically included Scripts.
        /// Script is represented as type token to the Script type.
        /// </param>
        /// <param name="inclusionsConditionalFlag">
        /// Array with bit flags determining if static inclusion on specific index is included conditionally.
        /// </param>
		public ScriptIncludeesAttribute(int[] inclusions, byte[] inclusionsConditionalFlag)
		{
            this.inclusions = inclusions;
            this.inclusionsConditionalFlag = inclusionsConditionalFlag;
		}

        /// <summary>
        /// Convert array of bools into array of bytes.
        /// Note: BitArray cannot be used, missing method <c>ToBytes</c>.
        /// </summary>
        /// <param name="array">An array to convert.</param>
        /// <returns>Bytes with particular bits set or null of <c>array</c> is null or empty.</returns>
        internal static byte[] ConvertBoolsToBits(bool[] array)
        {
            if (array == null || array.Length == 0)
                return null;

            // construct the bit array from given array
            int bytesCount = array.Length / 8;
            if ((array.Length % 8) != 0) bytesCount++;

            byte[] bits = new byte[bytesCount];

            for (int i = 0; i < array.Length; ++i)
                if (array[i])
                    bits[i / 8] |= (byte)(1 << (i % 8));

            return bits;
        }

        /// <summary>
        /// Determine the [ScriptIncludesAttribute] attribute of given script type.
        /// </summary>
        /// <param name="type">The script type to reflect from.</param>
        /// <returns>Script attribute associated with the given <c>type</c> or null.</returns>
        internal static ScriptIncludeesAttribute Reflect(Type/*!*/ type)
		{
			object[] attrs = type.GetCustomAttributes(typeof(ScriptIncludeesAttribute), false);
            return (attrs.Length == 1) ? (ScriptIncludeesAttribute)attrs[0] : null;
		}
	}

    /// <summary>
    /// An attribute associated with the persistent script type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ScriptDeclaresAttribute : Attribute
    {
        /// <summary>
        /// Array of Types that are statically declared by this Script.
        /// Type is represented as type token to the Type resolved within current module.
        /// </summary>
        public int[]/*!*/ DeclaredTypes { get { return declaredTypes ?? ArrayUtils.EmptyIntegers; } }
        private readonly int[] declaredTypes;

        /// <summary>
        /// Used in SSA/MSA (target web and dll). Contains info needed for compile-time/runtime reflection.
        /// </summary>
        /// <param name="declaredTypes">
        /// Array of Types that are statically declared by this Script.
        /// Type is represented as type token to the Type resolved within current module.
        /// </param>
        public ScriptDeclaresAttribute(int[] declaredTypes)
        {
            this.declaredTypes = declaredTypes;
        }

        /// <summary>
        /// Determine the [ScriptIncludersAttribute] attribute of given script type.
        /// </summary>
        /// <param name="type">The script type to reflect from.</param>
        /// <returns>Script attribute associated with the given <c>type</c> or null.</returns>
        internal static ScriptDeclaresAttribute Reflect(Type/*!*/ type)
        {
            object[] attrs = type.GetCustomAttributes(typeof(ScriptDeclaresAttribute), false);
            return (attrs.Length == 1) ? (ScriptDeclaresAttribute)attrs[0] : null;
        }
    }

	/// <summary>
	/// Stores information about scripts directly included by a module which is decorated by this attribute.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public sealed class IncludesAttribute : Attribute
	{
		/// <summary>
		/// Creates a new instance of the attribute with a specified source path and a conditionality flag.
		/// </summary>
		/// <param name="relativeSourcePath">Relative path remainder.</param>
		/// <param name="level">Relative path level.</param>
		/// <param name="isConditional">Whether the inclusion is conditional.</param>
		/// <param name="once"><B>True</B> if the include is include_once or require_once.</param>
		public IncludesAttribute(string relativeSourcePath, sbyte level, bool isConditional, bool once)
		{
			this.relativeSourceFile = new RelativePath(level, relativeSourcePath);
			this.isConditional = isConditional;
			this.once = once;
		}

		/// <summary>
		/// An included script's canonical source path relative to the application source root.
		/// </summary>
		public RelativePath RelativeSourceFile { get { return relativeSourceFile; } }
		private RelativePath relativeSourceFile;

		/// <summary>
		/// Whether the inclusion is conditional.
		/// </summary>
		public bool IsConditional { get { return isConditional; } }
		private bool isConditional;

		/// <summary>
		/// Whether the inclusion is include_once or require_once.
		/// </summary>
		public bool Once { get { return once; } }
		private bool once;
	}

	/// <summary>
	/// Associates a class with an eval id.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class PhpEvalIdAttribute : Attribute
	{
		public PhpEvalIdAttribute(int id)
		{
			this.id = id;
		}

		internal static PhpEvalIdAttribute Reflect(Type/*!*/ type)
		{
			object[] attrs = type.GetCustomAttributes(typeof(PhpEvalIdAttribute), false);
			return (attrs.Length == 1) ? (PhpEvalIdAttribute)attrs[0] : null;
		}

#if DEBUG

		public override string ToString()
		{
			//EvalInfo info = (EvalInfo)EvalCompilerManager.Default.GetEvalInfo(id);
			//return String.Format("PhpEvalId(id={4},parent={5},kind={0},line={2},column={3},file={1})",
			//  info.Kind,
			//  (info.File != null) ? "@\"" + info.File + "\"" : "null",
			//  info.Line,
			//  info.Column,
			//  id,
			//  info.ParentId);
			return "TODO";
		}

#endif

		/// <summary>
		/// Eval id.
		/// </summary>
		public int Id { get { return id; } }
		private int id;
	}

	#endregion

	#region Miscellaneous

	/// <summary>
	/// Specifies that a target field, property, or class defined in the configuration record 
	/// is not displayd by PHP info.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class NoPhpInfoAttribute : Attribute
	{
	}

	/// <summary>
	/// Used for marking Core members that are emitted to the user code.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Delegate, AllowMultiple = false, Inherited = false)]
	internal class EmittedAttribute : Attribute
	{
	}

    /// <summary>
    /// Attribute specifying that function should be called statically with valid PhpStack. Such a function needs function arguments,
    /// e.g. it calls func_num_args() or func_get_arg() inside.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    public class NeedsArglessAttribute : Attribute
    {
        /// <summary>
        /// Determines if given <c>method</c> has [NeedsArgless] attribute set.
        /// </summary>
        /// <param name="method">The method to reflect from.</param>
        /// <returns>True if the method is marked.</returns>
        internal static bool IsSet(MethodInfo/*!*/method)
        {
            object[] attrs;
            return (attrs = method.GetCustomAttributes(typeof(NeedsArglessAttribute), false)) != null && attrs.Length > 0;
        }
    }


#if DEBUG

	// GENERICS: replace with VSUnit
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public class TestAttribute : Attribute
	{
		public TestAttribute()
		{
			this.One = false;
		}

		public TestAttribute(bool one)
		{
			this.One = one;
		}

		public readonly bool One;
	}

#endif

	#endregion
}
