/*

 Copyright (c) 2006 Tomas Matousek and Ladislav Prosek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

//#define DEBUG_DELEGATE_STUBS

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using PHP.Core.Emit;
using System.Threading;

#if SILVERLIGHT
using PHP.CoreCLR;
using ArrayEx = PHP.CoreCLR.ArrayEx;
#else
using System.Runtime.Serialization;
using ArrayEx = System.Array;
#endif

namespace PHP.Core.Reflection
{
	#region DTypeDesc

	/// <summary>
	/// The representative of a dynamic type.
	/// </summary>
	[DebuggerNonUserCode]
	public abstract class DTypeDesc : DMemberDesc
	{
		#region Statics

		public static readonly DTypeDesc[]/*!*/ EmptyArray = new DTypeDesc[0];

		internal const BindingFlags MembersReflectionBindingFlags = BindingFlags.DeclaredOnly | BindingFlags.Instance |
			BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

		internal const BindingFlags AllMembersReflectionBindingFlags = BindingFlags.Instance |
			BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

		/// <summary>
		/// Open-instance delegate that returns the appropriate member dictionary for the specified
		/// <see cref="DTypeDesc"/> instance.
		/// </summary>
		private delegate Dictionary<N, T> GetMemberDictionary<N, T>(DTypeDesc typeDesc);

        /// <summary>
        /// Cache of <see cref="RuntimeTypeHandle"/> mapping into <see cref="DTypeDesc"/>.
        /// </summary>
        private readonly static SynchronizedCache<RuntimeTypeHandle, DTypeDesc>/*!*/cache
            = new SynchronizedCache<RuntimeTypeHandle, DTypeDesc>(CreateNoLockInternal);

		public static readonly DTypeDesc/*!*/ SystemObjectTypeDesc;
		public static readonly DTypeDesc/*!*/ DelegateTypeDesc;
		public static readonly DTypeDesc/*!*/ AttributeTypeDesc;
		public static readonly DTypeDesc/*!*/ AttributeUsageAttributeTypeDesc;
		public static readonly DTypeDesc/*!*/ InterlockedTypeDesc;

		// emitted:
		public static readonly PrimitiveTypeDesc/*!*/ BooleanTypeDesc;
		public static readonly PrimitiveTypeDesc/*!*/ IntegerTypeDesc;
		public static readonly PrimitiveTypeDesc/*!*/ LongIntegerTypeDesc;
		public static readonly PrimitiveTypeDesc/*!*/ DoubleTypeDesc;
		public static readonly PrimitiveTypeDesc/*!*/ StringTypeDesc;
		public static readonly PrimitiveTypeDesc/*!*/ ResourceTypeDesc;
		public static readonly PrimitiveTypeDesc/*!*/ ArrayTypeDesc;
		public static readonly PrimitiveTypeDesc/*!*/ ObjectTypeDesc;
        public static readonly PrimitiveTypeDesc/*!*/ CallableTypeDesc;

		private static readonly GetMemberDictionary<VariableName, DConstantDesc> getConstantDictionary;
		private static readonly GetMemberDictionary<VariableName, DPropertyDesc> getPropertyDictionary;
		private static readonly GetMemberDictionary<Name, DRoutineDesc> getMethodDictionary;

		static DTypeDesc()
		{
			getConstantDictionary = (GetMemberDictionary<VariableName, DConstantDesc>)
				Delegate.CreateDelegate(typeof(GetMemberDictionary<VariableName, DConstantDesc>), null,
				typeof(DTypeDesc).GetProperty("Constants", BindingFlags.NonPublic | BindingFlags.Instance).GetGetMethod(true));

			getPropertyDictionary = (GetMemberDictionary<VariableName, DPropertyDesc>)
				Delegate.CreateDelegate(typeof(GetMemberDictionary<VariableName, DPropertyDesc>), null,
				typeof(DTypeDesc).GetProperty("Properties", BindingFlags.NonPublic | BindingFlags.Instance).GetGetMethod(true));

			getMethodDictionary = (GetMemberDictionary<Name, DRoutineDesc>)
				Delegate.CreateDelegate(typeof(GetMemberDictionary<Name, DRoutineDesc>), null,
				typeof(DTypeDesc).GetProperty("Methods", BindingFlags.NonPublic | BindingFlags.Instance).GetGetMethod(true));

			if (UnknownModule.RuntimeModule == null)
				UnknownModule.RuntimeModule = new UnknownModule();

            SystemObjectTypeDesc = Create(typeof(System.Object));
            DelegateTypeDesc = Create(typeof(System.Delegate));
            AttributeTypeDesc = Create(typeof(System.Attribute));
            AttributeUsageAttributeTypeDesc = Create(typeof(System.AttributeUsageAttribute));
            InterlockedTypeDesc = Create(typeof(System.Threading.Interlocked));

			BooleanTypeDesc = new PrimitiveTypeDesc(typeof(bool), PhpTypeCode.Boolean);
			IntegerTypeDesc = new PrimitiveTypeDesc(typeof(int), PhpTypeCode.Integer);
			LongIntegerTypeDesc = new PrimitiveTypeDesc(typeof(long), PhpTypeCode.LongInteger);
			DoubleTypeDesc = new PrimitiveTypeDesc(typeof(double), PhpTypeCode.Double);
			StringTypeDesc = new PrimitiveTypeDesc(typeof(string), PhpTypeCode.String);
			ResourceTypeDesc = new PrimitiveTypeDesc(typeof(PhpResource), PhpTypeCode.PhpResource);
			ArrayTypeDesc = new PrimitiveTypeDesc(typeof(PhpArray), PhpTypeCode.PhpArray);
			ObjectTypeDesc = new PrimitiveTypeDesc(typeof(DObject), PhpTypeCode.DObject);
            CallableTypeDesc = new PrimitiveTypeDesc(typeof(void)/*not used*/, PhpTypeCode.PhpCallable);
		}

		#endregion

		#region Properties

		public virtual bool IsGlobal { get { return false; } }
		public virtual bool IsUnknown { get { return false; } }

		/// <summary>
		/// Triggers full-reflect when accessed and is <B>null</B>.
		/// </summary>
		public DType Type
		{
			get
			{
                if (Member == null) lock (this) if (Member == null) { Member = Reflect(); }
				return (DType)Member;
			}
		}

		/// <summary>
		/// Triggers full-reflect when accessed and is <B>null</B>.
		/// </summary>
		public KnownType KnownType { get { return (KnownType)Type; } }

		/// <summary>
		/// Triggers full-reflect when accessed and is <B>null</B>.
		/// </summary>
		public PhpType PhpType { get { return (PhpType)Type; } }

		/// <summary>
		/// Triggers full-reflect when accessed and is <B>null</B>.
		/// </summary>
		public ClrType ClrType { get { return (ClrType)Type; } }

		public bool IsInterface { get { return (memberAttributes & PhpMemberAttributes.Interface) != 0; } }
        public bool IsTrait { get { return (memberAttributes & PhpMemberAttributes.Trait) != 0; } }
		public virtual bool IsValueType { get { return false; } }

		/// <summary>
		/// For generated types, this is <B>null</B> before the type builder is defined.
		/// </summary>
		public Type RealType { get { return realType; } }
		private Type realType;

        /// <summary>
        /// Delegate that calls {.newobj .ctor(ScriptContext, bool)} of the <c>RealType</c>.
        /// </summary>
        internal Ctor_ScriptContext_Bool RealTypeCtor_ScriptContext_Bool = null;
        internal delegate object Ctor_ScriptContext_Bool(ScriptContext context, bool newInstance);
        
        /// <summary>
        /// Delegate that calls {.newobj .ctor(ScriptContext, DTypeDesc)} of the <c>RealType</c>.
        /// </summary>
        internal Ctor_ScriptContext_DTypeDesc RealTypeCtor_ScriptContext_DTypeDesc = null;
        internal delegate object Ctor_ScriptContext_DTypeDesc(ScriptContext context, DTypeDesc caller);
        
		/// <summary>
		/// <B>null</B> for types without a base type (PHP types only).
		/// </summary>
		public DTypeDesc Base { get { return baseDesc; } }
		private DTypeDesc baseDesc;

		/// <summary>
		/// Generic definition for this type or <B>null</B> if the type is non-generic or unknown.
		/// Generic definitions points to themselves, instantiations shares a single instance of generic definition.
		/// </summary>
		public GenericTypeDefinition GenericDefinition { get { return genericDefinition; } }
		protected GenericTypeDefinition genericDefinition;

		public bool IsGeneric
		{
			get { return genericDefinition != null; }
		}

		public virtual bool IsGenericDefinition
		{
			get { return genericDefinition != null && ReferenceEquals(genericDefinition.GenericType, this); }
		}

		public GenericParameterDesc[]/*!!*/ GenericParameters
		{
			get
			{
				return (genericDefinition != null) ? genericDefinition.GenericParameters : GenericParameterDesc.EmptyArray;
			}
		}

		#endregion

		#region Tables

		//
		// WARNING: Do not make following tables public as their readonly-ness is not imposed!
		// NOTE: The tables may be shared among multiple type-descs.
		//

        /// <summary>
        /// Reflection is pending. Tables should not be used until <c>pendingReflection</c> is <c>true</c>.
        /// </summary>
        private bool pendingReflection = false;

		/// <summary>
		/// All implemented interfaces (including inherited ones).
		/// It is impossible to get rid of inherited interfaces due to lack of support from CLR.
		/// For PHP types being compiled, this array is filled by inheritance hierarchy analysis.
		/// </summary>
		internal DTypeDesc[]/*!!*/ Interfaces
		{
			get
			{
                if (interfaces == null || pendingReflection)
                    lock(this)
                        if (interfaces == null)
                        {
                            pendingReflection = true;
                            ReflectInterfaces();
                            Debug.Assert(interfaces != null);
                            pendingReflection = false;
                        }

				return interfaces;
			}
		}
		protected DTypeDesc[] interfaces;

		internal Dictionary<Name, DRoutineDesc> Methods
		{
			get
			{
                if (methods == null || pendingReflection)
                    lock (this)
                        if (methods == null)
                        {
                            pendingReflection = true;
                            ReflectMethods();
                            Debug.Assert(methods != null);
                            pendingReflection = false;
                        }

				return methods;
			}
		}
		protected Dictionary<Name, DRoutineDesc> methods;

		// TODO: OrderedHashtable
		internal Dictionary<VariableName, DPropertyDesc> Properties
		{
			get
			{
                if (properties == null || pendingReflection)
                    lock(this)
                        if (properties == null)
                        {
                            pendingReflection = true;
                            ReflectProperties();
                            Debug.Assert(properties != null);
                            pendingReflection = false;
                        }

				return properties;
			}
		}
		protected Dictionary<VariableName, DPropertyDesc> properties;

		internal Dictionary<VariableName, DConstantDesc> Constants
		{
			get
			{
                if (constants == null || pendingReflection)
                    lock(this)
                        if (constants == null)
                        {
                            pendingReflection = true;
                            ReflectConstants();
                            Debug.Assert(constants != null);
                            pendingReflection = false;
                        }

				return constants;
			}
		}
		protected Dictionary<VariableName, DConstantDesc> constants;

        #endregion

        #region Construction

        /// <summary>
		/// Used only by <see cref="GlobalTypeDesc"/>, <see cref="UnknownTypeDesc"/>, <see cref="GenericParameterDesc"/>.
		/// </summary>
		protected DTypeDesc()
			: base()
		{
			this.genericDefinition = null;
			this.baseDesc = null;
			this.realType = null;
			this.methods = null;
			this.properties = null;
			this.constants = null;
		}

		/// <summary>
		/// Used only by <see cref="ConstructedTypeDesc"/> and <see cref="PhpTypeCompletionDesc"/>.
		/// </summary>
		protected DTypeDesc(DTypeDesc baseDesc, Type realType, GenericTypeDefinition genericDefinition)
			: base()
		{
			this.genericDefinition = genericDefinition;
			this.baseDesc = baseDesc;
			this.realType = realType;
			this.methods = null;
			this.properties = null;
			this.constants = null;
		}

		/// <summary>
		/// Used by the compiler.
		/// </summary>
		public DTypeDesc(DModule/*!*/ declaringModule, PhpMemberAttributes memberAttributes)
			: base(declaringModule.GlobalType.TypeDesc, memberAttributes)
		{
			this.realType = null;            // to be defined later
			this.genericDefinition = null;   // to be written up
			this.baseDesc = null;            // to be written up
			this.interfaces = null;          // to be written up
			this.methods = new Dictionary<Name, DRoutineDesc>();
			this.properties = new Dictionary<VariableName, DPropertyDesc>();
			this.constants = new Dictionary<VariableName, DConstantDesc>();
		}

		/// <summary>
		/// Used by the compiler after the type has been analyzed. First write up.
		/// Used also at run-time.
		/// </summary>
		internal void WriteUpGenericDefinition(GenericTypeDefinition genericDefinition)
		{
			this.genericDefinition = genericDefinition;
		}

		/// <summary>
		/// Used by the compiler after the type has been analyzed. Second write up.
		/// </summary>
		internal void WriteUpBaseType(DTypeDesc baseDesc)
		{
			this.baseDesc = baseDesc;
		}

		internal void WriteUpInterfaces(DTypeDesc[]/*!!*/ interfaces)
		{
			Debug.Assert(this.interfaces == null && interfaces != null);

			this.interfaces = interfaces;
		}

		/// <summary>
		/// Called by the compiler when the type is being defined.
		/// A <B>null</B> reference means that the type has no own real builder (e.g. <see cref="GlobalType"/>).
		/// </summary>
		internal void DefineBuilder(TypeBuilder typeBuilder)
		{
			this.realType = typeBuilder;
		}

		/// <summary>
		/// Called by the compiler when the type parameter is being defined.
		/// A <B>null</B> reference means that the type parameter has no own real builder (generic methods).
		/// </summary>
		internal void DefineBuilder(GenericTypeParameterBuilder genericTypeParameterBuilder)
		{
			this.realType = genericTypeParameterBuilder;
		}

		/// <summary>
		/// Called by the compiler when the type is being baked.
		/// </summary>
		internal void Bake(Type/*!*/ realType)
		{
			Debug.Assert(realType != null);

			this.realType = realType;
		}

		/// <summary>
		/// Used by the reflection.
		/// </summary>
		protected DTypeDesc(Type/*!*/ realType, GenericTypeDefinition genericDefinition)
			: base()
		{
			this.realType = realType;
			this.genericDefinition = genericDefinition;
			this.methods = null;
			this.properties = null;
			this.constants = null;
		}

		/// <summary>
		/// To be used by reflection and run-time.
		/// </summary>
		public DTypeDesc(DModule/*!*/ declaringModule, Type/*!*/ realType, DTypeDesc/*!*/ baseDesc,
			PhpMemberAttributes memberAttributes)
			: base(declaringModule.GlobalType.TypeDesc, memberAttributes)
		{
			this.realType = realType;
			this.baseDesc = baseDesc;
			this.genericDefinition = null;    // to be written up
			this.methods = null;              // to be filled later
			this.properties = null;           // to be filled later
			this.constants = null;            // to be filled later
		}

        #region Create proper DTypeDesc

        /// <summary>
		/// To be used at run-time for getting <see cref="DTypeDesc"/> from <see cref="Type"/>.
		/// </summary>
        public static DTypeDesc Create(Type realType)
		{
            if (object.ReferenceEquals(realType, null))
                return null;

            return Create(realType.TypeHandle);
		}

        /// <summary>
		/// To be used at run-time for getting <see cref="DTypeDesc"/> from <see cref="RuntimeTypeHandle"/>.
		/// </summary>
		[Emitted]
		public static DTypeDesc/*!*/ Create(RuntimeTypeHandle realTypeHandle)
		{
            return cache.Get(realTypeHandle);
		}

        /// <summary>
        /// Create the new <see cref="DTypeDesc"/> of given type.
        /// </summary>
        /// <param name="realTypeHandle">The type handle.</param>
        /// <param name="forcePhptype">True to force create <see cref="PhpTypeDesc"/>.</param>
        /// <returns>New <see cref="DTypeDesc"/>.</returns>
        /// <remarks>The newly created <see cref="DTypeDesc"/> is added into the cache.</remarks>
        internal static DTypeDesc/*!*/ Recreate(RuntimeTypeHandle realTypeHandle, bool forcePhptype)
        {
            return cache.Update(realTypeHandle, (key) => { return CreateNoLockInternal(key, forcePhptype); });
        }

        private static DTypeDesc/*!*/ CreateNoLockInternal(RuntimeTypeHandle realTypeHandle)
        {
            return CreateNoLockInternal(realTypeHandle, false);
        }

        /// <summary>
        /// Create <see cref="DTypeDesc"/> of given type.
        /// </summary>
        /// <param name="realTypeHandle">Runtime type handle of the type.</param>
        /// <param name="forcePhpType">True to force to create <see cref="PhpTypeDesc"/>.</param>
        /// <returns>New <see cref="DTypeDesc"/> wrapping the given <paramref name="realTypeHandle"/>.</returns>
        /// <remarks>The method is not thread safe and it does not lock or cache anything.</remarks>
        private static DTypeDesc/*!*/ CreateNoLockInternal(RuntimeTypeHandle realTypeHandle, bool forcePhpType)
		{
			DTypeDesc result;
			Type real_type = System.Type.GetTypeFromHandle(realTypeHandle);
			PhpMemberAttributes member_attrs = Enums.GetMemberAttributes(real_type);

			if (forcePhpType || PhpType.IsPhpRealType(real_type))
			{
				DTypeDesc base_desc = (real_type.BaseType == typeof(PhpObject)) ? null : Create(real_type.BaseType);
				result = new PhpTypeDesc(UnknownModule.RuntimeModule, real_type, base_desc, member_attrs);
			}
			else
			{
				DTypeDesc base_desc = DTypeDesc.Create(real_type.BaseType);

				if (!real_type.IsAbstract && base_desc != null && base_desc.IsSubclassOf(DelegateTypeDesc))
					result = new ClrDelegateDesc(UnknownModule.RuntimeModule, real_type, base_desc, member_attrs);
				else
					result = new ClrTypeDesc(UnknownModule.RuntimeModule, real_type, base_desc, member_attrs);
			}

			// associated the generic definition:
			if (real_type.IsGenericTypeDefinition)
				result.WriteUpGenericDefinition(new GenericTypeDefinition(result));
			else if (real_type.IsGenericType)
				result.WriteUpGenericDefinition(Create(real_type.GetGenericTypeDefinition().TypeHandle).GenericDefinition);

            return result;
		}

        #endregion

        #endregion

        #region Utility

        /// <summary>
        /// Name of the class excluding namespace name.
        /// </summary>
        public string/*!*/ MakeSimpleName()
        {
            if (simpleName == null)
            {
                if (this.Member != null && Member.FullName != null)
                {
                    int lastSeparator = this.Member.FullName.LastIndexOf(QualifiedName.Separator);
                    simpleName = (lastSeparator == -1) ? this.Member.FullName : this.Member.FullName.Substring(lastSeparator + 1);
                }
                else
                {
                    simpleName = MakeSimpleName(RealType);
                }
            }
            return simpleName;
        }
        private string simpleName = null;

        private static string/*!*/ MakeSimpleName(Type/*!*/realType)
        {
            var phpTypeAttr = ImplementsTypeAttribute.Reflect(realType);
            if (phpTypeAttr != null && phpTypeAttr.PHPTypeName != null)
                return phpTypeAttr.PHPTypeName;
            else
                return QualifiedName.SubstringWithoutBackquoteAndHash(realType.Name, 0, realType.Name.Length);
        }

        /// <summary>
        /// Full name of the type, including namespace name. Uses PHP namespace separator.
        /// </summary>
        public override string/*!*/ MakeFullName()
        {
            if (Member != null)
                return Member.FullName;

            return GetFullName(RealType, new StringBuilder()).ToString();
        }

        /// <summary>
        /// Full name of the type, including namespace name and generic parameters. Uses PHP namespace separator.
        /// </summary>
        public override string/*!*/ MakeFullGenericName()
        {
            return GetFullGenericName(RealType, new StringBuilder()).ToString();
        }

        /// <summary>
        /// Full name of the type, including namespace name and generic parameters. Uses PHP namespace separator.
        /// </summary>
        internal static StringBuilder/*!*/ GetFullGenericName(Type/*!*/ realType, StringBuilder/*!*/ result)
        {
            Debug.Assert(realType != null && result != null);

            GetFullName(realType, result);

            if (!realType.IsGenericType)
                return result;

            ConstructedTypeDesc.GenericArgumentsToString(realType.GetGenericArguments(), result);

            return result;
        }

        internal static StringBuilder/*!*/ GetFullName(Type/*!*/ realType, StringBuilder/*!*/ result)
        {
            Debug.Assert(realType != null && result != null);

            // TODO: do this better
            // proposed solution: RuntimeModule will convert lazily itself to a DModule 
            // DModule will then implement its policy of naming conventions and unmangling methods

            // primitive types first:
            string primitive_name = PrimitiveTypeDesc.GetPrimitiveName(realType);
            if (primitive_name != null)
                return result.Append(primitive_name);

            // naming policy of PhpLibraryModule is PHP.Library.library_namespace.type_name#n`m:
            // namespace is ignored
            if (!String.IsNullOrEmpty(realType.Namespace) && !realType.Namespace.StartsWith(Namespaces.Library))
            {
                if (realType.Namespace[0] == '<')
                {
                    // naming policy of ScriptModule is <coded_file_name>.user_namespace_clr.type_name#n`m:
                    int closing = realType.Namespace.IndexOf('>') + 2;
                    if (closing > 1 && closing < realType.Namespace.Length)
                    {
                        result.Append(realType.Namespace.Substring(closing).Replace('.', QualifiedName.Separator));
                        result.Append(QualifiedName.Separator);
                    }
                }
                else
                {
                    // naming policy of Pure Module is user_namespace_clr.type_name#n`m:
                    result.Append(realType.Namespace.Replace('.', QualifiedName.Separator));
                    result.Append(QualifiedName.Separator);
                }
            }

            if (realType.DeclaringType != null)
                GetNestedTypeNames(realType.DeclaringType, result);

            result.Append(MakeSimpleName(realType));

            return result;
        }

        internal static void GetNestedTypeNames(Type/*!*/ type, StringBuilder/*!*/ result)
        {
            Debug.Assert(type != null && result != null);

            // depth first:
            if (type.DeclaringType != null)
                GetNestedTypeNames(type.DeclaringType, result);

            result.Append(QualifiedName.SubstringWithoutBackquoteAndHash(type.Name, 0, type.Name.Length));
            result.Append(QualifiedName.Separator);
        }

		/// <summary>
		/// Determines whether this instance contains a given <see cref="DTypeDesc"/>
		/// in its inheritance hierarchy or vice-versa.
		/// </summary>
		/// <param name="anotherTypeDesc">Another <see cref="DTypeDesc"/>.</param>
		/// <returns><B>True</B> if the two types are related, <B>false</B> otherwise.</returns>
		/// <remarks>
		/// The (a &lt;: b || b &lt;: a) plays an important role in member accessibility checks.
		/// </remarks>
		public bool IsRelatedTo(DTypeDesc/*!*/ anotherTypeDesc)
		{
			// try to guess the shorter order
			if (IsInterface)
			{
				return (IsAssignableFrom(anotherTypeDesc) || anotherTypeDesc.IsAssignableFrom(this));
			}
			else
			{
				return (anotherTypeDesc.IsAssignableFrom(this) || IsAssignableFrom(anotherTypeDesc));
			}
		}

		/// <summary>
		/// Determines whether this instance is in the iheritance hierarchy (i.e. bases and implemented interfaces)
		/// of a given <see cref="DTypeDesc"/>.
		/// </summary>
		/// <param name="anotherTypeDesc">Another <see cref="DTypeDesc"/>.</param>
		/// <returns><B>True</B> if this instance is one of <paramref name="anotherTypeDesc"/>'s
		/// base types or implemented interfaces, <B>false</B> otherwise.</returns>
		public bool IsAssignableFrom(DTypeDesc/*!*/ anotherTypeDesc)
		{
			// Note, querying real type (Type.IsAssignableFrom) is slower than traversing our own hierarchy.
			// Besides, our implementation works also for descriptors whose real types have not yet been created.

			do
			{
				if (ReferenceEquals(anotherTypeDesc, this))
                    return true;

				if (IsInterface)
				{
					// search in interfaces implemented by the other type desc
					foreach (var iface in anotherTypeDesc.Interfaces)
						if (IsAssignableFrom(iface))
                            return true;
				}

				anotherTypeDesc = anotherTypeDesc.Base;
			}
			while (anotherTypeDesc != null);

			return false;
		}

		/// <summary>
		/// Determines whether this instance is a subclass of a given <see cref="DTypeDesc"/>.
		/// </summary>
		/// <param name="superTypeDesc">Another <see cref="DTypeDesc"/>.</param>
		/// <returns><B>True</B> if this instance is one of <paramref name="superTypeDesc"/>'s
		/// base types or implemented interfaces, <B>false</B> otherwise.</returns>
		public bool IsSubclassOf(DTypeDesc/*!*/ superTypeDesc)
		{
			// Note, querying real type (Type.IsSubclassOf) is slower than traversing our own hierarchy.
			// Besides, our implementation works also for descriptors whose real types have not yet been created.

			DTypeDesc desc = this;
			do
			{
				if (ReferenceEquals(desc, superTypeDesc)) return true;
				desc = desc.Base;
			}
			while (desc != null);

			return false;
		}

		/// <summary>
		/// Determines whether this instance can be used as a generic argument for the specified generic parameter.
		/// </summary>
		/// <param name="parameter">The generic parameter type.</param>
		/// <returns></returns>
		public bool IsCompatibleWithGenericParameter(Type/*!*/ parameter)
		{
			Debug.Assert(parameter.IsGenericParameter);

			GenericParameterAttributes attrs = parameter.GenericParameterAttributes;

			// check default ctor constraint
			if (Enums.GenericParameterAttrTest(attrs, GenericParameterAttributes.DefaultConstructorConstraint))
			{
				if (!Type.HasDefaultConstructor) return false;
			}

			// check ref/value type constraints
			if (Enums.GenericParameterAttrTest(attrs, GenericParameterAttributes.ReferenceTypeConstraint))
			{
				if (Type.IsValueType) return false;
			}
			else if (Enums.GenericParameterAttrTest(attrs, GenericParameterAttributes.NotNullableValueTypeConstraint))
			{
				if (!Type.IsValueType) return false;
			}

			Type[] constraints = parameter.GetGenericParameterConstraints();

			if (realType != null)
			{
				// perform the check using real types
				for (int i = 0; i < constraints.Length; i++)
				{
					if (!constraints[i].IsAssignableFrom(realType)) return false;
				}
			}
			else
			{
				// build type descs out of the constraints
				for (int i = 0; i < constraints.Length; i++)
				{
					DTypeDesc constraint_desc = DTypeDesc.Create(constraints[i]);
					if (constraint_desc.IsAssignableFrom(this)) return false;
				}
			}

			return true;
		}

		public static bool IsSystemObjectCompatibleWithGenericParameter(Type/*!*/ parameter)
		{
			if (Enums.GenericParameterAttrTest(parameter.GenericParameterAttributes, GenericParameterAttributes.NotNullableValueTypeConstraint))
			{
				return false;
			}

			// TODO: Is it possible for constraints to contain System.Object?
			// Is it possible that the types repeat in the constraints array?
			// If not, this can be optimized (ask for emptyness or single Object item):
			foreach (Type type in parameter.GetGenericParameterConstraints())
			{
				if (type != Types.Object[0]) return false;
			}

			return true;
		}

		public DTypeDesc[]/*!!*/ GetImplementedInterfaces()
		{
			return (DTypeDesc[])Interfaces.Clone();
		}

		#endregion

		#region Reflection

		protected abstract DType/*!*/ Reflect();

		/// <summary>
		/// Reflectes generic parameters of the generic type definition. Assumes no parameters if not overriden.
		/// </summary>
		internal virtual GenericParameterDesc[]/*!!*/ ReflectGenericParameters(DTypeDesc referringType, DRoutineDesc referringRoutine,
			ResolverDelegate resolver)
		{
			Debug.Assert(this.IsGenericDefinition, "Only a generic type definition can reflect generic parameters.");
			return GenericParameterDesc.EmptyArray;
		}

        /// <summary>
        /// Initialize the <c>interfaces</c> property with an array of items.
        /// This method is not thread safe. It assumes <c>interfaces</c> are not reflected yet.
        /// </summary>
		protected abstract void ReflectInterfaces();
        /// <summary>
        /// Initialize the <c>methods</c> property with an array of items.
        /// This method is not thread safe. It assumes <c>methods</c> are not reflected yet.
        /// </summary>
        protected abstract void ReflectMethods();
        /// <summary>
        /// Initialize the <c>properties</c> property with an array of items.
        /// This method is not thread safe. It assumes <c>properties</c> are not reflected yet.
        /// </summary>
        protected abstract void ReflectProperties();
        /// <summary>
        /// Initialize the <c>constants</c> property with an array of items.
        /// This method is not thread safe. It assumes <c>constants</c> are not reflected yet.
        /// </summary>
        protected abstract void ReflectConstants();

		#endregion

		#region Member Lookup

		#region Universal Lookup

		[Flags]
		private enum LookupFlags
		{
			None = 0,

			/// <summary>
			/// Specifies whether private members are reported as non-accessible by subtypes.
			/// </summary>
			InheritPrivate = 1,

			/// <summary>
			/// Specifies whether interfaces should be included in the lookup.
			/// </summary>
			SearchInterfaces = 2,

			/// <summary>
			/// Specifies whether only instance members should be searched.
			/// </summary>
			IgnoreStaticMembers = 4 // have to be checked explicitly
		}

		/// <summary>
		/// Searches for a member with the specified <paramref name="name"/> by walking up the inheritance hierarchy.
		/// </summary>
		/// <typeparam name="N">Type of member dictionary keys.</typeparam>
		/// <typeparam name="T">Type of member dictionary values.</typeparam>
		/// <param name="dictionary">Delegate that returns the member dictionary for a given <see cref="DTypeDesc"/>.</param>
		/// <param name="name">Member name.</param>
		/// <param name="context">Caller context.</param>
		/// <param name="flags">Flags that adjust the lookup.</param>
		/// <param name="member">Receives the member on success (<see cref="GetMemberResult.OK"/>) and on bad visiblity
		/// (<see cref="GetMemberResult.BadVisibility"/>).</param>
		/// <returns>The lookup result. If difference from <see cref="GetMemberResult.NotFound"/>, <paramref name="member"/>
		/// is non-<B>null</B>.</returns>
		/// <remarks>
		/// This method strives to incorporate all the PHP member lookup nuances. At the same time, all visibility
		/// combinations that are introduced by CLR members due to overloading must be supported.
		/// </remarks>
		private GetMemberResult GetMember<N, T>(GetMemberDictionary<N, T> dictionary, N name, DTypeDesc context,
			LookupFlags flags, out T member) where T : DMemberDesc
		{
			member = null;
			bool include_statics = ((flags & LookupFlags.IgnoreStaticMembers) == 0);
            bool static_used_as_nonstatic_reported = false;
            bool seen_context = ReferenceEquals(this, context);

			// start searching in this class
			DTypeDesc declarer = this;
            do
			{
				// get the dictionary of members for the current type desc
				Dictionary<N, T> members = dictionary(declarer);

				T candidate;
				if (members.TryGetValue(name, out candidate))
				{
                    if (include_statics || !candidate.IsStatic)
                    {
                        if ((candidate.IsPublic && context == null) ||
                            (candidate.IsPrivate && declarer == context)) // (1)
                        {
                            // if candidate is public and we surely won't hit the exact private match,
                            // or we are in the declarer's context, no further visibility checks are
                            // necessary (this is the fast path)
                            member = candidate;
                            return GetMemberResult.OK;
                        }
                        
                        if (candidate.IsPublic && (member == null || !member.IsPublic))
                        {
                            member = candidate;
                        }
                        
                        if (member == null)
                        {
                            member = candidate; // remember the first candidate encountered

                            // non-private members are always inherited
                            if (candidate.IsProtected &&
                                (seen_context || (context != null && declarer.IsRelatedTo(context))))
                            {
                                return GetMemberResult.OK;
                            }
                        }
                        else if (candidate.IsProtected && declarer is ClrTypeDesc)
                        {
                            // looser lookup rules for CLR members!!!
                            if (seen_context || (context != null && declarer.IsRelatedTo(context)))
                            {
                                member = candidate;
                                return GetMemberResult.OK;
                            }
                        }
                    }
                    else //if (!include_statics && candidate.IsStatic)
                    {
                        if (!static_used_as_nonstatic_reported)
                        {
                            static_used_as_nonstatic_reported = true;

                            PhpException.Throw(
                                PhpError.Strict,
                                CoreResources.GetString("static_property_as_nonstatic",
                                        candidate.DeclaringType.MakeFullName(), candidate.MakeFullName()));
                        }
                    }
				}
                else if (ReferenceEquals(declarer, context))
                {
                    seen_context = true;
                }

				// search implemented interfaces recursively (useful only for constants)
				if ((flags & LookupFlags.SearchInterfaces) != 0)
				{
					foreach (var iface in declarer.Interfaces)
					{
						GetMemberResult result = iface.GetMember<N, T>(dictionary, name, context, flags, out candidate);
						if (result != GetMemberResult.NotFound)
						{
							member = candidate;
							return result;
						}
					}
				}

				// move up along the hierarchy
				declarer = declarer.Base;
			}
			while (declarer != null);

			if (member == null) return GetMemberResult.NotFound;

			if (member.IsPublic) return GetMemberResult.OK;

			// now member contains the lower-most non-public member that was found -> perform visibility check
			// (this member is surely not public)
			if (member.IsProtected)
			{
				if (seen_context || (context != null && member.DeclaringType.IsRelatedTo(context))) return GetMemberResult.OK;
				return GetMemberResult.BadVisibility;
			}

			// no more check are necessary for private members - they would have been approved in (1)
			// return value in this case is different for methods and for properties
            if ((flags & LookupFlags.InheritPrivate) != 0 ||
                member.DeclaringType == this)
            {
                return GetMemberResult.BadVisibility;
            }
            else return GetMemberResult.NotFound;
		}

		#endregion

		/// <summary>
		/// Searches only in declared constants.
		/// </summary>
		public DConstantDesc GetConstant(VariableName constantName)
		{
			DConstantDesc constant;
			return (Constants.TryGetValue(constantName, out constant) ? constant : null);
		}

		/// <summary>
		/// Searches in all bases and interfaces.
		/// </summary>
		public GetMemberResult GetConstant(VariableName constantName, DTypeDesc context, out DConstantDesc constant)
		{
			return GetMember<VariableName, DConstantDesc>
				(getConstantDictionary, constantName, context, LookupFlags.SearchInterfaces |
				LookupFlags.InheritPrivate, out constant);
		}

		/// <summary>
		/// Searches only in declared properties.
		/// </summary>
		public DPropertyDesc GetProperty(VariableName propertyName)
		{
			DPropertyDesc property;
			return (Properties.TryGetValue(propertyName, out property) ? property : null);
		}

		//private struct MemberCacheKey<T> : IEquatable<MemberCacheKey<T>>
		//{
		//    public readonly T Name;
		//    public readonly DTypeDesc Context;

		//    public MemberCacheKey(T name, DTypeDesc context)
		//    {
		//        this.Name = name;
		//        this.Context = context;
		//    }

		//    public bool Equals(MemberCacheKey<T> other)
		//    {
		//        return Object.ReferenceEquals(Context, other.Context) && Name.Equals(other.Name);
		//    }

		//    public override int GetHashCode()
		//    {
		//        /*if (Context == null)*/ return Name.GetHashCode();
		//        //return Name.GetHashCode() ^ Context.GetType().GetHashCode();
		//    }
		//}

		//private struct MemberCacheValue<T>
		//{
		//    public T Member;
		//    public GetMemberResult Result;
		//}

		//private Dictionary<MemberCacheKey<VariableName>, MemberCacheValue<DPropertyDesc>> _cache =
		//    new Dictionary<MemberCacheKey<VariableName>, MemberCacheValue<DPropertyDesc>>();

		/// <summary>
		/// Searches in all bases.
		/// </summary>
		public GetMemberResult GetProperty(VariableName propertyName, DTypeDesc context, out DPropertyDesc property)
		{
			return GetMember<VariableName, DPropertyDesc>
				(getPropertyDictionary, propertyName, context, LookupFlags.None, out property);
		}

		/// <summary>
		/// Searches in all bases. Ignores static properties.
		/// </summary>
		public GetMemberResult GetInstanceProperty(VariableName propertyName, DTypeDesc context, out DPropertyDesc property)
		{
			return GetMember<VariableName, DPropertyDesc>
				    (getPropertyDictionary, propertyName, context, LookupFlags.IgnoreStaticMembers, out property);
		}

		/// <summary>
		/// Searches only in declared methods.
		/// </summary>
		public DRoutineDesc GetMethod(Name methodName)
		{
			DRoutineDesc method;
			return (Methods.TryGetValue(methodName, out method) ? method : null);
		}

		/// <summary>
		/// Searches in all bases.
		/// </summary>
		public GetMemberResult GetMethod(Name methodName, DTypeDesc context, out DRoutineDesc method)
		{
            return GetMember<Name, DRoutineDesc>
				(getMethodDictionary, methodName, context, LookupFlags.InheritPrivate, out method);
		}

        /// <summary>
        /// Gets the generic parameter with the specified name.
        /// </summary>
        /// <param name="lowercaseFullName">Lowercase name.</param>
        /// <returns>Generic parameter or <B>null</B> if not found.</returns>
        public GenericParameterDesc GetGenericParameter(string lowercaseFullName)
		{
			for (int i = 0; i < GenericParameters.Length; i++)
			{
				if (GenericParameters[i].RealType.Name.ToLower() == lowercaseFullName)
					return GenericParameters[i];
			}

			return null;
		}

		#endregion

		#region Member Enumeration

		#region Generic Enumeration

		/// <summary>
		/// Enumerates all members from the entire inheritance hierarchy regardless of visibility.
		/// </summary>
		/// <typeparam name="T">Type of member dictionary keys.</typeparam>
		/// <typeparam name="N">Type of member dictionary values.</typeparam>
		/// <param name="dictionary">Delegate that returns the member dictionary for a given <see cref="DTypeDesc"/>.</param>
		/// <param name="flags">Flags that adjust the enumeration.</param>
		/// <returns>The members.</returns>
		private IEnumerable<KeyValuePair<N, T>> EnumerateMembers<N, T>(GetMemberDictionary<N, T>/*!*/ dictionary,
			LookupFlags flags) where T : DMemberDesc
		{
			DTypeDesc type_desc = this;
			do
			{
				foreach (KeyValuePair<N, T> pair in dictionary(type_desc)) yield return pair;

				// enumerate implemented interfaces recursively
				if ((flags & LookupFlags.SearchInterfaces) == LookupFlags.SearchInterfaces)
				{
					for (int i = 0; i < type_desc.Interfaces.Length; i++)
					{
						foreach (KeyValuePair<N, T> pair in type_desc.Interfaces[i].EnumerateMembers<N, T>(dictionary, flags))
						{
							yield return pair;
						}
					}
				}

				type_desc = type_desc.Base;
			}
			while (type_desc != null);
		}

		/// <summary>
		/// Enumarates all members from the entire inheritance hierarchy that are visible in the specified
		/// <paramref name="context"/>.
		/// </summary>
		/// <typeparam name="T">Type of member dictionary keys.</typeparam>
		/// <typeparam name="N">Type of member dictionary values.</typeparam>
		/// <param name="dictionary">Delegate that returns the member dictionary for a given <see cref="DTypeDesc"/>.</param>
		/// <param name="context">Caller context.</param>
		/// <param name="flags">Flags that adjust the enumeration.</param>
		/// <returns>The members.</returns>
		private IEnumerable<KeyValuePair<N, T>> EnumerateMembers<N, T>(GetMemberDictionary<N, T>/*!*/ dictionary,
			DTypeDesc context, LookupFlags flags) where T : DMemberDesc
		{
			bool context_related = (context != null && IsRelatedTo(context));

			DTypeDesc type_desc = this;
			do
			{
				bool in_context = (type_desc == context);

				foreach (KeyValuePair<N, T> pair in dictionary(type_desc))
				{
					// check the member's visibility
					switch (pair.Value.MemberAttributes & PhpMemberAttributes.VisibilityMask)
					{
						case PhpMemberAttributes.Public: break;
						case PhpMemberAttributes.Protected:
							{
								if (context_related) break;
								else continue;
							}
						case PhpMemberAttributes.Private:
							{
								if (in_context) break;
								else continue;
							}
					}

					yield return pair;
				}

				// enumerate implemented interfaces recursively
				if ((flags & LookupFlags.SearchInterfaces) == LookupFlags.SearchInterfaces)
				{
					for (int i = 0; i < type_desc.Interfaces.Length; i++)
					{
						foreach (KeyValuePair<N, T> pair in
							type_desc.Interfaces[i].EnumerateMembers<N, T>(dictionary, context, flags))
						{
							yield return pair;
						}
					}
				}

				type_desc = type_desc.Base;
			}
			while (type_desc != null);
		}

		/// <summary>
		/// Removes overriden members from an enumeration.
		/// </summary>
		/// <typeparam name="N"></typeparam>
		/// <typeparam name="T"></typeparam>
		/// <param name="members">The member enumeration starting from the most derived type.</param>
		/// <returns></returns>
		private static IEnumerable<KeyValuePair<N, T>> RemoveOverridenMembers<N, T>(IEnumerable<KeyValuePair<N, T>> members)
			where T : DMemberDesc
		{
			Dictionary<N, T> cache = new Dictionary<N, T>();

			foreach (KeyValuePair<N, T> pair in members)
			{
				if (pair.Value.IsPublic || pair.Value.IsProtected)
				{
					if (!cache.ContainsKey(pair.Key))
					{
						// return only the members that have not been overriden
						cache.Add(pair.Key, pair.Value);
					}
					else continue;
				}
				yield return pair;
			}
		}

		#endregion

		/// <summary>
		/// Enumerates all properties declared by this type and its bases.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<KeyValuePair<VariableName, DPropertyDesc>> EnumerateProperties()
		{
			return RemoveOverridenMembers(
				EnumerateMembers<VariableName, DPropertyDesc>(getPropertyDictionary, LookupFlags.None));
		}

		/// <summary>
		/// Returns properties visible in the given <paramref name="context"/>.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <returns></returns>
		public IEnumerable<KeyValuePair<VariableName, DPropertyDesc>> EnumerateProperties(DTypeDesc context)
		{
			return RemoveOverridenMembers(
				EnumerateMembers<VariableName, DPropertyDesc>(getPropertyDictionary, context, LookupFlags.None));
		}

		/// <summary>
		/// Enumerates all methods declared by this type and its bases.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<KeyValuePair<Name, DRoutineDesc>> EnumerateMethods()
		{
			return RemoveOverridenMembers(
				EnumerateMembers<Name, DRoutineDesc>(getMethodDictionary, LookupFlags.None));
		}

		/// <summary>
		/// Returns methods visible in the given <paramref name="context"/>.
		/// </summary>
		/// <param name="context">The caller.</param>
		/// <returns></returns>
		public IEnumerable<KeyValuePair<Name, DRoutineDesc>> EnumerateMethods(DTypeDesc context)
		{
			return RemoveOverridenMembers(
				EnumerateMembers<Name, DRoutineDesc>(getMethodDictionary, context, LookupFlags.None));
		}

		#endregion

		#region Run-time Operations

		public abstract object New(PhpStack/*!*/ stack, DTypeDesc caller, NamingContext context);
		public abstract object New(ScriptContext/*!*/ context);

		internal enum MakeGenericArgumentsResult
		{
			IncompatibleConstraint,
			MissingArgument,
			TooManyArguments
		}

		/// <summary>
		/// Substitutes missing arguments by the default types where possible and reports error otherwise.
		/// Updates the specified array and sets the specified argument count to the parameter count.
		/// Expands the arguments array if needed.
		/// </summary>
		/// <returns>Whether some arguments are missing.</returns>
		internal bool MakeGenericArguments(ref DTypeDesc[]/*!*/ arguments, ref int argCount,
		  Action<MakeGenericArgumentsResult, DTypeDesc, DTypeDesc, GenericParameterDesc> onError)
		{
			GenericParameterDesc[] parameters = GenericParameters;

			for (int i = 0; i < parameters.Length; i++)
			{
				DTypeDesc default_type;

				if (i < argCount)
				{
					if (arguments[i] != null && !parameters[i].IsSubstitutableBy(arguments[i]))
					{
						// report error and continue:
						if (onError != null)
							onError(MakeGenericArgumentsResult.IncompatibleConstraint, this, arguments[i], parameters[i]);
					}
				}
				else if ((default_type = GetGenericParameterDefaultType(i)) != null)
				{
					if (i >= arguments.Length)
						Array.Resize(ref arguments, parameters.Length);

					arguments[i] = default_type;
				}
				else
				{
					if (onError != null)
						onError(MakeGenericArgumentsResult.MissingArgument, this, null, parameters[i]);

					argCount = parameters.Length;
					return false;
				}
			}

			if (argCount > parameters.Length)
				onError(MakeGenericArgumentsResult.TooManyArguments, this, null, null);

			argCount = parameters.Length;

			return true;
		}

		/// <summary>
		/// Gets a default type-desc associated with the index-th generic parameter.
		/// Overridden by <see cref="PhpTypeCompletionDesc"/>, which carries run-time resolved default types.
		/// </summary>
		internal virtual DTypeDesc GetGenericParameterDefaultType(int index)
		{
			Debug.Assert(index >= 0 && index < GenericParameters.Length);
			return GenericParameters[index].DefaultType;
		}

		#endregion
	}

	#endregion

	#region DTypeDescs

	/// <summary>
	/// Immutable <see cref="DTypeDesc"/> array with overriden <see cref="GetHashCode"/> and <see cref="Equals"/>.
	/// </summary>
	internal struct DTypeDescs : IEquatable<DTypeDescs>
	{
		public DTypeDesc[]/*!!*/ Types { get { return types; } }
		private readonly DTypeDesc[]/*!!*/ types;

		private readonly int hashCode;

		public int Count { get { return types.Length; } }
		public DTypeDesc/*!*/ this[int i] { get { return types[i]; } }

		public DTypeDescs(DType[]/*!*/ types)
		{
			this.hashCode = 0;
			this.types = new DTypeDesc[types.Length];

			for (int i = 0; i < types.Length; i++)
				this.types[i] = types[i].TypeDesc;

			this.hashCode = CalculateHashCode(this.types);
		}

		public DTypeDescs(DTypeDesc/*!*/ type, DTypeDesc[]/*!*/ types, int typeCount)
		{
			Debug.Assert(typeCount >= 0 && typeCount <= types.Length);

			this.hashCode = 0;
			this.types = new DTypeDesc[1 + typeCount];

			this.types[0] = type;
			for (int i = 0; i < typeCount; i++)
				this.types[i + 1] = types[i];

			this.hashCode = CalculateHashCode(this.types);
		}

		public DTypeDescs(PhpStack/*!*/ stack)
		{
			this.hashCode = 0;
			this.types = new DTypeDesc[stack.TypeArgCount];

			for (int i = 0; i < this.types.Length; i++)
				this.types[i] = stack.PeekType(i + 1);

			this.hashCode = CalculateHashCode(this.types);
		}

		private static int CalculateHashCode(DTypeDesc[]/*!!*/ types)
		{
			int result = 1254645177;

			for (int i = 0; i < types.Length; i++)
				result ^= types[i].GetHashCode();

			return result;
		}

		public override int GetHashCode()
		{
			return hashCode;
		}

		/// <summary>
		/// Converts the types to real <see cref="Type"/> array.
		/// </summary>
		public Type[]/*!!*/ GetRealTypes()
		{
			Type[] result = new Type[types.Length];
			for (int i = 0; i < types.Length; i++)
				result[i] = types[i].RealType;

			return result;
		}

		/// <summary>
		/// Puts real types to a provided <see cref="Type"/> array.
		/// </summary>
		public Type[]/*!*/ GetRealTypes(Type[]/*!*/ realTypes, int offset)
		{
			for (int i = 0; i < types.Length; i++)
				realTypes[i + offset] = types[i].RealType;

			return realTypes;
		}

		#region IEquatable<DTypeDescs> Members

		public bool Equals(DTypeDescs other)
		{
			if (other.types.Length != this.types.Length) return false;

			for (int i = 0; i < types.Length; i++)
			{
				if (!ReferenceEquals(other.types[i], this.types[i])) return false;
			}

			return true;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is DTypeDescs)) return false;
			return Equals((DTypeDescs)obj);
		}

		#endregion
	}

	#endregion

	#region DTypeSpec

	internal delegate DTypeDesc ResolverDelegate(string/*!*/ name, NamingContext namingContext, DTypeDesc caller);

	/// <summary>
	/// Encodes a dynamic type.
	/// </summary>
	internal struct DTypeSpec
	{
		private const int PrimitiveType = 0;
		private const int GenericMethodParam = 1;
		private const int GenericTypeParam = 2;
		private const int RealType = 3;
		private const int ConstructedTypeStart = 4;
		private const int ConstructedTypeEnd = 5;
		private const int NamedType = 10;
		
		private readonly int[]/*!*/ data;
		private readonly byte[] stringData;

		#region Construction

		/// <summary>
		/// The array shouldn't be modified by the caller.
		/// </summary>
		internal DTypeSpec(int[]/*!*/ data)
		{
			this.data = data;
			this.stringData = null;
		}

		public DTypeSpec(PhpTypeCode primitiveTypeCode)
		{
			data = new int[] { PrimitiveType, (int)primitiveTypeCode };
			stringData = null;
		}

		internal DTypeSpec(int[]/*!*/ data, byte[] strings)
		{
			this.data = data;
			this.stringData = strings;
		}
		
		public DTypeSpec(int genericParamIndex, MemberTypes memberType)
		{
			data = new int[] 
      { 
        (memberType == MemberTypes.TypeInfo) ? GenericMethodParam : GenericTypeParam,
        genericParamIndex
      };
			stringData = null;
		}

		public DTypeSpec(Type/*!*/ realType, ModuleBuilder/*!*/ moduleBuilder)
		{
			data = new int[] { RealType, moduleBuilder.GetTypeToken(realType).Token };
			stringData = null;
		}

		public DTypeSpec(string/*!*/ indefiniteTypeName, FieldBuilder namingContext, ModuleBuilder/*!*/ moduleBuilder)
		{
			Debug.Assert(indefiniteTypeName != null && moduleBuilder != null);

			stringData = ArrayUtils.Concat(Encoding.UTF8.GetBytes(indefiniteTypeName), new byte[] { 0 });
			data = new int[] 
			{ 
				NamedType, 
				namingContext != null ? Int32.Parse(namingContext.Name) : 0,
			};

			// On Silverlight 'GetStringConstant' can't be used, because 'ResolveString' isn't available
			/* 
			data = new int[2] 
      {
        moduleBuilder.GetStringConstant(indefiniteTypeName).Token,
        namingContext != null ? Int32.Parse(namingContext.Name) : 0
      };
			*/
		}

		public DTypeSpec(DTypeSpec genericType, DTypeSpec[]/*!*/ arguments)
		{
			// start marker + generic type code + end marker
			int size = 1 + 2 + 1;

			for (int i = 0; i < arguments.Length; i++)
				size += arguments[i].data.Length;

			data = new int[size];

			data[0] = ConstructedTypeStart;
			data[1] = genericType.data[0];
			data[2] = genericType.data[1];

			List<byte> stringList = new List<byte>();
			if (genericType.stringData != null)
				stringList.AddRange(genericType.stringData);

			int offset = 3;
			for (int i = 0; i < arguments.Length; i++)
			{
				Array.Copy(arguments[i].data, 0, data, offset, arguments[i].data.Length);
				// Buffer.BlockCopy(arguments[i].data, 0, data, offset, arguments[i].data.Length);
				offset += arguments[i].data.Length;

				if (arguments[i].stringData != null)
					stringList.AddRange(arguments[i].stringData);
			}

			data[offset] = ConstructedTypeEnd;
			stringData = stringList.ToArray();
			Debug.Assert(offset == size - 1);
		}

		#endregion

		#region Conversion to Attribute

		public CustomAttributeBuilder/*!*/ ToCustomAttributeBuilder()
		{
			if (stringData == null)
			{
				if (data.Length == 2)
					return new CustomAttributeBuilder(Emit.Constructors.DTypeSpec_IntInt, new object[] { data[0], data[1] });
				else
					return new CustomAttributeBuilder(Emit.Constructors.DTypeSpec_IntArray, new object[] { data });
			}
			else
			{
				if (data.Length == 2)
					return new CustomAttributeBuilder(Emit.Constructors.DTypeSpec_IntInt_ByteArray, new object[] { data[0], data[1], stringData });
				else
					return new CustomAttributeBuilder(Emit.Constructors.DTypeSpec_IntArray_ByteArray, new object[] { data, stringData });
			}
		}

		#endregion

		#region Conversion to DTypeDesc

		/// <summary>
		/// Decodes the type-spec to the type-desc within a given context (referring type, routine, and real module).
		/// If the type encoded in the type-spec was unknown when encoding, the specified resolver is called.
		/// Returns <B>null</B> if the data of type-spec are incorrect.
		/// Returns see cref="UnknownTypeSpec" instance if the type is currently unresolvable (or no resolver specified).
		/// </summary>
		public DTypeDesc GetTypeDesc(Module/*!*/ referringModule, DTypeDesc referringType, DRoutineDesc referringRoutine,
		  ResolverDelegate resolver)
		{
			if (data.Length < 2) return null;

			int sptr = 0;
			if (data[0] == ConstructedTypeStart)
			{
				int i = 1;
				return GetConstructedTypeDesc(ref i, ref sptr, referringModule, referringType, referringRoutine, resolver);
			}
			else
			{
				return GetTypeDesc(data[0], data[1], ref sptr, referringModule, referringType, referringRoutine, resolver);
			}
		}

		private DTypeDesc GetTypeDesc(int data0, int data1, ref int stringPtr, Module/*!*/ referringModule, 
			DTypeDesc referringType, DRoutineDesc referringRoutine, ResolverDelegate resolver)
		{
			switch (data0)
			{
				case PrimitiveType:
					return PrimitiveTypeDesc.GetByTypeCode((PhpTypeCode)data1);

				case GenericMethodParam:
					// TODO: currentRoutine
					return null;

				case GenericTypeParam:
					if (referringType == null || data1 < 0 && data1 >= referringType.GenericParameters.Length) return null;

					return referringType.GenericParameters[data1];

				case RealType:
					Type type;
					try
					{
						type = referringModule.ResolveType(data1);
					}
					catch (ArgumentException)
					{
						return null;
					}
					return DTypeDesc.Create(type);

				case ConstructedTypeStart:
				case ConstructedTypeEnd:
					Debug.Fail();
					throw null;

				case NamedType:
					string name;
					NamingContext naming_context = null;

					try
					{
						// Silverlight - no ResolveString
						// name = referringModule.ResolveString(data0);
						name = ReadNextString(stringData, ref stringPtr);
						if (name == null) return null;

						Type global_type = referringModule.GetType(QualifiedName.Global.Name.ToString(), false, false);
						if (global_type == null) return null;

						// fields are named by numbers:
						if (data1 != 0)
						{
							FieldInfo naming_context_fld = global_type.GetField(data1.ToString(), BindingFlags.Static | BindingFlags.NonPublic);

							if (naming_context_fld != null)
							{
								naming_context = naming_context_fld.GetValue(null) as NamingContext;
								if (naming_context == null) return null;
							}
						}
					}
					catch (Exception)
					{
						return null;
					}
					return (resolver != null) ? resolver(name, naming_context, referringType) : null;
					// TODO: new UnknownTypeSpec(name, naming_context);

				default:
					Debug.Fail("TypeSpec - decoding an unknown type!");
					return null;
				
			}
		}

		private string ReadNextString(byte[] stringData, ref int stringPtr)
		{
			int start = stringPtr;
			while (stringData[stringPtr] != 0) stringPtr++;
			string res = Encoding.UTF8.GetString(stringData, start, stringPtr - start);
			stringPtr++;
			return res;
		}

		private DTypeDesc GetConstructedTypeDesc(ref int i, ref int sptr, Module/*!*/ referringModule, DTypeDesc referringType,
		  DRoutineDesc referringRoutine, ResolverDelegate resolver)
		{
			if (i + 1 == data.Length) return null;
			DTypeDesc generic_type = GetTypeDesc(data[i], data[i + 1], ref sptr, referringModule, referringType, referringRoutine, resolver);
			if (generic_type == null) return null;

			List<DTypeDesc> args = new List<DTypeDesc>(generic_type.GenericParameters.Length);
			while (i + 1 < data.Length && data[i] != ConstructedTypeEnd)
			{
				DTypeDesc arg;

				if (data[i] == ConstructedTypeStart)
				{
					i++;
					arg = GetConstructedTypeDesc(ref i, ref sptr, referringModule, referringType, referringRoutine, resolver);
				}
				else
				{
					arg = GetTypeDesc(data[i], data[i + 1], ref sptr, referringModule, referringType, referringRoutine, resolver);
					i += 2;
				}

				if (arg == null) return null;
				args.Add(arg);
			}

			if (data[i] != ConstructedTypeEnd)
				return null;

			i++;
			return Operators.MakeGenericTypeInstantiation(generic_type, args.ToArray(), args.Count);
		}

		#endregion
	}

	#endregion

	#region UnknownTypeDesc

	/// <summary>
	/// Represents a descriptor for unknown type.
	/// Necessary when unknown types are held in lists of descriptors (such as implemented interface list).
	/// </summary>
	public sealed class UnknownTypeDesc : DTypeDesc
	{
        /// <summary>
        /// Singleton instance to be used wherever it is needed.
        /// </summary>
        public static readonly UnknownTypeDesc/*!*/Singleton = new UnknownTypeDesc();

		public override bool IsUnknown { get { return true; } }

		public UnknownTypeDesc()
			: base()
		{
		}

		#region Not Supported

		protected override DType/*!*/ Reflect()
		{
			Debug.Fail();
			return null;
		}

		protected override void ReflectInterfaces()
		{
			Debug.Fail();
		}

		protected override void ReflectMethods()
		{
			Debug.Fail();
		}

		protected override void ReflectProperties()
		{
			Debug.Fail();
		}

		protected override void ReflectConstants()
		{
			Debug.Fail();
		}

		public override object New(PhpStack stack, DTypeDesc caller, NamingContext nameContext)
		{
			Debug.Fail();
			return null;
		}

		public override object New(ScriptContext context)
		{
			Debug.Fail();
			return null;
		}

		#endregion
	}

	#endregion

	#region GlobalTypeDesc

	/// <summary>
	/// Represents a pseudo-type declaring all global functions and types.
	/// </summary>
	public sealed class GlobalTypeDesc : DTypeDesc
	{
		public override bool IsGlobal { get { return true; } }

		public override DModule DeclaringModule { get { return declaringModule; } }
		private readonly DModule/*!*/ declaringModule;

		public GlobalTypeDesc(DModule/*!*/ declaringModule)
			: base()
		{
			this.declaringModule = declaringModule;

			// we don't need to create tables per module, all entities are stored on ApplicationContext:
			this.methods = null;
			this.properties = null;
			this.constants = null;
		}

		#region Not Supported

		protected override DType/*!*/ Reflect()
		{
			Debug.Fail();
			return null;
		}

		protected override void ReflectInterfaces()
		{
			Debug.Fail();
		}

		protected override void ReflectMethods()
		{
			Debug.Fail();
		}

		protected override void ReflectProperties()
		{
			Debug.Fail();
		}

		protected override void ReflectConstants()
		{
			Debug.Fail();
		}

		public override object New(PhpStack stack, DTypeDesc caller, NamingContext nameContext)
		{
			Debug.Fail();
			return null;
		}

		public override object New(ScriptContext context)
		{
			Debug.Fail();
			return null;
		}

		#endregion
	}

	#endregion

	#region GericParameterDesc

	/// <summary>
	/// Represents a pseudo-type declaring all global functions and types.
	/// </summary>
	public sealed class GenericParameterDesc : DTypeDesc
	{
		public static new readonly GenericParameterDesc[]/*!*/ EmptyArray = new GenericParameterDesc[0];

		public GenericParameter GenericParameter { get { return (GenericParameter)Type; } }

		public override bool IsGlobal { get { return true; } }

		public DTypeDesc DefaultType { get { return defaultType; } }
		private DTypeDesc defaultType;

		#region Construction

		/// <summary>
		/// Used by the compiler.
		/// </summary>
		internal GenericParameterDesc()
			: base()
		{
			this.defaultType = null;  // to be written up
		}

		internal void WriteUp(DTypeDesc defaultType)
		{
			this.defaultType = defaultType;
		}

		/// <summary>
		/// Used by the reflection.
		/// </summary>
		public GenericParameterDesc(Type/*!*/ realType, DTypeDesc defaultType)
			: base(realType, null)
		{
			this.defaultType = defaultType;
		}

		#endregion

		/// <summary>
		/// Gets whether the argument can be substituted for this parameters.
		/// </summary>
		internal bool IsSubstitutableBy(DTypeDesc/*!*/ argument)
		{
			// if real type is null, the parameter is a PHP generic parameter, which can define no constraints:
			return (this.RealType == null) || argument.IsCompatibleWithGenericParameter(this.RealType);
		}

		#region Reflection

		protected override DType/*!*/ Reflect()
		{
			Debug.Fail();
			throw null;
		}

		protected override void ReflectInterfaces()
		{
			Debug.Fail();
			throw null;
		}

		protected override void ReflectMethods()
		{
			Debug.Fail();
			throw null;
		}

		protected override void ReflectProperties()
		{
			Debug.Fail();
			throw null;
		}

		protected override void ReflectConstants()
		{
			Debug.Fail();
		}

		#endregion

		#region Runtime Operations

		public override object New(PhpStack/*!*/ stack, DTypeDesc caller, NamingContext nameContext)
		{
			Debug.Fail();
			return null;
		}

		public override object New(ScriptContext/*!*/ context)
		{
			Debug.Fail();
			return null;
		}

		#endregion
	}

	#endregion

	#region GenericTypeDefinition

	/// <summary>
	/// Shared by all instantiations of the generic type and the type itself.
	/// </summary>
	public sealed class GenericTypeDefinition
	{
		public DTypeDesc/*!*/ GenericType { get { return genericType; } }
		private readonly DTypeDesc/*!*/ genericType;

		public GenericParameterDesc[]/*!!*/ GenericParameters
		{
			get
			{
				if (_genericParameters == null)
				{
					_genericParameters = genericType.ReflectGenericParameters(null, null, null);
					Debug.AssertAllNonNull(_genericParameters);
				}

				return _genericParameters;
			}
		}
		private GenericParameterDesc[] _genericParameters;

		public GenericTypeDefinition(DTypeDesc/*!*/ genericType)
		{
			Debug.Assert(genericType != null);

			this.genericType = genericType;
			this._genericParameters = null;
		}

		public GenericTypeDefinition(DTypeDesc/*!*/ genericType, GenericParameterDesc[]/*!!*/ genericParameters)
		{
			Debug.AssertAllNonNull(genericParameters);
			Debug.Assert(genericType != null && genericParameters.Length > 0);

			this.genericType = genericType;
			this._genericParameters = genericParameters;
		}
	}

	#endregion

	#region ConstructedTypeDesc

	public sealed class ConstructedTypeDesc : DTypeDesc
	{
		public override bool IsUnknown { get { return false; } }

		public DTypeDesc/*!*/ GenericType { get { return genericType; } }
		private readonly DTypeDesc/*!*/ genericType;

		public DTypeDesc[]/*!!*/ Arguments { get { return arguments; } }
		private readonly DTypeDesc[]/*!!*/ arguments;

		/// <summary>
		/// Used by <see cref="ConstructedType"/>.
		/// </summary>
		internal ConstructedTypeDesc(DTypeDesc/*!*/ genericType, DTypeDesc[]/*!!*/ arguments)
			: base(genericType.Base, null, genericType.GenericDefinition)
		{
			Debug.Assert(genericType != null && arguments != null && arguments.Length > 0);

			this.memberAttributes = genericType.MemberAttributes;
			this.genericType = genericType;
			this.arguments = arguments;
		}

		#region Reflection

		protected override DType/*!*/ Reflect()
		{
			Debug.Fail("Constructed types cannot be created at run-time. Instantiations are.");
			throw null;
		}

		protected override void ReflectInterfaces()
		{
			this.interfaces = genericType.Interfaces;
		}

		protected override void ReflectMethods()
		{
			this.methods = genericType.Methods;
		}

		protected override void ReflectProperties()
		{
			this.properties = genericType.Properties;
		}

		protected override void ReflectConstants()
		{
			this.constants = genericType.Constants;
		}

		#endregion

		#region Run-time Operations

		public override object New(PhpStack/*!*/ stack, DTypeDesc caller, NamingContext nameContext)
		{
			Debug.Fail();
			return null;
		}

		public override object New(ScriptContext/*!*/ context)
		{
			Debug.Fail();
			return null;
		}

		#endregion

		#region Utils

		internal static string MakeGenericFullName(DTypeDesc GenericType, DTypeDesc[] Arguments)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		internal static void GenericArgumentsToString(Type[]/*!!*/ arguments, StringBuilder/*!*/ result)
		{
			Debug.Assert(arguments != null && result != null);

			result.Append("<:");

			for (int i = 0; i < arguments.Length; i++)
			{
				if (i > 0) result.Append(',');
				GetFullGenericName(arguments[i], result);
			}

			result.Append(":>");
		}

		#endregion
	}

	#endregion

	#region RuntimePhpTypeDesc

	[DebuggerNonUserCode]
	public sealed class PhpTypeCompletionDesc : DTypeDesc
	{
		public override bool IsUnknown { get { return false; } }

		public override bool IsGenericDefinition
		{
			get { return genericDefinition != null && ReferenceEquals(genericDefinition.GenericType, incompleteType); }
		}

		public PhpTypeDesc/*!*/ IncompleteType { get { return incompleteType; } }
		private readonly PhpTypeDesc/*!*/ incompleteType;

		public GenericParameterDesc[]/*!!*/ CompleteGenericParameters { get { return completeGenericParameters; } }
		private readonly GenericParameterDesc[]/*!!*/ completeGenericParameters;

		/// <summary>
		/// Used by <c>ScriptContext.DeclareType</c>.
		/// </summary>
		internal PhpTypeCompletionDesc(PhpTypeDesc/*!*/ incompleteType, GenericParameterDesc[]/*!!*/ completeGenericParameters)
			: base(incompleteType.Base, incompleteType.RealType, incompleteType.GenericDefinition)
		{
			Debug.Assert(incompleteType != null && completeGenericParameters != null);

			this.memberAttributes = incompleteType.MemberAttributes;
			this.incompleteType = incompleteType;
			this.completeGenericParameters = completeGenericParameters;
		}

		internal override DTypeDesc GetGenericParameterDefaultType(int index)
		{
			Debug.Assert(index >= 0 && index < completeGenericParameters.Length);
			return completeGenericParameters[index].DefaultType;
		}

		#region Reflection

		protected override DType/*!*/ Reflect()
		{
			return incompleteType.Type; // TODO:
		}

		protected override void ReflectInterfaces()
		{
			this.interfaces = incompleteType.Interfaces;
		}

		protected override void ReflectMethods()
		{
			this.methods = incompleteType.Methods;
		}

		protected override void ReflectProperties()
		{
			this.properties = incompleteType.Properties;
		}

		protected override void ReflectConstants()
		{
			this.constants = incompleteType.Constants;
		}

		#endregion

		#region Run-time Operations

		public override object New(PhpStack/*!*/ stack, DTypeDesc caller, NamingContext nameContext)
		{
			return incompleteType.New(stack, caller, nameContext);
		}

		public override object New(ScriptContext/*!*/ context)
		{
			return incompleteType.New(context);
		}

		#endregion
	}

	#endregion

	#region PrimitiveTypeDesc

	[DebuggerNonUserCode]
    public sealed class PrimitiveTypeDesc : DTypeDesc
	{
		public override bool IsUnknown { get { return false; } }

		public PhpTypeCode TypeCode { get { return typeCode; } }
		private readonly PhpTypeCode typeCode;

		internal PrimitiveTypeDesc(Type/*!*/ realType, PhpTypeCode typeCode)
			: base(UnknownModule.RuntimeModule, realType, null, PhpMemberAttributes.Public | PhpMemberAttributes.Final)
		{
			this.typeCode = typeCode;
			this.WriteUpBaseType(null);
			this.WriteUpInterfaces(DTypeDesc.EmptyArray);
		}

		#region Utils

		public override string MakeFullName()
		{
			switch (typeCode)
			{
				case PhpTypeCode.Boolean: return PhpVariable.TypeNameBool;
				case PhpTypeCode.Integer: return PhpVariable.TypeNameInt;
				case PhpTypeCode.LongInteger: return PhpVariable.TypeNameLongInteger;
				case PhpTypeCode.Double: return PhpVariable.TypeNameDouble;
				case PhpTypeCode.String: return PhpVariable.TypeNameString;
				case PhpTypeCode.PhpResource: return PhpResource.PhpTypeName;
				case PhpTypeCode.PhpArray: return PhpArray.PhpTypeName;
				case PhpTypeCode.DObject: return PhpObject.PhpTypeName;
				default: throw null;
			}
		}

		internal static string GetPrimitiveName(Type/*!*/ realType)
		{
			Debug.Assert(realType != null);

			switch (System.Type.GetTypeCode(realType))
			{
				case System.TypeCode.Object:

					if (realType == typeof(PhpArray)) return PhpArray.PhpTypeName;

					if (realType == typeof(PhpBytes)) return PhpBytes.PhpTypeName;
					if (realType == typeof(PhpString)) return PhpString.PhpTypeName;

					if (realType == typeof(PhpObject)) return PhpObject.PhpTypeName;
					if (realType == typeof(DObject)) return DObject.PhpTypeName;
					if (realType == typeof(ClrObject)) return ClrObject.PhpTypeName;

					if (realType.IsSubclassOf(typeof(PhpResource))) return PhpResource.PhpTypeName;

					break;

				case System.TypeCode.Double: return PhpVariable.TypeNameDouble;
				case System.TypeCode.Int32: return PhpVariable.TypeNameInt;
				case System.TypeCode.Int64: return PhpVariable.TypeNameLongInteger;
				case System.TypeCode.Boolean: return PhpVariable.TypeNameBool;
				case System.TypeCode.String: return PhpVariable.TypeNameString;
			}

			return null;
		}

		#endregion

		#region Reflection

		protected override DType/*!*/ Reflect()
		{
			switch (typeCode)
			{
				case PhpTypeCode.Boolean: return PrimitiveType.Boolean;
				case PhpTypeCode.Integer: return PrimitiveType.Integer;
				case PhpTypeCode.LongInteger: return PrimitiveType.LongInteger;
				case PhpTypeCode.Double: return PrimitiveType.Double;
				case PhpTypeCode.String: return PrimitiveType.String;
				case PhpTypeCode.PhpResource: return PrimitiveType.Resource;
				case PhpTypeCode.PhpArray: return PrimitiveType.Array;
				case PhpTypeCode.DObject: return PrimitiveType.Object;
				default: throw null;
			}
		}

		protected override void ReflectInterfaces()
		{
			interfaces = DTypeDesc.EmptyArray;
		}

		protected override void ReflectMethods()
		{
			// TODO: fill methods
			Debug.Fail();
		}

		protected override void ReflectProperties()
		{
			// TODO: fill properties
			Debug.Fail();
		}

		protected override void ReflectConstants()
		{
			// TODO: fill constants
			Debug.Fail();
		}

		#endregion

		#region Runtime Operations

		public override object New(PhpStack/*!*/ stack, DTypeDesc caller, NamingContext nameContext)
		{
			return this.New(null);
		}

		public override object New(ScriptContext context)
		{
			switch (typeCode)
			{
				case PhpTypeCode.Boolean: return false;
				case PhpTypeCode.Integer: return 0;
				case PhpTypeCode.LongInteger: return (long)0;
				case PhpTypeCode.Double: return (double)0.0;
				case PhpTypeCode.String: return "";
				case PhpTypeCode.PhpResource: return null;
				case PhpTypeCode.PhpArray: return new PhpArray();
				case PhpTypeCode.DObject:
					if (Configuration.Application.Compiler.ClrSemantics)
						return ClrObject.Create(new object());
					else
						return new Library.stdClass(context);

				default: throw null;
			}
		}

		#endregion

		#region Statics

		public static PrimitiveTypeDesc GetByTypeCode(PhpTypeCode typeCode)
		{
			switch (typeCode)
			{
				case PhpTypeCode.Boolean: return BooleanTypeDesc;
				case PhpTypeCode.Integer: return IntegerTypeDesc;
				case PhpTypeCode.LongInteger: return LongIntegerTypeDesc;
				case PhpTypeCode.Double: return DoubleTypeDesc;
				case PhpTypeCode.String: return StringTypeDesc;
				case PhpTypeCode.PhpResource: return ResourceTypeDesc;
				case PhpTypeCode.PhpArray: return ArrayTypeDesc;
				case PhpTypeCode.DObject: return ObjectTypeDesc;
				default: return null;
			}
		}

		#endregion

	}

	#endregion

	#region PhpTypeDesc

	/// <summary>
	/// Represents PHP type, generic PHP type template, or PHP type instantiation.
	/// </summary>
	[DebuggerNonUserCode]
	public sealed class PhpTypeDesc : DTypeDesc
	{
        ///// <summary>
        ///// If <B>true</B>, the real type does not contain the <c>__PopulateTypeDesc</c> method and
        ///// further attempts to find it should be avoided.
        ///// </summary>
        //private bool hasNoPopulateMethod;

		/// <summary>
		/// <B>True</B> iff methods have been fully reflected (every <see cref="DRoutineDesc"/> in
		/// <see cref="DTypeDesc.methods"/> has its <see cref="PhpMethod"/>).
		/// </summary>
		private bool methodsReflected;

        /// <summary>
		/// <B>True</B> iff fields and class constants have been fully reflected (every <see cref="DPropertyDesc"/> in
		/// <see cref="DTypeDesc.properties"/> has its <see cref="PhpField"/> and every <see cref="DConstantDesc"/> in
		/// <see cref="DTypeDesc.constants"/> has its <see cref="ClassConstant"/>).
		/// </summary>
		private bool fieldsAndConstantsReflected;

		/// <summary>
		/// Points to this type's <c>__InitializeStaticFields</c> method.
		/// </summary>
		private Action<ScriptContext> initializeStaticFields;

		#region Construction

		/// <summary>
		/// To be used by compiler.
		/// </summary>
		public PhpTypeDesc(DModule/*!*/ declaringModule, PhpMemberAttributes memberAttributes)
			: base(declaringModule, memberAttributes)
		{
		}

		/// <summary>
		/// To be used by run-time reflection.
		/// </summary>
		public PhpTypeDesc(DModule/*!*/ declaringModule, Type/*!*/ realType, DTypeDesc baseDesc, PhpMemberAttributes memberAttributes)
			: base(declaringModule, realType, baseDesc, memberAttributes)
		{
		}

		/// <summary>
		/// To be used at run-time.
		/// </summary>
		[Emitted]
		public static new PhpTypeDesc/*!*/ Create(RuntimeTypeHandle realTypeHandle)
		{
			return (PhpTypeDesc)DTypeDesc.Create(realTypeHandle);
		}

		#endregion

		#region Reflection

		protected override DType/*!*/ Reflect()
		{
			// this type-desc represents non-generic type or the generic definition => create the ClrType for it:
			if (!RealType.IsGenericType || RealType.IsGenericTypeDefinition)
				return new PhpType(this, QualifiedName.FromClrNotation(RealType));

			// this type-desc represents a generic type instantiation; 
			// all instantiations of a single generic type share the same ClrType => do not create new ClrType;
			// note: we could do this faster if each type-desc had a reference to the generic type-desc.
			return DTypeDesc.Create(RealType.GetGenericTypeDefinition()).Type;
		}

		internal void FullReflect()
		{
			// members might have been fully reflected before
            if (!methodsReflected)
                lock (this) { FullReflectMethodsNoLock(); }

            if (!fieldsAndConstantsReflected)
                lock (this) { FullReflectFieldsAndConstantsNoLock(); }
		}

		/// <summary>
		/// Used by PhpType constructor invoked via <see cref="Reflect"/>.
		/// </summary>
		private void FullReflectMethodsNoLock()
		{
            // make sure that methods are at least fast reflected
			Dictionary<Name, DRoutineDesc> methods = this.Methods;

            if (methodsReflected)
                return;

            try
            {
                MethodInfo[] real_methods = RealType.GetMethods(MembersReflectionBindingFlags);

                Dictionary<string, MethodInfo> argless_stubs = new Dictionary<string, MethodInfo>(real_methods.Length / 3);

                // first pass - fill argless_stubs
                for (int i = 0; i < real_methods.Length; i++)
                {
                    MethodInfo info = real_methods[i];

                    if ((info.Attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.PrivateScope)
                    {
                        if (PhpFunctionUtils.IsArglessStub(info, null))
                        {
                            argless_stubs.Add(info.Name, info);
                            real_methods[i] = null;
                        }
                    }
                    else real_methods[i] = null; // expunge private scope methods
                }

                // second pass - match argfulls
                int methods_created = 0;
                for (int i = 0; i < real_methods.Length; i++)
                {
                    MethodInfo info = real_methods[i];

                    // argfulls detection:
                    if (info != null && PhpFunctionUtils.IsArgfullOverload(info, null))
                    {
                        string name_str = info.Name;
                        Name name = new Name(name_str);

                        DRoutineDesc method_desc;
                        MethodInfo argless_info = null;

                        PhpMemberAttributes attrs = Enums.GetMemberAttributes(info);

                        if (methods.TryGetValue(name, out method_desc))
                        {
                            // this method has been populated -> we have a PhpRoutineDesc

                            if ((attrs & PhpMemberAttributes.Abstract) != PhpMemberAttributes.Abstract &&
                                !argless_stubs.TryGetValue(name_str, out argless_info))
                            {
                                throw new ReflectionException(CoreResources.GetString("argless_stub_not_found", name_str));
                            }
                        }
                        else
                        {
                            // this method has not been populated -> create a new PhpRoutineDesc

                            if ((attrs & PhpMemberAttributes.Abstract) != PhpMemberAttributes.Abstract &&
                                !argless_stubs.TryGetValue(name_str, out argless_info))
                            {
                                // argless has to be generated on-the-fly
                                throw new NotImplementedException("Generating argless stubs for imported PHP types is not yet implemented");
                            }

                            if (argless_info == null)
                            {
                                // abstract methods have no argless
                                method_desc = new PhpRoutineDesc(this, attrs);
                            }
                            else
                            {
                                method_desc = new PhpRoutineDesc(
                                    this,
                                    attrs,
                                    (RoutineDelegate)Delegate.CreateDelegate(Types.RoutineDelegate, argless_info));
                            }

                            methods.Add(name, method_desc);
                        }

                        if (method_desc.Member == null)
                        {
                            PhpMethod method = new PhpMethod(name, (PhpRoutineDesc)method_desc, info, argless_info);
                            method.WriteUp(PhpRoutineSignature.FromArgfullInfo(method, info));
                            method_desc.Member = method;

                            methods_created++;
                        }
                    }
                }

                if (methods_created != methods.Count) throw new ReflectionException(CoreResources.GetString("not_all_methods_reflected"));
            }
            finally
            {
                methodsReflected = true;
            }
		}

		/// <summary>
		/// Used by PhpType constructor invoked via <see cref="Reflect"/>.
		/// </summary>
        private void FullReflectFieldsAndConstantsNoLock()
		{
            // make sure that props and consts are at least fast reflected
			Dictionary<VariableName, DPropertyDesc> properties = this.Properties;
			Dictionary<VariableName, DConstantDesc> constants = this.Constants;

			if (fieldsAndConstantsReflected)
                return;

            try
            {
                // get all real properties (fields and constants are exported to properties)
                Dictionary<string, PropertyInfo> real_properties = new Dictionary<string, PropertyInfo>();

                int fields_created = 0, constants_created = 0;

                // faster than GetProperties
                RealType.FindMembers(
                    MemberTypes.Property,
                    MembersReflectionBindingFlags,
                    delegate(MemberInfo m, object _)
                    {
                        var info = (PropertyInfo)m;

                        if (PhpVisibleAttribute.Reflect(info) != null)
                        {
                            // currently reflected just because of XmlDom extension, properties are not static and public

                            var name = new VariableName(info.Name.TrimEnd('#'));
                            DPropertyDesc property_desc;
                            if (properties.TryGetValue(name, out property_desc))
                                return false;

                            // create DPropertyDesc
                            property_desc = new DPhpFieldDesc(this, Enums.GetPropertyAttributes(info));
                            properties.Add(name, property_desc);

                            // remember PropertyInfo
                            property_desc.Member = new PhpVisibleProperty(name, property_desc, info);

                            fields_created++;
                        }
                        else
                        {
                            // checked later as an exported property
                            real_properties[m.Name] = info;
                        }
                        return false;
                    },
                    null);

                // faster than GetFields
                RealType.FindMembers(
                    MemberTypes.Field,
                    MembersReflectionBindingFlags,
                    delegate(MemberInfo m, object _)
                    {
                        FieldInfo info = (FieldInfo)m;

                        if (IsPhpField(info))
                        {
                            // field
                            VariableName name = new VariableName(info.Name.TrimEnd('#'));

                            DPropertyDesc property_desc;
                            if (!properties.TryGetValue(name, out property_desc))
                            {
                                // this property has not been populated -> create a new DPhpFieldDesc

                                property_desc = new DPhpFieldDesc(this, Enums.GetMemberAttributes(info));
                                properties.Add(name, property_desc);
                            }

                            if (property_desc.Member == null)
                            {
                                PropertyInfo exported_prop;
                                real_properties.TryGetValue(name.Value, out exported_prop);

                                property_desc.Member = new PhpField(name, property_desc, info, exported_prop);
                                fields_created++;
                            }
                        }
                        else if (IsPhpConstant(info))
                        {
                            // class constant
                            VariableName name = new VariableName(info.Name.TrimEnd('#'));

                            DConstantDesc constant_desc;
                            if (!constants.TryGetValue(name, out constant_desc))
                            {
                                // this constant has not been populated -> create a new DConstantDesc

                                constant_desc = new DConstantDesc(
                                    this,
                                    PhpMemberAttributes.Public | PhpMemberAttributes.Static,
                                    info.GetValue(null));

                                if (!info.IsInitOnly && !info.IsLiteral)    // deferred constant value
                                    constant_desc.ValueIsDeferred = true;

                                constants.Add(name, constant_desc);
                            }

                            if (constant_desc.Member == null)
                            {
                                constant_desc.Member = new ClassConstant(name, constant_desc, info);
                                constants_created++;
                            }
                        }

                        return false;
                    },
                    null);

                // reflect fields that are not implemented by this type but are declared and "visibility-upgraded"
                object[] attrs = RealType.GetCustomAttributes(typeof(PhpPublicFieldAttribute), false);
                for (int i = 0; i < attrs.Length; i++)
                {
                    PhpPublicFieldAttribute pubf_attr = (PhpPublicFieldAttribute)attrs[i];

                    VariableName name = new VariableName(pubf_attr.FieldName);

                    // try to find the implementor
                    DPropertyDesc implementing_property_desc;
                    if (Base != null && Base.GetProperty(name, this, out implementing_property_desc) == GetMemberResult.OK)
                    {
                        DPropertyDesc property_desc;
                        if (!properties.TryGetValue(name, out property_desc))
                        {
                            // this property has not been populated -> create a new DPhpFieldDesc

                            PhpMemberAttributes member_attrs = implementing_property_desc.MemberAttributes;
                            member_attrs &= ~(PhpMemberAttributes.Protected | PhpMemberAttributes.Private);
                            member_attrs |= PhpMemberAttributes.Public;

                            property_desc = new DPhpFieldDesc(this, member_attrs);
                            properties.Add(name, property_desc);
                        }

                        if (property_desc.Member == null)
                        {
                            PropertyInfo exported_prop;
                            real_properties.TryGetValue(name.Value, out exported_prop);

                            property_desc.Member = new PhpField(
                                name,
                                property_desc,
                                implementing_property_desc,
                                pubf_attr.HasInitValue,
                                exported_prop);

                            fields_created++;
                        }
                    }
                    else throw new ReflectionException(CoreResources.GetString("field_implementor_not_found"));
                }

                //if (fields_created != properties.Count) throw new ReflectionException(CoreResources.GetString("not_all_fields_reflected"));
                //if (constants_created != constants.Count) throw new ReflectionException(CoreResources.GetString("not_all_constants_reflected"));

            }
            finally
            {
                fieldsAndConstantsReflected = true;
            }
		}

		/// <summary>
		/// <para>
		/// Reflectes generic parameters of a generic definition within a specified script context at run-time.
		/// </para>
		/// 
		/// <para>
		/// Note, that all PHP generic type definitions have to reflect their generic parameters at the time of
		/// their declaration and not lazily (as they must resolve type names in the appropriate state of the script context).
		/// </para> 
		/// </summary>
		internal override GenericParameterDesc[]/*!!*/ ReflectGenericParameters(DTypeDesc referringType, DRoutineDesc referringRoutine,
			ResolverDelegate resolver)
		{
			if (this.IsGenericDefinition)
			{
				Type[] real_params = RealType.GetGenericArguments();
				GenericParameterDesc[] descs = new GenericParameterDesc[real_params.Length];

				for (int i = 0; i < real_params.Length; i++)
				{
					DTypeDesc default_type;
					DTypeSpecAttribute default_type_attr = DTypeSpecAttribute.Reflect(real_params[i]);
					if (default_type_attr != null)
					{
						default_type = default_type_attr.TypeSpec.GetTypeDesc(RealType.Module, referringType,
							referringRoutine, resolver);
					}
					else
					{
						default_type = null;
					}

					descs[i] = new GenericParameterDesc(real_params[i], default_type);
				}
				return descs;
			}
			else
			{
				return GenericParameterDesc.EmptyArray;
			}
		}

		#region Unit Test
#if DEBUG

		[Test]
		private static void TestPhpObjectInterfaces()
		{
			Type[] ifaces = typeof(PhpObject).GetInterfaces();
			Type[] expected = new Type[]
				{ 
					typeof(PHP.Core.IPhpVariable),
					typeof(PHP.Core.IPhpConvertible),
					typeof(PHP.Core.IPhpPrintable),
					typeof(PHP.Core.IPhpCloneable),
					typeof(PHP.Core.IPhpComparable),
					typeof(PHP.Core.IPhpObjectGraphNode),
					typeof(PHP.Core.IPhpEnumerable),
					typeof(System.IDisposable),
                    typeof(System.Dynamic.IDynamicMetaObjectProvider),
#if !SILVERLIGHT
					typeof(System.Runtime.Serialization.ISerializable),
					typeof(System.Runtime.Serialization.IDeserializationCallback) 
#endif
				};

            Debug.Assert(ifaces.Length == expected.Length && ArrayEx.TrueForAll(ifaces, delegate(System.Type iface)
			{
				return Array.IndexOf(expected, iface) != -1;
			}), "ReflectInterfaces must be updated if PhpObject implements different interfaces than listed");
		}

#endif
		#endregion

		private static bool IsRealInterfaceHidden(Type/*!*/ realType, Type/*!*/ realInterface)
		{
			// non public:
			if (!realInterface.IsPublic)
				return true;

			// Core interfaces:
			if (realInterface.Namespace == Namespaces.Core)
				return true;

			// Note: on silverlight we're using "stubs"
			bool isPhpObjectInterface = (realInterface == typeof(System.IDisposable) ||
				realInterface == typeof(ISerializable) ||
				realInterface == typeof(IDeserializationCallback) ||
                realInterface == typeof(System.Dynamic.IDynamicMetaObjectProvider));

			// interfaces implemented by PhpObject/DObject:
			if (isPhpObjectInterface)
			{
				InterfaceMapping map = realType.GetInterfaceMap(realInterface);

				for (int i = 0; i < map.TargetMethods.Length; i++)
				{
					Debug.Assert(map.TargetMethods[i] != null, "Interface method is always implemented, at least by DObject");
					Debug.Assert(map.TargetMethods[i].DeclaringType != typeof(PhpObject), "PhpObject shouldn't implement any method of any interface");

					if (map.TargetMethods[i].DeclaringType == typeof(DObject))
						return true;
				}
			}

			return false;
		}

        protected override void ReflectInterfaces()
        {
            Type[] real_interfaces = RealType.GetInterfaces();
            if (real_interfaces == null || real_interfaces.Length == 0)
            {
                this.interfaces = DTypeDesc.EmptyArray;
            }
            else
            {
                List<DTypeDesc> iface_list = null;

                for (int i = 0; i < real_interfaces.Length; i++)
                {
                    if (!IsRealInterfaceHidden(RealType, real_interfaces[i]))
                    {
                        if (iface_list == null)
                            iface_list = new List<DTypeDesc>(real_interfaces.Length - i);

                        iface_list.Add(DTypeDesc.Create(real_interfaces[i]));
                    }
                }

                interfaces = (iface_list != null) ? iface_list.ToArray() : DTypeDesc.EmptyArray;
            }
        }

        protected override void ReflectMethods()
        {
            if (methods == null)
                if (!AutoPopulateNoLock())
                {
                    methods = new Dictionary<Name, DRoutineDesc>();

                    // fall back to reflection (and make it full)
                    FullReflectMethodsNoLock();
                }
        }

		protected override void ReflectProperties()
		{
			ReflectFieldsAndConstants();
		}

		protected override void ReflectConstants()
		{
			ReflectFieldsAndConstants();
		}

        private void ReflectFieldsAndConstants()
        {
            if (properties == null || constants == null)
                if (!AutoPopulateNoLock())
                {
                    properties = new Dictionary<VariableName, DPropertyDesc>();
                    constants = new Dictionary<VariableName, DConstantDesc>();

                    // fall back to reflection (and make it full)
                    FullReflectFieldsAndConstantsNoLock();
                }
        }

        private bool IsPhpConstant(FieldInfo/*!*/ info)
        {
            return (info.IsStatic && info.IsPublic && (info.IsLiteral || info.IsInitOnly || info.FieldType == typeof(object)/*lazily initialized non-literal constant*/) &&
                (
                    info.FieldType == typeof(object) ||
                    info.FieldType == typeof(int) ||
                    info.FieldType == typeof(long) ||
                    info.FieldType == typeof(bool) ||
                    info.FieldType == typeof(double) ||
                    info.FieldType == typeof(string)
                ));
        }

		private bool IsPhpField(FieldInfo/*!*/ info)
		{
			FieldAttributes attrs_mask = FieldAttributes.InitOnly;
			FieldAttributes attrs_value = 0;

			return ((info.Attributes & attrs_mask) == attrs_value && info.FieldType == Types.PhpReference[0]);
		}

		#endregion

		#region Auto population

		/// <summary>
		/// Tries to populate methods, properties, and constants by invoking the <c>__PopulateTypeDesc</c> method.
		/// </summary>
		/// <returns><B>True</B> if this instance was successfully populated, <B>false</B> otherwise.</returns>
		private bool AutoPopulateNoLock()
		{
            return false;/* // (JM) we really need to reflect it; to get MethodInfo or aglesses and argfulls (can be done through the metadata token, but it is the same)
            if (RealType == null || hasNoPopulateMethod) return false;

            if (methods == null)
            {
                MethodInfo populator = null;
                try
                {
                    populator = RealType.GetMethod(
                        PhpObjectBuilder.PopulateTypeDescMethodName,
                        BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly,
                        null,
                        Types.PhpTypeDesc,
                        null);
                }
                catch (AmbiguousMatchException)
                { }

                if (populator != null)
                {
                    methods = new Dictionary<Name, DRoutineDesc>();
                    properties = new Dictionary<VariableName, DPropertyDesc>();
                    constants = new Dictionary<VariableName, DConstantDesc>();

                    populator.Invoke(null, new object[] { this });

                    return true;
                }
                else
                {
                    hasNoPopulateMethod = true;
                    return false;
                }
            }
            else
            {
                return true;
            }*/
		}

		/// <summary>
		/// To be called by the generated <c>__PopulateTypeDesc</c>.
		/// </summary>
		[Emitted]
		public void SetStaticInit(Action<ScriptContext> staticInit)
		{
			initializeStaticFields = staticInit;
		}

		/// <summary>
		/// To be called by the generated <c>__PopulateTypeDesc</c>.
		/// </summary>
		[Emitted]
		public void AddMethod(string name, PhpMemberAttributes attrs, RoutineDelegate arglessStub)
		{
			DRoutineDesc method = new PhpRoutineDesc(this, attrs, arglessStub);
			methods.Add(new Name(name), method);
		}

		/// <summary>
		/// To be called by the generated <c>__PopulateTypeDesc</c>.
		/// </summary>
		[Emitted]
		public void AddProperty(string name, PhpMemberAttributes attrs, GetterDelegate getter, SetterDelegate setter)
		{
			DPropertyDesc property = new DPhpFieldDesc(this, attrs, getter, setter);
			properties.Add(new VariableName(name), property);
		}

		/// <summary>
		/// To be called by generated helpers.
		/// </summary>
		[Emitted]
		public void AddConstant(string name, object value)
		{
            //Debug.Fail("Add      public static readonly object " + name + " = " + "(int)" + value.ToString() + "; instead of AddConstant usage.");

			DConstantDesc constant = new DConstantDesc(this, PhpMemberAttributes.Public | PhpMemberAttributes.Static, value);
			constants.Add(new VariableName(name), constant);
		}

		#endregion

		#region Run-time Operations

		public override object New(PhpStack stack, DTypeDesc caller, NamingContext nameContext)
		{
			// note that that this method may actually return a ClrObject
			DObject result = PhpFunctionUtils.InvokeConstructor(this, /*Types.ScriptContext_DTypeDesc,*/
				stack.Context, caller);

			return result;
		}

		public override object New(ScriptContext context)
		{
			if (typeof(PhpObject).IsAssignableFrom(RealType))
			{
				// this type is a PhpObject descendant -> no unwanted CLR constructor is in our way
				return PhpFunctionUtils.InvokeConstructor(this, /*Types.ScriptContext_Bool,*/ context, false);
			}
			else
			{
#if SILVERLIGHT
				// SILVERLIGHT: TODO...?
				throw new NotSupportedException("PhpTypeDesc.New - creating uninitalized object is not supported");
#else
				// return a clean new instance of this type without executing any of its constructors
				object real_object = System.Runtime.Serialization.FormatterServices.GetSafeUninitializedObject(RealType);
				return ClrObject.WrapDynamic(real_object);
#endif
			}
		}

		internal void EnsureThreadStaticFieldsInitialized(ScriptContext context)
		{
            if (initializeStaticFields != null)
                initializeStaticFields(context);
		}

		#endregion
	}

	#endregion

	#region ClrTypeDesc

	/// <summary>
	/// Represents CLR type, generic CLR type template, or CLR type instantiation.
	/// </summary>
	[DebuggerNonUserCode]
	public class ClrTypeDesc : DTypeDesc
	{
		internal static readonly List<ClrTypeDesc>/*!*/ EmptyList = new List<ClrTypeDesc>(1);

		public override bool IsValueType { get { return RealType.IsValueType; } }

		/// <summary>
		/// Instance constructor.
		/// </summary>
		public ClrMethodDesc/*!*/ Constructor
		{
			get
			{
				if (_constructor == null)
				{
					_constructor = ClrMethod.CreateConstructor(this).ClrMethodDesc;
				}
				return _constructor;
			}
		}
		private ClrMethodDesc/*!*/ _constructor;

		public int GenericParameterCount
		{
			get
			{
				return RealType.GetGenericArguments().Length; // TODO:
			}
		}

		/// <summary>
		/// List of all type parameter overloads. Empty for non-generic type.
		/// Otherwise, shared by all overloads and containing the type itself.
		/// Unsorted.
		/// </summary>
		public List<ClrTypeDesc>/*!*/ GenericOverloads { get { return genericOverloads; } }
		private List<ClrTypeDesc>/*!*/ genericOverloads;

		#region Construction

		/// <summary>
		/// Used by run-time reflection.
		/// </summary>
		public ClrTypeDesc(DModule/*!*/ declaringModule, Type/*!*/ realType, DTypeDesc baseDesc, PhpMemberAttributes memberAttributes)
			: base(declaringModule, realType, baseDesc, memberAttributes)
		{
			// initialize with an empty array, the ClrModule reflection takes care:
			genericOverloads = ClrTypeDesc.EmptyList;
		}

		#endregion

		#region Reflection

		internal void AddGenericOverload(ClrTypeDesc/*!*/ desc)
		{
			if (genericOverloads.Count == 0)
			{
				genericOverloads = new List<ClrTypeDesc>(2);
				genericOverloads.Add(this);
				genericOverloads.Add(desc);
			}
			else
			{
				genericOverloads.Add(desc);
			}

			desc.genericOverloads = this.genericOverloads;
		}

		/// <summary>
		/// Reflects the type itself.
		/// Members will be reflected lazily on per member table basis.
		/// </summary>
		protected override DType/*!*/ Reflect()
		{
			// this type-desc represents non-generic type or the generic definition => create the ClrType for it:
			if (!RealType.IsGenericType || RealType.IsGenericTypeDefinition)
				return new ClrType(this, QualifiedName.FromClrNotation(RealType.FullName, true));

			// this type-desc represents a generic type instantiation; 
			// all instantiations of a single generic type share the same ClrType => do not create new ClrType;
			// note: we could do this faster if each type-desc had a reference to the generic type-desc.
			return DTypeDesc.Create(RealType.GetGenericTypeDefinition()).Type;
		}

		/// <summary>
		/// Reflectes generic parameters if the type is generic type definition.
		/// </summary>
		internal override GenericParameterDesc[]/*!!*/ ReflectGenericParameters(DTypeDesc referringType, DRoutineDesc referringRoutine,
			ResolverDelegate resolver)
		{
			if (RealType.IsGenericTypeDefinition)
			{
				Type[] real_params = RealType.GetGenericArguments();
				GenericParameterDesc[] descs = new GenericParameterDesc[real_params.Length];

				// fills the default types going from the last one to the first and filling System.Object until 
				// a constraint disallows this substitution:
				bool defaults_enabled = true;
				for (int i = real_params.Length - 1; i >= 0; i--)
				{
					DTypeDesc default_type = null;

					if (defaults_enabled)
					{
						if (IsSystemObjectCompatibleWithGenericParameter(real_params[i]))
							default_type = DTypeDesc.SystemObjectTypeDesc;
						else
							defaults_enabled = false;
					}

					descs[i] = new GenericParameterDesc(real_params[i], default_type);
				}

				return descs;
			}
			else
			{
				return GenericParameterDesc.EmptyArray;
			}
		}

		protected override void ReflectInterfaces()
		{
			Type[] real_interfaces = RealType.GetInterfaces();

			List<DTypeDesc> iface_list = new List<DTypeDesc>();

			for (int i = 0; i < real_interfaces.Length; i++)
			{
				if (real_interfaces[i].IsPublic)
				{
					iface_list.Add(DTypeDesc.Create(real_interfaces[i]));
				}
			}

			interfaces = iface_list.ToArray();
		}

		protected override void ReflectMethods()
		{
			MethodInfo[] real_methods = RealType.GetMethods(MembersReflectionBindingFlags);

			// We need all methods to add inherited methods with same name to clr overloads
			MethodInfo[] tm = RealType.GetMethods(AllMembersReflectionBindingFlags);

/*#if SILVERLIGHT
			// SILVERLIGHT: TODO .. we need better solution here 
			// (also we should check when exactly is the MethodAccess exception thrown..)
			IEnumerable<MethodInfo> tmf = CollectionUtils.Filter(tm, delegate(MethodInfo m) {
                if (m.GetCustomAttributes(typeof(System.Security.SecurityCriticalAttribute), true).Length != 0) return false;
                
                // TODO: This has to be done better - depending on the caller
                return m.IsPublic;
            });
#else*/
			IEnumerable<MethodInfo> tmf = tm;
//#endif

            IDictionary<string, IList<MethodInfo>> all_methods = CollectionUtils.BuildListDictionary<string, MethodInfo>
                (tmf.Select(_m => _m.Name), tmf);

			// TODO: statistics: how many visible overloads in AVG?
			Dictionary<Name, DRoutineDesc> methods = new Dictionary<Name, DRoutineDesc>(real_methods.Length / 2);

			ReflectMethods(real_methods, all_methods, this, methods);

			this.methods = methods;
		}

		private static void ReflectMethods(MethodInfo[]/*!!*/ realMethods, IDictionary<string, IList<MethodInfo>>/*!!*/ all_methods,
			DTypeDesc/*!*/ declaringType, Dictionary<Name, DRoutineDesc>/*!*/ methods)
		{
			Debug.Assert(realMethods != null && declaringType != null && methods != null && all_methods != null);
			
			// is set to true after first loop when type contains method that may be explicit interface implementation
			bool have_explicit_impl = false;

			foreach (MethodInfo real_method in realMethods)
			{
				// TODO: indexer support
				if (IsMethodVisible(real_method))
				{
					if (!real_method.IsSpecialName || real_method.Name == "get_Item" || real_method.Name == "set_Item")
					{
						// method names are not mangled in any way in CLR:
						ReflectMethod(real_method, new Name(real_method.Name), Enums.GetMemberAttributes(real_method),
							declaringType, methods, all_methods[real_method.Name]);
					}
				}
				else if (!have_explicit_impl && DoesMethodLookLikeExplicitImpl(real_method)) have_explicit_impl = true;
			}

			if (have_explicit_impl)
			{
				// determine explicit interface method implementations according to the interface map
				foreach (Type iface_type in declaringType.RealType.GetInterfaces())
				{
					InterfaceMapping mapping = declaringType.RealType.GetInterfaceMap(iface_type);

					for (int i = 0; i < mapping.TargetMethods.Length; i++)
					{
						MethodInfo real_method = mapping.TargetMethods[i];
						if (!IsMethodVisible(real_method))
						{
							MethodInfo iface_method = mapping.InterfaceMethods[i];

							PhpMemberAttributes attrs = Enums.GetMemberAttributes(real_method) | PhpMemberAttributes.Final;

                            // reflect the method twice - under the iface method name and under the
                            // compound iface.method name
                            ReflectMethod(real_method, new Name(iface_method.Name), attrs, declaringType, methods, null);
                            ReflectMethod(real_method, new Name(iface_type.Name + "." + iface_method.Name), attrs, declaringType, methods, null);
						}
					}
				}
			}
		}


		/// <summary>
		/// Add method and its overloads to the table
		/// </summary>
		/// <param name="realMethod">First found methodinfo</param>
		/// <param name="name">Name of the method</param>
		/// <param name="attributes">Attributes</param>
		/// <param name="declaringType">Owner type</param>
		/// <param name="methods">Collection with already added methods</param>
		/// <param name="overloads">All visible methods with the same name (including inherited)</param>
		private static void ReflectMethod(MethodInfo/*!*/ realMethod, Name name, PhpMemberAttributes attributes,
			DTypeDesc/*!*/ declaringType, Dictionary<Name, DRoutineDesc>/*!*/ methods, IList<MethodInfo> overloads)
		{
			ClrMethod clr_method;
			
			// overloads are added when the first method with same name is found
			if (methods.ContainsKey(name)) return;
			
			// new method:
			clr_method = new ClrMethod(name, declaringType, attributes, 1, realMethod.ContainsGenericParameters);
			methods.Add(name, clr_method.ClrMethodDesc);
			if (overloads == null)
			{
				ClrMethod.Overload overload;
				clr_method.AddOverload(realMethod, out overload);
			}
			else
			{
				foreach (MethodInfo ovrInfo in overloads)
				{
#if SILVERLIGHT
                    // Maybe we should include this check in non-Silverlight version too - 
                    //   ensures that overload is also visible
                    if (!IsMethodVisible(ovrInfo)) continue;
#endif
					ClrMethod.Overload overload;
					clr_method.AddOverload(ovrInfo, out overload);
				}
			}
		}

		protected override void ReflectProperties()
		{
			ReflectPropertiesAndConstants();
		}

		protected override void ReflectConstants()
		{
			ReflectPropertiesAndConstants();
		}

		private void ReflectPropertiesAndConstants()
		{
			PropertyInfo[] real_properties = RealType.GetProperties(MembersReflectionBindingFlags);
			EventInfo[] real_events = RealType.GetEvents(MembersReflectionBindingFlags);
			FieldInfo[] real_fields = RealType.GetFields(MembersReflectionBindingFlags);

			int est_property_count, est_constant_count;

			if (RealType.IsEnum)
			{
				est_property_count = 0;
				est_constant_count = real_fields.Length - 1; // one __value field
			}
			else
			{
				// there is usually no many visible constants defined in non-enum types, 
				// majority of properties are visible, majority of fields are invisible:
				est_property_count = real_properties.Length + 2;
				est_constant_count = 1;
			}

			this.properties = new Dictionary<VariableName, DPropertyDesc>(est_property_count);
			this.constants = new Dictionary<VariableName, DConstantDesc>(est_constant_count);

			// Reflect properties
			// is set to true after first loop when type contains method that may be explicit interface implementation
			bool have_explicit_impl = false;

			// associates setter/getter with property for explicit interface impl.
			Dictionary<MethodInfo, PropertyInfo> explicit_properties = new Dictionary<MethodInfo, PropertyInfo>();

			foreach (PropertyInfo real_prop in real_properties)
			{
				if (!real_prop.IsSpecialName)
				{
					MethodInfo getter = real_prop.GetGetMethod(true);
					MethodInfo setter = real_prop.GetSetMethod(true);

					// Explicit interface implementation?
					if (getter != null && DoesMethodLookLikeExplicitImpl(getter))
					{
						explicit_properties.Add(getter, real_prop);
						have_explicit_impl |= true;
					}
					if (setter != null && DoesMethodLookLikeExplicitImpl(setter)) 
					{
						explicit_properties.Add(setter, real_prop);
						have_explicit_impl |= true;
					}
					
					// Add property
					bool has_visible_getter = getter != null && IsMethodVisible(getter);
					bool has_visible_setter = setter != null && IsMethodVisible(setter);

					if (has_visible_getter || has_visible_setter)
					{
						ParameterInfo[] index_params = real_prop.GetIndexParameters();

						if (index_params.Length == 0)
						{
							VariableName name = new VariableName(real_prop.Name);

							Debug.Assert(!properties.ContainsKey(name));

							// TODO: how to combine attrs?
							PhpMemberAttributes attrs = PhpMemberAttributes.None;
							if (has_visible_getter) attrs |= Enums.GetMemberAttributes(getter);
                            if (has_visible_setter) attrs |= Enums.GetMemberAttributes(setter);

							ReflectProperty(name, attrs, real_prop, has_visible_getter, has_visible_setter);
						}
						else
						{
							// TODO: indexers currently not supported => skip
						}
					}
				}
			}

			// Check explicitly implemented interfaces
			if (have_explicit_impl)
			{
				// determine explicit interface method implementations according to the interface map
				foreach (Type iface_type in RealType.GetInterfaces())
				{
					InterfaceMapping mapping = RealType.GetInterfaceMap(iface_type);
					IDictionary<MethodInfo, MethodInfo> mappingDict = CollectionUtils.JoinDictionary(mapping.InterfaceMethods, mapping.TargetMethods);

					foreach (PropertyInfo real_property in iface_type.GetProperties())
					{
						MethodInfo getter = real_property.GetGetMethod(false);
						MethodInfo setter = real_property.GetSetMethod(false);
						if ((getter != null && !IsMethodVisible(mappingDict[getter])) ||
							(setter != null && !IsMethodVisible(mappingDict[setter])))
						{
                            ParameterInfo[] index_params = real_property.GetIndexParameters();

                            if (index_params.Length == 0)
                            {
                                // TODO: how to combine attrs?
                                PhpMemberAttributes attrs = PhpMemberAttributes.None;
                                if (getter != null) attrs = Enums.GetMemberAttributes(getter);
                                if (setter != null) attrs = Enums.GetMemberAttributes(setter);

                                bool get_vis = getter != null && IsMethodVisible(getter);
                                bool set_vis = setter != null && IsMethodVisible(setter);

                                // add property under the iface property name and under the compound iface.property name
                                ReflectProperty(new VariableName(real_property.Name), attrs, real_property, get_vis, set_vis);
                                ReflectProperty(new VariableName(iface_type.Name + "." + real_property.Name), attrs, real_property, get_vis, set_vis);
                            }
                            else
                            {
                                // TODO: indexers currently not supported => TODO, problems ?
                            }
						}
					}
				}
			}


			// Reflect Events
			foreach (EventInfo real_event in real_events)
			{
				if (!real_event.IsSpecialName)
				{
					MethodInfo adder = real_event.GetAddMethod(true);
					MethodInfo remover = real_event.GetRemoveMethod(true);

					bool has_visible_adder = adder != null && IsMethodVisible(adder);
					bool has_visible_remover = remover != null && IsMethodVisible(remover);

					if (has_visible_adder || has_visible_remover)
					{
						VariableName name = new VariableName(real_event.Name);

						Debug.Assert(!properties.ContainsKey(name));

						PhpMemberAttributes attrs = PhpMemberAttributes.None;

						if (adder != null) attrs = Enums.GetMemberAttributes(adder);
						if (remover != null) attrs = Enums.GetMemberAttributes(remover);

						// TODO: how to combine attrs?

						ClrEvent clr_event = new ClrEvent(name, this, attrs, real_event, has_visible_adder, has_visible_remover);
						properties.Add(name, clr_event.PropertyDesc);
					}
				}
			}

			foreach (FieldInfo real_field in real_fields)
			{
				if (IsFieldVisible(real_field) && !real_field.IsSpecialName)
				{
					VariableName name = new VariableName(real_field.Name);
					PhpMemberAttributes attrs = Enums.GetMemberAttributes(real_field);

					if (real_field.IsLiteral)
					{
						// constant //

						Debug.Assert(real_field.IsStatic && !constants.ContainsKey(name));

						object value = real_field.GetValue(null);
						ClassConstant class_const = new ClassConstant(name, this, attrs);
						class_const.SetValue(Convert.ClrLiteralToPhpLiteral(value));
						constants.Add(name, class_const.ConstantDesc);
					}
					else
					{
						// field backed property (from the PHP point of view) //

						Debug.Assert(!properties.ContainsKey(name));

						ClrField clr_field = new ClrField(name, this, attrs, real_field);
						properties.Add(name, clr_field.PropertyDesc);
					}
				}
			}
		}

		private void ReflectProperty(VariableName vname, PhpMemberAttributes attrs, PropertyInfo real_property, bool get_vis, bool set_vis)
		{
			if (properties.ContainsKey(vname)) return;

			ClrProperty clr_property = new ClrProperty(vname, this, attrs, real_property, get_vis, set_vis);
			properties.Add(vname, clr_property.PropertyDesc);
		}

		internal static bool IsMethodVisible(MethodBase/*!*/ method)
		{
#if SILVERLIGHT
            //if (method.GetCustomAttributes(typeof(System.Security.SecurityCriticalAttribute), true).Length != 0) return false;
            return (method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly);
#else
			return (method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly);
#endif
		}

		internal static bool DoesMethodLookLikeExplicitImpl(MethodBase/*!*/ method)
		{
			return (method.IsFinal && method.IsVirtual && method.IsPrivate);
		}

		internal static bool IsFieldVisible(FieldInfo/*!*/ field)
		{
			return (field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly);
		}

		#endregion

		#region Run-time Operations

		public override object New(PhpStack/*!*/ stack, DTypeDesc caller, NamingContext nameContext)
		{
			// visibility test performed by the stub
			return ClrObject.WrapDynamic(Constructor.Invoke(null, stack, caller));
		}

		public override object New(ScriptContext/*!*/ context)
		{
#if SILVERLIGHT
				// SILVERLIGHT: TODO...?
				throw new NotSupportedException("PhpTypeDesc.New - creating uninitalized object is not supported");
#else
			// return a clean new instance of this type without executing any of its constructors
			object real_object = System.Runtime.Serialization.FormatterServices.GetSafeUninitializedObject(RealType);
			return ClrObject.WrapDynamic(real_object);
#endif
		}

		internal ClrTypeDesc/*!*/ ResolveGenericOverload(int argumentCount, out bool success)
		{
			List<ClrTypeDesc> overloads = genericOverloads;

			if (overloads.Count > 0)
			{
				Debug.Assert(overloads.Count > 1);
				overloads.Sort(delegate(ClrTypeDesc x, ClrTypeDesc y)
				{
					int lx = x.GenericParameterCount;
					int ly = y.GenericParameterCount;

					return (lx == ly) ? 0 : ((lx < ly) ? -1 : +1);
				});

				int overload_idx = 0;
				while (overload_idx < overloads.Count && argumentCount > overloads[overload_idx].GenericParameterCount)
					overload_idx++;

				if (overload_idx == overloads.Count)
				{
					// too many args:
					success = false;
					return overloads[overload_idx - 1];
				}

				// less or equal:
				success = true;
				return overloads[overload_idx];
			}
			else
			{
				success = (argumentCount <= this.GenericParameterCount);
				return this;
			}
		}

		/// <summary>
		/// Argless stub used as a fake CLR constructor if there is no such constructor.
		/// </summary>
		internal object _NoConstructorErrorStub(object instance, PhpStack/*!*/ stack)
		{
			stack.RemoveFrame();
			PhpException.Throw(PhpError.Error, CoreResources.GetString("constructor_less_type_instantiated", this.MakeFullName()));
			return null;
		}

		#endregion
	}

	#endregion

	#region ClrDelegateDesc

	[DebuggerNonUserCode]
	public sealed class ClrDelegateDesc : ClrTypeDesc
	{
		internal class DelegateStubBuilder
		{
			#region Fields

			/// <summary>
			/// Per delegate type stub cache.
			/// </summary>
			private Dictionary<DRoutineDesc, DynamicMethod> stubCache = new Dictionary<DRoutineDesc, DynamicMethod>();

			private ParameterInfo[]/*!*/ delegateParameters;
			private Type/*!*/ stubReturnType;
			private Type[]/*!*/ stubParamTypes;

			private ClrDelegateDesc/*!*/ delegateDesc;

			#endregion

			#region Construction

			public DelegateStubBuilder(ClrDelegateDesc/*!*/ delegateDesc)
			{
				// our generated stubs must have the same signature as the delegate (i.e. its Invoke method)
				// enriched with the first DObject parameter representing the target instance (to be closed)

				this.delegateDesc = delegateDesc;

				DRoutineDesc invoke_desc = delegateDesc.GetMethod(Name.ClrInvokeName);

				if (invoke_desc != null)
				{
					MethodInfo invoke_info = (MethodInfo)invoke_desc.ClrMethod.Overloads[0].Method;
					delegateParameters = invoke_desc.ClrMethod.Overloads[0].Parameters;

					// determine parameter types and return type
					stubReturnType = invoke_info.ReturnType;
					stubParamTypes = new Type[delegateParameters.Length + 1];

					stubParamTypes[0] = Types.DObject[0];
					for (int i = 0; i < delegateParameters.Length; i++) stubParamTypes[i + 1] = delegateParameters[i].ParameterType;
				}
				else
				{
					Debug.Fail("Creating non-specific delegates is not supported yet.");
				}
			}

			#endregion

			#region GetStub & Friends

			/// <summary>
			/// Returns delegate to CLR stub of the given target-routine pair.
			/// </summary>
			/// <param name="target">The target instance or <B>null</B>.</param>
			/// <param name="routine">The target routine desc.</param>
			/// <param name="realCalleeName">Real callee name if <paramref name="routine"/> is in fact <c>__call</c>,
			/// or <B>null</B> if <paramref name="routine"/> if the real callee.</param>
			/// <returns>
			/// Delegate to the stub or <B>null</B> if stub for this target-routine pair cannot be generated.
			/// </returns>
			public Delegate GetStub(DObject target, DRoutineDesc/*!*/ routine, string realCalleeName)
			{
				if (target == null && !routine.IsStatic) return null;

				DynamicMethod stub;

				// storing stubs (not delegates) in the cache allows us to reuse the same stub for different targets

				lock (stubCache)
				{
					if (!stubCache.TryGetValue(routine, out stub))
					{
						stub = GenerateStub(routine, realCalleeName);
						stubCache.Add(routine, stub);
					}
				}

				return stub.CreateDelegate(delegateDesc.RealType, target);
			}

			private IPlace/*!*/ GetScriptContextPlace(ILEmitter/*!*/ il, bool haveTarget)
			{
				LocalBuilder context = il.DeclareLocal(Types.ScriptContext[0]);

				// [ context = ScriptContent.CurrentContext ]

				il.Emit(OpCodes.Call, Emit.Methods.ScriptContext.GetCurrentContext);
				il.Stloc(context);

				return new Place(context);
			}

			private DynamicMethod/*!*/ GenerateStub(DRoutineDesc/*!*/ routine, string realCalleeName)
			{
#if DEBUG_DELEGATE_STUBS
				AssemblyName name = new AssemblyName("DelegateStub_" + routine.MakeFullName());
				AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Save, "C:\\Temp");
				ModuleBuilder mb = ab.DefineDynamicModule(name.Name, name.Name + ".dll");
				TypeBuilder tb = mb.DefineType(routine.MakeFullName());
				MethodBuilder mmb = tb.DefineMethod("<^DelegateStub>", MethodAttributes.Public | MethodAttributes.Static,
					stubReturnType, stubParamTypes);

				EmitStubBody(new ILEmitter(mmb), routine, realCalleeName);

				tb.CreateType();
				ab.Save(name.Name + ".dll");
#endif

#if SILVERLIGHT
				DynamicMethod dm = new DynamicMethod("<^DelegateStub>", stubReturnType, stubParamTypes);
#else
				DynamicMethod dm = new DynamicMethod("<^DelegateStub>", PhpFunctionUtils.DynamicStubAttributes, CallingConventions.Standard,
					stubReturnType, stubParamTypes, routine.ArglessStub.Method.DeclaringType, true);
#endif
				EmitStubBody(new ILEmitter(dm), routine, realCalleeName);

				return dm;
			}

			private void EmitStubBody(ILEmitter/*!*/ il, DRoutineDesc/*!*/ routine, string realCalleeName)
			{
				IPlace context_place = GetScriptContextPlace(il, !routine.IsStatic);
				ClrStubBuilder stub_builder = new ClrStubBuilder(il, context_place, delegateParameters.Length, 1);

				if (realCalleeName != null)
				{
					PhpStackBuilder.EmitAddFrame(il, context_place, 0, 2, null, delegate(ILEmitter _il, int arg_idx)
					{
						if (arg_idx == 0)
						{
							// push real callee name
							il.Emit(OpCodes.Ldstr, realCalleeName);
						}
						else
						{
							// create PHP array containing all converted CLR parameters
							il.LdcI4(delegateParameters.Length);
							il.LdcI4(0);
							il.Emit(OpCodes.Newobj, Constructors.PhpArray.Int32_Int32);

							// load CLR parameters, convert them to PHP, and add them to the PHP array
							for (int i = 0; i < delegateParameters.Length; i++)
							{
								il.Emit(OpCodes.Dup);
								stub_builder.EmitLoadClrParameter(delegateParameters[i], PhpTypeCode.Unknown);

								il.Emit(OpCodes.Call, Emit.Methods.PhpHashtable_Add);
								il.Emit(OpCodes.Pop);
							}
						}
					});
				}
				else
				{
					PhpStackBuilder.EmitAddFrame(il, context_place, 0, delegateParameters.Length, null, delegate(ILEmitter _il, int i)
					{
						stub_builder.EmitLoadClrParameter(delegateParameters[i], PhpTypeCode.Unknown);
					});
				}

				Label target_is_null = il.DefineLabel();

				// invoke the routine
				il.Ldarg(0);
				il.Ldarg(0);
				il.Emit(OpCodes.Brfalse_S, target_is_null);
				il.Emit(OpCodes.Callvirt, Emit.Properties.DObject_RealObject.GetGetMethod());
				il.MarkLabel(target_is_null, true);

				context_place.EmitLoad(il);
				il.Emit(OpCodes.Ldfld, Fields.ScriptContext_Stack);

				// beware the "reflection" here, IMHO better than going via KnownRoutine.ArglessInfo
				il.Emit(OpCodes.Call, routine.ArglessStub.Method);

				// convert ref/out parameters back from PHP to CLR
				for (int i = 0; i < delegateParameters.Length; i++)
				{
					stub_builder.EmitStoreClrParameter(delegateParameters[i]);
				}

				// convert return value from PHP to CLR
				stub_builder.EmitConvertReturnValue(stubReturnType, PhpTypeCode.Object);

				il.Emit(OpCodes.Ret);
			}

			#endregion
		}

		/// <summary>
		/// Lazily initialized stub builder.
		/// </summary>
		private volatile DelegateStubBuilder _stubBuilder;

		internal DelegateStubBuilder StubBuilder
		{
			get
			{
				if (_stubBuilder == null)
				{
					lock (this)
					{
						if (_stubBuilder == null)
						{
							_stubBuilder = new DelegateStubBuilder(this);
						}
					}
				}
				return _stubBuilder;
			}
		}

		#region Construction

		/// <summary>
		/// Used by run-time reflection.
		/// </summary>
		public ClrDelegateDesc(DModule/*!*/ declaringModule, Type/*!*/ realType, DTypeDesc baseDesc, PhpMemberAttributes memberAttributes)
			: base(declaringModule, realType, baseDesc, memberAttributes)
		{
		}

		#endregion

		public override object New(PhpStack/*!*/ stack, DTypeDesc caller, NamingContext nameContext)
		{
			// try to convert the value on the stack to a delegate of the type represented by this instance

			if (stack.ArgCount == 1)
			{
				// TODO: allow other syntax as well?
				// e.g.: new SomeDelegate($instance, "MethodName")

				object arg = stack.PeekValueUnchecked(1);
				stack.RemoveFrame();

				PhpCallback callback = Convert.ObjectToCallback(arg, false);
				if (callback != null && callback.Bind(false, caller, nameContext))
				{
					if (callback.TargetInstance == null && !callback.TargetRoutine.IsStatic)
					{
						PhpException.Throw(PhpError.Error, CoreResources.GetString("delegate_target_instance_missing",
							MakeFullName(),
							callback.TargetRoutine.DeclaringType.MakeFullName(),
							callback.TargetRoutine.MakeFullName()));
						return null;
					}

					Delegate result = StubBuilder.GetStub(
						callback.TargetInstance,
						callback.TargetRoutine,
						callback.IsBoundToCaller ? callback.RoutineName : null);

					// we have a delegate
					return ClrObject.Wrap(result);
				}
				return null;
			}
			else
			{
				stack.RemoveFrame();

				PhpException.Throw(PhpError.Error, CoreResources.GetString("delegate_unrecognized_ctor_args",
					MakeFullName()));
				return null;
			}
		}
	}

	#endregion
}
