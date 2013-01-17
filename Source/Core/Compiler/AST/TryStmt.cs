/*

 Copyright (c) 2004-2006 Tomas Matousek and Vaclav Novak.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

using PHP.Core.Emit;
using PHP.Core.Parsers;
using PHP.Core.Reflection;

namespace PHP.Core.AST
{
	/// <summary>
	/// Represents a try-catch statement.
	/// </summary>
	public sealed class TryStmt : Statement
	{
		/// <summary>
		/// A list of statements contained in the try-block.
		/// </summary>
		private readonly List<Statement>/*!*/ statements;
        /// <summary>A list of statements contained in the try-block.</summary>
        public List<Statement>/*!*/ Statements { get { return statements; } }

		/// <summary>
		/// A list of catch statements catching exceptions thrown inside the try block.
		/// </summary>
		private readonly List<CatchItem>/*!*/ catches;
        /// <summary>A list of catch statements catching exceptions thrown inside the try block.</summary>
        public List<CatchItem>/*!*/ Catches { get { return catches; } }

		public TryStmt(Position p, List<Statement>/*!*/ statements, List<CatchItem>/*!*/ catches)
			: base(p)
		{
			Debug.Assert(statements != null && catches != null && catches.Count > 0);

			this.statements = statements;
			this.catches = catches;
		}

		internal override Statement Analyze(Analyzer/*!*/ analyzer)
		{
			analyzer.EnterConditionalCode();
            this.Statements.Analyze(analyzer);
			analyzer.LeaveConditionalCode();

			for (int i = 0; i < catches.Count; i++)
				catches[i].Analyze(analyzer);

			return this;
		}

		/// <summary>
		/// Emits the try block and the catch blocks.
		/// </summary>
		/// <param name="codeGenerator">A code generator.</param>
		/// <remarks>
		/// <code>
		///	try
		/// {
		///   // guarded code //
		/// }
		/// catch(E1 $e1)
		/// {
		///   // E1 //
		/// }
		/// catch(E2 $e2)
		/// {
		///   // E2 //
		/// } 
		/// </code>
		/// is translated as follows:
		/// <code>
		/// try
		/// {
		///   // guarded code //
		/// }
		/// catch(PhpUserException _e)
		/// {
		///   PhpObject _o = _e.UserException;
		///   if (_o instanceOf E1)
		///   {
		///     $e1 = _o;
		///     // E1 //
		///   }
		///   else if (_o instanceOf E2)
		///   {
		///     $e2 = _o;
		///     // E2 //
		///   }
		///   else
		///   {
		///     throw;
		///   }
		/// }
		/// </code> 
		/// </remarks>
		internal override void Emit(CodeGenerator/*!*/ codeGenerator)
		{
			Statistics.AST.AddNode("TryStmt");
			ILEmitter il = codeGenerator.IL;
			codeGenerator.ExceptionBlockNestingLevel++;

			// TRY
			Label end_label = il.BeginExceptionBlock();

			foreach (Statement statement in statements)
				statement.Emit(codeGenerator);

            // catch (PHP.Core.ScriptDiedException)
            // { throw; }

            il.BeginCatchBlock(typeof(PHP.Core.ScriptDiedException));
            il.Emit(OpCodes.Rethrow);

            // catch (System.Exception ex)
			
			il.BeginCatchBlock(typeof(System.Exception));

            // <exception_local> = (DObject) (STACK is PhpUserException) ? ((PhpUserException)STACK).UserException : ClrObject.WrapRealObject(STACK)

            Label clrExceptionLabel = il.DefineLabel();
            Label wrapEndLabel = il.DefineLabel();
            LocalBuilder exception_local = il.GetTemporaryLocal(typeof(DObject));
            
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Isinst, typeof(PHP.Core.PhpUserException)); // <STACK> as PhpUserException
            il.Emit(OpCodes.Brfalse, clrExceptionLabel);
            
            // if (<STACK> as PhpUserException != null)
            {
                il.Emit(OpCodes.Ldfld, Fields.PhpUserException_UserException);
                il.Emit(OpCodes.Br, wrapEndLabel);
            }
            
            // else
            il.MarkLabel(clrExceptionLabel);
            {
                il.Emit(OpCodes.Call, Methods.ClrObject_WrapRealObject);
            }
            il.MarkLabel(wrapEndLabel);
            il.Stloc(exception_local);

            // emits all PHP catch-blocks processing into a single CLI catch-block:
			foreach (CatchItem c in catches)
			{
				Label next_catch_label = il.DefineLabel();

				// IF (exception <instanceOf> <type>);
				c.Emit(codeGenerator, exception_local, end_label, next_catch_label);

				// ELSE
				il.MarkLabel(next_catch_label);
			}

            il.ReturnTemporaryLocal(exception_local);

			// emits the "else" branch invoked if the exceptions is not catched:
			il.Emit(OpCodes.Rethrow);

            //
			il.EndExceptionBlock();

			codeGenerator.ExceptionBlockNestingLevel--;
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitTryStmt(this);
        }
	}

	/// <summary>
	/// Represents a catch-block.
	/// </summary>
	public sealed class CatchItem : LangElement
	{
		/// <summary>
		/// A list of statements contained in the catch-block.
		/// </summary>
		private readonly List<Statement>/*!*/ statements;
        /// <summary>A list of statements contained in the catch-block.</summary>
        public List<Statement>/*!*/ Statements { get { return statements; } }

		/// <summary>
		/// A variable where an exception is assigned in.
		/// </summary>
		private readonly DirectVarUse/*!*/ variable;
        /// <summary>A variable where an exception is assigned in.</summary>
        public DirectVarUse/*!*/ Variable { get { return variable; } }

		/// <summary>
		/// An index of type identifier.
		/// </summary>
		private GenericQualifiedName className;
        /// <summary>An index of type identifier.</summary>
        public GenericQualifiedName ClassName { get { return className; } }

		private DType resolvedType;

		public CatchItem(Position p, GenericQualifiedName className, DirectVarUse/*!*/ variable,
			List<Statement>/*!*/ statements)
			: base(p)
		{
			Debug.Assert(variable != null && statements != null);

			this.className = className;
			this.variable = variable;
			this.statements = statements;
		}


		internal void Analyze(Analyzer/*!*/ analyzer)
		{
			ExInfoFromParent info = new ExInfoFromParent(this);
			info.Access = AccessType.Write;

			resolvedType = analyzer.ResolveTypeName(className, analyzer.CurrentType, analyzer.CurrentRoutine, position, false);

			variable.Analyze(analyzer, info);

			analyzer.EnterConditionalCode();
            this.Statements.Analyze(analyzer);
			analyzer.LeaveConditionalCode();
		}

		/// <summary>
		/// Emits the catch-block.
		/// </summary>
		/// <param name="codeGenerator">A code generator.</param>
		/// <param name="exceptionLocal">A local variable containing an instance of <see cref="Library.SPL.Exception"/>.</param>
		/// <param name="endLabel">A label in IL stream where the processing of the try-catch blocks ends.</param>
		/// <param name="nextCatchLabel">A label in IL stream where the next catch block processing begins.</param>
		internal void Emit(CodeGenerator/*!*/ codeGenerator, LocalBuilder/*!*/ exceptionLocal, Label endLabel,
			Label nextCatchLabel)
		{
			ILEmitter il = codeGenerator.IL;

			codeGenerator.MarkSequencePoint(
			  variable.Position.FirstLine,
			  variable.Position.FirstColumn,
			  variable.Position.LastLine,
			  variable.Position.LastColumn + 1
			);

			// IF !InstanceOf(<class name>) GOTO next_catch;
			il.Ldloc(exceptionLocal);
			resolvedType.EmitInstanceOf(codeGenerator, null);
			il.Emit(OpCodes.Brfalse, nextCatchLabel);

			// variable = exception;
			variable.Emit(codeGenerator);
			il.Ldloc(exceptionLocal);
			variable.EmitAssign(codeGenerator);

			foreach (Statement statement in statements)
				statement.Emit(codeGenerator);

			// LEAVE end;
			il.Emit(OpCodes.Leave, endLabel);
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitCatchItem(this);
        }
	}

	/// <summary>
	/// Represents a throw statement.
	/// </summary>
	public sealed class ThrowStmt : Statement
	{
		/// <summary>
		/// An expression being thrown.
		/// </summary>
		private Expression/*!*/ expression;
        /// <summary>An expression being thrown.</summary>
        public Expression /*!*/ Expression { get { return expression; } }

		public ThrowStmt(Position position, Expression/*!*/ expression)
			: base(position)
		{
			Debug.Assert(expression != null);
			this.expression = expression;
		}

		internal override Statement Analyze(Analyzer/*!*/ analyzer)
		{
			expression = expression.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();
			return this;
		}

		internal override void Emit(CodeGenerator/*!*/ codeGenerator)
		{
			codeGenerator.MarkSequencePoint(
				position.FirstLine,
				position.FirstColumn,
				position.LastLine,
				position.LastColumn + 1
			);

			// CALL Operators.Throw(<context>, <expression>);
			codeGenerator.EmitLoadScriptContext();
			expression.Emit(codeGenerator);
			codeGenerator.IL.Emit(OpCodes.Call, Methods.Operators.Throw);
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitThrowStmt(this);
        }
	}
}
