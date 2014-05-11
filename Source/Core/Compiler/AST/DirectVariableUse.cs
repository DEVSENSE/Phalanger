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
using System.Reflection.Emit;

using PHP.Core.AST;
using PHP.Core.Emit;
using PHP.Core.Parsers;
using PHP.Core.Reflection;

namespace PHP.Core.Compiler.AST
{
    partial class NodeCompilers
    {
        [NodeCompiler(typeof(DirectVarUse))]
        sealed class DirectVarUseCompiler : SimpleVarUseCompiler<DirectVarUse>
        {
            #region Analysis
            
            public override Evaluation Analyze(DirectVarUse node, Analyzer analyzer, ExInfoFromParent info)
            {
                bool var_shall_be_ref;
                access = info.Access;

                base.Analyze(node, analyzer, info);

                var_shall_be_ref = !(access == AccessType.Read || access == AccessType.Write || access == AccessType.ReadAndWrite ||
                    access == AccessType.None);

                //If this VarUse is in GlobalCode, value of var_shall_be_ref does not matter.
                //All global vars are refs.
                
                //$this has special meaning, but $a->this has NOT
                if (node.VarName.IsThisVariableName && node.IsMemberOf == null)
                {
                    // report misuse of $this if we are sure about it, that is in a static routine:
                    if (analyzer.CurrentRoutine != null && analyzer.CurrentRoutine.IsStatic)
                    {
                        analyzer.ErrorSink.Add(Warnings.ThisOutOfMethod, analyzer.SourceUnit, node.Position);
                    }

                    if (!(info.Parent is VarLikeConstructUse) // $this->a = 1; is ok, but $this has write AT
                        && !(access == AccessType.None
                                    || access == AccessType.Read
                                    || access == AccessType.ReadRef
                                    || access == AccessType.ReadUnknown))
                    {
                        // False alarms
                        // analyzer.ErrorSink.Add(Warnings.ThisInWriteContext, analyzer.SourceUnit, node.Position);
                    }
                }

                if (node.IsMemberOf == null)
                {
                    if (analyzer.CurrentVarTable == null)
                    {
                        Debug.Assert(analyzer.SourceUnit.CompilationUnit.IsPure);

                        // variables used in global context when we do not have global variable table:
                        analyzer.ErrorSink.Add(Errors.GlobalCodeInPureUnit, analyzer.SourceUnit, node.Position);
                    }
                    else
                        analyzer.CurrentVarTable.Set(node.VarName, var_shall_be_ref);
                }

                return new Evaluation(node);
            }

            #endregion

            #region Code emitting

            public override PhpTypeCode Emit(DirectVarUse node, CodeGenerator codeGenerator)
            {
                Statistics.AST.AddNode("VariableUse.Direct");
                PhpTypeCode result = PhpTypeCode.Invalid;

                switch (codeGenerator.SelectAccess(access))
                {
                    // This case occurs everytime we want to get current variable value
                    // All we do is push the value onto the IL stack
                    case AccessType.Read: // Push value onto a IL stack
                        result = EmitNodeRead(node, codeGenerator);
                        break;

                    // This case occurs when the varible is written ($a = $b, then $a has Write mark)
                    // We only prepare the stack for storing, the work will be done later,
                    // by EmitAssign()
                    case AccessType.Write:
                        result = EmitNodeWrite(node, codeGenerator);
                        break;

                    case AccessType.None:
                        result = EmitNodeRead(node, codeGenerator);
                        codeGenerator.IL.Emit(OpCodes.Pop);
                        break;

                    case AccessType.ReadRef:
                        // if the selector is set to the ReadRef, the chain is emitted as if it was written
                        // (chained nodes are marked as ReadAndWrite):
                        if (codeGenerator.AccessSelector == AccessType.ReadRef)
                            codeGenerator.AccessSelector = AccessType.Write;

                        result = EmitNodeReadRef(node, codeGenerator);
                        Debug.Assert(result == PhpTypeCode.PhpReference);
                        break;

                    case AccessType.ReadUnknown:
                        result = EmitNodeReadUnknown(node, codeGenerator);
                        break;

                    case AccessType.WriteRef:
                        result = EmitNodeWriteRef(node, codeGenerator);
                        break;
                }
                return result;
            }

            internal override PhpTypeCode EmitAssign(DirectVarUse node, CodeGenerator codeGenerator)
            {
                PhpTypeCode result;

                switch (access)
                {
                    case AccessType.None:
                        result = PhpTypeCode.Void;
                        break;

                    case AccessType.Read:
                        result = PhpTypeCode.Object;
                        break;

                    case AccessType.ReadRef:
                        result = PhpTypeCode.PhpReference;
                        break;

                    case AccessType.Write:
                    case AccessType.WriteAndReadRef:
                    case AccessType.WriteAndReadUnknown:
                    case AccessType.ReadAndWrite:
                    case AccessType.ReadAndWriteAndReadRef:
                    case AccessType.ReadAndWriteAndReadUnknown:
                        result = EmitNodeWriteAssign(node, codeGenerator);
                        Debug.Assert(result == PhpTypeCode.Void);
                        break;

                    case AccessType.WriteRef:
                        EmitNodeWriteRefAssign(node, codeGenerator);
                        result = PhpTypeCode.PhpReference;
                        break;

                    default:
                        result = PhpTypeCode.Invalid;
                        Debug.Fail(null);
                        break;
                }

                return result;
            }

