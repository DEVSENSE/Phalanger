/*

 Copyright (c) 2004-2006 Tomas Matousek, Vaclav Novak and Martin Maly.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Reflection.Emit;
using System.Diagnostics;
using PHP.Core.Emit;
using PHP.Core.Parsers;

namespace PHP.Core.AST
{
	/// <summary>
	/// Base class for assignment expressions (by-value and by-ref).
	/// </summary>
	public abstract class AssignEx : Expression
	{
		internal override bool AllowsPassByReference { get { return true; } }

		internal VariableUse lvalue;
        /// <summary>Target of assignment</summary>
        public VariableUse LValue { get { return lvalue; } }

		protected AssignEx(Position p) : base(p) { }
	}

	#region ValueAssignEx

	/// <summary>
	/// By-value assignment expression with possibly associated operation.
	/// </summary>
	/// <remarks>
	/// Implements PHP operators: <c>=  +=  -=  *=  /=  %=  .= =.  &amp;=  |=  ^=  &lt;&lt;=  &gt;&gt;=</c>.
	/// </remarks>
	public sealed class ValueAssignEx : AssignEx
	{
        public override Operations Operation { get { return operation; } }
		internal Operations operation;

		internal Expression/*!*/ rvalue;
        /// <summary>Expression being assigned</summary>
        public Expression/*!*/RValue { get { return rvalue; } }

		public ValueAssignEx(Position position, Operations operation, VariableUse/*!*/ lvalue, Expression/*!*/ rvalue)
			: base(position)
		{
			this.lvalue = lvalue;
			this.rvalue = rvalue;
			this.operation = operation;
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitValueAssignEx(this);
        }
	}

	#endregion

	#region RefAssignEx

	/// <summary>
	/// By-reference assignment expression (<c>&amp;=</c> PHP operator).
	/// </summary>
	public sealed class RefAssignEx : AssignEx
	{
        public override Operations Operation { get { return Operations.AssignRef; } }

		/// <summary>Expression being assigned</summary>
        public Expression/*!*/RValue { get { return rvalue; } }
        internal Expression/*!*/ rvalue;
        
		public RefAssignEx(Position position, VariableUse/*!*/ lvalue, Expression/*!*/ rvalue)
			: base(position)
		{
			Debug.Assert(rvalue is VarLikeConstructUse || rvalue is NewEx);
			this.lvalue = lvalue;
			this.rvalue = rvalue;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitRefAssignEx(this);
        }
	}

	#endregion
}
