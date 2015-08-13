/*

 Copyright (c) 2006- DEVSENSE
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

using PHP.Core.Parsers;

namespace PHP.Core.AST
{
	/// <summary>
	/// Represents a try-catch statement.
	/// </summary>
	[Serializable]
    public sealed class TryStmt : Statement
	{
		/// <summary>
		/// A list of statements contained in the try-block.
		/// </summary>
        private readonly Statement[]/*!*/ statements;
        /// <summary>A list of statements contained in the try-block.</summary>
        public Statement[]/*!*/ Statements { get { return statements; } }

		/// <summary>
        /// A list of catch statements catching exceptions thrown inside the try block. Can be a <c>null</c> reference.
		/// </summary>
		private readonly CatchItem[]/*!*/catches;
        /// <summary>A list of catch statements catching exceptions thrown inside the try block.</summary>
        public CatchItem[]/*!*/Catches { get { return catches; } }
        internal bool HasCatches { get { return catches.Length != 0; } }

        /// <summary>
        /// A list of statements contained in the finally-block. Can be a <c>null</c> reference.
        /// </summary>
        private readonly FinallyItem finallyItem;
        /// <summary>A list of statements contained in the finally-block. Can be a <c>null</c> reference.</summary>
        public FinallyItem FinallyItem { get { return finallyItem; } }
        internal bool HasFinallyStatements { get { return finallyItem != null && finallyItem.Statements.Length != 0; } }

        public TryStmt(Text.Span p, IList<Statement>/*!*/ statements, List<CatchItem> catches, FinallyItem finallyItem)
			: base(p)
		{
            Debug.Assert(statements != null);
            
			this.statements = statements.AsArray();
			this.catches = catches.AsArray();
            this.finallyItem = finallyItem;
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
    [Serializable]
    public sealed class CatchItem : LangElement
	{
		/// <summary>
		/// A list of statements contained in the catch-block.
		/// </summary>
        private readonly Statement[]/*!*/ statements;
        /// <summary>A list of statements contained in the catch-block.</summary>
        public Statement[]/*!*/ Statements { get { return statements; } }

		/// <summary>
		/// A variable where an exception is assigned in.
		/// </summary>
		private readonly DirectVarUse/*!*/ variable;
        /// <summary>A variable where an exception is assigned in.</summary>
        public DirectVarUse/*!*/ Variable { get { return variable; } }

		/// <summary>
		/// An index of type identifier.
		/// </summary>
		private DirectTypeRef tref;
        /// <summary>An index of type identifier.</summary>
        public QualifiedName ClassName { get { return tref.QualifiedName; } }

        /// <summary>
        /// Position of <see cref="TypeRef"/>.
        /// </summary>
        public Text.Span ClassNameSpan { get { return tref.Span; } }

        /// <summary>
        /// Catch type reference.
        /// </summary>
        public DirectTypeRef TypeRef { get { return tref; } }

        public CatchItem(Text.Span p, DirectTypeRef tref, DirectVarUse/*!*/ variable,
			IList<Statement>/*!*/ statements)
			: base(p)
		{
			Debug.Assert(variable != null && statements != null);

			this.tref = tref;
			this.variable = variable;
			this.statements = statements.AsArray();
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
    /// Represents a finally-block.
    /// </summary>
    [Serializable]
    public sealed class FinallyItem : LangElement
    {
        /// <summary>
        /// A list of statements contained in the finally-block.
        /// </summary>
        private readonly Statement[]/*!*/statements;
        /// <summary>A list of statements contained in the try-block.</summary>
        public Statement[]/*!*/Statements { get { return statements; } }

        public FinallyItem(Text.Span span, IList<Statement>/*!*/statements)
            : base(span)
        {
            this.statements = statements.AsArray();
        }

        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitFinallyItem(this);
        }        
    }

	/// <summary>
	/// Represents a throw statement.
	/// </summary>
    [Serializable]
    public sealed class ThrowStmt : Statement
	{
		/// <summary>
		/// An expression being thrown.
		/// </summary>
		public Expression /*!*/ Expression { get { return expression; } internal set { expression = value; } }
        private Expression/*!*/ expression;
        
		public ThrowStmt(Text.Span span, Expression/*!*/ expression)
            : base(span)
		{
			Debug.Assert(expression != null);
			this.expression = expression;
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
