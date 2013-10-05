/*

 Copyright (c) 2004-2006 Tomas Matousek and Vaclav Novak.

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
	#region Literal

	/// <summary>
	/// Base class for literals.
	/// </summary>
    [Serializable]
	public abstract class Literal : Expression
	{
		protected Literal(Position position)
			: base(position)
		{
		}
	}

	#endregion

	#region IntLiteral

	/// <summary>
	/// Integer literal.
	/// </summary>
    [Serializable]
    public sealed class IntLiteral : Literal
	{
        public override Operations Operation { get { return Operations.IntLiteral; } }

		/// <summary>
		/// Gets a value of the literal.
		/// </summary>
		public override object Value { get { return value; } }
        private int value;

		/// <summary>
		/// Gets a type code of the literal.
		/// </summary>
		internal override PhpTypeCode ValueTypeCode { get { return PhpTypeCode.Integer; } }

		/// <summary>
		/// Initializes a new instance of the IntLiteral class.
		/// </summary>
		public IntLiteral(Position position, int value)
			: base(position)
		{
			this.value = value;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitIntLiteral(this);
        }
	}

	#endregion

	#region LongIntLiteral

	/// <summary>
	/// Integer literal.
	/// </summary>
    [Serializable]
    public sealed class LongIntLiteral : Literal
	{
        public override Operations Operation { get { return Operations.LongIntLiteral; } }

		/// <summary>
		/// Gets a value of the literal.
		/// </summary>
        public override object Value { get { return value; } }
		private long value;

		/// <summary>
		/// Gets a type code of the literal.
		/// </summary>
		internal override PhpTypeCode ValueTypeCode { get { return PhpTypeCode.LongInteger; } }

		/// <summary>
		/// Initializes a new instance of the IntLiteral class.
		/// </summary>
		public LongIntLiteral(Position position, long value)
			: base(position)
		{
			this.value = value;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitLongIntLiteral(this);
        }
	}

	#endregion

	#region DoubleLiteral

	/// <summary>
	/// Double literal.
	/// </summary>
    [Serializable]
    public sealed class DoubleLiteral : Literal
	{
        public override Operations Operation { get { return Operations.DoubleLiteral; } }

		/// <summary>
		/// Gets a value of the literal.
		/// </summary>
        public override object Value { get { return value; } }
		private double value;

		/// <summary>
		/// Gets a type code of the literal. 
		/// </summary>
		internal override PhpTypeCode ValueTypeCode { get { return PhpTypeCode.Double; } }

		/// <summary>
		/// Initializes a new instance of the DoubleLiteral class.
		/// </summary>
		/// <param name="value">A double value to be stored in node.</param>
		/// <param name="p">A position.</param>
		public DoubleLiteral(Position p, double value)
			: base(p)
		{
			this.value = value;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitDoubleLiteral(this);
        }
	}

	#endregion

	#region StringLiteral

	/// <summary>
	/// String literal.
	/// </summary>
    [Serializable]
    public sealed class StringLiteral : Literal
	{
        public override Operations Operation { get { return Operations.StringLiteral; } }

		/// <summary>
		/// A <see cref="string"/> value stored in node.
		/// </summary>
		private string value;

		/// <summary>
		/// A value of the literal.
		/// </summary>
        public override object Value { get { return value/*new PhpBytes(value)*/; } }

		/// <summary>
		/// Gets a type code of the literal. 
		/// </summary>
		internal override PhpTypeCode ValueTypeCode { get { return PhpTypeCode./*PhpBytes*/String; } }

		/// <summary>
		/// Initializes a new instance of the StringLiteral class.
		/// </summary>
		public StringLiteral(Position position, string value)
			: base(position)
		{
			this.value = value;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitStringLiteral(this);
        }
	}

	#endregion

	#region BinaryStringLiteral

	/// <summary>
	/// String literal.
	/// </summary>
    [Serializable]
    public sealed class BinaryStringLiteral : Literal
	{
        public override Operations Operation { get { return Operations.BinaryStringLiteral; } }

		/// <summary>
		/// Binary data stored in the node.
		/// </summary>
		private PhpBytes/*!*/ value;

		/// <summary>
		/// A value of the literal.
		/// </summary>
        public override object Value { get { return value; } }

		/// <summary>
		/// Gets a type code of the literal. 
		/// </summary>
		internal override PhpTypeCode ValueTypeCode { get { return PhpTypeCode.PhpBytes; } }

		/// <summary>
		/// Initializes a new instance of the StringLiteral class.
		/// </summary>
		public BinaryStringLiteral(Position position, PhpBytes/*!*/ value)
			: base(position)
		{
			this.value = value;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitBinaryStringLiteral(this);
        }
	}

	#endregion

	#region BoolLiteral

	/// <summary>
	/// Boolean literal.
	/// </summary>
    [Serializable]
    public sealed class BoolLiteral : Literal
	{
        public override Operations Operation { get { return Operations.BoolLiteral; } }

		/// <summary>
		/// Gets a value of the literal.
		/// </summary>
        public override object Value { get { return value; } }
		private bool value;

		/// <summary>
		/// Gets a type code of the literal.
		/// </summary>
		internal override PhpTypeCode ValueTypeCode { get { return PhpTypeCode.Boolean; } }

		public BoolLiteral(Position position, bool value)
			: base(position)
		{
			this.value = value;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitBoolLiteral(this);
        }
	}

	#endregion

	#region NullLiteral

	/// <summary>
	/// Null literal.
	/// </summary>
    [Serializable]
    public sealed class NullLiteral : Literal
	{
        public override Operations Operation { get { return Operations.NullLiteral; } }

		/// <summary>
		/// Gets a value of the literal.
		/// </summary>
        public override object Value { get { return null; } }

		/// <summary>
		/// Gets a type code of the literal.
		/// </summary>
		internal override PhpTypeCode ValueTypeCode { get { return PhpTypeCode.Object; } }

		public NullLiteral(Position position)
			: base(position)
		{
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitNullLiteral(this);
        }
	}

	#endregion
}
