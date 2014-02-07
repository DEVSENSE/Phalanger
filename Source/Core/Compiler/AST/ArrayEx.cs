/*

 Copyright (c) 2013 DEVSENSE

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections;
using System.Collections.Generic;
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
        [NodeCompiler(typeof(ArrayEx))]
        sealed class ArrayExCompiler : VarLikeConstructUseCompiler<ArrayEx>
        {
            #region Analysis

            public override Evaluation Analyze(ArrayEx node, Analyzer analyzer, ExInfoFromParent info)
            {
                access = info.Access;

                foreach (var i in node.Items)
                    if (i != null)
                        i.NodeCompiler<ItemCompiler>().Analyze(i, analyzer);

                return new Evaluation(node);
            }

            #endregion

            #region Code Emission

            /// <returns>It suffice to make a copy only if assignment nesting level is 1 or above (we are starting from 0).</returns>
            public override bool IsDeeplyCopied(ArrayEx node, CopyReason reason, int nestingLevel)
            {
                return nestingLevel > 0;
            }

            public override PhpTypeCode Emit(ArrayEx node, CodeGenerator codeGenerator)
            {
                Debug.Assert(access == AccessType.Read || access == AccessType.None);
                ILEmitter il = codeGenerator.IL;

                // count integer and string keys:
                int int_count = 0;
                int string_count = 0;
                DetermineCapacities(node, out int_count, out string_count);

                // array = new PhpArray(<int_count>, <string_count>);
                il.Emit(OpCodes.Ldc_I4, int_count);
                il.Emit(OpCodes.Ldc_I4, string_count);
                il.Emit(OpCodes.Newobj, Constructors.PhpArray.Int32_Int32);

                if (codeGenerator.Context.Config.Compiler.Debug)
                {
                    il.Emit(OpCodes.Nop);
                    il.Emit(OpCodes.Nop);
                    il.Emit(OpCodes.Nop);
                }

                foreach (var item in node.Items)
                {
                    var itemcompiler = item.NodeCompiler<ItemCompiler>();
                    // CALL array.SetArrayItemRef(z, p);
                    // CALL array.SetArrayItem(x, PhpVariable.Copy(y, CopyReason.Assigned));
                    // CALL array.SetArrayItem(PhpVariable.Copy(x, CopyReason.Assigned))
                    // CALL array.AddToEnd(x)

                    il.Emit(OpCodes.Dup);
                    PhpTypeCode index_type_code = itemcompiler.EmitIndex(item, codeGenerator);
                    itemcompiler.EmitValue(item, codeGenerator);
                    codeGenerator.EmitSetArrayItem(index_type_code, item.Index, item is RefItem, true);
                }

                switch (this.access)
                {
                    case AccessType.Read:
                        // keep array on the stack
                        return PhpTypeCode.PhpArray;

                    case AccessType.None:
                        // pop array from the stack
                        il.Emit(OpCodes.Pop);
                        return PhpTypeCode.Void;
                }

                throw new InvalidOperationException();
                //return PhpTypeCode.Invalid;
            }

            private void DetermineCapacities(ArrayEx node, out int intCount, out int stringCount)
            {
                intCount = 0;
                stringCount = 0;

                foreach (var item in node.Items)
                {
                    if (item.HasKey)
                    {
                        if (item.IsIndexStringLiteral)
                            stringCount++;
                        else
                            intCount++; // Item is IntLiteral, Variable, Constant, etc.
                    }
                    else
                        intCount++;
                }
            }

            #endregion
        }

        #region ItemCompiler

        abstract class ItemCompiler : INodeCompiler
        {
            internal virtual void Analyze(Item/*!*/node,  Analyzer/*!*/ analyzer)
            {
                if (node.Index != null)
                    node.Index = node.Index.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();
            }

            /// <summary>
            /// Emit IL instructions that load the value of array index at the stack.
            /// </summary>
            internal PhpTypeCode EmitIndex(Item/*!*/node, CodeGenerator/*!*/ codeGenerator)
            {
                return codeGenerator.EmitArrayKey(null, node.Index);
            }

            internal abstract PhpTypeCode EmitValue(Item/*!*/node, CodeGenerator/*!*/ codeGenerator);
        }

        #endregion

        #region ValueItemCompiler

        [NodeCompiler(typeof(ValueItem), Singleton = true)]
        sealed class ValueItemCompiler : ItemCompiler
        {
            internal override void Analyze(Item node, Analyzer analyzer)
            {
                var valueitem = (ValueItem)node;

                base.Analyze(node, analyzer);
                valueitem.ValueExpr = valueitem.ValueExpr.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();
            }

            /// <summary>
            /// Emit IL instructions that load the value of array item at the stack and make a copy 
            /// of it if necessary.
            /// </summary>
            internal override PhpTypeCode EmitValue(Item/*!*/node, CodeGenerator/*!*/ codeGenerator)
            {
                var valueitem = (ValueItem)node;

                Debug.Assert(valueitem.ValueExpr != null);
                Statistics.AST.AddNode("Array.ValueItem");

                codeGenerator.EmitBoxing(valueitem.ValueExpr.Emit(codeGenerator));
                codeGenerator.EmitVariableCopy(CopyReason.Assigned, valueitem.ValueExpr);

                return PhpTypeCode.Object;
            }
        }

        #endregion

        #region RefItemCompiler

        /// <summary>
        /// Reference to a variable containing the value of an array item defined by <c>array</c> constructor.
        /// </summary>
        [NodeCompiler(typeof(RefItem), Singleton = true)]
        sealed class RefItemCompiler : ItemCompiler
        {
            internal override void Analyze(Item node, Analyzer analyzer)
            {
<<<<<<< HEAD
                ExInfoFromParent info = new ExInfoFromParent(this);
=======
                ExInfoFromParent info = new ExInfoFromParent(node);
>>>>>>> refs/remotes/tfs/default
                info.Access = AccessType.ReadRef;
                ((RefItem)node).RefToGet.Analyze(analyzer, info);
                base.Analyze(node, analyzer);
            }

            /// <summary>
            /// Emit IL instructions that load the value of array item at the stack and make a copy 
            /// of it if necessary.
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="codeGenerator"></param>
            /// <returns></returns>
            /// <remarks>This node represents the item x=>&amp;y in PHP notation. See <see cref="PHP.Core.AST.ArrayEx"/>
            ///  for more details.</remarks>
            internal override PhpTypeCode EmitValue(Item node, CodeGenerator codeGenerator)
            {
                Debug.Assert(((RefItem)node).RefToGet != null);
                Statistics.AST.AddNode("Array.RefItem");

                // Emit refToGet
                return ((RefItem)node).RefToGet.Emit(codeGenerator);
            }
        }

        #endregion
    }
}