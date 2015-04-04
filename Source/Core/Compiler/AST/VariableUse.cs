/*

 Copyright (c) 2006- DEVSENSE
 Copyright (c) 2004-2006 Tomas Matousek, Ladislav Prosek, Vaclav Novak and Martin Maly.

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
        #region VariableUse

        /// <summary>
        /// Base class for variable uses.
        /// </summary>
        abstract class VariableUseCompiler<T> : VarLikeConstructUseCompiler<T>, IVariableUseCompiler where T : VariableUse
        {
            internal abstract PhpTypeCode EmitAssign(T/*!*/node, CodeGenerator codeGenerator);
            internal abstract PhpTypeCode EmitIsset(T/*!*/node, CodeGenerator codeGenerator, bool empty);
            internal abstract void EmitUnset(T/*!*/node, CodeGenerator codeGenerator);

            #region IVariableUseCompiler Members

            PhpTypeCode IVariableUseCompiler.EmitAssign(VariableUse node, CodeGenerator codeGenerator)
            {
                return EmitAssign((T)node, codeGenerator);
            }

            PhpTypeCode IVariableUseCompiler.EmitIsset(VariableUse node, CodeGenerator codeGenerator, bool empty)
            {
                return EmitIsset((T)node, codeGenerator, empty);
            }

            void IVariableUseCompiler.EmitUnset(VariableUse node, CodeGenerator codeGenerator)
            {
                EmitUnset((T)node, codeGenerator);
            }

            #endregion
        }

        #endregion

        // possible access values for all VariableUse subclasses: 
        // Read, Write, ReadRef, ReadUnknown, WriteRef, None

        #region CompoundVarUse

        abstract class CompoundVarUseCompiler<T> : VariableUseCompiler<T> where T : CompoundVarUse
        {

        }

        #endregion

        #region SimpleVarUse

        abstract class SimpleVarUseCompiler<T> : CompoundVarUseCompiler<T>, ISimpleVarUseCompiler where T : SimpleVarUse
        {
            /// <summary>
            /// Points to a method that emits code to be placed after the new instance field value has
            /// been loaded onto the evaluation stack.
            /// </summary>
            internal AssignmentCallback assignmentCallback;

            /// <summary>
            /// A holder of a temporary local variable which is used to obtain address of a variable
            /// stored in runtime variables table when methods from <see cref="PHP.Core.Operators"/>
            /// expecting a ref argument are invoked.
            /// </summary>
            /// <remarks>After the operator is invoked, the result should be stored back to table and this
            /// holder may be released. This holder does not take place in optimalized user functions and methods of
            /// user classes as the address of a variable shoul be obtained directly (variables are defined as
            /// locals).</remarks>
            protected LocalBuilder TabledLocalAddressStorage;

            internal abstract void EmitName(T/*!*/node, CodeGenerator codeGenerator);
            internal abstract PhpTypeCode EmitLoad(T/*!*/node, CodeGenerator codeGenerator);
            internal abstract void EmitLoadAddress(T/*!*/node, CodeGenerator codeGenerator);
            internal abstract void EmitLoadAddress_StoreBack(T/*!*/node, CodeGenerator codeGenerator);
            internal abstract void EmitLoadAddress_StoreBack(T/*!*/node, CodeGenerator codeGenerator, bool duplicate_value);
            internal abstract void EmitLoadRef(T/*!*/node, CodeGenerator codeGenerator);
            internal abstract void EmitStorePrepare(T/*!*/node, CodeGenerator codeGenerator);
            internal abstract void EmitStoreAssign(T/*!*/node, CodeGenerator codeGenerator);
            internal abstract void EmitStoreRefPrepare(T/*!*/node, CodeGenerator codeGenerator);
            internal abstract void EmitStoreRefAssign(T/*!*/node, CodeGenerator codeGenerator);

            /// <summary>
            /// Loads the value represented by this object from the runtime variables table,
            /// stores it to a local variable and loads the address of this local.
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <remarks>This method is used only in non-optimized user functions and global code.
            /// Specified local variable is obtained from current <see cref="ILEmitter"/> by
            /// <see cref="ILEmitter.GetTemporaryLocal"/> and stored to <see cref="TabledLocalAddressStorage"/>
            /// for later use. Once the local become useless, <see cref="ILEmitter.ReturnTemporaryLocal"/>
            /// should be called.
            /// </remarks>
            /// <param name="codeGenerator">Currently used <see cref="CodeGenerator"/>.</param>
            internal virtual void LoadTabledVariableAddress(T/*!*/node, CodeGenerator codeGenerator)
            {
                // This function should be call only once on every SimpleVarUse object
                // TODO: ASSERTION FAILS (e.g. PhpMyAdmin, common.lib.php)
                // Debug.Assert(this.TabledLocalAddressStorage == null);
                ILEmitter il = codeGenerator.IL;

                // Load the value represented by this node from the runtime variables table

                // LOAD Operators.GetVariableUnchecked(<script context>, <local variables table>, <variable name>);
                codeGenerator.EmitLoadScriptContext();
                codeGenerator.EmitLoadRTVariablesTable();
                EmitName(node, codeGenerator);
                codeGenerator.IL.Emit(OpCodes.Call, Methods.Operators.GetVariableUnchecked);

                // Get local from ILEmitter
                this.TabledLocalAddressStorage = il.GetTemporaryLocal(Types.Object[0]);
                // Store the value
                il.Stloc(this.TabledLocalAddressStorage);
                // Load the address
                il.Ldloca(this.TabledLocalAddressStorage);
            }

            /// <summary>
            /// Stores the value represented by <see cref="TabledLocalAddressStorage"/> to the runtime variables table and 
            /// returns the <see cref="TabledLocalAddressStorage"/> back to <c>temporaryLocals</c>.
            /// Duplicates the value if requested.
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="codeGenerator">Currently used <see cref="CodeGenerator"/>.</param>
            /// <param name="duplicate_value">If <c>true</c>, the value of specified local is left on the evaluation stack.
            /// </param>
            internal virtual void StoreTabledVariableBack(T/*!*/node, CodeGenerator codeGenerator, bool duplicate_value)
            {
                ILEmitter il = codeGenerator.IL;

                // CALL Operators.SetVariable(<local variables table>,<name>,<TabledLocalAddressStorage>);
                codeGenerator.EmitLoadScriptContext();
                codeGenerator.EmitLoadRTVariablesTable();
                EmitName(node, codeGenerator);
                il.Ldloc(TabledLocalAddressStorage);
                il.Emit(OpCodes.Call, Methods.Operators.SetVariable);

                // If requested, load the changed value on the evaluation stack
                if (duplicate_value)
                    il.Ldloc(this.TabledLocalAddressStorage);

                // Release temporary local
                il.ReturnTemporaryLocal(this.TabledLocalAddressStorage);
            }

            /// <summary>
            /// Emits IL instructions that read the value of an instance field.
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="codeGenerator">The current <see cref="CodeGenerator"/>.</param>
            /// <param name="wantRef">If <B>false</B> the field value should be left on the evaluation stack,
            /// if <B>true</B> the <see cref="PhpReference"/> should be left on the evaluation stack.</param>
            /// <returns>
            /// Nothing is expected on the evaluation stack. A <see cref="PhpReference"/> (if <paramref name="wantRef"/>
            /// is <B>true</B>) or the field value itself (if <paramref name="wantRef"/> is <B>false</B>) is left on the
            /// evaluation stack.
            /// </returns>
            internal virtual PhpTypeCode EmitReadField(T/*!*/node, CodeGenerator codeGenerator, bool wantRef)
            {
                ILEmitter il = codeGenerator.IL;

                DirectVarUse direct_instance = node.IsMemberOf as DirectVarUse;
                if (direct_instance != null && direct_instance.IsMemberOf == null && direct_instance.VarName.IsThisVariableName)
                {
                    return EmitReadFieldOfThis(node, codeGenerator, wantRef);
                }


                if (!wantRef)
                {
                    //codeGenerator.ChainBuilder.Lengthen();
                    //PhpTypeCode type_code = isMemberOf.Emit(codeGenerator);
                    //Debug.Assert(type_code == PhpTypeCode.Object || type_code == PhpTypeCode.DObject);

                    //// CALL Operators.GetProperty(STACK,<field name>,<type desc>,<quiet>);
                    //EmitName(codeGenerator);
                    //codeGenerator.EmitLoadClassContext();
                    //il.LoadBool(codeGenerator.ChainBuilder.QuietRead);
                    //il.Emit(OpCodes.Call, Methods.Operators.GetProperty);
                    //return PhpTypeCode.Object;

                    string fieldName = (node is DirectVarUse) ? (node as DirectVarUse).VarName.Value : null;
                    Expression fieldNameExpr = (node is IndirectVarUse) ? (node as IndirectVarUse).VarNameEx : null;
                    bool quietRead = wantRef ? false : codeGenerator.ChainBuilder.QuietRead;
                    return codeGenerator.CallSitesBuilder.EmitGetProperty(
                        codeGenerator, wantRef,
                        node.IsMemberOf, null, null,
                        null,
                        fieldName, fieldNameExpr,
                        quietRead);
                }

                // call GetProperty/GetObjectPropertyRef
                codeGenerator.ChainBuilder.Lengthen();
                // loads the variable which field is gotten:
                PhpTypeCode var_type_code = node.IsMemberOf.Emit(codeGenerator);

                if (codeGenerator.ChainBuilder.Exists)
                {
                    Debug.Assert(var_type_code == PhpTypeCode.DObject);

                    // CALL Operators.GetObjectPropertyRef(STACK,<field name>,<type desc>);
                    EmitName(node, codeGenerator);
                    codeGenerator.EmitLoadClassContext();
                    il.Emit(OpCodes.Call, Methods.Operators.GetObjectPropertyRef);
                }
                else
                {
                    Debug.Assert(var_type_code == PhpTypeCode.ObjectAddress);

                    // CALL Operators.GetPropertyRef(ref STACK,<field name>,<type desc>,<script context>);
                    EmitName(node, codeGenerator);
                    codeGenerator.EmitLoadClassContext();
                    codeGenerator.EmitLoadScriptContext();
                    il.Emit(OpCodes.Call, Methods.Operators.GetPropertyRef);

                    // stores the value of variable back:
                    SimpleVarUse simple_var = node.IsMemberOf as SimpleVarUse;
                    if (simple_var != null)
                        simple_var.EmitLoadAddress_StoreBack(codeGenerator);
                }

                return PhpTypeCode.PhpReference;
            }

            /// <summary>
            /// Emits IL instructions that read the value of a field of $this instance.
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="codeGenerator">The current <see cref="CodeGenerator"/>.</param>
            /// <param name="wantRef">If <B>false</B> the field value should be left on the evaluation stack,
            /// if <B>true</B> the <see cref="PhpReference"/> should be left on the evaluation stack.</param>
            /// <returns></returns>
            private PhpTypeCode EmitReadFieldOfThis(T/*!*/node, CodeGenerator/*!*/ codeGenerator, bool wantRef)
            {
                ILEmitter il = codeGenerator.IL;

                // $this->a
                switch (codeGenerator.LocationStack.LocationType)
                {
                    case LocationTypes.GlobalCode:
                        {
                            // load $this from one of Main's arguments and check for null
                            Label this_non_null = il.DefineLabel();
                            Label reading_over = il.DefineLabel();

                            codeGenerator.EmitLoadSelf();
                            il.Emit(OpCodes.Brtrue_S, this_non_null);
                            EmitThisUsedOutOfObjectThrow(codeGenerator, wantRef);
                            il.Emit(OpCodes.Br, reading_over);
                            il.MarkLabel(this_non_null, true);

                            // call GetObjectProperty/GetObjectPropertyRef
                            EmitGetFieldOfPlace(node, codeGenerator.SelfPlace, codeGenerator, wantRef);

                            il.MarkLabel(reading_over, true);

                            break;
                        }

                    case LocationTypes.FunctionDecl:
                        {
                            EmitThisUsedOutOfObjectThrow(codeGenerator, wantRef);
                            break;
                        }

                    case LocationTypes.MethodDecl:
                        {
                            CompilerLocationStack.MethodDeclContext context = codeGenerator.LocationStack.PeekMethodDecl();
                            if (context.Method.IsStatic)
                            {
                                EmitThisUsedOutOfObjectThrow(codeGenerator, wantRef);
                                break;
                            }

                            // attempt direct field reading (DirectVarUse only)
                            return EmitReadFieldOfThisInInstanceMethod(node, codeGenerator, wantRef);
                        }
                }

                return wantRef ? PhpTypeCode.PhpReference : PhpTypeCode.Object;
            }

            /// <summary>
            /// Emits IL instructions that read the value of a field of $this instance when we know that we
            /// are in an instance method and hence there's a chance of actually resolving the field being read.
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="codeGenerator">The current <see cref="CodeGenerator"/>.</param>
            /// <param name="wantRef">If <B>false</B> the field value should be left on the evaluation stack,
            /// if <B>true</B> the <see cref="PhpReference"/> should be left on the evaluation stack.</param>
            internal virtual PhpTypeCode EmitReadFieldOfThisInInstanceMethod(T/*!*/node, CodeGenerator/*!*/ codeGenerator, bool wantRef)
            {
                // the override in DirectVarUse is a bit more sophisticated ;)
                return EmitGetFieldOfPlace(node, codeGenerator.SelfPlace, codeGenerator, wantRef);
            }

            /// <summary>
            /// Emits error reporting call when "this" variable is used out of object context.
            /// </summary>
            private static void EmitThisUsedOutOfObjectThrow(CodeGenerator/*!*/ codeGenerator, bool wantRef)
            {
                codeGenerator.EmitPhpException(Methods.PhpException.ThisUsedOutOfObjectContext);
                if (wantRef)
                    codeGenerator.IL.Emit(OpCodes.Newobj, Constructors.PhpReference_Void);
                else
                    codeGenerator.IL.Emit(OpCodes.Ldnull);
            }

            /// <summary>
            /// Emits <see cref="Operators.GetObjectProperty"/> or <see cref="Operators.GetObjectPropertyRef"/>
            /// on a specified argument variable.
            /// </summary>
            private PhpTypeCode EmitGetFieldOfPlace(T/*!*/node, IPlace/*!*/ arg, CodeGenerator/*!*/ codeGenerator, bool wantRef)
            {
                //ILEmitter il = codeGenerator.IL;

                //arg.EmitLoad(il);
                //EmitName(codeGenerator);
                //codeGenerator.EmitLoadClassContext();

                //if (wantRef)
                //{
                //    il.Emit(OpCodes.Call, Methods.Operators.GetObjectPropertyRef);
                //    return PhpTypeCode.PhpReference;
                //}
                //else
                //{
                //    il.LoadBool(codeGenerator.ChainBuilder.QuietRead);
                //    il.Emit(OpCodes.Call, Methods.Operators.GetObjectProperty);
                //    return PhpTypeCode.Object;
                //}

                string fieldName = (node is DirectVarUse) ? (node as DirectVarUse).VarName.Value : null;
                Expression fieldNameExpr = (node is IndirectVarUse) ? (node as IndirectVarUse).VarNameEx : null;
                bool quietRead = wantRef ? false : codeGenerator.ChainBuilder.QuietRead;

                return codeGenerator.CallSitesBuilder.EmitGetProperty(
                    codeGenerator, wantRef,
                    null, arg, null,
                    null,
                    fieldName, fieldNameExpr,
                    quietRead);
            }

            private static void EmitCallSetObjectField(CodeGenerator/*!*/ codeGenerator, PhpTypeCode stackTypeCode)
            {
                // CALL Operators.SetObjectProperty(<STACK:instance>,<STACK:field name>,<STACK:field value>, <type desc>)
                codeGenerator.EmitLoadClassContext();

                codeGenerator.IL.Emit(OpCodes.Call, Methods.Operators.SetObjectProperty);

                //always when function with void return argument is called it's necesarry to add nop instruction due to debugger
                if (codeGenerator.Context.Config.Compiler.Debug)
                {
                    codeGenerator.IL.Emit(OpCodes.Nop);
                }
            }

            private static void EmitPopValue(CodeGenerator/*!*/ codeGenerator, PhpTypeCode stackTypeCode)
            {
                // just pop the value that was meant to be written
                codeGenerator.IL.Emit(OpCodes.Pop);
            }

            /// <summary>
            /// Emits IL instructions that write a value to an instance field.
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="codeGenerator">The current <see cref="CodeGenerator"/>.</param>
            /// <param name="writeRef">If <B>true</B> the value being written is a <see cref="PhpReference"/>
            /// instance, if <B>false</B> it is an <see cref="Object"/> instance.</param>
            /// <returns>Delegate to a method that emits code to be executed when the actual value has been
            /// loaded on the evaluation stack.</returns>
            /// <remarks>
            /// If the field could be resolved at compile time (because <see cref="VarLikeConstructUse.IsMemberOf"/> is <c>$this</c> or a
            /// variable is proved to be of a certain type by type analysis), direct field writing code is emitted.
            /// Otherwise, <see cref="Operators.SetProperty"/> or <see cref="Operators.SetObjectProperty"/> call is emitted.
            /// </remarks>
            internal virtual AssignmentCallback EmitWriteField(T/*!*/node, CodeGenerator/*!*/ codeGenerator, bool writeRef)
            {
                ILEmitter il = codeGenerator.IL;

                DirectVarUse direct_instance = node.IsMemberOf as DirectVarUse;
                if (direct_instance != null && direct_instance.IsMemberOf == null && direct_instance.VarName.IsThisVariableName)
                {
                    return EmitWriteFieldOfThis(node, codeGenerator, writeRef);
                }

                if (node.IsMemberOf is ItemUse || node.IsMemberOf is StaticFieldUse || node.IsMemberOf.IsMemberOf != null)
                {
                    // we are part of a chain
                    // Lengthen for hop over ->
                    codeGenerator.ChainBuilder.Lengthen();
                    FunctionCall funcCall = node.IsMemberOf as FunctionCall;
                    if (funcCall == null)
                    {
                        node.IsMemberOf.Emit(codeGenerator);
                        EmitName(node, codeGenerator);
                    }
                    else
                    {
                        codeGenerator.ChainBuilder.LoadAddressOfFunctionReturnValue = true;
                        node.IsMemberOf.Emit(codeGenerator);
                        codeGenerator.ChainBuilder.RecastValueReturnedByFunctionCall();

                        EmitName(node, codeGenerator);
                    }
                    return new AssignmentCallback(EmitCallSetObjectField);
                }
                else
                {
                    return delegate(CodeGenerator codeGen, PhpTypeCode stackTypeCode)
                    {
                        codeGen.ChainBuilder.Lengthen();

                        // CALL Operators.SetProperty(STACK,ref <instance>,<field name>,<handle>,<script context>);
                        node.IsMemberOf.Emit(codeGen);
                        EmitName(node, codeGen);
                        codeGen.EmitLoadClassContext();
                        codeGen.EmitLoadScriptContext();

                        // invoke the operator
                        codeGen.IL.Emit(OpCodes.Call, Methods.Operators.SetProperty);
                    };
                }
            }

            /// <summary>
            /// Emits IL instructions that prepare a field of $this for writing.
            /// </summary>
            private AssignmentCallback EmitWriteFieldOfThis(T/*!*/node, CodeGenerator/*!*/ codeGenerator, bool writeRef)
            {
                ILEmitter il = codeGenerator.IL;

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

                            // prepare the stack for SetObjectProperty call
                            codeGenerator.EmitLoadSelf();
                            EmitName(node, codeGenerator);

                            return new AssignmentCallback(EmitCallSetObjectField);
                        }

                    case LocationTypes.FunctionDecl:
                        {
                            // always throws error
                            codeGenerator.EmitPhpException(Methods.PhpException.ThisUsedOutOfObjectContext);
                            return new AssignmentCallback(EmitPopValue);
                        }

                    case LocationTypes.MethodDecl:
                        {
                            CompilerLocationStack.MethodDeclContext context = codeGenerator.LocationStack.PeekMethodDecl();
                            if (context.Method.IsStatic)
                            {
                                // always throws error
                                codeGenerator.EmitPhpException(Methods.PhpException.ThisUsedOutOfObjectContext);
                                return new AssignmentCallback(EmitPopValue);
                            }

                            // attempt direct field writing (DirectVarUse only)
                            return EmitWriteFieldOfThisInInstanceMethod(node, codeGenerator, writeRef);
                        }
                }

                Debug.Fail("Invalid lcoation type.");
                return null;
            }

            /// <summary>
            /// Emits IL instructions that write the value of a field of $this instance when we know that we
            /// are in an instance method and hence there's a chance of actually resolving the field being written.
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="codeGenerator">The current <see cref="CodeGenerator"/>.</param>
            /// <param name="writeRef">If <B>true</B> the value being written is a <see cref="PhpReference"/>; if
            /// <B>false</B> the value being written is an <see cref="Object"/>.</param>
            /// <returns></returns>
            internal virtual AssignmentCallback EmitWriteFieldOfThisInInstanceMethod(T/*!*/node, CodeGenerator/*!*/ codeGenerator, bool writeRef)
            {
                // prepare for SetObjectProperty call
                codeGenerator.EmitLoadSelf();
                EmitName(node, codeGenerator);

                return new AssignmentCallback(EmitCallSetObjectField);
            }

            #region ISimpleVarUseCompiler Members

            void ISimpleVarUseCompiler.EmitLoadAddress_StoreBack(SimpleVarUse node, CodeGenerator codeGenerator)
            {
                EmitLoadAddress_StoreBack((T)node, codeGenerator);
            }

            void ISimpleVarUseCompiler.EmitName(SimpleVarUse node, CodeGenerator codeGenerator)
            {
                EmitName((T)node, codeGenerator);
            }

            void ISimpleVarUseCompiler.EmitAssign(SimpleVarUse node, CodeGenerator codeGenerator)
            {
                EmitAssign((T)node, codeGenerator);
            }

            void ISimpleVarUseCompiler.EmitLoadAddress(SimpleVarUse node, CodeGenerator codeGenerator)
            {
                EmitLoadAddress((T)node, codeGenerator);
            }

            #endregion
        }

        #endregion
    }

    #region IVariableUseCompiler

    internal interface IVariableUseCompiler
    {
        PhpTypeCode EmitAssign(VariableUse/*!*/node, CodeGenerator codeGenerator);
        PhpTypeCode EmitIsset(VariableUse/*!*/node, CodeGenerator codeGenerator, bool empty);
        void EmitUnset(VariableUse/*!*/node, CodeGenerator codeGenerator);
    }

    internal static class VariableUseHelper
    {
        public static PhpTypeCode EmitAssign(this VariableUse/*!*/node, CodeGenerator codeGenerator)
        {
            return node.NodeCompiler<IVariableUseCompiler>().EmitAssign(node, codeGenerator);
        }
        public static PhpTypeCode EmitIsset(this VariableUse/*!*/node, CodeGenerator codeGenerator, bool empty)
        {
            return node.NodeCompiler<IVariableUseCompiler>().EmitIsset(node, codeGenerator, empty);
        }
        public static void EmitUnset(this VariableUse/*!*/node, CodeGenerator codeGenerator)
        {
            node.NodeCompiler<IVariableUseCompiler>().EmitUnset(node, codeGenerator);
        }
    }

    #endregion

    #region ISimpleVarUseCompiler

    interface ISimpleVarUseCompiler
    {
        void EmitLoadAddress_StoreBack(SimpleVarUse/*!*/node, CodeGenerator codeGenerator);
        void EmitName(SimpleVarUse/*!*/node, CodeGenerator codeGenerator);
        void EmitAssign(SimpleVarUse/*!*/node, CodeGenerator codeGenerator);
        void EmitLoadAddress(SimpleVarUse node, CodeGenerator codeGenerator);
    }

    static class SimpleVarUseHelper
    {
        public static void EmitLoadAddress_StoreBack(this SimpleVarUse/*!*/node, CodeGenerator codeGenerator)
        {
            node.NodeCompiler<ISimpleVarUseCompiler>().EmitLoadAddress_StoreBack(node, codeGenerator);
        }

        public static void EmitName(this SimpleVarUse/*!*/node, CodeGenerator codeGenerator)
        {
            node.NodeCompiler<ISimpleVarUseCompiler>().EmitName(node, codeGenerator);
        }

        public static void EmitAssign(this SimpleVarUse/*!*/node, CodeGenerator codeGenerator)
        {
            node.NodeCompiler<ISimpleVarUseCompiler>().EmitAssign(node, codeGenerator);
        }

        public static void EmitLoadAddress(this SimpleVarUse node, CodeGenerator codeGenerator)
        {
            node.NodeCompiler<ISimpleVarUseCompiler>().EmitLoadAddress(node, codeGenerator);
        }
    }

    #endregion
}
