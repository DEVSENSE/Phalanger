/*

 Copyright (c) 2004-2006 Tomas Matousek, Vaclav Novak and Martin Maly.

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
using PHP.Core.Reflection;

namespace PHP.Core.Compiler.AST
{
    partial class NodeCompilers
    {
        [NodeCompiler(typeof(BinaryEx))]
        sealed class BinaryExCompiler : ExpressionCompiler<BinaryEx>
        {
            #region Analysis

            public override Evaluation EvaluatePriorAnalysis(BinaryEx node, CompilationSourceUnit sourceUnit)
            {
                Evaluation left_eval = node.LeftExpr.EvaluatePriorAnalysis(sourceUnit);
                Evaluation right_eval = node.RightExpr.EvaluatePriorAnalysis(sourceUnit);

                return Evaluation.ReadOnlyEvaluate(node, left_eval, right_eval);
            }

            public override Evaluation Analyze(BinaryEx node, Analyzer analyzer, ExInfoFromParent info)
            {
                access = info.Access;
                ExInfoFromParent operand_info = ExInfoFromParent.DefaultExInfo;

                Evaluation left_eval = node.LeftExpr.Analyze(analyzer, operand_info);
                Evaluation right_eval;

                // Boolean expression evaluation semantics:
                if (node.Operation == Operations.Or)
                {
                    analyzer.EnterConditionalCode();
                    right_eval = node.RightExpr.Analyze(analyzer, operand_info);
                    analyzer.LeaveConditionalCode();
                }
                else
                {
                    right_eval = node.RightExpr.Analyze(analyzer, operand_info);
                }

                Expression rightTmp, leftTmp;
                Evaluation result = Evaluation.Evaluate(node, left_eval, out leftTmp, right_eval, out rightTmp);
                node.LeftExpr = leftTmp;
                node.RightExpr = rightTmp;

                // division by zero check:
                if ((node.Operation == Operations.Div || node.Operation == Operations.Mod) && result.HasValue && result.Value is bool && (bool)result.Value == false)
                {
                    analyzer.ErrorSink.Add(Warnings.DivisionByZero, analyzer.SourceUnit, node.RightExpr.Span);
                }
                else if ((node.Operation == Operations.Div || node.Operation == Operations.Mod) && right_eval.HasValue && right_eval.Value is int && (int)right_eval.Value == 0)
                {
                    result = new Evaluation(node, false);
                    analyzer.ErrorSink.Add(Warnings.DivisionByZero, analyzer.SourceUnit, node.RightExpr.Span);
                }

                return result;
            }

            public override object Evaluate(BinaryEx node, object leftValue, object rightValue)
            {
                switch (node.Operation)
                {
                    case Operations.Xor:
                        return Convert.ObjectToBoolean(leftValue) ^ Convert.ObjectToBoolean(rightValue);

                    case Operations.Or:
                        return Convert.ObjectToBoolean(leftValue) || Convert.ObjectToBoolean(rightValue);

                    case Operations.And:
                        return Convert.ObjectToBoolean(leftValue) && Convert.ObjectToBoolean(rightValue);

                    case Operations.BitOr:
                        return Operators.BitOperation(leftValue, rightValue, Operators.BitOp.Or);

                    case Operations.BitXor:
                        return Operators.BitOperation(leftValue, rightValue, Operators.BitOp.Xor);

                    case Operations.BitAnd:
                        return Operators.BitOperation(leftValue, rightValue, Operators.BitOp.And);

                    case Operations.Equal:
                        return PhpComparer./*Default.*/CompareEq(leftValue, rightValue);

                    case Operations.NotEqual:
                        return !PhpComparer./*Default.*/CompareEq(leftValue, rightValue);

                    case Operations.Identical:
                        return Operators.StrictEquality(leftValue, rightValue);

                    case Operations.NotIdentical:
                        return !Operators.StrictEquality(leftValue, rightValue);

                    case Operations.LessThan:
                        return PhpComparer.Default.Compare(leftValue, rightValue) < 0;

                    case Operations.GreaterThan:
                        return PhpComparer.Default.Compare(leftValue, rightValue) > 0;

                    case Operations.LessThanOrEqual:
                        return PhpComparer.Default.Compare(leftValue, rightValue) <= 0;

                    case Operations.GreaterThanOrEqual:
                        return PhpComparer.Default.Compare(leftValue, rightValue) >= 0;

                    case Operations.ShiftRight:
                        return Operators.ShiftRight(leftValue, rightValue); // int

                    case Operations.ShiftLeft:
                        return Operators.ShiftLeft(leftValue, rightValue);  // int

                    case Operations.Add:
                        return Operators.Add(leftValue, rightValue);

                    case Operations.Sub:
                        return Operators.Subtract(leftValue, rightValue);

                    case Operations.Mul:
                        return Operators.Multiply(leftValue, rightValue);

                    case Operations.Div:
                        return Operators.Divide(leftValue, rightValue);

                    case Operations.Mod:
                        return Operators.Remainder(leftValue, rightValue);

                    case Operations.Concat:
                        return Operators.Concat(leftValue, rightValue);

                    default:
                        throw null;
                }
            }

            #endregion

            #region Code emission

            /// <summary>
            /// Whether the result of binary expression should be deeply copied.
            /// </summary>
            /// <include file='Doc/Nodes.xml' path='doc/method[@name="IsDeeplyCopied"]/*'/>
            /// <param name="node">Instance.</param>
            /// <returns>All operators returns immutable values. Hence, returns <B>false</B>.</returns>
            public override bool IsDeeplyCopied(BinaryEx node, CopyReason reason, int nestingLevel)
            {
                switch (node.Operation)
                {
                    // respective operators returns immutable values:
                    case Operations.Xor:
                    case Operations.Or:
                    case Operations.And:
                    case Operations.BitOr:
                    case Operations.BitXor:
                    case Operations.BitAnd:
                    case Operations.Equal:
                    case Operations.NotEqual:
                    case Operations.Identical:
                    case Operations.NotIdentical:
                    case Operations.LessThan:
                    case Operations.GreaterThan:
                    case Operations.LessThanOrEqual:
                    case Operations.GreaterThanOrEqual:
                    case Operations.ShiftLeft:
                    case Operations.ShiftRight:
                    case Operations.Add:
                    case Operations.Sub:
                    case Operations.Mul:
                    case Operations.Div:
                    case Operations.Mod:
                    case Operations.Concat:
                        return false;
                }
                Debug.Fail("Illegal operation type.");
                return true;
            }

            /// <remarks>
            /// Nothing is expected at the evaluation stack. If AST node is read by other node,
            /// the operation result is left at the stack, otherwise it is poped from the stack.
            /// </remarks>
            public override PhpTypeCode Emit(BinaryEx node, CodeGenerator codeGenerator)
            {
                Debug.Assert(access == AccessType.None || access == AccessType.Read);
                Statistics.AST.AddNode("BinaryEx");

                PhpTypeCode returned_typecode;
                PhpTypeCode lo_typecode;
                PhpTypeCode ro_typecode;

                switch (node.Operation)
                {
                    #region Arithmetic Operations

                    case Operations.Add:
                        // Template: x + y : Operators.Add(x,y) [overloads]

                        switch (lo_typecode = node.LeftExpr.Emit(codeGenerator))
                        {
                            case PhpTypeCode.Double:
                                switch (ro_typecode = node.RightExpr.Emit(codeGenerator))
                                {
                                    case PhpTypeCode.Integer:
                                        codeGenerator.IL.Emit(OpCodes.Conv_R8);
                                        goto case PhpTypeCode.Double;   // fallback:
                                    case PhpTypeCode.Double:
                                        codeGenerator.IL.Emit(OpCodes.Add);
                                        returned_typecode = PhpTypeCode.Double;
                                        break;
                                    default:
                                        codeGenerator.EmitBoxing(ro_typecode);
                                        returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Add.Double_Object);
                                        break;
                                }

                                break;
                            default:
                                codeGenerator.EmitBoxing(lo_typecode);
                                ro_typecode = node.RightExpr.Emit(codeGenerator);

                                switch (ro_typecode)
                                {
                                    case PhpTypeCode.Integer:
                                        returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Add.Object_Int32);
                                        break;

                                    case PhpTypeCode.Double:
                                        returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Add.Object_Double);
                                        break;

                                    default:
                                        codeGenerator.EmitBoxing(ro_typecode);
                                        returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Add.Object_Object);
                                        break;
                                }
                                break;
                        }
                        break;

                    case Operations.Sub:
                        //Template: "x - y"        Operators.Subtract(x,y) [overloads]
                        lo_typecode = node.LeftExpr.Emit(codeGenerator);
                        switch (lo_typecode)
                        {
                            case PhpTypeCode.Integer:
                                codeGenerator.EmitBoxing(node.RightExpr.Emit(codeGenerator));
                                returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Subtract.Int32_Object);
                                break;
                            case PhpTypeCode.Double:
                                switch (ro_typecode = node.RightExpr.Emit(codeGenerator))
                                {
                                    case PhpTypeCode.Integer:
                                        codeGenerator.IL.Emit(OpCodes.Conv_R8);
                                        goto case PhpTypeCode.Double;   // fallback:
                                    case PhpTypeCode.Double:
                                        codeGenerator.IL.Emit(OpCodes.Sub);
                                        returned_typecode = PhpTypeCode.Double;
                                        break;
                                    default:
                                        codeGenerator.EmitBoxing(ro_typecode);
                                        returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Subtract.Double_Object);
                                        break;
                                }

                                break;
                            default:
                                codeGenerator.EmitBoxing(lo_typecode);
                                ro_typecode = node.RightExpr.Emit(codeGenerator);
                                if (ro_typecode == PhpTypeCode.Integer)
                                {
                                    returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Subtract.Object_Int);
                                }
                                else
                                {
                                    codeGenerator.EmitBoxing(ro_typecode);
                                    returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Subtract.Object_Object);
                                }
                                break;
                        }
                        break;

                    case Operations.Div:
                        //Template: "x / y"   Operators.Divide(x,y)

                        lo_typecode = node.LeftExpr.Emit(codeGenerator);
                        switch (lo_typecode)
                        {
                            case PhpTypeCode.Integer:
                                codeGenerator.EmitBoxing(node.RightExpr.Emit(codeGenerator));
                                returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Divide.Int32_Object);
                                break;

                            case PhpTypeCode.Double:
                                switch (ro_typecode = node.RightExpr.Emit(codeGenerator))
                                {
                                    case PhpTypeCode.Double:
                                        codeGenerator.IL.Emit(OpCodes.Div);
                                        returned_typecode = PhpTypeCode.Double;
                                        break;
                                    default:
                                        codeGenerator.EmitBoxing(ro_typecode);
                                        returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Divide.Double_Object);
                                        break;
                                }
                                break;

                            default:
                                codeGenerator.EmitBoxing(lo_typecode);
                                ro_typecode = node.RightExpr.Emit(codeGenerator);

                                switch (ro_typecode)
                                {
                                    case PhpTypeCode.Integer:
                                        returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Divide.Object_Int32);
                                        break;

                                    case PhpTypeCode.Double:
                                        returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Divide.Object_Double);
                                        break;

                                    default:
                                        codeGenerator.EmitBoxing(ro_typecode);
                                        returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Divide.Object_Object);
                                        break;
                                }
                                break;
                        }
                        break;

                    case Operations.Mul:
                        switch (lo_typecode = node.LeftExpr.Emit(codeGenerator))
                        {
                            case PhpTypeCode.Double:
                                // "x * (double)y"
                                // Operators.Multiply((double)x,(object)y)

                                switch (ro_typecode = node.RightExpr.Emit(codeGenerator))
                                {
                                    case PhpTypeCode.Integer:
                                        codeGenerator.IL.Emit(OpCodes.Conv_R8);
                                        goto case PhpTypeCode.Double;   // fallback:
                                    case PhpTypeCode.Double:
                                        codeGenerator.IL.Emit(OpCodes.Mul);
                                        returned_typecode = PhpTypeCode.Double;
                                        break;
                                    default:
                                        codeGenerator.EmitBoxing(ro_typecode);
                                        returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Multiply.Double_Object);
                                        break;
                                }

                                break;
                            default:
                                //Template: "x * y"  Operators.Multiply((object)x,y) [overloads]
                                codeGenerator.EmitBoxing(lo_typecode);

                                ro_typecode = node.RightExpr.Emit(codeGenerator);
                                switch (ro_typecode)
                                {
                                    case PhpTypeCode.Integer:
                                        returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Multiply.Object_Int32);
                                        break;

                                    case PhpTypeCode.Double:
                                        returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Multiply.Object_Double);
                                        break;

                                    default:
                                        codeGenerator.EmitBoxing(ro_typecode);
                                        returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Multiply.Object_Object);
                                        break;
                                }
                                break;
                        }
                        break;

                    case Operations.Mod:
                        //Template: "x % y"        Operators.Remainder(x,y)
                        codeGenerator.EmitBoxing(node.LeftExpr.Emit(codeGenerator));
                        ro_typecode = node.RightExpr.Emit(codeGenerator);
                        switch (ro_typecode)
                        {
                            case PhpTypeCode.Integer:
                                returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Remainder.Object_Int32);
                                break;

                            default:
                                codeGenerator.EmitBoxing(ro_typecode);
                                returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Remainder.Object_Object);
                                break;
                        }
                        break;

                    case Operations.ShiftLeft:

                        // LOAD Operators.ShiftLeft(box left, box right);
                        codeGenerator.EmitBoxing(node.LeftExpr.Emit(codeGenerator));
                        codeGenerator.EmitBoxing(node.RightExpr.Emit(codeGenerator));
                        returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.ShiftLeft);
                        break;

                    case Operations.ShiftRight:

                        // LOAD Operators.ShiftRight(box left, box right);
                        codeGenerator.EmitBoxing(node.LeftExpr.Emit(codeGenerator));
                        codeGenerator.EmitBoxing(node.RightExpr.Emit(codeGenerator));
                        returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.ShiftRight);
                        break;

                    #endregion

                    #region Boolean and Bitwise Operations

                    case Operations.And:
                        returned_typecode = EmitBinaryBooleanOperation(node, codeGenerator, true);
                        break;

                    case Operations.Or:
                        returned_typecode = EmitBinaryBooleanOperation(node, codeGenerator, false);
                        break;

                    case Operations.Xor:

                        // LOAD <(bool) leftSon> == <(bool) rightSon>;
                        codeGenerator.EmitConversion(node.LeftExpr, PhpTypeCode.Boolean);
                        codeGenerator.EmitConversion(node.RightExpr, PhpTypeCode.Boolean);
                        codeGenerator.IL.Emit(OpCodes.Ceq);

                        codeGenerator.IL.Emit(OpCodes.Ldc_I4_0);
                        codeGenerator.IL.Emit(OpCodes.Ceq);

                        returned_typecode = PhpTypeCode.Boolean;
                        break;

                    case Operations.BitAnd:
                        returned_typecode = EmitBitOperation(node, codeGenerator, Operators.BitOp.And);
                        break;

                    case Operations.BitOr:
                        returned_typecode = EmitBitOperation(node, codeGenerator, Operators.BitOp.Or);
                        break;

                    case Operations.BitXor:
                        returned_typecode = EmitBitOperation(node, codeGenerator, Operators.BitOp.Xor);
                        break;

                    #endregion

                    #region Comparing Operations

                    case Operations.Equal:

                        // LOAD PhpComparer.Default.CompareEq
                        returned_typecode = EmitComparison(node, codeGenerator, true);
                        break;

                    case Operations.NotEqual:

                        // LOAD PhpComparer.Default.CompareEq == false
                        EmitComparison(node, codeGenerator, true);
                        codeGenerator.IL.Emit(OpCodes.Ldc_I4_0);
                        codeGenerator.IL.Emit(OpCodes.Ceq);

                        returned_typecode = PhpTypeCode.Boolean;
                        break;

                    case Operations.GreaterThan:

                        // LOAD PhpComparer.Default.Compare > 0;
                        EmitComparison(node, codeGenerator, false);
                        codeGenerator.IL.Emit(OpCodes.Ldc_I4_0);
                        codeGenerator.IL.Emit(OpCodes.Cgt);

                        returned_typecode = PhpTypeCode.Boolean;
                        break;

                    case Operations.LessThan:

                        // LOAD PhpComparer.Default.Compare < 0;
                        EmitComparison(node, codeGenerator, false);
                        codeGenerator.IL.Emit(OpCodes.Ldc_I4_0);
                        codeGenerator.IL.Emit(OpCodes.Clt);

                        returned_typecode = PhpTypeCode.Boolean;
                        break;

                    case Operations.GreaterThanOrEqual:

                        // LOAD PhpComparer.Default.Compare >= 0 (not less than)
                        EmitComparison(node, codeGenerator, false);
                        codeGenerator.IL.Emit(OpCodes.Ldc_I4_0);
                        codeGenerator.IL.Emit(OpCodes.Clt);
                        codeGenerator.IL.Emit(OpCodes.Ldc_I4_0);
                        codeGenerator.IL.Emit(OpCodes.Ceq);

                        returned_typecode = PhpTypeCode.Boolean;
                        break;

                    case Operations.LessThanOrEqual:

                        // LOAD PhpComparer.Default.Compare >= 0 (not greater than)
                        EmitComparison(node, codeGenerator, false);
                        codeGenerator.IL.Emit(OpCodes.Ldc_I4_0);
                        codeGenerator.IL.Emit(OpCodes.Cgt);
                        codeGenerator.IL.Emit(OpCodes.Ldc_I4_0);
                        codeGenerator.IL.Emit(OpCodes.Ceq);

                        returned_typecode = PhpTypeCode.Boolean;
                        break;

                    case Operations.Identical:

                        // LOAD Operators.StrictEquality(box left,box right);
                        returned_typecode = EmitStrictEquality(node, codeGenerator);
                        break;

                    case Operations.NotIdentical:

                        // LOAD Operators.StrictEquality(box left,box right) == false;
                        EmitStrictEquality(node, codeGenerator);

                        codeGenerator.IL.Emit(OpCodes.Ldc_I4_0);
                        codeGenerator.IL.Emit(OpCodes.Ceq);

                        returned_typecode = PhpTypeCode.Boolean;
                        break;

                    #endregion

                    case Operations.Concat:
                        returned_typecode = ConcatExCompiler.EmitConcat(codeGenerator, node.LeftExpr, node.RightExpr);
                        break;

                    default:
                        throw null;
                }

                switch (access)
                {
                    case AccessType.Read:
                        // Result is read, do nothing.
                        break;

                    case AccessType.None:
                        // Result is not read, pop the result
                        codeGenerator.IL.Emit(OpCodes.Pop);
                        returned_typecode = PhpTypeCode.Void;
                        break;
                }

                return returned_typecode;
            }

            /// <summary>
            /// Emits bit operation <see cref="BinaryEx.LeftExpr"/> OP <see cref="BinaryEx.RightExpr"/>.
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="codeGenerator">A code generator.</param>
            /// <param name="op">The operation.</param>
            /// <returns>A type code of the result.</returns>
            private static PhpTypeCode EmitBitOperation(BinaryEx/*!*/node, CodeGenerator/*!*/ codeGenerator, Operators.BitOp op)
            {
                ILEmitter il = codeGenerator.IL;

                // LOAD Operators.BitOperation(box <leftSon>, box <rightSon>);
                codeGenerator.EmitBoxing(node.LeftExpr.Emit(codeGenerator));
                codeGenerator.EmitBoxing(node.RightExpr.Emit(codeGenerator));
                il.Emit(OpCodes.Ldc_I4, (int)op);
                il.Emit(OpCodes.Call, Methods.Operators.BitOperation);

                return PhpTypeCode.Object;
            }

            /// <summary>
            /// Emits binary boolean operation (AND or OR).
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="codeGenerator">A code generator.</param>
            /// <param name="isAnd">Whether to emit AND.</param>
            /// <returns>A type code of the result.</returns>
            private static PhpTypeCode EmitBinaryBooleanOperation(BinaryEx/*!*/node, CodeGenerator codeGenerator, bool isAnd)
            {
                ILEmitter il = codeGenerator.IL;
                Label partial_eval_label = il.DefineLabel();
                Label end_label = il.DefineLabel();

                // IF [!]<(bool) leftSon> THEN GOTO partial_eval;
                codeGenerator.EmitConversion(node.LeftExpr, PhpTypeCode.Boolean);
                il.Emit(isAnd ? OpCodes.Brfalse : OpCodes.Brtrue, partial_eval_label);

                // LOAD <(bool) leftSon>;
                codeGenerator.EmitConversion(node.RightExpr, PhpTypeCode.Boolean);

                il.Emit(OpCodes.Br, end_label);
                il.MarkLabel(partial_eval_label, true);
                il.LdcI4(isAnd ? 0 : 1);
                il.MarkLabel(end_label, true);

                return PhpTypeCode.Boolean;
            }

            /// <summary>
            /// Emits call to a default comparator method.
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="codeGenerator">A code generator.</param>
            /// <param name="equality">Whether to emit equality comparison (or generic comparison otherwise).</param>
            /// <returns>A type code of the result.</returns>
            private static PhpTypeCode EmitComparison(BinaryEx/*!*/node, CodeGenerator codeGenerator, bool equality)
            {
                PhpTypeCode x, y;
                // PhpComparer.Default.<CompareEq | Compare>(box left, box right <|, false>);
                /*changed to static method*/
                //codeGenerator.IL.Emit(OpCodes.Ldsfld, Fields.PhpComparer_Default);

                if (equality)
                {
                    return codeGenerator.EmitCompareEq(cg => node.LeftExpr.Emit(cg), cg => node.RightExpr.Emit(cg));
                }
                else
                {
                    x = node.LeftExpr.Emit(codeGenerator);

                    if (x == PhpTypeCode.Integer)
                    {
                        y = node.RightExpr.Emit(codeGenerator);

                        // int, ?

                        if (y == PhpTypeCode.Integer)
                        {
                            // int, int
                            codeGenerator.IL.Emit(OpCodes.Call, Methods.CompareOp_int_int);
                            return PhpTypeCode.Integer;
                        }
                        else
                        {
                            codeGenerator.EmitBoxing(y);

                            // int, object
                            codeGenerator.IL.LdcI4(0);  // throws = false
                            codeGenerator.IL.Emit(OpCodes.Call, Methods.CompareOp_int_object_bool);
                            return PhpTypeCode.Integer;
                        }

                    }
                    else
                    {
                        codeGenerator.EmitBoxing(x);

                        y = node.RightExpr.Emit(codeGenerator);

                        // object, ?

                        if (y == PhpTypeCode.Integer)
                        {
                            // object, int
                            codeGenerator.IL.LdcI4(0);  // throws = false
                            codeGenerator.IL.Emit(OpCodes.Call, Methods.CompareOp_object_int_bool);
                            return PhpTypeCode.Integer;
                        }
                        else
                        {
                            codeGenerator.EmitBoxing(y);

                            // object, object
                            codeGenerator.IL.LdcI4(0);  // throws = false
                            codeGenerator.IL.Emit(OpCodes.Call, Methods.CompareOp_object_object_bool);
                            return PhpTypeCode.Integer;
                        }
                    }
                }
            }

            /// <summary>
            /// Emits strict equality operation.
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="codeGenerator">A code generator.</param>
            /// <returns>A type code of the result (boolean).</returns>
            private static PhpTypeCode EmitStrictEquality(BinaryEx/*!*/node, CodeGenerator codeGenerator)
            {
                if (IsEmptyArrayEx(node.LeftExpr))
                {
                    EmitEmptyArrayStrictEquality(codeGenerator, node.RightExpr);
                }
                else if (IsEmptyArrayEx(node.RightExpr))
                {
                    EmitEmptyArrayStrictEquality(codeGenerator, node.LeftExpr);
                }
                else
                {
                    // LOAD Operators.StrictEquality(box left,box right);
                    codeGenerator.EmitBoxing(node.LeftExpr.Emit(codeGenerator));
                    codeGenerator.EmitBoxing(node.RightExpr.Emit(codeGenerator));
                    codeGenerator.IL.Emit(OpCodes.Call, Methods.Operators.StrictEquality);
                }

                return PhpTypeCode.Boolean;
            }

            /// <summary>
            /// Emits strict equality to empty PHP array.
            /// </summary>
            /// <param name="codeGenerator">A code generator.</param>
            /// <param name="expr">Expression to be compared against.</param>
            private static void EmitEmptyArrayStrictEquality(CodeGenerator/*!*/codeGenerator, Expression/*!*/expr)
            {
                if (IsEmptyArrayEx(expr))
                {
                    // array() === array()
                    // LOAD true
                    codeGenerator.IL.LoadBool(true);
                }
                else if (expr is Literal)
                {
                    // array() === NULL|int|double|string|...
                    // LOAD false
                    codeGenerator.IL.LoadBool(false);
                }
                else
                {
                    // array() === <expr>

                    // LOAD <expr>
                    var exprTypeCode = expr.Emit(codeGenerator);

                    // check whether <expr> type can be an array
                    switch (exprTypeCode)
                    {
                        case PhpTypeCode.Boolean:
                        case PhpTypeCode.DObject:
                        case PhpTypeCode.Double:
                        case PhpTypeCode.Integer:
                        case PhpTypeCode.LongInteger:
                        case PhpTypeCode.PhpBytes:
                        case PhpTypeCode.PhpString:
                        case PhpTypeCode.String:
                            // always FALSE
                            codeGenerator.IL.Emit(OpCodes.Pop);
                            codeGenerator.IL.LoadBool(false);
                            break;
                        case PhpTypeCode.PhpArray:
                            // compare (PhpArray)<expr> with array()
                            codeGenerator.IL.Emit(OpCodes.Call, Methods.Operators.StrictEmptyPhpArrayEquality_PhpArray);
                            break;
                        default:
                            // compare <expr> with array()
                            codeGenerator.EmitBoxing(exprTypeCode);
                            codeGenerator.IL.Emit(OpCodes.Call, Methods.Operators.StrictEmptyPhpArrayEquality);
                            break;
                    }
                }
            }

            /// <summary>
            /// Determines whether given <paramref name="expr"/> represents an empty array (<c>array()</c> or <c>[]</c>).
            /// </summary>
            /// <param name="expr">Expression to be checked.</param>
            /// <returns>True if <paramref name="expr"/> is an empty array expression.</returns>
            private static bool IsEmptyArrayEx(Expression/*!*/expr)
            {
                Debug.Assert(expr != null);
                return expr.GetType() == typeof(ArrayEx) && ((ArrayEx)expr).Items.Empty();
            }

            #endregion
        }
    }
}