/*

 Copyright (c) 2004-2006 Tomas Matousek and Vaclav Novak.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Reflection.Emit;
using PHP.Core.Emit;
using System.Diagnostics;
using PHP.Core.Parsers;

namespace PHP.Core.AST
{
	/// <summary>
	/// Represents a content of backtick operator (shell command execution).
	/// </summary>
	public sealed class ShellEx : Expression
	{
		internal override Operations Operation { get { return Operations.ShellCommand; } }

		private Expression/*!*/ command;
        /// <summary>Command to excute</summary>
        public Expression/*!*/ Command { get { return command; } }

		public ShellEx(Position position, Expression/*!*/ command)
			: base(position)
		{
#if SILVERLIGHT
			// SILVERLIGHT: Handle this in the parser!
			throw new NotSupportedException("ShellEx not supported on Silverlight!");
#endif
            Debug.Assert(command is StringLiteral || command is ConcatEx || command is BinaryStringLiteral);
			this.command = command;
		}

		internal override Evaluation Analyze(Analyzer/*!*/ analyzer, ExInfoFromParent info)
		{
			access = info.Access;
			command = command.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();
			return new Evaluation(this);
		}

		internal override bool IsDeeplyCopied(CopyReason reason, int nestingLevel)
		{
			// always returns a string:
			return false;
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
		internal override PhpTypeCode Emit(CodeGenerator/*!*/ codeGenerator)
		{
#if !SILVERLIGHT
			Debug.Assert(access == AccessType.Read || access == AccessType.None);
			Statistics.AST.AddNode("ShellEx");

			// CALL Execution.ShellExec(<(string) command>);
			codeGenerator.EmitConversion(command, PhpTypeCode.String);
			codeGenerator.IL.Emit(OpCodes.Call, Methods.ShellExec);

			if (access == AccessType.None)
			{
				codeGenerator.IL.Emit(OpCodes.Pop);
				return PhpTypeCode.Void;
			}
#endif

			// ShellExec returns a string containing the standard output of executed command
			return PhpTypeCode.String;
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
