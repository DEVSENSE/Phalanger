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
        internal static Dictionary<Type, Type>/*!*/AstNodeExtensionTypes
        {
            get
            {
                if (_astNodeExtensionTypes == null)
                    lock (typeof(AstNodeExtension))
                        if (_astNodeExtensionTypes == null)
                            _astNodeExtensionTypes = CreateNodeExtensionTypes();

                return _astNodeExtensionTypes;
            }
        }
        private static Dictionary<Type, Type> _astNodeExtensionTypes = null;
        
        /// <summary>
        /// Creates map of <see cref="AstNode"/> types corresponding to <see cref="INodeCompiler"/> types.
        /// </summary>
        /// <returns>Dictionary of <see cref="AstNode"/> types each mapped to <see cref="INodeCompiler"/> type.</returns>
        private static Dictionary<Type, Type>/*!*/CreateNodeExtensionTypes()
        {
            // like MEF, but simpler

            // lists all types within NodeCompilers,
            // maps types defining INodeCompiler with corresponding AstNode type

            var types = typeof(NodeCompilers).GetNestedTypes(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            var dict = new Dictionary<Type, Type>(types.Length);
            for (int i = 0; i < types.Length; i++)
            {
                var t = types[i];
                if (!t.IsAbstract && typeof(INodeCompiler).IsAssignableFrom(t))
                {
                    var attrs = t.GetCustomAttributes(typeof(NodeCompilerAttribute), false);
                    if (attrs != null && attrs.Length != 0)
                    {
                        var attr = (NodeCompilerAttribute)attrs[0];
                        dict.Add(attr.AstNodeType, t);
                    }
                }
            }

            return dict;
        }

        /// <summary>
        /// Key to <see cref="AstNode.Properties"/> referencing its <see cref="INodeCompiler"/>.
        /// </summary>
        private static object AstNodeCompilerKey = typeof(INodeCompiler);

        /// <summary>
        /// Gets (or creates new) <see cref="INodeCompiler"/> associated with given <paramref name="node"/>.
        /// </summary>
        /// <param name="node"><see cref="AstNode"/> instance.</param>
        /// <returns>Associuated <see cref="INodeCompiler"/> instance.</returns>
        private static T/*!*/NodeCompiler<T>(this AstNode/*!*/node) where T : class
        {
            Debug.Assert(typeof(INodeCompiler).IsAssignableFrom(typeof(T)));

            var obj = node.Properties[AstNodeCompilerKey] as T;
            if (obj == null)
            {
                node.Properties[AstNodeCompilerKey] = obj = CreateNodeCompiler(node.GetType()) as T;
                Debug.Assert(obj != null);
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
        /// Creates <see cref="INodeCompiler"/> instance for given <paramref name="nodeType"/>.
        /// </summary>
        /// <param name="nodeType">Type of <see cref="AstNode"/>.</param>
        /// <returns>New <see cref="INodeCompiler"/> instance for given <paramref name="nodeType"/>.</returns>
        private static INodeCompiler/*!*/CreateNodeCompiler(Type/*!*/nodeType)
        {
            Debug.Assert(nodeType != null);
            Debug.Assert(typeof(AstNode).IsAssignableFrom(nodeType));

            Type/*!*/nodeCompilerType = AstNodeExtensionTypes[nodeType];
            return (INodeCompiler)Activator.CreateInstance(nodeCompilerType);
        }

        #endregion

        #region Expression

        public static Evaluation EvaluatePriorAnalysis(this Expression/*!*/node, SourceUnit/*!*/sourceUnit)
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
