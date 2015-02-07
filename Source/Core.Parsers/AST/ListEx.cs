/*

 Copyright (c) 2006- DEVSENSE
 Copyright (c) 2004-2006 Tomas Matousek, Vaclav Novak and Martin Maly.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using PHP.Core.Parsers;

namespace PHP.Core.AST
{
	/// <summary>
	/// Represents a <c>list</c> construct.
	/// </summary>
    [Serializable]
	public sealed class ListEx : Expression
	{
        public override Operations Operation { get { return Operations.List; } }

		/// <summary>
        /// Elements of this list are VarLikeConstructUse, ListEx and null.
        /// Null represents empty expression - for example next piece of code is ok: 
        /// list(, $value) = each ($arr)
        /// </summary>
        public List<Expression>/*!*/LValues { get; private set; }
        /// <summary>Array being assigned</summary>
        public Expression RValue { get; internal set; }

        public ListEx(Position p, List<Expression>/*!*/ lvalues, Expression rvalue)
            : base(p)
        {
            Debug.Assert(lvalues != null /*&& rvalue != null*/);    // rvalue can be determined during runtime in case of list in list.
            Debug.Assert(lvalues.TrueForAll(delegate(Expression lvalue)
            {
                return lvalue == null || lvalue is VarLikeConstructUse || lvalue is ListEx;
            }));

            this.LValues = lvalues;
            this.RValue = rvalue;
        }

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitListEx(this);
        }
	}
}
