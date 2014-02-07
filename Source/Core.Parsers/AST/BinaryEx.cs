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
	/// Binary expression.
	/// </summary>
    [Serializable]
	public sealed class BinaryEx : Expression
    {
        #region Fields & Properties

        public Expression/*!*/ LeftExpr { get { return leftExpr; } internal set { leftExpr = value; } }
		private Expression/*!*/ leftExpr;

        public Expression/*!*/ RightExpr { get { return rightExpr; } internal set { rightExpr = value; } }
		private Expression/*!*/ rightExpr;

        public override Operations Operation { get { return operation; } }
		private Operations operation;

        #endregion

        #region Construction

        public BinaryEx(Text.Span span, Operations operation, Expression/*!*/ leftExpr, Expression/*!*/ rightExpr)
			: base(span)
		{
			Debug.Assert(leftExpr != null && rightExpr != null);
			this.operation = operation;
			this.leftExpr = leftExpr;
			this.rightExpr = rightExpr;
		}

		#endregion

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitBinaryEx(this);
        }
	}
}