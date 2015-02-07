/*

 Copyright (c) 2007- DEVSENSE

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PHP.Core;
using PHP.Core.AST;
using PHP.Core.Reflection;

namespace PHP.Core.Compiler.AST
{
    /// <summary>
    /// Annotates <see cref="INodeCompiler"/> implementation with type of <see cref="AstNode"/> which is used for.
    /// </summary>
    internal sealed class NodeCompilerAttribute : Attribute
    {
        /// <summary>
        /// Type of <see cref="AstNode"/> which is extended by corresponding <see cref="INodeCompiler"/>.
        /// </summary>
        public Type/*!*/AstNodeType { get { return _astNodeType; } }
        private readonly Type _astNodeType;

        public NodeCompilerAttribute(Type/*!*/nodeType)
        {
            if (nodeType == null)
                throw new ArgumentNullException("nodeType");

            Debug.Assert(typeof(AstNode).IsAssignableFrom(nodeType));
            _astNodeType = nodeType;
        }
    }

    /// <summary>
    /// Base compiler <see cref="AstNode"/> extension interface.
    /// </summary>
    internal interface INodeCompiler
    {
        
    }

    /// <summary>
    /// Base compiler <see cref="Expression"/> extension interface .
    /// </summary>
    internal interface IExpressionCompiler : INodeCompiler
    {
        /// <summary>
        /// Evaluates value of given <paramref name="node"/> before actual analysis, with no additional information provided.
        /// </summary>
        /// <param name="node"><see cref="Expression"/> instance, associated with this <see cref="INodeCompiler"/>.</param>
        /// <param name="sourceUnit">Containing <see cref="SourceUnit"/>.</param>
        /// <returns><see cref="Evaluation"/> with the result of the operation.</returns>
        /// <remarks>This method allows compiler to resolve static inclusions before analysis,
        /// when application constants or literals are used within the expression.</remarks>
        Evaluation EvaluatePriorAnalysis(Expression/*!*/node, SourceUnit/*!*/sourceUnit);

        /// <summary>
        /// Performs analysis of given expression, before actual emit.
        /// </summary>
        /// <param name="node"><see cref="Expression"/> instance, associated with this <see cref="INodeCompiler"/>.</param>
        /// <param name="analyzer">Analyzer object.</param>
        /// <param name="info">Access information from parent node.</param>
        /// <returns><see cref="Evaluation"/> containing this or new expression, as a result of this operation.</returns>
        Evaluation Analyze(Expression/*!*/node, Analyzer/*!*/ analyzer, ExInfoFromParent info);

        /// <summary>
        /// Emits given <paramref name="node"/> into IL.
        /// </summary>
        /// <param name="node"><see cref="Expression"/> instance, associated with this <see cref="INodeCompiler"/>.</param>
        /// <param name="codeGenerator"><see cref="CodeGenerator"/> object.</param>
        /// <returns>Whether this operation left a value on stack, the return value contains its type code. Othervise it returns <see cref="PhpTypeCode.Void"/>.</returns>
        PhpTypeCode Emit(Expression/*!*/node, CodeGenerator/*!*/ codeGenerator);
        
        /// <summary>
        /// Helper function to compute an unary operation.
        /// </summary>
        /// <param name="node"><see cref="Expression"/> instance, associated with this <see cref="INodeCompiler"/>.</param>
        /// <param name="value">Operand of the operation.</param>
        /// <returns>Computed value.</returns>
        object Evaluate(Expression/*!*/node, object value);

        /// <summary>
        /// Helper function to compute a binary operation.
        /// </summary>
        /// <param name="node"><see cref="Expression"/> instance, associated with this <see cref="INodeCompiler"/></param>
        /// <param name="leftValue">Left operand.</param>
        /// <param name="rightValue">Right operand.</param>
        /// <returns>Computed value.</returns>
        object Evaluate(Expression/*!*/node, object leftValue, object rightValue);

        /// <summary>
        /// Whether compiler should emit deep variable copying, when passing expression as an assignment, a function parameter or as a function return value.
        /// </summary>
        /// <param name="node"><see cref="Expression"/> instance, associated with this <see cref="INodeCompiler"/>.</param>
        /// <param name="reason">The reason of copying.</param>
        /// <param name="nestingLevel"></param>
        /// <returns>Whether compiler should perform variable copying.</returns>
        bool IsDeeplyCopied(Expression/*!*/node, CopyReason reason, int nestingLevel);

        /// <summary>
        /// Whether an expression represented by this node should be stored to a temporary local if assigned.
        /// </summary>
        bool StoreOnAssignment(Expression/*!*/node);
    }
}
