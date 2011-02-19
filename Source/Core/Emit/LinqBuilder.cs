/*

 Copyright (c) 2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Collections;

using PHP.Core;
using PHP.Core.Emit;
using PHP.Core.AST.Linq;

namespace PHP.Core.Emit
{
	#region Builder

	internal class LinqBuilder
	{
		private const string ContextTypeName = "LinqContext$";

		public ILEmitter IL { get { return cg.IL; } }

		private CodeGenerator/*!*/ cg;
		public CodeGenerator/*!*/ CodeGenerator { get { return cg; } }

		private TypeBuilder linqContextBuilder;
		private ConstructorInfo linqContextCtor;
		private LocalBuilder linqContextLocal;

		private IPlace/*!*/ rtVariablesPlace;
		private IPlace/*!*/ scriptContextPlace;
		private IPlace/*!*/ classContextPlace;
		private IPlace/*!*/ selfPlace;

		private int inLambda;

		public LinqBuilder(CodeGenerator/*!*/ cg)
		{
			this.cg = cg;

			IPlace this_place = new IndexedPlace(PlaceHolder.Argument, 0);

			this.rtVariablesPlace = new Place(this_place, Fields.LinqContext_variables);
			this.scriptContextPlace = new Place(this_place, Fields.LinqContext_context);
			this.classContextPlace = new Place(this_place, Fields.LinqContext_typeHandle);
			this.selfPlace = new Place(this_place, Fields.LinqContext_outerType);
		}

		public void DefineContextType()
		{
			linqContextBuilder = cg.IL.TypeBuilder.DefineNestedType(ContextTypeName + cg.IL.GetNextUniqueIndex(),
				TypeAttributes.Class | TypeAttributes.NestedPrivate | TypeAttributes.Sealed,
				typeof(PHP.Core.LinqContext), null);

			// .ctor:

			ConstructorBuilder ctor = linqContextBuilder.DefineConstructor(MethodAttributes.Assembly,
				CallingConventions.HasThis, Types.LinqContextArgs);

			ILEmitter il = new ILEmitter(ctor);
			il.Ldarg(0);
			il.Ldarg(1);
			il.Ldarg(2);
			il.Ldarg(3);
			il.Ldarg(4);
			il.Emit(OpCodes.Call, Constructors.LinqContext);
			il.Emit(OpCodes.Ret);

			linqContextCtor = ctor;
		}

		public MethodInfo EmitLambda(string name, AST.DirectVarUse variable, AST.Expression/*!*/ expression,
			PhpTypeCode returnType)
		{
			MethodBuilder result = linqContextBuilder.DefineMethod(name, MethodAttributes.PrivateScope | MethodAttributes.SpecialName,
				PhpTypeCodeEnum.ToType(returnType), new Type[] { typeof(object) });

			ILEmitter il = new ILEmitter(result);

			EnterLambdaDeclaration(il);

			if (variable != null)
			{
				// <variable> = COPY(<argument>);
				variable.Emit(cg);
				il.Emit(OpCodes.Ldarg_1);
				cg.EmitVariableCopy(CopyReason.Assigned, null);
				variable.EmitAssign(cg);
			}

			cg.EmitConversion(expression, returnType);
			il.Emit(OpCodes.Ret);

			LeaveLambdaDeclaration();

			return result;
		}

		public void EmitNewLinqContext()
		{
			ILEmitter il = cg.IL;

			linqContextLocal = il.DeclareLocal(linqContextBuilder);

			// linq_context = NEW <linq context>(<this>, <variables>, <script context>, <type desc>)
			cg.SelfPlace.EmitLoad(il);
			cg.RTVariablesTablePlace.EmitLoad(il);
			cg.ScriptContextPlace.EmitLoad(il);
			cg.TypeContextPlace.EmitLoad(il);
			il.Emit(OpCodes.Newobj, linqContextCtor);
			il.Stloc(linqContextLocal);
		}

		public void EmitLoadLinqContext()
		{
			if (inLambda > 0)
				cg.IL.Emit(OpCodes.Ldarg_0);
			else
				cg.IL.Ldloc(linqContextLocal);
		}

		private void EnterLambdaDeclaration(ILEmitter il)
		{
			inLambda++;
			cg.EnterLambdaDeclaration(il, false, rtVariablesPlace, scriptContextPlace, classContextPlace, selfPlace);
		}

		private void LeaveLambdaDeclaration()
		{
			cg.LeaveFunctionDeclaration();
			inLambda--;
		}

		public void BakeContextType()
		{
			linqContextBuilder.CreateType();
		}

		#region Indexes for Lambda Functions

		private int selectorCount = 0;
		private int comparerCount = 0;
		private int predicateCount = 0;
		private int multiSelectorCount = 0;

		internal int GetNextSelectorNum()
		{
			return selectorCount++;
		}

		internal int GetNextComparerNum()
		{
			return comparerCount++;
		}

		internal int GetNextPredicateNum()
		{
			return predicateCount++;
		}

		internal int GetNextMultiSelectorNum()
		{
			return multiSelectorCount++;
		}

		#endregion
	}

	#endregion
}
