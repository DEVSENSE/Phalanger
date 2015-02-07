/*

 Copyright (c) 2004-2006 Tomas Matousek, Ladislav Prosek, Vaclav Novak, and Martin Maly.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Collections.Generic;

using PHP.Core.Emit;
using PHP.Core.Parsers;
using PHP.Core.Reflection;

namespace PHP.Core.AST
{
	#region JumpStmt

	/// <summary>
	/// Represents a branching (jump) statement (return, continue, break). 
	/// </summary>
	public sealed class JumpStmt : Statement
	{
		/// <summary>
		/// Type of the statement.
		/// </summary>
		public enum Types { Return, Continue, Break };

		private Types type;
        /// <summary>Type of current statement</summary>
        public Types Type { get { return type; } }

		/// <summary>
        /// In case of continue and break, it is number of loop statements to skip. Note that switch is considered to be a loop for this case
        /// In case of return, it represents the returned expression.
        /// Can be null.
		/// </summary>
		public Expression Expression { get { return expr; } }
		private Expression expr; // can be null

		public JumpStmt(Position position, Types type, Expression expr)
			: base(position)
		{
			this.type = type;
			this.expr = expr;
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Statement.Analyze"]/*'/>
		internal override Statement/*!*/ Analyze(Analyzer/*!*/ analyzer)
		{
			if (analyzer.IsThisCodeUnreachable())
			{
				analyzer.ReportUnreachableCode(position);
				return EmptyStmt.Unreachable;
			}

			if (expr != null)
			{
				ExInfoFromParent sinfo = ExInfoFromParent.DefaultExInfo;

				if (type == Types.Return
					&& analyzer.CurrentRoutine != null && analyzer.CurrentRoutine.Signature.AliasReturn
					&& expr is VarLikeConstructUse)
				{
					sinfo.Access = AccessType.ReadRef;
				}

				expr = expr.Analyze(analyzer, sinfo).Literalize();

				if (type != Types.Return && expr.HasValue)
				{
					int level = Convert.ObjectToInteger(expr.Value);
					if (level > analyzer.LoopNestingLevel || level < 0)
					{
						analyzer.ErrorSink.Add(Errors.InvalidBreakLevelCount, analyzer.SourceUnit, position, level);
					}
				}
			}
			else if (type != Types.Return && analyzer.LoopNestingLevel == 0)
			{
				analyzer.ErrorSink.Add(Errors.InvalidBreakLevelCount, analyzer.SourceUnit, position, 1);
			}

			// code in the same block after return, break, continue is unreachable
			analyzer.EnterUnreachableCode();
			return this;
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
		internal override void Emit(CodeGenerator codeGenerator)
		{
			Statistics.AST.AddNode("JumpStmt");

			// marks a sequence point:
			codeGenerator.MarkSequencePoint(
			  position.FirstLine,
			  position.FirstColumn,
			  position.LastLine,
			  position.LastColumn + 1);

			switch (type)
			{
				case Types.Break:
					// Emit simple break; - break the most inner loop
					if (expr == null)
					{
						codeGenerator.BranchingStack.EmitBreak();
					}
					else if (expr.HasValue)
					{
						// We can get the number at compile time and generate the right branch 
						// instruction for break x; where x is Literal
						codeGenerator.BranchingStack.EmitBreak(Convert.ObjectToInteger(expr.Value));
					}
					else
					{
						// In this case we emit the switch that decides where to branch at runtime.
						codeGenerator.EmitConversion(expr, PhpTypeCode.Integer);
						codeGenerator.BranchingStack.EmitBreakRuntime();
					}
					break;

				case Types.Continue:
					// Emit simple continue; - banch back to the condition of the most inner loop
					if (expr == null)
					{
						codeGenerator.BranchingStack.EmitContinue();
					}
					else if (expr.HasValue)
					{
						// We can get the number at compile time and generate the right branch 
						// instruction for continue x; where x is Literal
						codeGenerator.BranchingStack.EmitContinue(Convert.ObjectToInteger(expr.Value));
					}
					else
					{
						// In this case we emit the switch that decides where to branch at runtime.
						codeGenerator.EmitConversion(expr, PhpTypeCode.Integer);
						codeGenerator.BranchingStack.EmitContinueRuntime();
					}
					break;

				case Types.Return:
					if (codeGenerator.ReturnsPhpReference)
						EmitReturnPhpReference(codeGenerator);
					else
						EmitReturnObject(codeGenerator);
					break;

				default:
					throw null;
			}
		}

		/// <summary>
		/// Return value is not deeply copied since the deep copy takes place when the caller accesses the value.
		/// </summary>
		private void EmitReturnObject(CodeGenerator/*!*/ codeGenerator)
		{
			ILEmitter il = codeGenerator.IL;
			PhpTypeCode result;

			if (expr != null)
			{
				result = expr.Emit(codeGenerator);

				// dereference return value:
				if (result == PhpTypeCode.PhpReference)
                {
					il.Emit(OpCodes.Ldfld, Fields.PhpReference_Value);
                }
                else if (result == PhpTypeCode.PhpArray)
                {
                    // <array>.InplaceCopyOnReturn = true;
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.Emit(OpCodes.Call, Properties.PhpArray_InplaceCopyOnReturn.GetSetMethod());
                }
				else
                {
					codeGenerator.EmitBoxing(result);
                }
			}
			else
			{
				il.Emit(OpCodes.Ldnull);
			}

			codeGenerator.EmitReturnBranch();
		}

		private void EmitReturnPhpReference(CodeGenerator codeGenerator)
		{
			ILEmitter il = codeGenerator.IL;
			PhpTypeCode result;

			if (expr != null)
			{
				result = expr.Emit(codeGenerator);

				if (result != PhpTypeCode.PhpReference)
				{
					// return value is "boxed" to PhpReference:
					if (result != PhpTypeCode.Void)
					{
						codeGenerator.EmitBoxing(result);

						// We can box the value without making a copy since the result of the return expression
						// is not accessible after returnign from the routine as it is a value (not a reference).
						il.Emit(OpCodes.Newobj, Constructors.PhpReference_Object);
					}
					else
					{
						il.Emit(OpCodes.Newobj, Constructors.PhpReference_Void);
					}
				}
			}
			else
			{
				il.Emit(OpCodes.Newobj, Constructors.PhpReference_Void);
			}

			codeGenerator.EmitReturnBranch();
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitJumpStmt(this);
        }
	}

	#endregion

	#region GotoStmt

	public sealed class GotoStmt : Statement
	{
		private VariableName labelName;
        /// <summary>Label that is target of goto statement</summary>
        public VariableName LabelName { get { return labelName; } }

		public GotoStmt(Position position, string/*!*/ labelName)
			: base(position)
		{
			this.labelName = new VariableName(labelName);
		}

		internal override Statement/*!*/ Analyze(Analyzer/*!*/ analyzer)
		{
			//
			// TODO: analyze reachability, restrict jumps inside blocks, ...
			//
			// goto x;
			// // unreachable
			// x:
			//

			if (analyzer.IsThisCodeUnreachable())
			{
				analyzer.ReportUnreachableCode(position);
				return EmptyStmt.Unreachable;
			}

			Dictionary<VariableName, Statement> labels = analyzer.CurrentLabels;

			Statement stmt;
			if (labels.TryGetValue(labelName, out stmt))
			{
				LabelStmt label = stmt as LabelStmt;
				if (label != null)
					label.IsReferred = true;
			}
			else
			{
				// add a stub (this node):
				labels.Add(labelName, this);
			}

			return this;
		}

		internal override void Emit(CodeGenerator/*!*/ codeGenerator)
		{
			Debug.Assert(codeGenerator.CurrentLabels.ContainsKey(labelName));
			Debug.Assert(codeGenerator.CurrentLabels[labelName] is LabelStmt);

			// marks a sequence point:
			codeGenerator.MarkSequencePoint(
				position.FirstLine,
				position.FirstColumn,
				position.LastLine,
				position.LastColumn + 2);

			codeGenerator.IL.Emit(OpCodes.Br, ((LabelStmt)codeGenerator.CurrentLabels[labelName]).Label);
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitGotoStmt(this);
        }
	}

	#endregion

	#region LabelStmt

	public sealed class LabelStmt : Statement
	{
		internal VariableName Name { get { return name; } }
		private VariableName name;

		internal Label Label { get { return label; } set { label = value; } }
		private Label label;

		internal bool IsReferred { get { return isReferred; } set { isReferred = value; } }
		private bool isReferred;

		public LabelStmt(Position position, string/*!*/ name)
			: base(position)
		{
			this.name = new VariableName(name);
		}

		internal override Statement Analyze(Analyzer/*!*/ analyzer)
		{
			Dictionary<VariableName, Statement> labels = analyzer.CurrentLabels;

			Statement stmt;
			if (labels.TryGetValue(name, out stmt))
			{
				if (stmt is LabelStmt)
				{
					analyzer.ErrorSink.Add(Errors.LabelRedeclared, analyzer.SourceUnit, this.position, name);
					analyzer.ErrorSink.Add(Errors.RelatedLocation, analyzer.SourceUnit, stmt.Position);
				}
				else
				{
					labels[name] = this;
					this.isReferred = true;
				}
			}
			else
			{
				labels.Add(name, this);
			}

			return this;
		}

		internal override void Emit(CodeGenerator/*!*/ codeGenerator)
		{
			codeGenerator.IL.MarkLabel(label);
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitLabelStmt(this);
        }
	}

	#endregion
}