            internal override PhpTypeCode EmitReadFieldOfThisInInstanceMethod(DirectVarUse node, CodeGenerator codeGenerator, bool wantRef)
            {
                PhpType type = codeGenerator.LocationStack.PeekMethodDecl().Type;
                DProperty property;

                if (type.GetProperty((node).VarName, type, out property) == GetMemberResult.OK && !property.IsStatic)
                {
                    // ask the DProperty to emit code that reads the field
                    return property.EmitGet(codeGenerator, IndexedPlace.ThisArg, wantRef, null, false);
                }
                else
                {
                    return base.EmitReadFieldOfThisInInstanceMethod(node, codeGenerator, wantRef);
                }
            }

            internal override AssignmentCallback EmitWriteFieldOfThisInInstanceMethod(DirectVarUse node, CodeGenerator/*!*/ codeGenerator, bool writeRef)
            {
                PhpType type = codeGenerator.LocationStack.PeekMethodDecl().Type;
                DProperty property;

                if (type.GetProperty(node.VarName, type, out property) == GetMemberResult.OK && !property.IsStatic)
                {
                    // ask the DProperty to emit code that writes the field
                    return property.EmitSet(codeGenerator, IndexedPlace.ThisArg, writeRef, null, false);
                }
                else
                {
                    return base.EmitWriteFieldOfThisInInstanceMethod(node, codeGenerator, writeRef);
                }
            }

            internal override void EmitUnset(DirectVarUse node, CodeGenerator codeGenerator)
            {
                //Template: "unset(x)"     x = null
                Debug.Assert(access == AccessType.Read);
                // Cases 1, 4, 5, 6, 9 never reached
                Debug.Assert(codeGenerator.ChainBuilder.IsMember == false);
                // Case 3 never reached
                Debug.Assert(codeGenerator.ChainBuilder.IsArrayItem == false);

                codeGenerator.ChainBuilder.QuietRead = true;

                // 2, 7, 8 
                if (node.IsMemberOf != null)
                {
                    // 2: $b->a
                    // 8: b[]->a
                    codeGenerator.ChainBuilder.Create();
                    codeGenerator.ChainBuilder.Begin();
                    codeGenerator.ChainBuilder.QuietRead = true;
                    EmitUnsetField(node, codeGenerator);
                    codeGenerator.ChainBuilder.End();
                    return;
                }

                // 7: $a
                // Check if the variable is auto-global
                ILEmitter il = codeGenerator.IL;
                if (codeGenerator.VariableIsAutoGlobal(node.VarName))
                {
                    codeGenerator.EmitAutoGlobalStorePrepare(node.VarName);
                    il.Emit(OpCodes.Ldnull);
                    codeGenerator.EmitAutoGlobalStoreAssign();
                    return;
                }

                // Unset this variable
                if (codeGenerator.OptimizedLocals)
                {
                    // Template:
                    //		unset(x) x = null 
                    //		unset(p) p.value = null <- this case isn't valid. When p is reference just create a new PhpReference
                    VariablesTable.Entry entry = codeGenerator.CurrentVariablesTable[node.VarName];
                    if (entry.IsPhpReference)
                    {
                        il.Emit(OpCodes.Newobj, Constructors.PhpReference_Void);
                        entry.Variable.EmitStore(il);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldnull);
                        entry.Variable.EmitStore(il);
                    }
                }
                else
                {
                    // CALL Operators.UnsetVariable(<script context>, <local variable table>, <variable name>);
                    codeGenerator.EmitLoadScriptContext();
                    codeGenerator.EmitLoadRTVariablesTable();
                    EmitName(node, codeGenerator);
                    il.Emit(OpCodes.Call, Methods.Operators.UnsetVariable);
                }
            }

