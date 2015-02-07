/*

 Copyright (c) 2006- DEVSENSE
 Copyright (c) 2004-2006 Tomas Matousek, Vaclav Novak, and Martin Maly.

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
        [NodeCompiler(typeof(IndirectVarUse))]
        sealed class IndirectVarUseCompiler : SimpleVarUseCompiler<IndirectVarUse>, IVariableSwitchEmitter
        {
            #region Analysis
            
            public override Evaluation Analyze(IndirectVarUse node, Analyzer analyzer, ExInfoFromParent info)
            {
                access = info.Access;

                base.Analyze(node, analyzer, info);

                if (node.IsMemberOf == null)
                {
                    if (!(access == AccessType.Read
                        || access == AccessType.Write
                        || access == AccessType.ReadAndWrite
                        || access == AccessType.None))
                    {
                        analyzer.CurrentVarTable.SetAllRef();
                    }
                    analyzer.AddCurrentRoutineProperty(RoutineProperties.IndirectLocalAccess);
                }

                node.varNameEx = node.VarNameEx.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();

                return new Evaluation(node);
            }

            #endregion

            #region Emission

            public override PhpTypeCode Emit(IndirectVarUse node, CodeGenerator codeGenerator)
            {
                Statistics.AST.AddNode("IndirectVarUse");
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
                        EmitNodeRead(node, codeGenerator);
                        codeGenerator.IL.Emit(OpCodes.Pop);
                        result = PhpTypeCode.Void;
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
                        EmitNodeWriteRef(node, codeGenerator);
                        result = PhpTypeCode.PhpReference;
                        break;

                    default:
                        result = PhpTypeCode.Invalid;
                        Debug.Fail(null);
                        break;
                }
                return result;
            }

            private PhpTypeCode EmitNodeRead(IndirectVarUse node, CodeGenerator codeGenerator)
            {
                if (codeGenerator.ChainBuilder.IsMember)
                {
                    // 1,4,5,6,9
                    if (node.IsMemberOf != null)
                    {
                        // 1: ...->$a->...
                        codeGenerator.ChainBuilder.Lengthen(); // for hop over ->
                        node.IsMemberOf.Emit(codeGenerator);
                        return codeGenerator.ChainBuilder.EmitGetProperty(node);
                    }

                    if (codeGenerator.ChainBuilder.IsArrayItem && !codeGenerator.ChainBuilder.IsLastMember)
                    {
                        // 6: $b->${"a"}[3]
                        return codeGenerator.ChainBuilder.EmitGetProperty(node);
                    }

                    // 4: ${"a"}[][] 
                    // 5: $$a->b->c->... 
                    // 9: $$a->b
                    return EmitLoad(node, codeGenerator);
                }

                // 2,3,7,8
                if (node.IsMemberOf != null)
                {
                    // 2: $b->$a
                    // 8: b[]->$a
                    codeGenerator.ChainBuilder.Create();
                    codeGenerator.ChainBuilder.Begin();

                    //codeGenerator.ChainBuilder.Lengthen(); // for hop over ->
                    //PhpTypeCode result = node.IsMemberOf.Emit(codeGenerator);
                    //codeGenerator.ChainBuilder.EmitGetProperty(this);
                    var result = codeGenerator.CallSitesBuilder.EmitGetProperty(
                        codeGenerator, false,
                        node.IsMemberOf, null, null,
                        null,
                        null, node.VarNameEx,
                        codeGenerator.ChainBuilder.QuietRead);

                    codeGenerator.ChainBuilder.End();
                    return result;
                }

                // 3: ${"a"}[3]
                // 7: $$a
                return EmitLoad(node, codeGenerator);
            }

            private PhpTypeCode EmitNodeReadRef(IndirectVarUse node, CodeGenerator codeGenerator)
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
                    PhpTypeCode result = EmitReadField(node, codeGenerator, true);
                    codeGenerator.ChainBuilder.EndRef();

                    Debug.Assert(result == PhpTypeCode.PhpReference);
                }
                else
                {
                    // 7: $a
                    //codeGenerator.EmitVariableLoadRef(this);
                    EmitLoadRef(node, codeGenerator);
                }

                return PhpTypeCode.PhpReference;
            }

            private PhpTypeCode EmitNodeReadUnknown(IndirectVarUse node, CodeGenerator codeGenerator)
            {
                if (codeGenerator.ChainBuilder.IsMember)
                {
                    // 1,4,5,6,9
                    if (node.IsMemberOf != null)
                    {
                        // 1: ...->$a->...
                        codeGenerator.ChainBuilder.Lengthen(); // for hop over ->
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
                        // 6: $b->${"a"}[3]
                        codeGenerator.ChainBuilder.EmitRTChainAddField(node);
                        return PhpTypeCode.PhpRuntimeChain;
                    }

                    // 4: ${"a"}[][] 
                    // 5: $$a->b->c->... 
                    // 9: $$a->b
                    this.EmitLoadRef(node, codeGenerator);
                    codeGenerator.ChainBuilder.EmitCreateRTChain();
                    return PhpTypeCode.PhpRuntimeChain;
                }
                // 2,3,7,8
                if (node.IsMemberOf != null)
                {
                    // 2: $b->$a
                    // 8: b[]->$a
                    codeGenerator.ChainBuilder.Create();
                    codeGenerator.ChainBuilder.Begin();
                    codeGenerator.ChainBuilder.Lengthen(); // for hop over ->
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

                // 3: ${"a"}[3]
                // 7: $$a
                this.EmitLoadRef(node, codeGenerator);
                return PhpTypeCode.PhpReference;
            }

            private PhpTypeCode EmitNodeWrite(IndirectVarUse node, CodeGenerator codeGenerator)
            {
                ChainBuilder chain = codeGenerator.ChainBuilder;

                if (chain.IsMember)
                {
                    // 1,4,5,6,9
                    if (node.IsMemberOf != null)
                    {
                        // 1:  ...->$a->...
                        chain.Lengthen();
                        chain.EmitEnsureProperty(node.IsMemberOf, node, false);
                        return PhpTypeCode.DObject;
                    }

                    if (chain.IsArrayItem)
                    {
                        // 4,6
                        if (chain.IsLastMember)
                        {
                            // 4: ${"a"}[][]
                            chain.EmitEnsureVariableIsArray(node);
                            return PhpTypeCode.PhpArray;
                        }
                        else
                        {
                            // 6: $b->${"a"}[3]
                            ChainBuilder.ObjectFieldLazyEmitInfo object_info = chain.GetObjectForLazyEmit();
                            // Lengthen for hop over ->
                            chain.EmitEnsureProperty(object_info.ObjectForLazyEmit, node, true);
                            chain.ReleaseObjectForLazyEmit(object_info);
                            chain.IsArrayItem = true;
                            chain.IsLastMember = false;
                            return PhpTypeCode.PhpArray;
                        }
                    }

                    if (chain.Exists)
                    {
                        // 5: $$a->b->c->...
                        chain.EmitEnsureVariableIsObject(node);
                        return PhpTypeCode.DObject;
                    }
                    else
                    {
                        // 9: $$a->b
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
                    return PhpTypeCode.Unknown;
                }

                // 3,7
                if (codeGenerator.ChainBuilder.IsArrayItem)
                {
                    // 3: ${"a"}[3]
                    this.EmitLoadAddress(node, codeGenerator);
                    return PhpTypeCode.ObjectAddress;
                }

                // 7: $a
                //codeGenerator.EmitVariableStorePrepare(this);
                this.EmitStorePrepare(node, codeGenerator);
                return PhpTypeCode.Unknown;
            }

            private void EmitNodeWriteAssign(IndirectVarUse node, CodeGenerator codeGenerator)
            {
                ChainBuilder chain = codeGenerator.ChainBuilder;

                // Note that for cases 1,3,4,5,6,9 EmitAssign is never called!!!

                // 2,7,8
                if (chain.IsMember)
                {
                    // 2,8
                    if (chain.Exists)
                    {
                        // 8: b[]->$a
                        chain.EmitSetObjectField();
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
                    chain.End();
                }
                else
                {
                    // 7: $a
                    //codeGenerator.EmitVariableStoreAssign(this);
                    this.EmitStoreAssign(node, codeGenerator);
                }
            }

            private void EmitNodeWriteRef(IndirectVarUse node, CodeGenerator codeGenerator)
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
                    return;
                }
                // 7: $a
                //codeGenerator.EmitVariableStoreRefPrepare(this);
                this.EmitStoreRefPrepare(node, codeGenerator);
            }

            private void EmitNodeWriteRefAssign(IndirectVarUse node, CodeGenerator codeGenerator)
            {
                // Note that for cases 1,3,4,5,6,9 EmitAssign is never called!!!

                // 2,7,8
                if (codeGenerator.ChainBuilder.IsMember)
                {
                    // 2,8
                    if (codeGenerator.ChainBuilder.Exists)
                    {
                        // 8: b[]->a
                        // TODO: <MARTIN> May be this call will change to SetObjectFieldRef
                        codeGenerator.ChainBuilder.EmitSetObjectField();
                    }
                    else
                    {
                        // 2: $b->$a
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
                    this.EmitStoreRefAssign(node, codeGenerator);
                }
            }

            internal override PhpTypeCode EmitAssign(IndirectVarUse node, CodeGenerator codeGenerator)
            {
                PhpTypeCode result;
                switch (access)
                {
                    case AccessType.None:
                        // Do nothing
                        result = PhpTypeCode.Void;
                        break;

                    case AccessType.Read:
                        // Do nothing
                        result = PhpTypeCode.Object;
                        break;

                    case AccessType.Write:
                    case AccessType.WriteAndReadRef:
                    case AccessType.WriteAndReadUnknown:
                    case AccessType.ReadAndWrite:
                    case AccessType.ReadAndWriteAndReadRef:
                    case AccessType.ReadAndWriteAndReadUnknown:
                        EmitNodeWriteAssign(node, codeGenerator);
                        result = PhpTypeCode.Void;
                        break;

                    case AccessType.ReadRef:
                        // Do nothing
                        result = PhpTypeCode.PhpReference;
                        break;

                    case AccessType.WriteRef:
                        EmitNodeWriteRefAssign(node, codeGenerator);
                        result = PhpTypeCode.PhpReference;
                        break;

                    default:
                        Debug.Fail(null);
                        result = PhpTypeCode.Invalid;
                        break;
                }

                return result;
            }

            internal override void EmitUnset(IndirectVarUse node, CodeGenerator codeGenerator)
            {
                //Template:  "unset($$x)"   $$x = null;
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
                    // 2: $b->$a
                    // 8: b[]->$a
                    codeGenerator.ChainBuilder.Create();
                    codeGenerator.ChainBuilder.Begin();
                    EmitUnsetField(node, codeGenerator);
                    codeGenerator.ChainBuilder.End();
                    return;
                }
                // 7: $a
                // Unset this variable
                //codeGenerator.EmitVariableUnset(this);
                ILEmitter il = codeGenerator.IL;
                if (codeGenerator.OptimizedLocals)
                {
                    EmitSwitch(node, codeGenerator, new SwitchMethod(UnsetLocal));
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
            private void EmitUnsetField(IndirectVarUse node, CodeGenerator codeGenerator)
            {
                // call UnsetProperty operator
                codeGenerator.ChainBuilder.Lengthen(); // for hop over ->
                node.IsMemberOf.Emit(codeGenerator);
                EmitName(node, codeGenerator);
                codeGenerator.EmitLoadClassContext();

                codeGenerator.IL.EmitCall(OpCodes.Call, Methods.Operators.UnsetProperty, null);
            }

            internal override PhpTypeCode EmitIsset(IndirectVarUse node, CodeGenerator codeGenerator, bool empty)
            {
                //TODO:
                // Template: "isset(x)"     x != null        
                //				isset doesn't distinguish between the NULL and uninitialized variable
                //				a reference is dereferenced, i.e. isset tells us whether the referenced variable is set 

                Debug.Assert(access == AccessType.Read);
                // Cases 1, 4, 5, 6, 9 never reached
                Debug.Assert(codeGenerator.ChainBuilder.IsMember == false);
                // Case 3 never reached
                Debug.Assert(codeGenerator.ChainBuilder.IsArrayItem == false);

                // 2,7,8
                if (node.IsMemberOf != null)
                {
                    // 2: $b->$a
                    // 8: b[]->$a
                    codeGenerator.ChainBuilder.Create();
                    codeGenerator.ChainBuilder.Begin();
                    codeGenerator.ChainBuilder.Lengthen(); // for hop over ->
                    codeGenerator.ChainBuilder.QuietRead = true;
                    EmitReadField(node, codeGenerator, false);
                    codeGenerator.ChainBuilder.End();
                    return PhpTypeCode.Object;
                }
                else
                {
                    // 7: $a
                    // Check wheteher this variable is set

                    codeGenerator.ChainBuilder.QuietRead = true;
                    this.EmitLoad(node, codeGenerator);
                    return PhpTypeCode.Object;
                }
            }

            /// <summary>
            /// Emits load of the name to the stack.
            /// </summary>
            internal override void EmitName(IndirectVarUse node, CodeGenerator codeGenerator)
            {
                codeGenerator.ChainBuilder.Create();
                codeGenerator.EmitConversion(node.VarNameEx, PhpTypeCode.String);
                codeGenerator.ChainBuilder.End();
            }

            /// <summary>
            /// Emits IL instructions that load the variable onto the evaluation stack.
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="codeGenerator"></param>
            /// <remarks><B>$this</B> cannot be accessed indirectly.</remarks>
            internal override PhpTypeCode EmitLoad(IndirectVarUse node, CodeGenerator codeGenerator)
            {
                ILEmitter il = codeGenerator.IL;
                if (codeGenerator.OptimizedLocals)
                {
                    // Switch over all local variables and dereference those being of type PhpReference
                    EmitSwitch(node, codeGenerator, new SwitchMethod(LoadLocal));
                }
                else
                {
                    // LOAD Operators.GetVariable[Unchecked](<script context>, <local variables table>, <variable name>);
                    codeGenerator.EmitLoadScriptContext();
                    codeGenerator.EmitLoadRTVariablesTable();
                    EmitName(node, codeGenerator);
                    if (codeGenerator.ChainBuilder.QuietRead)
                        il.Emit(OpCodes.Call, Methods.Operators.GetVariableUnchecked);
                    else
                        il.Emit(OpCodes.Call, Methods.Operators.GetVariable);
                }

                return PhpTypeCode.Object;
            }

            internal override void EmitLoadAddress(IndirectVarUse node, CodeGenerator codeGenerator)
            {
                ILEmitter il = codeGenerator.IL;
                if (codeGenerator.OptimizedLocals)
                {
                    // Template: for IndirectVarUse
                    //	***** emit "switch" to make sure whether a variable is PhpReference or not
                    //				Inside the switch do the same work as in DirectVarUse case. 

                    // For IndirectVarUse emit switch over all variables. Load address of specified variable.
                    EmitSwitch(node, codeGenerator, new SwitchMethod(LoadLocalAddress));
                }
                else
                {
                    // Template:
                    //		object Operators.GetVariableUnchecked(IDictionary table, string name) //returns variable value
                    this.LoadTabledVariableAddress(node, codeGenerator);
                }
            }

            internal override void EmitLoadAddress_StoreBack(IndirectVarUse node, CodeGenerator codeGenerator)
            {
                EmitLoadAddress_StoreBack(node, codeGenerator, false);
            }

            internal override void EmitLoadAddress_StoreBack(IndirectVarUse node, CodeGenerator codeGenerator, bool duplicate_value)
            {
                ILEmitter il = codeGenerator.IL;
                if (codeGenerator.OptimizedLocals)
                {
                    // Take no action
                    return;
                }
                this.StoreTabledVariableBack(node, codeGenerator, duplicate_value);
            }

            internal override void EmitLoadRef(IndirectVarUse node, CodeGenerator codeGenerator)
            {
                ILEmitter il = codeGenerator.IL;
                if (codeGenerator.OptimizedLocals)
                {
                    // For IndirectVarUse emit switch over all variables.
                    EmitSwitch(node, codeGenerator, new SwitchMethod(LoadLocalRef));
                }
                else
                {
                    // Template:
                    //		PhpReference Operators.GetVariableRef(IDictionary table, string name) //returns variable value; variable is of type PhpReference
                    codeGenerator.EmitLoadScriptContext();
                    codeGenerator.EmitLoadRTVariablesTable();
                    EmitName(node, codeGenerator);
                    il.Emit(OpCodes.Call, Methods.Operators.GetVariableRef);
                }
            }

            internal override void EmitStorePrepare(IndirectVarUse node, CodeGenerator codeGenerator)
            {
                ILEmitter il = codeGenerator.IL;
                if (codeGenerator.OptimizedLocals)
                {
                    // Switch over all variables
                    // /*historical reason, not needed now*/EmitSwitch(codeGenerator, new SwitchMethod(StoreLocalPrepare));
                }
                else
                {
                    // Template:
                    //		void Operators.SetVariable(table, "x", PhpVariable.Copy(Operators.getValue(table, "x"), CopyReason.Assigned));

                    codeGenerator.EmitLoadScriptContext();
                    codeGenerator.EmitLoadRTVariablesTable();
                    EmitName(node, codeGenerator);
                    // now load value the call Operators.SetVariable in EmitVariableStoreAssignFromTable	
                }
            }

            internal override void EmitStoreAssign(IndirectVarUse node, CodeGenerator codeGenerator)
            {
                ILEmitter il = codeGenerator.IL;
                if (codeGenerator.OptimizedLocals)
                {
                    // For IndirectVarUse emit switch over all variables
                    EmitSwitch(node, codeGenerator, new SwitchMethod(StoreLocalAssign));
                }
                else
                {
                    // Template:
                    //		void Operators.SetVariable(table, "x", PhpVariable.Copy(Operators.getValue(table, "x"), CopyReason.Assigned));
                    il.Emit(OpCodes.Call, Methods.Operators.SetVariable);
                }
            }

            internal override void EmitStoreRefPrepare(IndirectVarUse node, CodeGenerator codeGenerator)
            {
                ILEmitter il = codeGenerator.IL;
                if (codeGenerator.OptimizedLocals)
                {
                    // Switch over all variables
                    // /*copypaste bug*/EmitSwitch(codeGenerator, new SwitchMethod(StoreLocalPrepare));
                }
                else
                {
                    // Template:
                    //		void Operators.SetVariable(table, "x", PhpVariable.Copy(Operators.getValue(table, "x"), CopyReason.Assigned));

                    codeGenerator.EmitLoadScriptContext();
                    codeGenerator.EmitLoadRTVariablesTable();
                    EmitName(node, codeGenerator);
                    // now load value the call Operators.SetVariable in EmitVariableStoreAssignFromTable	
                }
            }

            internal override void EmitStoreRefAssign(IndirectVarUse node, CodeGenerator codeGenerator)
            {
                ILEmitter il = codeGenerator.IL;
                if (codeGenerator.OptimizedLocals)
                {
                    // For IndirectVarUse emit switch over all variables
                    EmitSwitch(node, codeGenerator, new SwitchMethod(StoreLocalRefAssign));
                }
                else
                {
                    // Operators.SetVariable( <FROM EmitStoreRefPrepare> )
                    il.Emit(OpCodes.Call, Methods.Operators.SetVariableRef);
                }
            }

            #endregion

            #region Switching over local variables

            /// <summary>
            /// <see cref="SwitchMethod"/> delegate instances stands as a parameter for <see cref="EmitSwitch"/> method.
            /// </summary>
            internal delegate void SwitchMethod(IndirectVarUse node, CodeGenerator codeGenerator, VariablesTable.Entry variable, LocalBuilder variableName);

            void IVariableSwitchEmitter.LoadLocal(IndirectVarUse node, CodeGenerator codeGenerator)
            {
                EmitSwitch(node, codeGenerator, new SwitchMethod(LoadLocal));
            }

            /// <summary>
            /// Emits local variable switch and performs a specified operation on each case.
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="codeGenerator">The code generator.</param>
            /// <param name="method">The operation performed in each case.</param>
            internal void EmitSwitch(IndirectVarUse node, CodeGenerator codeGenerator, SwitchMethod method)
            {
                ILEmitter il = codeGenerator.IL;

                Debug.Assert(method != null);

                Label default_case = il.DefineLabel();
                Label end_label = il.DefineLabel();
                LocalBuilder ivar_local = il.GetTemporaryLocal(Types.String[0], true);
                LocalBuilder non_interned_local = il.DeclareLocal(Types.String[0]);
                VariablesTable variables = codeGenerator.CurrentVariablesTable;
                Label[] labels = new Label[variables.Count];

                // non_interned_local = <name expression>;
                EmitName(node, codeGenerator);
                il.Stloc(non_interned_local);

                // ivar_local = String.IsInterned(non_interned_local)
                il.Ldloc(non_interned_local);
                il.Emit(OpCodes.Call, Methods.String_IsInterned);
                il.Stloc(ivar_local);

                // switch for every compile-time variable:
                int i = 0;
                foreach (VariablesTable.Entry variable in variables)
                {
                    labels[i] = il.DefineLabel();

                    // IF (ivar_local == <i-th variable name>) GOTO labels[i];
                    il.Ldloc(ivar_local);
                    il.Emit(OpCodes.Ldstr, variable.VariableName.ToString());
                    il.Emit(OpCodes.Beq, labels[i]);
                    i++;
                }

                // GOTO default_case:
                il.Emit(OpCodes.Br, default_case);

                // operation on each variable:
                i = 0;
                foreach (VariablesTable.Entry variable in variables)
                {
                    // labels[i]:
                    il.MarkLabel(labels[i]);

                    // operation:
                    method(node, codeGenerator, variable, null);

                    // GOTO end;
                    il.Emit(OpCodes.Br, end_label);
                    i++;
                }

                // default case - new variable created at runtime:
                il.MarkLabel(default_case);
                method(node, codeGenerator, null, non_interned_local);

                // END:
                il.MarkLabel(end_label);
            }

            /// <summary>
            /// Loads a value of a specified variable. If the variable is of type <see cref="PhpReference"/>, it is dereferenced.
            /// </summary>
            internal static void LoadLocal(IndirectVarUse node, CodeGenerator codeGenerator, VariablesTable.Entry variable, LocalBuilder variableName)
            {
                ILEmitter il = codeGenerator.IL;
                Debug.Assert(variable == null ^ variableName == null);

                if (variable != null)
                {
                    // LOAD DEREF <variable>;
                    variable.Variable.EmitLoad(il);
                    if (variable.IsPhpReference)
                        il.Emit(OpCodes.Ldfld, Fields.PhpReference_Value);
                }
                else
                {
                    // LOAD Operators.GetVariable[Unchecked](<script context>, <local variables table>, <variable name>);
                    codeGenerator.EmitLoadScriptContext();
                    codeGenerator.EmitLoadRTVariablesTable();
                    il.Ldloc(variableName);
                    if (codeGenerator.ChainBuilder.QuietRead)
                        il.Emit(OpCodes.Call, Methods.Operators.GetVariableUnchecked);
                    else
                        il.Emit(OpCodes.Call, Methods.Operators.GetVariable);
                }
            }

            /// <summary>
            /// Loads and address of a specified variable.
            /// </summary>
            internal void LoadLocalAddress(IndirectVarUse node, CodeGenerator codeGenerator, VariablesTable.Entry variable, LocalBuilder variableName)
            {
                ILEmitter il = codeGenerator.IL;
                Debug.Assert(variable == null ^ variableName == null);

                if (variable != null)
                {
                    if (variable.IsPhpReference)
                    {
                        // LOAD ADDR <variable>.value;
                        variable.Variable.EmitLoad(il);
                        il.Emit(OpCodes.Ldflda, Fields.PhpReference_Value);
                    }
                    else
                    {
                        variable.Variable.EmitLoadAddress(il);
                    }
                }
                else
                {
                    LoadTabledVariableAddress(node, codeGenerator);
                }
            }

            /// <summary>
            /// Loads a specified reference local variable.
            /// </summary>
            internal static void LoadLocalRef(IndirectVarUse node, CodeGenerator codeGenerator, VariablesTable.Entry variable, LocalBuilder variableName)
            {
                ILEmitter il = codeGenerator.IL;
                Debug.Assert(variable == null ^ variableName == null);

                if (variable != null)
                {
                    Debug.Assert(variable.IsPhpReference);
                    variable.Variable.EmitLoad(il);
                }
                else
                {
                    codeGenerator.EmitLoadScriptContext();
                    codeGenerator.EmitLoadRTVariablesTable();
                    il.Ldloc(variableName);
                    il.Emit(OpCodes.Call, Methods.Operators.GetVariableRef);
                }
            }

            ///// <summary>
            ///// Prepares local variable for a store operation.
            ///// </summary>
            //internal void StoreLocalPrepare(CodeGenerator codeGenerator, VariablesTable.Entry variable, LocalBuilder variableName)
            //{
            //    Debug.Assert(variable == null ^ variableName == null);
            //}

            /// <summary>
            /// Unsets a specified variable.
            /// </summary>
            internal static void UnsetLocal(IndirectVarUse node, CodeGenerator codeGenerator, VariablesTable.Entry variable, LocalBuilder variableName)
            {
                ILEmitter il = codeGenerator.IL;
                Debug.Assert(variable == null ^ variableName == null);

                if (variable != null)
                {
                    if (variable.IsPhpReference)
                    {
                        // <variable> = new PhpReference();
                        il.Emit(OpCodes.Newobj, Constructors.PhpReference_Void);
                        variable.Variable.EmitStore(il);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldnull);
                        variable.Variable.EmitStore(il);
                    }
                }
                else
                {
                    // CALL Operators.SetVariable(<local variables table>,<name>,null);
                    codeGenerator.EmitLoadScriptContext();
                    codeGenerator.EmitLoadRTVariablesTable();
                    il.Ldloc(variableName);
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Call, Methods.Operators.SetVariable);
                }
            }

            /// <summary>
            /// Stores a value on the top of the stack to a specified variable.
            /// </summary>
            internal static void StoreLocalAssign(IndirectVarUse node, CodeGenerator codeGenerator, VariablesTable.Entry variable, LocalBuilder variableName)
            {
                ILEmitter il = codeGenerator.IL;
                Debug.Assert(variable == null ^ variableName == null);
                LocalBuilder temp;

                if (variable != null)
                {
                    if (variable.IsPhpReference)
                    {
                        // temp = STACK 
                        temp = il.GetTemporaryLocal(Types.Object[0], true);
                        il.Stloc(temp);

                        // <variable>.value = temp;
                        variable.Variable.EmitLoad(il);
                        il.Ldloc(temp);
                        il.Emit(OpCodes.Stfld, Fields.PhpReference_Value);
                    }
                    else
                    {
                        variable.Variable.EmitStore(il);
                    }
                }
                else
                {
                    // temp = STACK
                    temp = il.GetTemporaryLocal(Types.Object[0], true);
                    il.Stloc(temp);

                    // CALL Operators.SetVariable(<local variables table>,<name>,temp);
                    codeGenerator.EmitLoadScriptContext();
                    codeGenerator.EmitLoadRTVariablesTable();
                    il.Ldloc(variableName);
                    il.Ldloc(temp);
                    il.Emit(OpCodes.Call, Methods.Operators.SetVariable);
                }
            }

            /// <summary>
            /// Stores a reference on the top of the stack to a specified variable.
            /// </summary>
            internal static void StoreLocalRefAssign(IndirectVarUse node, CodeGenerator codeGenerator, VariablesTable.Entry variable, LocalBuilder variableName)
            {
                ILEmitter il = codeGenerator.IL;
                Debug.Assert(variable == null ^ variableName == null);

                if (variable != null)
                {
                    Debug.Assert(variable.IsPhpReference);
                    variable.Variable.EmitStore(il);
                }
                else
                {
                    // temp = STACK
                    LocalBuilder temp = il.GetTemporaryLocal(Types.PhpReference[0], true);
                    il.Stloc(temp);

                    // CALL Operators.SetVariableRef(<local variables table>,<name>,temp);
                    codeGenerator.EmitLoadScriptContext();
                    codeGenerator.EmitLoadRTVariablesTable();
                    il.Ldloc(variableName);
                    il.Ldloc(temp);
                    il.Emit(OpCodes.Call, Methods.Operators.SetVariableRef);
                }
            }

            #endregion
        }
    }

    #region IVariableSwitchEmitter

    internal interface IVariableSwitchEmitter
    {
        void LoadLocal(IndirectVarUse node, CodeGenerator codeGenerator);
    }

    internal static class IndirectVarUseCompilerHelper
    {
        public static void EmitSwitch_LoadLocal(this IndirectVarUse node, CodeGenerator codeGenerator)
        {
            node.NodeCompiler<IVariableSwitchEmitter>().LoadLocal(node, codeGenerator);
        }
    }

    #endregion
}
