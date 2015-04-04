/*

 Copyright (c) 2006- DEVSENSE
 Copyright (c) 2004-2006 Tomas Matousek, Ladislav Prosek, Vaclav Novak, and Martin Maly.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Collections.Generic;

using PHP.Core.Parsers;

namespace PHP.Core.AST
{
	#region JumpStmt

	/// <summary>
	/// Represents a branching (jump) statement (return, continue, break). 
	/// </summary>
	[Serializable]
    public sealed class JumpStmt : Statement
	{
		/// <summary>
		/// Type of the statement.
		/// </summary>
		public enum Types { Return, Continue, Break };

		private Types type;
        /// <summary>Type of current statement</summary>
        public Types Type { get { return type; } }

		/// <summary>
        /// In case of continue and break, it is number of loop statements to skip. Note that switch is considered to be a loop for this case
        /// In case of return, it represents the returned expression.
        /// Can be null.
		/// </summary>
        public Expression Expression { get { return expr; } internal set { expr = value; } }
		private Expression expr; // can be null

		public JumpStmt(Text.Span span, Types type, Expression expr)
			: base(span)
		{
			this.type = type;
			this.expr = expr;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitJumpStmt(this);
        }
	}

	#endregion

	#region GotoStmt

    [Serializable]
    public sealed class GotoStmt : Statement
	{
		/// <summary>Label that is target of goto statement</summary>
        public VariableName LabelName { get { return _labelName; } }
        private VariableName _labelName;

        /// <summary>
        /// Position of the <see cref="LabelName"/>.
        /// </summary>
        public Text.Span NameSpan { get { return _nameSpan; } }
        private readonly Text.Span _nameSpan;
        
		public GotoStmt(Text.Span span, string/*!*/labelName, Text.Span nameSpan)
			: base(span)
		{
            Debug.Assert(!string.IsNullOrEmpty(labelName));
			_labelName = new VariableName(labelName);
            _nameSpan = nameSpan;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitGotoStmt(this);
        }
	}

	#endregion

	#region LabelStmt

    [Serializable]
    public sealed class LabelStmt : Statement
	{
        public VariableName Name { get { return _name; } }
		private VariableName _name;

		internal Label Label { get { return label; } set { label = value; } }
		private Label label;

		internal bool IsReferred { get { return isReferred; } set { isReferred = value; } }
		private bool isReferred;

        /// <summary>
        /// Position of the <see cref="Name"/>.
        /// </summary>
        public Text.Span NameSpan { get { return _nameSpan; } }
        private readonly Text.Span _nameSpan;
        

		public LabelStmt(Text.Span span, string/*!*/name, Text.Span nameSpan)
			: base(span)
		{
            Debug.Assert(!string.IsNullOrEmpty(name));

			_name = new VariableName(name);
            _nameSpan = nameSpan;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitLabelStmt(this);
        }
	}

	#endregion
}