            /// <summary>
            /// Emits IL instructions that unset an instance field.
            /// </summary>
            /// <remarks>
            /// Nothing is expected on the evaluation stack. Nothing is left on the evaluation stack.
            /// </remarks>
            private void EmitUnsetField(DirectVarUse node, CodeGenerator/*!*/ codeGenerator)
            {
                ILEmitter il = codeGenerator.IL;

                DirectVarUse direct_instance = node.IsMemberOf as DirectVarUse;
                if (direct_instance != null && direct_instance.VarName.IsThisVariableName)
                {
                    // $this->a
                    switch (codeGenerator.LocationStack.LocationType)
                    {
                        case LocationTypes.GlobalCode:
                            {
                                // load $this from one of Main's arguments and check for null
                                Label this_non_null = il.DefineLabel();

                                codeGenerator.EmitLoadSelf();
                                il.Emit(OpCodes.Brtrue_S, this_non_null);
                                codeGenerator.EmitPhpException(Methods.PhpException.ThisUsedOutOfObjectContext);
                                il.Emit(OpCodes.Br, codeGenerator.ChainBuilder.ErrorLabel);
                                il.MarkLabel(this_non_null, true);

                                // call UnsetProperty
                                codeGenerator.EmitLoadSelf();
                                il.Emit(OpCodes.Ldstr, node.VarName.ToString()); // TODO
                                codeGenerator.EmitLoadClassContext();

                                il.EmitCall(OpCodes.Call, Methods.Operators.UnsetProperty, null);
                                return;
                            }

                        case LocationTypes.FunctionDecl:
                            {
                                // always throws error
                                codeGenerator.EmitPhpException(Methods.PhpException.ThisUsedOutOfObjectContext);
                                il.Emit(OpCodes.Br, codeGenerator.ChainBuilder.ErrorLabel);
                                break;
                            }

                        case LocationTypes.MethodDecl:
                            {
                                CompilerLocationStack.MethodDeclContext context = codeGenerator.LocationStack.PeekMethodDecl();
                                if (context.Method.IsStatic)
                                {
                                    // always throws error
                                    codeGenerator.EmitPhpException(Methods.PhpException.ThisUsedOutOfObjectContext);
                                    il.Emit(OpCodes.Br, codeGenerator.ChainBuilder.ErrorLabel);
                                }
                                else
                                {
                                    DProperty property;
                                    if (context.Type.GetProperty(node.VarName, context.Type, out property) == GetMemberResult.OK &&
                                        !property.IsStatic)
                                    {
                                        // ask the DProperty to emit its unsetting code
                                        property.EmitUnset(codeGenerator, IndexedPlace.ThisArg, null, false);
                                    }
                                    else
                                    {
                                        // unable to resolve the field -> call UnsetProperty
                                        codeGenerator.EmitLoadSelf();
                                        il.Emit(OpCodes.Ldstr, node.VarName.ToString());
                                        codeGenerator.EmitLoadClassContext();

                                        il.EmitCall(OpCodes.Call, Methods.Operators.UnsetProperty, null);
                                    }
                                }
                            }
                            break;
                    }
                }
                else
                {
                    // call UnsetProperty
                    node.IsMemberOf.Emit(codeGenerator);
                    il.Emit(OpCodes.Ldstr, node.VarName.ToString());
                    codeGenerator.EmitLoadClassContext();

                    il.EmitCall(OpCodes.Call, Methods.Operators.UnsetProperty, null);
                }
            }

            internal override PhpTypeCode EmitIsset(DirectVarUse node, CodeGenerator codeGenerator, bool empty)
            {
                // Template: "isset(x)"     x != null        
                //				isset doesn't distinguish between the NULL and uninitialized variable
                //				a reference is dereferenced, i.e. isset tells us whether the referenced variable is set 

                Debug.Assert(access == AccessType.Read);
                // Cases 1, 4, 5, 6, 9 never reached
                Debug.Assert(codeGenerator.ChainBuilder.IsMember == false);
                // Case 3 never reached
                Debug.Assert(codeGenerator.ChainBuilder.IsArrayItem == false);

                codeGenerator.ChainBuilder.QuietRead = true;

                // 2,7,8
                if (node.IsMemberOf != null)
                {
                    // 2: $b->a
                    // 8: b[]->a
                    codeGenerator.ChainBuilder.Create();
                    codeGenerator.ChainBuilder.Begin();
                    codeGenerator.ChainBuilder.QuietRead = true;
                    EmitReadField(node, codeGenerator, false);
                    codeGenerator.ChainBuilder.End();
                    return PhpTypeCode.Object;
                }
                // 7: $a
                // Check whether this variable is set
                //codeGenerator.EmitVariableIsset(this);
                ILEmitter il = codeGenerator.IL;
                if (node.VarName.IsThisVariableName && codeGenerator.LocationStack.LocationType == LocationTypes.MethodDecl)
                {
                    CompilerLocationStack.MethodDeclContext context = codeGenerator.LocationStack.PeekMethodDecl();
                    if (!context.Method.IsStatic)
                    {
                        // $this is always set in instance methods
                        il.Emit(OpCodes.Ldarg_0);
                        return PhpTypeCode.Object;
                    }
                }
                this.EmitLoad(node, codeGenerator);
                return PhpTypeCode.Object;
            }

