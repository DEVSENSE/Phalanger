/*

 Copyright (c) 2006- DEVSENSE
 Copyright (c) 2004-2006 Tomas Matousek, Vaclav Novak and Martin Maly.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using PHP.Core.Parsers;

namespace PHP.Core.AST
{
	/// <summary>
	/// Represents an <c>echo</c> statement.
	/// </summary>
    [Serializable]
	public sealed class EchoStmt : Statement
	{
		/// <summary>Array of parameters - Expressions.</summary>
        public List<Expression> /*!*/ Parameters { get { return parameters; } }
        private List<Expression>/*!*/ parameters;
        
        /// <summary>
        /// Gets value indicating whether this <see cref="EchoStmt"/> represents HTML code.
        /// </summary>
        public bool IsHtmlCode { get { return isHtmlCode; } }
        private readonly bool isHtmlCode;

		public EchoStmt(Position position, List<Expression>/*!*/ parameters)
			: base(position)
		{
			Debug.Assert(parameters != null);
			this.parameters = parameters;
            this.isHtmlCode = false;
		}

        /// <summary>
        /// Initializes new echo statement as a representation of HTML code.
        /// </summary>
        public EchoStmt(Position position, string htmlCode)
            : base(position)
        {
            this.parameters = new List<Expression>(1) { new StringLiteral(position, htmlCode) };
            this.isHtmlCode = true;
        }

		internal override bool SkipInPureGlobalCode()
		{
			StringLiteral literal;
			if (parameters.Count == 1 && (literal = parameters[0] as StringLiteral) != null)
			{
				return StringUtils.IsWhitespace((string)literal.Value);
			}
			else
			{
				return false;
			}
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitEchoStmt(this);
        }
	}
}