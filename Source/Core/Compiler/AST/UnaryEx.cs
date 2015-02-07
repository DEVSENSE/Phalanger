/*

 Copyright (c) 20013 DEVSENSE

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

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
        [NodeCompiler(typeof(UnaryEx))]
        sealed class UnaryExCompiler : ExpressionCompiler<UnaryEx>
        {
            #region Analysis

            public override Evaluation EvaluatePriorAnalysis(UnaryEx node, CompilationSourceUnit sourceUnit)
            {
                return node.Expr.EvaluatePriorAnalysis(sourceUnit).ReadOnlyEvaluate(node);
            }

            public override Evaluation Analyze(UnaryEx node, Analyzer analyzer, ExInfoFromParent info)
            {
                access = info.Access;

                Evaluation result;

                switch (node.Operation)
                {
                    case Operations.Print:
                    case Operations.Clone:
                    case Operations.ObjectCast:
                    case Operations.ArrayCast:
                        node.Expr = node.Expr.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();
                        result = new Evaluation(node);
                        break;

                    default:
                        Expression tmp;
                        result = node.Expr.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Evaluate(node, out tmp);
                        node.Expr = tmp;
                        break;
                }

                return result;
            }

            public override object Evaluate(UnaryEx node, object value)
            {
                switch (node.Operation)
                {
                    case Operations.Plus:
                        return Operators.Plus(value);

                    case Operations.Minus:
                        return Operators.Minus(value);

                    case Operations.LogicNegation:
                        return !Convert.ObjectToBoolean(value);

                    case Operations.BitNegation:
                        return Operators.BitNot(value); // allows to return PhpBytes

                    case Operations.FloatCast:
                    case Operations.DoubleCast:
                        return Convert.ObjectToDouble(value);

                    case Operations.StringCast:
                    case Operations.UnicodeCast: // TODO
                        return Convert.ObjectToString(value);

                    case Operations.BinaryCast:
                        return Convert.ObjectToPhpBytes(value);

                    case Operations.BoolCast:
                        return Convert.ObjectToBoolean(value);

                    case Operations.AtSign:
                        return (value is bool) ? (bool)value : value;

                    case Operations.UnsetCast:
                        return null;

                    case Operations.Int32Cast:
                        return Convert.ObjectToInteger(value);

                    case Operations.Int8Cast:
                    case Operations.Int16Cast:
                    case Operations.Int64Cast:
                    case Operations.UInt8Cast:
                    case Operations.UInt16Cast:
                    case Operations.UInt32Cast:
                    case Operations.UInt64Cast:
                    case Operations.DecimalCast:
                        Debug.Fail("not supported yet");
                        throw null;

                    default:
                        Debug.Fail("Operation " + node.Operation + " shouldn't be evaluated");
                        throw null;
                }
            }

            #endregion

            public override bool IsDeeplyCopied(UnaryEx node, CopyReason reason, int nestingLevel)
            {
                switch (node.Operation)
                {
                    // respective operators returns immutable values:
                    case Operations.Plus:
                    case Operations.Minus:
                    case Operations.LogicNegation:
                    case Operations.BitNegation:

                    case Operations.Int8Cast:
                    case Operations.Int16Cast:
                    case Operations.Int32Cast:
                    case Operations.Int64Cast:
                    case Operations.UInt8Cast:
                    case Operations.UInt16Cast:
                    case Operations.UInt32Cast:
                    case Operations.UInt64Cast:
                    case Operations.DecimalCast:
                    case Operations.DoubleCast:
                    case Operations.FloatCast:
                    case Operations.StringCast:
                    case Operations.UnicodeCast:
                    case Operations.BoolCast:
                    case Operations.UnsetCast:

                    case Operations.Clone:
                    case Operations.Print:
                        return false;

                    // respective operators don't do a deep copy:
                    case Operations.ObjectCast:
                    case Operations.ArrayCast:
                    case Operations.BinaryCast:
                        return true;

                    // the result depends on what follows @:
                    case Operations.AtSign:
                        return node.Expr.IsDeeplyCopied(reason, nestingLevel);

                    default:
                        throw null;
                }
            }

            public override PhpTypeCode Emit(UnaryEx node, CodeGenerator codeGenerator)
            {
                Debug.Assert(access == AccessType.Read || access == AccessType.None);
                Statistics.AST.AddNode("UnaryEx");

                ILEmitter il = codeGenerator.IL;

                PhpTypeCode returned_typecode, o_typecode;

                switch (node.Operation)
                {
                    case Operations.AtSign:	// special arrangement
                        // Template:
                        //		context.DisableErrorReporting();
                        //		s;
                        //		context.EnableErrorReporting();
                        codeGenerator.EmitLoadScriptContext();
                        il.Emit(OpCodes.Call, Methods.ScriptContext.DisableErrorReporting);
                        returned_typecode = node.Expr.Emit(codeGenerator);
                        codeGenerator.EmitLoadScriptContext();
                        il.Emit(OpCodes.Call, Methods.ScriptContext.EnableErrorReporting);
                        break;

                    case Operations.BitNegation:
                        //Template: "~x" Operators.BitNot(x)                                     
                        codeGenerator.EmitBoxing(node.Expr.Emit(codeGenerator));
                        il.Emit(OpCodes.Call, Methods.Operators.BitNot);
                        returned_typecode = PhpTypeCode.Object;
                        break;

                    case Operations.Clone:
                        // Template: clone x        Operators.Clone(x,DTypeDesc,ScriptContext)
                        codeGenerator.EmitBoxing(node.Expr.Emit(codeGenerator));
                        codeGenerator.EmitLoadClassContext();
                        codeGenerator.EmitLoadScriptContext();
                        il.Emit(OpCodes.Call, Methods.Operators.Clone);
                        returned_typecode = PhpTypeCode.Object;
                        break;

                    case Operations.LogicNegation:
                        //Template: "!x"  !Convert.ObjectToBoolean(x);                              
                        codeGenerator.EmitObjectToBoolean(node.Expr, true);
                        returned_typecode = PhpTypeCode.Boolean;
                        break;

                    case Operations.Minus:
                        //Template: "-x"  Operators.Minus(x)
                        switch (o_typecode = node.Expr.Emit(codeGenerator))
                        {
                            case PhpTypeCode.Double:
                                il.Emit(OpCodes.Neg);
                                returned_typecode = PhpTypeCode.Double;
                                break;
                            default:
                                codeGenerator.EmitBoxing(o_typecode);
                                returned_typecode = codeGenerator.EmitMethodCall(Methods.Operators.Minus);
                                break;
                        }
                        break;

                    case Operations.ObjectCast:
                        //Template: "(object)x"   Convert.ObjectToDObject(x,ScriptContext)
                        codeGenerator.EmitBoxing(node.Expr.Emit(codeGenerator));
                        codeGenerator.EmitLoadScriptContext();
                        il.Emit(OpCodes.Call, Methods.Convert.ObjectToDObject);
                        returned_typecode = PhpTypeCode.Object;
                        break;

                    case Operations.Plus:
                        //Template: "+x"  Operators.Plus(x)
                        codeGenerator.EmitBoxing(node.Expr.Emit(codeGenerator));
                        il.Emit(OpCodes.Call, Methods.Operators.Plus);
                        returned_typecode = PhpTypeCode.Object;
                        break;

                    case Operations.Print:
                        codeGenerator.EmitEcho(node.Expr);
                        // Always returns 1
                        il.Emit(OpCodes.Ldc_I4_1);
                        returned_typecode = PhpTypeCode.Integer;
                        break;

                    case Operations.BoolCast:
                        //Template: "(bool)x"     Convert.ObjectToBoolean(x)
                        codeGenerator.EmitObjectToBoolean(node.Expr, false);
                        returned_typecode = PhpTypeCode.Boolean;
                        break;

                    case Operations.Int8Cast:
                    case Operations.Int16Cast:
                    case Operations.Int32Cast:
                    case Operations.UInt8Cast:
                    case Operations.UInt16Cast:
                        // CALL int Convert.ObjectToInteger(<node.Expr>)
                        o_typecode = node.Expr.Emit(codeGenerator);
                        if (o_typecode != PhpTypeCode.Integer)
                        {
                            codeGenerator.EmitBoxing(o_typecode);
                            il.Emit(OpCodes.Call, Methods.Convert.ObjectToInteger);
                        }

                        // CONV for unsigned:
                        switch (node.Operation)
                        {
                            case Operations.UInt8Cast: il.Emit(OpCodes.Conv_U1); il.Emit(OpCodes.Conv_I4); break;
                            case Operations.UInt16Cast: il.Emit(OpCodes.Conv_U2); il.Emit(OpCodes.Conv_I4); break;
                        }

                        returned_typecode = PhpTypeCode.Integer;
                        break;

                    case Operations.UInt64Cast:
                    case Operations.UInt32Cast:
                    case Operations.Int64Cast:
                        // CALL long Convert.ObjectToLongInteger(<node.Expr>)
                        o_typecode = node.Expr.Emit(codeGenerator);
                        if (o_typecode != PhpTypeCode.LongInteger)
                        {
                            codeGenerator.EmitBoxing(o_typecode);
                            il.Emit(OpCodes.Call, Methods.Convert.ObjectToLongInteger);
                        }

                        // CONV for unsigned:
                        switch (node.Operation)
                        {
                            case Operations.UInt32Cast: il.Emit(OpCodes.Conv_U4); il.Emit(OpCodes.Conv_I8); break;
                            case Operations.UInt64Cast: il.Emit(OpCodes.Conv_U8); il.Emit(OpCodes.Conv_I8); break;
                        }

                        returned_typecode = PhpTypeCode.LongInteger;
                        break;

                    case Operations.DecimalCast:
                    case Operations.DoubleCast:
                    case Operations.FloatCast:
                        // CALL double Convert.ObjectToDouble(<node.Expr>)
                        o_typecode = node.Expr.Emit(codeGenerator);
                        if (o_typecode != PhpTypeCode.Double)
                        {
                            codeGenerator.EmitBoxing(o_typecode);
                            il.Emit(OpCodes.Call, Methods.Convert.ObjectToDouble);
                        }
                        returned_typecode = PhpTypeCode.Double;
                        break;

                    case Operations.UnicodeCast: // TODO
                    case Operations.StringCast:
                        if ((returned_typecode = node.Expr.Emit(codeGenerator)) != PhpTypeCode.String)
                        {
                            codeGenerator.EmitBoxing(returned_typecode);
                            //codeGenerator.EmitLoadClassContext();
                            il.Emit(OpCodes.Call, Methods.Convert.ObjectToString);
                            returned_typecode = PhpTypeCode.String;
                        }
                        break;

                    case Operations.BinaryCast:
                        if ((returned_typecode = node.Expr.Emit(codeGenerator)) != PhpTypeCode.PhpBytes)
                        {
                            codeGenerator.EmitBoxing(returned_typecode);
                            //codeGenerator.EmitLoadClassContext();
                            il.Emit(OpCodes.Call, Methods.Convert.ObjectToPhpBytes);
                            returned_typecode = PhpTypeCode.PhpBytes;
                        }
                        break;

                    case Operations.ArrayCast:
                        //Template: "(array)x"   Convert.ObjectToArray(x)
                        o_typecode = node.Expr.Emit(codeGenerator);
                        if (o_typecode != PhpTypeCode.PhpArray)
                        {
                            codeGenerator.EmitBoxing(o_typecode);
                            il.Emit(OpCodes.Call, Methods.Convert.ObjectToPhpArray);
                        }
                        returned_typecode = PhpTypeCode.PhpArray;
                        break;

                    case Operations.UnsetCast:
                        // Template: "(unset)x"  null
                        il.Emit(OpCodes.Ldnull);
                        returned_typecode = PhpTypeCode.Object;
                        break;

                    default:
                        Debug.Assert(false, "illegal type of operation!");
                        returned_typecode = PhpTypeCode.Void;
                        break;
                }

                switch (access)
                {
                    case AccessType.Read:
                        // do nothing
                        break;
                    case AccessType.None:
                        // pop operation's result value from stack
                        if (returned_typecode != PhpTypeCode.Void)
                            il.Emit(OpCodes.Pop);
                        return PhpTypeCode.Void;
                }

                return returned_typecode;
            }
        }
    }
}