            private PhpTypeCode EmitNodeWrite(DirectVarUse node, CodeGenerator codeGenerator)
            {
                ChainBuilder chain = codeGenerator.ChainBuilder;

                if (chain.IsMember)
                {
                    // 1,4,5,6,9
                    if (node.IsMemberOf != null)
                    {
                        // 1:  ...->a->...
                        chain.Lengthen();
                        chain.EmitEnsureProperty(node.IsMemberOf, node, false);
                        return PhpTypeCode.DObject;
                    }
                    if (chain.IsArrayItem)
                    {
                        // 4,6
                        if (chain.IsLastMember)
                        {
                            // 4: a[][]
                            chain.EmitEnsureVariableIsArray(node);
                        }
                        else
                        {
                            // 6: $b->a[3]
                            ChainBuilder.ObjectFieldLazyEmitInfo object_info = chain.GetObjectForLazyEmit();
                            // Lengthen for hop over ->
                            chain.EmitEnsureProperty(object_info.ObjectForLazyEmit, node, true);
                            chain.ReleaseObjectForLazyEmit(object_info);
                            chain.IsArrayItem = true;
                            chain.IsLastMember = false;
                        }

                        return PhpTypeCode.PhpArray;
                    }
                    if (chain.Exists)
                    {
                        // 5: $a->b->c->...
                        chain.EmitEnsureVariableIsObject(node);
                        return PhpTypeCode.DObject;
                    }
                    else
                    {
                        // 9: $a->b
                        this.EmitLoadAddress(node, codeGenerator);
                        return PhpTypeCode.ObjectAddress;
                    }
                }
                // 2,3,7,8
                if (node.IsMemberOf != null)
                {
                    // 2: $b->a 
                    // 8: b[]->a
                    chain.Create();
                    chain.Begin();
                    assignmentCallback = EmitWriteField(node, codeGenerator, false);
                    // Note: more work is done in EmitAssign 

                    // some data are preloaded but nothing that can be consumed is loaded on stack:
                    return PhpTypeCode.Unknown;
                }
                // 3,7
                if (codeGenerator.ChainBuilder.IsArrayItem)
                {
                    // 3: a[3]
                    EmitLoadAddress(node, codeGenerator);
                    return PhpTypeCode.ObjectAddress;
                }
                else
                {
                    // 7: $a
                    EmitStorePrepare(node, codeGenerator);
                    return PhpTypeCode.Unknown;
                }
            }

            private PhpTypeCode EmitNodeWriteAssign(DirectVarUse node, CodeGenerator codeGenerator)
            {
                // Note that for cases 1,3,4,5,6,9 EmitAssign is never called!!!

                // 2,7,8
                if (codeGenerator.ChainBuilder.IsMember)
                {
                    // 2,8
                    if (codeGenerator.ChainBuilder.Exists)
                    {
                        // 8: b[]->a
                        codeGenerator.ChainBuilder.EmitSetObjectField();
                    }
                    else
                    {
                        // 2: $b->a 
                        Debug.Assert(node.IsMemberOf is SimpleVarUse || node.IsMemberOf is FunctionCall);
                        if (node.IsMemberOf is FunctionCall)
                            codeGenerator.ChainBuilder.LoadAddressOfFunctionReturnValue = true;

                        assignmentCallback(codeGenerator, PhpTypeCode.Object);

                        SimpleVarUse svu = node.IsMemberOf as SimpleVarUse;
                        if (svu != null)
                            SimpleVarUseHelper.EmitLoadAddress_StoreBack(svu, codeGenerator);
                        // else do nothing
                    }
                    codeGenerator.ChainBuilder.End();
                }
                else
                {
                    // 7: $a
                    EmitStoreAssign(node, codeGenerator);
                }

                return PhpTypeCode.Void;
            }

            /// <summary>
            /// Emits code for loading the variable's value onto the evaluation stack. Supports operators chaining.
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="codeGenerator">Code generator.</param>
            private PhpTypeCode EmitNodeRead(DirectVarUse node, CodeGenerator codeGenerator)
            {
                if (codeGenerator.ChainBuilder.IsMember)
                {
                    // 1,4,5,6,9
                    if (node.IsMemberOf != null)
                    {
                        // 1: ...->a->...
                        codeGenerator.ChainBuilder.Lengthen();
                        return EmitReadField(node, codeGenerator, false);
                    }
                    if (codeGenerator.ChainBuilder.IsArrayItem && !codeGenerator.ChainBuilder.IsLastMember)
                    {
                        // 6: $b->a[3]
                        return codeGenerator.ChainBuilder.EmitGetProperty(node);
                    }
                    // 4: a[][] 
                    // 5: $a->b->c->... 
                    // 9: $a->b
                    return this.EmitLoad(node, codeGenerator);

                }
                // 2,3,7,8
                if (node.IsMemberOf != null)
                {
                    // 2: $b->a
                    // 8: b[]->a
                    codeGenerator.ChainBuilder.Create();
                    codeGenerator.ChainBuilder.Begin();
                    PhpTypeCode result = EmitReadField(node, codeGenerator, false);
                    codeGenerator.ChainBuilder.End();
                    return result;
                }
                // 3: a[3]
                // 7: $a
                return this.EmitLoad(node, codeGenerator);
            }

            /// <summary>
            /// Emits code for loading the variable's value as a <see cref="PhpReference"/>. This function is called only
            /// by first AST node in chain.
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="codeGenerator"></param>
            private PhpTypeCode EmitNodeReadRef(DirectVarUse node, CodeGenerator codeGenerator)
            {
                // Cases 1, 4, 5, 6, 9 never reached
                Debug.Assert(codeGenerator.ChainBuilder.IsMember == false);

                // Case 3 never reached
                Debug.Assert(codeGenerator.ChainBuilder.IsArrayItem == false);

                // 2, 7, 8
                if (node.IsMemberOf != null)
                {
                    // 2: $b->a
                    // 8: b[]->a
                    codeGenerator.ChainBuilder.Create();
                    codeGenerator.ChainBuilder.Begin();
                    if (node.IsMemberOf is FunctionCall)
                        codeGenerator.ChainBuilder.LoadAddressOfFunctionReturnValue = true;

                    PhpTypeCode type_code = EmitReadField(node, codeGenerator, true);
                    Debug.Assert(type_code == PhpTypeCode.PhpReference);

                    codeGenerator.ChainBuilder.EndRef();
                    return PhpTypeCode.PhpReference;
                }

                // 7: $a
                EmitLoadRef(node, codeGenerator);
                return PhpTypeCode.PhpReference;
            }

