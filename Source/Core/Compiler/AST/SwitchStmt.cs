/*

 Copyright (c) 2004-2006 Tomas Matousek, Vaclav Novak and Martin Maly.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using PHP.Core.Emit;
using System.Diagnostics;
using PHP.Core.Parsers;

namespace PHP.Core.AST
{
	#region SwitchStmt

	/// <summary>
	/// Switch statement.
	/// </summary>
	public sealed class SwitchStmt : Statement
	{
		private Expression/*!*/ switchValue;
        /// <summary>Value to switch by</summary>
        public Expression/*!*/ SwitchValue { get { return switchValue; } }
        private List<SwitchItem>/*!*/ switchItems;
        /// <summary>Body of switch statement</summary>
        public List<SwitchItem>/*!*/ SwitchItems { get { return switchItems; } }

		public SwitchStmt(Position position, Expression/*!*/ switchValue, List<SwitchItem>/*!*/ switchItems)
			: base(position)
		{
			Debug.Assert(switchValue != null && switchItems != null);

			this.switchValue = switchValue;
			this.switchItems = switchItems;
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Statement.Analyze"]/*'/>
		internal override Statement Analyze(Analyzer/*!*/ analyzer)
		{
			if (analyzer.IsThisCodeUnreachable())
			{
				analyzer.ReportUnreachableCode(position);
				return EmptyStmt.Unreachable;
			}

			switchValue = switchValue.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();

			analyzer.EnterSwitchBody();

			foreach (SwitchItem item in switchItems)
				item.Analyze(analyzer);

			analyzer.LeaveSwitchBody();
			return this;
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
		internal override void Emit(CodeGenerator/*!*/ codeGenerator)
		{
			Statistics.AST.AddNode("SwitchStmt");
			ILEmitter il = codeGenerator.IL;

			// Note: 
			//  SwitchStmt is now implemented in the most general (and unefficient) way. The whole switch
			//  is understood as a series of if-elseif-else statements.

			Label exit_label = il.DefineLabel();
			bool fall_through = false;
			Label fall_through_label = il.DefineLabel();
			Label last_default_label = il.DefineLabel();
			DefaultItem last_default = GetLastDefaultItem();
			LocalBuilder branch_to_lastdefault = null;

			if (last_default != null)
			{
				branch_to_lastdefault = il.DeclareLocal(Types.Bool[0]);
				il.LdcI4(0);
				il.Stloc(branch_to_lastdefault);
			}

			codeGenerator.BranchingStack.BeginLoop(exit_label, exit_label,
			  codeGenerator.ExceptionBlockNestingLevel);

			// marks a sequence point containing the discriminator evaluation:
			codeGenerator.MarkSequencePoint(
			  switchValue.Position.FirstLine,
			  switchValue.Position.FirstColumn,
			  switchValue.Position.LastLine,
			  switchValue.Position.LastColumn + 1);

			// Evaluate condition value and store the result into local variable
            codeGenerator.EmitBoxing(switchValue.Emit(codeGenerator));
			LocalBuilder condition_value = il.DeclareLocal(Types.Object[0]);
			il.Stloc(condition_value);

			foreach (SwitchItem item in switchItems)
			{
				item.MarkSequencePoint(codeGenerator);

				// switch item is either CaseItem ("case xxx:") or DefaultItem ("default") item:
				CaseItem case_item = item as CaseItem;
				if (case_item != null)
				{
					Label false_label = il.DefineLabel();

					// PhpComparer.Default.CompareEq(<switch expr. value>,<case value>);
                    /*changed to static method*/ //il.Emit(OpCodes.Ldsfld, Fields.PhpComparer_Default);
                    codeGenerator.EmitCompareEq(
                        cg => { cg.IL.Ldloc(condition_value); return PhpTypeCode.Object; },
                        cg => case_item.EmitCaseValue(cg));
					
					// IF (!STACK) GOTO false_label;
					il.Emit(OpCodes.Brfalse, false_label);
					if (fall_through == true)
					{
						il.MarkLabel(fall_through_label, true);
						fall_through = false;
					}

					case_item.EmitStatements(codeGenerator);

					if (fall_through == false)
					{
						fall_through_label = il.DefineLabel();
						fall_through = true;
					}

					il.Emit(OpCodes.Br, fall_through_label);

					il.MarkLabel(false_label, true);
				}
				else
				{
					DefaultItem default_item = (DefaultItem)item;

					// Only the last default branch defined in source code is used.
					// So skip default while testing "case" items at runtime.
					Label false_label = il.DefineLabel();
					il.Emit(OpCodes.Br, false_label);

					if (default_item == last_default)
					{
						il.MarkLabel(last_default_label, false);
					}

					if (fall_through == true)
					{
						il.MarkLabel(fall_through_label, true);
						fall_through = false;
					}

					default_item.EmitStatements(codeGenerator);

					if (fall_through == false)
					{
						fall_through_label = il.DefineLabel();
						fall_through = true;
					}

					il.Emit(OpCodes.Br, fall_through_label);
					il.MarkLabel(false_label, true);
				}
			}

			// If no case branch matched, branch to last default case if any is defined
			if (last_default != null)
			{
				// marks a sequence point containing the condition evaluation or skip of the default case:
				codeGenerator.MarkSequencePoint(
				  last_default.Position.FirstLine,
				  last_default.Position.FirstColumn,
				  last_default.Position.LastLine,
				  last_default.Position.LastColumn + 1);

				Debug.Assert(branch_to_lastdefault != null);
				Label temp = il.DefineLabel();

				// IF (!branch_to_lastdefault) THEN 
				il.Ldloc(branch_to_lastdefault);
				il.LdcI4(0);
				il.Emit(OpCodes.Bne_Un, temp);
				if (true)
				{
					// branch_to_lastdefault = TRUE; 
					il.LdcI4(1);
					il.Stloc(branch_to_lastdefault);

					// GOTO last_default_label;
					il.Emit(OpCodes.Br, last_default_label);
				}
				il.MarkLabel(temp, true);
				// END IF;

				il.ForgetLabel(last_default_label);
			}

			if (fall_through == true)
			{
				il.MarkLabel(fall_through_label, true);
			}

			il.MarkLabel(exit_label);
			codeGenerator.BranchingStack.EndLoop();
			il.ForgetLabel(exit_label);
		}

		/// <summary>
		/// Determines the last default item in the list of switch items.
		/// </summary>
		/// <returns>The last default item or a <b>null</b> reference if there is no default item.</returns>
		private DefaultItem GetLastDefaultItem()
		{
			DefaultItem result = null;
			foreach (SwitchItem item in switchItems)
			{
				DefaultItem di = item as DefaultItem;
				if (di != null) result = di;
			}
			return result;
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitSwitchStmt(this);
        }
	}

	#endregion

	#region SwitchItem

	/// <summary>
	/// Base class for switch case/default items.
	/// </summary>
	public abstract class SwitchItem : LangElement
	{
		protected readonly List<Statement>/*!*/ statements;
        /// <summary>Statements in this part of switch</summary>
        public List<Statement>/*!*/ Statements { get { return statements; } }

		protected SwitchItem(Position position, List<Statement>/*!*/ statements)
			: base(position)
		{
			Debug.Assert(statements != null);
			this.statements = statements;
		}

		internal virtual void Analyze(Analyzer/*!*/ analyzer)
		{
			analyzer.EnterConditionalCode();

            this.Statements.Analyze(analyzer);
			
			analyzer.LeaveConditionalCode();
		}

		internal abstract void MarkSequencePoint(CodeGenerator/*!*/ codeGenerator);

		internal virtual void EmitStatements(CodeGenerator/*!*/ codeGenerator)
		{
			foreach (Statement statement in this.statements)
			{
				statement.Emit(codeGenerator);
			}
		}
	}

	/// <summary>
	/// Switch <c>case</c> item.
	/// </summary>
	public sealed class CaseItem : SwitchItem
	{
        /// <summary>Value to compare with swich expression</summary>
        public Expression CaseVal { get { return caseVal; } }
        private Expression caseVal;

		public CaseItem(Position position, Expression/*!*/ caseVal, List<Statement>/*!*/ statements)
			: base(position, statements)
		{
			Debug.Assert(caseVal != null);
			this.caseVal = caseVal;
		}

		internal override void Analyze(Analyzer analyzer)
		{
			caseVal = caseVal.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();

			if (caseVal.HasValue)
				analyzer.AddConstCaseToCurrentSwitch(caseVal.Value, position);

			base.Analyze(analyzer);
		}

		/// <summary>
		/// Marks a sequence point "case {caseVal}".
		/// </summary>
		internal override void MarkSequencePoint(CodeGenerator codeGenerator)
		{
			codeGenerator.MarkSequencePoint(
			  position.FirstLine,
			  position.FirstColumn,
			  caseVal.Position.LastLine,
			  caseVal.Position.LastColumn + 1);
		}

		internal PhpTypeCode EmitCaseValue(CodeGenerator codeGenerator)
		{
			return caseVal.Emit(codeGenerator);
		}

		internal override void EmitStatements(CodeGenerator codeGenerator)
		{
			base.EmitStatements(codeGenerator);
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitCaseItem(this);
        }
	}

	/// <summary>
	/// Switch <c>default</c> item.
	/// </summary>
	public sealed class DefaultItem : SwitchItem
	{
		public DefaultItem(Position position, List<Statement>/*!*/ statements)
			: base(position, statements)
		{
		}

		internal override void Analyze(Analyzer analyzer)
		{
			analyzer.AddDefaultToCurrentSwitch(position);
			base.Analyze(analyzer);
		}

		/// <summary>
		/// Marks a sequence point "default".
		/// </summary>
		internal override void MarkSequencePoint(CodeGenerator/*!*/ codeGenerator)
		{
			codeGenerator.MarkSequencePoint(
			  position.FirstLine,
			  position.FirstColumn,
			  position.LastLine,
			  position.LastColumn + 1);
		}

		internal override void EmitStatements(CodeGenerator/*!*/ codeGenerator)
		{
			base.EmitStatements(codeGenerator);
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitDefaultItem(this);
        }
	}

	#endregion
}
