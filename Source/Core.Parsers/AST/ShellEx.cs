/*
 Copyright (c) 2006- DEVSENSE
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
	/// <summary>
	/// Represents a content of backtick operator (shell command execution).
	/// </summary>
    [Serializable]
	public sealed class ShellEx : Expression
	{
        public override Operations Operation { get { return Operations.ShellCommand; } }

		/// <summary>Command to excute</summary>
        public Expression/*!*/ Command { get { return command; } internal set { command = value; } }
        private Expression/*!*/ command;
        
		public ShellEx(Text.Span span, Expression/*!*/ command)
            : base(span)
		{
            Debug.Assert(command is StringLiteral || command is ConcatEx || command is BinaryStringLiteral);
			this.command = command;
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitShellEx(this);
        }
	}
}