            /// <summary>
            /// Emits code for loading the variable's value as either <see cref="PhpReference"/> or
            /// <see cref="PhpRuntimeChain"/>.
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="codeGenerator"></param>
            private PhpTypeCode EmitNodeReadUnknown(DirectVarUse node, CodeGenerator codeGenerator)
            {
                if (codeGenerator.ChainBuilder.IsMember)
                {
                    // 1,4,5,6,9
                    if (node.IsMemberOf != null)
                    {
                        // 1: ...->a->...
                        codeGenerator.ChainBuilder.Lengthen();
                        PhpTypeCode res = node.IsMemberOf.Emit(codeGenerator);
                        if (res != PhpTypeCode.PhpRuntimeChain)
                        {
                            codeGenerator.EmitBoxing(res);
                            codeGenerator.ChainBuilder.EmitCreateRTChain();
                        }
                        codeGenerator.ChainBuilder.EmitRTChainAddField(node);
                        return PhpTypeCode.PhpRuntimeChain;
                    }
                    if (codeGenerator.ChainBuilder.IsArrayItem && !codeGenerator.ChainBuilder.IsLastMember)
                    {
                        // 6: $b->a[3]
                        codeGenerator.ChainBuilder.EmitRTChainAddField(node);
                        return PhpTypeCode.PhpRuntimeChain;
                    }
                    // 4: a[][] 
                    // 5: $a->b->c->... 
                    // 9: $a->b
                    this.EmitLoadRef(node, codeGenerator);
                    codeGenerator.ChainBuilder.EmitCreateRTChain();
                    return PhpTypeCode.PhpRuntimeChain;
                }
                // 2,3,7,8
                if (node.IsMemberOf != null)
                {
                    // 2: $b->a
                    // 8: b[]->a
                    codeGenerator.ChainBuilder.Create();
                    codeGenerator.ChainBuilder.Begin();
                    PhpTypeCode res = node.IsMemberOf.Emit(codeGenerator);
                    if (res != PhpTypeCode.PhpRuntimeChain)
                    {
                        codeGenerator.EmitBoxing(res);
                        codeGenerator.ChainBuilder.EmitCreateRTChain();
                    }
                    codeGenerator.ChainBuilder.EmitRTChainAddField(node);
                    codeGenerator.ChainBuilder.End();
                    return PhpTypeCode.PhpRuntimeChain;
                }
                // 3: a[3]
                // 7: $a
                this.EmitLoadRef(node, codeGenerator);
                return PhpTypeCode.PhpReference;
            }

            private PhpTypeCode EmitNodeWriteRef(DirectVarUse node, CodeGenerator codeGenerator)
            {
                // Cases 1, 4, 5, 6, 9 never reached
                Debug.Assert(codeGenerator.ChainBuilder.IsMember == false);

                // Case 3 never reached
                Debug.Assert(codeGenerator.ChainBuilder.IsArrayItem == false);

                // 2,7,8
                if (node.IsMemberOf != null)
                {
                    // 2: $b->a 
                    // 8: b[]->a
                    codeGenerator.ChainBuilder.Create();
                    codeGenerator.ChainBuilder.Begin();
                    assignmentCallback = EmitWriteField(node, codeGenerator, true);
                    // Note: more work is done in EmitAssign 
                }
                else
                {
                    // 7: $a
                    EmitStoreRefPrepare(node, codeGenerator);
                }
                return PhpTypeCode.Unknown;
            }

            private void EmitNodeWriteRefAssign(DirectVarUse node, CodeGenerator codeGenerator)
            {
                // Note that for cases 1,3,4,5,6,9 EmitAssign is never called!!!

                // 2,7,8
                if (codeGenerator.ChainBuilder.IsMember)
                {
                    // 2,8
                    if (codeGenerator.ChainBuilder.Exists)
                    {
                        // 8: b[]->a
                        codeGenerator.ChainBuilder.EmitSetObjectField();
                    }
                    else
                    {
                        // 2: $b->a
                        Debug.Assert(node.IsMemberOf is SimpleVarUse || node.IsMemberOf is FunctionCall);
                        if (node.IsMemberOf is FunctionCall)
                            codeGenerator.ChainBuilder.LoadAddressOfFunctionReturnValue = true;

                        assignmentCallback(codeGenerator, PhpTypeCode.Object);

                        SimpleVarUse svu = node.IsMemberOf as SimpleVarUse;
                        if (svu != null)
                            SimpleVarUseHelper.EmitLoadAddress_StoreBack(svu, codeGenerator);
                    }
                    codeGenerator.ChainBuilder.End();
                }
                else
                {
                    // 7: $a
                    //codeGenerator.EmitVariableStoreRefAssign(this);
                    EmitStoreRefAssign(node, codeGenerator);
                }
            }

            /// <summary>
            /// Emits IL instructions that load the name of the variable onto evaluation stack.
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="codeGenerator"></param>
            /// <remarks>
            /// Nothing is expected on the evaluation stack. The <B>string</B> representing the 
            /// name of the variable is left on the evaluation stack.
            /// </remarks>
            internal override void EmitName(DirectVarUse node, CodeGenerator codeGenerator)
            {
                codeGenerator.IL.Emit(OpCodes.Ldstr, node.VarName.Value);
            }

