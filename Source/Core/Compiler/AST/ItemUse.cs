/*

 Copyright (c) 2004-2006 Tomas Matousek, Ladislav Prosek, Vaclav Novak, and Martin Maly.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Diagnostics;
using System.Reflection.Emit;
using PHP.Core.AST;
using PHP.Core.Emit;
using PHP.Core.Parsers;

namespace PHP.Core.Compiler.AST
{
    partial class NodeCompilers
    {
        #region ItemUse

        [NodeCompiler(typeof(ItemUse))]
        sealed class ItemUseCompiler : CompoundVarUseCompiler<ItemUse>
        {
            /// <summary>
            /// Set when the index is emitted.
            /// </summary>
            private PhpTypeCode indexTypeCode = PhpTypeCode.Unknown;

            public override Evaluation Analyze(ItemUse node, Analyzer analyzer, ExInfoFromParent info)
            {
                access = info.Access;

                // checks for write context of key-less array operator ($a =& $x[] is ok):
                if (node.Index == null
                    && (access == AccessType.Read
                        || access == AccessType.ReadAndWrite
                        || access == AccessType.ReadAndWriteAndReadRef
                        || access == AccessType.ReadAndWriteAndReadUnknown))
                {
                    analyzer.ErrorSink.Add(Errors.EmptyIndexInReadContext, analyzer.SourceUnit, node.Position);
                    return new Evaluation(node);
                }

                base.Analyze(node, analyzer, info);

                ExInfoFromParent sinfo = new ExInfoFromParent(node);
                switch (info.Access)
                {
                    case AccessType.Write:
                    case AccessType.WriteRef:
                    case AccessType.ReadRef: sinfo.Access = AccessType.Write; break;

                    case AccessType.ReadAndWriteAndReadRef:
                    case AccessType.WriteAndReadRef:
                    case AccessType.ReadAndWrite: sinfo.Access = AccessType.ReadAndWrite; break;

                    case AccessType.WriteAndReadUnknown:
                    case AccessType.ReadAndWriteAndReadUnknown: sinfo.Access = info.Access; break;

                    case AccessType.ReadUnknown: sinfo.Access = AccessType.ReadUnknown; break;
                    default: sinfo.Access = AccessType.Read; break;
                }

                ((ItemUse)node).Array.Analyze(analyzer, sinfo);

                if (node.Index != null)
                    node.Index = node.Index.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();

                return new Evaluation(node);
            }

            public override PhpTypeCode Emit(ItemUse node, CodeGenerator codeGenerator)
            {
                Statistics.AST.AddNode("ItemUse");
                PhpTypeCode result = PhpTypeCode.Invalid;

                switch (codeGenerator.SelectAccess(access))
                {
                    case AccessType.None:
                        result = EmitNodeRead((ItemUse)node, codeGenerator, Operators.GetItemKinds.Get);
                        codeGenerator.IL.Emit(OpCodes.Pop);
                        break;

                    case AccessType.Read:
                        result = EmitNodeRead((ItemUse)node, codeGenerator, Operators.GetItemKinds.Get);
                        break;

                    case AccessType.Write:
                        // prepares for write:
                        result = EmitNodeWrite((ItemUse)node, codeGenerator);
                        break;

                    case AccessType.ReadRef:
                        // if the selector is set to the ReadRef, the chain is emitted as if it was written
                        // (chained nodes are marked as ReadAndWrite):
                        if (codeGenerator.AccessSelector == AccessType.ReadRef)
                            codeGenerator.AccessSelector = AccessType.Write;

                        result = EmitNodeReadRef((ItemUse)node, codeGenerator);
                        break;

                    case AccessType.ReadUnknown:
                        result = EmitNodeReadUnknown((ItemUse)node, codeGenerator);
                        break;

                    case AccessType.WriteRef:
                        // prepares for write:					
                        result = EmitNodeWriteRef((ItemUse)node, codeGenerator);
                        break;

                    default:
                        Debug.Fail(null);
                        break;
                }

                return result;
            }

            internal override PhpTypeCode EmitAssign(ItemUse node, CodeGenerator codeGenerator)
            {
                var chain = codeGenerator.ChainBuilder;
                PhpTypeCode result;

                switch (access)
                {
                    case AccessType.WriteAndReadRef:
                    case AccessType.WriteAndReadUnknown:
                    case AccessType.ReadAndWrite:
                    case AccessType.ReadAndWriteAndReadRef:
                    case AccessType.ReadAndWriteAndReadUnknown:
                    case AccessType.Write:
                    case AccessType.WriteRef:
                        {
                            bool reference = access == AccessType.WriteRef;

                            // Note that some work was done in Emit() !
                            // In cases 3, 4, 5 EmitAssign is not called

                            if (node.IsMemberOf != null ||
                               (node.IsMemberOf == null && (node.Array is DirectStFldUse || node.Array is IndirectStFldUse || node.Array is ItemUse)))
                            {
                                // 2, 6, 7
                                chain.EmitSetArrayItem(indexTypeCode, node.Index, reference);
                                chain.End();
                            }
                            else
                            {
                                // Note: The value which should be stored is already loaded on the evaluation stack.
                                //				Push the destination array and index and call the operator
                                // 1: a_[x]_
                                Debug.Assert(node.Array is SimpleVarUse);
                                chain.IsArrayItem = true;
                                chain.IsLastMember = true;
                                indexTypeCode = codeGenerator.EmitArrayKey(chain, node.Index);
                                node.Array.Emit(codeGenerator);
                                chain.EmitSetItem(indexTypeCode, node.Index, reference);

                                // Store the changed variable into table of variables (do nothing in optimalized functions)
                                SimpleVarUseHelper.EmitLoadAddress_StoreBack((SimpleVarUse)node.Array, codeGenerator);
                            }

                            result = PhpTypeCode.Void;
                            break;
                        }

                    case AccessType.None:
                        // do nothing
                        result = PhpTypeCode.Void;
                        break;

                    case AccessType.Read:
                        // do nothing
                        result = PhpTypeCode.Object;
                        break;

                    case AccessType.ReadRef:
                        // Do nothing
                        result = PhpTypeCode.PhpReference;
                        break;

                    default:
                        Debug.Fail(null);
                        result = PhpTypeCode.Invalid;
                        break;
                }

                return result;
            }

            internal override void EmitUnset(ItemUse node, CodeGenerator codeGenerator)
            {
                var chain = codeGenerator.ChainBuilder;
                var itemuse = (ItemUse)node;

                // Template: "unset(x[y])"  Operators.UnsetItem(object obj,object index)

                // Case 3: a_[x]_[x] never reached
                Debug.Assert(chain.IsArrayItem == false);
                // Case 4,5 never reached
                // 4: a[x]->...
                // 5: ...->a[]->...
                Debug.Assert(chain.IsMember == false);

                chain.QuietRead = true;

                // 1, 2, 6, 7
                if (node.IsMemberOf != null)
                {
                    // 6 , 7: ...->a[]_[]_ , ...->a_[]_
                    chain.Create();
                    chain.Begin();
                    chain.Lengthen(); // for hop over ->
                    node.IsMemberOf.Emit(codeGenerator);
                    chain.IsArrayItem = true;
                    chain.IsLastMember = false;
                    chain.EmitUnsetItem(itemuse.Array, itemuse.Index);
                    chain.IsArrayItem = false;
                    chain.End();
                    return;
                }
                // 1, 2
                if (itemuse.Array is ItemUse || itemuse.Array is DirectStFldUse || itemuse.Array is IndirectStFldUse /* ??? */)
                {
                    // 2: a[]_[]_
                    chain.Create();
                    chain.Begin();
                    chain.IsArrayItem = true;
                    chain.IsLastMember = true;
                    chain.EmitUnsetItem(itemuse.Array, itemuse.Index);
                    chain.IsArrayItem = false;
                    chain.End();
                    return;
                }
                // 1: a_[x]_
                chain.IsArrayItem = true;
                chain.IsLastMember = true;
                chain.EmitUnsetItem(itemuse.Array, itemuse.Index);
                chain.IsArrayItem = false;
            }

            internal override PhpTypeCode EmitIsset(ItemUse node, CodeGenerator codeGenerator, bool empty)
            {
                //Template:
                //		"isset(x[y])"  Operators.GetItem(x,y) != null      

                codeGenerator.ChainBuilder.QuietRead = true;

                // GetItem(x,y) ...
                this.EmitNodeRead(node, codeGenerator, (empty) ? Operators.GetItemKinds.Empty : Operators.GetItemKinds.Isset);
                return PhpTypeCode.Object;
            }

            /// <summary>
            /// Emits code to load variable onto the evaluation stack. Supports operators chaining.
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="codeGenerator">A geenrator.</param>
            /// <param name="itemGetterKind">Whether to load for "get", "isset", or "empty".</param>
            private PhpTypeCode EmitNodeRead(ItemUse/*!*/node, CodeGenerator/*!*/ codeGenerator, Operators.GetItemKinds itemGetterKind)
            {
                var chain = codeGenerator.ChainBuilder;
                var itemuse = (ItemUse)node;
                PhpTypeCode result;

                if (chain.IsArrayItem)
                {
                    // we are in the itemuse.Array subchain //

                    // 3: a_[x]_[x]
                    chain.Lengthen(); // for []
                    result = chain.EmitGetItem(itemuse.Array, itemuse.Index, itemGetterKind);
                    return result;
                }

                // 1,2,4,5,6,7
                if (chain.IsMember)
                {
                    // we are in the field chain //

                    // 4, 5
                    if (node.IsMemberOf != null)
                    {
                        // we are in the middle of the field chain //

                        // 5: ...->a[]->...

                        // Lengthen chain for isMemberOf
                        chain.Lengthen(); // for hop over ->

                        node.IsMemberOf.Emit(codeGenerator);

                        // Lengthen chain for own []
                        chain.Lengthen();

                        chain.IsArrayItem = true;
                        chain.IsLastMember = false;

                        result = chain.EmitGetItem(itemuse.Array, itemuse.Index, itemGetterKind);

                        chain.IsArrayItem = false;
                        return result;
                    }
                    else
                    {
                        // we are at the beginning of the field chain //

                        // 4: a[x]->...
                        // Lengthen chain for itself
                        chain.Lengthen(); // for own []
                        chain.IsArrayItem = true;
                        chain.IsLastMember = true;

                        result = chain.EmitGetItem(itemuse.Array, itemuse.Index, itemGetterKind);

                        chain.IsArrayItem = false;
                        return result;
                    }
                }

                // 1, 2, 6, 7
                if (node.IsMemberOf != null)
                {
                    // last node of the field chain //

                    // 6 , 7: ...->a[]_[]_ , ...->a_[]_
                    bool quiet_read = chain.QuietRead;
                    chain.Create();
                    chain.Begin();
                    chain.QuietRead = quiet_read;
                    chain.Lengthen(); // for hop over ->

                    node.IsMemberOf.Emit(codeGenerator);

                    // let's emit the itemuse.Array subchain followed by the GetItem:
                    chain.IsArrayItem = true;
                    chain.IsLastMember = false;
                    result = chain.EmitGetItem(itemuse.Array, itemuse.Index, itemGetterKind);
                    chain.IsArrayItem = false;
                    chain.End();
                    return result;
                }

                // 1, 2
                if (itemuse.Array is ItemUse || itemuse.Array is DirectStFldUse || itemuse.Array is IndirectStFldUse)
                {
                    // we are at the beginning of the field chain //

                    // 2: a[]_[]_
                    bool quiet_read = chain.QuietRead;
                    chain.Create();
                    chain.Begin();
                    chain.QuietRead = quiet_read;
                    chain.IsArrayItem = true;
                    chain.IsLastMember = true;

                    result = chain.EmitGetItem(itemuse.Array, itemuse.Index, itemGetterKind);

                    chain.IsArrayItem = false;
                    chain.End();
                    return result;
                }

                // no chains //

                // 1: a_[x]_
                chain.IsArrayItem = true;
                chain.IsLastMember = true;
                result = chain.EmitGetItem(itemuse.Array, itemuse.Index, itemGetterKind);
                chain.IsArrayItem = false;
                return result;
            }

            /// <summary>
            /// Emits code to load a reference to a variable onto an evaluation stack.  Supports operators chaining.
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="codeGenerator"></param>
            private PhpTypeCode EmitNodeReadRef(ItemUse/*!*/node, CodeGenerator codeGenerator)
            {
                ChainBuilder chain = codeGenerator.ChainBuilder;
                LocalBuilder local = codeGenerator.IL.DeclareLocal(typeof(object));

                // Case 3: a_[x]_[x] never reached
                Debug.Assert(chain.IsArrayItem == false, "ReadRef access shouldn't be set to node.Array subchain nodes");

                // Case 4,5 never reached
                // 4: a[x]->...
                // 5: ...->a[]->...
                Debug.Assert(chain.IsMember == false);

                // 1, 2, 6, 7
                if (node.IsMemberOf != null)
                {
                    // last node of the field chain //

                    // 6 , 7: ...->a[]_[]_ , ...->a_[]_
                    chain.Create();
                    chain.Begin();
                    if (node.IsMemberOf is FunctionCall)
                        chain.LoadAddressOfFunctionReturnValue = true;

                    chain.SetObjectForLazyEmit(node);

                    // let's emit the node.Array subchain followed by the GetArrayItemRef:
                    chain.IsArrayItem = true;
                    chain.IsLastMember = false;
                    chain.Lengthen(); // for own []
                    chain.EmitGetArrayItemRef(node.Array, node.Index);
                    chain.IsArrayItem = false;
                    chain.EndRef();
                    return PhpTypeCode.PhpReference;
                }

                // 1, 2
                if (node.Array is ItemUse || node.Array is DirectStFldUse || node.Array is IndirectStFldUse)
                {
                    // we are at the beginning of the field chain //

                    // 2: a[]_[]_
                    chain.Create();
                    chain.Begin();
                    chain.IsArrayItem = true;
                    chain.IsLastMember = true;
                    chain.Lengthen();
                    chain.EmitGetArrayItemRef(node.Array, node.Index);
                    chain.IsArrayItem = false;
                    chain.EndRef();
                    return PhpTypeCode.PhpReference;
                }

                // no chains //

                // 1: a_[x]_
                return chain.EmitGetItemRef((SimpleVarUse)node.Array, node.Index);
            }

            /// <summary>
            /// Emits code to load <see cref="PhpRuntimeChain"/> onto an evaluation stack. Supports operators chaining.
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="codeGenerator"></param>
            private PhpTypeCode EmitNodeReadUnknown(ItemUse/*!*/node, CodeGenerator codeGenerator)
            {
                ChainBuilder chain = codeGenerator.ChainBuilder;
                PhpTypeCode result = PhpTypeCode.PhpRuntimeChain;

                if (chain.IsArrayItem)
                {
                    // 3: a_[x]_[x]
                    chain.Lengthen(); // for []
                    chain.EmitRTChainAddItem(node);
                    return result;
                }
                // 1,2,4,5,6,7
                if (chain.IsMember)
                {
                    // 4, 5
                    if (node.IsMemberOf != null)
                    {
                        // 5: ...->a[]->...

                        // Lengthen chain for isMemberOf
                        chain.Lengthen(); // for hop over ->
                        PhpTypeCode res = node.IsMemberOf.Emit(codeGenerator);
                        if (res != PhpTypeCode.PhpRuntimeChain)
                        {
                            codeGenerator.EmitBoxing(res);
                            chain.EmitCreateRTChain();
                        }
                        // Lengthen chain for own []
                        chain.Lengthen();
                        chain.IsArrayItem = true;
                        chain.IsLastMember = false;
                        chain.EmitRTChainAddItem(node);
                        chain.IsArrayItem = false;
                        return result;
                    }
                    // 4: a[x]->...
                    // Lengthen chain for itself
                    chain.Lengthen(); // for own []
                    chain.IsArrayItem = true;
                    chain.IsLastMember = true;
                    chain.EmitRTChainAddItem(node);
                    chain.IsArrayItem = false;
                    return result;
                }
                // 1, 2, 6, 7
                if (node.IsMemberOf != null)
                {
                    // 6 , 7: ...->a[]_[]_ , ...->a_[]_
                    bool quiet_read = chain.QuietRead;
                    chain.Create();
                    chain.Begin();
                    chain.QuietRead = quiet_read;
                    chain.Lengthen(); // for hop over ->
                    PhpTypeCode res = node.IsMemberOf.Emit(codeGenerator);
                    if (res != PhpTypeCode.PhpRuntimeChain)
                    {
                        codeGenerator.EmitBoxing(res);
                        chain.EmitCreateRTChain();
                    }
                    chain.IsArrayItem = true;
                    chain.IsLastMember = false;
                    chain.EmitRTChainAddItem(node);
                    chain.IsArrayItem = false;
                    chain.End();
                    return result;
                }
                // 1, 2
                if (node.Array is ItemUse || node.Array is DirectStFldUse || node.Array is IndirectStFldUse /* ??? */)
                {
                    // 2: a[]_[]_
                    bool quiet_read = chain.QuietRead;
                    chain.Create();
                    chain.Begin();
                    chain.QuietRead = quiet_read;
                    chain.IsArrayItem = true;
                    chain.IsLastMember = true;
                    chain.EmitRTChainAddItem(node);
                    chain.IsArrayItem = false;
                    chain.End();
                    return result;
                }
                // 1: a_[x]_
                chain.IsArrayItem = true;
                chain.IsLastMember = true;
                chain.EmitRTChainAddItem(node);
                chain.IsArrayItem = false;
                return result;
            }

            /// <summary>
            /// Emits code to prepare an evaluation stack for storing a value into a variable.
            /// Supports operators chaining. Store is finished by calling <see cref="EmitAssign"/>.
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="codeGenerator"></param>
            private PhpTypeCode EmitNodeWrite(ItemUse/*!*/node, CodeGenerator codeGenerator)
            {
                ChainBuilder chain = codeGenerator.ChainBuilder;

                if (chain.IsArrayItem)
                {
                    // 3: a_[x]_[v]
                    Debug.Assert(node.IsMemberOf == null);
                    return chain.EmitEnsureItem(node.Array, node.Index, true);
                }

                // 1, 2, 4, 5, 6, 7
                if (chain.IsMember)
                {
                    // 4, 5
                    if (node.IsMemberOf != null)
                    {
                        // 5: ...->a[]->...
                        // Store isMemberOf for lazy emit
                        chain.SetObjectForLazyEmit(node);
                        chain.IsArrayItem = true;
                        chain.IsLastMember = false;
                    }
                    else
                    {
                        // 4: a_[x]_->c->..., a[x]_[x]_->c->...
                        chain.IsArrayItem = true;
                        chain.IsLastMember = true;
                    }

                    PhpTypeCode result = chain.EmitEnsureItem(node.Array, node.Index, false);
                    chain.IsArrayItem = false;
                    return result;
                }

                // 1, 2, 6, 7
                if (node.IsMemberOf != null)
                {
                    // 6, 7: ...->a[x]_[x]_
                    chain.Create();
                    chain.Begin();
                    // Store isMemberOf for lazy emit
                    chain.SetObjectForLazyEmit(node);
                    chain.IsArrayItem = true;
                    chain.IsLastMember = false;
                    chain.Lengthen(); // for own []
                    node.Array.Emit(codeGenerator);
                    indexTypeCode = codeGenerator.EmitArrayKey(chain, node.Index);

                    // Note that EmitAssign will finish the work (SetArrayItem or PhpArray.Add)
                    return PhpTypeCode.Unknown;
                }
                // 1, 2
                Debug.Assert(node.IsMemberOf == null);

                if (node.Array is ItemUse || node.Array is DirectStFldUse || node.Array is IndirectStFldUse /* ??? */)
                {
                    // 2: a[]_[]_
                    chain.Create();
                    chain.Begin();
                    chain.IsArrayItem = true;
                    chain.IsLastMember = true;
                    node.Array.Emit(codeGenerator);
                    indexTypeCode = codeGenerator.EmitArrayKey(chain, node.Index);


                    // Note that further work will be done by EmitAssign (SetArrayItem or PhpArray.Add)
                    return PhpTypeCode.Unknown;
                }

                // 1: a_[x]_
                // Do nothing now, let the work be done in EmitAssign()
                return PhpTypeCode.Unknown;
            }

            /// <summary>
            /// Emits code to prepare an evaluation stack for storing a reference into a variable.
            /// Supports operators chaining. Store is finished by calling <see cref="EmitAssign"/>.
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="codeGenerator"></param>
            private PhpTypeCode EmitNodeWriteRef(ItemUse/*!*/node, CodeGenerator codeGenerator)
            {
                ChainBuilder chain = codeGenerator.ChainBuilder;

                // Case 3: a_[x]_[x] never reached
                Debug.Assert(chain.IsArrayItem == false);

                // Case 4,5 never reached
                // 4: a[x]->...
                // 5: ...->a[]->...
                Debug.Assert(chain.IsMember == false);

                // 1, 2, 6, 7
                if (node.IsMemberOf != null)
                {
                    // 6, 7: ...->a[x]_[x]_
                    chain.Create();
                    chain.Begin();
                    // Store isMemberOf for lazy emit
                    chain.SetObjectForLazyEmit(node);
                    chain.IsArrayItem = true;
                    chain.IsLastMember = false;
                    chain.Lengthen(); // for own []
                    node.Array.Emit(codeGenerator);
                    indexTypeCode = codeGenerator.EmitArrayKey(chain, node.Index);

                    // Note that EmitAssign will finish the work (SetArrayItem or PhpArray.Add)
                }
                else
                {
                    // 1, 2
                    Debug.Assert(node.IsMemberOf == null);

                    if (node.Array is ItemUse || node.Array is DirectStFldUse || node.Array is IndirectStFldUse /* ??? */)
                    {
                        // 2: a[]_[]_
                        chain.Create();
                        chain.Begin();
                        chain.IsArrayItem = true;
                        chain.IsLastMember = true;
                        node.Array.Emit(codeGenerator);
                        indexTypeCode = codeGenerator.EmitArrayKey(chain, node.Index);

                        // Note that further work will be done by EmitAssign (SetArrayItem or PhpArray.Add)
                    }
                    // 1: a_[x]_
                    // Do nothing now, let the work be done in EmitAssign()
                    // Note further work will be done by EmitAssign (either SetItem or SetItemRef);	
                }
                return PhpTypeCode.Unknown;
            }
        }

        #endregion

        #region StringLiteralDereferenceEx

        [NodeCompiler(typeof(StringLiteralDereferenceEx))]
        sealed class StringLiteralDereferenceExCompiler : ExpressionCompiler<StringLiteralDereferenceEx>
        {
            public override Evaluation Analyze(StringLiteralDereferenceEx node, Analyzer analyzer, ExInfoFromParent info)
            {
                access = info.Access;

                node.StringExpr = node.StringExpr.Analyze(analyzer, info).Literalize();
                node.KeyExpr = node.KeyExpr.Analyze(analyzer, info).Literalize();

                IntLiteral @int = node.KeyExpr as IntLiteral;
                if (@int != null)
                {
                    int key = (int)@int.Value;
                    if (key >= 0)
                    {
                        StringLiteral str;
                        BinaryStringLiteral bstr;

                        if ((str = node.StringExpr as StringLiteral) != null)
                        {
                            string strValue = (string)str.Value;
                            if (key < strValue.Length)
                                return new Evaluation(node, strValue[key].ToString());
                            else
                            { }// report invalid index
                        }
                        else if ((bstr = node.StringExpr as BinaryStringLiteral) != null)
                        {
                            var bytesValue = (PhpBytes)bstr.GetValue();
                            if (key < bytesValue.Length)
                                return new Evaluation(node, new PhpBytes(new byte[] { bytesValue[key] }));
                            else
                            { }// report invalid index
                        }
                    }
                    else
                    {
                        // report invalid index
                    }
                }

                return new Evaluation(node);
            }

            public override PhpTypeCode Emit(StringLiteralDereferenceEx node, CodeGenerator codeGenerator)
            {
                codeGenerator.ChainBuilder.Create();
                var typeCode = codeGenerator.ChainBuilder.EmitGetItem(node.StringExpr, node.KeyExpr, Operators.GetItemKinds.Get);
                codeGenerator.ChainBuilder.End();

                return typeCode;
            }
        }

        #endregion
    }
}
