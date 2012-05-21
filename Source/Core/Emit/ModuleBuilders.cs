/*

 Copyright (c) 2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;

using PHP.Core;
using PHP.Core.Reflection;

namespace PHP.Core.Emit
{
	public interface IPhpModuleBuilder
	{
		PhpAssemblyBuilderBase/*!*/ AssemblyBuilder { get; }
		ILEmitter CreateGlobalCodeEmitter();

		TypeBuilder/*!*/ DefineRealType(string/*!*/ fullName, TypeAttributes attributes);
		MethodInfo/*!*/ DefineRealFunction(string/*!*/ name, MethodAttributes attributes, Type/*!*/ returnType, Type[]/*!*/ parameterTypes);
	}

	#region TransientModuleBuilder

	/// <summary>
	/// Provides means for building transient modules.
	/// </summary>
	public sealed class TransientModuleBuilder : TransientModule, IPhpModuleBuilder
	{
		PhpAssemblyBuilderBase/*!*/ IPhpModuleBuilder.AssemblyBuilder { get { return assemblyBuilder; } }

		public TransientAssemblyBuilder/*!*/ AssemblyBuilder { get { return assemblyBuilder; } }
		private readonly TransientAssemblyBuilder/*!*/ assemblyBuilder;

		private MethodInfo mainMethod;
		private TypeBuilder globalBuilder;

		internal TransientModuleBuilder(int id, EvalKinds kind, TransientCompilationUnit/*!*/ compilationUnit,
			TransientAssemblyBuilder/*!*/ assemblyBuilder, TransientModule containingModule, string sourcePath)
            : base(id, kind, compilationUnit, assemblyBuilder.TransientAssembly, containingModule, sourcePath)
		{
			this.assemblyBuilder = assemblyBuilder;

			if (!compilationUnit.IsDynamic)
			{
				this.globalBuilder = assemblyBuilder.RealModuleBuilder.DefineType(MakeName("<Global>", true),
					TypeAttributes.SpecialName | TypeAttributes.Class | TypeAttributes.Public);
			}
			else
			{
				this.globalBuilder = null;
			}
		}

		public ILEmitter/*!*/ CreateGlobalCodeEmitter()
		{
			string name = MakeName(ScriptModule.MainHelperName, true);

			if (globalBuilder != null)
			{
				mainMethod = globalBuilder.DefineMethod(name,
					MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName,
					Types.Object[0], ScriptModule.MainHelperArgTypes);
			}
			else
			{
				mainMethod = new DynamicMethod(name, PhpFunctionUtils.DynamicStubAttributes,
					CallingConventions.Standard, Types.Object[0], ScriptModule.MainHelperArgTypes, DynamicCode.DynamicMethodType, false);
			}

			return new ILEmitter(mainMethod);
		}

		public TypeBuilder/*!*/ DefineRealType(string/*!*/ fullName, TypeAttributes attributes)
		{
			TypeBuilder result = assemblyBuilder.RealModuleBuilder.DefineType(
				this.MakeName(fullName, false), attributes);

			if ((attributes & TypeAttributes.Interface) == 0)
			{
				// mark type with eval-id (used by stack tracer):
				result.SetCustomAttribute(new CustomAttributeBuilder(Constructors.PhpEvalId, new object[] { this.Id }));
			}

			return result;
		}

		public MethodInfo/*!*/ DefineRealFunction(string/*!*/ name, MethodAttributes attributes, Type/*!*/ returnType, Type[]/*!*/ parameterTypes)
		{
			Debug.Assert((attributes & MethodAttributes.Static) != 0, "Only static functions can be defined by DefineRealFunction");

			// encode specialname attribute to the name (DM doesn't support it directly):
			name = MakeName(name, (attributes & MethodAttributes.SpecialName) != 0);

			if (globalBuilder != null)
			{
				return globalBuilder.DefineMethod(name, attributes, returnType, parameterTypes);
			}
			else
			{
				return new DynamicMethod(name, PhpFunctionUtils.DynamicStubAttributes, CallingConventions.Standard, returnType, parameterTypes,
					DynamicCode.DynamicMethodType, false);
			}
		}

		internal void Bake()
		{
			if (globalBuilder != null)
			{
				Type baked = globalBuilder.CreateType();
				mainMethod = baked.GetMethod(mainMethod.Name, BindingFlags.Public | BindingFlags.Static);
				this.main = (MainRoutineDelegate)Delegate.CreateDelegate(typeof(MainRoutineDelegate), mainMethod);
			}
			else
			{
				this.main = (MainRoutineDelegate)((DynamicMethod)mainMethod).CreateDelegate(typeof(MainRoutineDelegate));
			}
		}
	}

	#endregion

	#region ScriptBuilder

	/// <summary>
	/// Provides means for building scripts.
	/// </summary>
	public sealed partial class ScriptBuilder 
	{
		#region Statics

		/// <summary>
		/// An index of "context" argument in all helpers.
		/// </summary>
		public const int ArgContext = 0;

		/// <summary>
		/// An index of "variables" argument in all helpers.
		/// </summary>
		public const int ArgVariables = 1;

		/// <summary>
		/// An index of "self" argument used in some helpers.
		/// </summary>
		public const int ArgSelf = 2;

		/// <summary>
		/// An index of "includer" argument used in some helpers.
		/// </summary>
		public const int ArgIncluder = 3;

		/// <summary>
		/// An index of "isMain" argument in Main helper.
		/// </summary>
		public const int ArgIsMain = 4;

		#endregion
	}

	#endregion
}