            /// <summary>
            /// Emits IL instructions that load the variable onto the evaluation stack.
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="codeGenerator"></param>
            /// <remarks>Also handles loading of <B>$this</B>.</remarks>
            internal override PhpTypeCode EmitLoad(DirectVarUse node, CodeGenerator codeGenerator)
            {
                if (node.VarName.IsThisVariableName)
                {
                    return EmitLoadThis(node, codeGenerator);
                }

                return EmitLoad(codeGenerator, node.VarName);
            }

            /// <summary>
            /// Emit load of variable named <paramref name="varName"/>.
            /// </summary>
            internal static PhpTypeCode EmitLoad(CodeGenerator codeGenerator, VariableName varName)
            {
                ILEmitter il = codeGenerator.IL;

                // Check if the variable is auto-global
                if (codeGenerator.VariableIsAutoGlobal(varName))
                {
                    codeGenerator.EmitAutoGlobalLoad(varName);
                    return PhpTypeCode.Object;
                }

                // Variable is local
                if (codeGenerator.OptimizedLocals)
                {
                    // Template:
                    //			ldloc loc
                    //	***** // If the specidied variable is of type PhpReference
                    //				ldfld PhpReference.value
                    //	*****
                    VariablesTable.Entry entry = codeGenerator.CurrentVariablesTable[varName];
                    entry.Variable.EmitLoad(il);
                    if (entry.IsPhpReference)
                        il.Emit(OpCodes.Ldfld, Fields.PhpReference_Value);

                    return PhpTypeCode.Object;
                }

                // LOAD Operators.GetVariable[Unchecked](<script context>, <local variable table>, <name>);
                codeGenerator.EmitLoadScriptContext();
                codeGenerator.EmitLoadRTVariablesTable();
                il.Emit(OpCodes.Ldstr, varName.Value);

                if (codeGenerator.ChainBuilder.QuietRead)
                    il.Emit(OpCodes.Call, Methods.Operators.GetVariableUnchecked);
                else
                    il.Emit(OpCodes.Call, Methods.Operators.GetVariable);

                return PhpTypeCode.Object;
            }

            /// <summary>
            /// Emits IL instructions that load the "$this" variable onto the evaluation stack.
            /// </summary>
            private PhpTypeCode EmitLoadThis(DirectVarUse node, CodeGenerator codeGenerator)
            {
                ILEmitter il = codeGenerator.IL;
                CompilerLocationStack locationStack = codeGenerator.LocationStack;

                // special treatment of $this
                switch (locationStack.LocationType)
                {
                    case LocationTypes.GlobalCode:
                        {
                            // load $this from one of Main's arguments and check for null
                            Label this_non_null = il.DefineLabel();

                            codeGenerator.EmitLoadSelf();
                            il.Emit(OpCodes.Dup);
                            il.Emit(OpCodes.Brtrue_S, this_non_null);
                            il.Emit(OpCodes.Ldstr, VariableName.ThisVariableName.Value);
                            codeGenerator.EmitPhpException(Methods.PhpException.UndefinedVariable);
                            il.MarkLabel(this_non_null, true);

                            return PhpTypeCode.Object;
                        }

                    case LocationTypes.FunctionDecl:
                        {
                            // always null
                            il.Emit(OpCodes.Ldstr, VariableName.ThisVariableName.Value);
                            codeGenerator.EmitPhpException(Methods.PhpException.UndefinedVariable);
                            il.Emit(OpCodes.Ldnull);

                            return PhpTypeCode.Object;
                        }

                    case LocationTypes.MethodDecl:
                        {
                            CompilerLocationStack.MethodDeclContext context = locationStack.PeekMethodDecl();
                            if (context.Method.IsStatic)
                            {
                                // always null in static methods
                                il.Emit(OpCodes.Ldstr, VariableName.ThisVariableName.Value);
                                codeGenerator.EmitPhpException(Methods.PhpException.UndefinedVariable);
                                il.Emit(OpCodes.Ldnull);

                                return PhpTypeCode.Object;
                            }
                            else
                            {
                                // arg0 or <proxy> in instance methods
                                codeGenerator.EmitLoadSelf();
                                return PhpTypeCode.DObject;
                            }
                        }

                    default:
                        Debug.Fail("Invalid location type.");
                        return PhpTypeCode.Invalid;
                }
            }

