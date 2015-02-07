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
        /// <summary>
        /// Gets internal value of literal.
        /// </summary>
        internal abstract object ValueObj { get; }

		protected Literal(Text.Span span)
			: base(span)
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
        /// Gets internal value of literal.
        /// </summary>
        internal override object ValueObj { get { return this.value; } }

		/// <summary>
		/// Gets a value of the literal.
		/// </summary>
        public int Value { get { return value; } }
        private int value;

		/// <summary>
		/// Initializes a new instance of the IntLiteral class.
		/// </summary>
		public IntLiteral(Text.Span span, int value)
            : base(span)
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
        /// Gets internal value of literal.
        /// </summary>
        internal override object ValueObj { get { return this.value; } }

		/// <summary>
		/// Gets a value of the literal.
		/// </summary>
        public long Value { get { return value; } }
		private long value;

		/// <summary>
		/// Initializes a new instance of the IntLiteral class.
		/// </summary>
		public LongIntLiteral(Text.Span span, long value)
			: base(span)
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
        /// Gets internal value of literal.
        /// </summary>
        internal override object ValueObj { get { return this.value; } }

		/// <summary>
		/// Gets a value of the literal.
		/// </summary>
        public double Value { get { return value; } }
		private double value;

		/// <summary>
		/// Initializes a new instance of the DoubleLiteral class.
		/// </summary>
		/// <param name="value">A double value to be stored in node.</param>
		/// <param name="p">A position.</param>
        public DoubleLiteral(Text.Span p, double value)
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
        /// Gets internal value of literal.
        /// </summary>
        internal override object ValueObj { get { return this.value; } }

		/// <summary>
		/// A <see cref="string"/> value stored in node.
		/// </summary>
		private string value;

		/// <summary>
		/// A value of the literal.
		/// </summary>
        public string Value { get { return value; } }

		/// <summary>
		/// Initializes a new instance of the StringLiteral class.
		/// </summary>
		public StringLiteral(Text.Span span, string value)
			: base(span)
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
        /// Gets internal value of literal.
        /// </summary>
        internal override object ValueObj { get { return this.value; } }

		/// <summary>
		/// A value of the literal.
		/// </summary>
        public byte[] Value { get { return value; } }

        /// <summary>
        /// Binary data stored in the node.
        /// </summary>
        private byte[]/*!*/ value;
        
        /// <summary>
		/// Initializes a new instance of the StringLiteral class.
		/// </summary>
		public BinaryStringLiteral(Text.Span span, byte[]/*!*/ value)
			: base(span)
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
        /// Gets internal value of literal.
        /// </summary>
        internal override object ValueObj { get { return this.value; } }

		/// <summary>
		/// Gets a value of the literal.
		/// </summary>
        public bool Value { get { return value; } }
		private bool value;

		public BoolLiteral(Text.Span span, bool value)
			: base(span)
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
        /// Gets internal value of literal.
        /// </summary>
        internal override object ValueObj { get { return null; } }

		public NullLiteral(Text.Span span)
			: base(span)
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
