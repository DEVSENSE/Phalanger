/*

 Copyright (c) 2013 DEVSENSE
 
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Reflection.Emit;
using System.Diagnostics;

using PHP.Core;
using PHP.Core.AST;
using PHP.Core.Emit;
using PHP.Core.Parsers;

namespace PHP.Core.Compiler.AST
{
    partial class NodeCompilers
    {
        /// <summary>
        /// Represents a content of backtick operator (shell command execution).
        /// </summary>
        [NodeCompiler(typeof(ShellEx))]
        sealed class ShellExCompiler : ExpressionCompiler<ShellEx>
        {
            public override bool IsDeeplyCopied(ShellEx node, CopyReason reason, int nestingLevel)
            {
                // always returns a string:
                return false;
            }

            public override Evaluation Analyze(ShellEx node, Analyzer analyzer, ExInfoFromParent info)
            {
                access = info.Access;
                node.Command = node.Command.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();
                return new Evaluation(node);
            }

            public override PhpTypeCode Emit(ShellEx node, CodeGenerator codeGenerator)
            {
                Debug.Assert(access == AccessType.Read || access == AccessType.None);
                Statistics.AST.AddNode("ShellEx");

                // CALL Execution.ShellExec(<(string) command>);
                codeGenerator.EmitConversion(node.Command, PhpTypeCode.String);
                codeGenerator.IL.Emit(OpCodes.Call, Methods.ShellExec);

                if (access == AccessType.None)
                {
                    codeGenerator.IL.Emit(OpCodes.Pop);
                    return PhpTypeCode.Void;
                }

                // ShellExec returns a string containing the standard output of executed command
                return PhpTypeCode.String;
            }
        }
    }
}
