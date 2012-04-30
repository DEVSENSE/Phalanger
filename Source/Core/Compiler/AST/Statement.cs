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
using System.Reflection.Emit;
using System.Diagnostics;
using PHP.Core.Emit;
using PHP.Core.Parsers;

namespace PHP.Core.AST
{
	#region Statement

	/// <summary>
	/// Abstract base class representing all statements elements of PHP source file.
	/// </summary>
	public abstract class Statement : LangElement
	{
		protected Statement(Position position)
			: base(position)
		{
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Statement.Analyze"]/*'/>
		internal abstract Statement/*!*/ Analyze(Analyzer/*!*/ analyzer);

		/// <summary>
		/// Reports the statement unreachability. 
		/// The block statement reports the position of its first statement.
		/// </summary>
		internal virtual void ReportUnreachable(Analyzer/*!*/ analyzer)
		{
			analyzer.ErrorSink.Add(Warnings.UnreachableCodeDetected, analyzer.SourceUnit, position);
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
		internal abstract void Emit(CodeGenerator/*!*/ codeGenerator);

		/// <summary>
		/// Whether the statement is a declaration statement (class, function, namespace, const).
		/// </summary>
		internal virtual bool IsDeclaration { get { return false; } }

		internal virtual bool SkipInPureGlobalCode() { return false; }
	}

    #region StatementUtils

    internal static class StatementUtils
    {
        /// <summary>
        /// Analyze all the <see cref="Statement"/> objects in the <paramref name="statements"/> list.
        /// This methods replaces items in the original list if <see cref="Statement.Analyze"/> returns a different instance.
        /// </summary>
        /// <param name="statements">List of statements to be analyzed.</param>
        /// <param name="analyzer">Current <see cref="Analyzer"/>.</param>
        public static void Analyze(this List<Statement>/*!*/statements, Analyzer/*!*/ analyzer)
        {
            Debug.Assert(statements != null);
            Debug.Assert(analyzer != null);

            // analyze statements:
            for (int i = 0; i < statements.Count; i++)
            {
                // analyze the statement
                var statement = statements[i];
                var analyzed = statement.Analyze(analyzer);

                // update the statement in the list
                if (!object.ReferenceEquals(statement, analyzer))
                    statements[i] = analyzed;
            }
        }
    }

    #endregion

    #endregion

    #region BlockStmt

	/// <summary>
	/// Block statement.
	/// </summary>
	public sealed class BlockStmt : Statement
	{
		private readonly List<Statement>/*!*/ statements;
        /// <summary>Statements in block</summary>
        public List<Statement>/*!*/ Statements { get { return statements; } }

		public BlockStmt(Position position, List<Statement>/*!*/ body)
			: base(position)
		{
			Debug.Assert(body != null);
			this.statements = body;
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Statement.Analyze"]/*'/>
		internal override Statement Analyze(Analyzer/*!*/ analyzer)
		{
			if (analyzer.IsThisCodeUnreachable())
			{
				analyzer.ReportUnreachableCode(position);
				return EmptyStmt.Unreachable;
			}

            this.Statements.Analyze(analyzer);
			return this;
		}

		internal override void ReportUnreachable(Analyzer/*!*/ analyzer)
		{
			if (statements.Count > 0)
				statements[0].ReportUnreachable(analyzer);
			else
				base.ReportUnreachable(analyzer);
		}


		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
		internal override void Emit(CodeGenerator codeGenerator)
		{
			foreach (Statement s in statements)
				s.Emit(codeGenerator);
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitBlockStmt(this);
        }
	}

	#endregion

	#region ExpressionStmt

	/// <summary>
	/// Expression statement.
	/// </summary>
	public sealed class ExpressionStmt : Statement
	{
		private Expression/*!*/ expression;
        /// <summary>Expression that repesents this statement</summary>
        public Expression/*!*/ Expression { get { return expression; } }

		public ExpressionStmt(Position position, Expression/*!*/ expression)
			: base(position)
		{
			Debug.Assert(expression != null);
			this.expression = expression;
		}

		internal override Statement/*!*/ Analyze(Analyzer/*!*/ analyzer)
		{
			if (analyzer.IsThisCodeUnreachable())
			{
				analyzer.ReportUnreachableCode(position);
				return EmptyStmt.Unreachable;
			}

			ExInfoFromParent info = new ExInfoFromParent(this);
			info.Access = AccessType.None;

			Evaluation expr_eval = expression.Analyze(analyzer, info);

			// skip statement if it is evaluable (has no side-effects):
			if (expr_eval.HasValue)
				return EmptyStmt.Skipped;

			expression = expr_eval.Expression;
			return this;
		}

		internal override void Emit(CodeGenerator/*!*/ codeGenerator)
		{
			if (expression.DoMarkSequencePoint)
				codeGenerator.MarkSequencePoint(position.FirstLine, position.FirstColumn, position.LastLine, position.LastColumn + 1);

            try
            {
                // emit the expression
                expression.Emit(codeGenerator);
            }
            catch (CompilerException ex)
            {
                // put the error into the error sink,
                // so the user can see, which expression is problematic (work item 20695)
                codeGenerator.Context.Errors.Add(
                    ex.ErrorInfo,
                    codeGenerator.SourceUnit,
                    Position,   // exact position of the statement
                    ex.ErrorParams
                    );

                // terminate the emit with standard Exception
                throw new Exception(CoreResources.GetString(ex.ErrorInfo.MessageId, ex.ErrorParams));
            }
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitExpressionStmt(this);
        }
	}

	#endregion

	#region EmptyStmt

	/// <summary>
	/// Empty statement.
	/// </summary>
	public sealed class EmptyStmt : Statement
	{
		public static readonly EmptyStmt Unreachable = new EmptyStmt(Position.Invalid);
		public static readonly EmptyStmt Skipped = new EmptyStmt(Position.Invalid);
		public static readonly EmptyStmt PartialMergeResiduum = new EmptyStmt(Position.Invalid);

		internal override bool SkipInPureGlobalCode()
		{
			return true;
		}

		public EmptyStmt(Position p) : base(p) { }

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Statement.Analyze"]/*'/>
		internal override Statement Analyze(Analyzer/*!*/ analyzer)
		{
			return this;
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
		internal override void Emit(CodeGenerator/*!*/ codeGenerator)
		{
			if (position.IsValid)
			{
				codeGenerator.MarkSequencePoint(position.FirstLine, position.FirstColumn, position.LastLine, position.LastColumn + 2);
			}
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitEmptyStmt(this);
        }
	}

	#endregion

	#region UseStmt - not supported
	/* the use construct is not supported currently
	public class UseStmt : Statement
	{
		private int fileNameIndex;

		public UseStmt(Position p,int fileNameIndex) : base(p)
		{
			this.fileNameIndex = fileNameIndex;
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Statement.Analyze"]/*'/>
		internal override Statement Analyze(Analyzer analyzer)
		{
			
		}
	
    /// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
		internal override void Emit(CodeGenerator codeGenerator)
		{
			
		}
	}*/
	#endregion

	#region UnsetStmt

	/// <summary>
	/// Represents an <c>unset</c> statement.
	/// </summary>
	public sealed class UnsetStmt : Statement
	{
		private readonly List<VariableUse>/*!*/ varList;
        /// <summary>List of variables to be unset</summary>
        public List<VariableUse> /*!*/VarList { get { return varList; } }

		public UnsetStmt(Position p, List<VariableUse>/*!*/ varList)
			: base(p)
		{
			Debug.Assert(varList != null);
			this.varList = varList;
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Statement.Analyze"]/*'/>
		internal override Statement Analyze(Analyzer analyzer)
		{
			if (analyzer.IsThisCodeUnreachable())
			{
				analyzer.ReportUnreachableCode(position);
				return EmptyStmt.Unreachable;
			}

			//retval not needed, VariableUse analyzis always returns the same instance
			//Access really shall by Read
			foreach (VariableUse vu in varList) vu.Analyze(analyzer, ExInfoFromParent.DefaultExInfo);
			return this;
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
		internal override void Emit(CodeGenerator codeGenerator)
		{
			Statistics.AST.AddNode("UnsetStmt");

			codeGenerator.MarkSequencePoint(
			  position.FirstLine,
			  position.FirstColumn,
			  position.LastLine,
			  position.LastColumn + 1);

			foreach (VariableUse variable in varList)
			{
				codeGenerator.ChainBuilder.Create();
				codeGenerator.ChainBuilder.QuietRead = true;
				variable.EmitUnset(codeGenerator);
				codeGenerator.ChainBuilder.End();
			}

		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitUnsetStmt(this);
        }
	}

	#endregion

	#region GlobalStmt

	/// <summary>
	/// Represents a <c>global</c> statement.
	/// </summary>
	public sealed class GlobalStmt : Statement
	{
		private List<SimpleVarUse>/*!*/ varList;
        public List<SimpleVarUse>/*!*/ VarList { get { return varList; } }

		public GlobalStmt(Position p, List<SimpleVarUse>/*!*/ varList)
			: base(p)
		{
			Debug.Assert(varList != null);
			this.varList = varList;
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Statement.Analyze"]/*'/>
		internal override Statement Analyze(Analyzer/*!*/ analyzer)
		{
			if (analyzer.IsThisCodeUnreachable())
			{
				analyzer.ReportUnreachableCode(position);
				return EmptyStmt.Unreachable;
			}

			ExInfoFromParent info = new ExInfoFromParent(this);
			info.Access = AccessType.WriteRef;
			foreach (SimpleVarUse svu in varList)
				svu.Analyze(analyzer, info);
			return this;
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
		internal override void Emit(CodeGenerator codeGenerator)
		{
			Statistics.AST.AddNode("GlobalStmt");

			foreach (SimpleVarUse variable in varList)
			{
				variable.Emit(codeGenerator);

				// CALL Operators.GetItemRef(<string variable name>, ref context.AutoGlobals.GLOBALS);
				variable.EmitName(codeGenerator);
				codeGenerator.EmitAutoGlobalLoadAddress(new VariableName(AutoGlobals.GlobalsName));
				codeGenerator.IL.Emit(OpCodes.Call, Methods.Operators.GetItemRef.String);

				variable.EmitAssign(codeGenerator);
			}
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitGlobalStmt(this);
        }
	}

	#endregion

	#region StaticStmt

	/// <summary>
	/// Represents a <c>static</c> statement.
	/// </summary>
	public sealed class StaticStmt : Statement
	{
		private List<StaticVarDecl>/*!*/ stVarList;
        /// <summary>List of static variables</summary>
        public List<StaticVarDecl>/*!*/ StVarList { get { return stVarList; } }

		public StaticStmt(Position p, List<StaticVarDecl>/*!*/ stVarList)
			: base(p)
		{
			Debug.Assert(stVarList != null);
			this.stVarList = stVarList;
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Statement.Analyze"]/*'/>
		internal override Statement Analyze(Analyzer/*!*/ analyzer)
		{
			if (analyzer.IsThisCodeUnreachable())
			{
				analyzer.ReportUnreachableCode(position);
				return EmptyStmt.Unreachable;
			}

			foreach (StaticVarDecl svd in stVarList)
				svd.Analyze(analyzer);

			return this;
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
		internal override void Emit(CodeGenerator codeGenerator)
		{
			Statistics.AST.AddNode("StaticStmt");
			foreach (StaticVarDecl svd in stVarList)
				svd.Emit(codeGenerator);
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitStaticStmt(this);
        }
	}

	/// <summary>
	/// Helpful class. No error or warning can be caused by declaring variable as static.
	/// </summary>
	/// <remarks>
	/// Even this is ok:
	/// 
	/// function f()
	///	{
	///   global $a;
	///   static $a = 1;
	/// }
	/// 
	/// That's why we dont'need to know Position => is not child of LangElement
	/// </remarks>
	public class StaticVarDecl : LangElement
	{
		private DirectVarUse/*!*/ variable;
        /// <summary>Static variable being declared</summary>
        public DirectVarUse /*!*/ Variable { get { return variable; } }
		private Expression initializer;
        /// <summary>Expression used to initialize static variable</summary>
        public Expression Initializer { get { return initializer; } }

		public StaticVarDecl(Position position, DirectVarUse/*!*/ variable, Expression initializer)
			: base(position)
		{
			Debug.Assert(variable != null);

			this.variable = variable;
			this.initializer = initializer;
		}

		internal void Analyze(Analyzer analyzer)
		{
			ExInfoFromParent sinfo = new ExInfoFromParent(this);
			sinfo.Access = AccessType.WriteRef;

			variable.Analyze(analyzer, sinfo);

			if (initializer != null)
				initializer = initializer.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();
		}

		internal void Emit(CodeGenerator codeGenerator)
		{
			ILEmitter il = codeGenerator.IL;
			string id = codeGenerator.GetLocationId();

			if (id == null)
			{
				// we are in global code -> just assign the iniVal to the variable
				variable.Emit(codeGenerator);

				if (initializer != null)
				{
					codeGenerator.EmitBoxing(initializer.Emit(codeGenerator));
					il.Emit(OpCodes.Newobj, Constructors.PhpReference_Object);
				}
				else il.Emit(OpCodes.Newobj, Constructors.PhpReference_Void);

				variable.EmitAssign(codeGenerator);
			}
			else
			{
                // (J): cache the integer index of static local variable to access its value fast from within the array

                // unique static local variable string ID
				id = String.Format("{0}${1}${2}${3}", id, variable.VarName, position.FirstLine, position.FirstColumn);

                // create static field for static local index: private static int <id>;
                var type = codeGenerator.IL.TypeBuilder;
                Debug.Assert(type != null, "The method does not have declaring type! (global code in pure mode?)");
                var field_id = type.DefineField(id, Types.Int[0], System.Reflection.FieldAttributes.Private | System.Reflection.FieldAttributes.Static);

				// we are in a function or method -> try to retrieve the local value from ScriptContext
				variable.Emit(codeGenerator);

                // <context>.GetStaticLocal( <field> )
                codeGenerator.EmitLoadScriptContext();  // <context>
                il.Emit(OpCodes.Ldsfld, field_id);         // <field>
                il.Emit(OpCodes.Callvirt, Methods.ScriptContext.GetStaticLocal);    // GetStaticLocal
                il.Emit(OpCodes.Dup);

                // ?? <context>.AddStaticLocal( <field> != 0 ? <field> : ( <field> = ScriptContext.GetStaticLocalId(<id>) ), <initializer> )
                if (true)
                {
                    // if (GetStaticLocal(<field>) == null)
                    Label local_initialized = il.DefineLabel();
                    il.Emit(OpCodes.Brtrue/*not .S, initializer can emit really long code*/, local_initialized);

                    il.Emit(OpCodes.Pop);
                    
                    // <field> != 0 ? <field> : ( <field> = ScriptContext.GetStaticLocalId(<id>) )
                    il.Emit(OpCodes.Ldsfld, field_id);         // <field>

                    if (true)
                    {
                        // if (<field> == 0)
                        Label id_initialized = il.DefineLabel();
                        il.Emit(OpCodes.Brtrue_S, id_initialized);

                        // <field> = GetStaticLocalId( <id> )
                        il.Emit(OpCodes.Ldstr, id);
                        il.Emit(OpCodes.Call, Methods.ScriptContext.GetStaticLocalId);
                        il.Emit(OpCodes.Stsfld, field_id);

                        il.MarkLabel(id_initialized);
                    }

                    // <context>.AddStaticLocal(<field>,<initialize>)
                    codeGenerator.EmitLoadScriptContext();  // <context>
                    il.Emit(OpCodes.Ldsfld, field_id);         // <field>
                    if (initializer != null) codeGenerator.EmitBoxing(initializer.Emit(codeGenerator)); // <initializer>
				    else il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Callvirt, Methods.ScriptContext.AddStaticLocal);    // AddStaticLocal

                    // 
                    il.MarkLabel(local_initialized);
                }

                // (J) Following code used Dictionary. It was replaced by the code above.
                /*
                codeGenerator.EmitLoadScriptContext();
				il.Emit(OpCodes.Ldstr, id);
				il.Emit(OpCodes.Call, Methods.ScriptContext.GetStaticLocal);

				Label reference_gotten = codeGenerator.IL.DefineLabel();
				il.Emit(OpCodes.Dup);
				il.Emit(OpCodes.Brtrue, reference_gotten);
				il.Emit(OpCodes.Pop);

				// this is the first time execution reach the statement for current request -> initialize the local
				codeGenerator.EmitLoadScriptContext();
				il.Emit(OpCodes.Ldstr, id);

				if (initializer != null)
					codeGenerator.EmitBoxing(initializer.Emit(codeGenerator));
				else
					il.Emit(OpCodes.Ldnull);

				il.Emit(OpCodes.Call, Methods.ScriptContext.AddStaticLocal);
                
				// assign the resulting PhpReference into the variable
				il.MarkLabel(reference_gotten, true);
                */

				variable.EmitAssign(codeGenerator);
			}
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitStaticVarDecl(this);
        }
	}
	#endregion

}
