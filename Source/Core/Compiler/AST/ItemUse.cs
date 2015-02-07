/*

 Copyright (c) 2004-2006 Tomas Matousek, Ladislav Prosek, Vaclav Novak, and Martin Maly.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Diagnostics;
using PHP.Core.Parsers;

namespace PHP.Core.AST
{
    #region ItemUse

    /// <summary>
	/// Access to an item of a structured variable by [] PHP operator.
	/// </summary>
    [Serializable]
	public sealed class ItemUse : CompoundVarUse
	{
        public override Operations Operation { get { return Operations.ItemUse; } }

        /// <summary>
        /// Whether this represents function array dereferencing.
        /// </summary>
        public bool IsFunctionArrayDereferencing { get { return this.functionArrayDereferencing; } }
        private readonly bool functionArrayDereferencing = false;

		/// <summary>
		/// Variable used as an array identifier.
		/// </summary>
        public VarLikeConstructUse Array { get { return array; } set { array = value; } }
        private VarLikeConstructUse/*!*/ array;

		/// <summary>
		/// Expression used as an array index. 
		/// A <B>null</B> reference means key-less array operator (write context only).
		/// </summary>
        public Expression Index { get { return index; } internal set { index = value; } }
		private Expression index;
		
		public ItemUse(Position p, VarLikeConstructUse/*!*/ array, Expression index, bool functionArrayDereferencing = false)
			: base(p)
		{
			Debug.Assert(array != null);

			this.array = array;
			this.index = index;
            this.functionArrayDereferencing = functionArrayDereferencing;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitItemUse(this);
        }
	}

    #endregion

    #region StringLiteralDereferenceEx

    /// <summary>
    /// String literal dereferencing.
    /// </summary>
    [Serializable]
    public sealed class StringLiteralDereferenceEx : Expression
    {
        public override Operations Operation
        {
            get { return Operations.ItemUse; }
        }

        /// <summary>
        /// Expression representing the string value.
        /// </summary>
        public Expression/*!*/StringExpr { get; internal set; }

        /// <summary>
        /// Expression representing index in the string.
        /// </summary>
        public Expression/*!*/KeyExpr { get; internal set; }

        public StringLiteralDereferenceEx(Position position, Expression expr, Expression key)
            : base(position)
        {
            this.StringExpr = expr;
            this.KeyExpr = key;
        }

        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitStringLiteralDereferenceEx(this);
        }
    }

    #endregion
}
