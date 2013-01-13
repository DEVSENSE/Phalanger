/*

 Copyright (c) 2006 Tomas Matousek and Ladislav Prosek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

//#define DEBUG_DYNAMIC_CTOR_STUBS

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

using PHP.Core;
using PHP.Core.AST;
using PHP.Core.Emit;
using PHP.Core.Parsers;
using System.IO;
using System.Reflection.Emit;
using System.Diagnostics;

namespace PHP.Core.Reflection
{
	#region VersionInfo

	public struct VersionInfo
	{
		public const int Unconditional = 0;
		public const int ReflectedRuntimeActivated = -1;

		/// <summary>
		/// Versions are numbered starting from zero. Unconditionally declared entities has a single version #0.
		/// Conditionally declared entities have index > 0.
		/// </summary>
		public int Index { get { return index; } internal /* PhpType, PhpFunction reflection */ set { index = value; } }
		private int index;

		public IDeclaree Next { get { return next; } }
		private IDeclaree next;

		public VersionInfo(int index, IDeclaree next)
		{
			this.index = index;
			this.next = next;
		}
	}

	#endregion

	#region DType

	/// <summary>
	/// Represents a type. For generic types, this instance represents the generic template
	/// all instantiations (represented by DTypeDesc) are sharing.
	/// </summary>
	[DebuggerDisplay("{MakeFullGenericName()}")]
	public abstract class DType : DMember, IEquatable<DType>
	{
		#region Properties

		public static readonly DType[]/*!*/ EmptyArray = new DType[0];

		/// <summary>
		/// Whether all base types are definite. Note that the type itself needn't to be definite nor identity definite.
		/// </summary>
		public abstract bool IsComplete { get; }

		public abstract bool IsOpen { get; }
		public bool IsClosed { get { return !IsOpen; } }

		public virtual bool IsGeneric { get { return false; } }

		public abstract bool ClrVerified { get; }

		/// <summary>
		/// Whether the type can be definitely emitted in the resulting compulation unit, i.e.
		/// whether it is a completely declared type or it is a reflected type.
		/// 
		/// Note that a reflected type is always complete.
		/// </summary>
		public override bool IsDefinite { get { return IsIdentityDefinite && IsComplete; } }

		/// <summary>
		/// <B>true</B> if the type is PHP type or a constructed type of PHP generic type.
		/// </summary>
		public virtual bool IsPhpType { get { return false; } }

		/// <summary>
		/// <B>true</B> if the type is CLR type or a constructed type of CLR type.
		/// </summary>
		public virtual bool IsClrType { get { return false; } }

		/// <summary>
		/// <B>true</B> if the type is generic parameter or a constructed type of generic parameter.
		/// </summary>
		public virtual bool IsGenericParameter { get { return false; } }

		public DTypeDesc TypeDesc { get { return (DTypeDesc)memberDesc; } }

		public DType Base { get { return (TypeDesc.Base != null) ? TypeDesc.Base.Type : null; } }
		public TypeBuilder RealTypeBuilder { get { return (TypeBuilder)RealType; } }
		public Type RealType
		{
			get
			{
				Debug.Assert(TypeDesc.RealType == null || !TypeDesc.RealType.IsGenericType || TypeDesc.RealType.IsGenericTypeDefinition
				  || TypeDesc is ConstructedTypeDesc, "The DType (except for ConstructedType) shouldn't represent generic type instantiations");
				return TypeDesc.RealType;
			}
		}

		public bool IsInterface { get { return TypeDesc.IsInterface; } }
		public bool IsValueType { get { return TypeDesc.IsValueType; } }

		/// <summary>
		/// Abstract methods and properties (declared or inherited) not implemented by the type.
		/// Computed lazily during full analysis.
		/// </summary>
		internal DMemberRef[] AllAbstracts { get { return allAbstracts; } }
		internal /* protected */ DMemberRef[] allAbstracts = null;

		/// <summary>
		/// Gets constructor of this type (if any).
		/// </summary>
		public virtual KnownRoutine Constructor { get { return null; } }

		/// <summary>
		/// Determines whether this type exposes a public parameterless constructor.
		/// </summary>
		public bool HasDefaultConstructor
		{
			get
			{
				KnownRoutine ctor = GetConstructor();
				if (ctor == null) return true;

				// PHP constructor
				PhpRoutine php_routine = ctor as PhpRoutine;
				if (php_routine != null)
				{
					return (php_routine.IsPublic && php_routine.Signature.MandatoryParamCount == 0);
				}
				else
				{
					// CLR constructor
					ClrMethod clr_method = (ClrMethod)ctor;
					return (clr_method.HasParameterlessOverload && clr_method.Overloads[0].Method.IsPublic);
				}
			}
		}

		/// <summary>
		/// <B>True</B>, if the type defines a custom attribute class.
		/// </summary>
		public bool IsCustomAttributeType
		{
			get { return TypeDesc.IsSubclassOf(DTypeDesc.AttributeTypeDesc) && !IsGeneric && !(this is ConstructedType); }
		}

		#endregion

		#region Construction

		/// <summary>
		/// Known types.
		/// </summary>
		protected DType(DTypeDesc/*!*/ typeDesc)
			: base(typeDesc)
		{
			Debug.Assert(typeDesc != null);
		}

		/// <summary>
		/// For unknown types and generic type parameters.
		/// </summary>
		protected DType(DTypeDesc typeDesc, string/*!*/ fullName)
			: base(typeDesc, fullName)
		{
			Debug.Assert(fullName != null);
		}

		#endregion

		#region Member Lookup

		public virtual T GetDeclaredMethod<T>(Name methodName) where T : KnownRoutine
		{
			Debug.Assert(!IsUnknown);

			DRoutineDesc desc;
			if (TypeDesc.Methods.TryGetValue(methodName, out desc))
				return (T)desc.Member;

			return null;
		}

		public virtual T GetDeclaredProperty<T>(VariableName propertyName) where T : KnownProperty
		{
			Debug.Assert(!IsUnknown);

			DPropertyDesc desc;
			if (TypeDesc.Properties.TryGetValue(propertyName, out desc))
				return (T)desc.Member;

			return null;
		}

		public GetMemberResult GetMethod(Name methodName, PhpType context, out DRoutine routine)
		{
			DRoutineDesc desc;
			GetMemberResult result;

			result = TypeDesc.GetMethod(methodName, (context == null ? null : context.TypeDesc), out desc);

			if (desc == null) routine = null;
			else
			{
				routine = desc.Routine;
				if (routine == null && result == GetMemberResult.OK) result = GetMemberResult.NotFound;
			}
			return result;
		}

		public GetMemberResult GetProperty(VariableName propertyName, PhpType context, out DProperty property)
		{
			DPropertyDesc desc;
			GetMemberResult result;

			result = TypeDesc.GetProperty(propertyName, (context == null ? null : context.TypeDesc), out desc);

			if (desc == null) property = null;
			else
			{
				property = desc.Property;
				if (property == null && result == GetMemberResult.OK) result = GetMemberResult.NotFound;
			}
			return result;
		}

		public GetMemberResult GetConstant(VariableName constantName, PhpType context, out ClassConstant constant)
		{
			DConstantDesc desc;
			GetMemberResult result;

			result = TypeDesc.GetConstant(constantName, (context == null ? null : context.TypeDesc), out desc);

			if (desc == null) constant = null;
			else
			{
				constant = desc.ClassConstant;
				if (constant == null && result == GetMemberResult.OK) result = GetMemberResult.NotFound;
			}
			return result;
		}

		/// <summary>
		/// Returns a <see cref="DRoutine"/> representing the constructor effective for this class
		/// or <B>null</B> if there is no constructor.
		/// </summary>
		/// <returns>The constructor or <B>null</B>.</returns>
		public abstract KnownRoutine GetConstructor();

		public GetMemberResult GetConstructor(PhpType context, out KnownRoutine constructor)
		{
			constructor = GetConstructor();
			if (constructor == null) return GetMemberResult.NotFound;

			// check visibility
			switch (constructor.Visibility)
			{
				case PhpMemberAttributes.Private:
					{
						if (context == null || !constructor.DeclaringType.Equals(context))
							return GetMemberResult.BadVisibility;

						break;
					}

				case PhpMemberAttributes.Protected:
					{
						if (context == null || !constructor.DeclaringType.IsRelatedTo(context))
							return GetMemberResult.BadVisibility;

						break;
					}

				case PhpMemberAttributes.Public:
					{
						break;
					}
			}

			return GetMemberResult.OK;
		}

		#endregion

		#region Member Enumeration

		public IEnumerable<T>/*!*/ GetMethods<T>()
			where T : DRoutine
		{
			foreach (DRoutineDesc desc in TypeDesc.Methods.Values)
				yield return (T)desc.Routine;
		}

		public IEnumerable<T>/*!*/ GetProperties<T>()
			where T : DProperty
		{
			foreach (DPropertyDesc desc in TypeDesc.Properties.Values)
				yield return (T)desc.Property;
		}

		#endregion

		#region Analysis: MakeConstructedType, AnalyzeInheritance, ResolveAbstractOverrides

		internal virtual DType/*!*/ MakeConstructedType(Analyzer/*!*/ analyzer, DTypeDesc[]/*!*/ arguments, Position position)
		{
			if (arguments.Length > 0)
			{
				Debug.Assert(!(this is PrimitiveType) && !(this is ConstructedType) && !(this is GlobalType), "Prevented by grammar");

				// constructed type with unknown generic type:
				return analyzer.CreateConstructedType(this.TypeDesc, arguments, arguments.Length);
			}
			else
			{
				return this;
			}
		}

		/// <summary>
		/// Analyzes inheritance properties of the PHP types and constructed types being declared in the current compilation.
		/// The properties being analyzed primarily include the definiteness of the type.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The constructed types themselves can't get the analysis into a loop as they form a DAG structure
		/// (grammar defines a tree structure and unifying the same constructed types results in the DAG).
		/// However, the completeness of a constructed type depends on the completeness of the components -
		/// the generic type and all the type arguments. Since a polymorphic recursion can take place (e.g. B{T} extends A{B{T}}),
		/// we need to employ minimal fixpoint algorithm to determine the type completeness.
		/// </para>
		/// </remarks>	
		internal void AnalyzeInheritance(ErrorSink/*!*/ errors)
		{
			Debug.Assert(errors != null);

			List<DType> worklist = null;

			this.AnalyzeInheritance(errors, ref worklist, null, null, 0, 0);

			while (worklist != null && worklist.Count > 0)
			{
				DType type = worklist[worklist.Count - 1];
				worklist.RemoveAt(worklist.Count - 1);
				type.AnalyzeInheritance(errors, ref worklist, null, null, -1, -1);
			}
		}

		/// <remarks>
		/// <para>
		/// If <paramref name="dfsDepth"/> is non-negative the method performs DFS on the graph of PHP types and constructed types, 
		/// going to the depth recursively adding the type being analyzed to the <paramref name="mfpWorklist"/> if it closes a loop.
		/// It also adds the <paramref name="dfsPredecessor"/> into its list of DFS predecessors (if non-null).
		/// The <paramref name="phpPredecessor"/> tracks the last DFS predecessor that was PhpType (not constructed).
		/// </para>
		/// 
		/// <para>
		/// Terminology: 
		///   - "inheritance edge" is an edge going from a type to its immediate base type (class or interface)
		///   - "constructed edge" is an edge going from a constructed type to a generic argument
		///   - "generic edge" is an edge going from a constructed type to the generic type
		/// </para>
		/// 
		/// <para>
		/// During the DFS, <paramref name="dfsDepth"/> records the current depth in the DFS tree.
		/// <paramref name="inheritanceDepth"/> records the length of a path from the current type to the first node of a constructed edge
		/// (the path therefore comprises of inheritance and generic edges). 
		/// 
		/// The DFS depth is stored on PhpTypes and is used to discover circular inheriting among inherited types. 
		/// When a backward edge is discovered and the target type (which must be either 
		/// a PhpType or a ConstructedType, considering the generic type in the latter case) the DFS depth difference between 
		/// the target type and the current type is calculated. If it is less than the current inheritance depth then there 
		/// is a loop comprising of inheritance edges only (which is an error).
		/// 
		/// NOTE: It suffice to store the DFS depth only on PhpTypes (in <see cref="PhpTypeBuilder.InheritanceAnalysisDfsDepth"/>).
		/// Depth comparison in constructed type compares the inheritance depth of the generic type with the difference of the 
		/// DFS depths minus one -- as if the backward edge pointed directly to the generic type.
		/// </para>
		/// 
		/// <para>
		/// If <paramref name="dfsDepth"/> is -1 the method only updates the properties whose procesing requires MFP and 
		/// adds the types listed in the DFS predecessors list to the worklist if any of these properties changed.
		/// </para>
		/// 
		/// <para>
		/// The lists of DFS predecessors are cleaned up later during the DType clean-up (TODO: clean the builders as well).
		/// </para>
		/// </remarks>
		internal virtual void AnalyzeInheritance(ErrorSink/*!*/ errors, ref List<DType> mfpWorklist, DType dfsPredecessor, PhpType phpPredecessor,
			int dfsDepth, int inheritanceDepth)
		{
			// overridden by PhpType and ConstructedType
		}

		internal virtual void GetInheritanceProperties(out bool extendsClrType, out DType/*!*/ hierarchyRoot)
		{
			if (Base != null)
			{
				Base.GetInheritanceProperties(out extendsClrType, out hierarchyRoot);
			}
			else
			{
				extendsClrType = this.IsClrType;
				hierarchyRoot = this;
			}
		}

		/// <summary>
		/// For each method M, finds out the method that is overridden by the method M as well as all interface methods
		/// that are implemented by the method M.
		/// </summary>
		/// <remarks>
		/// Must be called after member-analysis as all types and their members have to be known.
		/// Assumes that the inheritance hierarchy is free of circular dependencies.
		/// </remarks>
        /// <param name="errors">Error sink, can be null is you don't care.</param>
        internal DMemberRef[]/*!!*/ ResolveAbstractOverrides(ErrorSink errors)
		{
			// already resolved:
			if (allAbstracts != null)
				return allAbstracts;

			if (!IsDefinite || ClrVerified && !IsAbstract)
				return allAbstracts = DMemberRef.EmptyArray;

			// all inherited abstract methods are surely overridden (either by a abstract or non-abstract method):
			DMemberRef[] inherited_abstracts;

			// resolve overrides on the base class (if exists and is definite):
			if (Base != null)
				inherited_abstracts = Base.ResolveAbstractOverrides(null);
			else
				inherited_abstracts = DMemberRef.EmptyArray;

			// in the following, the array of inherited abstract members is copied, 
			// members implemented by this type nulled in the copy,
			// and unimplemented members from interfaces and declared abstract methods are added; 
			// if there remain some nulls, the array is trimmed:

			DMemberRef[] abstracts;

			if (inherited_abstracts.Length > 0)
				abstracts = (DMemberRef[])inherited_abstracts.Clone();
			else
				abstracts = DMemberRef.EmptyArray;

			// filter inherited members implemented in this type:
			int nulled = abstracts.Length;
			int remains = abstracts.Length;
			for (int i = 0; i < inherited_abstracts.Length; i++)
			{
				bool in_supertype = false;
				DMemberRef implementation = inherited_abstracts[i].Member.GetImplementationInSuperTypes(this, false, ref in_supertype);

				if (implementation != null)
				{
					implementation.Member.AddAbstractOverride(inherited_abstracts[i]);

					if (nulled == abstracts.Length) nulled = i;
					abstracts[i] = null;
					remains--;

                    // check for implementation compatibility
                    // implementation.Member must be able to override inherited_abstracts[i].Member
                    if (errors != null && this is PhpType && inherited_abstracts[i].Member is DRoutine)
                    {
                        // implementation
                        DRoutine droutine = implementation.Member as DRoutine;
                        Debug.Assert(droutine != null);

                        RoutineSignature rsignature = droutine.GetSignature(0);
                        
                        // Base
                        DRoutine declaringRoutine = (DRoutine)inherited_abstracts[i].Member;
                        RoutineSignature declaringSignature = declaringRoutine.GetSignature(0);

                        // check if it's a valid override
                        if (!rsignature.CanOverride(declaringSignature))
                            droutine.ReportMethodNotCompatible(errors, Base, (PhpType)this);
                    }
				}
			}

			// nulled is the first nulled item of the inherited_abstracts array or its Length //

			// TODO: move interface scanning to a virtual method

			ConstructedType cted = this as ConstructedType;
			DType type = (cted != null) ? cted.GenericType.Type : this;

			// add unimplemented interface methods:
			PhpType php_type = type as PhpType;
			if (php_type != null)
			{
				IEnumerable<DTypeDesc> interfaces = (php_type.Builder != null) ? php_type.Builder.BaseInterfaces : (IEnumerable<DTypeDesc>)TypeDesc.Interfaces;
				foreach (DTypeDesc iface_desc in interfaces)
				{
					if (iface_desc.Type.IsDefinite)
					{
						DMemberRef[] interface_abstracts = iface_desc.Type.ResolveAbstractOverrides(null);

						foreach (DMemberRef abstract_declaration in interface_abstracts)
						{
							bool in_supertype = false;
							DMemberRef implementation = abstract_declaration.Member.GetImplementationInSuperTypes(this, true, ref in_supertype);

							if (implementation != null && !in_supertype)
							{
								implementation.Member.AddAbstractOverride(abstract_declaration);
							}
							else if (in_supertype)
							{
								// we need to add a ghost implementation:
                                if (php_type.Builder != null)
								    this.AddGhostImplementation(abstract_declaration, implementation);
							}
							else
							{
								nulled = ArrayUtils.IndexOfNull(ref abstracts, nulled);
								abstracts[nulled] = abstract_declaration;
								remains++;
							}
						}
					}
				}
			}
			else
			{
				foreach (DTypeDesc iface_desc in TypeDesc.Interfaces)
				{
					InterfaceMapping real_mapping = type.RealType.GetInterfaceMap(iface_desc.RealType);

					for (int i = 0; i < real_mapping.TargetMethods.Length; i++)
					{
						if (real_mapping.TargetMethods[i] == null)
						{
							// unimplemented interfaces method:
							nulled = ArrayUtils.IndexOfNull(ref abstracts, nulled);

							DRoutineDesc abstract_method_desc = iface_desc.GetMethod(new Name(real_mapping.InterfaceMethods[i].Name));
							Debug.Assert(abstract_method_desc != null);

							abstracts[nulled] = new DMemberRef(abstract_method_desc.Member, iface_desc.Type);
							remains++;
						}
					}
				}
			}

			// add declared abstract methods:
			foreach (KnownRoutine declared_method in GetMethods<KnownRoutine>())
			{
				if (declared_method.IsAbstract)
				{
					nulled = ArrayUtils.IndexOfNull(ref abstracts, nulled);
					abstracts[nulled] = new DMemberRef(declared_method, this);
					remains++;
				}
			}

			// add declared abstract properties:
			foreach (KnownProperty declared_property in GetProperties<KnownProperty>())
			{
				if (declared_property.IsAbstract)
				{
					nulled = ArrayUtils.IndexOfNull(ref abstracts, nulled);
					abstracts[nulled] = new DMemberRef(declared_property, this);
					remains++;
				}
			}

			if (remains < abstracts.Length)
			{
				if (remains == 0)
					allAbstracts = DMemberRef.EmptyArray;
				else
					allAbstracts = ArrayUtils.Filter(abstracts, new DMemberRef[remains], null);
			}
			else
				allAbstracts = abstracts;

            allAbstracts = ArrayUtils.EnsureUnique(allAbstracts);

			DebugDumpAllAbstracts();

			return allAbstracts;
		}

		[Conditional("DEBUG")]
		private void DebugDumpAllAbstracts()
		{
			Debug.WriteLine("F-ANALYSIS", "Type '{0}': abstracts = {{{1}}}", this.FullName,
				ArrayUtils.ToList(allAbstracts, delegate(StringBuilder sb, DMemberRef m)
				{
					sb.Append(m.Type.MakeFullGenericName());
					sb.Append("::");
					sb.Append(m.Member.FullName);
				}));
		}

		private static void GetDeclaredAbstracts(DType/*!*/ type, List<DMemberRef>/*!*/ abstracts)
		{
			foreach (KnownRoutine declared_method in type.GetMethods<KnownRoutine>())
			{
				if (declared_method.IsAbstract)
					abstracts.Add(new DMemberRef(declared_method, type));
			}

			foreach (KnownProperty declared_property in type.GetProperties<KnownProperty>())
			{
				if (declared_property.IsAbstract)
					abstracts.Add(new DMemberRef(declared_property, type));
			}
		}

		internal virtual void AddGhostImplementation(DMemberRef/*!*/ abstractMember, DMemberRef/*!*/ implementation)
		{
			Debug.Fail("N/A");
			throw null;
		}

		#endregion

		#region Emission

		/// <summary>
		/// Defines real builders neccessary to reference the type in the IL and, recursively, 
		/// all the depending real builders.
		/// Idempotent operation.
		/// </summary>
		internal virtual void DefineBuilders()
		{
			// nop
		}

		internal abstract PhpTypeCode EmitNew(CodeGenerator/*!*/ codeGenerator, ConstructedType constructedType,
		  DRoutine/*!*/ constructor, CallSignature callSignature, bool runtimeVisibilityCheck);

		internal abstract void EmitInstanceOf(CodeGenerator/*!*/ codeGenerator, ConstructedType constructedType);
		
		internal abstract void EmitTypeOf(CodeGenerator/*!*/ codeGenerator, ConstructedType constructedType);

		internal abstract void EmitLoadTypeDesc(CodeGenerator/*!*/ codeGenerator, ResolveTypeFlags flags);

		/// <summary>
		/// Emits invocation of this type's constructor from within a derived type's constructor.
		/// </summary>
		/// <param name="il"><see cref="ILEmitter"/>.</param>
		/// <param name="derivedType">The derived type in whose (<see cref="ScriptContext"/>, <B>bool</B>)
		/// constructor the emission takes place.</param>
		/// <param name="constructedType"></param>
		internal virtual void EmitInvokeConstructor(ILEmitter/*!*/ il, PhpType/*!*/ derivedType, ConstructedType constructedType)
		{
			Debug.Fail();
		}

#if !SILVERLIGHT
		/// <summary>
		/// Analogous to <see cref="EmitInvokeConstructor"/>.
		/// </summary>
		internal virtual void EmitInvokeDeserializationConstructor(ILEmitter/*!*/ il, PhpType/*!*/ derivedType,
			ConstructedType constructedType)
		{
			Debug.Fail();
		}
#endif

		internal abstract DTypeSpec GetTypeSpec(SourceUnit/*!*/ referringUnit);

		/// <summary>
		/// Defines a real method builder on the type. 
		/// Unifies creation of global, type and dynamic methods.
		/// Returns either <see cref="MethodBuilder"/> or <see cref="DynamicMethod"/>.
		/// </summary>
		public virtual MethodInfo/*!*/ DefineRealMethod(string/*!*/ name, MethodAttributes attributes,
		  Type/*!*/ returnType, Type[]/*!*/ parameterTypes)
		{
			Debug.Fail("This type doesn't support defining methods.");
			throw null;
		}

		#endregion

		#region Utils

		public GenericParameter/*!*/ GetGenericParameter(int i)
		{
			return TypeDesc.GenericParameters[i].GenericParameter;
		}

		public GenericParameter GetGenericParameter(Name name)
		{
			for (int i = 0; i < TypeDesc.GenericParameters.Length; i++)
			{
				GenericParameter param = GetGenericParameter(i);
				if (param.Name.Equals(name))
					return param;
			}

			return null;
		}

		/// <summary>
		/// Gets attribute usage information for this attribute type (if applicable). 
		/// </summary>
		public virtual AttributeUsageAttribute GetCustomAttributeUsage(out bool isDefinite)
		{
			isDefinite = true;
			return null;
		}

		internal static MethodInfo/*!*/ MakeConstructed(MethodInfo/*!*/ info, ConstructedType constructedType)
		{
			if (constructedType != null)
			{
				if (constructedType.IsOpen || constructedType.IsPhpType && ((PhpType)constructedType.GenericType.Type).Declaration != null)
				{
					return TypeBuilder.GetMethod(constructedType.RealType, info);
				}
				else
				{
					// this is workaround TODO !!!
					return constructedType.RealType.GetMethod(info.Name, BindingFlags.DeclaredOnly |
						BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
						null, ReflectionUtils.GetParameterTypes(info.GetParameters()), null);
				}
			}
			else
				return info;
		}

		internal static FieldInfo/*!*/ MakeConstructed(FieldInfo/*!*/ info, ConstructedType constructedType)
		{
			if (constructedType != null)
			{
				if (constructedType.IsOpen || constructedType.IsPhpType && ((PhpType)constructedType.GenericType.Type).Declaration != null)
				{
					return TypeBuilder.GetField(constructedType.RealType, info);
				}
				else
				{
					// this is workaround TODO !!!
					return constructedType.RealType.GetField(info.Name, BindingFlags.DeclaredOnly |
						BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
				}
			}
			else
				return info;
		}

		internal static ConstructorInfo/*!*/ MakeConstructed(ConstructorInfo/*!*/ info, ConstructedType constructedType)
		{
			if (constructedType != null)
			{
				if (constructedType.IsOpen || constructedType.IsPhpType && ((PhpType)constructedType.GenericType.Type).Declaration != null)
				{
					return TypeBuilder.GetConstructor(constructedType.RealType, info);
				}
				else
				{
					// this is workaround TODO !!!
					return constructedType.RealType.GetConstructor(BindingFlags.DeclaredOnly |
						BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
						null, ReflectionUtils.GetParameterTypes(info.GetParameters()), null);
				}
			}
			else
				return info;
		}

		public bool IsSubclassOf(DType/*!*/ other)
		{
			return TypeDesc.IsSubclassOf(other.TypeDesc);
		}

		public bool IsAssignableFrom(DType/*!*/ other)
		{
			return TypeDesc.IsAssignableFrom(other.TypeDesc);
		}

		public bool IsRelatedTo(DType/*!*/ other)
		{
			return TypeDesc.IsRelatedTo(other.TypeDesc);
		}

		public virtual string MakeFullGenericName()
		{
			return this.FullName;
		}

		#endregion

		#region IEquatable<DType> Members

		public abstract bool Equals(DType other);

		public override bool Equals(object obj)
		{
			DType other = obj as DType;
			return (other == null ? false : Equals(other));
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		#endregion
	}

	#endregion

	#region UnknownType

	public sealed class UnknownType : DType
	{
		public static readonly UnknownType UnknownSelf = new UnknownType(Name.SelfClassName.Value);
        public static readonly UnknownType UnknownStatic = new UnknownType(Name.StaticClassName.Value);
		public static readonly UnknownType UnknownParent = new UnknownType(Name.ParentClassName.Value);

		public override bool IsComplete { get { return false; } }
		public override bool IsUnknown { get { return true; } }
		public override bool IsOpen { get { return false; } }
		public override bool IsIdentityDefinite { get { return false; } }
		public override bool ClrVerified { get { return false; } }

		public override KnownRoutine Constructor { get { return null; } }

        /// <summary>
        /// Optionally a <see cref="TypeRef"/> instead of full name.
        /// </summary>
        private readonly TypeRef typeRef;

        public UnknownType(string/*!*/ fullName)
            : this(fullName, null)
        {
        }

        public UnknownType(string/*!*/ fullName, TypeRef typeRef)
            : base(new UnknownTypeDesc()/*use own instance here, not a singleton*/, fullName)
		{
			Debug.Assert(fullName != null);
            this.typeRef = typeRef;
		}

        public override string GetFullName()
		{
			Debug.Fail("full name is set by ctor");
			throw null;
		}

		public override KnownRoutine GetConstructor()
		{
			return null; // default constructor (unknown)
		}


		#region Emission

		internal override PhpTypeCode EmitNew(CodeGenerator/*!*/ codeGenerator, ConstructedType constructedType,
		  DRoutine/*!*/ constructor, CallSignature callSignature, bool runtimeVisibilityCheck)
		{
			Debug.Assert(constructor.IsUnknown);
            codeGenerator.EmitNewOperator(null, typeRef, (constructedType != null) ? constructedType : (DType)this, callSignature);
			return PhpTypeCode.Object;
		}

		internal override void EmitInstanceOf(CodeGenerator/*!*/ codeGenerator, ConstructedType constructedType)
		{
            codeGenerator.EmitInstanceOfOperator(null, typeRef, (constructedType != null) ? constructedType : (DType)this);
		}

		internal override void EmitTypeOf(CodeGenerator/*!*/ codeGenerator, ConstructedType constructedType)
		{
            codeGenerator.EmitTypeOfOperator(null, typeRef, (constructedType != null) ? constructedType : (DType)this);
		}

		internal override void EmitLoadTypeDesc(CodeGenerator/*!*/ codeGenerator, ResolveTypeFlags flags)
		{
            if (typeRef != null)
                typeRef.EmitLoadTypeDesc(codeGenerator, flags);
            else
			    codeGenerator.EmitLoadTypeDescOperator(this.FullName, null, flags);
		}

		internal override DTypeSpec GetTypeSpec(SourceUnit/*!*/ referringUnit)
		{
			return new DTypeSpec(FullName, referringUnit.NamingContextFieldBuilder,
				referringUnit.CompilationUnit.ModuleBuilder.AssemblyBuilder.RealModuleBuilder);
		}

		#endregion

		public override bool Equals(DType other)
		{
			if (ReferenceEquals(this, other)) return true;
			if (other == null) return false;

            return String.Compare(this.FullName, other.FullName, StringComparison.CurrentCultureIgnoreCase) == 0;
		}
	}

	#endregion

	#region KnownType

	[DebuggerDisplay("{FullName}")]
	public abstract class KnownType : DType
	{
		public QualifiedName QualifiedName { get { return qualifiedName; } }
		protected readonly QualifiedName qualifiedName;

		#region Construction

		protected KnownType(DTypeDesc/*!*/ typeDesc, QualifiedName qualifiedName)
			: base(typeDesc)
		{
			Debug.Assert(typeDesc != null);

			this.qualifiedName = qualifiedName;
		}

		#endregion

		public override string GetFullName()
		{
			return qualifiedName.ToString();
		}

		public override bool Equals(DType other)
		{
			if (ReferenceEquals(this, other)) return true;
			if (other == null) return false;
			if (other.IsUnknown) return other.Equals(this);

			return ReferenceEquals(this.TypeDesc, other.TypeDesc);
		}
	}

	#endregion

	#region GlobalType

	public sealed class GlobalType : KnownType
	{
		public override bool IsComplete { get { return true; } }
		public override bool IsUnknown { get { return false; } }
		public override bool IsOpen { get { return false; } }
		public override bool IsIdentityDefinite { get { return true; } }
		public override bool ClrVerified { get { return true; } }

		public IPhpModuleBuilder DeclaringModuleBuilder { get { return (IPhpModuleBuilder)TypeDesc.DeclaringModule; } }
		public ModuleBuilder RealModuleBuilder { get { return DeclaringModuleBuilder.AssemblyBuilder.RealModuleBuilder; } }

		#region Construction

		public GlobalType(DModule/*!*/ declaringModule)
			: base(new GlobalTypeDesc(declaringModule), QualifiedName.Global)
		{
		}

		#endregion

		#region Emission

		public override MethodInfo/*!*/ DefineRealMethod(string/*!*/ name, MethodAttributes attributes,
		  Type/*!*/ returnType, Type[]/*!*/ parameterTypes)
		{
			return DeclaringModuleBuilder.DefineRealFunction(name, attributes, returnType, parameterTypes);
		}

		#endregion

		#region N/A

		internal override DTypeSpec GetTypeSpec(SourceUnit/*!*/ referringUnit)
		{
			Debug.Fail();
			throw null;
		}

		public override KnownRoutine GetConstructor()
		{
			Debug.Fail();
			throw null;
		}

		internal override PhpTypeCode EmitNew(CodeGenerator/*!*/ codeGenerator, ConstructedType constructedType,
		  DRoutine/*!*/ constructor, CallSignature callSignature, bool runtimeVisibilityCheck)
		{
			Debug.Fail("N/A");
			throw null;
		}

		internal override void EmitInstanceOf(CodeGenerator/*!*/ codeGenerator, ConstructedType constructedType)
		{
			Debug.Fail("N/A");
			throw null;
		}

		internal override void EmitTypeOf(CodeGenerator/*!*/ codeGenerator, ConstructedType constructedType)
		{
			Debug.Fail("N/A");
			throw null;
		}

		internal override void EmitLoadTypeDesc(CodeGenerator/*!*/ codeGenerator, ResolveTypeFlags flags)
		{
			Debug.Fail("N/A");
			throw null;
		}

		#endregion
	}

	#endregion

	#region PrimitiveType

	public sealed class PrimitiveType : KnownType
	{
		#region Statiscs

		public static readonly PrimitiveType/*!*/ Boolean;
		public static readonly PrimitiveType/*!*/ Integer;
		public static readonly PrimitiveType/*!*/ LongInteger;
		public static readonly PrimitiveType/*!*/ Double;
		public static readonly PrimitiveType/*!*/ String;
		public static readonly PrimitiveType/*!*/ Resource;
		public static readonly PrimitiveType/*!*/ Array;
		public static readonly PrimitiveType/*!*/ Object;
        public static readonly PrimitiveType/*!*/ Callable;

		static PrimitiveType()
		{
			if (UnknownModule.RuntimeModule == null) UnknownModule.RuntimeModule = new UnknownModule();

			Boolean = new PrimitiveType(DTypeDesc.BooleanTypeDesc, QualifiedName.Boolean);
			Integer = new PrimitiveType(DTypeDesc.IntegerTypeDesc, QualifiedName.Integer);
			LongInteger = new PrimitiveType(DTypeDesc.LongIntegerTypeDesc, QualifiedName.LongInteger);
			Double = new PrimitiveType(DTypeDesc.DoubleTypeDesc, QualifiedName.Double);
			String = new PrimitiveType(DTypeDesc.StringTypeDesc, QualifiedName.String);
			Resource = new PrimitiveType(DTypeDesc.ResourceTypeDesc, QualifiedName.Resource);
			Array = new PrimitiveType(DTypeDesc.ArrayTypeDesc, QualifiedName.Array);
			Object = new PrimitiveType(DTypeDesc.ObjectTypeDesc, QualifiedName.Object);
            Callable = new PrimitiveType(DTypeDesc.CallableTypeDesc, QualifiedName.Callable);
		}

		#endregion

		#region Properties

		public override bool IsComplete { get { return true; } }
		public override bool IsUnknown { get { return false; } }
		public override bool IsOpen { get { return false; } }
		public override bool IsIdentityDefinite { get { return true; } }
		public override bool ClrVerified { get { return true; } }

		public PrimitiveTypeDesc PrimitiveTypeDesc { get { return (PrimitiveTypeDesc)TypeDesc; } }
		public PhpTypeCode TypeCode { get { return PrimitiveTypeDesc.TypeCode; } }

		#endregion

		#region Construction

		private PrimitiveType(PrimitiveTypeDesc/*!*/ typeDesc, QualifiedName qualifiedName)
			: base(typeDesc, qualifiedName)
		{
		}

		#endregion

		#region Utils

		public override KnownRoutine GetConstructor()
		{
			// default ctor:
			return null;
		}

		internal static PrimitiveType GetByTypeCode(PhpTypeCode typeCode)
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
				default: return null;
			}
		}

		[Obsolete]
		internal static PrimitiveType GetByName(Name name)
		{
			if (name == QualifiedName.Boolean.Name) return Boolean;
			if (name == QualifiedName.Integer.Name) return Integer;
			if (name == QualifiedName.Double.Name) return Double;
			if (name == QualifiedName.String.Name) return String;
			if (name == QualifiedName.Array.Name) return Array;
			if (name == QualifiedName.Resource.Name) return Resource;
			return null;
		}

		#endregion

		#region Emission

		internal override PhpTypeCode EmitNew(CodeGenerator/*!*/ codeGenerator, ConstructedType constructedType,
		  DRoutine/*!*/ constructor, CallSignature callSignature, bool runtimeVisibilityCheck)
		{
			ILEmitter il = codeGenerator.IL;

			switch (TypeCode)
			{
				case PhpTypeCode.Boolean:
					il.Emit(OpCodes.Ldc_I4_0);
					break;

				case PhpTypeCode.Integer:
					il.Emit(OpCodes.Ldc_I4_0);
					break;

				case PhpTypeCode.LongInteger:
					il.Emit(OpCodes.Ldc_I8, (long)0);
					break;

				case PhpTypeCode.Double:
					il.Emit(OpCodes.Ldc_R8, (double)0.0);
					break;

				case PhpTypeCode.PhpResource:
					il.Emit(OpCodes.Ldnull);
					break;

				case PhpTypeCode.PhpArray:
					il.Emit(OpCodes.Newobj, Constructors.PhpArray.Void);
					break;

				case PhpTypeCode.String:
					il.Emit(OpCodes.Ldstr, "");
					break;

				case PhpTypeCode.DObject:
					codeGenerator.EmitLoadScriptContext();
					il.Emit(OpCodes.Newobj, Constructors.StdClass_ScriptContext);
					break;

                case PhpTypeCode.PhpCallable:
                    throw new InvalidOperationException();

				default:
					throw null;
			}

			return TypeCode;
		}

		internal override void EmitInstanceOf(CodeGenerator/*!*/ codeGenerator, ConstructedType constructedType)
		{
			Debug.Assert(constructedType == null, "primitive types cannot be generic");

			ILEmitter il = codeGenerator.IL;

			switch (TypeCode)
			{
				case PhpTypeCode.Boolean:
				case PhpTypeCode.Integer:
				case PhpTypeCode.LongInteger:
				case PhpTypeCode.Double:
					il.Emit(OpCodes.Isinst, RealType);
					il.Emit(OpCodes.Ldnull);
					il.Emit(OpCodes.Cgt_Un);
					break;

				case PhpTypeCode.PhpResource:
				case PhpTypeCode.PhpArray:
					il.Emit(OpCodes.Isinst, RealType);
					break;

				case PhpTypeCode.String:
					il.Emit(OpCodes.Call, Methods.PhpVariable.IsString);
					break;

				case PhpTypeCode.DObject:
					if (!codeGenerator.Context.Config.Compiler.ClrSemantics)
						il.Emit(OpCodes.Isinst, typeof(DObject));
					else
						il.Emit(OpCodes.Ldc_I4_1); // true; all values are of type System.Object	
					break;

                case PhpTypeCode.PhpCallable:
                    // LOAD Operators.IsCallable( <stack>, <classcontext>, false)
                    codeGenerator.EmitLoadClassContext();
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Call, Methods.Operators.IsCallable);
                    break;

				default:
					throw null;
			}
		}

		internal override void EmitTypeOf(CodeGenerator/*!*/ codeGenerator, ConstructedType constructedType)
		{
			Debug.Assert(constructedType == null, "primitive types cannot be generic");

            if (this.TypeCode == PhpTypeCode.PhpCallable)
                throw new InvalidOperationException();

			ILEmitter il = codeGenerator.IL;
			il.Emit(OpCodes.Ldtoken, this.RealType);
			il.Emit(OpCodes.Call, Methods.GetTypeFromHandle);
			il.Emit(OpCodes.Call, Methods.ClrObject_WrapRealObject);
		}
		
		internal override void EmitLoadTypeDesc(CodeGenerator/*!*/ codeGenerator, ResolveTypeFlags flags)
		{
			FieldInfo field;

			switch (TypeCode)
			{
				case PhpTypeCode.Boolean: field = Fields.DTypeDesc.BooleanTypeDesc; break;
				case PhpTypeCode.Integer: field = Fields.DTypeDesc.IntegerTypeDesc; break;
				case PhpTypeCode.LongInteger: field = Fields.DTypeDesc.LongIntegerTypeDesc; break;
				case PhpTypeCode.Double: field = Fields.DTypeDesc.DoubleTypeDesc; break;
				case PhpTypeCode.String: field = Fields.DTypeDesc.StringTypeDesc; break;
				case PhpTypeCode.PhpResource: field = Fields.DTypeDesc.ResourceTypeDesc; break;
				case PhpTypeCode.PhpArray: field = Fields.DTypeDesc.ArrayTypeDesc; break;
				case PhpTypeCode.DObject: field = Fields.DTypeDesc.ObjectTypeDesc; break;
                case PhpTypeCode.PhpCallable: throw new InvalidOperationException();
				default: throw null;
			}

			codeGenerator.IL.Emit(OpCodes.Ldsfld, field);
		}

		internal override DTypeSpec GetTypeSpec(SourceUnit/*!*/ referringUnit)
		{
            return new DTypeSpec(TypeCode);
		}

		#endregion
	}

	#endregion

    #region StaticType

    /// <summary>
    /// Type representing <c>static</c> keyword (late static binding).
    /// Used only during compilation.
    /// </summary>
    public sealed class StaticType : KnownType
    {
        public override bool IsComplete { get { return true; } }
		public override bool IsUnknown { get { return true; } }
		public override bool IsOpen { get { return false; } }
		public override bool IsIdentityDefinite { get { return false; } }
		public override bool ClrVerified { get { return false; } }

		public override KnownRoutine Constructor { get { return null; } }

        public static StaticType/*!*/Singleton { get { return _singleton ?? (_singleton = new StaticType()); } }
        private static StaticType _singleton = null;

        private StaticType()
            : base(new UnknownTypeDesc(), new QualifiedName(Name.StaticClassName))
        {
		}

		public override string GetFullName()
		{
            throw new NotSupportedException();
		}

		public override KnownRoutine GetConstructor()
		{
			return null; // default constructor (unknown)
		}

		#region Emission

		internal override PhpTypeCode EmitNew(CodeGenerator/*!*/ codeGenerator, ConstructedType constructedType,
		  DRoutine/*!*/ constructor, CallSignature callSignature, bool runtimeVisibilityCheck)
		{
			Debug.Assert(constructor.IsUnknown);
			codeGenerator.EmitNewOperator(null, null, (DType)this, callSignature);
			return PhpTypeCode.Object;
		}

		internal override void EmitInstanceOf(CodeGenerator/*!*/ codeGenerator, ConstructedType constructedType)
		{
			codeGenerator.EmitInstanceOfOperator(null, null, (DType)this);
		}

		internal override void EmitTypeOf(CodeGenerator/*!*/ codeGenerator, ConstructedType constructedType)
		{
			codeGenerator.EmitTypeOfOperator(null, null, (DType)this);
		}

		internal override void EmitLoadTypeDesc(CodeGenerator/*!*/ codeGenerator, ResolveTypeFlags flags)
		{
            codeGenerator.EmitLoadStaticTypeDesc(flags);
		}

		internal override DTypeSpec GetTypeSpec(SourceUnit/*!*/ referringUnit)
		{
            throw new NotSupportedException();
		}

		#endregion
    }

    #endregion

    #region GenericParameter

    /// <summary>
	/// Represents a type parameter of a generic type or method.
	/// Created during pre-analysis.
	/// </summary>
	[DebuggerNonUserCode, DebuggerDisplay("{FullName}")]
	public sealed class GenericParameter : KnownType
	{
		#region Properties

		public new static readonly GenericParameter[] EmptyArray = new GenericParameter[0];

		public override bool IsComplete { get { return false; } }
		public override bool IsUnknown { get { return false; } }
		public override bool IsOpen { get { return true; } }

		public override bool ClrVerified { get { return true; } }
		public override bool IsGenericParameter { get { return true; } }

		/// <summary>
		/// The actual identity of the type (the substitute) is not known.
		/// The analyzer cannot reason about it's modifiers, generic parameters nor declared members.
		/// </summary>
		public override bool IsIdentityDefinite { get { return false; } }

		public GenericParameterDesc/*!*/ GenericParameterDesc { get { return (GenericParameterDesc)TypeDesc; } }

		/// <summary>
		/// Valid only if the declaring member is a type whose type builder has been defined.
		/// </summary>
		public GenericTypeParameterBuilder RealGenericTypeParameterBuilder
		{
			get
			{
				Debug.Assert(declaringMember is PhpType);
				return (GenericTypeParameterBuilder)RealType;
			}
		}

		/// <summary>
		/// Valid only if the declaring member is a routine whose builders has already been defined.
		/// </summary>
		public ParameterBuilder RealPseudoGenericParameterBuilder
		{
			get
			{
				Debug.Assert(declaringMember is PhpRoutine);
				PhpRoutine routine = (PhpRoutine)declaringMember;
				return routine.Builder.ParameterBuilders[routine.FirstPseudoGenericParameterIndex + index];
			}
		}

		public Name Name { get { return qualifiedName.Name; } }

		/// <summary>
		/// Filled by member analysis (written up).
		/// </summary>
		public DType DefaultType
		{
			get { return (GenericParameterDesc.DefaultType != null) ? GenericParameterDesc.DefaultType.Type : null; }
		}

		/// <summary>
		/// Index in the parameter list.
		/// </summary>
		public int Index { get { return index; } }
		private readonly int index;

		/// <summary>
		/// Declarer: DType or DMethod.
		/// </summary>
		public DMember DeclaringMember { get { return declaringMember; } }
		private readonly DMember declaringMember;

		public override DType DeclaringType { get { return (PhpType)declaringMember; } }
		public PhpRoutine DeclaringRoutine { get { return (PhpRoutine)declaringMember; } }

		#endregion

		#region Construction

		/// <summary>
		/// Used by the compiler.
		/// </summary>
		public GenericParameter(Name name, int index, DMember/*!*/ declaringMember)
			: base(new GenericParameterDesc(), new QualifiedName(name, Name.EmptyNames))
		{
			Debug.Assert(declaringMember != null);

			this.index = index;
			this.declaringMember = declaringMember;
		}

		internal void WriteUp(DType defaultType)
		{
			this.GenericParameterDesc.WriteUp((defaultType != null) ? defaultType.TypeDesc : null);
		}

		#endregion

		#region Utils

		/// <summary>
		/// Returns <B>null</B> as the constructor for the type is unknown.
		/// </summary>
		public override KnownRoutine GetConstructor()
		{
			return null;
		}

		internal string GetPseudoArgumentName()
		{
			return "#" + FullName;
		}

		#endregion

		#region Emission

		/// <summary>
		/// Define builders for parameters on type.
		/// </summary>
		internal void DefineBuildersWithinType(GenericTypeParameterBuilder/*!*/ paramBuilder)
		{
			TypeDesc.DefineBuilder(paramBuilder);
		}

		/// <summary>
		/// Define builders for parameters on method.
		/// </summary>
		internal void DefineBuildersWithinMethod()
		{
			Debug.Assert(declaringMember is PhpRoutine);
			PhpRoutine routine = (PhpRoutine)declaringMember;
			Debug.Assert(routine.Builder != null && routine.ArgFullInfo != null);

			ParameterBuilder param_builder;

			int real_index = routine.FirstPseudoGenericParameterIndex + index;

			// define pseudo-generic parameter:
			routine.Builder.ParameterBuilders[real_index] = param_builder = ReflectionUtils.DefineParameter(routine.ArgFullInfo,
			  (routine.IsStatic ? 1 : 0) + real_index,
			  ParameterAttributes.None,
			  GetPseudoArgumentName());
		}

		internal override PhpTypeCode EmitNew(CodeGenerator/*!*/ codeGenerator, ConstructedType constructedType,
		  DRoutine/*!*/ constructor, CallSignature callSignature, bool runtimeVisibilityCheck)
		{
			codeGenerator.EmitNewOperator(null, null, (constructedType != null) ? constructedType : (DType)this, callSignature);
			return PhpTypeCode.Object;
		}

		internal override void EmitInstanceOf(CodeGenerator/*!*/ codeGenerator, ConstructedType constructedType)
		{
			codeGenerator.EmitInstanceOfOperator(null, null, (constructedType != null) ? constructedType : (DType)this);
		}

		internal override void EmitTypeOf(CodeGenerator/*!*/ codeGenerator, ConstructedType constructedType)
		{
			codeGenerator.EmitTypeOfOperator(null, null, (constructedType != null) ? constructedType : (DType)this);
		}

		internal override void EmitLoadTypeDesc(CodeGenerator/*!*/ codeGenerator, ResolveTypeFlags flags)
		{
			Debug.Assert(declaringMember is PhpType || declaringMember is PhpRoutine);

			PhpRoutine routine = declaringMember as PhpRoutine;
			if (routine != null)
			{
				codeGenerator.IL.Ldarg(((PhpRoutine)declaringMember).FirstPseudoGenericParameterIndex + index);
			}
			else
			{
				codeGenerator.EmitLoadTypeDesc(this.RealType);
			}
		}

		internal void SetCustomAttribute(CustomAttributeBuilder/*!*/ builder)
		{
			if (declaringMember is PhpRoutine)
			{
				RealPseudoGenericParameterBuilder.SetCustomAttribute(builder);
			}
			else
			{
				RealGenericTypeParameterBuilder.SetCustomAttribute(builder);
			}
		}

		internal override DTypeSpec GetTypeSpec(SourceUnit/*!*/ referringUnit)
		{
			return new DTypeSpec(this.index, (declaringMember is PhpRoutine) ? MemberTypes.Method : MemberTypes.TypeInfo);
		}

		#endregion
	}

	#endregion

	#region ConstructedType

	/// <summary>
	/// Represents a type constructed from a generic type filling the type parameters.
	/// </summary>
	/// <remarks>
	/// Compiler creates a single instance per occurrence.
	/// Shares type-desc with the generic type and has also the same qualified name.
	/// This sharing transfers the resposibility for member look-up to the generic type (via its type-desc).
	/// </remarks>
	[DebuggerDisplay("{MakeFullGenericName()}")]
	public sealed class ConstructedType : DType, IEquatable<ConstructedType>
	{
		#region Properties

		/// <summary>
		/// Constructed type is unknown iff its generic type is unknown.
		/// </summary>
		public override bool IsUnknown { get { return GenericType.IsUnknown; } }

		/// <summary>
		/// To decide whether or not to report an error, it suffice to known whether the generic type is known definitely.
		/// </summary>
		public override bool IsIdentityDefinite { get { return GenericType.Type.IsIdentityDefinite; } }

		/// <summary>
		/// <B>true</B> iff the generic type and all arguments are complete types.
		/// </summary>
		public override bool IsComplete { get { Debug.Assert(inheritanceAnalysisState != DfsStates.Initial); return isComplete; } }

		/// <summary>
		/// <B>true</B> iff the generic type or any argument is a open type.
		/// Note that the generic type can be a generic parameter, which is an open type.
		/// </summary>
		public override bool IsOpen { get { Debug.Assert(inheritanceAnalysisState != DfsStates.Initial); return isOpen; } }

		/// <summary>
		/// <B>true</B> iff the generic type is a generic parameter.
		/// </summary>
		public override bool IsGenericParameter { get { return GenericType.Type.IsGenericParameter; } }

		/// <summary>
		/// <B>true</B> iff the generic type is a PHP type.
		/// </summary>
		public override bool IsPhpType { get { return GenericType.Type.IsPhpType; } }

		/// <summary>
		/// <B>true</B> iff the generic type is a CLR type.
		/// </summary>
		public override bool IsClrType { get { return GenericType.Type.IsClrType; } }

		/// <summary>
		/// Constructed type is definite iff the generic type is definite and it is not a generic parameter 
		///	(hence we cannot refer to the members then) and all arguments are definite types.
		/// </summary>
		/// <remarks>
		/// We can emit the TypeSpec token to the IL if the constructed type is definite.
		/// The constructed types that are not definite are reconstructed at run-time using type-desc.
		/// </remarks>
		public override bool IsDefinite { get { Debug.Assert(inheritanceAnalysisState != DfsStates.Initial); return isDefinite; } }

		public override bool ClrVerified { get { return GenericType.Type.ClrVerified; } }

		private bool isOpen;
		private bool isComplete;
		private bool isDefinite;

		public ConstructedTypeDesc ConstructedTypeDesc { get { return (ConstructedTypeDesc)TypeDesc; } }

		public DTypeDesc/*!*/ GenericType { get { return ConstructedTypeDesc.GenericType; } }
		public DTypeDesc[]/*!!*/ Arguments { get { return ConstructedTypeDesc.Arguments; } }

		#endregion

		#region Construction

		/// <summary>
		/// To be used by the compiler for creation of constructed types during analysis. 
		/// </summary>
		internal ConstructedType(DTypeDesc/*!*/ genericType, DTypeDesc[]/*!!*/ arguments)
			: base(new ConstructedTypeDesc(genericType, arguments))
		{
			Debug.Assert(genericType != null && arguments != null && arguments.Length > 0);

			this.inheritanceAnalysisState = DfsStates.Initial;
		}

		#endregion

		#region Analysis

		private DfsStates inheritanceAnalysisState;
		private List<DType> dfsPredecessors;

		internal void Analyze(Analyzer/*!*/ analyzer)
		{
			if (inheritanceAnalysisState != DfsStates.Done)
			{
				AnalyzeInheritance(analyzer.ErrorSink);
			}
		}

		internal override void AnalyzeInheritance(ErrorSink/*!*/ errors, ref List<DType> mfpWorklist, DType dfsPredecessor,
			PhpType phpPredecessor, int dfsDepth, int inheritanceDepth)
		{
			Debug.Assert(errors != null);

			if (dfsDepth >= 0)
			{
				switch (inheritanceAnalysisState)
				{
					case DfsStates.Initial:
						{
							inheritanceAnalysisState = DfsStates.Entered;

							// initialize
							isOpen = false;
							isComplete = true;
							isDefinite = true;

							// generic edge descend (increase the inheritance depth):
							GenericType.Type.AnalyzeInheritance(errors, ref mfpWorklist, this, phpPredecessor, dfsDepth + 1, inheritanceDepth + 1);

							// constructed edge descend (zero the inheritance depth):
							foreach (DTypeDesc arg_desc in Arguments)
								arg_desc.Type.AnalyzeInheritance(errors, ref mfpWorklist, this, phpPredecessor, dfsDepth + 1, 0);

							UpdateInheritanceProperties();

							inheritanceAnalysisState = DfsStates.Done;
							break;
						}

					case DfsStates.Entered:
						if (mfpWorklist == null)
							mfpWorklist = new List<DType>();

						// edge from DFS predecessor to this node closes a loop => add predecessor to the worklist as 
						// its properties depends on the values that will be computed on this node later:
						mfpWorklist.Add(dfsPredecessor);

						// check whether the backward edge closes inheritance loop:
						Debug.Assert(GenericType.Type is PhpType, "A loop can only comprise of PhpTypes and ConstructedTypes");

						PhpType generic_php_type = (PhpType)this.GenericType.Type;

						Debug.Assert(generic_php_type.Builder != null, "A loop can only comprise of non-reflected types");
						Debug.Assert(dfsDepth - 2 >= generic_php_type.Builder.InheritanceAnalysisDfsDepth);
						Debug.Assert(phpPredecessor != null, "There has to be some PhpType bellow this constructed type in the tree (at least the generic type)");

						if ((dfsDepth - 2) - generic_php_type.Builder.InheritanceAnalysisDfsDepth <= inheritanceDepth)
						{
							// fatal error - circular inheritance (the further analysis assumes non-circularity):
							errors.Add((generic_php_type.IsInterface) ? FatalErrors.CircularBaseInterfaceDependency : FatalErrors.CircularBaseClassDependency,
								generic_php_type.Declaration.SourceUnit, generic_php_type.Declaration.Position, phpPredecessor.FullName,
								generic_php_type.FullName);
							phpPredecessor.ReportError(errors, FatalErrors.RelatedLocation);

							throw new CompilerException();
						}

						break;

					case DfsStates.Done:
						// do nothing, just add predecessor ...
						break;
				}

				// add predecessor regardless of the DFS state of the current node:
				if (dfsPredecessor != null)
				{
					if (dfsPredecessors == null)
						dfsPredecessors = new List<DType>();

					dfsPredecessors.Add(dfsPredecessor);
				}
			}
			else
			{
				Debug.Assert(mfpWorklist != null);

				// save current state of MFP properties:
				bool was_complete = this.isComplete;

				// recalculate the properties:
				UpdateInheritanceProperties();

				// check changes in MFP properties and queue the influenced nodes for MFP processing:
				if (this.isComplete != was_complete && dfsPredecessors != null)
					mfpWorklist.AddRange(dfsPredecessors);
			}
		}

		private void UpdateInheritanceProperties()
		{
			DType generic_type = GenericType.Type;

			// generic type can be open if it is a generic parameter 
			// (which is allowed unless occurring in extends/implements clause);
			this.isOpen |= generic_type.IsGenericParameter;
			this.isComplete &= generic_type.IsComplete;
			this.isDefinite &= generic_type.IsDefinite && !generic_type.IsGenericParameter;

			foreach (DTypeDesc arg_desc in Arguments)
			{
				DType arg = arg_desc.Type;

				this.isOpen |= arg.IsOpen;

				// we cannot use arg.IsGenericParameter as only non-constructed generic parameters should be treated definite:
				GenericParameter gp = arg as GenericParameter;

				if (gp != null)
				{
					// method generic params are indefinite, type generic params are definite:
					this.isDefinite &= gp.DeclaringMember is PhpType;
				}
				else
				{
					this.isComplete &= arg.IsComplete;
					this.isDefinite &= arg.IsDefinite;
				}
			}
		}

		internal override void GetInheritanceProperties(out bool extendsClrType, out DType/*!*/ hierarchyRoot)
		{
			if (Base != null)
			{
				Base.GetInheritanceProperties(out extendsClrType, out hierarchyRoot);
			}
			else
			{
				extendsClrType = this.GenericType.Type.IsClrType;
				hierarchyRoot = this.GenericType.Type;
			}
		}

		internal override void AddGhostImplementation(DMemberRef/*!*/ abstractMember, DMemberRef/*!*/ implementation)
		{
			GenericType.Type.AddGhostImplementation(abstractMember, implementation);
		}

		#endregion

		#region Utils

		public override string GetFullName()
		{
			return GenericType.Type.FullName;
		}

		public override string MakeFullGenericName()
		{
			StringBuilder result = new StringBuilder(GenericType.Type.FullName);
			result.Append("<:");

			for (int i = 0; i < Arguments.Length; i++)
			{
				if (i > 0) result.Append(',');
				result.Append(Arguments[i].Type.MakeFullGenericName());
			}

			result.Append(":>");

			return result.ToString();
		}

		public Type/*!*/ MapRealType(Type/*!*/ type)
		{
			if (type.IsGenericParameter &&
				type.DeclaringMethod == null && type.DeclaringType == GenericType.RealType)
			{
				return Arguments[type.GenericParameterPosition].RealType;
			}
			else return type;
		}

		#endregion

		#region Member Lookup

		public override KnownRoutine GetConstructor()
		{
			return GenericType.Type.GetConstructor();
		}

		#endregion

		#region Emission

		internal override void DefineBuilders()
		{
			// already baked or won't have builders:
			if (this.RealType != null || !isDefinite)
				return;

			GenericType.Type.DefineBuilders();

			for (int i = 0; i < Arguments.Length; i++)
				Arguments[i].Type.DefineBuilders();

			// creates a real constructed type:
			bool success;
			Type[] real = GetRealArguments(out success);
			if (success)
				this.TypeDesc.Bake(GenericType.RealType.MakeGenericType(real));
		}

		/// <param name="success">Is set to false when any argument is not known at this point
		/// (for example when reflecting a base class reference in a generic class declaration that 
		/// depends on the type argument that is not defined until emission).</param>
		/// <returns></returns>
		internal Type[]/*!*/ GetRealArguments(out bool success)
		{
			Type[] real_arguments = new Type[Arguments.Length];
			success = true;

			for (int i = 0; i < Arguments.Length; i++)
			{
				Type t = Arguments[i].RealType;
				real_arguments[i] = t;
				if (t == null) success = false;
			}

			return real_arguments;
		}

		internal override PhpTypeCode EmitNew(CodeGenerator/*!*/ codeGenerator, ConstructedType constructedType,
		  DRoutine/*!*/ constructor, CallSignature callSignature, bool runtimeVisibilityCheck)
		{
			Debug.Assert(constructedType == null);

			if (isDefinite && !runtimeVisibilityCheck)
			{
				// delegate emission to the generic type passing it a back-reference:
				return GenericType.Type.EmitNew(codeGenerator, this, constructor, callSignature, runtimeVisibilityCheck);
			}
			else
			{
				codeGenerator.EmitNewOperator(null, null, this, callSignature);
				return PhpTypeCode.Object;
			}
		}

		internal override void EmitInstanceOf(CodeGenerator/*!*/ codeGenerator, ConstructedType constructedType)
		{
			Debug.Assert(constructedType == null);

			if (isDefinite)
			{
				// delegate emission to the generic type passing it a back-reference:
				GenericType.Type.EmitInstanceOf(codeGenerator, this);
			}
			else
			{
				codeGenerator.EmitInstanceOfOperator(null, null, this);
			}
		}

		internal override void EmitTypeOf(CodeGenerator/*!*/ codeGenerator, ConstructedType constructedType)
		{
			Debug.Assert(constructedType == null);

			if (isDefinite)
			{
				// delegate emission to the generic type passing it a back-reference:
				GenericType.Type.EmitTypeOf(codeGenerator, this);
			}
			else
			{
				codeGenerator.EmitTypeOfOperator(null, null, this);
			}
		}


		internal override void EmitInvokeConstructor(ILEmitter/*!*/ il, PhpType/*!*/ derivedType, ConstructedType constructedType)
		{
			Debug.Assert(constructedType == null);
			this.GenericType.Type.EmitInvokeConstructor(il, derivedType, this);
		}

#if !SILVERLIGHT
		internal override void EmitInvokeDeserializationConstructor(ILEmitter/*!*/ il, PhpType/*!*/ derivedType,
			ConstructedType constructedType)
		{
			Debug.Assert(constructedType == null);
			this.GenericType.Type.EmitInvokeDeserializationConstructor(il, derivedType, this);
		}
#endif

		internal override void EmitLoadTypeDesc(CodeGenerator/*!*/ codeGenerator, ResolveTypeFlags flags)
		{
			if (isDefinite)
			{
				// emit ldtoken for the real type TypeSpec:
				Debug.Assert(this.RealType != null);
				codeGenerator.EmitLoadTypeDesc(this.RealType);
			}
			else
			{
				// LOAD Operators.MakeGenericInstantiation(<generic type desc>, <generic argument descs>)
				ILEmitter il = codeGenerator.IL;

				GenericType.Type.EmitLoadTypeDesc(codeGenerator, flags);

				il.EmitOverloadedArgs(Types.DTypeDesc[0], Arguments.Length, Methods.Operators.MakeGenericTypeInstantiation.ExplicitOverloads, delegate(ILEmitter _il, int i)
				{
					Arguments[i].Type.EmitLoadTypeDesc(codeGenerator, flags);
				});

				il.Emit(OpCodes.Call, Methods.Operators.MakeGenericTypeInstantiation.Overload(Arguments.Length));
			}
		}

		internal override DTypeSpec GetTypeSpec(SourceUnit/*!*/ referringUnit)
		{
			if (isDefinite)
			{
				return new DTypeSpec(RealType, referringUnit.CompilationUnit.ModuleBuilder.AssemblyBuilder.RealModuleBuilder);
			}
			else
			{

				DTypeSpec[] arg_specs = new DTypeSpec[Arguments.Length];

				for (int i = 0; i < Arguments.Length; i++)
					arg_specs[i] = Arguments[i].Type.GetTypeSpec(referringUnit);

				return new DTypeSpec(GenericType.Type.GetTypeSpec(referringUnit), arg_specs);
			}
		}

		#endregion

		#region IEquatable<ConstructedType> Members

		public override bool Equals(DType other)
		{
			return Equals(other as ConstructedType);
		}

		public bool Equals(ConstructedType other)
		{
			if (ReferenceEquals(this, other)) return true;

			if (!this.GenericType.Equals(other.GenericType))
				return false;

			for (int i = 0; i < Arguments.Length; i++)
			{
				if (!this.Arguments[i].Equals(other.Arguments[i]))
					return false;
			}

			return true;
		}

		#endregion
	}

	#endregion

	#region ClrType

	/// <summary>
	/// Represents CLR type. For generic types, this instance represents the generic template
	/// all instantiations (represented by ClrTypeDesc) are sharing.
	/// </summary>
	[DebuggerDisplay("{FullName}")]
	public sealed class ClrType : KnownType
	{
		#region Properties

		public static readonly ClrType/*!*/ SystemObject = new ClrType(DTypeDesc.SystemObjectTypeDesc, QualifiedName.SystemObject);

		public override bool IsComplete { get { return true; } }
		public override bool IsUnknown { get { return false; } }
		public override bool IsOpen { get { return false; } }
		public override bool IsIdentityDefinite { get { return true; } }

		public override bool IsClrType { get { return true; } }
		public override bool ClrVerified { get { return true; } }

		/// <summary>
		/// CLR type is considered generic if its real type is generic or if there are some generic overloads.
		/// In the latter case, at least one of the overloads has generic real type.
		/// </summary>
		public override bool IsGeneric
		{
			get { return RealType.IsGenericTypeDefinition || ClrTypeDesc.GenericOverloads.Count > 0; }
		}

		public ClrTypeDesc ClrTypeDesc { get { return (ClrTypeDesc)TypeDesc; } }

		/// <summary>
		/// Gets constructor of this type (if any).
		/// </summary>
		public override KnownRoutine/*!*/ Constructor { get { return ClrTypeDesc.Constructor.KnownRoutine; } }
		public ClrMethod/*!*/ ClrConstructor { get { return ClrTypeDesc.Constructor.ClrMethod; } }

		#endregion

		#region Construction

		/// <summary>
		/// Used by full-reflect (<see cref="DTypeDesc.Reflect"/>).
		/// </summary>
		public ClrType(DTypeDesc/*!*/ typeDesc, QualifiedName qualifiedName)
			: base(typeDesc, qualifiedName)
		{
		}

		#endregion

		#region Utils

		public override KnownRoutine/*!*/ GetConstructor()
		{
			return ClrTypeDesc.Constructor.ClrMethod;
		}

		#endregion

		#region Analysis: ResolveAbstractOverrides, MakeConstructedType

		internal override DType/*!*/ MakeConstructedType(Analyzer/*!*/ analyzer, DTypeDesc[]/*!*/ arguments, Position position)
		{
			if (!IsGeneric)
			{
				if (arguments.Length > 0)
					analyzer.ErrorSink.Add(Errors.NonGenericTypeUsedWithTypeArgs, analyzer.SourceUnit, position, this.FullName);

				return this;
			}

			bool success;
			ClrTypeDesc overload = ClrTypeDesc.ResolveGenericOverload(arguments.Length, out success);

			// too many args:
			if (!success)
			{
				analyzer.ErrorSink.Add(Errors.TooManyTypeArgumentsInTypeUse, analyzer.SourceUnit, position,
				  this.FullName, overload.GenericParameterCount);

				return this;
			}

			// the matching overload's real type is not a generic type:
			if (!overload.RealType.IsGenericTypeDefinition)
				return this;

			int arg_count = arguments.Length;
			if (!overload.MakeGenericArguments(ref arguments, ref arg_count,
				delegate(DTypeDesc.MakeGenericArgumentsResult error, DTypeDesc/*!*/ genericType, DTypeDesc argument, GenericParameterDesc/*!*/ parameter)
				{
					switch (error)
					{
						case DTypeDesc.MakeGenericArgumentsResult.IncompatibleConstraint:
							analyzer.ErrorSink.Add(Errors.IncompatibleTypeParameterConstraintsInTypeUse, analyzer.SourceUnit, position, argument.Type.FullName,
								parameter.GenericParameter.Index.ToString(), parameter.RealType.Name, genericType.Type.FullName);
							break;

						case DTypeDesc.MakeGenericArgumentsResult.MissingArgument:
							analyzer.ErrorSink.Add(Errors.MissingTypeArgumentInTypeUse, analyzer.SourceUnit, position,
								genericType.Type.FullName, parameter.RealType.GenericParameterPosition + 1, parameter.RealType.Name);
							break;

						default:
							Debug.Fail("Other options checked earlier");
							break;
					}
				}
			))
			{
				// missing mandatory argument(s):
				return this;
			}

			// create a constructed type from the resolved type and arguments:
			return analyzer.CreateConstructedType(overload, arguments, arg_count);
		}

		#endregion

		#region Custom Attributes

		private AttributeUsageAttribute _customAttributeUsage = null;
		private bool _customAttributeUsageSet = false;

		/// <summary>
		/// Gets attribute usage attribute applied on the type (if any).
		/// </summary>
		public override AttributeUsageAttribute GetCustomAttributeUsage(out bool isDefinite)
		{
			isDefinite = true;

			if (!_customAttributeUsageSet)
			{
				object[] attrs = this.RealType.GetCustomAttributes(typeof(AttributeUsageAttribute), true);
				if (attrs.Length == 1) _customAttributeUsage = (AttributeUsageAttribute)attrs[0];
				_customAttributeUsageSet = true;
			}

			return _customAttributeUsage;
		}

		#endregion

		#region Emission

		internal override PhpTypeCode EmitNew(CodeGenerator/*!*/ codeGenerator, ConstructedType constructedType,
			DRoutine/*!*/ constructor, CallSignature callSignature, bool runtimeVisibilityCheck)
		{
			// TODO: delegate construction analysis and compile-time stub generation

			if (!runtimeVisibilityCheck && !TypeDesc.IsSubclassOf(DTypeDesc.DelegateTypeDesc))
			{
				// surely visible constructor //

				callSignature.EmitLoadOnPhpStack(codeGenerator);

				ILEmitter il = codeGenerator.IL;

				ResolveTypeFlags flags = ResolveTypeFlags.UseAutoload | ResolveTypeFlags.ThrowErrors;

				// [ Operators.NewClr(<type desc>, <context>) ]
				if (constructedType != null)
					constructedType.EmitLoadTypeDesc(codeGenerator, flags);
				else
					this.EmitLoadTypeDesc(codeGenerator, flags);

				codeGenerator.EmitLoadScriptContext();

				il.Emit(OpCodes.Call, Methods.Operators.NewClr);

#if DEBUG_DYNAMIC_CTOR_STUBS
				ClrMethod clr_ctor = (ClrMethod)constructor;

				MethodBuilder mb = codeGenerator.IL.TypeBuilder.DefineMethod(FullName + "::" + clr_ctor.FullName,
					MethodAttributes.PrivateScope | MethodAttributes.Static, typeof(object), Types.Object_PhpStack);

				IndexedPlace instance = new IndexedPlace(PlaceHolder.Argument, 0);
				IndexedPlace stack = new IndexedPlace(PlaceHolder.Argument, 1);

				clr_ctor.EmitArglessStub(new ILEmitter(mb), stack, instance);
#endif

			}
			else
			{
				// a possibly visible CLR constructor - perform visibility check at runtime
				// (and handle delegate instantiation)

				codeGenerator.EmitNewOperator(null, null, (constructedType != null) ? constructedType : (DType)this, callSignature);
			}

			return PhpTypeCode.Object;
		}

		internal override void EmitInstanceOf(CodeGenerator/*!*/ codeGenerator, ConstructedType constructedType)
		{
			codeGenerator.EmitDirectInstanceOf((constructedType != null) ? constructedType : (DType)this);
		}

		internal override void EmitTypeOf(CodeGenerator/*!*/ codeGenerator, ConstructedType constructedType)
		{
			codeGenerator.EmitDirectTypeOf((constructedType != null) ? constructedType : (DType)this);
		}

		internal override void EmitLoadTypeDesc(CodeGenerator/*!*/ codeGenerator, ResolveTypeFlags flags)
		{
			codeGenerator.EmitLoadTypeDesc(RealType);
		}

		internal override DTypeSpec GetTypeSpec(SourceUnit/*!*/ referringUnit)
		{
			return new DTypeSpec(this.RealType, referringUnit.CompilationUnit.ModuleBuilder.AssemblyBuilder.RealModuleBuilder);
		}

		internal override void EmitInvokeConstructor(ILEmitter/*!*/ il, PhpType/*!*/ derivedType, ConstructedType constructedType)
		{
			// invoke base constructor:
			ClrMethod ctor = this.ClrConstructor;

			IPlace context_place = new IndexedPlace(PlaceHolder.Argument, FunctionBuilder.ArgContextInstance);
			IPlace stack_place = new Place(context_place, Fields.ScriptContext_Stack);

			ClrOverloadBuilder overload_builder = new ClrOverloadBuilder(
				il,
				ctor,
				constructedType,
				stack_place,
				IndexedPlace.ThisArg,
				true,
				new ClrOverloadBuilder.ParameterLoader(PhpStackBuilder.EmitValuePeek),
				new ClrOverloadBuilder.ParameterLoader(PhpStackBuilder.EmitReferencePeek));

			overload_builder.EmitResolutionByTypes(derivedType.Builder.BaseCtorCallOverloadIndex);

			// check whether we added the <proxy> field and init it if so
			if (derivedType.ProxyFieldInfo.DeclaringType == derivedType.TypeDesc.RealType)
			{
				// [ this.<proxy> = ClrObject.Create(this) ]
				il.Ldarg(FunctionBuilder.ArgThis);

				il.Ldarg(FunctionBuilder.ArgThis);
				il.Emit(OpCodes.Call, Methods.ClrObject_Create);

				il.Emit(OpCodes.Stfld, derivedType.ProxyFieldInfo);
			}
		}

		#endregion
	}

	#endregion

	#region PhpType, PhpTypeBuilder

	/// <summary>
	/// Represents PHP type. For generic types, this instance represents the generic template
	/// all instantiations (represented by PhpTypeDesc) are sharing.
	/// </summary>
	[DebuggerDisplay("{DebuggerDisplay}")]
	public sealed class PhpType : KnownType, IDeclaree, IPhpMember
	{
		#region Properties

		public override bool IsUnknown { get { return false; } }
		public override bool IsGeneric { get { return GenericParams.Length > 0; } }
		public override bool IsOpen { get { return false; } }
		public override bool IsPhpType { get { return true; } }
		public override bool ClrVerified { get { return declaration == null; } }

		/// <summary>
		/// Whether the analyzer can be sure about the identity of the type.
		/// A type is error definite if it is a unconditionally declared type or if it is a reflected type.
		/// 
		/// Note that reflected types are resolved iff they are active at the time of the resolving. 
		/// A reflected type declared unconditionally in its compilation unit is active since the module is loaded 
		/// (the module's auto-reflection code declares it on <see cref="ApplicationContext"/>). 
		/// A reflected type declared conditionally is activated by a call to the <see cref="ScriptContext.DeclareType"/>.
		/// </summary>
		public override bool IsIdentityDefinite
		{
			get { return declaration == null || !declaration.IsConditional; }
		}

		/// <summary>
		/// Type can be either unconditional/load-time activated (version index is zero), 
		/// conditionally declared with known declaration (version index is positive) or 
		/// runtime activated by an already compiled code (version index is <see cref="VersionInfo.ReflectedRuntimeActivated"/>).
		/// </summary>
		public bool IsRuntimeActivated { get { return version.Index != VersionInfo.Unconditional; } }

		public PhpTypeDesc PhpTypeDesc { get { return (PhpTypeDesc)TypeDesc; } }
		public IPhpModuleBuilder DeclaringModuleBuilder { get { return (IPhpModuleBuilder)DeclaringModule; } }

		public Declaration Declaration { get { return declaration; } }
		private Declaration declaration;

		public GenericParameterDesc[]/*!!*/ GenericParams { get { return PhpTypeDesc.GenericParameters; } }

		public VersionInfo Version { get { return version; } set { version = value; } }
		private VersionInfo version;

		/// <summary>
		/// Auxiliary fields used for emission, <B>null</B> for reflected types.
		/// </summary>
		internal PhpTypeBuilder Builder { get { return builder; } }
		private PhpTypeBuilder builder;


		/// <summary>
		/// Gets constructor of this type (if any).
		/// Filled by member analysis.
		/// </summary>
		public override KnownRoutine Constructor { get { return constructor; } }
		private PhpRoutine constructor;

		/// <summary>
		/// Set in constructor for reflected types.
		/// Initialized on demand, not available before full analysis starts (the hierarchy needs to be validated first).
		/// </summary>
		public override bool IsComplete { get { Debug.Assert(inheritanceAnalysisState != DfsStates.Initial); return isComplete; } }
		private bool isComplete;

		/// <summary>
		/// Root in the inheritance hierarchy.
		/// Available after full analysis.
		/// </summary>
		public DType/*!A*/ Root { get { Debug.Assert(inheritanceAnalysisState != DfsStates.Initial); return root; } }
		private DType/*!A*/ root;

		/// <summary>
		/// <B>True</B> if there is a CLR type (or a constructed type with CLR generic type) in this type's base chain.
		/// Available after full analysis.
		/// </summary>
		public bool ExtendsClrType { get { Debug.Assert(inheritanceAnalysisState != DfsStates.Initial); return extendsClrType; } }
		private bool extendsClrType;

		/// <summary>
		/// Gets whether the type is exported.
		/// </summary>
		internal bool IsExported
		{
			get
			{
				return builder.ExportInfo != null || this.DeclaringModuleBuilder.AssemblyBuilder.IsExported;
			}
		}


		/// <summary>
		/// Returns <see cref="MethodInfo"/> of <see cref="PhpObjectBuilder.StaticFieldInitMethodName"/>.
		/// </summary>
		public MethodInfo StaticFieldInitMethodInfo { get { return staticFieldInitMethodInfo; } set { staticFieldInitMethodInfo = value; } }
		private MethodInfo staticFieldInitMethodInfo;
		public MethodBuilder StaticFieldInitMethodBuilder { get { return (MethodBuilder)staticFieldInitMethodInfo; } }

		/// <summary>
		/// Returns exported aka &quot;C#-friendly&quot; constructors defined in this PHP type.
		/// </summary>
		/// <remarks>
		/// Constructors are sorted according to their parameter count in the ascending order.
		/// </remarks>
		public ConstructorInfo[] ClrConstructorInfos
		{
			#region Getter
			get
			{
				// this is rarely needed -> obtain the constructor info lazily
				if (clrConstructorInfos == null && !IsInterface)
				{
					Debug.Assert(builder == null, "Should be called only on reflected types. Otherwise clrConstructorInfos is set by DefineBuilders");

					// reflect constructors
					ConstructorInfo[] ctors =
						this.RealType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

					List<ConstructorInfo> ctor_list = new List<ConstructorInfo>();
					for (int i = 0; i < ctors.Length; i++)
					{
						ParameterInfo[] parms = ctors[i].GetParameters();

						// On SL we don't have (de)serialization, so we don't check 
						// for the arguments matching serialization pattern
#if SILVERLIGHT
						bool isPublicSig =
							parms.Length != 2 || parms[0].ParameterType != Types.ScriptContext[0];
#else
						bool isPublicSig =
							parms.Length != 2 ||
								(parms[0].ParameterType != Types.ScriptContext[0] &&
								 (parms[0].ParameterType != Types.SerializationInfo_StreamingContext[0] ||
									parms[1].ParameterType != Types.SerializationInfo_StreamingContext[1]));
#endif
						if (isPublicSig)
						{
							int index = 0;
							while (index < ctor_list.Count && parms.Length > ctor_list[index].GetParameters().Length) index++;

							ctor_list.Insert(index, ctors[i]);
						}
					}

					clrConstructorInfos = ctor_list.ToArray();
				}
				return clrConstructorInfos;
			}
			#endregion
			internal set { clrConstructorInfos = value; }
		}
		private ConstructorInfo[] clrConstructorInfos;

		/// <summary>
		/// Returns <see cref="ConstructorInfo"/> to short constructor.
		/// </summary>
		public ConstructorInfo ShortConstructorInfo { get { return shortConstructorInfo; } }
		private ConstructorInfo shortConstructorInfo;
		public ConstructorBuilder ShortConstructorBuilder { get { return (ConstructorBuilder)shortConstructorInfo; } }

		/// <summary>
		/// Returns <see cref="ConstructorInfo"/> to long constructor.
		/// </summary>
		public ConstructorInfo LongConstructorInfo { get { return longConstructorInfo; } }
		private ConstructorInfo longConstructorInfo;
		public ConstructorBuilder LongConstructorBuilder { get { return (ConstructorBuilder)longConstructorInfo; } }

#if !SILVERLIGHT
		/// <summary>
		/// Returns <see cref="ConstructorInfo"/> to deserializing constructor.
		/// </summary>
		public ConstructorInfo DeserializingConstructorInfo { get { return deserializingConstructorInfo; } }
		private ConstructorInfo deserializingConstructorInfo;
		public ConstructorBuilder DeserializingConstructorBuilder { get { return (ConstructorBuilder)DeserializingConstructorInfo; } }
#endif

		/// <summary>
		/// Static readonly field emitted to each PHP type. Holds reference to the type's <see cref="PhpTypeDesc"/>.
		/// </summary>
		public FieldInfo TypeDescFieldInfo { get { return typeDescFieldInfo; } }
		private FieldInfo typeDescFieldInfo;

		/// <summary>
		/// Instance readonly field emitted to the first PHP type that extends a CLR base. Holds reference to the
		/// instance's <see cref="ClrObject"/>.
		/// </summary>
		public FieldInfo ProxyFieldInfo { get { return proxyFieldInfo; } }
		private FieldInfo proxyFieldInfo;

        /// <summary>
        /// Method that declares (compiles) incomplete class definition. Is <c>null</c> if the type is not incomplete.
        /// Method is in format <c>private static void &lt;Declare&gt;XXX(ScriptContext)</c>
        /// </summary>
        public MethodInfo IncompleteClassDeclareMethodInfo { get; internal set; }

        /// <summary>
        /// Unique identifier of the incomplete class declaration.
        /// Used in runtime (<see cref="ScriptContext.IncompleteTypesInAdvance"/>) to determine whether class was declared in advance.
        /// </summary>
        public string IncompleteClassDeclarationId { get; internal set; }

		#endregion

		#region Construction

		/// <summary>
		/// Used by full-reflect (<see cref="DTypeDesc.Reflect"/>).
		/// </summary>
		public PhpType(DTypeDesc/*!*/ typeDesc, QualifiedName qualifiedName)
			: base(typeDesc, qualifiedName)
		{
			this.isComplete = true;
			this.inheritanceAnalysisState = DfsStates.Done;

			GetInheritanceProperties(out this.extendsClrType, out this.root);
			ReflectRealType();
			DetermineConstructor();
		}

		/// <summary>
		/// To be used by the compiler.
		/// </summary>
		public PhpType(QualifiedName qualifiedName, PhpMemberAttributes memberAttributes, bool isPartial,
			TypeSignature typeSignature, bool isConditionalDeclaration, Scope scope, SourceUnit/*!*/ sourceUnit, Position position)
			: base(new PhpTypeDesc(sourceUnit.CompilationUnit.Module, memberAttributes), qualifiedName)
		{
			Debug.Assert(sourceUnit != null && position.IsValid);

			GenericParameterDesc[] generic_params = typeSignature.ToGenericParameters(this);
			TypeDesc.WriteUpGenericDefinition(generic_params.Length > 0 ? new GenericTypeDefinition(TypeDesc, generic_params) : null);

			this.declaration = new Declaration(sourceUnit, this, isPartial, isConditionalDeclaration, scope, position);
			this.builder = new PhpTypeBuilder(this);
			this.inheritanceAnalysisState = DfsStates.Initial;
		}

		#endregion

		#region Utils

		/// <summary>
		/// Determines whether a given type represents a PHP class or a PHP interface.
		/// </summary>
		/// <param name="realType">The <see cref="Type"/> to test.</param>
		/// <returns><B>true</B> if <paramref name="realType"/> implements a PHP class or a PHP interface, <B>false</B>
		/// otherwise.</returns>
		/// <exception cref="NullReferenceException"><paramref name="realType"/> is a <B>null</B> reference.</exception>
		public static bool IsPhpRealType(Type/*!*/ realType)
		{
			return realType.IsDefined(Types.ImplementsTypeAttribute, false);
		}

		/// <exception cref="NullReferenceException"><paramref name="realType"/> is a <B>null</B> reference.</exception>
		public static bool IsRealConditionalDefinition(Type/*!*/ realType)
		{
			return realType.Name.IndexOf('#') > 0;
		}

		internal override void ReportError(ErrorSink/*!*/ sink, ErrorInfo error)
		{
			if (declaration != null)
				sink.Add(error, declaration.SourceUnit, declaration.Position);
		}

		public void ReportRedeclaration(ErrorSink/*!*/ errors)
		{
			Debug.Assert(declaration != null);
			errors.Add(FatalErrors.TypeRedeclared, declaration.SourceUnit, declaration.Position, FullName);
		}


		#endregion

		#region Member Analysis, Validation

		private DfsStates inheritanceAnalysisState;
		private List<DType> dfsPredecessors;

		/// <summary>
		/// To be used by analyzer in member-analysis.
		/// </summary>
		/// <remarks>
		/// All types are known at this point (their inheritance relationships needn't to be known).
		/// This instance knows its base class and interfaces (the others needn't to).
		/// Members of this type are known, however members of the other types are not known yet.
		/// Partial declarations are merged at this point.
		/// </remarks>
		internal void ValidateMembers(ErrorSink/*!*/ errors)
		{
			if (inheritanceAnalysisState != DfsStates.Done)
			{
				AnalyzeInheritance(errors);
			}

			DetermineConstructor();

			// add implicit export attribute if the class represents a custom attribute:
			if (IsCustomAttributeType)
				builder.ExportInfo = new ExportAttribute();

			Debug.WriteLine("M-ANALYSIS", "PhpType '{0}': root = '{1}', extendsClrType = {2}",
				this.MakeFullGenericName(), (root != null) ? root.MakeFullGenericName() : "?", extendsClrType);
		}

		internal override void AnalyzeInheritance(ErrorSink/*!*/ errors, ref List<DType> mfpWorklist, DType dfsPredecessor, PhpType phpPredecessor,
			int dfsDepth, int inheritanceDepth)
		{
			Debug.Assert(errors != null);

			if (dfsDepth >= 0)
			{
				switch (inheritanceAnalysisState)
				{
					case DfsStates.Initial:

						inheritanceAnalysisState = DfsStates.Entered;

						// initialize:
						isComplete = true;
						builder.InheritanceAnalysisDfsDepth = inheritanceDepth;

						// inheritance edge descend (increase inheritance depth):
						if (TypeDesc.Base != null)
							TypeDesc.Base.Type.AnalyzeInheritance(errors, ref mfpWorklist, this, this, dfsDepth + 1, inheritanceDepth + 1);

						// inheritance edge descend (increase inheritance depth);
						// note, we cannot use TypeDesc.Interfaces as has not been defined yet:
						foreach (DTypeDesc iface in builder.BaseInterfaces)
							iface.Type.AnalyzeInheritance(errors, ref mfpWorklist, this, this, dfsDepth + 1, inheritanceDepth + 1);

						inheritanceAnalysisState = DfsStates.Done;

						UpdateInheritanceProperties();

						break;

					case DfsStates.Entered:
						if (mfpWorklist == null)
							mfpWorklist = new List<DType>();

						// edge from DFS predecessor to this node closes a loop => add predecessor to the worklist as 
						// its properties depends on the values that will be computed on this node later:
						mfpWorklist.Add(dfsPredecessor);

						// check whether the backward edge closes inheritance loop:
						Debug.Assert(this.builder != null, "Backward edge cannot point to a reflected type.");
						Debug.Assert(dfsDepth - 1 >= this.builder.InheritanceAnalysisDfsDepth);
						Debug.Assert(phpPredecessor != null, "There has to be some PhpType (at least this type)");

						if ((dfsDepth - 1) - this.builder.InheritanceAnalysisDfsDepth <= inheritanceDepth)
						{
							// fatal error - circular inheritance (the further analysis assumes non-circularity):
							errors.Add((this.IsInterface) ? FatalErrors.CircularBaseInterfaceDependency : FatalErrors.CircularBaseClassDependency,
								declaration.SourceUnit, declaration.Position, phpPredecessor.FullName, this.FullName);
							phpPredecessor.ReportError(errors, FatalErrors.RelatedLocation);

							throw new CompilerException();
						}

						break;

					case DfsStates.Done:
						// do nothing
						break;
				}

				// add predecessor regardless of the DFS state of the current node:
				if (dfsPredecessor != null)
				{
					if (dfsPredecessors == null)
						dfsPredecessors = new List<DType>();

					dfsPredecessors.Add(dfsPredecessor);
				}
			}
			else
			{
				Debug.Assert(mfpWorklist != null);

				// save current state of MFP properties:
				bool was_complete = this.isComplete;

				// recalculate the properties:
				UpdateInheritanceProperties();

				// check changes in MFP properties and queue the influenced nodes for MFP processing:
				if (this.isComplete != was_complete && dfsPredecessors != null)
					mfpWorklist.AddRange(dfsPredecessors);
			}
		}

		private void UpdateInheritanceProperties()
		{
			if (TypeDesc.Base != null)
			{
				Debug.Assert(!TypeDesc.Base.Type.IsGenericParameter, "cannot inherit from ganeric parameter (cted or not)");

				isComplete &= TypeDesc.Base.Type.IsDefinite;

				if (isComplete)
				{
					// the properties can be determined from the values on the base type;
					// if the type isn't complete, it doesn't make sense to get properties of the base class:
					Base.GetInheritanceProperties(out this.extendsClrType, out this.root);
				}
			}
			else
			{
				this.extendsClrType = false;
				this.root = this;
			}

			// note, we cannot use TypeDesc.Interfaces as has not been defined yet:
			foreach (DTypeDesc iface in builder.BaseInterfaces)
			{
				Debug.Assert(!iface.Type.IsGenericParameter, "cannot inherit from ganeric parameter (cted or not)");
				isComplete &= iface.Type.IsDefinite;
			}

			// determine implemented interfaces for complete types:
			if (isComplete)
				TypeDesc.WriteUpInterfaces(GetImplementedInterfaces());
		}

		internal override void GetInheritanceProperties(out bool extendsClrType, out DType/*!*/ hierarchyRoot)
		{
			// we must check for null builder to prevent running into infinite recursion if there is a circular inheritance:
			if (root == null && builder == null)
			{
				if (Base != null)
				{
					Base.GetInheritanceProperties(out this.extendsClrType, out this.root);
				}
				else
				{
					this.extendsClrType = false;
					this.root = this;
				}
			}

			// called from AnalyseInheritance on already analyzed base type:
			extendsClrType = this.extendsClrType;
			hierarchyRoot = this.root;
		}

		/// <summary>
		/// Gathers all implemented interfaces including those inherited from base types.
		/// Filters duplicates so that implements/ghost arrays won't contain duplicates when populated.
		/// </summary>
		private DTypeDesc[]/*!!*/ GetImplementedInterfaces()
		{
			Debug.Assert(isComplete);

			DTypeDesc[] result;
			DTypeDesc base_type = TypeDesc.Base;

			if (builder.BaseInterfaces.Count > 0)
			{
				// base class and interfaces should have their interfaces tables populated:

				// we expect small numbers of interfaces so the list should be sufficient:
				List<DTypeDesc> all_interfaces = new List<DTypeDesc>();

				if (base_type != null)
				{
					foreach (DTypeDesc iface in base_type.Interfaces)
					{
						if (all_interfaces.IndexOf(iface) == -1)
							all_interfaces.Add(iface);
					}
				}

				foreach (DTypeDesc base_iface in builder.BaseInterfaces)
				{
					if (all_interfaces.IndexOf(base_iface) == -1)
					{
						foreach (DTypeDesc iface in base_iface.Interfaces)
						{
							if (all_interfaces.IndexOf(iface) == -1)
								all_interfaces.Add(iface);
						}

						all_interfaces.Add(base_iface);
					}
				}

				result = all_interfaces.ToArray();
			}
			else if (base_type != null)
			{
				// this type implements exactly the same set of interfaces as the base one:
				result = base_type.Interfaces;
			}
			else
			{
				result = DTypeDesc.EmptyArray;
			}

			Debug.WriteLine("M-ANALYSIS", "PhpType '{0}': interfaces = {{{1}}}", this.FullName,
				ArrayUtils.ToList(result, delegate(StringBuilder sb, DTypeDesc iface)
				{
					sb.Append(iface.Type.MakeFullGenericName());
				}));

			return result;
		}

		/// <summary>
		/// Looks the constructor up among the methods of the type. Used by analysis and reflection.
		/// </summary>
		private void DetermineConstructor()
		{
			DRoutineDesc ctor_desc;
			if (TypeDesc.Methods.TryGetValue(DObject.SpecialMethodNames.Construct, out ctor_desc) ||
				TypeDesc.Methods.TryGetValue(this.qualifiedName.Name, out ctor_desc))
			{
				ctor_desc.MemberAttributes |= PhpMemberAttributes.Constructor;
				this.constructor = ctor_desc.PhpRoutine;
			}
		}

		#endregion

		#region Analysis, Validation

		internal override DType/*!*/ MakeConstructedType(Analyzer/*!*/ analyzer, DTypeDesc[]/*!*/ arguments, Position position)
		{
			// only error definites know their generic parameters:
			if (!IsIdentityDefinite)
			{
				if (arguments.Length > 0)
					return analyzer.CreateConstructedType(this.TypeDesc, arguments, arguments.Length);
				else
					return this;
			}

			// check too many args:
			if (arguments.Length > GenericParams.Length)
			{
				if (GenericParams.Length == 0)
				{
					analyzer.ErrorSink.Add(Errors.NonGenericTypeUsedWithTypeArgs, analyzer.SourceUnit, position,
						  this.FullName);
				}
				else
				{
					analyzer.ErrorSink.Add(Errors.TooManyTypeArgumentsInTypeUse, analyzer.SourceUnit, position,
						this.FullName, GenericParams.Length);
				}
			}

			if (GenericParams.Length > 0)
			{
				int arg_count = arguments.Length;

				if (!TypeDesc.MakeGenericArguments(ref arguments, ref arg_count,
					delegate(DTypeDesc.MakeGenericArgumentsResult/*!*/ error, DTypeDesc/*!*/ genericType, DTypeDesc argument, GenericParameterDesc/*!*/ parameter)
					{
						Debug.Assert(error == DTypeDesc.MakeGenericArgumentsResult.MissingArgument);

						analyzer.ErrorSink.Add(Errors.MissingTypeArgumentInTypeUse, analyzer.SourceUnit, position,
							genericType.Type.FullName, parameter.GenericParameter.Index + 1, parameter.GenericParameter.FullName);
					}
				))
				{
					// missing mandatory arg:
					return this;
				}

				// create a constructed type from the resolved type and arguments:
				return analyzer.CreateConstructedType(this.TypeDesc, arguments, arg_count);
			}
			else
			{
				return this;
			}
		}

		/// <summary>
		/// Expects all declared members to be validated.
		/// </summary>
		internal void Validate(ErrorSink/*!*/ errors)
		{
			ResolveAbstractOverrides(errors);

			Debug.Assert(allAbstracts != null);

			// fills the overrides array for methods:
			foreach (PhpMethod method in GetMethods<PhpMethod>())
			{
				Debug.Assert(method.Overrides == null || method.Overrides.Member.IsAbstract, "Resolved by ResolveAbstractOverrides");

				if (method.Overrides == null)
				{
					if (method.IsConstructor && this.Base != null && this.Base.IsClrType)
					{
						// check the parent for the .ctor if it is a CLR type:
						method.Overrides = (this.Base.Constructor != null) ? new DMemberRef(this.Base.Constructor, this.Base) : null;
					}
					else
					{
						bool in_supertype = true;
						method.Overrides = method.GetImplementationInSuperTypes(this.Base, true, ref in_supertype);
					}

					if (method.Overrides != null)
					{
						Debug.WriteLine("F-ANALYSIS", "GetUserEntryPoint '{0}::{1}': overrides = '{2}::{3}'",
							this.MakeFullGenericName(), method.FullName, method.Overrides.Type.MakeFullGenericName(), method.Overrides.Member.FullName);
					}
				}

                if (method.Overrides != null)
                {
                    method.ValidateOverride(errors, (KnownRoutine)method.Overrides.Member);

                    //// decreasing amount of arguments:
                    //// We have to add missing args as hidden args, so we can optimize method calls.
                    //// In case args could be decreased, we would loose args when calling virtually;
                    //// class A{function f($a)}
                    //// class B{function f()}    // <-- loosing $a, we have to add hidden arg, so override in C would be correct
                    //// class C{function f($a)}  // <-- $a is ArgDefault
                    //if (method.Overrides.Member.GetType() == typeof(PhpMethod) &&
                    //    ((PhpMethod)method.Overrides.Member).Signature.ParamCount > method.Signature.ParamCount)
                    //{
                    //    // Signature has to be extended with hidden args
                    //    // ... then we can enable this->foo optimization
                    //    // last problem is changing return type (object <-> PhpReference)
                    //}
                }

				if (method.Implements != null)
				{
					foreach (DMemberRef implemented in method.Implements)
						method.ValidateOverride(errors, (KnownRoutine)implemented.Member);
				}
			}

			// fills the overrides array for fields:
			foreach (PhpField field in GetProperties<PhpField>())
			{
				Debug.Assert(field.Overrides == null || field.Overrides.Member.IsAbstract, "Resolved by ResolveAbstractOverrides");

				if (field.Overrides == null)
				{
					bool in_supertype = true;
					field.Overrides = field.GetImplementationInSuperTypes(this.Base, true, ref in_supertype);
				}

				if (field.Overrides != null)
					field.ValidateOverride(errors, (KnownProperty)field.Overrides.Member);

				if (field.Implements != null)
				{
					foreach (DMemberRef implemented in field.Implements)
						field.ValidateOverride(errors, (KnownProperty)implemented.Member);
				}
			}

            // check class constants
            foreach (var constant in TypeDesc.Constants.Values)
            {
                if (constant.ClassConstant != null && !constant.ClassConstant.HasValue)
                {
                    // this constant will behave like PHP static field,
                    // it needs to be initialized every request statically:
                    this.Builder.HasThreadStaticFields = true;
                }
            }

			// abstracts must be implemented in non-abstract class:
			if (!IsAbstract)
			{
				foreach (DMemberRef unimplemented in allAbstracts)
					unimplemented.ReportAbstractNotImplemented(errors, this);
			}

			// check missing ctor in CLR subclass without default ctor:
			if (constructor == null)
			{
				ClrType base_clr_type = this.Base as ClrType;
				if (base_clr_type != null && !base_clr_type.ClrConstructor.HasParameterlessOverload)
				{
					errors.Add(Errors.MissingCtorInClrSubclass, declaration.SourceUnit, declaration.Position,
						this.FullName);
				}
			}

			Debug.WriteLine("F-ANALYSIS", "PhpType '{0}': ghosts = {{{1}}}", this.FullName,
				ArrayUtils.ToList(this.Builder.ghostImplementations,
					delegate(StringBuilder sb, KeyValuePair<DMemberRef, DMemberRef> m)
					{
						sb.AppendFormat("{0}::{1} by {2}::{3}",
							m.Key.Type.MakeFullGenericName(), m.Key.Member.FullName,
							m.Value.Type.MakeFullGenericName(), m.Value.Member.FullName);
					}));
		}

		internal override void AddGhostImplementation(DMemberRef/*!*/ abstractMember, DMemberRef/*!*/ implementation)
		{
			this.builder.AddGhostImplementation(abstractMember, implementation);
		}

		#endregion

		#region Member Addition

		internal ClassConstant AddConstant(VariableName name, PhpMemberAttributes memberAttributes,
			Position position, SourceUnit/*!*/ sourceUnit, ErrorSink/*!*/ errors)
		{
			DConstantDesc existing;

			// name uniqueness:
			if (TypeDesc.Constants.TryGetValue(name, out existing))
			{
				errors.Add(Errors.ConstantRedeclared, sourceUnit, position, QualifiedName.ToString(new Name(name.Value), false));
				errors.Add(Errors.RelatedLocation, sourceUnit, existing.ClassConstant.Position);
				return null;
			}

			ClassConstant constant = new ClassConstant(name, TypeDesc, memberAttributes, sourceUnit, position);

			constant.Validate(errors);

			TypeDesc.Constants.Add(name, constant.ConstantDesc);

			return constant;
		}

		/// <summary>
		/// Adds a field to the type.
		/// </summary>
		/// <returns>Whether the field has been added.</returns>
		internal PhpField AddField(VariableName name, PhpMemberAttributes memberAttributes, bool hasInitialValue,
			Position position, SourceUnit/*!*/ sourceUnit, ErrorSink/*!*/ errors)
		{
			DPropertyDesc existing;

			// name uniqueness:
			if (TypeDesc.Properties.TryGetValue(name, out existing))
			{
				errors.Add(Errors.PropertyRedeclared, sourceUnit, position, QualifiedName, name);
				errors.Add(Errors.RelatedLocation, sourceUnit, existing.PhpField.Position);
				return null;
			}

			PhpField field = new PhpField(name, TypeDesc, memberAttributes, hasInitialValue, sourceUnit, position);

			field.Validate(sourceUnit, errors);

			TypeDesc.Properties.Add(name, field.PropertyDesc);
			if (field.PropertyDesc.IsThreadStatic) builder.HasThreadStaticFields = true;

			return field;
		}

		internal PhpMethod AddMethod(Name name, PhpMemberAttributes memberAttributes, bool hasBody,
		  Signature astSignature, TypeSignature astTypeSignature,
		  Position position, SourceUnit/*!*/ sourceUnit, ErrorSink/*!*/ errors)
		{
			DRoutineDesc existing;

			// name uniqueness:
			if (TypeDesc.Methods.TryGetValue(name, out existing))
			{
				errors.Add(Errors.MethodRedeclared, sourceUnit, position, QualifiedName, name);
				errors.Add(Errors.RelatedLocation, sourceUnit, existing.PhpMethod.Position);
				return null;
			}

			if (this.IsInterface)
				memberAttributes |= PhpMemberAttributes.Abstract;

			PhpMethod method = new PhpMethod(this, name, memberAttributes, hasBody, astSignature, astTypeSignature,
			  sourceUnit, position);

			TypeDesc.Methods.Add(name, method.RoutineDesc);

			return method;
		}

		#endregion

		#region Member & Type Parameter Lookup

		public override KnownRoutine GetConstructor()
		{
			// Do allow incomplete types here - they won't be emitted but just for sake of consistent error reporting.
			// Debug.Assert(IsComplete);

			KnownRoutine result = this.Constructor;
			if (result == null && Base != null) return Base.GetConstructor();
			return result;
		}

		#endregion

		#region Reflection

		private void ReflectRealType()
		{
			Type type = this.RealType;

			// the type has been declared conditionally when compiled:
			if (IsRealConditionalDefinition(RealType))
			{
				this.version.Index = VersionInfo.ReflectedRuntimeActivated;
			}

			// <typeDesc> readonly static field
			typeDescFieldInfo = type.GetField(
				PhpObjectBuilder.TypeDescFieldName,
				BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

			if (!IsInterface)
			{
				if (!(Root is PhpType))
				{
					// <proxy> readonly instance field
					proxyFieldInfo = type.GetField(
						PhpObjectBuilder.ProxyFieldName,
						BindingFlags.NonPublic | BindingFlags.Instance);
				}

				// clrConstructor is reflected lazily

				staticFieldInitMethodInfo = type.GetMethod(
					PhpObjectBuilder.StaticFieldInitMethodName,
					BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly,
					null,
					Types.ScriptContext,
					null);

				shortConstructorInfo = type.GetConstructor(
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
					null,
					PhpObjectBuilder.ShortConstructorParamTypes,
					null);

				longConstructorInfo = type.GetConstructor(
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
					null,
					PhpObjectBuilder.LongConstructorParamTypes,
					null);

#if !SILVERLIGHT
				if (!ExtendsClrType)
				{
					deserializingConstructorInfo = type.GetConstructor(
						BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly,
						null,
						PhpObjectBuilder.DeserializingConstructorParamTypes,
						null);
				}
#endif
			}

			// perform full reflect of fields and methods
			PhpTypeDesc.FullReflect();
		}

		#endregion

		#region Emission: DefineBuilders, Bake

		public override MethodInfo/*!*/ DefineRealMethod(string/*!*/ name, MethodAttributes attributes,
		  Type/*!*/ returnType, Type[]/*!*/ parameterTypes)
		{
			return RealTypeBuilder.DefineMethod(name, attributes, returnType, parameterTypes);
		}

		internal override void DefineBuilders()
		{
			// skip types with builders already defined, incomplete types, 
			// types with unreachable declaration and types inside incomplete class (won't be emitted)
			if (RealType == null && IsComplete && !declaration.IsUnreachable && !declaration.IsInsideIncompleteClass)
			{
				// type builder (define base type and interfaces later to prevent loops in cases like A<T> extends/implements B<A>):
				TypeBuilder type_builder = this.DeclaringModuleBuilder.DefineRealType(
					qualifiedName.ToClrNotation(GenericParams.Length, version.Index),
					Enums.ToTypeAttributes(TypeDesc.MemberAttributes));

				type_builder.SetCustomAttribute(AttributeBuilders.ImplementsType);
				Enums.DefineCustomAttributes(TypeDesc.MemberAttributes, type_builder);

				// generic parameters:
				GenericTypeParameterBuilder[] generic_param_builders = null;
				if (GenericParams.Length > 0)
				{
					string[] generic_param_names = new string[GenericParams.Length];

					for (int i = 0; i < GenericParams.Length; i++)
						generic_param_names[i] = GetGenericParameter(i).Name.Value;

					generic_param_builders = type_builder.DefineGenericParameters(generic_param_names);

					for (int i = 0; i < GenericParams.Length; i++)
						GetGenericParameter(i).DefineBuildersWithinType(generic_param_builders[i]);
				}

				// set the builder to the type-desc so that the base types can refer to it in their DefineBuilders routine:
				this.TypeDesc.DefineBuilder(type_builder);

				// base type:
				if (Base != null)
				{
					Base.DefineBuilders();
					Debug.Assert(Base.RealType != null);
					type_builder.SetParent(Base.RealType);
				}
				else if (!IsInterface)
				{
					type_builder.SetParent(typeof(PhpObject));
				}

				// implemented interfaces:
				for (int i = 0; i < builder.BaseInterfaces.Count; i++)
				{
					DTypeDesc iface = builder.BaseInterfaces[i];
					iface.Type.DefineBuilders();
					Debug.Assert(iface.RealType != null);
					type_builder.AddInterfaceImplementation(iface.RealType);
				}

                // implements IPhpDestructable (can be implemented in every extending class) if destructor can be called anywhere
                DRoutineDesc destructor;
                if ((destructor = this.TypeDesc.GetMethod(DObject.SpecialMethodNames.Destruct)) != null && destructor.IsPublic && !destructor.IsStatic)
                    type_builder.AddInterfaceImplementation(typeof(IPhpDestructable));
                
				// define fully open type:
				if (generic_param_builders != null)
					builder.RealOpenType = type_builder.MakeGenericType(generic_param_builders);
				else
					builder.RealOpenType = type_builder;

				// declared members:
				foreach (DRoutineDesc method_desc in this.TypeDesc.Methods.Values)
					method_desc.PhpMethod.DefineBuilders();

				foreach (DPropertyDesc property_desc in this.TypeDesc.Properties.Values)
					property_desc.PhpField.DefineBuilders();

				foreach (DConstantDesc constant_desc in this.TypeDesc.Constants.Values)
					constant_desc.ClassConstant.DefineBuilders();

				// helpers:

				// <typeDesc> readonly static field
				FieldBuilder fb = type_builder.DefineField(PhpObjectBuilder.TypeDescFieldName,
					Types.PhpTypeDesc[0], FieldAttributes.Public | FieldAttributes.InitOnly | FieldAttributes.Static);
#if !SILVERLIGHT
				fb.SetCustomAttribute(AttributeBuilders.EditorBrowsableNever);
#endif

				typeDescFieldInfo = fb;

				// check whether we need the <proxy> field
				if (!(Root is PhpType))
				{
					DType type = Base;
					while (type != null)
					{
						PhpType php_type = type as PhpType;
						if (php_type != null)
						{
							// <proxy> field declared by a base
							proxyFieldInfo = php_type.proxyFieldInfo;
							break;
						}

						type = type.Base;
					}

					if (proxyFieldInfo == null)
					{
						// <proxy> readonly instance field
						fb = type_builder.DefineField(PhpObjectBuilder.ProxyFieldName, typeof(ClrObject),
							FieldAttributes.Family | FieldAttributes.InitOnly);
#if !SILVERLIGHT
						fb.SetCustomAttribute(AttributeBuilders.EditorBrowsableNever);
#endif
						proxyFieldInfo = fb;
					}
				}

				// static constructor - will contain <typeDesc> and class constant initialization
				builder.StaticCtorEmitter = new ILEmitter(type_builder.DefineTypeInitializer());

				if (!IsInterface)
				{
					// method builder for static fields initialization method
					staticFieldInitMethodInfo = PhpObjectBuilder.DefineStaticFieldInitMethod(type_builder);

					// constructor builders
					shortConstructorInfo = type_builder.DefineConstructor(
						PhpObjectBuilder.ShortConstructorAttributes, CallingConventions.Standard,
						PhpObjectBuilder.ShortConstructorParamTypes);

					longConstructorInfo = type_builder.DefineConstructor(
						PhpObjectBuilder.LongConstructorAttributes, CallingConventions.Standard,
						PhpObjectBuilder.LongConstructorParamTypes);

#if !SILVERLIGHT
					ShortConstructorBuilder.SetCustomAttribute(AttributeBuilders.EditorBrowsableNever);
					LongConstructorBuilder.SetCustomAttribute(AttributeBuilders.EditorBrowsableNever);

					if (!ExtendsClrType)
					{
						// do not add the deserializing constructor if we are extending a CLR type
						deserializingConstructorInfo = type_builder.DefineConstructor(
							PhpObjectBuilder.DeserializingConstructorAttributes, CallingConventions.Standard,
							PhpObjectBuilder.DeserializingConstructorParamTypes);
					}
#endif

					if (IsExported)
						PhpObjectBuilder.DefineExportedConstructors(this);

					PhpObjectBuilder.EmitInitFieldHelpers(this);
				}

				this.EmitTypeDescInitialization();
                this.EmitSetStaticInit();
			}

			// define builders for the other versions:
			if (version.Next != null)
				((PhpType)version.Next).DefineBuilders();
		}

		private void EmitTypeDescInitialization()
		{
			ILEmitter il = builder.StaticCtorEmitter;

			il.Emit(OpCodes.Ldtoken, builder.RealOpenType);
			il.Emit(OpCodes.Call, Methods.PhpTypeDesc_Create);
			il.Emit(OpCodes.Stsfld, this.typeDescFieldInfo);
		}

        private void EmitSetStaticInit()
        {
            // thread static field init
            if (this.Builder.HasThreadStaticFields)
            {
                ILEmitter il = builder.StaticCtorEmitter;

                // [ typeDesc.SetStaticInit(new Action<ScriptContext>(__tsinit)) ]

                il.Emit(OpCodes.Ldsfld, this.typeDescFieldInfo);
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Ldftn, this.StaticFieldInitMethodInfo);
                il.Emit(OpCodes.Newobj, Constructors.Action_ScriptContext);

                il.Emit(OpCodes.Call, Methods.SetStaticInit);
            }
        }
        
		/// <summary>
		/// Returns a plain type-desc corresponding to the baked real type or a <B>null</B> reference if the type 
		/// cannot be baked due to its indefiniteness.
		/// </summary>
		internal PhpTypeDesc Bake()
		{
			// skip when we don't have a builder (already baked):
			if (builder != null && builder.BakedTypeDesc != null)
				return builder.BakedTypeDesc;

			// bake the other versions:
			if (version.Next != null)
				((PhpType)version.Next).Bake();

			// incomplete types are converted to eval, the type may however be conditionally declared:
			if (!IsComplete)
				return null;

			if (!(RealType is TypeBuilder))
				return this.PhpTypeDesc;

			// bake base type:
			if (Base != null)
			{
				PhpType php_base = Base as PhpType;
				if (php_base != null)
					php_base.Bake();
			}

			// bake implemented interfaces:
			for (int i = 0; i < builder.BaseInterfaces.Count; i++)
			{
				PhpType php_iface = builder.BaseInterfaces[i].Type as PhpType;
				if (php_iface != null)
					php_iface.Bake();
			}

			if (!IsInterface)
				PhpObjectBuilder.EmitClassConstructors(this);

			// finish static constructor:
			builder.StaticCtorEmitter.Emit(OpCodes.Ret);

			if (!IsInterface)
			{
				// finish <InitializeInstanceFields> and __InitializeStaticFields methods
				builder.InstanceFieldInitEmitter.Emit(OpCodes.Ret);
				StaticFieldInitMethodBuilder.GetILGenerator().Emit(OpCodes.Ret);
			}

			// generate type desc population (methods, fields, constants)
			PhpObjectBuilder.GenerateTypeDescPopulation(this);

			Type baked_type = this.RealTypeBuilder.CreateType();

			// set the baked type to the current type desc; although the next line replaces the current 
			// type desc with a new one, we need to keep the old one consistent -- the other types being baked 
			// can still access the old type desc: 
			TypeDesc.Bake(baked_type);

			// Initiate re-reflection.
			// PhpTypeDesc is thrown away and a plain new is added - a subsequent access reflects it.
			// Note that every type referring this type in its tables (via PhpType or PhpTypeDesc)
			// have to be baked as well, so there are no references that needs to be rewritten.
			builder.BakedTypeDesc = (PhpTypeDesc)DTypeDesc.Recreate(baked_type.TypeHandle, true);
			
			return builder.BakedTypeDesc;
		}

		#endregion

		#region Emission: Operations

		internal override PhpTypeCode EmitNew(CodeGenerator/*!*/ codeGenerator, ConstructedType constructedType,
		  DRoutine/*!*/ constructor, CallSignature callSignature, bool runtimeVisibilityCheck)
		{
			ILEmitter il = codeGenerator.IL;

			if (IsDefinite)
			{
				// s-decl type //

				if (constructor.IsUnknown)
				{
					// no (visible) PHP constructor //

					// just instantiate the class:
					codeGenerator.EmitLoadScriptContext();
					il.LdcI4(1);
					il.Emit(OpCodes.Newobj, MakeConstructed(ShortConstructorInfo, constructedType));
				}
				else if (!runtimeVisibilityCheck)
				{
					// surely visible constructor //
					PhpMethod php_ctor = constructor as PhpMethod;

					if (ExtendsClrType)
					{
						// prepare the stack before newobjing the instance

						callSignature.EmitLoadOnPhpStack(codeGenerator);

						// newobj
						codeGenerator.EmitLoadScriptContext();
						il.LdcI4(1);
						il.Emit(OpCodes.Newobj, MakeConstructed(ShortConstructorInfo, constructedType));

						if (php_ctor != null)
						{
							// call the PHP ctor
							il.Emit(OpCodes.Dup);
							codeGenerator.EmitLoadScriptContext();

							il.Emit(OpCodes.Ldfld, Fields.ScriptContext_Stack);
							il.Emit(OpCodes.Call, MakeConstructed(php_ctor.ArgLessInfo, constructedType));
							il.Emit(OpCodes.Pop);
						}
						else
						{
							// just remove the frame from stack
							codeGenerator.EmitLoadScriptContext();

							il.Emit(OpCodes.Ldfld, Fields.ScriptContext_Stack);
							il.Emit(OpCodes.Call, Methods.PhpStack.RemoveFrame);
						}
					}
					else
					{
						// no CLR constructors in the way

						// newobj
						codeGenerator.EmitLoadScriptContext();
						il.LdcI4(1);
						il.Emit(OpCodes.Newobj, MakeConstructed(ShortConstructorInfo, constructedType));
                        if (php_ctor.ArgFullInfo != null && !php_ctor.IsArgsAware)
                        {
                            // invoke the arg-full version of the constructor:
                            il.Emit(OpCodes.Dup);
                            codeGenerator.EmitLoadScriptContext();
                            callSignature.EmitLoadOnEvalStack(codeGenerator, php_ctor);
                            il.Emit(OpCodes.Call, php_ctor.ArgFullInfo);
                            il.Emit(OpCodes.Pop);
                        }
                        else
						{
							// invoke the arg-less version of the constructor:
							il.Emit(OpCodes.Dup);

							callSignature.EmitLoadOnPhpStack(codeGenerator);
							codeGenerator.EmitLoadScriptContext();

							il.Emit(OpCodes.Ldfld, Fields.ScriptContext_Stack);
							il.Emit(OpCodes.Call, MakeConstructed(php_ctor.ArgLessInfo, constructedType));
							il.Emit(OpCodes.Pop);
						}
					}
				}
				else
				{
					// a possibly visible PHP constructor //

					callSignature.EmitLoadOnPhpStack(codeGenerator);

					// invoke the (ScriptContext, DTypeDesc) constructor that will resolve PHP
					// constructor and the visibility at runtime
					codeGenerator.EmitLoadScriptContext();
					codeGenerator.EmitLoadClassContext();
					il.Emit(OpCodes.Newobj, MakeConstructed(LongConstructorInfo, constructedType));
				}

				// remember that we have just initialized class_entry's static fields
				if (!il.IsFeatureControlFlowPrecedent(this)) il.MarkFeature(this);

				if (!(Root is PhpType))
				{
					// the newly created real object must be wrapped
					// TODO: optimize by introducing an artificial IRealObject interface
					il.Emit(OpCodes.Call, Methods.ClrObject_Wrap);
				}
			}
			else
			{
				codeGenerator.EmitNewOperator(null, null, (constructedType != null) ? constructedType : (DType)this, callSignature);
			}

			return PhpTypeCode.Object;
		}

		internal override void EmitInstanceOf(CodeGenerator/*!*/ codeGenerator, ConstructedType constructedType)
		{
			if (IsDefinite)
			{
				codeGenerator.EmitDirectInstanceOf((constructedType != null) ? constructedType : (DType)this);
			}
			else
			{
				codeGenerator.EmitInstanceOfOperator(null, null, (constructedType != null) ? constructedType : (DType)this);
			}
		}

		internal override void EmitTypeOf(CodeGenerator/*!*/ codeGenerator, ConstructedType constructedType)
		{
			if (IsDefinite)
			{
				codeGenerator.EmitDirectTypeOf((constructedType != null) ? constructedType : (DType)this);
			}
			else
			{
				codeGenerator.EmitTypeOfOperator(null, null, (constructedType != null) ? constructedType : (DType)this);
			}
		}

		internal override void EmitLoadTypeDesc(CodeGenerator/*!*/ codeGenerator, ResolveTypeFlags flags)
		{
			if (IsDefinite)
			{
				if (IsGeneric || typeDescFieldInfo == null)
				{
					codeGenerator.EmitLoadTypeDesc(this.RealType);
				}
				else
				{
					codeGenerator.IL.Emit(OpCodes.Ldsfld, typeDescFieldInfo);
				}
			}
			else
			{
				codeGenerator.EmitLoadTypeDescOperator(this.FullName, null, flags);
			}
		}

		internal override void EmitInvokeConstructor(ILEmitter/*!*/ il, PhpType/*!*/ derivedType, ConstructedType constructedType)
		{
            if (ShortConstructorInfo != null)
            {
                // [ base(arg1,arg2) ]
                il.Ldarg(FunctionBuilder.ArgThis);
                il.Ldarg(FunctionBuilder.ArgContextInstance);
                il.Ldarg(2);

                il.Emit(OpCodes.Call, MakeConstructed(ShortConstructorInfo, constructedType));
            }
		}

#if !SILVERLIGHT
		internal override void EmitInvokeDeserializationConstructor(ILEmitter/*!*/ il, PhpType/*!*/ derivedType,
			ConstructedType constructedType)
		{
            if (DeserializingConstructorInfo != null)
            {
                // [ base(arg0, arg1, arg2) ]
                il.Ldarg(FunctionBuilder.ArgThis);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);

                il.Emit(OpCodes.Call, MakeConstructed(DeserializingConstructorInfo, constructedType));
            }
		}
