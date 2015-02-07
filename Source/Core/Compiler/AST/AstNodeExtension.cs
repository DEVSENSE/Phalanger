/*

 Copyright (c) 2013 DEVSENSE

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using PHP.Core.Reflection;

using PHP.Core.AST;

namespace PHP.Core.Compiler.AST
{
    /// <summary>
    /// <see cref="AstNode"/> extension methods.
    /// </summary>
    internal static class AstNodeExtension
    {
        #region INodeCompiler instantiation

        /// <summary>
        /// Gets map of <see cref="AstNode"/> types corresponding to <see cref="INodeCompiler"/> types.
        /// </summary>
        internal static Dictionary<Type, NodeCompilers.NodeCompilerInfo>/*!*/AstNodeExtensionTypes
        {
            get
            {
                if (_astNodeExtensionTypes == null)
                    lock (typeof(AstNodeExtension))
                        if (_astNodeExtensionTypes == null)
                            _astNodeExtensionTypes = NodeCompilers.CreateNodeExtensionTypes();

                return _astNodeExtensionTypes;
            }
        }
        private static Dictionary<Type, NodeCompilers.NodeCompilerInfo> _astNodeExtensionTypes = null;
        
        /// <summary>
        /// Key to <see cref="AstNode.Properties"/> referencing its <see cref="INodeCompiler"/>.
        /// </summary>
        private static object AstNodeCompilerKey = typeof(INodeCompiler);

        /// <summary>
        /// Gets (or creates new) <see cref="INodeCompiler"/> associated with given <paramref name="node"/>.
        /// </summary>
        /// <param name="node"><see cref="AstNode"/> instance.</param>
        /// <returns>Associuated <see cref="INodeCompiler"/> instance.</returns>
        public static T/*!*/NodeCompiler<T>(this AstNode/*!*/node) where T : class
        {
            var obj = node.Properties[AstNodeCompilerKey] as T;
            if (obj == null)
            {
                node.Properties[AstNodeCompilerKey] = obj = CreateNodeCompiler(node) as T;
                Debug.Assert(obj != null, "AstNode " + node.GetType().ToString() + " does not implement INodeCompiler of type " + typeof(T).ToString());
            }
            return obj;
        }

        /// <summary>
        /// Gets (or creates) <see cref="IExpressionCompiler"/> associatd with given expression.
        /// </summary>
        private static IExpressionCompiler/*!*/ExpressionCompiler(this Expression/*!*/expr)
        {
            return NodeCompiler<IExpressionCompiler>(expr);
        }

        /// <summary>
        /// Creates <see cref="INodeCompiler"/> instance for given <paramref name="node"/>.
        /// </summary>
        /// <param name="node">Corresponding <see cref="AstNode"/> instance.</param>
        /// <returns>New <see cref="INodeCompiler"/> instance for given <paramref name="node"/>.</returns>
        private static INodeCompiler/*!*/CreateNodeCompiler(AstNode/*!*/node)
        {
            var/*!*/nodeCompilerType = AstNodeExtensionTypes[node.GetType()];
            if (nodeCompilerType.hasDefaultCtor)
                return (INodeCompiler)Activator.CreateInstance(nodeCompilerType.type);
            else
                return (INodeCompiler)Activator.CreateInstance(nodeCompilerType.type, node);
        }

        #endregion

        #region Expression

        public static AccessType GetAccess(this Expression/*!*/node)
        {
            return node.ExpressionCompiler().Access;
        }

        public static bool HasValue(this Expression/*!*/node)
        {
            return node.ExpressionCompiler().HasValue(node);
        }

        public static object GetValue(this Expression/*!*/node)
        {
            return node.ExpressionCompiler().GetValue(node);
        }

        public static PhpTypeCode GetValueTypeCode(this Expression/*!*/node)
        {
            return node.ExpressionCompiler().GetValueTypeCode(node);
        }

        public static Evaluation EvaluatePriorAnalysis(this Expression/*!*/node, CompilationSourceUnit/*!*/sourceUnit)
        {
            var nodecompiler = node.ExpressionCompiler();
            return nodecompiler.EvaluatePriorAnalysis(node, sourceUnit);
        }
        public static Evaluation Analyze(this Expression/*!*/node, Analyzer/*!*/ analyzer, ExInfoFromParent info)
        {
            var nodecompiler = node.ExpressionCompiler();
            return nodecompiler.Analyze(node, analyzer, info);
        }
        
        public static object Evaluate(this Expression/*!*/node, object value)
        {
            var nodecompiler = node.ExpressionCompiler();
            return nodecompiler.Evaluate(node, value);
        }
        public static object Evaluate(this Expression/*!*/node, object leftValue, object rightValue)
        {
            var nodecompiler = node.ExpressionCompiler();
            return nodecompiler.Evaluate(node, leftValue, rightValue);
        }

        public static PhpTypeCode Emit(this Expression/*!*/node, CodeGenerator/*!*/ codeGenerator)
        {
            var nodecompiler = node.ExpressionCompiler();
            return nodecompiler.Emit(node, codeGenerator);
        }

        public static bool IsDeeplyCopied(this Expression/*!*/node, CopyReason reason, int nestingLevel)
        {
            var nodecompiler = node.ExpressionCompiler();
            return nodecompiler.IsDeeplyCopied(node, reason, nestingLevel);
        }

        public static bool IsCustomAttributeArgumentValue(this Expression/*!*/node)
        {
            var nodecompiler = node.ExpressionCompiler();
            return nodecompiler.IsCustomAttributeArgumentValue(node);
        }

        /// <summary>
        /// Whether an expression represented by this node should be stored to a temporary local if assigned.
        /// </summary>
        public static bool StoreOnAssignment(this Expression/*!*/node)
        {
            var nodecompiler = node.ExpressionCompiler();
            return nodecompiler.StoreOnAssignment(node);
        }

        #endregion        
    }
}