            /// <summary>
            /// Loads an address of a variable on the stack.
            /// </summary>
            internal override void EmitLoadAddress(DirectVarUse node, CodeGenerator codeGenerator)
            {
                var varName = node.VarName;

                if (codeGenerator.VariableIsAutoGlobal(varName))
                {
                    codeGenerator.EmitAutoGlobalLoadAddress(varName);
                    return;
                }
                if (codeGenerator.OptimizedLocals)
                {
                    // Template: for DirectVarUse
                    //	***** // If the specidied variable is of type PhpReference
                    //				ldloc loc
                    //				ldflda PhpReference.value
                    //	***** // Otherwise
                    //				ldloca loc
                    //	*****
                    VariablesTable.Entry entry = codeGenerator.CurrentVariablesTable[varName];
                    if (entry.IsPhpReference)
                    {
                        // Load variable (of type PhpReference) from IPlace
                        entry.Variable.EmitLoad(codeGenerator.IL);
                        // ... and get address (ref) of its Value field
                        codeGenerator.IL.Emit(OpCodes.Ldflda, Fields.PhpReference_Value);
                    }
                    else
                        // Load address of variable from IPlace
                        entry.Variable.EmitLoadAddress(codeGenerator.IL);
                    return;
                }
                else
                {
                    // Template:
                    //		object Operators.GetVariableUnchecked(IDictionary table, string name) //returns variable value
                    this.LoadTabledVariableAddress(node, codeGenerator);
                }
            }

            internal override void EmitLoadAddress_StoreBack(DirectVarUse node, CodeGenerator codeGenerator)
            {
                EmitLoadAddress_StoreBack(node, codeGenerator, false);
            }

            internal override void EmitLoadAddress_StoreBack(DirectVarUse node, CodeGenerator codeGenerator, bool duplicate_value)
            {
                ILEmitter il = codeGenerator.IL;

                // Skip $this->a
                if (node.VarName.IsThisVariableName)
                    // Take no action
                    return;

                if (codeGenerator.VariableIsAutoGlobal(node.VarName))
                {
                    // Take no action
                    return;
                }
                if (codeGenerator.OptimizedLocals)
                {
                    // Take no action
                    return;
                }
                this.StoreTabledVariableBack(node, codeGenerator, duplicate_value);
            }

            internal override void EmitLoadRef(DirectVarUse node, CodeGenerator/*!*/ codeGenerator)
            {
                if (node.VarName.IsThisVariableName)
                {
                    EmitLoadThisRef(node, codeGenerator);
                    return;
                }

                EmitLoadRef(codeGenerator, node.VarName);
            }

            /// <summary>
            /// Emit reference load of variable named <paramref name="varName"/>.
            /// </summary>
            internal static void EmitLoadRef(CodeGenerator/*!*/ codeGenerator, VariableName varName)
            {
                ILEmitter il = codeGenerator.IL;

                // Check if the variable is auto-global
                if (codeGenerator.VariableIsAutoGlobal(varName))
                {
                    codeGenerator.EmitAutoGlobalLoadRef(varName);
                    return;
                }

                if (codeGenerator.OptimizedLocals)
                {
                    // Template: for DirectVarUse			
                    //		"LOAD ref $x;"
                    //
                    //		ldloc loc // Local variable should be of type PhpReference
                    VariablesTable.Entry entry = codeGenerator.CurrentVariablesTable[varName];
                    entry.Variable.EmitLoad(il);
                }
                else
                {
                    // Template:
                    //		PhpReference Operators.GetVariableRef(IDictionary table, string name) 
                    codeGenerator.EmitLoadScriptContext();
                    codeGenerator.EmitLoadRTVariablesTable();
                    il.Emit(OpCodes.Ldstr, varName.Value);
                    il.Emit(OpCodes.Call, Methods.Operators.GetVariableRef);
                }
            }

            /// <summary>
            /// Loads a PhpReference to "this" special variable to the evaluation stack.
            /// If "this" is not available, loads an empty PhpReference.
            /// </summary>
            private PhpTypeCode EmitLoadThisRef(DirectVarUse node, CodeGenerator/*!*/ codeGenerator)
            {
                ILEmitter il = codeGenerator.IL;

                switch (codeGenerator.LocationStack.LocationType)
                {
                    case LocationTypes.GlobalCode:
                        {
                            // load $this from one of Main's arguments:
                            codeGenerator.EmitLoadSelf();

                            // NOTE: If $this is used by ref somewhere in the method each access to it is boxed to the reference.
                            // Only calls to methods use the "this" pointer itself. Thus the rule "no duplicate pointers" is slightly
                            // broken here yet everything should work fine.
                            il.Emit(OpCodes.Newobj, Constructors.PhpReference_Object);
                            break;
                        }

                    case LocationTypes.FunctionDecl:
                        {
                            // always null referencing PhpReference
                            il.Emit(OpCodes.Newobj, Constructors.PhpReference_Void);
                            break;
                        }

                    case LocationTypes.MethodDecl:
                        {
                            CompilerLocationStack.MethodDeclContext context = codeGenerator.LocationStack.PeekMethodDecl();
                            if (context.Method.IsStatic)
                            {
                                // always null referencing PhpReference in static methods
                                il.Emit(OpCodes.Newobj, Constructors.PhpReference_Void);
                            }
                            else
                            {
                                // arg0 or <proxy> referencing PhpReference in instance methods
                                codeGenerator.EmitLoadSelf();

                                // NOTE: If $this is used by ref somewhere in the method each access to it is boxed to the reference.
                                // Only calls to methods use the "this" pointer itself. Thus the rule "no duplicate pointers" is slightly
                                // broken here yet everything should work fine.
                                il.Emit(OpCodes.Newobj, Constructors.PhpReference_Object);
                            }
                            break;
                        }

                    default:
                        Debug.Fail("Invalid location type.");
                        break;
                }

                // always returns a reference:
                return PhpTypeCode.PhpReference;
            }

