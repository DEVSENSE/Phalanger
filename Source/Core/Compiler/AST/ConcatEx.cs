/*

 Copyright (c) 2004-2006 Tomas Matousek, Vaclav Novak and Martin Maly.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Diagnostics;
using PHP.Core.Emit;
using PHP.Core.Parsers;

namespace PHP.Core.AST
{
    /// <summary>
    /// Represents a concatenation expression (dot PHP operator).
    /// </summary>
    public sealed class ConcatEx : Expression
    {
        internal override Operations Operation { get { return Operations.ConcatN; } }

        public List<Expression>/*!*/ Expressions { get { return this.expressions; } }
        private List<Expression>/*!*/ expressions;

        /// <summary>
        /// Initialize the ConcatEx AST node and optimize the subtree if possible. Look for child expressions and chain possible concatenations. This prevents StackOverflowException in case of huge concatenation expressions.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="expressions">List of expressions to concatenate.</param>
        /// <remarks>This method tries to propagate child concatenations and chain them.</remarks>
        public ConcatEx(Position position, List<Expression>/*!*/ expressions)
            : base(position)
        {
            Debug.Assert(expressions != null);
            this.expressions = ChainConcatenations(expressions);    // try to chain child ConcatEx expressions
        }

        #region Analysis

        internal static List<Expression> ChainConcatenations(List<Expression>/*!*/ expressions)
        {
            //return expressions;

            List<Expression> newExpressions = null;

            ConcatEx expr;

            for (int index = 0; index < expressions.Count; ++index)
            {
                if ((expr = expressions[index] as ConcatEx) != null)
                {
                    if (newExpressions == null)
                        newExpressions = expressions.GetRange(0, index); // initial list of expression (that were not ConcatEx)

                    newExpressions.AddRange(expr.Expressions);
                }
                else if (newExpressions != null)
                {
                    newExpressions.Add(expressions[index]);
                }
            }

            // something was chained ?? or not
            return newExpressions ?? expressions;
        }

        /// <summary>
        /// Analyze the list of expressions and separate them into a list of chunks. (HasValue and !HasValue together)
        /// </summary>
        /// <returns>List of chunks - expressions chunked by that, if they are evaluable during compilation. Cannot return null.</returns>
        private static List<ConcatChunk> AnalyzeChunks(Analyzer/*!*/ analyzer, List<Expression>/*!*/expressions)
        {
            Debug.Assert(expressions != null);
            Debug.Assert(expressions.Count > 0);

            List<ConcatChunk> chunks = new List<ConcatChunk>();

            ConcatChunk lastChunk = null;

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
                    chunks.Add(lastChunk = new ConcatChunk(expr.Position, evaluation));
                }
                else if (evaluation.HasValue)
                {
                    lastChunk.Value = Operators.Concat(lastChunk.Value, evaluation.Value);
                    lastChunk.Position = Position.CombinePositions(lastChunk.Position, expr.Position);
                }
                else//if (!evaluation.HasValue)
                {
                    lastChunk.Expressions.Add(evaluation.Expression);
                    lastChunk.Position = Position.CombinePositions(lastChunk.Position, expr.Position);
                }
            }

            // there must be at least one expression
            if (chunks.Count == 0)
            {
                Position position = Position.Invalid;
                if (expressions.Count > 0) position = expressions[0].Position;

                chunks.Add(new ConcatChunk(position, string.Empty));
            }

            //
            return chunks;
        }

        /// <summary>
        /// Expressions from ConcatChunks, Values are transformed into corresponding literals.
        /// </summary>
        private static IEnumerable<Expression> ChunkExpressions(List<ConcatChunk>/*!*/concatChunks)
        {
            Debug.Assert(concatChunks != null);
            foreach (var chunk in concatChunks)
            {
                if (chunk.HasValue)
                {
                    if (chunk.Value is PhpBytes)
                        yield return new BinaryStringLiteral(chunk.Position, (PhpBytes)chunk.Value, AccessType.Read);
                    else
                        yield return new StringLiteral(chunk.Position, Convert.ObjectToString(chunk.Value), AccessType.Read);
                }
                else
                {
                    foreach (var expr in chunk.Expressions)
                        yield return expr;
                }
            }
        }

        internal override Evaluation Analyze(Analyzer/*!*/ analyzer, ExInfoFromParent info)
        {
            Debug.Assert(expressions.Count > 0);
            access = info.Access;

            var concatChunks = AnalyzeChunks(analyzer, expressions);
            this.expressions = new List<Expression>(ChunkExpressions(concatChunks));   // replace expressions with optimized one

            if (concatChunks.Count == 1 && concatChunks[0].HasValue)
                return new Evaluation(this, concatChunks[0].Value); // can be resolved during compilation time
            else
                return new Evaluation(this);

            // obsolete: The ConcatEx represents encaps_list (see parse.y), which contains alternating StringLiterals and VarLikeConstructUses.
            //for (int i = 0; i < expressions.Count; i++)
            //{
            //    // evaluate the expression
            //    expressions[i] = expressions[i].Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Expression;
            //    Debug.Assert(expressions[i] is StringLiteral || expressions[i] is VarLikeConstructUse);
            //}

            //return new Evaluation(this);

            // obsolete:
            //if (concat != null)
            //{
            //  ce.Append(re);
            //  this.expressions = ce.expressions;
            //  return this;
            //}
            //else
            //{
            //  ce = re as ConcatEx;
            //  if (ce != null)
            //  {
            //    ce.Prepend(le);
            //    this.expressions = ce.expressions;
            //    return this;
            //  }
            //  else
            //  {
            //    object l_value;
            //    if (!le.TryEvaluate(out l_value)) return this;

            //    object r_value;
            //    if (!re.TryEvaluate(out r_value)) return this;

            //    return new StringLiteral(position, Evaluate(l_value, r_value), access);
            //  }
            //}
        }

        internal override object Evaluate(object leftValue, object rightValue)
        {
            return Operators.Concat(leftValue, rightValue);
        }

        /// <summary>
        /// Piece of analyzed ConcatEx expressions list.
        /// </summary>
        private class ConcatChunk
        {
            public ConcatChunk(Position position, Evaluation evaluation)
            {
                if ((this.HasValue = evaluation.HasValue) == true)
                    this.Value = evaluation.Value;
                else
                    this.Expressions = new List<Expression>() { evaluation.Expression };
                
                this.Position = position;
            }

            public ConcatChunk(Position position, object value)
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
            public Position Position;

            /// <summary>
            /// If HasValue is false, list of expression to be emitted.
            /// </summary>
            public List<Expression> Expressions;
        }

        #endregion

        #region Emission

        /// <include file='Doc/Nodes.xml' path='doc/method[@name="IsDeeplyCopied"]/*'/>
        internal override bool IsDeeplyCopied(CopyReason reason, int nestingLevel)
        {
            return false;
        }

        internal override PhpTypeCode Emit(CodeGenerator codeGenerator)
        {
            Debug.Assert(access == AccessType.Read || access == AccessType.None);
            Statistics.AST.AddNode("Class.Concat." + expressions.Count);

            PhpTypeCode result;

            //
            // For low numbers call specialized methods
            switch (expressions.Count)
            {
                case 1:
                    result = expressions[0].Emit(codeGenerator);

                    if (result != PhpTypeCode.PhpBytes && result != PhpTypeCode.String)
                    {
                        var lbl = codeGenerator.IL.DefineLabel();
                        codeGenerator.EmitBoxing(result);
                        codeGenerator.IL.Emit(OpCodes.Dup);
                        codeGenerator.IL.Emit(OpCodes.Isinst,typeof(PhpBytes));

                        // IF (STACK)
                        codeGenerator.IL.Emit(OpCodes.Brtrue_S,lbl);                      
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
            if (expression.HasValue)
            {
                if (expression.Value is PhpBytes)
                {
                    codeGenerator.IL.LoadLiteral(expression.Value);
                    return PhpTypeCode.PhpBytes;
                }
                else
                {
                    // evaluated expression is converted to a string if necessary:
                    codeGenerator.IL.Emit(OpCodes.Ldstr, Convert.ObjectToString(expression.Value));
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

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitConcatEx(this);
        }
    }
}

