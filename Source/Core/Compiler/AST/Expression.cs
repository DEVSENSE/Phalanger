/*

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
using PHP.Core.Reflection;
using PHP.Core.Emit;
using PHP.Core.AST;

namespace PHP.Core.AST
{
	#region Enums: AccessType, Operations

	/// <summary>
	/// Access type - describes context within which an expression is used.
	/// </summary>
	public enum AccessType : byte
	{
		None,          // serves for case when Expression is body of a ExpressionStmt.
		// It is useless to push its value on the stack in that case
		Read,
		Write,         // this access can only have VariableUse of course
		ReadAndWrite,  // dtto, it serves for +=,*=, etc.
		ReadRef,       // this access can only have VarLikeConstructUse and RefAssignEx (eg. f($a=&$b); where decl. is: function f(&$x) {} )
		WriteRef,      // this access can only have VariableUse of course
		ReadUnknown,   // this access can only have VarLikeConstructUse and NewEx, 
		// when they are act. param whose related formal param is not known
		WriteAndReadRef,		/*this access can only have VariableUse, it is used in case like:
													function f(&$x) {}
													f($a=$b);
												*/
		WriteAndReadUnknown, //dtto, but it is used when the signature of called function is not known 
		/* It is because of implementation of code generation that we
			* do not use an AccessType WriteRefAndReadRef in case of ReafAssignEx
			* f(&$x){} 
			* f($a=&$b)
			*/
		ReadAndWriteAndReadRef, //for f($a+=$b);
		ReadAndWriteAndReadUnknown

	}

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

		// LINQ:
		Linq,
		LinqOpChain,
		LinqTuple,
		LinqTupleItemAccess,

        // lambda function:
        Closure,
	}

	#endregion

	#region Expression

	/// <summary>
	/// Abstract base class for expressions.
	/// </summary>
	public abstract class Expression : LangElement
	{
		internal abstract Operations Operation { get; }

		internal AccessType Access { get { return access; } }
		protected AccessType access;

		protected Expression(Position position) : base(position) { }

		internal virtual Evaluation EvaluatePriorAnalysis(SourceUnit/*!*/ sourceUnit)
		{
			// in-evaluable by default:
			return new Evaluation(this);
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Expression.Analyze"]/*'/>
		internal abstract Evaluation Analyze(Analyzer/*!*/ analyzer, ExInfoFromParent info);

		internal bool HasValue { get { return ValueTypeCode != PhpTypeCode.Unknown; } }
        public virtual object Value { get { return null; } }
		internal virtual PhpTypeCode ValueTypeCode { get { return PhpTypeCode.Unknown; } }
		
		/// <summary>
		/// Whether the expression can be used as a value of a custom attribute argument
		/// (a constant expression, CLR array, CLR type object).
		/// </summary>
		internal virtual bool IsCustomAttributeArgumentValue { get { return HasValue; } }

		internal virtual object Evaluate(object value) { return null; }
		internal virtual object Evaluate(object leftValue, object rightValue) { return null; }

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
		internal abstract PhpTypeCode Emit(CodeGenerator/*!*/ codeGenerator);

		/// <summary>
		/// Whether the expression is allowed to be passed by reference to a routine.
		/// </summary>
		internal virtual bool AllowsPassByReference { get { return false; } }

		/// <summary>
		/// Whether to mark sequence point when the expression appears in an expression statement.
		/// </summary>
		internal virtual bool DoMarkSequencePoint { get { return true; } }

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="IsDeeplyCopied"]/*'/>
		internal virtual bool IsDeeplyCopied(CopyReason reason, int nestingLevel)
		{
			return true;
		}

		/// <summary>
		/// Whether an expression represented by this node should be stored to a temporary local if assigned.
		/// </summary>
		internal virtual bool StoreOnAssignment()
		{
			return true;
		}

		internal virtual void DumpTo(AstVisitor/*!*/ visitor, TextWriter/*!*/ output)
		{

		}

		internal void DumpAccess(TextWriter/*!*/ output)
		{
			output.Write(GetShortAccessString());

		}

		internal string GetShortAccessString()
		{
			switch (access)
			{
				case AccessType.None: return "N";
				case AccessType.Read: return "R";
				case AccessType.Write: return "W";
				case AccessType.ReadAndWrite: return "RW";
				case AccessType.ReadRef: return "R&";
				case AccessType.WriteRef: return "W&";
				case AccessType.ReadUnknown: return "R?";
				case AccessType.WriteAndReadRef: return "WR&";
				case AccessType.WriteAndReadUnknown: return "WR?";
				case AccessType.ReadAndWriteAndReadRef: return "RWR&";
				case AccessType.ReadAndWriteAndReadUnknown: return "RWR?";
			}
			Debug.Fail();
			return null;
		}

		internal string Dump(AstVisitor/*!*/ visitor)
		{
			StringWriter s = new StringWriter();
			DumpTo(visitor, s);
			return s.ToString();
		}
	}

	#endregion

	#region ExpressionPlace

    /// <summary>
    /// <see cref="IPlace"/> representing an <see cref="Expression"/>. Supports only loading onto the top of evaluation stack.
    /// </summary>
	internal sealed class ExpressionPlace : IPlace
	{
		private CodeGenerator/*!*/ codeGenerator;
		private Expression/*!*/ expression;

		public PhpTypeCode TypeCode { get { return typeCode; } }
		private PhpTypeCode typeCode;

        /// <summary>
        /// Get the expression if given place represents ExpressionPlace.
        /// </summary>
        /// <param name="place"></param>
        /// <returns></returns>
        public static Expression GetExpression(IPlace place)
        {
            if (place != null && place.GetType() == typeof(ExpressionPlace))
                return ((ExpressionPlace)place).expression;
            else
                return null;
        }

		public ExpressionPlace(CodeGenerator/*!*/ codeGenerator, Expression/*!*/ expression)
		{
			this.codeGenerator = codeGenerator;
			this.expression = expression;
			this.typeCode = PhpTypeCode.Invalid;
		}

		#region IPlace Members

		public void EmitLoad(ILEmitter/*!*/ il)
		{
			Debug.Assert(ReferenceEquals(il, codeGenerator.IL));
			typeCode = expression.Emit(codeGenerator);
		}

		public void EmitStore(ILEmitter/*!*/ il)
		{
			throw new InvalidOperationException();
		}

		public void EmitLoadAddress(ILEmitter/*!*/ il)
		{
			throw new InvalidOperationException();
		}

		public bool HasAddress
		{
			get { return false; }
		}

		public Type PlaceType
		{
			get { return (typeCode != PhpTypeCode.Invalid) ? PhpTypeCodeEnum.ToType(typeCode) : null; }
		}

		#endregion
	}


	#endregion

	#region ConstantDecl

	public abstract class ConstantDecl : LangElement
	{
		public VariableName Name { get { return name; } }
		protected VariableName name;

        public Expression/*!*/ Initializer { get { return initializer; } }
		protected Expression/*!*/ initializer;

		/// <summary>
		/// Whether the node has been analyzed.
		/// </summary>
		protected bool analyzed;

        public abstract KnownConstant Constant { get; }

		public ConstantDecl(Position position, string/*!*/ name, Expression/*!*/ initializer)
			: base(position)
		{
			this.name = new VariableName(name);
			this.initializer = initializer;
			this.analyzed = false;
		}

		internal virtual void Analyze(Analyzer/*!*/ analyzer)
		{
            if (!this.analyzed && Constant != null) // J: Constant can be null, if there was an error
			{
				Evaluation eval = initializer.Analyze(analyzer, ExInfoFromParent.DefaultExInfo);
				if (eval.HasValue)
				{
					Constant.SetValue(eval.Value);
				}
				else
				{
					this.initializer = eval.Expression;
					Constant.SetNode(this);
				}

				this.analyzed = true;
			}
		}

	}

	#endregion

	#region VarLikeConstructUse

	/// <summary>
	/// Common abstract base class representing all constructs that behave like a variable (L-value).
	/// </summary>
	public abstract class VarLikeConstructUse : Expression
	{
		internal override bool AllowsPassByReference { get { return true; } }

		public VarLikeConstructUse IsMemberOf { get { return isMemberOf; } set { isMemberOf = value; } }
		protected VarLikeConstructUse isMemberOf;

		protected VarLikeConstructUse(Position p) : base(p) { }

		internal override Evaluation Analyze(Analyzer/*!*/ analyzer, ExInfoFromParent info)
		{
			if (isMemberOf != null)
                isMemberOf.Analyze(analyzer, new ExInfoFromParent(this, DetermineAccessType(info.Access)));
			
			return new Evaluation(this);
		}

        /// <summary>
        /// Determine the AccessType based in <c>isMemberOf</c> type and <c>AccessType</c> of parent.
        /// </summary>
        /// <param name="parentInfoAccess"></param>
        /// <returns></returns>
        private AccessType DetermineAccessType(AccessType parentInfoAccess)
        {
            Debug.Assert(isMemberOf != null);

            switch (parentInfoAccess)
            {
                case AccessType.Write:
                    // Example: $x->f()->c = "foo";
                    // Chain is read up to function call "$x->f()", the rest is written "->c"
                    return (isMemberOf is FunctionCall) ? AccessType.Read : AccessType.Write;
                    
                case AccessType.WriteRef:
                    // Example: $x->f()->c =& $v;
                    // Chain is read up to function call "$x->f()", the rest is written or written ref "->c"
                    return (isMemberOf is FunctionCall) ? AccessType.Read : AccessType.Write;
                    
                case AccessType.ReadRef:
                    return (isMemberOf is FunctionCall || this is FunctionCall) ? AccessType.Read : AccessType.Write;
                    
                case AccessType.ReadAndWriteAndReadRef:
                case AccessType.WriteAndReadRef:
                case AccessType.ReadAndWrite:
                    // Example: $x->f()->c = "foo";
                    // Chain is read up to function call "$x->f()", the rest is both read and written "->c"
                    return (isMemberOf is FunctionCall) ? AccessType.Read : AccessType.ReadAndWrite;
                    
                case AccessType.WriteAndReadUnknown:
                case AccessType.ReadAndWriteAndReadUnknown:
                    return (isMemberOf is FunctionCall) ? AccessType.Read : parentInfoAccess;
                    
                case AccessType.ReadUnknown:
                    return (isMemberOf is FunctionCall || this is FunctionCall) ? AccessType.Read : AccessType.ReadUnknown;

                default:
                    return AccessType.Read;
            }
        }

        
	}

	#endregion

	#region ExInfoFromParent

	/// <summary>
	/// Structure used to pass inherited attributes during expression analyzis.
	/// </summary>
	internal struct ExInfoFromParent
	{
		public AccessType Access { get { return access; } set { access = value; } }
		public AccessType access;

		/// <summary>
		/// Used only by DirectVarUse to avoid assigning to $this. 
		/// Can be null reference if not needed.
		/// </summary>
		public object Parent { get { return parent; } }
		private object parent;

		public readonly static ExInfoFromParent DefaultExInfo = new ExInfoFromParent(null);

		public ExInfoFromParent(object parent)
		{
			this.parent = parent;
			this.access = AccessType.Read;
		}

		public ExInfoFromParent(object parent, AccessType access)
		{
			this.parent = parent;
			this.access = access;
		}
	}

	#endregion
}