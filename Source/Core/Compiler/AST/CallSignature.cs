/*

 Copyright (c) 2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Collections;

using PHP.Core.Emit;
using PHP.Core.Parsers;
using PHP.Core.Reflection;

namespace PHP.Core.AST
{
	#region ActualParam

	public sealed class ActualParam : LangElement
	{
		public Expression/*!*/ Expression { get { return expression; } }
		private Expression/*!*/ expression;

		private bool ampersand;

		public ActualParam(Position p, Expression param, bool ampersand)
			: base(p)
		{
			Debug.Assert(param != null);
			this.expression = param;
			this.ampersand = ampersand;
		}

		internal void Analyze(Analyzer/*!*/ analyzer, bool isBaseCtorCallConstrained)
		{
			// TODO: isBaseCtorCallConstrained

			ExInfoFromParent info = new ExInfoFromParent(this);

			analyzer.EnterActParam();

			if (analyzer.ActParamDeclIsUnknown())
			{
				// we don't know whether the parameter will be passed by reference at run-time:
				if (expression.AllowsPassByReference)
				{
					info.Access = AccessType.ReadUnknown;

					// Although we prepare to pass reference, value can be really passed.
					// That's why we report warning when user use '&' in calling, 
					// because it has no influence.
					if (ampersand)
						analyzer.ErrorSink.Add(Warnings.ActualParamWithAmpersand, analyzer.SourceUnit, position);
				}
				else
				{
					info.Access = AccessType.Read;
				}
			}
			else
			{
				if (analyzer.ActParamPassedByRef())
				{
					if (expression.AllowsPassByReference)
					{
						info.Access = AccessType.ReadRef;
					}
					else
					{
						analyzer.ErrorSink.Add(Errors.NonVariablePassedByRef, analyzer.SourceUnit, expression.Position);
						analyzer.LeaveActParam();
						return;
					}
				}
				else
				{
					info.Access = AccessType.Read;
					if (ampersand) analyzer.ErrorSink.Add(Warnings.ActualParamWithAmpersand, analyzer.SourceUnit, position);
				}
			}

			expression = expression.Analyze(analyzer, info).Literalize();

			analyzer.LeaveActParam();
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
		internal PhpTypeCode Emit(CodeGenerator/*!*/ codeGenerator)
		{
			codeGenerator.ChainBuilder.Create();
			PhpTypeCode result = expression.Emit(codeGenerator);
			codeGenerator.ChainBuilder.End();

			return result;
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitActualParam(this);
        }
	}

	#endregion

	#region NamedActualParam

	public sealed class NamedActualParam : LangElement
	{
		public Expression/*!*/ Expression { get { return expression; } }
		private Expression/*!*/ expression;

		public VariableName Name { get { return name; } }
		private VariableName name;

		internal DProperty Property { get { return property; } }
		private DProperty property;

		public NamedActualParam(Position position, string name, Expression/*!*/ expression)
			: base(position)
		{
			this.name = new VariableName(name);
			this.expression = expression;
		}

		internal void Analyze(Analyzer/*!*/ analyzer, DType/*!*/ propertiesDeclarer)
		{
			// TODO: Named parameters can target the non-static, public, and read-write fields 
			// or properties of the attribute class

			bool visibility_check;

			if (!propertiesDeclarer.IsUnknown)
			{
				property = analyzer.ResolveProperty(propertiesDeclarer, name, position, false, null, null, out visibility_check);
			}

			expression = expression.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitNamedActualParam(this);
        }
	}

	#endregion

	#region CallSignature

	public struct CallSignature
	{
		/// <summary>
		/// List of actual parameters (<see cref="ActualParam"/> nodes).
		/// </summary>	
		public List<ActualParam>/*!*/ Parameters { get { return parameters; } }
		private readonly List<ActualParam>/*!*/ parameters;

		/// <summary>
		/// List of generic parameters.
		/// </summary>
		public List<TypeRef>/*!*/ GenericParams { get { return genericParams; } }
		private readonly List<TypeRef>/*!*/ genericParams;

        /// <summary>
        /// Gets true if all the Parameters (after the analysis) have the value and could be evaluated during the compilation time.
        /// </summary>
        public bool AllParamsHaveValue
        {
            get
            {
                foreach (var p in Parameters)
                    if (!p.Expression.HasValue)
                        return false;

                return true;
            }
        }

        /// <summary>
        /// Initialize new instance of <see cref="CallSignature"/>.
        /// </summary>
        /// <param name="parameters">List of parameters.</param>
        /// <param name="genericParams">List of type parameters for generics.</param>
        public CallSignature(List<ActualParam>/*!*/ parameters, List<TypeRef>/*!*/ genericParams)
		{
			Debug.Assert(parameters != null && genericParams != null);

			this.parameters = parameters;
			this.genericParams = genericParams;
		}

        /// <summary>
        /// Builds <see cref="Expression"/> that creates <see cref="PhpArray"/> with call signature parameters.
        /// </summary>
        /// <returns></returns>
        public ArrayEx/*!*/BuildPhpArray()
        {
            Debug.Assert(this.genericParams == null || this.genericParams.Count == 0);

            List<Item> arrayItems = new List<Item>(this.parameters.Count);
            Position pos = Position.Invalid;

            foreach (var p in this.parameters)
            {
                arrayItems.Add(new ValueItem(null, p.Expression));
                if (pos.IsValid) pos = p.Position;
                else
                {
                    pos.LastColumn = p.Position.LastColumn;
                    pos.LastLine = p.Position.LastLine;
                    pos.LastOffset = p.Position.LastOffset;
                }
            }

            return new ArrayEx(pos, arrayItems);
        }

		internal void Analyze(Analyzer/*!*/ analyzer, RoutineSignature/*!*/ signature, ExInfoFromParent info,
			bool isBaseCtorCallConstrained)
		{
			// generic:

			for (int i = 0; i < genericParams.Count; i++)
				genericParams[i].Analyze(analyzer);

			// regular:

			analyzer.EnterActualParams(signature, parameters.Count);

			for (int i = 0; i < parameters.Count; i++)
				parameters[i].Analyze(analyzer, isBaseCtorCallConstrained);

			analyzer.LeaveActualParams();
		}

        #region Emission

		/// <summary>
		/// Emits IL instructions that load actual parameters and optionally add a new stack frame to
		/// current <see cref="PHP.Core.ScriptContext.Stack"/>.
		/// </summary>
		/// <param name="codeGenerator">Code generator.</param>
		/// <remarks>
		/// Nothing is expected on the evaluation stack. Nothing is left on the evaluation stack.
		/// </remarks>
		internal void EmitLoadOnPhpStack(CodeGenerator/*!*/ codeGenerator)
		{
			List<ActualParam> parameters = this.parameters;
			List<TypeRef> genericParams = this.genericParams;

			PhpStackBuilder.EmitAddFrame(codeGenerator.IL, codeGenerator.ScriptContextPlace, genericParams.Count, parameters.Count,
			  delegate(ILEmitter il, int i)
			  {
				  // generic arguments:
				  genericParams[i].EmitLoadTypeDesc(codeGenerator, ResolveTypeFlags.UseAutoload | ResolveTypeFlags.ThrowErrors);
			  },
			  delegate(ILEmitter il, int i)
			  {
				  // regular arguments:
				  codeGenerator.EmitBoxing(parameters[i].Emit(codeGenerator));
			  }
			);
		}

		/// <summary>
		/// Emits IL instructions that load actual parameters on the evaluation stack.
		/// </summary>
		/// <param name="codeGenerator">Code generator.</param>
		/// <param name="routine">PHP method being called.</param>
		/// <remarks>
		/// <para>
		/// The function has mandatory and optional formal arguments.
		/// Mandatory arguments are those formal arguments which are not preceded by 
		/// any formal argument with default value. The others are optional.
		/// If a formal argument without default value is declared beyond the last mandatory argument
		/// it is treated as optional one by the caller. The callee checks this and throws warning.
		/// </para>
		/// Missing arguments handling:
		/// <list type="bullet">
		///   <item>missing mandatory argument - WARNING; LOAD(null);</item>
		///   <item>missing optional argument - LOAD(Arg.Default);</item>
		///   <item>superfluous arguments are ignored</item>
		/// </list>
		/// </remarks>
		internal void EmitLoadOnEvalStack(CodeGenerator/*!*/ codeGenerator, PhpRoutine/*!*/ routine)
		{
			EmitLoadTypeArgsOnEvalStack(codeGenerator, routine);
			EmitLoadArgsOnEvalStack(codeGenerator, routine);
		}

		internal void EmitLoadTypeArgsOnEvalStack(CodeGenerator/*!*/ codeGenerator, PhpRoutine/*!*/ routine)
		{
			ILEmitter il = codeGenerator.IL;

			int mandatory_count = (routine.Signature != null) ? routine.Signature.MandatoryGenericParamCount : 0;
			int formal_count = (routine.Signature != null) ? routine.Signature.GenericParamCount : 0;
			int actual_count = genericParams.Count;

			// loads all actual parameters which are not superfluous:
			for (int i = 0; i < Math.Min(actual_count, formal_count); i++)
				genericParams[i].EmitLoadTypeDesc(codeGenerator, ResolveTypeFlags.UseAutoload | ResolveTypeFlags.ThrowErrors);

			// loads missing mandatory arguments:
			for (int i = actual_count; i < mandatory_count; i++)
			{
				// CALL PhpException.MissingTypeArgument(<i+1>,<name>);
				il.LdcI4(i + 1);
				il.Emit(OpCodes.Ldstr, routine.FullName);
				codeGenerator.EmitPhpException(Methods.PhpException.MissingTypeArgument);

				// LOAD DTypeDesc.ObjectTypeDesc;
				il.Emit(OpCodes.Ldsfld, Fields.DTypeDesc.ObjectTypeDesc);
			}

			// loads missing optional arguments:
			for (int i = Math.Max(mandatory_count, actual_count); i < formal_count; i++)
			{
				// LOAD Arg.DefaultType;
				il.Emit(OpCodes.Ldsfld, Fields.Arg_DefaultType);
			}
		}

		internal void EmitLoadArgsOnEvalStack(CodeGenerator/*!*/ codeGenerator, PhpRoutine/*!*/ routine)
		{
			ILEmitter il = codeGenerator.IL;

			int mandatory_count = (routine.Signature != null) ? routine.Signature.MandatoryParamCount : 0;
			int formal_count = (routine.Signature != null) ? routine.Signature.ParamCount : 0;
			int actual_count = parameters.Count;
			PhpTypeCode param_type;

			// loads all actual parameters which are not superfluous:
			for (int i = 0; i < Math.Min(actual_count, formal_count); i++)
			{
				codeGenerator.EmitBoxing(param_type = parameters[i].Emit(codeGenerator));

				// Actual param emitter should emit "boxing" to a reference if its access type is ReadRef.
				// That's why no operation is needed here and references should match.
				Debug.Assert((routine.Signature == null || routine.Signature.IsAlias(i)) == (param_type == PhpTypeCode.PhpReference));
			}

			// loads missing mandatory arguments:
			for (int i = actual_count; i < mandatory_count; i++)
			{
				// CALL PhpException.MissingArgument(<i+1>,<name>);
				il.LdcI4(i + 1);
				il.Emit(OpCodes.Ldstr, routine.FullName);
				codeGenerator.EmitPhpException(Methods.PhpException.MissingArgument);

				// LOAD null;
				if (routine.Signature.IsAlias(i))
					il.Emit(OpCodes.Newobj, Constructors.PhpReference_Void);
				else
					il.Emit(OpCodes.Ldnull);
			}

			// loads missing optional arguments:
			for (int i = Math.Max(mandatory_count, actual_count); i < formal_count; i++)
			{
				// LOAD Arg.Default;
				il.Emit(OpCodes.Ldsfld, Fields.Arg_Default);
			}
		}

		/// <summary>
		/// Emits parameter loading.
		/// </summary>
		/// <param name="il">Emitter.</param>
		/// <param name="index">The index of the parameter starting from 0.</param>
		/// <param name="codeGenerator">Code generator.</param>
		/// <returns>The type of the actual argument or its value if it is a leteral.</returns>
		internal object EmitLibraryLoadArgument(ILEmitter/*!*/ il, int index, object/*!*/ codeGenerator)
		{
			Debug.Assert(codeGenerator != null);
			Debug.Assert(index < parameters.Count, "Missing arguments prevents code generation");
			
			// returns value if the parameter is evaluable at compile time:
			if (parameters[index].Expression.HasValue)
				return parameters[index].Expression.Value;

			// emits parameter evaluation:
			return PhpTypeCodeEnum.ToType(parameters[index].Emit((CodeGenerator)codeGenerator));
		}

		/// <summary>
		/// Emits load of optional parameters array on the evaluation stack.
		/// </summary>
		/// <param name="builder">An overloads builder.</param>
		/// <param name="start">An index of the first optional parameter to be loaded into the array (indices start from 0).</param>
		/// <param name="param">
		/// A <see cref="ParameterInfo"/> of the formal parameter of the target method where the array will be passed.
		/// This information influences conversions all optional parameters.
		/// </param>
		/// <param name="optArgCount">Optional argument count (unused).</param>
		internal void EmitLibraryLoadOptArguments(OverloadsBuilder/*!*/ builder, int start, ParameterInfo/*!*/ param, IPlace optArgCount)
		{
			Debug.Assert(start >= 0 && builder != null && param != null && builder.Aux is CodeGenerator);

			ILEmitter il = builder.IL;
			Type elem_type = param.ParameterType.GetElementType();
			Type array_type = elem_type.MakeArrayType();

			LocalBuilder loc_array = il.DeclareLocal(array_type);
			LocalBuilder loc_item = il.DeclareLocal(elem_type);

			// NEW <alem_type>[<parameters count - start>]
			il.LdcI4(parameters.Count - start);
			il.Emit(OpCodes.Newarr, elem_type);
			il.Stloc(loc_array);

			// loads each optional parameter into the appropriate bucket of the array:
			for (int i = start; i < parameters.Count; i++)
			{
				// item = <parameter value>;
				object type_or_value = EmitLibraryLoadArgument(il, i, builder.Aux);
				builder.EmitArgumentConversion(elem_type, type_or_value, false, param);
				il.Stloc(loc_item);

				// array[<i-start>] = item;
				il.Ldloc(loc_array);
				il.LdcI4(i - start);
				il.Ldloc(loc_item);
				il.Stelem(elem_type);
			}

			// loads the array:
			il.Ldloc(loc_array);
		}

		#endregion
	}

	#endregion
}
