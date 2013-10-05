/*

 Copyright (c) 2007- DEVSENSE
 Copyright (c) 2004-2006 Tomas Matousek and Vaclav Novak.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;

using PHP.Core.Parsers;
using PHP.Core.AST;

namespace PHP.Core.AST
{
	#region enum Operations

	public enum Operations
	{
		// unary ops:
		Plus,
		Minus,
		LogicNegation,
		BitNegation,
		AtSign,
		Print,
		Clone,

		// casts:
		BoolCast,
		Int8Cast,
		Int16Cast,
		Int32Cast,
		Int64Cast,
		UInt8Cast,
		UInt16Cast,
		UInt32Cast,
		UInt64Cast,
		DoubleCast,
		FloatCast,
		DecimalCast,
		StringCast,
        BinaryCast,
		UnicodeCast,
		ObjectCast,
		ArrayCast,
		UnsetCast,

		// binary ops:
		Xor, Or, And,
		BitOr, BitXor, BitAnd,
		Equal, NotEqual,
		Identical, NotIdentical,
		LessThan, GreaterThan, LessThanOrEqual, GreaterThanOrEqual,
		ShiftLeft, ShiftRight,
		Add, Sub, Mul, Div, Mod,
		Concat,

		// n-ary ops:
		ConcatN,
		List,
		Conditional,

		// assignments:
		AssignRef,
		AssignValue,
		AssignAdd,
		AssignSub,
		AssignMul,
		AssignDiv,
		AssignMod,
		AssignAnd,
		AssignOr,
		AssignXor,
		AssignShiftLeft,
		AssignShiftRight,
		AssignAppend,
		AssignPrepend,

		// constants, variables, fields, items:
		GlobalConstUse,
		ClassConstUse,
		PseudoConstUse,
		DirectVarUse,
		IndirectVarUse,
		DirectStaticFieldUse,
		IndirectStaticFieldUse,
		ItemUse,

		// literals:
		NullLiteral,
		BoolLiteral,
		IntLiteral,
		LongIntLiteral,
		DoubleLiteral,
		StringLiteral,
		BinaryStringLiteral,

		// routine calls:
		DirectCall,
		IndirectCall,
		DirectStaticCall,
		IndirectStaticCall,

		// instances:
		New,
		Array,
		InstanceOf,
		TypeOf,

		// built-in functions:
		Inclusion,
		Isset,
		Empty,
		Eval,

		// others:
		Exit,
		ShellCommand,
		IncDec,
        Yield,

        // lambda function:
        Closure,
	}

	#endregion

	#region Expression

	/// <summary>
	/// Abstract base class for expressions.
	/// </summary>
    [Serializable]
    public abstract class Expression : LangElement
	{
		public abstract Operations Operation { get; }

		/// <summary>
        /// Gets or sets possible types that the expression can evaluate to. 
        /// This memeber might be null and should not be accessed without a null check.
        /// </summary>
        internal IExTypeInfo TypeInfo { get; set; }

		protected Expression(Position position) : base(position) { }

		internal bool HasValue { get { return ValueTypeCode != PhpTypeCode.Unknown; } }
        public virtual object Value { get { return null; } }
		internal virtual PhpTypeCode ValueTypeCode { get { return PhpTypeCode.Unknown; } }

        /// <summary>
        /// Whether the expression is allowed to be passed by reference to a routine.
        /// </summary>
        internal virtual bool AllowsPassByReference { get { return false; } }

		/// <summary>
		/// Whether to mark sequence point when the expression appears in an expression statement.
		/// </summary>
		internal virtual bool DoMarkSequencePoint { get { return true; } }
	}

	#endregion

	#region ConstantDecl

    [Serializable]
    public abstract class ConstantDecl : LangElement
	{
		public VariableName Name { get { return name; } }
		protected VariableName name;

        public Expression/*!*/ Initializer { get { return initializer; } internal set { initializer = value; } }
		private Expression/*!*/ initializer;

		public ConstantDecl(Position position, string/*!*/ name, Expression/*!*/ initializer)
			: base(position)
		{
			this.name = new VariableName(name);
			this.initializer = initializer;
		}
	}

	#endregion

	#region VarLikeConstructUse

	/// <summary>
	/// Common abstract base class representing all constructs that behave like a variable (L-value).
	/// </summary>
    [Serializable]
	public abstract class VarLikeConstructUse : Expression
	{
        public VarLikeConstructUse IsMemberOf { get { return isMemberOf; } set { isMemberOf = value; } }
        protected VarLikeConstructUse isMemberOf;
            
		internal override bool AllowsPassByReference { get { return true; } }

		protected VarLikeConstructUse(Position p) : base(p) { }
	}

	#endregion
}