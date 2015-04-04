/*

 Copyright (c) 2013 DEVSENSE

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Diagnostics;

using PHP.Core.AST;
using PHP.Core.Emit;
using PHP.Core.Parsers;

namespace PHP.Core.Compiler.AST
{
    partial class NodeCompilers
    {
        [NodeCompiler(typeof(ConcatEx))]
        sealed class ConcatExCompiler : ExpressionCompiler<ConcatEx>
        {
            #region Analysis

            private static IList<Expression> ChainConcatenations(IList<Expression>/*!*/ expressions)
            {
                //return expressions;

                List<Expression> newExpressions = null;

                ConcatEx expr;

                for (int index = 0; index < expressions.Count; index++)
                {
                    if ((expr = expressions[index] as ConcatEx) != null)
                    {
                        if (newExpressions == null)
                        {
                            newExpressions = new List<Expression>(index);
                            newExpressions.AddRange(expressions.Take(index)); // initial list of expressions (that were not ConcatEx)
                        }

                        newExpressions.AddRange(expr.Expressions);
                    }
                    else if (newExpressions != null)
                    {
                        newExpressions.Add(expressions[index]);
                    }
                }

                // something was chained ?? or not
                return (IList<Expression>)newExpressions ?? expressions;
            }

            /// <summary>
            /// Analyze the list of expressions and separate them into a list of chunks. (HasValue and !HasValue together)
            /// </summary>
            /// <returns>List of chunks - expressions chunked by that, if they are evaluable during compilation. Cannot return null.</returns>
            private static List<ConcatChunk> AnalyzeChunks(Analyzer/*!*/ analyzer, IList<Expression>/*!*/expressions)
            {
                Debug.Assert(expressions.Any());

                List<ConcatChunk> chunks = new List<ConcatChunk>();

                ConcatChunk lastChunk = null;

                // flattern concatenation expressions:
                expressions = ChainConcatenations(expressions);

                // analyze chunks
                foreach (var expr in expressions)
                {
                    var evaluation = expr.Analyze(analyzer, ExInfoFromParent.DefaultExInfo);

                    // skip empty evaluated expressions
                    if (evaluation.HasValue &&
                        (
                            evaluation.Value == null ||
                            (evaluation.Value is string && ((string)evaluation.Value) == string.Empty) ||
                            Convert.ObjectToPhpBytes(evaluation.Value).Length == 0
                        ))
                    {
                        continue;  // empty literal => skip
                    }

                    // add chunk
                    if (lastChunk == null || lastChunk.HasValue != evaluation.HasValue)
                    {
                        chunks.Add(lastChunk = new ConcatChunk(expr.Span, evaluation));
                    }
                    else if (evaluation.HasValue)
                    {
                        lastChunk.Value = Operators.Concat(lastChunk.Value, evaluation.Value);
                        lastChunk.Position = Text.Span.Combine(lastChunk.Position, expr.Span);
                    }
                    else//if (!evaluation.HasValue)
                    {
                        lastChunk.Expressions.Add(evaluation.Expression);
                        lastChunk.Position = Text.Span.Combine(lastChunk.Position, expr.Span);
                    }
                }

                // there must be at least one expression
                if (chunks.Count == 0)
                {
                    var position = Text.Span.Invalid;
                    if (expressions.Count > 0) position = expressions[0].Span;

                    chunks.Add(new ConcatChunk(position, string.Empty));
                }

                //
                return chunks;
            }

            /// <summary>
            /// Expressions from ConcatChunks, Values are transformed into corresponding literals.
            /// </summary>
            private static IEnumerable<Expression> ChunkExpressions(IEnumerable<ConcatChunk>/*!*/concatChunks)
            {
                Debug.Assert(concatChunks != null);
                foreach (var chunk in concatChunks)
                {
                    if (chunk.HasValue)
                    {
                        yield return LiteralUtils.Create(chunk.Position, chunk.Value, AccessType.Read);
                    }
                    else
                    {
                        foreach (var expr in chunk.Expressions)
                            yield return expr;
                    }
                }
            }

            public override Evaluation Analyze(ConcatEx node, Analyzer analyzer, ExInfoFromParent info)
            {
                Debug.Assert(node.Expressions.Length > 0);
                access = info.Access;

                var concatChunks = AnalyzeChunks(analyzer, node.Expressions);
                node.Expressions = ChunkExpressions(concatChunks).ToArray();   // replace expressions with optimized one

                if (concatChunks.Count == 1 && concatChunks[0].HasValue)
                    return new Evaluation(node, concatChunks[0].Value); // can be resolved during compilation time
                else
                    return new Evaluation(node);
            }

            public override object Evaluate(ConcatEx node, object leftValue, object rightValue)
            {
                return Operators.Concat(leftValue, rightValue);
            }

            /// <summary>
            /// Piece of analyzed ConcatEx expressions list.
            /// </summary>
            private class ConcatChunk
            {
                public ConcatChunk(Text.Span position, Evaluation evaluation)
                {
                    if ((this.HasValue = evaluation.HasValue) == true)
                        this.Value = evaluation.Value;
                    else
                        this.Expressions = new List<Expression>() { evaluation.Expression };

                    this.Position = position;
                }

                public ConcatChunk(Text.Span position, object value)
                {
                    this.HasValue = true;
                    this.Value = value;
                    this.Position = position;
                }

                public bool HasValue;

                /// <summary>
                /// If HasValue is true, the Value of the chunk.
                /// </summary>
                public object Value;

                /// <summary>
                /// Position of the <see cref="Value"/> within the source code.
                /// </summary>
                public Text.Span Position;

                /// <summary>
                /// If HasValue is false, list of expression to be emitted.
                /// </summary>
                public List<Expression> Expressions;
            }

            #endregion

            #region Emission

            public override bool IsDeeplyCopied(ConcatEx node, CopyReason reason, int nestingLevel)
            {
                return false;
            }

            public override PhpTypeCode Emit(ConcatEx node, CodeGenerator codeGenerator)
            {
                Debug.Assert(access == AccessType.Read || access == AccessType.None);
                Statistics.AST.AddNode("Class.Concat." + node.Expressions.Length);

                PhpTypeCode result;

                //
                // For low numbers call specialized methods
                var/*!*/expressions = node.Expressions;
                switch (expressions.Length)
                {
                    case 1:
                        result = expressions[0].Emit(codeGenerator);

                        if (result != PhpTypeCode.PhpBytes && result != PhpTypeCode.String)
                        {
                            var lbl = codeGenerator.IL.DefineLabel();
                            codeGenerator.EmitBoxing(result);
                            codeGenerator.IL.Emit(OpCodes.Dup);
                            codeGenerator.IL.Emit(OpCodes.Isinst, typeof(PhpBytes));

                            // IF (STACK)
                            codeGenerator.IL.Emit(OpCodes.Brtrue_S, lbl);
                            if (true)
                            {
                                codeGenerator.IL.Emit(OpCodes.Call, Methods.Convert.ObjectToString);
                            }

                            // ELSE                        
                            codeGenerator.IL.MarkLabel(lbl, true);

                            //END IF

                            result = PhpTypeCode.Object;
                        }
                        break;

                    case 2:
                        result = EmitConcat(codeGenerator, expressions[0], expressions[1]);
                        break;

                    default:

                        codeGenerator.EmitObjectArrayPopulation(expressions);
                        codeGenerator.IL.Emit(OpCodes.Call, Methods.Operators.Concat.ObjectArray);

                        result = PhpTypeCode.Object;    // string, PhpBytes
                        break;
                }

                switch (access)
                {
                    case AccessType.Read:
                        // do nothing
                        break;

                    case AccessType.None:
                        // pop result from stack
                        codeGenerator.IL.Emit(OpCodes.Pop);
                        result = PhpTypeCode.Void;
                        break;
                }

                return result;
            }

            /// <summary>
            /// Emits concatenation of a pair of expressions.
            /// </summary>
            /// <param name="codeGenerator">A code generator.</param>
            /// <param name="x">The first expression.</param>
            /// <param name="y">The second expression.</param>
            /// <returns>The resulting type code.</returns>
            internal static PhpTypeCode EmitConcat(CodeGenerator/*!*/ codeGenerator, Expression/*!*/ x, Expression/*!*/ y)
            {
                PhpTypeCode type_code_x = EmitConcatExpressionLoad(codeGenerator, x);
                PhpTypeCode type_code_y = EmitConcatExpressionLoad(codeGenerator, y);

                if (type_code_x == PhpTypeCode.String)
                {
                    if (type_code_y == PhpTypeCode.String)
                    {
                        // string.string:
                        codeGenerator.IL.Emit(OpCodes.Call, Methods.String_Concat_String_String);

                        return PhpTypeCode.String;
                    }
                    else if (type_code_y == PhpTypeCode.PhpBytes)
                    {
                        // check the return type:
                        Debug.Assert(Methods.PhpBytes.Concat_Object_PhpBytes.ReturnType == typeof(PhpBytes));

                        // string.PhpBytes:
                        codeGenerator.IL.Emit(OpCodes.Call, Methods.PhpBytes.Concat_Object_PhpBytes);

                        return PhpTypeCode.PhpBytes;
                    }
                    else
                    {
                        // string.object:
                        codeGenerator.IL.Emit(OpCodes.Call, Methods.Operators.Concat.String_Object);

                        return PhpTypeCode.Object;
                    }
                }
                else if (type_code_x == PhpTypeCode.PhpBytes)
                {
                    if (type_code_y == PhpTypeCode.PhpBytes)
                    {
                        // check the return type:
                        Debug.Assert(Methods.PhpBytes.Concat_PhpBytes_PhpBytes.ReturnType == typeof(PHP.Core.PhpBytes));

                        // PhpBytes.PhpBytes
                        codeGenerator.IL.Emit(OpCodes.Call, Methods.PhpBytes.Concat_PhpBytes_PhpBytes);
                    }
                    else
                    {
                        // check the return type:
                        Debug.Assert(Methods.PhpBytes.Concat_PhpBytes_Object.ReturnType == typeof(PHP.Core.PhpBytes));

                        // PhpBytes.object:
                        codeGenerator.IL.Emit(OpCodes.Call, Methods.PhpBytes.Concat_PhpBytes_Object);
                    }

                    return PhpTypeCode.PhpBytes;
                }
                else
                {
                    if (type_code_y == PhpTypeCode.String)
                    {
                        // check the return type:
                        //Debug.Assert(Methods.Operators.Concat.Object_String.ReturnType == typeof(object));

                        // object.string:
                        codeGenerator.IL.Emit(OpCodes.Call, Methods.Operators.Concat.Object_String);
                        return PhpTypeCode.Object;
                    }
                    else if (type_code_y == PhpTypeCode.PhpBytes)
                    {
                        // check the return type:
                        Debug.Assert(Methods.PhpBytes.Concat_Object_PhpBytes.ReturnType == typeof(PhpBytes));

                        // object.PhpBytes:
                        codeGenerator.IL.Emit(OpCodes.Call, Methods.PhpBytes.Concat_Object_PhpBytes);
                        return PhpTypeCode.PhpBytes;
                    }
                    else
                    {   // object.object:
                        codeGenerator.IL.Emit(OpCodes.Call, Methods.Operators.Concat.Object_Object);
                        return PhpTypeCode.Object;
                    }
                }
            }

            /// <summary>
            /// Emits load of an argument of a concatenation.
            /// </summary>
            private static PhpTypeCode EmitConcatExpressionLoad(CodeGenerator/*!*/ codeGenerator, Expression/*!*/ expression)
            {
                // tries to evaluate the expression:
                if (expression.HasValue())
                {
                    var value = expression.GetValue();
                    if (value is PhpBytes)
                    {
                        codeGenerator.IL.LoadLiteral(value);
                        return PhpTypeCode.PhpBytes;
                    }
                    else
                    {
                        // evaluated expression is converted to a string if necessary:
                        codeGenerator.IL.Emit(OpCodes.Ldstr, Convert.ObjectToString(value));
                        return PhpTypeCode.String;
                    }
                }
                else
                {
                    // emits non-evaluable expression:
                    PhpTypeCode result = expression.Emit(codeGenerator);

                    // the result should be converted to string: (so we know the type for the further analysis)
                    if (result != PhpTypeCode.String && // string already
                        result != PhpTypeCode.Object && // object can contain PhpBytes, should be converted just when we know we need string
                        result != PhpTypeCode.PhpBytes  // keep PhpBytes
                        )
                    {
                        codeGenerator.EmitBoxing(result);   // in case of value-type
                        codeGenerator.IL.Emit(OpCodes.Call, Methods.Convert.ObjectToString);
                        result = PhpTypeCode.String;
                    }

                    return result;
                }
            }

            #endregion
        }
    }
}

