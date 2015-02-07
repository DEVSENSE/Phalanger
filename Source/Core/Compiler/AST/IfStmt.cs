/*

 Copyright (c) 2004-2006 Tomas Matousek, Vaclav Novak, and Martin Maly.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using PHP.Core;
using PHP.Core.Parsers;
using PHP.Core.Emit;

#if SILVERLIGHT
using PHP.CoreCLR;
#endif

namespace PHP.Core.AST
{
	/// <summary>
	/// Represents an if-statement.
	/// </summary>
    [Serializable]
	public sealed class IfStmt : Statement
	{
		/// <summary>
		/// List of conditions including the if-conditions and the final else.
		/// </summary>
		private List<ConditionalStmt>/*!!*/ conditions;
        public List<ConditionalStmt>/*!!*/ Conditions { get { return conditions; } }

		public IfStmt(Position position, List<ConditionalStmt>/*!!*/ conditions)
			: base(position)
		{
			Debug.Assert(conditions != null && conditions.Count > 0);
			Debug.Assert(conditions.TrueForAll(delegate(ConditionalStmt stmt) { return stmt != null; }));
			this.conditions = conditions;
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitIfStmt(this);
        }
	}

    [Serializable]
	public sealed class ConditionalStmt : AstNode
	{
		/// <summary>
		/// Condition or a <B>null</B> reference for the case of "else" branch.
		/// </summary>
		public Expression Condition { get { return condition; } internal set { condition = value; } }
		private Expression condition;

		public Statement/*!*/ Statement { get { return statement; } internal set { statement = value; } }
		private Statement/*!*/ statement;

        /// <summary>
        /// Beginning of <see cref="ConditionalStmt"/>.
        /// </summary>
        public readonly ShortPosition Position;

		public ConditionalStmt(ShortPosition position, Expression condition, Statement/*!*/ statement)
		{
            this.Position = position;
			this.condition = condition;
			this.statement = statement;
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        internal void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitConditionalStmt(this);
        }
	}
}
