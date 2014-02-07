/*

 Copyright (c) 2004-2006 Tomas Matousek, Ladislav Prosek, Vaclav Novak and Martin Maly.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Diagnostics;

using PHP.Core.Parsers;

namespace PHP.Core.AST
{
	/// <summary>
	/// Direct variable use - a variable or a field accessed by an identifier.
	/// </summary>
    [Serializable]
	public sealed class DirectVarUse : SimpleVarUse
	{
        public override Operations Operation { get { return Operations.DirectVarUse; } }

		public VariableName VarName { get { return varName; } set { varName = value; } }
		private VariableName varName;

		public DirectVarUse(Text.Span span, VariableName varName)
            : base(span)
		{
			this.varName = varName;
		}

		public DirectVarUse(Text.Span span, string/*!*/ varName)
            : base(span)
		{
			this.varName = new VariableName(varName);
		}

		/// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitDirectVarUse(this);
        }
	}
}
