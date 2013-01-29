/*

 Copyright (c) 2006 Tomas Matousek and Ladislav Prosek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/


//#define DEBUG_DYNAMIC_STUBS

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection.Emit;
using System.Collections;
using System.Reflection;
using System.Diagnostics;

using PHP.Core;
using PHP.Core.AST;
using PHP.Core.Emit;
using PHP.Core.Parsers;


namespace PHP.Core.Reflection
{
	#region RoutineProperties

	/// <summary>
	/// Properties of a PHP user function.
	/// </summary>
	[Flags]
	public enum RoutineProperties : int
	{
		/// <summary>
		/// No significant properties.
		/// </summary>
		None = 0,

		/// <summary>
		/// A function contains ${expr}() construct.
		/// </summary>
		ContainsIndirectFcnCall = 1,

		/// <summary>
		/// A function contains eval or assert.
		/// </summary>
		ContainsEval = 2,

		/// <summary>
		/// A function contains include (either dynamic or static).
		/// </summary>
		ContainsInclude = 4,

		/// <summary>
		/// A function contains ${expr}.
		/// </summary>
		IndirectLocalAccess = 8,

		/// <summary>
		/// A function contains call to a class library method with <see cref="FunctionImplOptions.NeedsVariables"/> option.
		/// </summary>
		ContainsLocalsWorker = 16,

		/// <summary>
		/// A function contains call to system function which manipulates arguments on PHP stack.
		/// (e.g. func_get_arg, func_get_args...)
		/// </summary>
		UseVarArgs = 32,

        /// <summary>
        /// A function contains late static binding call (use of <c>static</c> keyword referring to current runtime type).
        /// </summary>
        LateStaticBinding = 64,

		/// <summary>
		/// A function uses arguments from <see cref="PhpStack"/>.
		/// </summary>
        IsArgsAware = ContainsIndirectFcnCall | ContainsEval | ContainsInclude | UseVarArgs,

		/// <summary>
		/// A function local variable accesses can be optimized.
		/// </summary>
		HasUnoptimizedLocals = ContainsEval | ContainsInclude | ContainsIndirectFcnCall | ContainsLocalsWorker,

		/// <summary>
		/// A function contains a table of run-time variables.
		/// </summary>
		HasRTVariablesTable = HasUnoptimizedLocals | IndirectLocalAccess
	}

	#endregion

	#region RoutineSignature

	public abstract class RoutineSignature
	{
		public abstract bool IsUnknown { get; }

		public abstract DType GetTypeHint(int index);

		public abstract bool IsAlias(int index);
		public abstract bool AliasReturn { get; }

		public abstract int MandatoryParamCount { get; }
		public abstract int ParamCount { get; }
		public abstract int MandatoryGenericParamCount { get; }
		public abstract int GenericParamCount { get; }

        #region Utils

        /// <summary>
        /// Determines whether this signature can override given signature.
        /// Parameters count, type hints and names must match.
        /// </summary>
        /// <param name="sig"></param>
        /// <returns></returns>
        internal virtual bool CanOverride(RoutineSignature sig)
        {
            // additional parameters must have default value
            // all arguments in base method with default value has the default value in overriding (this) method
                
            return (sig != null && sig.AliasReturn == this.AliasReturn && sig.ParamCount <= this.ParamCount && sig.MandatoryParamCount == this.MandatoryParamCount);
        }

        #endregion
    }

	#endregion

	#region DRoutine, KnownRoutine

	[DebuggerNonUserCode]
	public abstract class DRoutine : DMember
	{
		public const int InvalidOverloadIndex = -1;

		public sealed override bool IsDefinite { get { return IsIdentityDefinite; } }

		public abstract bool IsLambda { get; }
		public abstract bool IsConstructor { get; }

		/// <summary>
		/// Whether the deep-copy-testing code (if applicable) is emitted by <see cref="EmitCall"/> method and thus
		/// the AST node emitter needn't to care any more.
		/// </summary>
		/// <remarks>
		/// It is better to make test on need for a deep-copy in the caller than in the callee
		/// since the callee doesn't know whether the copying is really necessary.
		/// </remarks>
		public abstract bool ReturnValueDeepCopyEmitted { get; }

		#region Construction

		/// <summary>
		/// Used by subclasses when creating known routines.
		/// </summary>
		public DRoutine(DMemberDesc/*!*/ memberDesc)
			: base(memberDesc)
		{
		}

		/// <summary>
		/// Used by subclasses when creating unknown routines.
		/// </summary>
		public DRoutine(string/*!*/ fullName)
			: base(null, fullName)
		{
			Debug.Assert(IsUnknown);
		}

		#endregion

		#region Utils

		public abstract RoutineSignature/*!*/ GetSignature(int overloadIndex);

		/// <summary>
		/// Gets <see cref="RoutineProperties"/> that each PHP caller of this routine is required to satisfy.
		/// </summary>
		public virtual RoutineProperties GetCallerRequirements()
		{
			return RoutineProperties.None;
		}

		internal override void ReportAbstractNotImplemented(ErrorSink/*!*/ errors, DType/*!*/ declaringType, PhpType/*!*/ referringType)
		{
			errors.Add(Errors.AbstractMethodNotImplemented, referringType.Declaration.SourceUnit,
				referringType.Declaration.Position,
                referringType.FullName, declaringType.MakeFullGenericName(), this.FullName);

			//ReportError(errors, Errors.RelatedLocation);
		}

        internal override void ReportMethodNotCompatible(ErrorSink errors, DType declaringType, PhpType referringType)
        {
            errors.Add(Errors.MethodNotCompatible, referringType.Declaration.SourceUnit,
                referringType.Declaration.Position,
                referringType.FullName, this.FullName, declaringType.MakeFullGenericName(), this.FullName);

            //ReportError(errors, Errors.RelatedLocation);
        }

		#endregion

        /// <summary>
        /// Emits the call of DRoutine.
        /// </summary>
        /// <param name="codeGenerator">Used code generator.</param>
        /// <param name="fallbackQualifiedName">Fallback function name to call, if the origin one does not exist.</param>
        /// <param name="callSignature">Call signature.</param>
        /// <param name="instance">IPlace containing instance of object in case of non static method call.</param>
        /// <param name="runtimeVisibilityCheck">True to check visibility during runtime.</param>
        /// <param name="overloadIndex">The index of overload (used in case of PhpLibraryFunction).</param>
        /// <param name="type">Type used to resolve this routine.</param>
        /// <param name="position">Position of the call expression.</param>
        /// <param name="access">Access type of the routine call. Used to determine wheter the caller does not need return value. In such case additional operations (like CastToFalse) should not be emitted.</param>
        /// <param name="callVirt">True to call the instance method virtually, using <c>.callvirt</c> instruction. This is used when current routine is non-static routine called on instance, not statically.</param>
        /// <returns>PhpTypeCode of the resulting value that is on the top of the evaluation stack after the DRoutine call. Value types are not boxed.</returns>
        internal abstract PhpTypeCode EmitCall(CodeGenerator/*!*/ codeGenerator, string fallbackQualifiedName, CallSignature callSignature,
            IPlace instance, bool runtimeVisibilityCheck, int overloadIndex, DType type, Position position,
            AccessType access, bool callVirt);

		/// <summary>
		/// Finds most suitable overload. Returns <see cref="InvalidOverloadIndex"/> and 
		/// <see cref="UnknownSignature.Default"/> in <c>overloadSignature</c> if no suitable overload exists.
		/// </summary>
		internal abstract int ResolveOverload(Analyzer/*!*/ analyzer, CallSignature callSignature, Position position,
			out RoutineSignature overloadSignature);

	}

	//[DebuggerNonUserCode]
	public abstract class KnownRoutine : DRoutine
	{
		public static readonly KnownRoutine[]/*!*/ EmptyArray = new KnownRoutine[0];

		public override bool IsUnknown { get { return false; } }
		public override bool IsConstructor { get { return (memberDesc.MemberAttributes & PhpMemberAttributes.Constructor) != 0; } }

		public DRoutineDesc/*!*/ RoutineDesc { get { return (DRoutineDesc)memberDesc; } }

		/// <summary>
		/// Simple name of the routine.
		/// </summary>
		public abstract Name Name { get; }

		/// <summary>
		/// Compiled functions/methods: Set during analysis.
		/// </summary>
		public RoutineProperties Properties { get { return properties; } set { properties = value; } }
		protected RoutineProperties properties;

		public MethodInfo ArgLessInfo { get { return argless; } }
		protected MethodInfo argless;

		public MethodInfo ArgFullInfo { get { return argfull; } }
		protected MethodInfo argfull;

        /// <summary>
        /// Whether the routine contains use of late static binding.
        /// </summary>
        public bool UsesLateStaticBinding { get { return (properties & RoutineProperties.LateStaticBinding) != 0; } }

        /// <summary>
        /// Whether the routine should be called via argless stub. (Needs PhpStack).
        /// </summary>
        public bool IsArgsAware { get { return (properties & RoutineProperties.IsArgsAware) != 0 || (IsStatic && UsesLateStaticBinding); } }

		#region Construction

		/// <summary>
		/// Used by the compiler and the reflector.
		/// </summary>
		public KnownRoutine(DRoutineDesc/*!*/ routineDesc)
			: base(routineDesc)
		{
		}

		#endregion

		#region Analysis

		internal override DMemberRef GetImplementationInSuperTypes(DType/*!*/ type, bool searchSupertypes, ref bool inSupertype)
		{
			if (type == null || type.IsUnknown)
				return null;

			do
			{
				KnownRoutine result = type.GetDeclaredMethod<KnownRoutine>(Name);
				if (result != null)
				{
					// private members are not visible from subtype:
					if (result.IsPrivate && inSupertype) break;

					return new DMemberRef(result, type);
				}

				inSupertype = true;
				type = type.Base;
			}
			while (type != null && !type.IsUnknown && searchSupertypes);

			inSupertype = false;
			return null;
		}

		#endregion
	}

	#endregion

	#region UnknownFunction, UnknownMethod, UnknownSignature

	public sealed class UnknownFunction : DRoutine
	{
		public override bool IsUnknown { get { return true; } }
		public override bool IsLambda { get { return false; } }
		public override bool ReturnValueDeepCopyEmitted { get { return true; } }
		public override bool IsIdentityDefinite { get { return false; } }

		public override bool IsConstructor { get { return false; } }

		#region Construction

		public UnknownFunction(string/*!*/ fullName)
			: base(fullName)
		{
		}

		#endregion

		public override RoutineSignature GetSignature(int overloadIndex)
		{
			return UnknownSignature.Default;
		}

		public override string GetFullName()
		{
			Debug.Fail("full name is set by ctor");
			throw null;
		}

		internal override int ResolveOverload(Analyzer/*!*/ analyzer, CallSignature callSignature, Position position,
			out RoutineSignature overloadSignature)
		{
			overloadSignature = UnknownSignature.Default;
			return 0;
		}

        internal override PhpTypeCode EmitCall(CodeGenerator/*!*/ codeGenerator, string fallbackQualifiedName, CallSignature callSignature,
            IPlace instance, bool runtimeVisibilityCheck, int overloadIndex, DType type, Position position,
            AccessType access, bool callVirt)
		{
			return codeGenerator.EmitRoutineOperatorCall(null, null, FullName, fallbackQualifiedName, null, callSignature, access);
		}
	}

	public sealed class UnknownMethod : DRoutine
	{
		private const string CtorName = "";

		public override bool IsUnknown { get { return true; } }
		public override bool IsLambda { get { return false; } }
		public override bool ReturnValueDeepCopyEmitted { get { return true; } }
		public override bool IsIdentityDefinite { get { return false; } }

		public override bool IsConstructor { get { return FullName == CtorName; } }

		public override DType/*!*/ DeclaringType { get { return declaringType; } }
		private readonly DType/*!*/ declaringType;

		#region Construction

		/// <summary>
		/// Used by the compiler for unresolved methods.
		/// </summary>
		public UnknownMethod(DType/*!*/ declaringType, string/*!*/ name)
			: base(name)
		{
			Debug.Assert(declaringType != null && name != null);

			this.declaringType = declaringType;
		}

		/// <summary>
		/// Used by the compiler for unresolved ctors.
		/// </summary>
		public UnknownMethod(DType/*!*/ declaringType)
			: base(CtorName)
		{
			Debug.Assert(declaringType != null);

			this.declaringType = declaringType;
		}

		#endregion

		public override RoutineSignature/*!*/ GetSignature(int overloadIndex)
		{
			return UnknownSignature.Default;
		}

		public override string GetFullName()
		{
			Debug.Fail("full name set in ctor");
			throw null;
		}

		internal override int ResolveOverload(Analyzer/*!*/ analyzer, CallSignature callSignature, Position position,
			out RoutineSignature overloadSignature)
		{
			// no ctor defined => default is to be used => should have no parameters;
			// do not report errors if the declaring type is open type (constructed or a generic parameter);
			if (declaringType.IsDefinite && IsConstructor && declaringType.IsClosed && callSignature.Parameters.Count > 0)
			{
				analyzer.ErrorSink.Add(Warnings.NoCtorDefined, analyzer.SourceUnit, position, declaringType.FullName);
				declaringType.ReportError(analyzer.ErrorSink, Warnings.RelatedLocation);
			}

			overloadSignature = UnknownSignature.Default;
			return 0;
		}

        internal override PhpTypeCode EmitCall(CodeGenerator/*!*/ codeGenerator, string fallbackQualifiedName, CallSignature callSignature,
            IPlace instance, bool runtimeVisibilityCheck, int overloadIndex, DType type, Position position,
            AccessType access, bool callVirt)
		{
            Debug.Assert(instance == null || instance is ExpressionPlace);
            Debug.Assert(fallbackQualifiedName == null);

            return codeGenerator.EmitRoutineOperatorCall(declaringType, ExpressionPlace.GetExpression(instance), FullName, null, null, callSignature, access);

			// TODO: check operators: should deep-copy return value if PhpRoutine is called
		}
	}

	public sealed class UnknownSignature : RoutineSignature
	{
		public static readonly UnknownSignature/*!*/ Default = new UnknownSignature(0, 0);
		public static readonly UnknownSignature/*!*/ Delegate = new UnknownSignature(0, 1);

		public override bool IsUnknown { get { return true; } }

		/// <summary>
		/// Returns <B>true</B> as parameters should be passed to the stack by reference since
		/// we don't know whether they are passed by ref in the real signature.
		/// </summary>
		public override bool IsAlias(int index) { return true; }
		public override bool AliasReturn { get { return true; } }

		public override int MandatoryParamCount { get { return paramCount; } }
		public override int ParamCount { get { return paramCount; } }
		public override int GenericParamCount { get { return genericParamCount; } }
		public override int MandatoryGenericParamCount { get { return genericParamCount; } }

		public override DType GetTypeHint(int index) { return null; }

		private int paramCount;
		private int genericParamCount;

		private UnknownSignature(int genericParamCount, int paramCount)
		{
			this.genericParamCount = genericParamCount;
			this.paramCount = paramCount;
		}
	}

	#endregion

	#region PhpRoutineSignature

	/// <summary>
	/// Represents PHP routine signature. Immutable.
	/// </summary>
	public sealed class PhpRoutineSignature : RoutineSignature
	{
		#region Properties

		//^ invariant typeHints.Length == aliasMaks.Length;
		//^ invariant mandatoryParamCount >= 0 && mandatoryParamCount <= typeHints.Length;
		//^ invariant mandatoryGenericParamCount >= 0 && mandatoryGenericParamCount <= genericParams.Length;

		public override bool IsUnknown { get { return false; } }

		/// <summary>
		/// Type parameters.
		/// </summary>
		public GenericParameter[]/*!!*/ GenericParams { get { return genericParams; } }
		private readonly GenericParameter[]/*!!*/ genericParams;

		/// <summary>
		/// Gets the number of mandatory type parameters.
		/// </summary>
		public override int MandatoryGenericParamCount { get { return mandatoryGenericParamCount; } }
		private readonly int mandatoryGenericParamCount;

		/// <summary>
		/// Gets alias mask, i.e. the bitmask where a flag is set iff the respective formal parameter aliased.
		/// </summary>
		public BitArray/*!W*/ AliasMask { get { return aliasMask; } }
		private BitArray aliasMask;

		/// <summary>
		/// Gets type hints. Array, int, bool, etc. hints are represented by special DTypeDesc.
		/// </summary>
		public DType[]/*!!W*/ TypeHints { get { return typeHints; } }
		private DType[]/*!!W*/ typeHints;

		/// <summary>
		/// Gets whether the routine returns by alias.
		/// </summary>
		public override bool AliasReturn { get { return aliasReturn; } }
		private bool aliasReturn;

		/// <summary>
		/// Gets the number of mandatory parameters.
		/// </summary>
		public override int MandatoryParamCount { get { return mandatoryParamCount; } }
		private int mandatoryParamCount;

		/// <summary>
		/// Gets the number of all parameters.
		/// </summary>
		public override int ParamCount { get { return typeHints.Length; } }

		/// <summary>
		/// Gets the number of all type parameters.
		/// </summary>
		public override int GenericParamCount { get { return genericParams.Length; } }

		#endregion

		#region Construction

		/// <summary>
		/// Creates a signature partially initialized with type parameters. 
		/// Rationale for partial initialization: 
		/// When analyzing a routine, the analyzer needs to know about type parameters prior to the analysis of actual parameters.
		/// </summary>
		public PhpRoutineSignature(GenericParameter[]/*!!*/ genericParams, int mandatoryGenericParamCount)
		{
			Debug.Assert(genericParams != null);
			Debug.Assert(mandatoryGenericParamCount >= 0 && mandatoryGenericParamCount <= genericParams.Length);

			this.genericParams = genericParams;
			this.mandatoryGenericParamCount = mandatoryGenericParamCount;
		}

		/// <summary>
		/// Completes the signature with information on actual parameters and return value. 
		/// Returns this instance for convenience.
		/// </summary>
		public PhpRoutineSignature/*!*/ WriteUp(bool aliasReturn, BitArray/*!*/ aliasMask, DType[]/*!*/ typeHints, int mandatoryParamCount)
		{
			Debug.Assert(aliasMask != null && typeHints != null);
			Debug.Assert(aliasMask.Length == typeHints.Length);
			Debug.Assert(mandatoryParamCount >= 0 && mandatoryParamCount <= typeHints.Length);

			this.aliasReturn = aliasReturn;
			this.aliasMask = aliasMask;
			this.typeHints = typeHints;
			this.mandatoryParamCount = mandatoryParamCount;

			return this;
		}

		#endregion

		#region Misc

		public override DType GetTypeHint(int index)
		{
			return (index < typeHints.Length) ? typeHints[index] : null;
		}

		public GenericParameter GetGenericParameter(Name name)
		{
			for (int i = 0; i < genericParams.Length; i++)
			{
				if (genericParams[i].Name.Equals(name))
					return genericParams[i];
			}

			return null;
		}

		/// <summary>
		/// Whether a index-th parameter has by-ref semantics.
		/// Can be called with index greater than the number of parameters. 
		/// Return <B>false</B> in such cases as the arguments behind the last formal parameter cannot be 
		/// returned to the caller modified.
		/// </summary>
		public override bool IsAlias(int index)
		{
			Debug.Assert(index >= 0);
			return (index < aliasMask.Count) ? aliasMask[index] : false;
		}

        ///// <summary>
        ///// Determines whether specified signatures are compatible, i.e.
        ///// one of them is unknown or their param. counts are the same and type hints are the same.
        ///// </summary>
        //public static bool AreCompatible(PhpRoutineSignature sig1, PhpRoutineSignature sig2)
        //{
        //  // signature not known => assume ok (for some system methods):
        //  if (sig1 == null || sig2 == null) return true;

        //  // different return types:
        //  if (sig1.AliasReturn != sig2.AliasReturn) return false;

        //  // different parameter counts:
        //  if (sig1.ParamCount != sig2.ParamCount) return false;

        //  for (int i = 0; i < sig1.ParamCount; i++)
        //  {
        //    if (sig1.aliasMask[i] != sig2.aliasMask[i] || sig1.typeHints[i] != sig2.typeHints[i])
        //      return false;
        //  }

        //  return true;
        //}

        /// <summary>
        /// Determines whether this signature can override given signature.
        /// Parameters count, type hints and names must match.
        /// </summary>
        /// <param name="sig"></param>
        /// <returns>True if method with this signature can override method with given signature.</returns>
        internal override bool CanOverride( RoutineSignature sig )
        {
            if (!base.CanOverride(sig))
                return false;

            PhpRoutineSignature phpsig = sig as PhpRoutineSignature;

            if (phpsig != null)
            {
                if (this.TypeHints.Length < phpsig.TypeHints.Length)
                    return false;

                // parameters by ref as it is in base declaration ?
                for (int i = 0; i < phpsig.AliasMask.Count; ++i)
                    if (this.IsAlias(i) != phpsig.IsAlias(i))
                        return false;

                // different type hints?
                for (int i = 0; i < phpsig.TypeHints.Length; ++i)
                {
                    DType hintType = this.TypeHints[i];
                    DType hintBaseType = phpsig.GetTypeHint(i);

                    if (hintType == null || hintBaseType == null)
                        continue;   // skip this now, we don't know some type hint, or it has not been specified ...
                    // TODO: in some cases Phalanger does not remember type hints (e.g. abstract class in external file ?)

                    string hintName = (hintType != null) ? hintType.FullName.ToLower() : null;
                    string hintBaseName = (hintBaseType != null) ? hintBaseType.FullName.ToLower() : null;

                    if (hintName != hintBaseName)
                        return false;
                }
            }

            return true;
        }

		/// <summary>
		/// Returns an array of types according to the alias mask of the signature (for argfull use).
		/// </summary>
		/// <param name="hiddenParamCount">
		/// Number of hidden parameters preceding the parameters captured by alias mask.
		/// </param>
		/// <param name="returnType">Return type.</param>
		/// <returns>
		/// Argfull overload CLR parameter types. Contains <B>null</B> references in places of hidden params.
		/// </returns>
		public Type[] ToArgfullSignature(int hiddenParamCount, out Type returnType)
		{
			Type[] result = new Type[hiddenParamCount + GenericParamCount + ParamCount];

			int arg_idx = hiddenParamCount;

			for (int i = 0; i < GenericParamCount; i++)
			{
				result[arg_idx++] = Emit.Types.DTypeDesc[0];
			}

			for (int i = 0; i < ParamCount; i++)
			{
				result[arg_idx++] = (aliasMask[i]) ? Emit.Types.PhpReference[0] : Emit.Types.Object[0];
			}

			returnType = aliasReturn ? Emit.Types.PhpReference[0] : Emit.Types.Object[0];

			return result;
		}

		#endregion

		#region Reflection

		/// <summary>
		/// Returns a <see cref="PhpRoutineSignature"/> for the specified argfull <see cref="MethodInfo"/>.
		/// </summary>
		/// <exception cref="ReflectionException">Invalid argfull.</exception>
		public static PhpRoutineSignature/*!*/ FromArgfullInfo(PhpRoutine/*!*/ routine, MethodInfo/*!*/ argfull)
		{
			// determine aliasReturn
			bool alias_return = false;

			if (argfull.ReturnType != Types.Object[0])
			{
				if (argfull.ReturnType == Types.PhpReference[0]) alias_return = true;
				else throw new ReflectionException("TODO");
			}

			ParameterInfo[] parms = argfull.GetParameters();

			int parms_length = parms.Length;
			int parms_offset = 1;

			if (--parms_length < 0 || parms[0].ParameterType != Types.ScriptContext[0])
			{
				// TODO: resource
				throw new ReflectionException("Invalid static argfull signature of method " + routine.FullName);
			}

			// process pseudo-generic parameters:
			int j = parms_offset;
			while (j < parms.Length && parms[j].ParameterType == Types.DTypeDesc[0]) j++;
			int generic_param_count = j - parms_offset;

			int mandatory_generic_param_count = 0;
			GenericParameter[] generic_params = (generic_param_count > 0) ? new GenericParameter[generic_param_count] : GenericParameter.EmptyArray;

			for (int i = 0; i < generic_param_count; i++)
			{
				ParameterInfo pinfo = parms[i + parms_offset];
				generic_params[i] = new GenericParameter(new Name(pinfo.Name), i, routine);

				DTypeSpecAttribute default_type = DTypeSpecAttribute.Reflect(pinfo);
				if (default_type != null)
				{
					// TODO:
					// generic_params[i].WriteUp(default_type.TypeSpec.GetTypeDesc(null, null, null, null).Type);
				}
				else
				{
					generic_params[i].WriteUp(null);

					if (mandatory_generic_param_count < i)
					{
						// TODO: resource
						throw new ReflectionException("Invalid signature");
					}
					else
						mandatory_generic_param_count++;
				}
			}

			parms_offset += generic_param_count;
			parms_length -= generic_param_count;

			// set up genericParams, aliasMask, typeHints, and mandatoryParamCount:
			BitArray alias_mask = new BitArray(parms_length, false);
			DType[] type_hints = new DType[parms_length];
			int mandatory_param_count = 0;

			for (int i = 0; i < parms_length; i++)
			{
				ParameterInfo pinfo = parms[i + parms_offset];

				if (pinfo.ParameterType != Types.Object[0])
				{
					if (pinfo.ParameterType == Types.PhpReference[0])
						alias_mask[i] = true;
					else
						return null;
				}

				DTypeSpecAttribute type_hint = DTypeSpecAttribute.Reflect(pinfo);
				if (type_hint != null)
				{
					// TODO:
					// type_hints[i] = type_hint.TypeSpec.GetTypeDesc(null, null, null, null).Type;
				}

				if (!pinfo.IsOptional)
				{
					if (mandatory_param_count < i)
					{
						// invalid optional parameters
						throw new ReflectionException("Invalid signature");
					}
					else mandatory_param_count++;
				}
			}

			return new PhpRoutineSignature(generic_params, mandatory_generic_param_count).
			  WriteUp(alias_return, alias_mask, type_hints, mandatory_param_count);
		}

		#endregion

		#region Debug

#if DEBUG
		internal void Print(string name)
		{
			Console.Write(aliasReturn ? "ref " : "obj ");
			Console.Write(name + "(");
			foreach (bool is_ref in aliasMask)
				Console.Write(is_ref ? "ref " : "obj ");
			Console.WriteLine(")");
		}
#endif

		#endregion
	}

	#endregion

	#region PhpRoutine, PhpRoutineBuilder

	public abstract class PhpRoutine : KnownRoutine, IPhpMember
	{
		public const string ContextParamName = "<context>";
		public const string LocalVariablesTableName = "<locals>";

		#region Properties

		public PhpRoutineDesc/*!*/ PhpRoutineDesc { get { return (PhpRoutineDesc)memberDesc; } }
		
		public abstract bool IsFunction { get; }
		public bool IsMethod { get { return !IsFunction; } }
        public virtual bool IsLambdaFunction { get { return false; } }

		public abstract SourceUnit SourceUnit { get; }
		public abstract Position Position { get; }

        /// <summary>Contains value of the <see cref="IsDllImport"/> property</summary>
        private bool isDllImport = false;
        /// <summary>Indicates if this method is decorated with <see cref="System.Runtime.InteropServices.DllImportAttribute"/></summary>
        public bool IsDllImport{
            get{return isDllImport;}
            protected internal set { isDllImport = value; }
        }

		/// <summary>
		/// Written up, not null after that.
		/// </summary>
		public PhpRoutineSignature/*!W*/ Signature { get { return signature; } }
		protected PhpRoutineSignature/*!W*/ signature;

		/// <summary>
		/// Auxiliary fields used for emission, <B>null</B> for reflected types.
		/// </summary>
		public PhpRoutineBuilder Builder { get { return builder; } }
		protected PhpRoutineBuilder builder;

		/// <summary>
		/// <B>true</B>, if the method has generic arguments. 
		/// Valid since write-up.
		/// </summary>
		public bool IsGeneric { get { return signature.GenericParamCount > 0; } }

		/// <summary>
		/// Gets the number of hidden arguments of the arg-full overload.
		/// Valid since write-up.
		/// </summary>
		internal int FirstPhpParameterIndex
		{
			get { return FirstPseudoGenericParameterIndex + signature.GenericParamCount; }
		}

		/// <summary>
		/// Gets an index of the first pseudo-generic argument.
		/// Methods and functions use the 0-th argument for passing the <see cref="ScriptContext"/>.
		/// In instance methods, the 0-th argument is "this".
		/// </summary>
		internal int FirstPseudoGenericParameterIndex
		{
			get { return (IsStatic ? 1 : 2); }
		}

		/// <summary>
		/// PHP routine result should be checked for deep-copy.
		/// </summary>
		public override bool ReturnValueDeepCopyEmitted { get { return false; } }

		internal abstract bool IsExported { get; }

		#endregion

		#region Construction

		/// <summary>
		/// Used by the compiler.
		/// </summary>
		internal PhpRoutine(DRoutineDesc/*!*/ functionDesc, Signature astSignature, TypeSignature astTypeSignature)
			: base(functionDesc)
		{
			this.signature = null; // to be written up
			this.builder = new PhpRoutineBuilder(this, astSignature, astTypeSignature);
		}

		internal void WriteUp(PhpRoutineSignature/*!*/ signature)
		{
			Debug.Assert(signature != null);
			Debug.Assert(this.signature == null, "Already written up.");

			this.signature = signature;
		}

		/// <summary>
		/// Used by the reflection.
		/// </summary>
		public PhpRoutine(DRoutineDesc/*!*/ functionDesc)
			: base(functionDesc)
		{
			this.signature = null; // to be written up
			this.builder = null; // unused
		}

		#endregion

		#region Utils

		public override RoutineSignature/*!*/ GetSignature(int overloadIndex)
		{
			Debug.Assert(signature != null);
			return signature;
		}

		public abstract string GetFullClrName();

		#endregion

		#region Analysis

        internal void ValidateBody(ErrorSink/*!*/ errors)
		{
			// checks whether there are too many local variables (warning only):
			if (builder.LocalVariables.Count > VariablesTable.SuboptimalLocalsCount)
			{
				properties |= RoutineProperties.HasUnoptimizedLocals;

				if (IsMethod)
				{
					errors.Add(Warnings.TooManyLocalVariablesInMethod, SourceUnit, Position,
						DeclaringType.FullName, FullName, builder.LocalVariables.Count.ToString());
				}
				else
				{
					errors.Add(Warnings.TooManyLocalVariablesInFunction, SourceUnit, Position,
						FullName, builder.LocalVariables.Count.ToString());
				}
			}

			// check labels:
			Analyzer.ValidateLabels(errors, SourceUnit, builder.Labels);
		}

		#endregion

		#region Emission

		#region DefineBuilders

        /// <summary>
        /// Defines real method on routine declaring type.
        /// </summary>
        protected virtual MethodInfo/*!*/DefineRealMethod(string/*!*/realMethodName, MethodAttributes attrs, Type/*!*/returnType, Type[]/*!!*/parametersType)
        {
            Debug.Assert(realMethodName != null);

            return this.DeclaringType.DefineRealMethod(realMethodName, attrs, returnType, parametersType);
        }

		internal virtual void DefineBuilders()
		{
			MethodAttributes attrs = Enums.ToMethodAttributes(memberDesc.MemberAttributes);

			string clr_name;

			if (IsLambda)
			{
				clr_name = Name.LambdaFunctionName.Value;
				attrs &= ~MethodAttributes.MemberAccessMask;
				attrs |= MethodAttributes.PrivateScope | MethodAttributes.SpecialName;
			}
			else
			{
				clr_name = this.GetFullClrName();
			}

			DefineArglessOverload(attrs, clr_name);
			DefineArgfullOverload(attrs, clr_name);

			// we can emit argless overload here as it requires argfull method info only:
			if (ArgLessInfo != null) EmitArglessOverload();
		}

		private void DefineArglessOverload(MethodAttributes attrs, string/*!*/ realMethodName)
		{
			if (!IsAbstract)
			{
				// defines overload (even instance methods have static argless overloads):
				// mark argless as having a special name to enable fast removal from the stack trace:
				attrs |= MethodAttributes.Static | MethodAttributes.SpecialName;
				attrs &= ~(MethodAttributes.Virtual | MethodAttributes.Final);

				this.argless = DefineRealMethod(realMethodName, attrs, Types.Object[0], Types.Object_PhpStack);

				// [EditorBrowsable(Never)] for user convenience - not available on SL:
#if !SILVERLIGHT
				if (IsExported)
					ReflectionUtils.SetCustomAttribute(argless, AttributeBuilders.EditorBrowsableNever);
#endif

				// [DebuggerHidden] drives the stack tracer to skip the frame:
				// ReflectionUtils.SetCustomAttribute(argless, AttributeBuilders.DebuggerHidden);					

				// we need to name the arguments as ASP.NET's binding reflection requires named args:
				MethodBuilder method_builder = argless as MethodBuilder;
				if (method_builder != null)
				{
					method_builder.DefineParameter(1, ParameterAttributes.None, "instance");
					method_builder.DefineParameter(2, ParameterAttributes.None, "stack");
				}
			}
			else
			{
				this.argless = null;
			}
		}

        private void DefineArgfullOverload(MethodAttributes attrs, string/*!*/ realMethodName)
		{
			Type return_type;
            Type[] param_types;

            param_types = signature.ToArgfullSignature(1, out return_type);
            param_types[0] = Types.ScriptContext[0];

			// defines overload:
			this.argfull = DefineRealMethod(realMethodName, attrs, return_type, param_types);

			DefineParameterBuildersOnArgFull();

			// needs to be emitted to distinguish arg-full from exported:
			// [EditorBrowsable(Never)] - not available on SL
#if !SILVERLIGHT
			ReflectionUtils.SetCustomAttribute(argfull, AttributeBuilders.EditorBrowsableNever);
#endif

            // [NeedsArglessAttribute] to mark the function if it should be called via argless stub
            if ((this.Properties & RoutineProperties.IsArgsAware) != 0)  // function requires PhpStack to be loaded
                ReflectionUtils.SetCustomAttribute(argfull,
                    new CustomAttributeBuilder(typeof(NeedsArglessAttribute).GetConstructor(Type.EmptyTypes), ArrayUtils.EmptyObjects));

            // [UsesLateStaticBindingAttribute] to mark the function if it needs type used to call method statically
            if (this.UsesLateStaticBinding)  // function requires PhpStack to be loaded
                ReflectionUtils.SetCustomAttribute(argfull,
                    new CustomAttributeBuilder(typeof(UsesLateStaticBindingAttribute).GetConstructor(Type.EmptyTypes), ArrayUtils.EmptyObjects));
            
            // [PhpAbstract][PhpFinal] if needed
			Enums.DefineCustomAttributes(MemberDesc.MemberAttributes, this.argfull);
		}

		private void DefineParameterBuildersOnArgFull()
		{
			builder.ReturnParamBuilder = ReflectionUtils.DefineParameter(argfull, 0, ParameterAttributes.None, null);

			// include "this" and "context" parameters as well (0-th parameters):
			int real_param_count = this.FirstPhpParameterIndex + signature.ParamCount;
			builder.ParameterBuilders = new ParameterBuilder[real_param_count];

			// names the first argument of the static argfull overload - the context:
			if (IsStatic)
                builder.ParameterBuilders[0] = ReflectionUtils.DefineParameter(argfull, 1, ParameterAttributes.None, PluginHandler.ConvertParameterName(PhpRoutine.ContextParamName));

			// pseudo-generic parameters:
			foreach (GenericParameter param in signature.GenericParams)
				param.DefineBuildersWithinMethod();

			// PHP parameters:
            for (int i = 0, real_index = FirstPhpParameterIndex; i < signature.ParamCount; i++, real_index++)
            {
                ParameterBuilder param_builder;

                string argName = PluginHandler.ConvertParameterName(builder.Signature.FormalParams[i].Name.Value);


                builder.ParameterBuilders[real_index] = param_builder = ReflectionUtils.DefineParameter(
                    argfull,
                    (IsStatic ? 1 : 0) + real_index,
                    (i < signature.MandatoryParamCount) ? ParameterAttributes.None : ParameterAttributes.Optional,
                    argName);
            }
		}

		#endregion

		#region Argless

		// argless signature: static object <name>(object instance, PhpStack stack);
		private static readonly IndexedPlace/*!*/ arglessInstancePlace = new IndexedPlace(PlaceHolder.Argument, 0);
		private static readonly IndexedPlace/*!*/ arglessStackPlace = new IndexedPlace(PlaceHolder.Argument, 1);

		private void EmitArglessOverload()
		{
			bool args_aware = (properties & RoutineProperties.IsArgsAware) != 0;

			// we need a mediator only to make the code verifiable, which is not applicable on dynamic methods:
            MethodInfo call_target = ArgFullInfo; // (IsStatic || ArgLessInfo is DynamicMethod) ? ArgFullInfo : BuildNonVirtualMediator();
			ILEmitter il = new ILEmitter(ArgLessInfo);

			LocalBuilder loc_count;

			// PhpStack.CalleeName = <name>;
			arglessStackPlace.EmitLoad(il);
			il.Emit(OpCodes.Ldstr, IsLambda ? DynamicCode.InlinedLambdaFunctionName : FullName);
			il.Emit(OpCodes.Stfld, Fields.PhpStack_CalleeName);

			if (!IsStatic)
			{
				// LOAD <instance>;
				arglessInstancePlace.EmitLoad(il);
				il.Emit(OpCodes.Castclass, DeclaringPhpType.Builder.RealOpenType);
			}

			// LOAD <script context>;
			arglessStackPlace.EmitLoad(il);
			il.Emit(OpCodes.Ldfld, Fields.PhpStack_Context);

			// peek pseudo-generic arguments:
			for (int i = 0; i < signature.GenericParamCount; i++)
				EmitPeekPseudoGenericArgument(il, i);

			// peek regular arguments:
			for (int i = 0; i < signature.ParamCount; i++)
				EmitPeekArgument(il, i);

            // emits pre call code (alters a frame if a function is args-aware removes it otherwise):
			PhpStackBuilder.EmitArgFullPreCall(il, arglessStackPlace, args_aware, signature.ParamCount,
			  signature.GenericParamCount, out loc_count);

			// emits call to the arg-full overload non-virtually;
			// the return value is left on the stack until return:
			il.Emit(OpCodes.Call, call_target);

			// emits post call code (removes a frame if the function is args-aware):
			PhpStackBuilder.EmitArgFullPostCall(il, arglessStackPlace, loc_count);

			il.Emit(OpCodes.Ret);
		}

        private void EmitPeekPseudoGenericArgument(ILEmitter/*!*/ il, int index)
		{
			bool optional = index >= signature.MandatoryGenericParamCount;
			int stack_offset = index + 1;

			// LOAD stack.PeekType[Optional](<stack_offset>, [default_value]);
			arglessStackPlace.EmitLoad(il);                           // stack
			il.LdcI4(stack_offset);                                   // offset on stack

			if (optional)
				il.Emit(OpCodes.Call, Methods.PhpStack.PeekTypeOptional);
			else
				il.Emit(OpCodes.Call, Methods.PhpStack.PeekType);
		}

		/// <summary>
		/// Emits code which pops argument from the <see cref="PhpStack"/> and pushes it on the evaluation stack.
		/// </summary>
		private void EmitPeekArgument(ILEmitter/*!*/ il, int index)
		{
			bool optional = index >= signature.MandatoryParamCount;
			int stack_offset = index + 1;

			if (signature.IsAlias(index))
			{
				// LOAD stack.PeekReference[Optional](<stack_offset>, [default_value]);
				arglessStackPlace.EmitLoad(il);                           // stack
				il.LdcI4(stack_offset);                                   // offset on stack

				if (optional)
					il.Emit(OpCodes.Call, Methods.PhpStack.PeekReferenceOptional);
				else
					il.Emit(OpCodes.Call, Methods.PhpStack.PeekReference);
			}
			else
			{
				// LOAD stack.PeekValue[Optional](<stack_offset>);
				arglessStackPlace.EmitLoad(il);                           // stack
				il.LdcI4(stack_offset);                                   // offset on stack

				if (optional)
					il.Emit(OpCodes.Call, Methods.PhpStack.PeekValueOptional);
				else
					il.Emit(OpCodes.Call, Methods.PhpStack.PeekValue);
			}
		}

		#endregion

		#region Call

        /// <summary>
        /// Emit load <paramref name="instance"/> in top of the evaluation stack. Unwraps the value if &lt;proxy&gt; is used instead of <c>this</c>.
        /// </summary>
        /// <param name="codeGenerator"></param>
        /// <param name="instance"></param>
        private static void EmitLoadInstanceUnwrapped(CodeGenerator/*!*/ codeGenerator, IPlace instance)
        {
            if (instance != null)
            {
                // just detect DirectVarUse holding $this in context of Type with <proxy> property:
                var targetExpression = ExpressionPlace.GetExpression(instance);

                // pass RealObject instead of DObject when using <proxy>:   // J: ASP.NET code behind fix // ArgLesses expect RealObject too
                if (targetExpression != null &&
                    codeGenerator.LocationStack.InMethodDecl && codeGenerator.LocationStack.PeekMethodDecl().Type.ProxyFieldInfo != null &&    // current type has "<proxy>" property
                    targetExpression is DirectVarUse && ((DirectVarUse)targetExpression).VarName.IsThisVariableName && ((DirectVarUse)targetExpression).IsMemberOf == null)   // we are accessing "this"
                    instance = IndexedPlace.ThisArg;    // "this" instead of "this.<proxy>"

                //
                instance.EmitLoad(codeGenerator.IL);
            }
        }

        internal override PhpTypeCode EmitCall(CodeGenerator/*!*/ codeGenerator, string fallbackQualifiedName, CallSignature callSignature,
            IPlace instance, bool runtimeVisibilityCheck, int overloadIndex, DType type, Position position,
            AccessType access, bool callVirt)
		{
            if (IsStatic != (instance == null) || runtimeVisibilityCheck)
			{
                Expression targetExpression = null;
                if (instance != null)
                {
                    targetExpression = ExpressionPlace.GetExpression(instance); // the instance expression, $this would be determined in runtime
                    Debug.Assert(targetExpression != null || instance == IndexedPlace.ThisArg,
                        "Unexpected instance IPlace type" + this.Name.Value);
                }

                // call the operator if we could not provide an appropriate instance or the visibility has to be checked:
                return codeGenerator.EmitRoutineOperatorCall(this.UsesLateStaticBinding ? type : this.DeclaringType, targetExpression, this.FullName, fallbackQualifiedName, null, callSignature, access);
			}

            Debug.Assert(IsStatic == (instance == null));

            if (IsStatic) callVirt = false; // never call static method virtually
            
			ILEmitter il = codeGenerator.IL;
			var constructedType = type as ConstructedType;

            // load the instance reference if we have one:
            // Just here we need RealObject if possible. When calling CLR method on $this,
            // Phalanger has "this.<proxy>" in "codeGenerator.SelfPlace". We need just "this".
			EmitLoadInstanceUnwrapped(codeGenerator, instance);

            // arg-full overload may not be present in the case of classes declared Class Library where
			// we do not require the user to specify both overloads
			if (IsArgsAware || ArgFullInfo == null)
			{
				// args-aware routines //
                Debug.Assert(callVirt == false, "Cannot call ArgLess stub virtually!");

				// all arg-less stubs have the 'instance' parameter
				if (instance == null) il.Emit(OpCodes.Ldnull);

				// emits load of parameters to the PHP stack:
				callSignature.EmitLoadOnPhpStack(codeGenerator);

                // CALL <routine>(context.Stack)
				codeGenerator.EmitLoadScriptContext();
				il.Emit(OpCodes.Ldfld, Fields.ScriptContext_Stack);

                if (this.UsesLateStaticBinding)
                {
                    // <stack>.LateStaticBindType = <type>
                    il.Emit(OpCodes.Dup);
                    type.EmitLoadTypeDesc(codeGenerator, ResolveTypeFlags.None);
                    il.Emit(OpCodes.Stfld, Fields.PhpStack_LateStaticBindType);
                }

                il.Emit(OpCodes.Call, DType.MakeConstructed(ArgLessInfo, constructedType));

				// arg-less overload's return value has to be type-cast to a reference if it returns one:
				if (signature == null || signature.AliasReturn)
					codeGenerator.IL.Emit(OpCodes.Castclass, typeof(PhpReference));
			}
			else
			{
				// args-unaware routines //

				// CALL <routine>(context, <argumens>)
				codeGenerator.EmitLoadScriptContext();
				callSignature.EmitLoadOnEvalStack(codeGenerator, this);
                il.Emit(callVirt ? OpCodes.Callvirt : OpCodes.Call, DType.MakeConstructed(ArgFullInfo, constructedType));
			}

			// marks transient sequence point just after the call:
			codeGenerator.MarkTransientSequencePoint();

			return ((signature == null || signature.AliasReturn) ? PhpTypeCode.PhpReference : PhpTypeCode.Object);
		}

		#endregion

		#endregion
	}

	public sealed class PhpRoutineBuilder
	{
		public PhpRoutine/*!*/ Routine { get { return routine; } }
		private readonly PhpRoutine/*!*/ routine;

		internal ExportAttribute ExportInfo
		{
			get { return exportInfo; }
			set /* FunctionDecl, MethodDecl */ { exportInfo = value; }
		}
		private ExportAttribute exportInfo;

		internal Signature Signature { get { return signature; } }
		private readonly Signature signature;

		internal TypeSignature TypeSignature { get { return typeSignature; } }
		private readonly TypeSignature typeSignature;

		internal VariablesTable/*!*/ LocalVariables { get { return localVariables; } }
		private readonly VariablesTable/*!*/ localVariables;

		/// <summary>
		/// TODO: lazy creation
		/// </summary>
		internal Dictionary<VariableName, Statement>/*!*/ Labels { get { return labels; } }
		private Dictionary<VariableName, Statement>/*!*/ labels;

		internal ParameterBuilder ReturnParamBuilder { get { return returnParamBuilder; } set { returnParamBuilder = value; } }
		private ParameterBuilder returnParamBuilder = null;

		public ParameterBuilder[] ParameterBuilders { get { return parameterBuilders; } set { parameterBuilders = value; } }
		private ParameterBuilder[] parameterBuilders;

		internal PhpRoutineBuilder(PhpRoutine/*!*/ routine, Signature signature, TypeSignature typeSignature)
		{
			this.routine = routine;
			this.signature = signature;
			this.typeSignature = typeSignature;
			this.localVariables = new VariablesTable(10);
			this.labels = new Dictionary<VariableName, Statement>(1);
		}
	}

	#endregion

	#region PhpFunction

	public sealed class PhpFunction : PhpRoutine, IDeclaree
	{
		public override bool IsFunction { get { return true; } }

		public override bool IsIdentityDefinite
		{
			get { return declaration == null || !declaration.IsConditional; }
		}

		public override Name Name { get { return qualifiedName.Name; } }

		public QualifiedName QualifiedName { get { return qualifiedName; } }
		private QualifiedName qualifiedName;

		public VersionInfo Version { get { return version; } set { version = value; } }
		private VersionInfo version;

		public Declaration/*!*/ Declaration { get { return declaration; } }
		private Declaration/*!*/ declaration;

		public override SourceUnit SourceUnit { get { return declaration.SourceUnit; } }
		public override Position Position { get { return declaration.Position; } }

		public override bool IsLambda { get { return isLambda; } }
		private bool isLambda;

		internal override bool IsExported
		{
			get { return builder.ExportInfo != null; }
		}


		#region Construction

		/// <summary>
		/// To be used by compiler.
		/// </summary>
		internal PhpFunction(QualifiedName qualifiedName, PhpMemberAttributes memberAttributes,
			Signature astSignature, TypeSignature astTypeSignature, bool isConditional, Scope scope,
			SourceUnit/*!*/ sourceUnit, Position position)
			: base(new PhpRoutineDesc(sourceUnit.CompilationUnit.Module, memberAttributes), astSignature, astTypeSignature)
		{
			Debug.Assert(sourceUnit != null && position.IsValid);

			this.declaration = new Declaration(sourceUnit, this, false, isConditional, scope, position);
			this.qualifiedName = qualifiedName;
			this.version = new VersionInfo();
			this.signature = null; // to be written up
		}

		
		/// <summary>
		/// Used by full-reflect.
		/// </summary>
		internal PhpFunction(QualifiedName name, PhpRoutineDesc/*!*/ routineDesc, MethodInfo/*!*/ argfull, MethodInfo argless)
			: base(routineDesc)
		{
			Debug.Assert(argless != null);

			this.qualifiedName = name;
			this.argfull = argfull;
			this.argless = argless;

            // if the function needs to be called via argless stub, update the property
            if (NeedsArglessAttribute.IsSet(argfull))
                this.Properties |= RoutineProperties.UseVarArgs;    // the function calls some arg-aware class-library function so it has to be called with PhpStack

            Debug.Assert(!UsesLateStaticBindingAttribute.IsSet(argfull), "Function cannot use late static binding! Only methods can.");
		}

		#endregion

		#region Utils

		public void ConvertToLambda()
		{
			isLambda = true;
		}

		public override string GetFullName()
		{
			return qualifiedName.ToString();
		}

		public override string GetFullClrName()
		{
			return qualifiedName.ToClrNotation(0, version.Index);
		}

		internal override void ReportError(ErrorSink/*!*/ sink, ErrorInfo error)
		{
			if (declaration != null)
				sink.Add(error, declaration.SourceUnit, declaration.Position);
		}

		public void ReportRedeclaration(ErrorSink/*!*/ errors)
		{
			Debug.Assert(declaration != null);
			errors.Add(FatalErrors.FunctionRedeclared, declaration.SourceUnit, declaration.Position, FullName);
		}

		#endregion

		#region Analysis

		internal void Validate(ErrorSink/*!*/ errors)
		{
			// TODO: check special functions (__autoload)
		}

		internal override int ResolveOverload(Analyzer/*!*/ analyzer, CallSignature callSignature, Position position,
			out RoutineSignature overloadSignature)
		{
			if (callSignature.Parameters.Count < signature.MandatoryParamCount)
			{
				analyzer.ErrorSink.Add(Warnings.TooFewFunctionParameters, analyzer.SourceUnit, position,
					qualifiedName, signature.MandatoryParamCount, callSignature.Parameters.Count);
			}

			overloadSignature = signature;
			return 0;
		}

		#endregion

		#region Emission

		internal override void DefineBuilders()
		{
			Debug.Assert(declaration != null);

			// don't define builders for unreachable functions and functions in incomplete class
			// (but we need to process next version)
			if (!declaration.IsUnreachable && !declaration.IsInsideIncompleteClass) 
				base.DefineBuilders();

			// define builders for the other versions:
			if (version.Next != null)
				((PhpFunction)version.Next).DefineBuilders();
		}

		internal PhpRoutineDesc Bake()
		{
			DynamicMethod dynamic_argless = argless as DynamicMethod;
			if (dynamic_argless != null)
			{
				RoutineDesc.ArglessStub = (RoutineDelegate)dynamic_argless.CreateDelegate(Types.RoutineDelegate);

				// nulls member to cut the PhpRoutine off (we need to rereflect it):
				argless = null;
			}
			else if (argless != null)
			{
				// rereflect:

				// TODO (this is an incredible hacking :-) ):
				if (argless.DeclaringType != null && DeclaringModule.Assembly is TransientAssembly)
				{
					Type type = DeclaringModule.Assembly.RealModule.GetType(argless.DeclaringType.FullName);

					if (type != null /*&& !(type is TypeBuilder)*/ &&
                        (!(type is TypeBuilder) || ((TypeBuilder)type).IsCreated())) // TODO: order of baking in persistent units (1.script type then functions)
					{
						argless = type.GetMethod(argless.Name, BindingFlags.DeclaredOnly |
							BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
							null, ReflectionUtils.GetParameterTypes(argless.GetParameters()), null);

						RoutineDesc.ArglessStub = (RoutineDelegate)Delegate.CreateDelegate(Types.RoutineDelegate, argless);
					}
				}
			}

			RoutineDesc.Member = null;
			
			return this.PhpRoutineDesc;
		}

        internal override PhpTypeCode EmitCall(CodeGenerator/*!*/ codeGenerator, string fallbackQualifiedName, CallSignature callSignature,
            IPlace instance, bool runtimeVisibilityCheck, int overloadIndex, DType type, Position position,
            AccessType access, bool callVirt)
		{
			Debug.Assert(instance == null && !runtimeVisibilityCheck);
            Debug.Assert(callVirt == false);

			if (!IsDefinite)
			{
				return codeGenerator.EmitRoutineOperatorCall(null, null, this.FullName, null, null, callSignature, access);
			}
			else
			{
				return base.EmitCall(codeGenerator, fallbackQualifiedName, callSignature, null, false, overloadIndex, null, position, access, callVirt);
			}
		}

		#endregion
	}

	#endregion

	#region PhpMethod

	public sealed class PhpMethod : PhpRoutine
	{
		#region Properties

		public override bool IsFunction { get { return false; } }
		public override bool IsLambda { get { return false; } }
		public override bool IsIdentityDefinite { get { return true; } }

		public override Name Name { get { return name; } }
		private readonly Name name;

		/// <summary>
		/// Error reporting.
		/// <c>Position.Invalid</c> for reflected PHP methods.
		/// </summary>
		public override Position Position { get { return position; } }
		private readonly Position position;

		/// <summary>
		/// Error reporting (for partial classes).
		/// <B>null</B> for reflected PHP methods.
		/// </summary>
		public override SourceUnit SourceUnit { get { return sourceUnit; } }
		private SourceUnit sourceUnit;

		/// <summary>
		/// Methods only.
		/// </summary>
		public bool HasBody { get { return hasBody; } }
		private readonly bool hasBody;

		internal DMemberRef Overrides { get { return overrides; } set /* PhpType.Validate */ { overrides = value; } }
		private DMemberRef overrides;

		internal List<DMemberRef> Implements { get { return implements; } }
		private List<DMemberRef> implements;

		internal override bool IsExported
		{
			get { return builder.ExportInfo != null || this.DeclaringPhpType.IsExported; }
		}

		#endregion

		#region Construction

		/// <summary>
		/// Used by the compiler.
		/// </summary>
		internal PhpMethod(PhpType/*!*/ declaringType, Name name, PhpMemberAttributes memberAttributes, bool hasBody,
	  Signature astSignature, TypeSignature astTypeSignature, SourceUnit/*!*/ sourceUnit, Position position)
			: base(new PhpRoutineDesc(declaringType.TypeDesc, memberAttributes), astSignature, astTypeSignature)
		{
			Debug.Assert(declaringType != null && sourceUnit != null && position.IsValid);

			this.name = name;
			this.position = position;
			this.hasBody = hasBody;
			this.sourceUnit = sourceUnit;
		}

		/// <summary>
		/// Used by full-reflect.
		/// </summary>
		internal PhpMethod(Name name, PhpRoutineDesc/*!*/ routineDesc, MethodInfo/*!*/ argfull, MethodInfo argless)
			: base(routineDesc)
		{
			Debug.Assert(argless != null ^ IsAbstract);

			this.name = name;
			this.argfull = argfull;
			this.argless = argless;

            // if the function needs to be called via argless stub, update the properties
            if (NeedsArglessAttribute.IsSet(argfull))
                this.Properties |= RoutineProperties.UseVarArgs;    // the function calls some arg-aware class-library function so it has to be called with PhpStack

            if (UsesLateStaticBindingAttribute.IsSet(argfull))
                this.Properties |= RoutineProperties.LateStaticBinding;    // this method uses late static binding
		}

		#endregion

		#region Utils

		public override string GetFullName()
		{
			return name.Value;
		}

		public override string GetFullClrName()
		{
			return name.Value;
		}

		#endregion

		#region Analysis

		internal override int ResolveOverload(Analyzer/*!*/ analyzer, CallSignature callSignature, Position position,
			out RoutineSignature overloadSignature)
		{
			if (callSignature.Parameters.Count < signature.MandatoryParamCount)
			{
				if (IsConstructor)
				{
					analyzer.ErrorSink.Add(Warnings.TooFewCtorParameters, analyzer.SourceUnit, position,
						DeclaringType.FullName, signature.MandatoryParamCount, callSignature.Parameters.Count);
				}
				else if (IsStatic)
				{
					analyzer.ErrorSink.Add(Warnings.TooFewMethodParameters, analyzer.SourceUnit, position,
						DeclaringType.FullName, this.FullName, signature.MandatoryParamCount.ToString(),
			callSignature.Parameters.Count.ToString());
				}
			}

			overloadSignature = signature;
			return 0;
		}

		internal override void AddAbstractOverride(DMemberRef/*!*/ abstractMethod)
		{
			if (abstractMethod.Member.DeclaringType.IsInterface)
			{
				if (implements == null)
					implements = new List<DMemberRef>();

				implements.Add(abstractMethod);

				Debug.WriteLine("F-ANALYSIS", "GetUserEntryPoint '{0}::{1}': implemens += '{2}::{3}'",
					DeclaringType.FullName, FullName, abstractMethod.Type.MakeFullGenericName(), abstractMethod.Member.FullName);
			}
			else
			{
				overrides = abstractMethod;
				Debug.WriteLine("F-ANALYSIS", "GetUserEntryPoint '{0}::{1}': overrides = '{2}::{3}'",
					DeclaringType.FullName, FullName, overrides.Type.MakeFullGenericName(), overrides.Member.FullName);
			}
		}

		#endregion

		#region Validation

		internal void Validate(ErrorSink/*!*/ errors)
		{
			if (hasBody)
			{
				// make sure that interface methods have no bodies:
				if (DeclaringType.IsInterface)
				{
					errors.Add(Errors.InterfaceMethodWithBody, sourceUnit, position, DeclaringType.FullName, this.FullName);
				}
				else if (IsAbstract) // all methods in interfaces are abstract
				{
					// make sure that abstract methods have no bodies
					errors.Add(Errors.AbstractMethodWithBody, sourceUnit, position, DeclaringType.FullName, this.FullName);
					MemberDesc.MemberAttributes &= ~PhpMemberAttributes.Abstract;
				}
			}
			else
			{
				// make sure that non-abstract methods have bodies
				if (!IsAbstract)
				{
					errors.Add(Errors.NonAbstractMethodWithoutBody, sourceUnit, position, DeclaringType.FullName, this.FullName);
					MemberDesc.MemberAttributes |= PhpMemberAttributes.Abstract;
				}
			}

			if (this.IsConstructor)
			{
				// constructor non-staticness:
				if (IsStatic)
				{
					errors.Add(Errors.ConstructCannotBeStatic, sourceUnit, position, DeclaringType.FullName, this.FullName);
					MemberDesc.MemberAttributes &= ~PhpMemberAttributes.Static;
				}

				// no generic parameters on ctor:
				if (signature.GenericParamCount > 0)
				{
					errors.Add(Errors.ConstructorWithGenericParameters, sourceUnit, position, DeclaringType.FullName, this.FullName);
					// generic arguments needn't to be removed
				}
			}
			else if (this.Name.IsCloneName)
			{
				// clone argumentless-ness
				if (signature != null && signature.ParamCount > 0)
					errors.Add(Errors.CloneCannotTakeArguments, sourceUnit, position, DeclaringType.FullName);

				// clone non-staticness
				if (IsStatic)
				{
					errors.Add(Errors.CloneCannotBeStatic, sourceUnit, position, DeclaringType.FullName);
					RoutineDesc.MemberAttributes &= ~PhpMemberAttributes.Final;
				}
			}
			else if (this.Name.IsDestructName)
			{
				// destructor argumentless-ness
				if (signature != null && signature.ParamCount > 0)
					errors.Add(Errors.DestructCannotTakeArguments, sourceUnit, position, DeclaringType.FullName);

				// destructor non-staticness
				if (IsStatic)
				{
					errors.Add(Errors.DestructCannotBeStatic, sourceUnit, position, DeclaringType.FullName);
					MemberDesc.MemberAttributes &= ~PhpMemberAttributes.Static;
				}
			}
            else if (this.Name.IsCallName || this.Name.IsCallStaticName)
            {
                // check visibility & staticness
                if (this.Name.IsCallName && (this.IsStatic || !this.IsPublic))
                    errors.Add(Warnings.MagicMethodMustBePublicNonStatic, sourceUnit, position, this.Name.Value);

                if (this.Name.IsCallStaticName && (!this.IsStatic || !this.IsPublic))
                    errors.Add(Warnings.CallStatMustBePublicStatic, sourceUnit, position);

                // check args count
                if (signature != null && signature.ParamCount != 2)
                {
                    errors.Add(FatalErrors.MethodMustTakeExacArgsCount, sourceUnit, position, this.DeclaringType.FullName, this.Name.Value, 2);
                }
            }
            else if (this.Name.IsToStringName)
            {
                if (IsStatic || !IsPublic)
                    errors.Add(Warnings.MagicMethodMustBePublicNonStatic, sourceUnit, position, this.Name.Value);

                if (signature != null && signature.ParamCount != 0)
                    errors.Add(Errors.MethodCannotTakeArguments, sourceUnit, position, this.DeclaringType.FullName, this.Name.Value);
            }

			// no final abstract member:
			if (IsAbstract && IsFinal)
			{
				errors.Add(Errors.AbstractFinalMethodDeclared, sourceUnit, position);
				MemberDesc.MemberAttributes &= ~((hasBody) ? PhpMemberAttributes.Final : PhpMemberAttributes.Abstract);
			}

			// no private abstract member:
			if (IsAbstract && IsPrivate)
			{
				errors.Add(Errors.AbstractPrivateMethodDeclared, sourceUnit, position);
				MemberDesc.MemberAttributes &= ~((hasBody) ? PhpMemberAttributes.Private : PhpMemberAttributes.Abstract);
			}

			// no non-public interface methods
			if (DeclaringType.IsInterface && (IsPrivate || IsProtected))
			{
				errors.Add(Errors.InterfaceMethodNotPublic, sourceUnit, position, DeclaringType.FullName, this.FullName);
				MemberDesc.MemberAttributes &= ~PhpMemberAttributes.VisibilityMask;
				MemberDesc.MemberAttributes |= PhpMemberAttributes.Public;
			}
		}

		internal void ValidateOverride(ErrorSink/*!*/ errors, KnownRoutine/*!*/ overridden)
        {
            Debug.Assert(errors != null && overridden != null);
            Debug.Assert(sourceUnit != null, "Not applicable on reflected routines");

            // final method cannot be overridden:
            if (overridden.IsFinal)
            {
                errors.Add(Errors.OverrideFinalMethod, SourceUnit, position, DeclaringType.FullName, this.FullName);
                overridden.ReportError(errors, Errors.RelatedLocation);
            }

            // cannot override non-abstract method by abstract:
            if (this.IsAbstract && !overridden.IsAbstract)
            {
                errors.Add(Errors.OverridingNonAbstractMethodByAbstract, SourceUnit, position,
                    overridden.DeclaringType.FullName, overridden.FullName, DeclaringType.FullName);

                overridden.ReportError(errors, Errors.RelatedLocation);
            }

            // restricting method visibility:
            if ((overridden.IsPublic && !IsPublic ||
                    overridden.IsProtected && !this.IsProtected && !this.IsPublic) &&
                // visibility of .ctor in CLR base can be restricted:
                    !(overridden.DeclaringType.IsClrType && overridden.IsConstructor))
            {
                errors.Add(Errors.OverridingMethodRestrictsVisibility, SourceUnit, position,
                    DeclaringType.FullName, this.FullName, Enums.VisibilityToString(overridden.MemberDesc.MemberAttributes),
                    overridden.DeclaringType.FullName);

                overridden.ReportError(errors, Errors.RelatedLocation);
            }

            // method staticness non-overridable:
            if (overridden.IsStatic && !this.IsStatic)
            {
                errors.Add(Errors.MakeStaticMethodNonStatic, SourceUnit, position,
                    overridden.DeclaringType.FullName, overridden.FullName, DeclaringType.FullName);

                overridden.ReportError(errors, Errors.RelatedLocation);
            }

            // method non-staticness non-overridable:
            if (!overridden.IsStatic && this.IsStatic)
            {
                errors.Add(Errors.MakeNonStaticMethodStatic, SourceUnit, position,
                    overridden.DeclaringType.FullName, overridden.FullName, DeclaringType.FullName);

                overridden.ReportError(errors, Errors.RelatedLocation);
            }

            // strict standards: function reference
            // Declaration of bar::a() should be compatible with that of foo::a()
            // This check is not performed for __construct() function in PHP.
            if (!Signature.CanOverride(overridden.GetSignature(0)) && !this.Name.IsConstructName)
            {
                errors.Add(Warnings.DeclarationShouldBeCompatible, SourceUnit, position,
                    DeclaringType.FullName, this.FullName, overridden.DeclaringType.FullName, overridden.FullName);
                /*PhpException.Throw(PhpError.Strict,
                    CoreResources.GetString("declaration_should_be_compatible",
                        DeclaringType.FullName, this.FullName, overridden.DeclaringType.FullName, overridden.FullName));*/
            }
        }

		internal override void ReportError(ErrorSink/*!*/ sink, ErrorInfo error)
		{
			if (sourceUnit != null)
				sink.Add(error, SourceUnit, position);
		}

		#endregion

		#region Emission

		internal override void DefineBuilders()
		{
			base.DefineBuilders();
		}

        internal override PhpTypeCode EmitCall(
            CodeGenerator codeGenerator, string fallbackQualifiedName, CallSignature callSignature, IPlace instance,
            bool runtimeVisibilityCheck, int overloadIndex, DType type, Position position,
            AccessType access, bool callVirt)
        {
            Debug.Assert(fallbackQualifiedName == null, "Methods do not have fallbacks");

            // private PHP methods called directly, ignoring overrides
            if (IsPrivate || IsFinal || DeclaringType.IsFinal)
                callVirt = false;
            
            if ((Properties & RoutineProperties.IsArgsAware) != 0 || ArgFullInfo == null)
                runtimeVisibilityCheck = true;  // force dynamic call when the method routine cannot be called virtually

            // when calling an instance virtual method, and some passed arguments would be ignored,
            // force dynamic call in case there will be an overload that takes more arguments
            else if (callVirt
                && callSignature.Parameters.Count > 0   // some overload may accept less arguments, and its overload more, so we may loose some passed arguments; only if we are not passing any, we are safe
                //&& callSignature.Parameters.Count != this.Signature.ParamCount     // TODO: only if 'Declaration should be compatible' warning would be considered as error, or overrides must not have less arguments than base overriden method
                ) runtimeVisibilityCheck = true;
            
            // emit the routine call
            return base.EmitCall(codeGenerator, fallbackQualifiedName, callSignature, instance, runtimeVisibilityCheck, overloadIndex, type, position, access, callVirt);
        }

        #endregion
	}

	#endregion

    #region PhpLambdaFunction

    public sealed class PhpLambdaFunction : PhpRoutine
    {
        #region Properties

        public override bool IsFunction { get { return true; } }
        public override bool IsLambda { get { return true; } } // but different lambda
        public override bool IsLambdaFunction { get { return true; } }
        public override bool IsIdentityDefinite { get { return true; } }

        public override Name Name { get { return Name.ClosureFunctionName; } }

        public override Position Position { get { return position; } }
        private readonly Position position;

        public override SourceUnit SourceUnit { get { return sourceUnit; } }
        private SourceUnit sourceUnit;

        internal override bool IsExported { get { return false; } }

        #endregion

        #region Construction

        /// <summary>
        /// Used by the compiler.
        /// </summary>
        internal PhpLambdaFunction(Signature astSignature, SourceUnit/*!*/ sourceUnit, Position position)
            : base(
            new PhpRoutineDesc(
                DTypeDesc.Create(typeof(PHP.Library.SPL.Closure)),
                PhpMemberAttributes.Private | PhpMemberAttributes.Static | PhpMemberAttributes.Final),
            astSignature,
            new TypeSignature(FormalTypeParam.EmptyList))
        {
            Debug.Assert(sourceUnit != null && position.IsValid);

            this.position = position;
            this.sourceUnit = sourceUnit;
        }

        #endregion

        #region Utils

        public override string GetFullName()
        {
            return Name.Value;
        }

        public override string GetFullClrName()
        {
            return Name.Value;
        }

        #endregion

        #region Analysis

        internal override int ResolveOverload(Analyzer/*!*/ analyzer, CallSignature callSignature, Position position,
            out RoutineSignature overloadSignature)
        {
            overloadSignature = signature;
            return 0;
        }

        internal override void AddAbstractOverride(DMemberRef/*!*/ abstractMethod)
        {
            throw new NotSupportedException();
        }

        #endregion

        #region Validation

        internal override void ReportError(ErrorSink/*!*/ sink, ErrorInfo error)
        {
            if (sourceUnit != null)
                sink.Add(error, SourceUnit, position);
        }

        #endregion

        #region Emission

        private TypeBuilder/*!*/typeBuilder;
        protected override MethodInfo DefineRealMethod(string realMethodName, MethodAttributes attrs, Type returnType, Type[] parametersType)
        {
            return typeBuilder.DefineMethod(realMethodName, attrs, returnType, parametersType);
        }
        internal override void DefineBuilders()
        {
            throw new InvalidOperationException();
        }
        public void DefineBuilders(TypeBuilder/*!*/typeBuilder)
        {
            this.typeBuilder = typeBuilder;
            base.DefineBuilders();
        }

        internal override PhpTypeCode EmitCall(
            CodeGenerator codeGenerator, string fallbackQualifiedName, CallSignature callSignature, IPlace instance,
            bool runtimeVisibilityCheck, int overloadIndex, DType type, Position position,
            AccessType access, bool callVirt)
        {
            // calling closured function directly is not handled yet (not needed without type inference),
            // anyway in future, this will be probably handled thru Closure::__invoke( instance, stack ).
            throw new NotImplementedException();
        }

        #endregion
    }

    #endregion

	#region PhpLibraryFunction

	public sealed class PhpLibraryFunction : KnownRoutine
	{
		#region Overload

		/// <summary>
		/// Additional overload flags.
		/// </summary>
        [Flags]
        public enum OverloadFlags : byte
        {
            /// <summary>
            /// None.
            /// </summary>
            None = 0,

            /// <summary>
            /// Needs local variables of caller
            /// </summary>
            NeedsVariables = 1,

            /// <summary>
            /// Needs $this reference of caller
            /// </summary>
            NeedsThisReference = 2,

            NeedsNamingContext = 4,

            /// <summary>
            /// Needs DTypeDesc class context of the caller.
            /// </summary>
            NeedsClassContext = 8,

            /// <summary>Overload has "params" array as its last argument.</summary>
            IsVararg = 16,

            /// <summary>
            /// The overload has the ScriptContext as the first parameter. It will be passed automatically.
            /// </summary>
            NeedsScriptContext = 32,

            /// <summary>
            /// Function is not supported.
            /// </summary>
            NotSupported = 64,
            
            /// <summary>
            /// Needs DTypeDesc class context of the late static binding.
            /// </summary>
            NeedsLateStaticBind = 128,
        }

		public sealed class Overload : RoutineSignature
		{
			public override bool IsUnknown { get { return false; } }

			/// <summary>
			/// Parameters.
			/// </summary>
			public ParameterInfo[]/*!!*/ RealParameters { get { return parameters; } }
			private readonly ParameterInfo[]/*!!*/ parameters;

			/// <summary>
			/// The method implementing the overload.
			/// </summary>
			public MethodInfo/*!*/ Method { get { return method; } }
			private readonly MethodInfo/*!*/ method;

			/// <summary>
			/// Mandatory PHP parameters count. 
			/// Auxiliary parameters such are the defined variable table and "params" array are not included.
			/// Example:
			///   f(VarTable, p1, p2, params[])  -> 2
			/// </summary>
			public override int MandatoryParamCount { get { return paramCount; } }
			public override int ParamCount { get { return paramCount; } }
			public override int GenericParamCount { get { return 0; } }
			public override int MandatoryGenericParamCount { get { return 0; } }

			private readonly short paramCount;

			/// <summary>
			/// Additional flags.
			/// </summary>
			public OverloadFlags Flags { get { return flags; } }
			private readonly OverloadFlags flags;

			private Overload(MethodInfo/*!*/ method, ParameterInfo[]/*!!*/ parameters, short paramCount, OverloadFlags flags)
			{
				Debug.Assert(method != null && parameters != null);

				this.method = method;
				this.paramCount = paramCount;
				this.flags = flags;
				this.parameters = parameters;
			}

			/// <summary>
			/// Creates an overload of the function. May return <B>null</B> on error.
			/// </summary>
			internal static Overload Create(MethodInfo/*!*/ realOverload, FunctionImplOptions options)
			{
				ParameterInfo[] parameters = realOverload.GetParameters();

				OverloadFlags flags = OverloadFlags.None;
				short param_count = (short)parameters.Length;

                if (parameters.Length >= 1 && parameters[0].ParameterType == Types.ScriptContext[0])
                {
                    flags |= OverloadFlags.NeedsScriptContext;
                    param_count--;
                }

				if (parameters.Length > 0 && parameters[parameters.Length - 1].IsDefined(typeof(ParamArrayAttribute), false))
				{
					flags |= OverloadFlags.IsVararg;
					param_count--;
				}

				if ((options & FunctionImplOptions.NeedsThisReference) != 0)
				{
					param_count--;
					flags |= OverloadFlags.NeedsThisReference;
				}

				if ((options & FunctionImplOptions.NeedsVariables) != 0)
				{
					param_count--;
					flags |= OverloadFlags.NeedsVariables;
				}

				if ((options & FunctionImplOptions.NeedsNamingContext) != 0)
				{
					param_count--;
					flags |= OverloadFlags.NeedsNamingContext;
				}

                if ((options & FunctionImplOptions.NeedsClassContext) != 0)
                {
                    param_count--;
                    flags |= OverloadFlags.NeedsClassContext;
                }

                if ((options & FunctionImplOptions.NeedsLateStaticBind) != 0)
                {
                    param_count--;
                    flags |= OverloadFlags.NeedsLateStaticBind;
                }

                if ((options & FunctionImplOptions.NotSupported) != 0)
                {
                    flags |= OverloadFlags.NotSupported;
                }

				return new Overload(realOverload, parameters, param_count, flags);
			}

			public override bool AliasReturn
			{
				get
				{
					return method.ReturnParameter.ParameterType == Types.PhpReference[0];
				}
			}

			public override DType GetTypeHint(int index)
			{
				return null;
			}

			public int GetHiddenParameterCount()
			{
				return
                    ((flags & OverloadFlags.NeedsScriptContext) != 0 ? 1 : 0) +
                    ((flags & OverloadFlags.NeedsThisReference) != 0 ? 1 : 0) + 
                    ((flags & OverloadFlags.NeedsVariables) != 0 ? 1 : 0) +
					((flags & OverloadFlags.NeedsNamingContext) != 0 ? 1 : 0) +
                    ((flags & OverloadFlags.NeedsLateStaticBind) != 0 ? 1 : 0) +
                    ((flags & OverloadFlags.NeedsClassContext) != 0 ? 1 : 0);
			}
            
            /// <summary>
			/// Returns <B>true</B> if the <c>index</c>-th formal parameter of the PHP signature is by-ref.
			/// </summary>
			public override bool IsAlias(int index)
			{
				if (index < paramCount)
				{
					// skips the auxiliary parameters (storing defined variables or eval info):
					int real_index = index + GetHiddenParameterCount();

					Type type = RealParameters[real_index].ParameterType;
					return type.IsByRef || type == Types.PhpReference[0];
				}
				else if ((flags & OverloadFlags.IsVararg) != 0)
				{
					Debug.Assert(RealParameters.Length > 0);
					return RealParameters[RealParameters.Length - 1].ParameterType == Types.PhpReferenceArray[0];
				}
				else
				{
					return true;
				}
			}
		}

		#endregion

		#region Properties

		public override bool IsLambda { get { return false; } }
		public override bool IsIdentityDefinite { get { return true; } }

		/// <summary>
		/// Library function does deep-copy according to the <see cref="PhpDeepCopyAttribute"/>.
		/// The call itself emits the deep-copy, so the outer code needn't to care.
		/// </summary>
		public override bool ReturnValueDeepCopyEmitted { get { return true; } }

		public override Name Name { get { return name; } }
		private readonly Name name;

		/// <summary>
		/// Options associated with the function (all overloads have to have the same options associated).
		/// </summary>
		public FunctionImplOptions Options { get { return options; } }
		private readonly FunctionImplOptions options;

		public List<Overload>/*!!*/ Overloads { get { return overloads; } }
		private readonly List<Overload>/*!!*/ overloads;

		#endregion

		#region Construction

		/// <summary>
		/// Used by full-reflect.
		/// </summary>
		public PhpLibraryFunction(PhpLibraryFunctionDesc/*!*/ functionDesc, Name name, FunctionImplOptions options,
			int estimatedOverloadCount)
			: base(functionDesc)
		{
			this.name = name;
			this.options = options;
			this.overloads = new List<Overload>(estimatedOverloadCount);
		}

		#endregion

		public override string GetFullName()
		{
			return name.Value;
		}

		#region Analysis Helpers

		public override RoutineProperties GetCallerRequirements()
		{
			RoutineProperties result = RoutineProperties.None;

			if ((options & FunctionImplOptions.NeedsFunctionArguments) != 0)
				result |= RoutineProperties.UseVarArgs;

			if ((options & FunctionImplOptions.NeedsVariables) != 0)
				result |= RoutineProperties.ContainsLocalsWorker;

            if ((options & FunctionImplOptions.NeedsLateStaticBind) != 0)
                result |= RoutineProperties.LateStaticBinding;

			return result;
		}

		#endregion

		#region Overloads

		public override RoutineSignature/*!*/ GetSignature(int overloadIndex)
		{
			Debug.Assert(overloadIndex >= 0 && overloadIndex < overloads.Count);
			return overloads[overloadIndex];
		}

		internal int AddOverload(MethodInfo/*!*/ realOverload, out Overload overload)
		{
			int index = AddOverload(overloads, realOverload, options);
			overload = (index >= 0) ? overloads[index] : null;
			return index;
		}

		internal static int AddOverload(List<Overload>/*!!*/ overloads, MethodInfo/*!*/ realOverload, FunctionImplOptions options)
		{
			Overload overload = Overload.Create(realOverload, options);
			if (overload == null) return DRoutine.InvalidOverloadIndex;

			int i = 0;
			while (i < overloads.Count && overloads[i].ParamCount < overload.ParamCount) i++;

			overloads.Insert(i, overload);

			return i;
		}

		internal override int ResolveOverload(Analyzer/*!*/ analyzer, CallSignature callSignature, Position position,
			out RoutineSignature overloadSignature)
		{
			if (callSignature.GenericParams.Count > 0)
			{
				analyzer.ErrorSink.Add(Errors.GenericCallToLibraryFunction, analyzer.SourceUnit, position);
				callSignature = new CallSignature(callSignature.Parameters, TypeRef.EmptyList);
			}

			bool exact_match;
			int result = ResolveOverload(callSignature.Parameters.Count, out exact_match);

			if (!exact_match)
			{
				// library function with wrong number of actual arguments:
				analyzer.ErrorSink.Add(Errors.InvalidArgumentCountForFunction, analyzer.SourceUnit, position, FullName);
			}

			overloadSignature = overloads[result];
			return result;
		}

		/// <summary>
		/// Finds an overload whose parameter count matches the specified one.
		/// </summary>
		/// <param name="argumentCount">The number of parameters.</param>
		/// <param name="exactMatch">Whether the exactly required overload has been found.</param>
		/// <returns>The resulting overload index (always valid).</returns>
		/// <remarks>
		/// If the last overload (when sorted by parameter counts) has <see cref="OverloadFlags"/> 
		/// flag set and <paramref name="argumentCount"/> is greater than this overload's parameter count
		/// then the last overload is returned. If there is no exactly matching overload returns first 
        /// which has more arguments than specified by <paramref name="argumentCount"/>.
		/// </remarks>
		private int ResolveOverload(int argumentCount, out bool exactMatch)
		{
			Debug.Assert(overloads.Count > 0);

			// searches for the exactly matching parameter count:
			int i = 0;
			while (i < overloads.Count && overloads[i].ParamCount < argumentCount) i++;

			// if requested parameter count is greater than any overload's one:
			if (i == overloads.Count)
			{
				exactMatch = (overloads[i - 1].Flags & OverloadFlags.IsVararg) != 0;
				return i - 1;
			}

			// if exactly matching overload has been found:
			exactMatch = overloads[i].ParamCount == argumentCount;

			// the "nearest greater" overload (having >= paramCount parameters):
			return i;
		}

		#endregion

		#region Emission

        internal override PhpTypeCode EmitCall(CodeGenerator/*!*/ codeGenerator, string fallbackQualifiedName, CallSignature callSignature,
            IPlace instance, bool runtimeVisibilityCheck, int overloadIndex, DType type, Position position,
            AccessType access, bool callVirt)
		{
			Overload overload = overloads[overloadIndex];
			Statistics.AST.AddLibraryFunctionCall(FullName, overload.ParamCount);

            if ((overload.Flags & OverloadFlags.NotSupported) != 0)
            {
                codeGenerator.IL.Emit(OpCodes.Ldstr, FullName);
                codeGenerator.IL.Emit(OpCodes.Call, Methods.PhpException.FunctionNotSupported_String);
                if (codeGenerator.Context.Config.Compiler.Debug)
                    codeGenerator.IL.Emit(OpCodes.Nop);

                return OverloadsBuilder.EmitLoadDefault(codeGenerator.IL, overload.Method);
            }

            //IPlace return_value;
            IPlace script_context = null;
			IPlace opt_arg_count = null;
            IPlace self_ref = null;
            IPlace rt_variables = null;
			IPlace naming_context = null;
            IPlace class_context = null;

            // 

			// captures eval info:
			if ((options & FunctionImplOptions.CaptureEvalInfo) != 0)
			{
				codeGenerator.EmitEvalInfoCapture(position.FirstLine, position.FirstColumn, false);
			}

            // current ScriptContext:
            if ((overload.Flags & OverloadFlags.NeedsScriptContext) != 0)
            {
                script_context = codeGenerator.ScriptContextPlace;
            }

			// number of optional arguments passed to a function (empty or a literal place):
			if ((overload.Flags & OverloadFlags.IsVararg) != 0)
			{
				opt_arg_count = new IndexedPlace(PlaceHolder.None, callSignature.Parameters.Count - overload.ParamCount);
			}

			// this reference?
			if ((options & FunctionImplOptions.NeedsThisReference) != 0)
			{
				self_ref = codeGenerator.SelfPlace;
			}

			// run-time variables table:
			if ((options & FunctionImplOptions.NeedsVariables) != 0)
			{
				rt_variables = codeGenerator.RTVariablesTablePlace;
			}

			// naming context
			if ((options & FunctionImplOptions.NeedsNamingContext) != 0)
			{
                naming_context =
                    (codeGenerator.SourceUnit.NamingContextFieldBuilder != null) ?
                        (IPlace)new Place(null, codeGenerator.SourceUnit.NamingContextFieldBuilder) : (IPlace)LiteralPlace.Null;
			}

            // call context
            if ((options & FunctionImplOptions.NeedsClassContext) != 0)
            {
                class_context = codeGenerator.TypeContextPlace;
            }

            // late static binding context
            if ((options & FunctionImplOptions.NeedsLateStaticBind) != 0)
            {
                Debug.Assert(class_context == null, "NeedsClassContext and NeedsLateStaticBind cannot be used concurently!");
                class_context = codeGenerator.LateStaticBindTypePlace;
            }

			OverloadsBuilder.ParameterLoader param_loader = new OverloadsBuilder.ParameterLoader(callSignature.EmitLibraryLoadArgument);
			OverloadsBuilder.ParametersLoader opt_param_loader = new OverloadsBuilder.ParametersLoader(callSignature.EmitLibraryLoadOptArguments);

			OverloadsBuilder builder = new OverloadsBuilder(
				codeGenerator.Context.Config.Compiler.Debug,
				null,                           // PHP stack is not used
				param_loader,                   // value parameter loader
				param_loader,                   // reference parameter loader
				opt_param_loader);              // optional parameter array loader

			// setups builder:
			builder.Aux = codeGenerator;
			builder.IL = codeGenerator.IL;
			builder.FunctionName = name;

			// emits overload call:
            Type/*!*/return_type = builder.EmitOverloadCall(overload.Method, overload.RealParameters, overload.ParamCount,
				script_context, rt_variables, naming_context, class_context, opt_arg_count, self_ref, access == AccessType.None);

            //if (return_value != null)
            //{
            //    // loads value on the stack:
            //    return_value.EmitLoad(codeGenerator.IL);

            //    return PhpTypeCodeEnum.FromType(return_value.PlaceType);
            //}
            if (return_type != Types.Void)
            {
                return PhpTypeCodeEnum.FromType(return_type);
            }
            else
            {
                if (codeGenerator.Context.Config.Compiler.Debug)
                {
                    codeGenerator.IL.Emit(OpCodes.Nop);
                }
                return PhpTypeCode.Void;
            }
		}


		#endregion
	}

	#endregion

	#region ClrMethod

	[DebuggerNonUserCode]
	public sealed class ClrMethod : KnownRoutine
	{
		#region Nested Type: Overload

		/// <summary>
		/// Additional overload flags.
		/// </summary>
		[Flags]
		public enum OverloadFlags : byte
		{
			/// <summary>
			/// None.
			/// </summary>
			None = 0,

			/// <summary>
			/// Overload has "params" array as its last argument.
			/// </summary>
			IsVararg = 1
		}

		public class Overload : RoutineSignature
		{
			public const int Invalid = -1;

			public override bool IsUnknown { get { return false; } }

			/// <summary>
			/// The method implementing the overload.
			/// </summary>
			public MethodBase/*!*/ Method { get { return method; } }
			private readonly MethodBase/*!*/ method;

			public ParameterInfo[]/*!!*/ Parameters { get { return parameters; } }
			private readonly ParameterInfo[]/*!!*/ parameters;

			public Type[]/*!!*/ GenericParameters { get { return genericParameters; } }
			private readonly Type[]/*!!*/ genericParameters;

			public OverloadFlags Flags { get { return flags; } }
			private OverloadFlags flags;

			public override bool AliasReturn
			{
				get { return false; }
			}

			public override bool IsAlias(int index)
			{
				return (index < parameters.Length) ? parameters[index].ParameterType.IsByRef : false;
			}

			public override DType/*!*/ GetTypeHint(int index)
			{
				Debug.Fail("Type hints are currently used only for PHP routine body emission");
				return null;
			}

			/// <summary>
			/// Mandatory parameter count. 
			/// Same as total parameter count as the default parameters are currently not supported.
			/// Differs from parameters.Length if the last argument is param-array in which case, the last parameter is not counted.
			/// </summary>
			public override int MandatoryParamCount { get { return paramCount; } }
			public override int ParamCount { get { return parameters.Length; } }
			private int paramCount;

			/// <summary>
			/// Mandatory type parameter count.
			/// Same as total parameter count as CLR doesn't use concept of default type parameters.
			/// Same as the number of genericParameters.Length.
			/// </summary>
			public override int MandatoryGenericParamCount { get { return genericParameters.Length; } }
			public override int GenericParamCount { get { return genericParameters.Length; } }

			public Overload(MethodBase/*!*/ method, Type[]/*!!*/ genericParameters, ParameterInfo[]/*!!*/ parameters)
			{
				this.method = method;
				this.parameters = parameters;
				this.genericParameters = genericParameters;

				flags = OverloadFlags.None;

				if (parameters.Length > 0 && parameters[parameters.Length - 1].IsDefined(typeof(ParamArrayAttribute), false))
					flags |= OverloadFlags.IsVararg;

				this.paramCount = parameters.Length - ((flags & OverloadFlags.IsVararg) != 0 ? 1 : 0);
			}

			internal static Overload Create(MethodBase/*!*/ realOverload)
			{
				if (realOverload.IsPrivate || realOverload.CallingConvention == CallingConventions.VarArgs)
					return null;

				ParameterInfo[] parameters = realOverload.GetParameters();

				foreach (ParameterInfo parameter in parameters)
				{
					if (parameter.ParameterType.IsPointer)
						return null;
				}

				Type[] generic_params;
				if (realOverload.IsGenericMethodDefinition)
					generic_params = realOverload.GetGenericArguments();
				else
					generic_params = Type.EmptyTypes;

				return new Overload(realOverload, generic_params, parameters);
			}

			/// <summary>
			/// Stringifies this signature for easier equality checks.
			/// </summary>
			public override string/*!*/ ToString()
			{
				MethodInfo mi = method as MethodInfo;
				Type return_type = (mi != null ? mi.ReturnType : Types.Void);

				return ClrSignatureToString(genericParameters.Length, parameters, return_type);
			}

			/// <summary>
			/// Stringifies a CLR method signature for easier equality checks.
			/// </summary>
			internal static string/*!*/ ClrSignatureToString(int genParamCount, ParameterInfo[]/*!*/ parameters,
				Type/*!*/ returnType)
			{
				StringBuilder sb = new StringBuilder(parameters.Length * 32);

				for (int i = 0; i < parameters.Length; i++)
				{
					Type type = parameters[i].ParameterType;
					if (type.IsGenericParameter && type.DeclaringMethod != null)
					{
						if (type.IsByRef) sb.Append(-(type.GenericParameterPosition + 1));
						else sb.Append(type.GenericParameterPosition);
					}
					else sb.Append(type.ToString());
					sb.Append(',');
				}

				if (returnType.IsGenericParameter && returnType.DeclaringMethod != null)
				{
					sb.Append(returnType.GenericParameterPosition);
				}
				else sb.Append(returnType.ToString());

				sb.Append('`');
				sb.Append(genParamCount);

				return sb.ToString();
			}

			/// <summary>
			/// Stringifies a CLR method signature for easier equality checks.
			/// </summary>
			/// <remarks>
			/// Elements of <paramref name="parameters"/> and <paramref name="returnType"/> are either <see cref="Type"/>s
			/// or integeres denoting generic method type parameter indexes. Negative index means byref parameter.
			/// </remarks>
			internal static string/*!*/ ClrSignatureToString(int genParamCount, object[]/*!*/ parameters, object/*!*/ returnType)
			{
				StringBuilder sb = new StringBuilder(parameters.Length * 32);

				for (int i = 0; i < parameters.Length; i++)
				{
					sb.Append(parameters[i].ToString());
					sb.Append(',');
				}
				sb.Append(returnType.ToString());

				sb.Append('`');
				sb.Append(genParamCount);

				return sb.ToString();
			}

			/// <summary>
			/// Returns parameters and return type remapped according to a constructed type.
			/// </summary>
			public ParameterInfo[]/*!*/ MakeConstructed(ConstructedType constructedType, out Type/*!*/ returnType)
			{
				MethodInfo method_info = method as MethodInfo;
				returnType = (method_info != null ? method_info.ReturnType : Types.Void);

				if (constructedType != null)
				{
					returnType = constructedType.MapRealType(returnType);

					ParameterInfo[] new_params = new ParameterInfo[parameters.Length];
					for (int i = 0; i < new_params.Length; i++)
					{
						ParameterInfo param_info = parameters[i];

						new_params[i] = new StubParameterInfo(
							param_info.Position,
							constructedType.MapRealType(param_info.ParameterType),
							param_info.Attributes,
							param_info.Name);
					}

					return new_params;
				}
				else
				{
					return parameters;
				}
			}
		}

		#endregion

		#region Properties

		public ClrMethodDesc/*!*/ ClrMethodDesc { get { return (ClrMethodDesc)memberDesc; } }

		public override bool IsLambda { get { return false; } }
		public override bool ReturnValueDeepCopyEmitted { get { return false; } }
		public override bool IsIdentityDefinite { get { return true; } }

		public override Name Name { get { return name; } }
		private readonly Name name;

		/// <summary>
		/// Array of overloads represented by the <see cref="ClrMethod"/>.
		/// Sorted by the number of mandatory parameters.
		/// Contrary to <see cref="PhpLibraryFunction"/>, there may be more overloads with the same parameter count.
		/// </summary>
		public List<Overload>/*!!*/ Overloads { get { return overloads; } }
		private readonly List<Overload>/*!!*/ overloads;

		public BitArray ArgCounts { get { return argCounts; } internal /* friend ClrOverloadBuilder */ set { argCounts = value; } }
		private BitArray argCounts;

		#endregion

		#region Construction

		/// <summary>
		/// Used by full-reflect.
		/// </summary>
		public ClrMethod(Name name, DTypeDesc/*!*/ declaringType, PhpMemberAttributes memberAttributes,
			int estimatedOverloadCount, bool isGeneric)
			: base(isGeneric ?
				new ClrGenericMethodDesc(declaringType, memberAttributes) :
				new ClrMethodDesc(declaringType, memberAttributes))
		{
			this.name = name;
			this.overloads = new List<Overload>(estimatedOverloadCount);
		}

		/// <summary>
		/// Used by full-reflect (<see cref="DTypeDesc"/>).
		/// </summary>
		internal static ClrMethod/*!*/ CreateConstructor(ClrTypeDesc/*!*/ declaringType)
		{
			ClrMethod result;

			if (declaringType is ClrDelegateDesc)
			{
				// the real constructor should not be accessible from PHP code
				result = new ClrMethod(Name.ClrCtorName, declaringType, PhpMemberAttributes.Constructor, 1, false);
				result.ClrMethodDesc.ArglessStub = new RoutineDelegate(declaringType._NoConstructorErrorStub);
			}
			else
			{
				ConstructorInfo[] realOverloads = declaringType.RealType.GetConstructors(
				BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

				//// TODO (less restrictive?):
				PhpMemberAttributes attrs = PhpMemberAttributes.Constructor;

				int visible_count = 0;
				for (int i = 0; i < realOverloads.Length; i++)
				{
					if (ClrTypeDesc.IsMethodVisible(realOverloads[i]))
						visible_count++;
				}

                if (declaringType.RealType.IsValueType) // add an empty .ctor
                    visible_count++;


				if (visible_count > 0)
				{
					result = new ClrMethod(Name.ClrCtorName, declaringType, attrs, visible_count, false);

					foreach (MethodBase real_overload in realOverloads)
					{
						if (ClrTypeDesc.IsMethodVisible(real_overload))
						{
							Overload overload;
							result.AddOverload(real_overload, out overload);
						}
					}

                    if (declaringType.RealType.IsValueType) // add an empty .ctor
                    {
                        Overload overload;
                        result.AddOverload(BuildDefaultValueCtor(declaringType.RealType), out overload);
                    }
				}
				else
				{
					result = new ClrMethod(Name.ClrCtorName, declaringType, PhpMemberAttributes.Constructor, 1, false);
					result.ClrMethodDesc.ArglessStub = new RoutineDelegate(declaringType._NoConstructorErrorStub);
				}
			}

			return result;
		}

		#endregion

		#region Utils

		public override string GetFullName()
		{
			return name.Value;
		}

		#endregion

		#region Analysis: Overloads

		public override RoutineSignature/*!*/ GetSignature(int overloadIndex)
		{
			return overloads[overloadIndex];
		}

		internal bool HasParameterlessOverload
		{
			get { return overloads.Count > 0 && overloads[0].MandatoryParamCount == 0; }
		}

		internal bool HasOverload
		{
			get { return overloads.Count > 0; }
		}

		/// <summary>
		/// Adds an overload by reflecting the specified real overload.
		/// Returns <see cref="DRoutine.InvalidOverloadIndex"/> and <B>null</B> in <paramref name="overload"/>
		/// if the reflection fails.
		/// </summary>
		internal int AddOverload(MethodBase/*!*/ realOverload, out Overload overload)
		{
			overload = Overload.Create(realOverload);
			if (overload == null) return DRoutine.InvalidOverloadIndex;

			int i = 0;
			int params_sum = overload.MandatoryParamCount + overload.ParamCount;
			while (i < overloads.Count && params_sum >= overloads[i].MandatoryParamCount + overloads[i].ParamCount)
			{
				// varargs (>= n params) are between (n params) and (n + 1 params)
				i++;
			}
			overloads.Insert(i, overload);

			// ClrMethod is static if at least one overload is static
			if (overload.Method.IsStatic) memberDesc.MemberAttributes |= PhpMemberAttributes.Static;

			// ClrMethod is public if at least one overload is public
			if (overload.Method.IsPublic)
			{
				memberDesc.MemberAttributes &= ~PhpMemberAttributes.VisibilityMask;
				memberDesc.MemberAttributes |= PhpMemberAttributes.Public;
			}

			// ClrMethod has a generic method desc if at least one overload is generic
			if (overload.GenericParamCount > 0 && !(memberDesc is ClrGenericMethodDesc))
			{
				memberDesc = new ClrGenericMethodDesc(memberDesc.DeclaringType, memberDesc.MemberAttributes);
			}

			return i;
		}

		internal override int ResolveOverload(Analyzer/*!*/ analyzer, CallSignature callSignature, Position position,
			out RoutineSignature/*!*/ overloadSignature)
		{
			if (overloads.Count == 0)
			{
				if (DeclaringType.TypeDesc is ClrDelegateDesc)
				{
					overloadSignature = UnknownSignature.Delegate;
					return 0;
				}

				// structures without ctor:
				if (DeclaringType.TypeDesc.RealType.IsValueType)
				{
					overloadSignature = UnknownSignature.Default;
					return 0;
				}

				Debug.Assert(this.IsConstructor, "Only constructors can have no overload.");
				overloadSignature = UnknownSignature.Default;
				return DRoutine.InvalidOverloadIndex;
			}

			int i = 0;
			bool found = false;

			Overload overload;
			while (i < overloads.Count && (overload = overloads[i]).MandatoryParamCount <= callSignature.Parameters.Count)
			{
				if (overload.MandatoryParamCount == callSignature.Parameters.Count ||
					(overload.Flags & OverloadFlags.IsVararg) != 0)
				{
					found = true;
					break;
				}

				i++;
			}

			// TODO: by type resolving
			// evaluate arguments?

			if (!found)
			{
				analyzer.ErrorSink.Add(Warnings.InvalidArgumentCountForMethod, analyzer.SourceUnit, position,
					this.DeclaringType.FullName, this.FullName);

				if (i > 0) i--;
				overloadSignature = overloads[i];
				return i;
			}

			overloadSignature = overloads[i];
			return i;
		}

		internal BitArray/*!*/ ResolveArgCounts()
		{
			if (argCounts != null)
				return argCounts;

			return null;

			// TODO:
			//int case_count = 1;
			//Overload previous = null;
			//foreach (Overload current in overloads)
			//{
			//  if (previous != null)
			//  {
			//    if (previous.MandatoryParamCount != current.MandatoryParamCount)
			//      case_count++;
			//  }
			//  else
			//  {
			//    this.minArgCount = current.MandatoryParamCount;
			//  }

			//  previous = current;
			//  overload_count++;
			//}

			//this.maxArgCount = previous.MandatoryParamCount;
		}

		#endregion

		#region Analysis: Overrides

		internal override void AddAbstractOverride(DMemberRef/*!*/ abstractMethod)
		{
			// nop, we don't need to maintain information about abstract overrides
		}

		#endregion

		#region Emission

        internal override PhpTypeCode EmitCall(CodeGenerator/*!*/ codeGenerator, string fallbackQualifiedName, CallSignature callSignature,
            IPlace instance, bool runtimeVisibilityCheck, int overloadIndex, DType type, Position position,
            AccessType access, bool callVirt)
		{
#if DEBUG_DYNAMIC_STUBS
			
			MethodBuilder mb = codeGenerator.IL.TypeBuilder.DefineMethod(DeclaringType.FullName + "::" + FullName,
				MethodAttributes.PrivateScope | MethodAttributes.Static, typeof(object), Types.Object_PhpStack);

			ILEmitter il = new ILEmitter(mb);
			IndexedPlace instance2 = new IndexedPlace(PlaceHolder.Argument, 0);
			IndexedPlace stack = new IndexedPlace(PlaceHolder.Argument, 1);

			EmitArglessStub(il, stack, instance2);
			
#endif
            Debug.Assert(instance == null || instance is ExpressionPlace || instance == IndexedPlace.ThisArg);
            Debug.Assert(fallbackQualifiedName == null);
			return codeGenerator.EmitRoutineOperatorCall(DeclaringType, ExpressionPlace.GetExpression(instance), this.FullName, null, null, callSignature, access);
		}

		/// <summary>
		/// Run-time argless-stub emission.
		/// </summary>
		internal void EmitArglessStub(ILEmitter/*!*/ il, IPlace/*!*/ stack, IPlace/*!*/ instance)
		{
			ClrOverloadBuilder builder = new ClrOverloadBuilder(il, this, null /* !!! TODO !!! */, stack, instance, false,
				new ClrOverloadBuilder.ParameterLoader(PhpStackBuilder.EmitValuePeekUnchecked),
				new ClrOverloadBuilder.ParameterLoader(PhpStackBuilder.EmitReferencePeekUnchecked));

			builder.EmitResolutionByNumber();
		}

        /// <summary>
        /// Emit static method that creates an instance of given value type using default ctor.
        /// </summary>
        /// <param name="valueType">Value type to be instantiated by resulting method.</param>
        /// <returns>The method that returns value boxed into new instance of <see cref="ClrValue&lt;T&gt;"/>.</returns>
        private static MethodBase BuildDefaultValueCtor(Type/*!*/valueType)
        {
            Debug.Assert(valueType != null && valueType.IsValueType, "Argument 'valueType' must not be null and must represent a value type!");

            DynamicMethod method = new DynamicMethod(valueType.Name + "..ctor", Types.Object[0], Type.EmptyTypes);
            Emit.ILEmitter il = new PHP.Core.Emit.ILEmitter(method);

            // local 0
            // ldloca.s 0
            // initobj <valueType>
            
            var loc = il.DeclareLocal(valueType);

            il.Emit(OpCodes.Ldloca_S, loc);
            il.Emit(OpCodes.Initobj, valueType);
            
            // LOAD <loc>
            // box ConvertToPhp <valueType>

            il.Emit(OpCodes.Ldloc, loc);
            il.EmitBoxing(ClrOverloadBuilder.EmitConvertToPhp(il, valueType));

            // return
            il.Emit(OpCodes.Ret);

            // done
            return method;
        }

		#endregion
	}

	#endregion

    #region PurePhpFunction

    /// <summary>
    /// Represents runtime global PHP function declared in &lt;Declare&gt; helper method.
    /// </summary>
    [DebuggerNonUserCode]
    public sealed class PurePhpFunction : PhpRoutine
    {
        #region Properties

        public override bool IsLambda { get { return false; } }
        public override bool ReturnValueDeepCopyEmitted { get { return false; } }
        public override bool IsIdentityDefinite { get { return true; } }

        public override Name Name { get { return name; } }
        private readonly Name name;

        public override bool IsFunction { get { return true; } }

        public override SourceUnit SourceUnit { get { throw new NotSupportedException(); } }
        public override Position Position { get { throw new NotSupportedException(); } }

        internal override bool IsExported { get { return false; } }

        public override string GetFullClrName() { return Name.Value; }

        #endregion

        #region Construction

        /// <summary>
        /// Used by full-reflect.
        /// </summary>
        public PurePhpFunction(PhpRoutineDesc/*!*/routine, string name, MethodInfo/*!*/argfull)
            : base(routine)
        {
            Debug.Assert(routine != null);
            Debug.Assert(argfull != null);

            this.name = new Name(name);
            this.argfull = argfull;
            this.signature = PhpRoutineSignature.FromArgfullInfo(this, argfull);
        }

        #endregion

        #region Utils

        public override string GetFullName()
        {
            return name.Value;
        }

        internal override int ResolveOverload(Analyzer analyzer, CallSignature callSignature, Position position, out RoutineSignature overloadSignature)
        {
            overloadSignature = signature;
            return 0;
        }

        #endregion

        
    }

    #endregion
}