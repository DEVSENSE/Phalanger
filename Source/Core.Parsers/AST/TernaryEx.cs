/*

 Copyright (c) 2004-2006 Tomas Matousek, Vaclav Novak and Martin Maly.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Diagnostics;
using PHP.Core.Parsers;

namespace PHP.Core.AST
{
	/// <summary>
	/// Conditional expression.
	/// </summary>
    [Serializable]
	public sealed class ConditionalEx : Expression
	{
        public override Operations Operation { get { return Operations.Conditional; } }

		private Expression/*!*/ condExpr;
		private Expression trueExpr;
		private Expression/*!*/ falseExpr;
        /// <summary>Condition</summary>
        public Expression/*!*/ CondExpr { get { return condExpr; } }
        /// <summary>Expression evaluated when <see cref="CondExpr"/> is true. Can be <c>null</c> in case of ternary shortcut (?:).</summary>
        public Expression TrueExpr { get { return trueExpr; } set { trueExpr = value; } }
        /// <summary><summary>Expression evaluated when <see cref="CondExpr"/> is false</summary></summary>
        public Expression/*!*/ FalseExpr { get { return falseExpr; } set { falseExpr = value; } }

		public ConditionalEx(Text.Span span, Expression/*!*/ condExpr, Expression trueExpr, Expression/*!*/ falseExpr)
			: base(span)
		{
            Debug.Assert(condExpr != null);
            // Debug.Assert(trueExpr != null); // allowed to enable ternary shortcut
            Debug.Assert(falseExpr != null);

			this.condExpr = condExpr;
			this.trueExpr = trueExpr;
			this.falseExpr = falseExpr;
		}

		public ConditionalEx(Expression/*!*/ condExpr, Expression/*!*/ trueExpr, Expression/*!*/ falseExpr)
            : this(Text.Span.Invalid, condExpr, trueExpr, falseExpr)
		{
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitConditionalEx(this);
        }
	}
}

