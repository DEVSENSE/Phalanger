/*

 Copyright (c) 2004-2006 Tomas Matousek, Vaclav Novak and Martin Maly.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using PHP.Core.Emit;
using System.Diagnostics;
using PHP.Core.Parsers;

namespace PHP.Core.AST
{
	#region SwitchStmt

	/// <summary>
	/// Switch statement.
	/// </summary>
    [Serializable]
	public sealed class SwitchStmt : Statement
	{
		/// <summary>Value to switch by</summary>
        public Expression/*!*/ SwitchValue { get { return switchValue; } internal set { switchValue = value; } }
        private Expression/*!*/ switchValue;
        /// <summary>Body of switch statement</summary>
        public List<SwitchItem>/*!*/ SwitchItems { get { return switchItems; } }
        private List<SwitchItem>/*!*/ switchItems;
        
		public SwitchStmt(Position position, Expression/*!*/ switchValue, List<SwitchItem>/*!*/ switchItems)
			: base(position)
		{
			Debug.Assert(switchValue != null && switchItems != null);

			this.switchValue = switchValue;
			this.switchItems = switchItems;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitSwitchStmt(this);
        }
	}

	#endregion

	#region SwitchItem

	/// <summary>
	/// Base class for switch case/default items.
	/// </summary>
    [Serializable]
    public abstract class SwitchItem : LangElement
	{
		protected readonly List<Statement>/*!*/ statements;
        /// <summary>Statements in this part of switch</summary>
        public List<Statement>/*!*/ Statements { get { return statements; } }

		protected SwitchItem(Position position, List<Statement>/*!*/ statements)
			: base(position)
		{
			Debug.Assert(statements != null);
			this.statements = statements;
		}
	}

	/// <summary>
	/// Switch <c>case</c> item.
	/// </summary>
    [Serializable]
    public sealed class CaseItem : SwitchItem
	{
        /// <summary>Value to compare with swich expression</summary>
        public Expression CaseVal { get { return caseVal; } internal set { caseVal = value; } }
        private Expression caseVal;

		public CaseItem(Position position, Expression/*!*/ caseVal, List<Statement>/*!*/ statements)
			: base(position, statements)
		{
			Debug.Assert(caseVal != null);
			this.caseVal = caseVal;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitCaseItem(this);
        }
	}

	/// <summary>
	/// Switch <c>default</c> item.
	/// </summary>
    [Serializable]
    public sealed class DefaultItem : SwitchItem
	{
		public DefaultItem(Position position, List<Statement>/*!*/ statements)
			: base(position, statements)
		{
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitDefaultItem(this);
        }
    }

	#endregion
}
