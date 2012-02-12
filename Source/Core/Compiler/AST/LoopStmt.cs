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
using PHP.Core;
using System.Diagnostics;
using PHP.Core.Parsers;

namespace PHP.Core.AST
{
	#region WhileStmt

	/// <summary>
	/// Represents a while-loop statement.
	/// </summary>
	public sealed class WhileStmt : Statement
	{
		public enum Type { While, Do };

		private Type type;
        /// <summary>Type of statement</summary>
        public Type LoopType { get { return type; } }

		/// <summary>
		/// Condition or a <B>null</B> reference for unbounded loop.
		/// </summary>
		private Expression condExpr;
        /// <summary>Condition or a <B>null</B> reference for unbounded loop.</summary>
        public Expression CondExpr { get { return condExpr; } }

		private Statement/*!*/ body;
        /// <summary>Body of loop</summary>
        public Statement/*!*/ Body { get { return body; } }

		public WhileStmt(Position position, Type type, Expression/*!*/ condExpr, Statement/*!*/ body)
			: base(position)
		{
			Debug.Assert(condExpr != null && body != null);

			this.type = type;
			this.condExpr = condExpr;
			this.body = body;
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Statement.Analyze"]/*'/>
		internal override Statement/*!*/ Analyze(Analyzer/*!*/ analyzer)
		{
			if (analyzer.IsThisCodeUnreachable())
			{
				analyzer.ReportUnreachableCode(position);
				return EmptyStmt.Unreachable;
			}

			Evaluation cond_eval = condExpr.Analyze(analyzer, ExInfoFromParent.DefaultExInfo);

			if (cond_eval.HasValue)
			{
				if (Convert.ObjectToBoolean(cond_eval.Value))
				{
					// unbounded loop:
					condExpr = null;
				}
				else
				{
					// unreachable body:
					if (type == Type.While)
					{
						body.ReportUnreachable(analyzer);
						return EmptyStmt.Unreachable;
					}
				}
			}

			condExpr = cond_eval.Literalize();

			analyzer.EnterLoopBody();
			body = body.Analyze(analyzer);
			analyzer.LeaveLoopBody();

			return this;
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
		internal override void Emit(CodeGenerator codeGenerator)
		{
			Statistics.AST.AddNode("Loop.While");

			ILEmitter il = codeGenerator.IL;
			Label cond_label = il.DefineLabel();
			Label exit_label = il.DefineLabel();
			Label stat_label = il.DefineLabel();

			codeGenerator.BranchingStack.BeginLoop(cond_label, exit_label, codeGenerator.ExceptionBlockNestingLevel);

			if (this.type == Type.While)
			{
				il.Emit(OpCodes.Br, cond_label);
			}

			// body:
			il.MarkLabel(stat_label);
			body.Emit(codeGenerator);

			// marks a sequence point containing condition:
			codeGenerator.MarkSequencePoint(
			  condExpr.Position.FirstLine,
			  condExpr.Position.FirstColumn,
			  condExpr.Position.LastLine,
			  condExpr.Position.LastColumn + 1);

			// condition:
			il.MarkLabel(cond_label);

			// bounded loop:
			if (condExpr != null)
			{
				// IF (<(bool) condition>) GOTO stat;
				codeGenerator.EmitConversion(condExpr, PhpTypeCode.Boolean);
				il.Emit(OpCodes.Brtrue, stat_label);
			}

			il.MarkLabel(exit_label);
			codeGenerator.BranchingStack.EndLoop();

			il.ForgetLabel(cond_label);
			il.ForgetLabel(exit_label);
			il.ForgetLabel(stat_label);
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitWhileStmt(this);
        }
	}

	#endregion

	#region ForStmt

	/// <summary>
	/// Represents a for-loop statement.
	/// </summary>
	public sealed class ForStmt : Statement
	{
		private readonly List<Expression>/*!*/ initExList;
		private readonly List<Expression>/*!*/ condExList;
		private readonly List<Expression>/*!*/ actionExList;
		private Statement/*!*/ body;

        /// <summary>List of expressions used for initialization</summary>
        public List<Expression> /*!*/ InitExList { get { return initExList; } }
        /// <summary>List of expressions used as condition</summary>
        public List<Expression> /*!*/ CondExList { get { return condExList; } }
        /// <summary>List of expressions used to incrent iterator</summary>
        public List<Expression> /*!*/ ActionExList { get { return actionExList; } }
        /// <summary>Body of statement</summary>
        public Statement/*!*/ Body { get { return body; } }

		public ForStmt(Position p, List<Expression>/*!*/ initExList, List<Expression>/*!*/ condExList,
		  List<Expression>/*!*/ actionExList, Statement/*!*/ body)
			: base(p)
		{
			Debug.Assert(initExList != null && condExList != null && actionExList != null && body != null);

			this.initExList = initExList;
			this.condExList = condExList;
			this.actionExList = actionExList;
			this.body = body;
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Statement.Analyze"]/*'/>
		internal override Statement Analyze(Analyzer analyzer)
		{
			if (analyzer.IsThisCodeUnreachable())
			{
				analyzer.ReportUnreachableCode(position);
				return EmptyStmt.Unreachable;
			}

			ExInfoFromParent info = new ExInfoFromParent(this);

			info.Access = AccessType.None;

			for (int i = 0; i < initExList.Count; i++)
			{
				initExList[i] = initExList[i].Analyze(analyzer, info).Literalize();
			}

			if (condExList.Count > 0)
			{
				// all but the last expression is evaluated and the result is ignored (AccessType.None), 
				// the last is read:

				for (int i = 0; i < condExList.Count - 1; i++)
				{
					condExList[i] = condExList[i].Analyze(analyzer, info).Literalize();
				}

				condExList[condExList.Count - 1] = condExList[condExList.Count - 1].Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();
			}

			for (int i = 0; i < actionExList.Count; i++)
			{
				actionExList[i] = actionExList[i].Analyze(analyzer, info).Literalize();
			}

			analyzer.EnterLoopBody();
			body = body.Analyze(analyzer);
			analyzer.LeaveLoopBody();

			return this;
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
		internal override void Emit(CodeGenerator codeGenerator)
		{
			Statistics.AST.AddNode("Loop.For");

			// Template: 
			// we expand the for-statement
			//		for (<expr1>; <expr2>; <expr3>) <loop body>
			// in the while form
			//		{
			//			<expr1>;
			//			while (<expr2>) {
			//				<loop body>;
			//				<expr 3>;
			//			}
			//		}	

			Label cond_label = codeGenerator.IL.DefineLabel();
			Label iterate_label = codeGenerator.IL.DefineLabel();
			Label exit_label = codeGenerator.IL.DefineLabel();
			Label stat_label = codeGenerator.IL.DefineLabel();

			codeGenerator.BranchingStack.BeginLoop(iterate_label, exit_label,
			  codeGenerator.ExceptionBlockNestingLevel);

			// marks a sequence point containing initialization statements (if any):
			if (initExList.Count > 0)
			{
				codeGenerator.MarkSequencePoint(
				  initExList[0].Position.FirstLine,
				  initExList[0].Position.FirstColumn,
				  initExList[initExList.Count - 1].Position.LastLine,
				  initExList[initExList.Count - 1].Position.LastColumn + 1);
			}

			// Emit <expr1>
			foreach (Expression expr in initExList)
				expr.Emit(codeGenerator);

			// Branch unconditionally to the begin of condition evaluation
			codeGenerator.IL.Emit(OpCodes.Br, cond_label);

			// Emit loop body
			codeGenerator.IL.MarkLabel(stat_label);
			body.Emit(codeGenerator);
			codeGenerator.IL.MarkLabel(iterate_label);

			// marks a sequence point containing action statements (if any):
			if (actionExList.Count > 0)
			{
				codeGenerator.MarkSequencePoint(
				  actionExList[0].Position.FirstLine,
				  actionExList[0].Position.FirstColumn,
				  actionExList[actionExList.Count - 1].Position.LastLine,
				  actionExList[actionExList.Count - 1].Position.LastColumn + 1);
			}

			// Emit <expr3>
			foreach (Expression expr in actionExList)
				expr.Emit(codeGenerator);

			// marks a sequence point containing condition (if any):
			if (condExList.Count > 0)
			{
				codeGenerator.MarkSequencePoint(
				  condExList[0].Position.FirstLine,
				  condExList[0].Position.FirstColumn,
				  condExList[condExList.Count - 1].Position.LastLine,
				  condExList[condExList.Count - 1].Position.LastColumn + 1);
			}

			// Emit <expr2>
			codeGenerator.IL.MarkLabel(cond_label);
			if (condExList.Count > 0)
			{
				for (int i = 0; i < (condExList.Count - 1); i++)
					condExList[i].Emit(codeGenerator);

				// LOAD <(bool) condition>
				codeGenerator.EmitConversion(condExList[condExList.Count - 1], PhpTypeCode.Boolean);
			}
			else
				codeGenerator.IL.LdcI4(1);

			codeGenerator.IL.Emit(OpCodes.Brtrue, stat_label);

			codeGenerator.IL.MarkLabel(exit_label);
			codeGenerator.BranchingStack.EndLoop();

			codeGenerator.IL.ForgetLabel(cond_label);
			codeGenerator.IL.ForgetLabel(iterate_label);
			codeGenerator.IL.ForgetLabel(exit_label);
			codeGenerator.IL.ForgetLabel(stat_label);
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitForStmt(this);
        }
	}
	#endregion

	#region ForeachStmt

	/// <summary>
	/// Represents a foreach-loop statement.
	/// </summary>
	public sealed class ForeachVar : AstNode
	{
		/// <summary>
		/// Whether the variable is aliased.
		/// </summary>
		public bool Alias { get { return alias; } set { alias = value; } }
		private bool alias;

		/// <summary>
		/// The variable itself.
		/// </summary>
		private VariableUse variable;
        /// <summary>The variable itself.</summary>
        public VariableUse Variable { get { return variable; } }
		internal Position Position { get { return variable.Position; } }

		public ForeachVar(VariableUse variable, bool alias)
		{
			this.variable = variable;
			this.alias = alias;
		}

		internal void Analyze(Analyzer analyzer)
		{
			ExInfoFromParent info = new ExInfoFromParent(this);
			if (alias) info.Access = AccessType.WriteRef;
			else info.Access = AccessType.Write;

			//retval not needed
			variable.Analyze(analyzer, info);
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
		internal PhpTypeCode Emit(CodeGenerator codeGenerator)
		{
			return this.variable.Emit(codeGenerator);
		}

		internal PhpTypeCode EmitAssign(CodeGenerator codeGenerator)
		{
			return this.variable.EmitAssign(codeGenerator);
		}

	}

	/// <summary>
	/// Represents a foreach statement.
	/// </summary>
	public class ForeachStmt : Statement
	{
		private Expression/*!*/ enumeree;
        /// <summary>Array to enumerate through</summary>
        public Expression /*!*/Enumeree { get { return enumeree; } }
		private ForeachVar keyVariable;
        /// <summary>Variable to store key in (can be null)</summary>
        public ForeachVar KeyVariable { get { return keyVariable; } }
		private ForeachVar/*!*/ valueVariable;
        /// <summary>Variable to store value in</summary>
        public ForeachVar /*!*/ ValueVariable { get { return valueVariable; } }
		private Statement/*!*/ body;
        /// <summary>Body - statement in loop</summary>
        public Statement/*!*/ Body { get { return body; } }

		public ForeachStmt(Position position, Expression/*!*/ enumeree, ForeachVar key, ForeachVar/*!*/ value,
		  Statement/*!*/ body)
			: base(position)
		{
			Debug.Assert(enumeree != null && value != null && body != null);

			this.enumeree = enumeree;
			this.keyVariable = key;
			this.valueVariable = value;
			this.body = body;
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Statement.Analyze"]/*'/>
		internal override Statement/*!*/ Analyze(Analyzer/*!*/ analyzer)
		{
			if (analyzer.IsThisCodeUnreachable())
			{
				analyzer.ReportUnreachableCode(position);
				return EmptyStmt.Unreachable;
			}

			//next version: array.SetSeqPoint();
			enumeree.Analyze(analyzer, ExInfoFromParent.DefaultExInfo);
			if (keyVariable != null) keyVariable.Analyze(analyzer);
			valueVariable.Analyze(analyzer);

			analyzer.EnterLoopBody();
			body = body.Analyze(analyzer);
			analyzer.LeaveLoopBody();
			return this;
		}

		/// <author>Tomas Matousek</author>
		/// <remarks>
		/// Emits the following code:
		/// <code>
		/// IPhpEnumerable enumerable = ARRAY as IPhpEnumerable;
		/// if (enumerable==null)
		/// {
		///   PhpException.InvalidForeachArgument();
		/// }
		/// else
		/// FOREACH_BEGIN:
		/// {
		///   IDictionaryEnumerator enumerator = enumerable.GetForeachEnumerator(KEYED,ALIASED,TYPE_HANDLE);
		///    
		///   goto LOOP_TEST;
		///   LOOP_BEGIN:
		///   {
		///     ASSIGN(value,enumerator.Value);
		///     ASSIGN(key,enumerator.Key);
		///     
		///     BODY; 
		///   }
		///   LOOP_TEST:
		///   if (enumerator.MoveNext()) goto LOOP_BEGIN;
		/// } 
		/// FOREACH_END:
		/// </code>
		/// </remarks>
		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
		internal override void Emit(CodeGenerator codeGenerator)
		{
			Statistics.AST.AddNode("Loop.Foreach");
			ILEmitter il = codeGenerator.IL;

			Label foreach_end = il.DefineLabel();
			Label foreach_begin = il.DefineLabel();
			Label loop_begin = il.DefineLabel();
			Label loop_test = il.DefineLabel();

			codeGenerator.BranchingStack.BeginLoop(loop_test, foreach_end,
			  codeGenerator.ExceptionBlockNestingLevel);

			LocalBuilder enumerable = il.GetTemporaryLocal(typeof(IPhpEnumerable));

			// marks foreach "header" (the first part of the IL code):
			codeGenerator.MarkSequencePoint(
			  enumeree.Position.FirstLine,
			  enumeree.Position.FirstColumn,
			  valueVariable.Position.LastLine,
			  valueVariable.Position.LastColumn + 1);

			// enumerable = array as IPhpEnumerable;
			enumeree.Emit(codeGenerator);
			il.Emit(OpCodes.Isinst, typeof(IPhpEnumerable));
			il.Stloc(enumerable);

			// if (enumerable==null)
			il.Ldloc(enumerable);
			il.Emit(OpCodes.Brtrue, foreach_begin);
			{
				// CALL PhpException.InvalidForeachArgument();
				codeGenerator.EmitPhpException(Methods.PhpException.InvalidForeachArgument);
				il.Emit(OpCodes.Br, foreach_end);
			}
			// FOREACH_BEGIN:
			il.MarkLabel(foreach_begin);
			{
				LocalBuilder enumerator = il.GetTemporaryLocal(typeof(System.Collections.IDictionaryEnumerator));

				// enumerator = enumerable.GetForeachEnumerator(KEYED,ALIASED,TYPE_HANDLE);
				il.Ldloc(enumerable);
				il.LoadBool(keyVariable != null);
				il.LoadBool(valueVariable.Alias);
				codeGenerator.EmitLoadClassContext();
				il.Emit(OpCodes.Callvirt, Methods.IPhpEnumerable_GetForeachEnumerator);
				il.Stloc(enumerator);

				// goto LOOP_TEST;
				il.Emit(OpCodes.Br, loop_test);

				// LOOP_BEGIN:
				il.MarkLabel(loop_begin);
				{
					// enumerator should do dereferencing and deep copying (if applicable):
					// ASSIGN(value,enumerator.Value);
					valueVariable.Emit(codeGenerator);
					il.Ldloc(enumerator);
					il.Emit(OpCodes.Callvirt, Properties.IDictionaryEnumerator_Value.GetGetMethod());
					if (valueVariable.Alias) il.Emit(OpCodes.Castclass, typeof(PhpReference));
					valueVariable.EmitAssign(codeGenerator);

					if (keyVariable != null)
					{
						// enumerator should do dereferencing and deep copying (if applicable):
						// ASSIGN(key,enumerator.Key);
						keyVariable.Emit(codeGenerator);
						il.Ldloc(enumerator);
						il.Emit(OpCodes.Callvirt, Properties.IDictionaryEnumerator_Key.GetGetMethod());
						keyVariable.EmitAssign(codeGenerator);
					}

					// BODY:
					body.Emit(codeGenerator);
				}
				// LOOP_TEST:
				il.MarkLabel(loop_test);

				// marks foreach "header" (the second part of the code):
				codeGenerator.MarkSequencePoint(
				  enumeree.Position.FirstLine,
				  enumeree.Position.FirstColumn,
				  valueVariable.Position.LastLine,
				  valueVariable.Position.LastColumn + 1);

				// if (enumerator.MoveNext()) goto LOOP_BEGIN;
				il.Ldloc(enumerator);
				il.Emit(OpCodes.Callvirt, Methods.IEnumerator_MoveNext);
				il.Emit(OpCodes.Brtrue, loop_begin);

                //
                il.ReturnTemporaryLocal(enumerator);
			}
			// FOREACH_END:
			il.MarkLabel(foreach_end);

            il.ReturnTemporaryLocal(enumerable);

			codeGenerator.BranchingStack.EndLoop();

			il.ForgetLabel(foreach_end);
			il.ForgetLabel(foreach_begin);
			il.ForgetLabel(loop_begin);
			il.ForgetLabel(loop_test);
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitForeachStmt(this);
        }
	}

	#endregion
}
