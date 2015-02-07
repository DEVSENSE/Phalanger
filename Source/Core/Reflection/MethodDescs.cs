/*

 Copyright (c) 2006 Tomas Matousek and Ladislav Prosek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/
#if !SILVERLIGHT
//#define DEBUG_METHOD_STUBS
#endif

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Reflection.Emit;
using PHP.Core.Emit;

namespace PHP.Core.Reflection
{
	#region DRoutineDesc

	[DebuggerNonUserCode]
	public abstract class DRoutineDesc : DMemberDesc
	{
		public DRoutine Routine { get { return (DRoutine)Member; } }
		public KnownRoutine KnownRoutine { get { return (KnownRoutine)Member; } }
		public PhpRoutine PhpRoutine { get { return (PhpRoutine)Member; } }
		public PhpMethod PhpMethod { get { return (PhpMethod)Member; } }
		public PhpFunction PhpFunction { get { return (PhpFunction)Member; } }
		public PhpLibraryFunction PhpLibraryFunction { get { return (PhpLibraryFunction)Member; } }
		public ClrMethod ClrMethod { get { return (ClrMethod)Member; } }

		public bool IsConstructor { get { return (memberAttributes & PhpMemberAttributes.Constructor) != 0; } }

		protected bool AllowProtectedCall(DTypeDesc/*!*/ caller) 
		{ 
			return (!IsProtected || DeclaringType.IsRelatedTo(caller));
		}

		protected internal RoutineDelegate ArglessStub
		{
			get
			{
                return _arglessStub ?? (_arglessStub = GenerateArglessStub());
			}
			// used by ClrMethod for defining CLR method stubs
			// used by PhpRoutine for baking dynamic methods
			internal set
			{
				_arglessStub = value;
			}
		}
		private RoutineDelegate _arglessStub = null;

        /// <summary>
        /// <see cref="MethodInfo"/> to be called thru .call IL OpCode.
        /// </summary>
        /// <remarks>
        /// By default, it is the Method of <see cref="ArglessStub"/> delegate. In case of emitted <see cref="DynamicMethod"/>,
        /// we need to remember the original <see cref="DynamicMethod"/> so it can be called within DLR.
        /// </remarks>
        internal virtual MethodInfo ArglessStubMethod { get { return this.ArglessStub.Method; } }

        /// <summary>
        /// Internal index used for call cache.
        /// </summary>
        internal int Index = -1;
        internal static int LastIndex = -1;

		#region Construction

		/// <summary>
		/// Used by compiler through subclasses (<paramref name="arglessStub"/> is <B>null</B> then).
		/// Called by a declaring helper at run-time.
		/// </summary>
        /// <param name="declaringType">The declaring type. Can be null.</param>
        /// <param name="memberAttributes">Attributes of the function.</param>
        /// <param name="arglessStub">The stub to be called. Cannot be null.</param>
        /// <param name="needsIndex">True to allocate <see cref="Index"/>. Usable for preserved descs, for global functions that can be reused.</param>
		internal DRoutineDesc(DTypeDesc/*!*/ declaringType, PhpMemberAttributes memberAttributes, RoutineDelegate arglessStub, bool needsIndex)
			: base(declaringType, memberAttributes)
		{
			Debug.Assert(declaringType != null);
			this._arglessStub = arglessStub;

            // allocate an index, only for preserved descs
            if (needsIndex) // assign an index only if needed (save indexes, since they cause prealocation of larger BitArray)
                this.Index = System.Threading.Interlocked.Increment(ref LastIndex);
		}

		#endregion

		#region Utils

		internal static StringBuilder/*!*/ GetFullName(MethodInfo/*!*/ realMethod, StringBuilder/*!*/ result)
		{
			Debug.Assert(realMethod != null && result != null);

			int name_start = GetNameStartIndex(realMethod.Name);
			int hash = realMethod.Name.IndexOf('#', name_start);
			int name_end = (hash > 0) ? hash : realMethod.Name.Length;

			result.Append(realMethod.Name.Substring(name_start, name_end - name_start).Replace('.', QualifiedName.Separator));

			return result;
		}

		internal static StringBuilder/*!*/ GetFullGenericName(MethodInfo/*!*/ realMethod, StringBuilder/*!*/ result)
		{
			Debug.Assert(realMethod != null && result != null);

			GetFullName(realMethod, result);

			if (!realMethod.IsGenericMethod)
				return result;

			ConstructedTypeDesc.GenericArgumentsToString(realMethod.GetGenericArguments(), result);

			return result;
		}

		private static int GetNameStartIndex(string/*!*/ name)
		{
			Debug.Assert(name != null);

			// format: <hidden>.namespace.name.#n
			if (name.Length > 0 && name[0] == '<')
			{
				int closing = name.IndexOf('>');
				return (closing != -1 && closing < name.Length - 1) ? closing + 2 : 0;
			}

			return 0;
		}

		internal static string/*!*/ GetSpecialName(MethodBase/*!*/ realMethod)
		{
			Debug.Assert(realMethod != null);

			int name_start = GetNameStartIndex(realMethod.Name);
			return realMethod.Name.Substring(name_start);
		}

		#endregion

		protected abstract RoutineDelegate GenerateArglessStub();

		#region Run-time Operations

		[Emitted]
		public object Invoke(DObject instance, PhpStack stack)
		{
			return ArglessStub(instance == null ? null : instance.InstanceObject, stack);
		}

		public object Invoke(DObject instance, PhpStack stack, DTypeDesc caller)
		{
			stack.AllowProtectedCall = AllowProtectedCall(caller);
			return Invoke(instance, stack);
		}

		#endregion
	}

	#endregion

	#region PhpRoutineDesc

	[DebuggerNonUserCode]
	public sealed class PhpRoutineDesc : DRoutineDesc
	{
		public bool IsFunction { get { return declaringType.IsGlobal; } }
		public bool IsMethod { get { return !declaringType.IsGlobal; } }

		#region Construction

		/// <summary>
		/// Used by compiler for functions.
		/// </summary>
		internal PhpRoutineDesc(DModule/*!*/ declaringModule, PhpMemberAttributes memberAttributes)
			: base(declaringModule.GlobalType.TypeDesc, memberAttributes, null, false)
		{
			Debug.Assert(declaringModule != null);
			Debug.Assert((memberAttributes & PhpMemberAttributes.Static) != 0);
		}

		/// <summary>
		/// Used by compiler for methods.
		/// </summary>
		internal PhpRoutineDesc(DTypeDesc/*!*/ declaringType, PhpMemberAttributes memberAttributes)
			: base(declaringType, memberAttributes, null, false)
		{
			Debug.Assert(declaringType != null);
		}

		/// <summary>
		/// Creates a descriptor for specified PHP method at run-time if argless stub delegate is available.
		/// Called by declaring helper emitted on PHP types.
		/// </summary>
		internal PhpRoutineDesc(DTypeDesc/*!*/ declaringType, PhpMemberAttributes memberAttributes, RoutineDelegate/*!*/ arglessStub)
			: base(declaringType, memberAttributes, arglessStub, false)
		{
			Debug.Assert(arglessStub != null);
		}

		/// <summary>
		/// Creates a descriptor for specified PHP function at run-time if argless stub delegate is available.
		/// Called by declaring helpers emitted on script or when a callback is created.
		/// </summary>
        public PhpRoutineDesc(PhpMemberAttributes memberAttributes, RoutineDelegate/*!*/ arglessStub, bool needsIndex)
            : base(UnknownModule.RuntimeModule.GlobalType.TypeDesc, memberAttributes, arglessStub, needsIndex)
		{
			Debug.Assert(arglessStub != null);
		}

		#endregion

		protected override RoutineDelegate GenerateArglessStub()
		{
			return (RoutineDelegate)Delegate.CreateDelegate(typeof(RoutineDelegate), this.PhpRoutine.ArgLessInfo);
		}

		public override string MakeFullName()
		{
            if (fullName != null)
                return fullName;

			if (Member != null)
                return fullName = Member.FullName;

			if (IsMethod)
                return fullName = ArglessStub.Method.Name;

            return fullName = GetFullName(ArglessStub.Method, new StringBuilder()).ToString();
		}
        private string fullName;

		public override string MakeFullGenericName()
		{
			// TODO:
			//if (Member != null) 
			//  return Member.FullName;

			return GetFullGenericName(ArglessStub.Method, new StringBuilder()).ToString();
		}

	}

	#endregion

	#region PhpLibraryRoutineDesc

	public sealed class PhpLibraryFunctionDesc : DRoutineDesc
	{
		/// <summary>
		/// Used by both fast and full reflectors.
		/// </summary>
		internal PhpLibraryFunctionDesc(PhpLibraryModule/*!*/ declaringModule, RoutineDelegate/*!*/ arglessStub)
			: base(declaringModule.GlobalType.TypeDesc, PhpMemberAttributes.Public | PhpMemberAttributes.Static, arglessStub, true)
		{
			Debug.Assert(declaringModule != null && arglessStub != null);
		}

		protected override RoutineDelegate GenerateArglessStub()
		{
			Debug.Fail("delegate already created");
			throw null;
		}

		public override string MakeFullName()
		{
			return ArglessStub.Method.Name;
		}

		public override string MakeFullGenericName()
		{
			// library functions cannot have generic arguments
			return MakeFullName();
		}
	}

	#endregion

	#region ClrMethodDesc

	/// <summary>
	/// Represents a non-generic CLR method.
	/// </summary>
	[DebuggerNonUserCode]
	public class ClrMethodDesc : DRoutineDesc
	{
		#region Construction

		/// <summary>
		/// Used by compiler and full-reflect.
		/// </summary>
		public ClrMethodDesc(DTypeDesc/*!*/ declaringType, PhpMemberAttributes memberAttributes)
			: base(declaringType, memberAttributes, null, false)
		{
			Debug.Assert(declaringType != null);
		}

		#endregion

		public override string MakeFullName()
		{
			Debug.Assert(Member != null);
			return Member.FullName;
		}

		public override string MakeFullGenericName()
		{
			Debug.Assert(Member != null);

			// TODO: generic arguments
			return Member.FullName;
		}
        
        protected override RoutineDelegate GenerateArglessStub()
		{
			ClrMethod clr_method = ClrMethod;
			Debug.Assert(clr_method != null, "CLR method should be fully reflected");

#if DEBUG_METHOD_STUBS
			AssemblyName name = new AssemblyName("MethodStub_" + clr_method.ToString().Replace(':','_'));
			AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Save, "C:\\Temp");
			ModuleBuilder mb = ab.DefineDynamicModule(name.Name, name.Name + ".dll");
			TypeBuilder tb = mb.DefineType(clr_method.Name.ToString());
			MethodBuilder mmb = tb.DefineMethod("<^MethodStub>", PhpFunctionUtils.DynamicStubAttributes,
				CallingConventions.Standard, Types.Object[0], Types.Object_PhpStack);

			ILEmitter tmpil = new ILEmitter(mmb);

			IndexedPlace tmpinstance = new IndexedPlace(PlaceHolder.Argument, 0);
			IndexedPlace tmpstack = new IndexedPlace(PlaceHolder.Argument, 1);

			clr_method.EmitArglessStub(tmpil, tmpstack, tmpinstance);

			tb.CreateType();
			ab.Save(name.Name + ".dll");
#endif

#if SILVERLIGHT
			DynamicMethod stub = new DynamicMethod("<^>." + clr_method.Name.Value, Types.Object[0], Types.Object_PhpStack);
#else
			DynamicMethod stub = new DynamicMethod("<^>." + clr_method.Name.Value, PhpFunctionUtils.DynamicStubAttributes,
				CallingConventions.Standard, Types.Object[0], Types.Object_PhpStack, this.declaringType.RealType, true);
#endif
            ILEmitter il = new ILEmitter(stub);

			IndexedPlace instance = new IndexedPlace(PlaceHolder.Argument, 0);
			IndexedPlace stack = new IndexedPlace(PlaceHolder.Argument, 1);

			clr_method.EmitArglessStub(il, stack, instance);

			// TODO: is it possible to drop the member?
			// the compiler may get activated:
			// member = null; 

            this.arglessStubMethod = stub;
            return (RoutineDelegate)stub.CreateDelegate(typeof(RoutineDelegate));
		}

        private MethodInfo arglessStubMethod = null;
        internal override MethodInfo ArglessStubMethod
        {
            get
            {
                var argless = this.ArglessStub; // ensure argless is generated
                return arglessStubMethod;
            }
        }

		// TODO: caller == null when IsProtected == true ?

		//protected override bool AllowProtectedCall(DTypeDesc/*!*/ caller) 
		//{ 
		//	return DeclaringType.IsRelatedTo(caller);
		//}
	}

	/// <summary>
	/// Represents a generic CLR method.
	/// </summary>
	[DebuggerNonUserCode]
	public sealed class ClrGenericMethodDesc : ClrMethodDesc
	{
		private Dictionary<DTypeDescs, ClrMethodDesc>/*!*/ instantiations = new Dictionary<DTypeDescs, ClrMethodDesc>();

		#region Construction

		/// <summary>
		/// Used by full-reflect.
		/// </summary>
		public ClrGenericMethodDesc(DTypeDesc/*!*/ declaringType, PhpMemberAttributes memberAttributes)
			: base(declaringType, memberAttributes)
		{
			Debug.Assert(declaringType != null);
		}

		#endregion

        internal override MethodInfo ArglessStubMethod { get { return this.ArglessStub.Method; } }  // ArglessPreStub MethodInfo

		protected override RoutineDelegate GenerateArglessStub()
		{
			// always make the delegate point to ArglessPreStub
			return new RoutineDelegate(ArglessPreStub);
		}

		/// <summary>
		/// Dispatches the invocation to a constructed method according to type arguments on the stack.
		/// </summary>
		/// <remarks>
		/// Constructed methods are cached in the <see cref="instantiations"/> dictionary.
		/// </remarks>
		private object ArglessPreStub(object instance, PhpStack/*!*/ stack)
		{
			// get type arguments from the stack
			DTypeDescs type_args = new DTypeDescs(stack);

			ClrMethodDesc method_desc;
			lock (instantiations)
			{
				if (!instantiations.TryGetValue(type_args, out method_desc))
				{
					method_desc = ConstructMethodDesc(type_args);
					if (method_desc == null) return null;

					instantiations.Add(type_args, method_desc);
				}
			}

			return method_desc.ArglessStub(instance, stack);
		}

		/// <summary>
		/// Creates a constructed method desc for the given type arguments.
		/// </summary>
		private ClrMethodDesc ConstructMethodDesc(DTypeDescs/*!*/ typeArgs)
		{
			ClrMethod generic_method = ClrMethod;
			ClrMethod constructed_method = new ClrMethod(generic_method.Name, declaringType, memberAttributes, 1, false);

			// add overloads which have an exact match
			AddCompatibleOverloads(constructed_method, generic_method, typeArgs, true);

			if (constructed_method.Overloads.Count == 0)
			{
				// now try overloads whose additional type parameters can by substituted by System.Object
				AddCompatibleOverloads(constructed_method, generic_method, typeArgs, false);

				if (constructed_method.Overloads.Count == 0)
				{
					// sorry, bad luck
					PhpException.NoSuitableOverload(generic_method.DeclaringType.FullName, generic_method.FullName);
					return null;
				}
			}

			return constructed_method.ClrMethodDesc;
		}

		private void AddCompatibleOverloads(ClrMethod/*!*/ constructedMethod, ClrMethod/*!*/ genericMethod,
			DTypeDescs/*!*/ typeArgs, bool exactMatch)
		{
			foreach (ClrMethod.Overload overload in genericMethod.Overloads)
			{
				// add the overloads that are compatible with typeArgs
				int gen_count = overload.GenericParamCount;
				if (exactMatch ? gen_count == typeArgs.Count : gen_count > typeArgs.Count)
				{
					bool compatible = true;

					for (int i = 0; i < gen_count; i++)
					{
						DTypeDesc desc = (i < typeArgs.Count ? typeArgs[i] : PrimitiveTypeDesc.SystemObjectTypeDesc);
						if (!desc.IsCompatibleWithGenericParameter(overload.GenericParameters[i]))
						{
							compatible = false;
							break;
						}
					}

					if (compatible)
					{
						// make generic method
						Type[] real_type_args;
						if (exactMatch) real_type_args = typeArgs.GetRealTypes();
						else
						{
							real_type_args = new Type[gen_count];

							typeArgs.GetRealTypes(real_type_args, 0);
							for (int i = typeArgs.Count; i < gen_count; i++) real_type_args[i] = Types.Object[0];
						}
						MethodInfo info = ((MethodInfo)overload.Method).MakeGenericMethod(real_type_args);

						ClrMethod.Overload constructed_overload;
						constructedMethod.AddOverload(info, out constructed_overload);
					}
				}
			}
		}
	}

	#endregion
}
