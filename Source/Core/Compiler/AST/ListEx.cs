/*

 Copyright (c) 2013 DEVSENSE

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Diagnostics;

using PHP.Core.AST;
using PHP.Core.Emit;
using PHP.Core.Parsers;

namespace PHP.Core.Compiler.AST
{
    partial class NodeCompilers
    {
        [NodeCompiler(typeof(ListEx))]
        sealed class ListExCompiler : ExpressionCompiler<ListEx>
        {
            public override Evaluation Analyze(ListEx node, Analyzer analyzer, ExInfoFromParent info)
            {
                access = info.Access;
                ExInfoFromParent sinfo = new ExInfoFromParent(node);

                // r-value
                if (node.RValue != null)
                    node.RValue = node.RValue.Analyze(analyzer, sinfo).Literalize();

                // l-values
                sinfo.Access = AccessType.Write;

                for (int i = 0; i < node.LValues.Count; i++)
                {
                    if (node.LValues[i] != null)
                        node.LValues[i] = node.LValues[i].Analyze(analyzer, sinfo).Expression;
                }

                return new Evaluation(node);
            }

            public override PhpTypeCode Emit(ListEx node, CodeGenerator codeGenerator)
            {
                Statistics.AST.AddNode("ListEx");

                Debug.Assert(access == AccessType.Read || access == AccessType.None);
                Debug.Assert(node.RValue != null);   // the root of the lists structure must have RValue assigned. list(whatever) = RValue

                codeGenerator.EmitBoxing(node.RValue.Emit(codeGenerator));   // put object on the top of the stack

                // assign the value from top of evaluation stack to the list
                return this.EmitAssign(node, codeGenerator);
            }

            /// <summary>
            /// Assigns items of array from the top of evaluation stack to the list.
            /// </summary>
            internal PhpTypeCode EmitAssign(ListEx listex, CodeGenerator codeGenerator)
            {
                LocalBuilder o1 = codeGenerator.IL.GetTemporaryLocal(Types.Object[0]);   // temporary variable for object to be copied
                EmitAssignList(codeGenerator, listex.LValues, o1);                 // assign particular elements of the list, using the array from the stack

                // return temporary local
                codeGenerator.IL.ReturnTemporaryLocal(o1);

                // the original top of the stack is replaced with the instance of array or null
                if (access == AccessType.Read)
                {
                    return PhpTypeCode.PhpArray;    // return the top of the stack (null or array)
                }
                else
                {
                    codeGenerator.IL.Emit(OpCodes.Pop); // remove the top of the stack, not used
                    return PhpTypeCode.Void;
                }
            }

            /// <summary>
            /// Use the object on the top of the stack, the object here will stay untouched.
            /// 
            /// Assigns recursively into lvalues. If the object is PhpArray, assign items, otherwise assign nulls.
            /// </summary>
            /// <param name="codeGenerator"></param>
            /// <param name="vals">Arguments of the list expression.</param>
            /// <param name="o1">Temporary local variable.</param>
            /// <remarks>After the method finishes, the top of the stack is the same.</remarks>
            private static void EmitAssignList(CodeGenerator codeGenerator, List<Expression> vals, LocalBuilder o1)
            {
                Label end_label = codeGenerator.IL.DefineLabel();
                Label storearray_label = codeGenerator.IL.DefineLabel();

                // PUSH stack[0] as PhpArray
                codeGenerator.IL.Emit(OpCodes.Dup);                         // copy of the value, keep original value on the top of the stack
                codeGenerator.IL.Emit(OpCodes.Isinst, Types.PhpArray[0]);   // convert the top of the stack into PhpArray

                // the top of the stack points to array or null
                // if (stack[0] != null) goto storearray_label
                codeGenerator.IL.Emit(OpCodes.Dup);                         // copy of the value, keep original value on the top of the stack
                codeGenerator.IL.Emit(OpCodes.Brtrue, storearray_label);    // jump to storearray_label if conversion succeeded

                // Conversion to array failed, assign null into lvalues
                EmitAssignListNulls(codeGenerator, vals);                   // fill vals with null recursively
                codeGenerator.IL.Emit(OpCodes.Br, end_label);               // goto end_label

                // Conversion to PhpArray succeeded
                codeGenerator.IL.MarkLabel(storearray_label, true);

                // assign array items into lvalues
                EmitAssignListArray(codeGenerator, vals, o1);

                // End label
                codeGenerator.IL.MarkLabel(end_label, true);

                codeGenerator.IL.Emit(OpCodes.Pop); // remove the top of the stack (array or null), not used then
            }

            private static void EmitAssignListArray(CodeGenerator codeGenerator, List<Expression> vals, LocalBuilder o1)
            {
                //
                // the array is on the top of the evaluation stack, value will be kept, must be duplicated to be used
                //

                // Process in the reverse order !
                for (int i = vals.Count - 1; i >= 0; i--)
                {
                    if (vals[i] == null)
                        continue;

                    // push the array item onto the stack

                    // LOAD array.GetArrayItem(i,false)
                    codeGenerator.IL.Emit(OpCodes.Dup);         // copy of the array
                    codeGenerator.IL.Emit(OpCodes.Ldc_I4, i);   // i
                    codeGenerator.IL.Emit(OpCodes.Ldc_I4_0);    // false (!quiet)
                    codeGenerator.IL.Emit(OpCodes.Callvirt, Methods.PhpArray.GetArrayItem_Int32);

                    // assign the item from the stack into vals[i]

                    if (vals[i] is VariableUse)
                    {
                        // o1 = stack[0]
                        codeGenerator.IL.Stloc(o1);                 // store the value into local variable o1

                        // PREPARE <variable>:
                        codeGenerator.ChainBuilder.Create();
                        vals[i].Emit(codeGenerator);

                        // LOAD o1
                        codeGenerator.IL.Ldloc(o1);

                        // LOAD PhpVariable.Copy(STACK,CopyReason.Assigned)
                        codeGenerator.EmitVariableCopy(CopyReason.Assigned, null);

                        // STORE <variable>:
                        VariableUseHelper.EmitAssign((VariableUse)vals[i], codeGenerator);
                        codeGenerator.ChainBuilder.End();
                    }
                    else if (vals[i] is ListEx)
                    {
                        EmitAssignList(codeGenerator, (vals[i] as ListEx).LValues, o1);
                        codeGenerator.IL.Emit(OpCodes.Pop); // removes used value from the stack
                    }
                    else
                    {
                        codeGenerator.IL.Emit(OpCodes.Pop); // removes used value from the stack

                        Debug.Fail("Unsupported list argument of type " + vals[i].GetType().ToString());
                    }
                }
            }

            /// <summary>
            /// Assigns null into given lvalues recursively.
            /// </summary>
            /// <param name="codeGenerator"></param>
            /// <param name="vals"></param>
            private static void EmitAssignListNulls(CodeGenerator codeGenerator, List<Expression> vals)
            {
                // clear lvalues recursively

                for (int i = 0; i < vals.Count; ++i)
                {
                    if (vals[i] == null)
                        continue;

                    if (vals[i] is VariableUse)
                    {
                        // Prepare stack for writing result...
                        codeGenerator.ChainBuilder.Create();
                        (vals[i] as VariableUse).Emit(codeGenerator);

                        codeGenerator.IL.Emit(OpCodes.Ldnull);

                        // Store result
                        VariableUseHelper.EmitAssign((VariableUse)vals[i], codeGenerator);
                        codeGenerator.ChainBuilder.End();
                    }
                    else if (vals[i] is ListEx)
                    {
                        EmitAssignListNulls(codeGenerator, (vals[i] as ListEx).LValues);
                    }
                    else
                    {
                        Debug.Fail("Unsupported list argument of type " + vals[i].GetType().ToString());
                    }
                }
            }
        }
    }
}
