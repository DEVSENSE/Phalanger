/*

 Copyright (c) 2013 DEVSENSE

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Diagnostics;
using System.Reflection.Emit;

using PHP.Core.AST;
using PHP.Core.Emit;
using PHP.Core.Parsers;

namespace PHP.Core.Compiler.AST
{
    partial class NodeCompilers
    {
        [NodeCompiler(typeof(IncDecEx))]
        sealed class IncDecExCompiler : ExpressionCompiler<IncDecEx>
        {
            public override Evaluation Analyze(IncDecEx node, Analyzer analyzer, ExInfoFromParent info)
            {
                access = info.Access;
                ExInfoFromParent var_info = new ExInfoFromParent(node);
                var_info.Access = AccessType.ReadAndWrite;

                node.Variable.Analyze(analyzer, var_info);

                return new Evaluation(node);
            }

            public override PhpTypeCode Emit(IncDecEx node, CodeGenerator codeGenerator)
            {
                Statistics.AST.AddNode("IncDecEx");
                Debug.Assert(access == AccessType.Read || access == AccessType.None);

                AccessType old_selector = codeGenerator.AccessSelector;

                PhpTypeCode returned_typecode = PhpTypeCode.Void;

                codeGenerator.AccessSelector = AccessType.Write;
                codeGenerator.ChainBuilder.Create();
                node.Variable.Emit(codeGenerator);
                codeGenerator.AccessSelector = AccessType.Read;
                codeGenerator.ChainBuilder.Create();
                node.Variable.Emit(codeGenerator);
                codeGenerator.ChainBuilder.End();

                LocalBuilder old_value = null;

                if (access == AccessType.Read && node.Post)
                {
                    old_value = codeGenerator.IL.DeclareLocal(Types.Object[0]);
                    // Save variable's value for later use
                    codeGenerator.IL.Emit(OpCodes.Dup);
                    codeGenerator.IL.Stloc(old_value);
                }

                if (node.Inc)
                {
                    // Increment
                    codeGenerator.IL.Emit(OpCodes.Call, Methods.Operators.Increment);
                }
                else
                {
                    // Decrement
                    codeGenerator.IL.Emit(OpCodes.Call, Methods.Operators.Decrement);
                }

                codeGenerator.AccessSelector = AccessType.Write;
                if (access == AccessType.Read)
                {
                    if (node.Post)
                    {
                        node.Variable.EmitAssign(codeGenerator);
                        // Load original value (as was before operation)
                        codeGenerator.IL.Ldloc(old_value);
                    }
                    else
                    {
                        old_value = codeGenerator.IL.DeclareLocal(Types.Object[0]);
                        // pre-incrementation
                        // Load variable's value after operation
                        codeGenerator.IL.Emit(OpCodes.Dup);
                        codeGenerator.IL.Stloc(old_value);
                        node.Variable.EmitAssign(codeGenerator);
                        codeGenerator.IL.Ldloc(old_value);
                    }

                    returned_typecode = PhpTypeCode.Object;
                }
                else
                {
                    node.Variable.EmitAssign(codeGenerator);
                }
                codeGenerator.AccessSelector = old_selector;

                codeGenerator.ChainBuilder.End();

                return returned_typecode;
            }
        }
    }
}