            internal override void EmitStorePrepare(DirectVarUse node, CodeGenerator codeGenerator)
            {
                var varName = node.VarName;

                if (varName.IsThisVariableName)
                {
                    // Error throwing code will be emitted in EmitVariableStoreAssign
                }
                else if (codeGenerator.VariableIsAutoGlobal(varName))
                {
                    // Check if the variable is auto-global
                    codeGenerator.EmitAutoGlobalStorePrepare(varName);
                }
                else if (codeGenerator.OptimizedLocals)
                {
                    // Template:
                    //		"WRITE($x,value);"
                    //		**** // if specified variable is of type PhpReference
                    //		ldloc local
                    //		**** // Otherwise do nothing

                    VariablesTable.Entry entry = codeGenerator.CurrentVariablesTable[varName];
                    if (entry.IsPhpReference)
                    {
                        entry.Variable.EmitLoad(codeGenerator.IL);
                    }
                    // Otherwise do nothing
                    // Now load the value, then call EmitVariableStoreAssignOptimized() to store the value ...
                }
                else
                {
                    // Template:
                    //		void Operators.SetVariable(table, "x", PhpVariable.Copy(Operators.getValue(table, "x"), CopyReason.Assigned));		
                    codeGenerator.EmitLoadScriptContext();
                    codeGenerator.EmitLoadRTVariablesTable();
                    EmitName(node, codeGenerator);
                    // Now load the value, then call SetVariable() to store the value ...
                }
            }

            internal override void EmitStoreAssign(DirectVarUse node, CodeGenerator codeGenerator)
            {
                var varName = node.VarName;

                if (varName.IsThisVariableName)
                {
                    // emit error throwing code
                    codeGenerator.IL.Emit(OpCodes.Pop);
                    codeGenerator.EmitPhpException(Methods.PhpException.CannotReassignThis);
                }
                else if (codeGenerator.VariableIsAutoGlobal(varName))
                {
                    // Check if the variable is auto-global
                    codeGenerator.EmitAutoGlobalStoreAssign();
                }
                else if (codeGenerator.OptimizedLocals)
                {
                    // Template:
                    //		"WRITE($x,value);"
                    //		**** // if specified variable is of type PhpReference
                    //		ldloc local
                    //		**** // Otherwise do nothing

                    VariablesTable.Entry entry = codeGenerator.CurrentVariablesTable[varName];

                    if (entry.IsPhpReference)
                        codeGenerator.IL.Emit(OpCodes.Stfld, Fields.PhpReference_Value);
                    else
                        entry.Variable.EmitStore(codeGenerator.IL);
                }
                else
                {
                    // CALL Operators.SetVariable(STACK:table,STACK:name,STACK:value);
                    codeGenerator.IL.Emit(OpCodes.Call, Methods.Operators.SetVariable);
                }
            }

            internal override void EmitStoreRefPrepare(DirectVarUse node, CodeGenerator codeGenerator)
            {
                var varName = node.VarName;

                if (varName.IsThisVariableName)
                {
                    // error throwing code will be emitted in EmitVariableStoreRefAssign
                }
                else if (codeGenerator.VariableIsAutoGlobal(varName))
                {
                    // Check if the variable is auto-global
                    codeGenerator.EmitAutoGlobalStoreRefPrepare(varName);
                }
                else if (codeGenerator.OptimizedLocals)
                {
                    // Template:
                    //		WRITE ref ($x,value);

                    //		DO NOTHING !!!!
                    // now load the value then store to local variable
                }
                else
                {
                    // Template:
                    //		WRITE ref ($x,value); // by Martin
                    //
                    //		ldarg.1 
                    //		ldstr "name"   
                    //		LOAD value
                    //		call instance IDictionary.set_Item(object)

                    codeGenerator.EmitLoadScriptContext();
                    codeGenerator.EmitLoadRTVariablesTable();
                    EmitName(node, codeGenerator);
                    // now load value, then call EmitVariableStoreRefAssignGlobalContext() to emit stfld ...
                }
            }

            internal override void EmitStoreRefAssign(DirectVarUse node, CodeGenerator codeGenerator)
            {
                var varName = node.VarName;

                if (varName.IsThisVariableName)
                {
                    // emit error throwing code
                    codeGenerator.IL.Emit(OpCodes.Pop);
                    codeGenerator.EmitPhpException(Methods.PhpException.CannotReassignThis);
                }
                else if (codeGenerator.VariableIsAutoGlobal(varName))
                {
                    // Check if the variable is auto-global
                    codeGenerator.EmitAutoGlobalStoreRefAssign(varName);
                }
                else if (codeGenerator.OptimizedLocals)
                {
                    VariablesTable.Entry entry = codeGenerator.CurrentVariablesTable[varName];
                    entry.Variable.EmitStore(codeGenerator.IL);
                }
                else
                {
                    // call instance IDictionary.set_Item(object, object)
                    // OBSOLETE: il.Emit(OpCodes.Callvirt, Methods.IDictionary_SetItem);
                    codeGenerator.IL.Emit(OpCodes.Call, Methods.Operators.SetVariableRef);
                }
            }

            #endregion
        }
    }
}
