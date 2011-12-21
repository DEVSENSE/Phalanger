/*

 Copyright (c) 2004-2006 Tomas Matousek and Vaclav Novak.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Diagnostics;
using System.Reflection.Emit;

using PHP.Core.Emit;
using PHP.Core.Parsers;
using PHP.Core.Reflection;

namespace PHP.Core.AST
{
	#region Literal

	/// <summary>
	/// Base class for literals.
	/// </summary>
	public abstract class Literal : Expression
	{
		protected Literal(Position position)
			: base(position)
		{
		}

		public static Literal/*!*/ Create(Position position, object value, AccessType access)
		{
			string s;
			PhpBytes b;

			if (value is int) return new IntLiteral(position, (int)value, access);
			if ((s = value as string) != null) return new StringLiteral(position, s, access);
			if (value == null) return new NullLiteral(position, access);
			if (value is bool) return new BoolLiteral(position, (bool)value, access);
			if (value is double) return new DoubleLiteral(position, (double)value, access);
			if (value is long) return new LongIntLiteral(position, (long)value, access);
			if ((b = value as PhpBytes) != null) return new BinaryStringLiteral(position, b, access);

			Debug.Fail("Invalid literal type");
			throw null;
		}

		internal override Evaluation EvaluatePriorAnalysis(SourceUnit/*!*/ sourceUnit)
		{
			return new Evaluation(this, Value);
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Expression.Analyze"]/*'/>
		internal override Evaluation Analyze(Analyzer/*!*/ analyzer, ExInfoFromParent info)
		{
			// possible access values: Read, None
			access = info.Access;
			return new Evaluation(this, Value);
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="IsDeeplyCopied"]/*'/>
		internal override bool IsDeeplyCopied(CopyReason reason, int nestingLevel)
		{
			return false;
		}

		/// <summary>
		/// Emits the literal. The common code for all literals.
		/// </summary>
		internal override PhpTypeCode Emit(CodeGenerator/*!*/ codeGenerator)
		{
			ILEmitter il = codeGenerator.IL;

			// loads the value:
			il.LoadLiteral(Value);

			switch (access)
			{
				case AccessType.Read:
					return ValueTypeCode;

				case AccessType.None:
					il.Emit(OpCodes.Pop);
					return ValueTypeCode;

				case AccessType.ReadUnknown:
				case AccessType.ReadRef:
					// created by evaluation a function called on literal, e.g. $x =& sin(10);
					codeGenerator.EmitBoxing(ValueTypeCode);
					il.Emit(OpCodes.Newobj, Constructors.PhpReference_Object);

					return PhpTypeCode.PhpReference;
			}

			Debug.Fail("Invalid access type");
			return PhpTypeCode.Invalid;
		}

		internal override void DumpTo(AstVisitor visitor, System.IO.TextWriter output)
		{
			output.Write(Value);
		}
	}

	#endregion

	#region IntLiteral

	/// <summary>
	/// Integer literal.
	/// </summary>
	public sealed class IntLiteral : Literal
	{
		internal override Operations Operation { get { return Operations.IntLiteral; } }

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
		/// Called only by Analyzer. On this instance Analyze method will not be called.
		/// </summary>
		internal IntLiteral(Position position, int value, AccessType access)
			: base(position)
		{
			this.value = value;
			this.access = access;
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
	public sealed class LongIntLiteral : Literal
	{
		internal override Operations Operation { get { return Operations.LongIntLiteral; } }

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
		/// Called only by Analyzer. On this instance Analyze method will not be called.
		/// </summary>
		internal LongIntLiteral(Position position, long value, AccessType access)
			: base(position)
		{
			this.value = value;
			this.access = access;
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
	public sealed class DoubleLiteral : Literal
	{
		internal override Operations Operation { get { return Operations.DoubleLiteral; } }

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
		/// Called only by Analyzer. On this instance Analyze method will not be called.
		/// </summary>
		/// <param name="value">A double value to be stored in node.</param>
		/// <param name="p">A position.</param>
		/// <param name="access">An access type.</param>
		internal DoubleLiteral(Position p, double value, AccessType access)
			: base(p)
		{
			this.value = value;
			this.access = access;
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
	public sealed class StringLiteral : Literal
	{
		internal override Operations Operation { get { return Operations.StringLiteral; } }

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
		/// Called only by Analyzer. On this instance Analyze method will not be called.
		/// </summary>
		internal StringLiteral(Position position, string value, AccessType access)
			: base(position)
		{
			this.value = value;
			this.access = access;
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
	public sealed class BinaryStringLiteral : Literal
	{
		internal override Operations Operation { get { return Operations.BinaryStringLiteral; } }

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
		/// Called only by Analyzer. On this instance Analyze method will not be called.
		/// </summary>
		internal BinaryStringLiteral(Position position, PhpBytes value, AccessType access)
			: base(position)
		{
			this.value = value;
			this.access = access;
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
	public sealed class BoolLiteral : Literal
	{
		internal override Operations Operation { get { return Operations.BoolLiteral; } }

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

		internal BoolLiteral(Position position, bool value, AccessType access)
			: base(position)
		{
			this.value = value;
			this.access = access;
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
	public sealed class NullLiteral : Literal
	{
		internal override Operations Operation { get { return Operations.NullLiteral; } }

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

		internal NullLiteral(Position position, AccessType access)
			: base(position)
		{
			this.access = access;
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
