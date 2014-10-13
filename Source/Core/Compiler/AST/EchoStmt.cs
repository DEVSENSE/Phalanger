/*

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
using System.Reflection;
using System.Reflection.Emit;

using PHP.Core.AST;
using PHP.Core.Parsers;

namespace PHP.Core.Compiler.AST
{
    partial class NodeCompilers
    {
        [NodeCompiler(typeof(EchoStmt), Singleton = true)]
        sealed class EchoStmtCompiler : StatementCompiler<EchoStmt>
        {
            internal override Core.AST.Statement Analyze(EchoStmt node, Analyzer analyzer)
            {
                if (analyzer.IsThisCodeUnreachable())
                {
                    analyzer.ReportUnreachableCode(node.Span);
                    return EmptyStmt.Unreachable;
                }

                ExInfoFromParent info = ExInfoFromParent.DefaultExInfo;
                
                var parameters = node.Parameters;
                for (int i = 0; i < parameters.Length; i++)
                {
                    parameters[i] = parameters[i].Analyze(analyzer, info).Literalize();
                }

                return node;
            }

            /// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
            /// <param name="node">Instance.</param>
            /// <remarks>
            /// Nothing is expected on the evaluation stack. Nothing is left on the evaluation stack.
            /// </remarks>
            internal override void Emit(EchoStmt node, CodeGenerator codeGenerator)
            {
                Statistics.AST.AddNode("EchoStmt");

                codeGenerator.MarkSequencePoint(node.Span);
                foreach (Expression parameter in node.Parameters)
                {
                    // skip empty evaluated expression
                    var value = parameter.GetValue();
                    if (parameter.HasValue() &&
                        (
                            value == null ||
                            (value is string && ((string)value) == string.Empty) ||
                            Convert.ObjectToPhpBytes(value).Length == 0
                        ))
                    {
                        continue;
                    }

                    // emit the echo of parameter expression
                    codeGenerator.EmitEcho(parameter);
                }
            }
        }
    }
}