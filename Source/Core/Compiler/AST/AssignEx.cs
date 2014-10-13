/*

 Copyright (c) 2004-2006 Tomas Matousek, Vaclav Novak and Martin Maly.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Reflection.Emit;
using System.Diagnostics;
using PHP.Core.AST;
using PHP.Core.Emit;
using PHP.Core.Parsers;

namespace PHP.Core.Compiler.AST
{
    partial class NodeCompilers
    {
        abstract class AssignExCompiler : ExpressionCompiler<AssignEx>
        {
            /// <summary>
            /// Whether an expression represented by this node should be stored to a temporary local if assigned.
            /// </summary>
            public override bool StoreOnAssignment(AssignEx node)
            {
                return false;
            }
        }

        #region ValueAssignEx

        [NodeCompiler(typeof(ValueAssignEx))]
        sealed class ValueAssignExCompiler : AssignExCompiler
        {
            /// <include file='Doc/Nodes.xml' path='doc/method[@name="IsDeeplyCopied"]/*'/>
            /// <param name="node">Instance.</param>
            /// <returns>
            /// The copy-on-assignment value of the right hand side.
            /// </returns>
            public override bool IsDeeplyCopied(AssignEx node, CopyReason reason, int nestingLevel)
            {
                return true; // J: once assigned value must be copied again // rvalue.IsDeeplyCopied(reason, nestingLevel + 1);
            }

            #region Analysis

            public override Evaluation Analyze(AssignEx node, Analyzer analyzer, ExInfoFromParent info)
            {
                access = info.Access;

                var valueassignex = (ValueAssignEx)node;

                ExInfoFromParent lvalue_info = new ExInfoFromParent(node);

                // x[] = y
                if (node.LValue is ItemUse && ((ItemUse)node.LValue).Index == null)
                    if (node.Operation != Operations.AssignValue)
                    {
                        var oldop = node.Operation;
                        valueassignex.operation = Operations.AssignValue;

                        // x[] .= y -> x[] = null . y
                        if (oldop == Operations.AssignAppend)
                            valueassignex.rvalue = new BinaryEx(node.Span, Operations.Concat, new NullLiteral(node.Span), valueassignex.rvalue);
                        // x[] += y -> x[] = 0 + y
                        else if (oldop == Operations.AssignAdd)
                            valueassignex.rvalue = new BinaryEx(node.Span, Operations.Add, new NullLiteral(node.Span), valueassignex.rvalue);
                        // x[] -= y -> x[] = 0 - y
                        else if (oldop == Operations.AssignSub)
                            valueassignex.rvalue = new BinaryEx(node.Span, Operations.Sub, new NullLiteral(node.Span), valueassignex.rvalue);
                        // x[] *= y -> x[] = 0 * y
                        else if (oldop == Operations.AssignMul)
                            valueassignex.rvalue = new BinaryEx(node.Span, Operations.Mul, new NullLiteral(node.Span), valueassignex.rvalue);
                        // x[] /= y -> x[] = 0 / y
                        else if (oldop == Operations.AssignDiv)
                            valueassignex.rvalue = new BinaryEx(node.Span, Operations.Div, new NullLiteral(node.Span), valueassignex.rvalue);
                        // x[] &= y -> x[] = 0 & y
                        else if (oldop == Operations.AssignAnd)
                            valueassignex.rvalue = new BinaryEx(node.Span, Operations.BitAnd, new NullLiteral(node.Span), valueassignex.rvalue);
                        else
                        {
                            Debug.Fail("Unhandled operation " + oldop.ToString() + " must be reduced!");
                            valueassignex.operation = oldop;  // change it back, this will result in compile time exception
                        }
                    }

                // stop evaluation:
                valueassignex.rvalue = valueassignex.rvalue.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();

                if (node.Operation == Operations.AssignValue)
                {
                    // elimination of $x = $x . expr
                    var concat = valueassignex.rvalue as ConcatEx;
                    DirectVarUse vur;
                    DirectVarUse vul = valueassignex.lvalue as DirectVarUse;

                    if (concat != null && concat.Expressions.Length >= 2 && vul != null && vul.IsMemberOf == null)
                    {
                        if ((vur = concat.Expressions[0] as DirectVarUse) != null && vur.VarName.Equals(vul.VarName) && vur.IsMemberOf == null)
                        {
                            // $x = $x.a.b.c
                            // =>
                            // $x .= a.b.c

                            valueassignex.operation = Operations.AssignAppend;
                            lvalue_info.Access = AccessType.ReadAndWrite;

                            //rvalue = concat.RightExpr;
                            concat.Expressions = concat.Expressions.TakeArray(1, concat.Expressions.Length - 1);
                        }
                        else if ((vur = concat.Expressions[concat.Expressions.Length - 1] as DirectVarUse) != null && vur.VarName.Equals(vul.VarName) && vur.IsMemberOf == null)
                        {
                            // $x = a.b.c.$x
                            // =>
                            // $x =. a.b.c

                            valueassignex.operation = Operations.AssignPrepend;
                            lvalue_info.Access = AccessType.ReadAndWrite;

                            //rvalue = (Expression)concat.LeftExpr;
                            concat.Expressions = concat.Expressions.TakeArray(0, concat.Expressions.Length - 1);
                        }
                        else
                            lvalue_info.Access = AccessType.Write;
                    }
                    else
                        lvalue_info.Access = AccessType.Write;
                }
                else
                    lvalue_info.Access = AccessType.ReadAndWrite;

                // If this ValueAssignEx is actual param that is to be passed by reference,
                // AccessType of the destVar has to be changed, because its reference will be
                // (potencially) passeed
                ActualParam ap = info.Parent as ActualParam;
                if (ap != null)
                {
                    if (analyzer.ActParamDeclIsUnknown())
                    {
                        if (lvalue_info.Access == AccessType.Write)
                            lvalue_info.Access = AccessType.WriteAndReadUnknown;
                        else
                            lvalue_info.Access = AccessType.ReadAndWriteAndReadUnknown;
                    }
                    else if (analyzer.ActParamPassedByRef())
                    {
                        if (lvalue_info.Access == AccessType.Write)
                            lvalue_info.Access = AccessType.WriteAndReadRef;
                        else
                            lvalue_info.Access = AccessType.ReadAndWriteAndReadRef;
                    }
                }

                valueassignex.lvalue.Analyze(analyzer, lvalue_info); //retval not needed ...

                return new Evaluation(node);
            }

            #endregion

            /// <summary>
            /// Emits assignment.
            /// </summary>
            /// <remarks>
            /// Pattern: a op= b
            ///
            /// PREPARE a      (prepared)
            /// LOAD a         (prepared,a)
            /// LOAD b         (prepared,a,b)
            /// OP             (prepared,result)
            /// *DUP           (prepared,result,result)
            /// *STORE tmp     (prepared,result)           must be this stack here!
            /// STORE a        ()
            /// *LOAD tmp      (result)
            ///
            /// * only if the resulting value needs to be propagated to the right
            ///
            /// Note: There is a possible improvement: some store operations (SetVariable) may return the value set
            /// which would replace DUP and second temp op.
            /// </remarks>
            public override PhpTypeCode Emit(AssignEx node, CodeGenerator codeGenerator)
            {
                Debug.Assert(access == AccessType.Read || access == AccessType.None || access == AccessType.ReadRef ||
                  access == AccessType.ReadUnknown);
                Statistics.AST.AddNode("Assign.Value");

                AccessType old_selector = codeGenerator.AccessSelector;

                codeGenerator.ChainBuilder.Create();

                PhpTypeCode result;

                if (node.Operation == Operations.AssignValue)
                {
                    //
                    // Access Type = ReadRef/ReadUnknown
                    // ---------------------------------
                    //
                    // f(&$x) { }
                    //
                    // f($a = $b); 
                    // f($a = $b =& $c); 
                    //
                    // Destination variable $a is prepared for reference write.
                    // A new reference is created and its value set to a deep copy of the result of RHS ($b, $b =& $c).
                    // RHS has Read access => it has been dereferenced.
                    //

                    // PREPARE a:
                    codeGenerator.AccessSelector = AccessType.Write;
                    node.lvalue.Emit(codeGenerator);
                    codeGenerator.AccessSelector = AccessType.None;

                    PhpTypeCode src_type_code = EmitSourceValRead((ValueAssignEx)node, codeGenerator);

                    // RHS should have Read access => should be dereferenced
                    Debug.Assert(src_type_code != PhpTypeCode.PhpReference);

                    // LOAD BOX b
                    codeGenerator.EmitBoxing(src_type_code);

                    // makes a copy if necessary:
                    if (PhpTypeCodeEnum.IsDeeplyCopied(src_type_code))
                        codeGenerator.EmitVariableCopy(CopyReason.Assigned, ((ValueAssignEx)node).rvalue);
                }
                else
                {
                    // PREPARE a:
                    codeGenerator.AccessSelector = AccessType.Write;
                    node.LValue.Emit(codeGenerator);
                    codeGenerator.AccessSelector = AccessType.None;

                    // LOAD b,a (rvalue must be processed first, than +-*/ with lvalue, since lvalu can be changed by rvalue expression)
                    //must be the second operand// EmitDestVarRead(codeGenerator);
                    PhpTypeCode right_type = EmitSourceValRead((ValueAssignEx)node, codeGenerator);
                    var rvalue_tmp = codeGenerator.IL.GetTemporaryLocal(PhpTypeCodeEnum.ToType(right_type), false);
                    codeGenerator.IL.Emit(OpCodes.Stloc, rvalue_tmp);
                    EmitDestVarRead(node, codeGenerator);
                    codeGenerator.IL.Emit(OpCodes.Ldloc, rvalue_tmp);
                    codeGenerator.IL.ReturnTemporaryLocal(rvalue_tmp);

                    switch (node.Operation)
                    {
                        #region Arithmetic

                        case Operations.AssignAdd:
                            {
                                switch (right_type)
                                {
                                    case PhpTypeCode.Integer:
                                        result = codeGenerator.EmitMethodCall(Methods.Operators.Add.Object_Int32);
                                        break;

                                    case PhpTypeCode.Double:
                                        result = codeGenerator.EmitMethodCall(Methods.Operators.Add.Object_Double);
                                        break;

                                    default:
                                        codeGenerator.EmitBoxing(right_type);
                                        result = codeGenerator.EmitMethodCall(Methods.Operators.Add.Object_Object);
                                        break;
                                }
                                break;
                            }

                        case Operations.AssignSub:
                            {
                                switch (right_type)
                                {
                                    case PhpTypeCode.Integer:
                                        result = codeGenerator.EmitMethodCall(Methods.Operators.Subtract.Object_Int);
                                        break;

                                    default:
                                        codeGenerator.EmitBoxing(right_type);
                                        result = codeGenerator.EmitMethodCall(Methods.Operators.Subtract.Object_Object);
                                        break;
                                }
                                break;
                            }

                        case Operations.AssignDiv:
                            {
                                switch (right_type)
                                {
                                    case PhpTypeCode.Integer:
                                        result = codeGenerator.EmitMethodCall(Methods.Operators.Divide.Object_Int32);
                                        break;

                                    case PhpTypeCode.Double:
                                        result = codeGenerator.EmitMethodCall(Methods.Operators.Divide.Object_Double);
                                        break;

                                    default:
                                        codeGenerator.EmitBoxing(right_type);
                                        result = codeGenerator.EmitMethodCall(Methods.Operators.Divide.Object_Object);
                                        break;
                                }
                                break;
                            }

                        case Operations.AssignMul:
                            {
                                switch (right_type)
                                {
                                    case PhpTypeCode.Integer:
                                        result = codeGenerator.EmitMethodCall(Methods.Operators.Multiply.Object_Int32);
                                        break;

                                    case PhpTypeCode.Double:
                                        result = codeGenerator.EmitMethodCall(Methods.Operators.Multiply.Object_Double);
                                        break;

                                    default:
                                        codeGenerator.EmitBoxing(right_type);
                                        result = codeGenerator.EmitMethodCall(Methods.Operators.Multiply.Object_Object);
                                        break;
                                }
                                break;
                            }

                        case Operations.AssignMod:

                            if (right_type == PhpTypeCode.Integer)
                            {
                                result = codeGenerator.EmitMethodCall(Methods.Operators.Remainder.Object_Int32);
                            }
                            else
                            {
                                codeGenerator.EmitBoxing(right_type);
                                result = codeGenerator.EmitMethodCall(Methods.Operators.Remainder.Object_Object);
                            }
                            break;


                        #endregion

                        #region Bitwise

                        case Operations.AssignAnd:
                            codeGenerator.EmitBoxing(right_type);
                            codeGenerator.IL.Emit(OpCodes.Ldc_I4, (int)Operators.BitOp.And);
                            result = codeGenerator.EmitMethodCall(Methods.Operators.BitOperation);
                            break;

                        case Operations.AssignOr:
                            codeGenerator.EmitBoxing(right_type);
                            codeGenerator.IL.Emit(OpCodes.Ldc_I4, (int)Operators.BitOp.Or);
                            result = codeGenerator.EmitMethodCall(Methods.Operators.BitOperation);
                            break;

                        case Operations.AssignXor:
                            codeGenerator.EmitBoxing(right_type);
                            codeGenerator.IL.Emit(OpCodes.Ldc_I4, (int)Operators.BitOp.Xor);
                            result = codeGenerator.EmitMethodCall(Methods.Operators.BitOperation);
                            break;

                        case Operations.AssignShiftLeft:
                            codeGenerator.EmitBoxing(right_type);
                            result = codeGenerator.EmitMethodCall(Methods.Operators.ShiftLeft);
                            break;

                        case Operations.AssignShiftRight:
                            codeGenerator.EmitBoxing(right_type);
                            result = codeGenerator.EmitMethodCall(Methods.Operators.ShiftRight);
                            break;

                        #endregion

                        #region String

                        case Operations.AssignAppend:
                            {
                                if (right_type == PhpTypeCode.String)
                                {
                                    result = codeGenerator.EmitMethodCall(Methods.Operators.Append.Object_String);
                                }
                                else if (right_type == PhpTypeCode.PhpBytes)
                                {
                                    result = codeGenerator.EmitMethodCall(Methods.PhpBytes.Append_Object_PhpBytes);
                                }
                                else
                                {
                                    codeGenerator.EmitBoxing(right_type);
                                    result = codeGenerator.EmitMethodCall(Methods.Operators.Append.Object_Object);
                                }
                                break;
                            }

                        case Operations.AssignPrepend:
                            {
                                if (right_type == PhpTypeCode.String)
                                {
                                    result = codeGenerator.EmitMethodCall(Methods.Operators.Prepend.Object_String);
                                }
                                else
                                {
                                    codeGenerator.EmitBoxing(right_type);
                                    result = codeGenerator.EmitMethodCall(Methods.Operators.Prepend.Object_Object);
                                }
                                break;
                            }

                        #endregion

                        default:
                            throw new InvalidOperationException();
                    }

                    codeGenerator.IL.EmitBoxing(result);
                }

                switch (access)
                {
                    case AccessType.Read:
                        {
                            // DUP
                            codeGenerator.IL.Emit(OpCodes.Dup);

                            // STORE tmp
                            codeGenerator.IL.Stloc(codeGenerator.IL.GetAssignmentLocal());

                            // STORE prepared, result
                            codeGenerator.AccessSelector = AccessType.Write;
                            result = VariableUseHelper.EmitAssign(node.LValue, codeGenerator);
                            codeGenerator.AccessSelector = AccessType.None;
                            Debug.Assert(result == PhpTypeCode.Void);

                            // LOAD result
                            codeGenerator.IL.Ldloc(codeGenerator.IL.GetAssignmentLocal());

                            result = PhpTypeCode.Object;
                            break;
                        }

                    case AccessType.ReadRef:
                    case AccessType.ReadUnknown:

                        // STORE prepared,result
                        codeGenerator.AccessSelector = AccessType.Write;
                        result = VariableUseHelper.EmitAssign(node.LValue, codeGenerator);
                        codeGenerator.AccessSelector = AccessType.None;
                        Debug.Assert(result == PhpTypeCode.Void);

                        // loads a reference on the LHS variable:
                        codeGenerator.AccessSelector = access;
                        codeGenerator.ChainBuilder.Create();
                        result = node.LValue.Emit(codeGenerator);
                        codeGenerator.ChainBuilder.EndRef();
                        codeGenerator.AccessSelector = AccessType.None;
                        break;

                    case AccessType.None:

                        // STORE a:
                        codeGenerator.AccessSelector = AccessType.Write;
                        result = VariableUseHelper.EmitAssign(node.LValue, codeGenerator);
                        codeGenerator.AccessSelector = AccessType.None;
                        Debug.Assert(result == PhpTypeCode.Void);

                        break;

                    default:
                        Debug.Fail("Invalid access type.");
                        result = PhpTypeCode.Invalid;
                        break;
                }

                codeGenerator.ChainBuilder.End();

                codeGenerator.AccessSelector = old_selector;

                return result;
            }

            private PhpTypeCode EmitDestVarRead(AssignEx/*!*/node, CodeGenerator codeGenerator)
            {
                PhpTypeCode result;

                codeGenerator.AccessSelector = AccessType.Read;
                codeGenerator.ChainBuilder.Create();
                result = node.LValue.Emit(codeGenerator);
                codeGenerator.ChainBuilder.End();
                codeGenerator.AccessSelector = AccessType.None;

                return result;
            }

            /// <summary>
            /// Emits the RHS of assignment.
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="codeGenerator">A code generator.</param>
            /// <returns><see cref="PhpTypeCode"/> of the RHS.</returns>
            private PhpTypeCode EmitSourceValRead(ValueAssignEx/*!*/node, CodeGenerator/*!*/ codeGenerator)
            {
                PhpTypeCode result;

                codeGenerator.ChainBuilder.Create();
                result = node.RValue.Emit(codeGenerator);
                codeGenerator.ChainBuilder.End();

                return result;
            }
        }

        #endregion

        #region RefAssignEx

        [NodeCompiler(typeof(RefAssignEx))]
        sealed class RefAssignExCompiler : AssignExCompiler
        {
            public override Evaluation Analyze(AssignEx node, Analyzer analyzer, ExInfoFromParent info)
            {
                access = info.Access;
                ExInfoFromParent lvalue_info = new ExInfoFromParent(node);
                ExInfoFromParent rvalue_info = new ExInfoFromParent(node);

                lvalue_info.Access = AccessType.WriteRef;
                rvalue_info.Access = AccessType.ReadRef;

                var refassignex = (RefAssignEx)node;

                node.lvalue = (VariableUse)node.LValue.Analyze(analyzer, lvalue_info).Expression;
                refassignex.rvalue = refassignex.RValue.Analyze(analyzer, rvalue_info).Literalize();

                if (refassignex.RValue is NewEx)
                {
                    //PhpException.Throw(PhpError.Deprecated, CoreResources.GetString("assign_new_as_ref_is_deprecated"));
                    analyzer.ErrorSink.Add(Warnings.AssignNewByRefDeprecated, analyzer.SourceUnit, node.Span);
                }

                return new Evaluation(node);
            }

            public override PhpTypeCode Emit(AssignEx node, CodeGenerator codeGenerator)
            {
                Debug.Assert(access == AccessType.None || access == AccessType.Read || access == AccessType.ReadRef
                  || access == AccessType.ReadUnknown);
                Statistics.AST.AddNode("Assign.Ref");

                //ChainBuilder.RefErrorLabelInfo labelInfo;
                
                // Strict Standards: Only variables should be assigned by reference
                /*if (rvalue is FunctionCall)//TODO: only variables (but also variables given by function call return value!)
                {
                    il.LdcI4( (int)PhpError.Strict );
                    il.Emit(OpCodes.Ldstr, CoreResources.GetString("only_vars_assign ed_by_ref"));
                    codeGenerator.EmitPhpException(il,Methods.PhpException.Throw);
                }*/

                // PREPARE:
                codeGenerator.ChainBuilder.Create();
                node.LValue.Emit(codeGenerator);

                // LOAD <right hand side>:
                codeGenerator.ChainBuilder.Create();
                ((RefAssignEx)node).RValue.Emit(codeGenerator);
                codeGenerator.ChainBuilder.End();

                PhpTypeCode result;

                // Dup source value if assignment is read
                switch (access)
                {
                    case AccessType.Read:
                    case AccessType.ReadUnknown:
                    case AccessType.ReadRef:
                        {
                            // DUP
                            codeGenerator.IL.Emit(OpCodes.Dup);

                            // STORE tmp
                            codeGenerator.IL.Stloc(codeGenerator.IL.GetAssignmentLocalRef());

                            // STORE prepared,result
                            VariableUseHelper.EmitAssign(node.LValue, codeGenerator);

                            // LOAD DEREF tmp
                            codeGenerator.IL.Ldloc(codeGenerator.IL.GetAssignmentLocalRef());

                            if (access == AccessType.Read)
                            {
                                codeGenerator.IL.Emit(OpCodes.Ldfld, Fields.PhpReference_Value);
                                result = PhpTypeCode.Object;
                            }
                            else
                            {
                                result = PhpTypeCode.PhpReference;
                            }
                            break;
                        }

                    case AccessType.None:
                        VariableUseHelper.EmitAssign(node.LValue, codeGenerator);
                        result = PhpTypeCode.Void;
                        break;

                    default:
                        throw new InvalidOperationException();
                        //result = PhpTypeCode.Invalid;
                        //break;
                }
                codeGenerator.ChainBuilder.End();

                return result;
            }
        }

        #endregion
    }
}