#endif

		/// <summary>
		/// Emits call that declares this type to <see cref="ApplicationContext"/>.
		/// </summary>
		internal void EmitAutoDeclareOnApplicationContext(ILEmitter/*!*/ il, IPlace/*!*/ contextPlace)
		{
			contextPlace.EmitLoad(il);

			if (IsGeneric)
			{
				// generic types registeres the template:
				il.Emit(OpCodes.Ldtoken, RealType);
				il.Emit(OpCodes.Ldstr, FullName);
				il.Emit(OpCodes.Call, Methods.ApplicationContext.DeclareType_Handle);
			}
			else
			{
				// non-generic types can use their <typeDesc> field:
				il.Emit(OpCodes.Ldsfld, TypeDescFieldInfo);
				il.Emit(OpCodes.Ldstr, FullName);
				il.Emit(OpCodes.Call, Methods.ApplicationContext.DeclareType_TypeDesc);
			}
		}

		/// <summary>
		/// Emits call that declares this type to <see cref="ScriptContext"/>.
		/// </summary>
		internal void EmitAutoDeclareOnScriptContext(ILEmitter/*!*/ il, IPlace/*!*/ contextPlace)
		{
            Debug.Assert(this.IsComplete);

			contextPlace.EmitLoad(il);

			if (IsGeneric)
			{
				// generic types registeres the template:
				il.Emit(OpCodes.Ldtoken, RealType);
				il.Emit(OpCodes.Ldstr, FullName);
				il.Emit(OpCodes.Call, Methods.ScriptContext.DeclareType_Handle);
			}
			else
			{
				// non-generic types can use their <typeDesc> field:
				il.Emit(OpCodes.Ldsfld, TypeDescFieldInfo);
				il.Emit(OpCodes.Ldstr, FullName);
				il.Emit(OpCodes.Call, Methods.ScriptContext.DeclareType_TypeDesc);
			}
		}

        internal void EmitDeclareIncompleteOnScriptContext(ILEmitter/*!*/ il, IPlace/*!*/ contextPlace)
        {
            Debug.Assert(!string.IsNullOrEmpty(this.IncompleteClassDeclarationId));
            Debug.Assert(this.IncompleteClassDeclareMethodInfo != null);

            if (!this.Declaration.IsConditional &&
                this.Base != null && this.Builder.BaseInterfaces.Count == 0)    // for now, only types without interfaces
            {
                // if (<context>.DeclareIncompleteTypeHelper(<uid>, type.Base))
                //     CALL <type.IncompleteClassDeclareMethodInfo>(<context>)

                var end_if = il.DefineLabel();

                contextPlace.EmitLoad(il);
                il.Emit(OpCodes.Ldstr, this.IncompleteClassDeclarationId);
                il.Emit(OpCodes.Ldstr, this.Base.FullName);
                il.Emit(OpCodes.Call, Methods.ScriptContext.DeclareIncompleteTypeHelper);
                il.Emit(OpCodes.Brfalse, end_if);
                if (true)
                {
                    contextPlace.EmitLoad(il);
                    il.Emit(OpCodes.Call, this.IncompleteClassDeclareMethodInfo);
                }
                il.MarkLabel(end_if);
                il.ForgetLabel(end_if);
            }                        
        }

        internal override DTypeSpec GetTypeSpec(SourceUnit/*!*/ referringUnit)
		{
			if (IsDefinite)
			{
				return new DTypeSpec(RealType, referringUnit.CompilationUnit.ModuleBuilder.AssemblyBuilder.RealModuleBuilder);
			}
			else
			{
				return new DTypeSpec(FullName, referringUnit.NamingContextFieldBuilder,
					referringUnit.CompilationUnit.ModuleBuilder.AssemblyBuilder.RealModuleBuilder);
			}
		}

        internal void EmitThreadStaticInit(CodeGenerator/*!*/ codeGenerator, ConstructedType constructedType)
        {
            ILEmitter il = codeGenerator.IL;

            DType feature = ((DType)constructedType ?? (DType)this);

            // ensure that the field has been initialized for this request by invoking __InitializeStaticFields
            if (!il.IsFeatureControlFlowPrecedent(feature))
            {
                codeGenerator.EmitLoadScriptContext();
                il.Emit(OpCodes.Call, DType.MakeConstructed(this.StaticFieldInitMethodInfo, constructedType));

                // remember that we have just initialized class_entry's static fields
                il.MarkFeature(feature);
            }
        }

		#endregion

		#region Custom Attributes

		private AttributeUsageAttribute _customAttributeUsage = null;
		private bool _customAttributeUsageSet = false;

		/// <summary>
		/// Gets attribute usage attribute applied on the type (if any).
		/// </summary>
		public override AttributeUsageAttribute GetCustomAttributeUsage(out bool isDefinite)
		{
			if (!_customAttributeUsageSet && RealType != null)
			{
				object[] attrs = this.RealType.GetCustomAttributes(typeof(AttributeUsageAttribute), true);
				if (attrs.Length == 1) _customAttributeUsage = (AttributeUsageAttribute)attrs[0];
				_customAttributeUsageSet = true;
			}

			isDefinite = _customAttributeUsageSet;
			return _customAttributeUsage;
		}

		/// <summary>
		/// Called by the analyzer when it determines the <see cref="AttributeUsageAttribute"/> is (not) defined on the type.
		/// </summary>
		internal void SetCustomAttributeUsage(AttributeUsageAttribute customAttributeUsage)
		{
			_customAttributeUsage = customAttributeUsage;
			_customAttributeUsageSet = true;
		}

		#endregion

		#region Debug

		internal string DebuggerDisplay
		{
			get
			{
				return MakeFullGenericName() + ((declaration != null) ? " " + declaration.Position.ToString() : "");
			}
		}

		#endregion
	}

	internal sealed class PhpTypeBuilder
	{
		public PhpType/*!*/ Type { get { return type; } }
		private readonly PhpType/*!*/ type;

		internal ExportAttribute ExportInfo
		{
			get { return exportInfo; }
			set /* TypeDecl */ { exportInfo = value; }
		}
		private ExportAttribute exportInfo;


		// filled by analysis:
		public CallSignature BaseCtorCallSignature;
		public int BaseCtorCallOverloadIndex;

		/// <summary>
		/// Interfaces implemented directly by this type.
		/// Set by Pre-Analysis of the TypeDecl.
		/// </summary>
		internal List<DTypeDesc> BaseInterfaces { get { return baseInterfaces; } set { baseInterfaces = value; } }
		private List<DTypeDesc> baseInterfaces;

		/// <summary>
		/// Used by <c>InheritanceAnalysis</c>.
		/// </summary>
		internal int InheritanceAnalysisDfsDepth;

		/// <summary>
		/// List of "ghost implementations" mapping abstract member (key) to its implementation (value).
		/// Ghost implementations are stubs that are required to be added to the class
		/// in order to tackle CLR inability to bind abstract method/property defined in interface I with
		/// its implementation defined in a superclass B of the class A implementing the interface.
		/// (class B { f() {} }, interface I { f() { } }, class A extends B implements I { }). 
		/// </summary>
		internal List<KeyValuePair<DMemberRef, DMemberRef>> GhostImplementations { get { return ghostImplementations; } }
		internal /* protected */ List<KeyValuePair<DMemberRef, DMemberRef>> ghostImplementations = null;

		internal void AddGhostImplementation(DMemberRef/*!*/ abstractMember, DMemberRef/*!*/ implementation)
		{
			if (ghostImplementations == null)
				ghostImplementations = new List<KeyValuePair<DMemberRef, DMemberRef>>();

			ghostImplementations.Add(new KeyValuePair<DMemberRef, DMemberRef>(abstractMember, implementation));
		}

		/// <summary>
		/// <B>True</B> if the type contains at least one thread-static field.
		/// </summary>
		public bool HasThreadStaticFields;


		// filled by DefineBuilders
		public ILEmitter StaticCtorEmitter;

		public MethodBuilder InstanceFieldInit;
		public ILEmitter InstanceFieldInitEmitter;

		public List<StubInfo> ClrConstructorStubs;

		// filled by Bake:
		public PhpTypeDesc BakedTypeDesc { get { return bakedTypeDesc; } set { bakedTypeDesc = value; } }
		private PhpTypeDesc bakedTypeDesc;

		/// <summary>
		/// Fully open constructed real type (type arguments are filled with the type parameters).
		/// </summary>
		public Type RealOpenType
		{
			get { return realOpenType; }
			set /* PhpType.DefineBuilders */ { realOpenType = value; }
		}
		private Type realOpenType;

		public PhpTypeBuilder(PhpType/*!*/ type)
		{
			Debug.Assert(type != null);
			this.type = type;
		}
	}

	#endregion
}
