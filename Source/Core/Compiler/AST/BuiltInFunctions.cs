/*

 Copyright (c) 2006- DEVSENSE
 Copyright (c) 2004-2006 Tomas Matousek, Vaclav Novak and Martin Maly.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using PHP.Core.Parsers;
using PHP.Core.Reflection;

namespace PHP.Core.AST
{
    #region IncludingEx

    /// <summary>
	/// Inclusion expression (include, require, synthetic auto-inclusion nodes).
	/// </summary>
    [Serializable]
	public sealed class IncludingEx : Expression
	{
        public override Operations Operation { get { return Operations.Inclusion; } }

		/// <summary>
		/// An argument of the inclusion.
		/// </summary>
        public Expression/*!*/ Target { get { return fileNameEx; } set { fileNameEx = value; } }
		private Expression/*!*/ fileNameEx;

		/// <summary>
		/// A type of an inclusion (include, include-once, ...).
		/// </summary>
		public InclusionTypes InclusionType { get { return inclusionType; } }
		private InclusionTypes inclusionType;

		/// <summary>
		/// Whether the inclusion is conditional.
		/// </summary>
		public bool IsConditional { get { return isConditional; } }
		private bool isConditional;

		public Scope Scope { get { return scope; } }
		private Scope scope;

		public SourceUnit/*!*/ SourceUnit { get { return sourceUnit; } }
		private SourceUnit/*!*/ sourceUnit;

		public IncludingEx(SourceUnit/*!*/ sourceUnit, Scope scope, bool isConditional, Position position,
			InclusionTypes inclusionType, Expression/*!*/ fileName)
			: base(position)
		{
			Debug.Assert(fileName != null);

			this.inclusionType = inclusionType;
			this.fileNameEx = fileName;
			this.scope = scope;
			this.isConditional = isConditional;
			this.sourceUnit = sourceUnit;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitIncludingEx(this);
        }
	}

	#endregion

	#region IssetEx

	/// <summary>
	/// Represents <c>isset</c> construct.
	/// </summary>
    [Serializable]
	public sealed class IssetEx : Expression
	{
        public override Operations Operation { get { return Operations.Isset; } }

		private readonly List<VariableUse>/*!*/ varList;
        /// <summary>List of variables to test</summary>
        public List<VariableUse>/*!*/ VarList { get { return varList; } }

		public IssetEx(Position position, List<VariableUse>/*!*/ varList)
			: base(position)
		{
			Debug.Assert(varList != null && varList.Count > 0);
			this.varList = varList;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitIssetEx(this);
        }
	}

	#endregion

	#region EmptyEx

	/// <summary>
	/// Represents <c>empty</c> construct.
	/// </summary>
    [Serializable]
	public sealed class EmptyEx : Expression
	{
        public override Operations Operation { get { return Operations.Empty; } }

        /// <summary>
        /// Expression to be checked for emptiness.
        /// </summary>
        public Expression/*!*/Expression { get { return this.expression; } set { this.expression = value; } }
        private Expression/*!*/expression;
        
        public EmptyEx(Position p, Expression expression)
			: base(p)
		{
            if (expression == null)
                throw new ArgumentNullException("expression");

            this.expression = expression;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitEmptyEx(this);
        }
	}

	#endregion

	#region EvalEx

	/// <summary>
	/// Represents <c>eval</c> construct.
	/// </summary>
    [Serializable]
	public sealed class EvalEx : Expression
	{
        public override Operations Operation { get { return Operations.Eval; } }

		internal override bool DoMarkSequencePoint { get { return kind != EvalKinds.SyntheticEval; } }

		/// <summary>Expression containing source code to be evaluated.</summary>
        public Expression /*!*/ Code { get { return code; } set { code = value; } }

        /// <summary>
        /// Expression containing source code to be evaluated.
        /// </summary>
        private Expression/*!*/ code;
        
		/// <summary>
        /// Aliases copied from current scope which were valid in place of this expression.
        /// Used for deferred code compilation in run time, when creating transient compilation unit.
        /// </summary>
        public Dictionary<string, QualifiedName> CurrentAliases { get { return _aliases; } }
        private readonly Dictionary<string, QualifiedName> _aliases;

        /// <summary>
        /// Current namespace.
        /// Used for deferred code compilation in run time, when creating transient compilation unit.
        /// </summary>
        public QualifiedName? CurrentNamespace { get { return _namespace; } }
        private readonly QualifiedName? _namespace;

		/// <summary>
		/// Says if this eval is real in source code, or if it was made during analyzis to
		/// defer some compilation to run-time.
		/// </summary>
        public EvalKinds Kind { get { return kind; } }
		private EvalKinds kind;

		#region Construction

		/// <summary>
		/// Creates a node representing an eval or assert constructs.
		/// </summary>
		/// <param name="position">Position.</param>
		/// <param name="code">Source code expression.</param>
		/// <param name="isAssert">Whether the node represents an assert construct.</param>
		public EvalEx(Position position, Expression/*!*/ code, bool isAssert)
			: base(position)
		{
			this.kind = (isAssert) ? EvalKinds.Assert : EvalKinds.ExplicitEval;
			this.code = code;
		}

		/// <summary>
		/// Creates a node representing an eval-like construct (eval, assert, deferred type).
		/// </summary>
		/// <param name="position">Position.</param>
		/// <param name="code">Source code.</param>
        /// <param name="currentNamespace">Current namespace to be passed into the transient compilation unit.</param>
        /// <param name="aliases">Aliases to be passed into the transient compilation unit.</param>
		/// <remarks>
		/// Creates a node which actually doesn't exist in the source code but represents a piece of code 
		/// which compilation has been deferred to the run-time. It is used for example when
		/// a class is declared to be child of a unknown class or interface.
		/// </remarks>
		public EvalEx(Position position, string code, QualifiedName? currentNamespace, Dictionary<string,QualifiedName> aliases)
			: base(position)
		{
			this.kind = EvalKinds.SyntheticEval;
			this.code = new StringLiteral(position, code);

            _namespace = currentNamespace;
            _aliases = aliases;
		}

		#endregion

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitEvalEx(this);
        }
	}

	#endregion

	#region ExitEx

	/// <summary>
	/// Represents <c>exit</c> expression.
	/// </summary>
    [Serializable]
	public sealed class ExitEx : Expression
	{
        public override Operations Operation { get { return Operations.Exit; } }

		/// <summary>Die (exit) expression. Can be null.</summary>
        public Expression ResulExpr { get { return resultExpr; } set { resultExpr = value; } }
        private Expression resultExpr; //can be null
        
		public ExitEx(Position position, Expression resultExpr)
			: base(position)
		{
			this.resultExpr = resultExpr;
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitExitEx(this);
        }
	}

	#endregion
}
