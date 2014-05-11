/*

 Copyright (c) 2007- DEVSENSE
 Copyright (c) 2004-2006 Tomas Matousek, Vaclav Novak and Martin Maly.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Diagnostics;

using PHP.Core;
using PHP.Core.Parsers;

namespace PHP.Core.AST
{
	/// <summary>
	/// Unary expression.
	/// </summary>
    [Serializable]
    public sealed class UnaryEx : Expression
    {
        #region Fields & Properties

        public override Operations Operation { get { return operation; } }
		private Operations operation;

		/// <summary>Expression the operator is applied on</summary>
        public Expression /*!*/ Expr { get { return expr; } internal set { expr = value; } }
        private Expression/*!*/ expr;

        #endregion

        #region Construction

        public UnaryEx(Text.Span span, Operations operation, Expression/*!*/ expr)
			: base(span)
		{
			Debug.Assert(expr != null);
			this.operation = operation;
			this.expr = expr;
		}

		public UnaryEx(Operations operation, Expression/*!*/ expr)
			: this(Text.Span.Invalid, operation, expr)
		{
		}

		#endregion

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitUnaryEx(this);
        }
	}
}