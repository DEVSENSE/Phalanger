/*

 Copyright (c) 2004-2006 Tomas Matousek, Vaclav Novak and Martin Maly.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using PHP.Core.Emit;
using PHP.Core.Parsers;
using PHP.Core.Reflection;

namespace PHP.Core.AST
{
	#region IncludingEx

	/// <summary>
	/// Inclusion expression (include, require, synthetic auto-inclusion nodes).
	/// </summary>
	public sealed partial class IncludingEx : Expression
	{
		internal override Operations Operation { get { return Operations.Inclusion; } }

		/// <summary>
		/// An argument of the inclusion.
		/// </summary>
		public Expression/*!*/ Target { get { return fileNameEx; } }
		private Expression/*!*/ fileNameEx;

		/// <summary>
		/// A type of an inclusion (include, include-once, ...).
		/// </summary>
		public InclusionTypes InclusionType { get { return inclusionType; } }
		private InclusionTypes inclusionType;

		/// <summary>
		/// Whether the inclusion is conditional.
		/// </summary>
		public bool IsConditional { get { return isConditional; } }
		private bool isConditional;

		public Scope Scope { get { return scope; } }
		private Scope scope;

		public SourceUnit/*!*/ SourceUnit { get { return sourceUnit; } }
		private SourceUnit/*!*/ sourceUnit;

		public IncludingEx(SourceUnit/*!*/ sourceUnit, Scope scope, bool isConditional, Position position,
			InclusionTypes inclusionType, Expression/*!*/ fileName)
			: base(position)
		{
			Debug.Assert(fileName != null);

			this.inclusionType = inclusionType;
			this.fileNameEx = fileName;
			this.scope = scope;
			this.isConditional = isConditional;
			this.sourceUnit = sourceUnit;
		}


		/// <summary>
		/// Emits dynamic inclusion.
		/// </summary>
		private PhpTypeCode EmitDynamicInclusion(CodeGenerator/*!*/ codeGenerator)
		{
			// do not generate dynamic auto inclusions:
			if (InclusionTypesEnum.IsAutoInclusion(inclusionType))
				return PhpTypeCode.Void;

			ILEmitter il = codeGenerator.IL;

			// CALL context.DynamicInclude(<file name>,<relative includer source path>,variables,self,includer);
			codeGenerator.EmitLoadScriptContext();
			codeGenerator.EmitConversion(fileNameEx, PhpTypeCode.String);
			il.Emit(OpCodes.Ldstr, codeGenerator.SourceUnit.SourceFile.RelativePath.ToString());
			codeGenerator.EmitLoadRTVariablesTable();
			codeGenerator.EmitLoadSelf();
			codeGenerator.EmitLoadClassContext();
			il.LoadLiteral(inclusionType);
			il.Emit(OpCodes.Call, Methods.ScriptContext.DynamicInclude);

			return PhpTypeCode.Object;
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitIncludingEx(this);
        }
	}

	#endregion

	#region IssetEx

	/// <summary>
	/// Represents <c>isset</c> construct.
	/// </summary>
	public sealed class IssetEx : Expression
	{
		internal override Operations Operation { get { return Operations.Isset; } }

		private readonly List<VariableUse>/*!*/ varList;
        /// <summary>List of variables to test</summary>
        public List<VariableUse>/*!*/ VarList { get { return varList; } }

		public IssetEx(Position position, List<VariableUse>/*!*/ varList)
			: base(position)
		{
			Debug.Assert(varList != null && varList.Count > 0);
			this.varList = varList;
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Expression.Analyze"]/*'/>
		internal override Evaluation Analyze(Analyzer/*!*/ analyzer, ExInfoFromParent info)
		{
			access = info.Access;

			for (int i = 0; i < varList.Count; i++)
				varList[i].Analyze(analyzer, ExInfoFromParent.DefaultExInfo);

			return new Evaluation(this);
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
		internal override PhpTypeCode Emit(CodeGenerator/*!*/ codeGenerator)
		{
			Debug.Assert(access == AccessType.None || access == AccessType.Read);
			Statistics.AST.AddNode("IssetEx");
			ILEmitter il = codeGenerator.IL;

			codeGenerator.ChainBuilder.Create();
			codeGenerator.ChainBuilder.QuietRead = true;

			if (varList.Count == 1)
			{
				codeGenerator.EmitBoxing(varList[0].EmitIsset(codeGenerator, false));

				// Compare the result with "null"
				il.CmpNotNull();
			}
			else
			{
				// Define labels 
				Label f_label = il.DefineLabel();
				Label x_label = il.DefineLabel();

				// Get first variable
				codeGenerator.EmitBoxing(varList[0].EmitIsset(codeGenerator, false));

				// Compare the result with "null"
				il.CmpNotNull();

				// Process following variables and include branching
				for (int i = 1; i < varList.Count; i++)
				{
					il.Emit(OpCodes.Brfalse, f_label);
					codeGenerator.EmitBoxing(varList[i].EmitIsset(codeGenerator, false));

					// Compare the result with "null"
					codeGenerator.IL.CmpNotNull();
				}

				il.Emit(OpCodes.Br, x_label);
				il.MarkLabel(f_label, true);
				il.Emit(OpCodes.Ldc_I4_0);
				il.MarkLabel(x_label, true);
			}

			codeGenerator.ChainBuilder.End();

			if (access == AccessType.None)
			{
				il.Emit(OpCodes.Pop);
				return PhpTypeCode.Void;
			}

			return PhpTypeCode.Boolean;
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitIssetEx(this);
        }
	}

	#endregion

	#region EmptyEx

	/// <summary>
	/// Represents <c>empty</c> construct.
	/// </summary>
	public sealed class EmptyEx : Expression
	{
		internal override Operations Operation { get { return Operations.Empty; } }

		private VariableUse variable;
        public VariableUse Variable { get { return variable; } }

		public EmptyEx(Position p, VariableUse variable)
			: base(p)
		{
			this.variable = variable;
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Expression.Analyze"]/*'/>
		internal override Evaluation Analyze(Analyzer analyzer, ExInfoFromParent info)
		{
			access = info.Access;

			variable.Analyze(analyzer, ExInfoFromParent.DefaultExInfo);

			return new Evaluation(this);
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
		/// <remarks>
		/// Nothing is expected on the evaluation stack. The result value is left on the
		/// evaluation stack.
		/// </remarks>
		internal override PhpTypeCode Emit(CodeGenerator codeGenerator)
		{
			Debug.Assert(access == AccessType.Read || access == AccessType.None);
			Statistics.AST.AddNode("EmptyEx");

			codeGenerator.ChainBuilder.Create();
			codeGenerator.ChainBuilder.QuietRead = true;

			// call EmitIsset in order to evaluate the variable quietly
			codeGenerator.EmitBoxing(variable.EmitIsset(codeGenerator, true));
			codeGenerator.IL.Emit(OpCodes.Call, Methods.PhpVariable.IsEmpty);

			codeGenerator.ChainBuilder.End();

			if (access == AccessType.None)
			{
				codeGenerator.IL.Emit(OpCodes.Pop);
				return PhpTypeCode.Void;
			}

			return PhpTypeCode.Boolean;
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitEmptyEx(this);
        }
	}

	#endregion

	#region EvalEx

	/// <summary>
	/// Represents <c>eval</c> construct.
	/// </summary>
	public sealed class EvalEx : Expression
	{
		internal override Operations Operation { get { return Operations.Eval; } }

		internal override bool DoMarkSequencePoint { get { return kind != EvalKinds.SyntheticEval; } }

		/// <summary>
		/// Expression containing source code to be evaluated.
		/// </summary>
		private Expression/*!*/ code;
        /// <summary>Expression containing source code to be evaluated.</summary>
        public Expression /*!*/ Code { get { return code; } }

		/// <summary>
		/// Contains the code string literal that has been inlined.
		/// </summary>
		private string inlinedCode;
        /// <summary>Contains the code string literal that has been inlined.</summary>
        public string InlinedCode { get { return inlinedCode; } }

        /// <summary>
        /// Aliases copied from current scope which were valid in place of this expression.
        /// Used for deferred code compilation in run time, when creating transient compilation unit.
        /// </summary>
        private readonly Dictionary<string, QualifiedName> aliases;

        /// <summary>
        /// Current namespace.
        /// Used for deferred code compilation in run time, when creating transient compilation unit.
        /// </summary>
        private readonly QualifiedName? currentNamespace;

		/// <summary>
		/// Says if this eval is real in source code, or if it was made during analyzis to
		/// defer some compilation to run-time.
		/// </summary>
		private EvalKinds kind;

		#region Construction

		/// <summary>
		/// Creates a node representing an eval or assert constructs.
		/// </summary>
		/// <param name="position">Position.</param>
		/// <param name="code">Source code expression.</param>
		/// <param name="isAssert">Whether the node represents an assert construct.</param>
		public EvalEx(Position position, Expression/*!*/ code, bool isAssert)
			: base(position)
		{
			this.kind = (isAssert) ? EvalKinds.Assert : EvalKinds.ExplicitEval;
			this.code = code;
		}

		/// <summary>
		/// Creates a node representing an eval-like construct (eval, assert, deferred type).
		/// </summary>
		/// <param name="position">Position.</param>
		/// <param name="code">Source code.</param>
        /// <param name="currentNamespace">Current namespace to be passed into the transient compilation unit.</param>
        /// <param name="aliases">Aliases to be passed into the transient compilation unit.</param>
		/// <remarks>
		/// Creates a node which actually doesn't exist in the source code but represents a piece of code 
		/// which compilation has been deferred to the run-time. It is used for example when
		/// a class is declared to be child of a unknown class or interface.
		/// </remarks>
		public EvalEx(Position position, string code, QualifiedName? currentNamespace, Dictionary<string,QualifiedName> aliases)
			: base(position)
		{
			this.kind = EvalKinds.SyntheticEval;
			this.code = new StringLiteral(position, code);

            this.currentNamespace = currentNamespace;
            this.aliases = aliases;
		}

		#endregion

		#region Analysis

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Expression.Analyze"]/*'/>
		internal override Evaluation Analyze(Analyzer/*!*/ analyzer, ExInfoFromParent info)
		{
			access = info.Access;

			// assertion:
			if (kind == EvalKinds.Assert)
			{
				if (analyzer.Context.Config.Compiler.Debug)
				{
					Evaluation code_evaluation = code.Analyze(analyzer, ExInfoFromParent.DefaultExInfo);

					// string parameter is parsed and converted to an expression:
					if (code_evaluation.HasValue)
					{
						inlinedCode = Convert.ObjectToString(code_evaluation.Value);
						if (inlinedCode != "")
						{
							const string prefix = "return ";

							// position setup:
							Position pos = Position.Initial;

							// the position of the last character before the parsed string:
							pos.FirstLine = code.Position.FirstLine;
							pos.FirstOffset = code.Position.FirstOffset - prefix.Length + 1;
							pos.FirstColumn = code.Position.FirstColumn - prefix.Length + 1;

							List<Statement> statements = analyzer.BuildAst(pos, String.Concat(prefix, inlinedCode, ";"));

							// code is unevaluable:
							if (statements == null)
								return new Evaluation(this, true);

							if (statements.Count > 1)
								analyzer.ErrorSink.Add(Warnings.MultipleStatementsInAssertion, analyzer.SourceUnit, position);

							Debug.Assert(statements.Count > 0 && statements[0] is JumpStmt);

							this.code = ((JumpStmt)statements[0]).Expression;
						}
						else
						{
							// empty assertion:
							return new Evaluation(this, true);
						}
					}
					else
					{
						code = code_evaluation.Expression;
					}
				}
				else
				{
					// replace with "true" value in release mode:
					return new Evaluation(this, true);
				}
			}

			// it is not necessary to analyze an argument nor set the declaring function's contains-eval property
			// in the case of synthetic eval:
			if (kind != EvalKinds.SyntheticEval)
			{
				code = code.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();
				analyzer.AddCurrentRoutineProperty(RoutineProperties.ContainsEval);
			}

			return new Evaluation(this);
		}

		#endregion

		#region Emission

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
		internal override PhpTypeCode Emit(CodeGenerator/*!*/ codeGenerator)
		{
			// not emitted in release mode:
			Debug.Assert(kind != EvalKinds.LambdaFunction, "Invalid eval kind.");
			Debug.Assert(kind != EvalKinds.Assert || codeGenerator.Context.Config.Compiler.Debug, "Assert should be cut off in release mode.");
			Debug.Assert(access == AccessType.None || access == AccessType.Read || access == AccessType.ReadRef);
			Debug.Assert(inlinedCode != null || codeGenerator.RTVariablesTablePlace != null, "Function should have variables table.");
			Statistics.AST.AddNode("EvalEx");

			ILEmitter il = codeGenerator.IL;
            PhpTypeCode result;

			if (inlinedCode != null)
			{
				Debug.Assert(kind == EvalKinds.Assert, "Only assert can be inlined so far.");
				Label endif_label = il.DefineLabel();
				Label else_label = il.DefineLabel();

				// IF DynamicCode.PreAssert(context) THEN
				codeGenerator.EmitLoadScriptContext();
				il.Emit(OpCodes.Call, Methods.DynamicCode.PreAssert);
				il.Emit(OpCodes.Brfalse, else_label);
				if (true)
				{
					// LOAD <evaluated assertion>;
					codeGenerator.EmitBoxing(((Expression)code).Emit(codeGenerator));

					// CALL DynamicCode.PostAssert(context);
					codeGenerator.EmitLoadScriptContext();
					il.Emit(OpCodes.Call, Methods.DynamicCode.PostAssert);

					// LOAD bool CheckAssertion(STACK, <inlined code>, context, <source path>, line, column);
					il.Emit(OpCodes.Ldstr, inlinedCode);
					codeGenerator.EmitLoadScriptContext();
					il.Emit(OpCodes.Ldstr, codeGenerator.SourceUnit.SourceFile.RelativePath.ToString());
					il.LdcI4(this.position.FirstLine);
					il.LdcI4(this.position.FirstColumn);
					codeGenerator.EmitLoadNamingContext();
					il.Emit(OpCodes.Call, Methods.DynamicCode.CheckAssertion);

					// GOTO END IF;
					il.Emit(OpCodes.Br, endif_label);
				}
				// ELSE
				il.MarkLabel(else_label);
				if (true)
				{
					// LOAD true;
					il.Emit(OpCodes.Ldc_I4_1);
				}
				// END IF;
				il.MarkLabel(endif_label);

                result = PhpTypeCode.Object;
			}
			else
			{
                result = codeGenerator.EmitEval(kind, code, position, currentNamespace, aliases);
			}

			// handles return value according to the access type:
			codeGenerator.EmitReturnValueHandling(this, false, ref result);
			return result;
		}

        #endregion

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitEvalEx(this);
        }
	}

	#endregion

	#region ExitEx

	/// <summary>
	/// Represents <c>exit</c> expression.
	/// </summary>
	public sealed class ExitEx : Expression
	{
		internal override Operations Operation { get { return Operations.Exit; } }

		private Expression resultExpr; //can be null
        /// <summary>Die (exit) expression. Can be null.</summary>
        public Expression ResulExpr { get { return resultExpr; } }

		public ExitEx(Position position, Expression resultExpr)
			: base(position)
		{
			this.resultExpr = resultExpr;
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Expression.Analyze"]/*'/>
		internal override Evaluation Analyze(Analyzer/*!*/ analyzer, ExInfoFromParent info)
		{
			access = info.Access;

			if (resultExpr != null)
				resultExpr = resultExpr.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();

			analyzer.EnterUnreachableCode();
			return new Evaluation(this);
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
		internal override PhpTypeCode Emit(CodeGenerator/*!*/ codeGenerator)
		{
			Debug.Assert(access == AccessType.None || access == AccessType.Read);
			Statistics.AST.AddNode("ExitEx");

			codeGenerator.EmitLoadScriptContext();

			if (resultExpr == null)
			{
				codeGenerator.IL.Emit(OpCodes.Ldnull);
			}
			else
			{
				codeGenerator.EmitBoxing(resultExpr.Emit(codeGenerator));
			}
			codeGenerator.IL.Emit(OpCodes.Call, Methods.ScriptContext.Die);

			if (access == AccessType.Read)
			{
				codeGenerator.IL.Emit(OpCodes.Ldnull);
				return PhpTypeCode.Object;
			}
			else return PhpTypeCode.Void;
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitExitEx(this);
        }
	}

	#endregion
}
