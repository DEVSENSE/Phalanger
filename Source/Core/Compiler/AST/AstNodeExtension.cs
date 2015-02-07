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
        /// Reflected information about specific node compiler.
        /// </summary>
        public struct NodeCompilerInfo
        {
            [Flags]
            private enum Flags : byte
            {
                HasDefaultCtor = 1,
                IsSingleton = 2,
            }

            private object data;
            private Flags flags;

            public bool IsSingleton { get { return (flags & Flags.IsSingleton) != 0; } }
            public bool HasDefaultCtor { get { return (flags & Flags.HasDefaultCtor) != 0; } }

            public NodeCompilerInfo(Type type, bool hasDefaultCtor, bool isSingleton)
            {
                Debug.Assert(type != null);
                Debug.Assert(!isSingleton || hasDefaultCtor);   // isSingleton => hasDefaultCtor
                data = type;
                flags = (Flags)0;
                if (hasDefaultCtor) flags |= Flags.HasDefaultCtor;
                if (isSingleton) flags |= Flags.IsSingleton;
            }

            /// <summary>
            /// Type of <see cref="INodeCompiler"/> to be used. In case of <see cref="IsSingleton"/>, this property is invalid.
            /// </summary>
            public Type/*!*/NodeCompilerType
            {
                get
                {
                    Debug.Assert(!IsSingleton);
                    return (Type)data;
                }
            }

            /// <summary>
            /// Instance of <see cref="INodeCompiler"/> is case of <see cref="IsSingleton"/> is <c>true</c>.
            /// </summary>
            public INodeCompiler/*!*/NodeCompilerSingleton
            {
                get
                {
                    Debug.Assert(IsSingleton);

                    var result = data as INodeCompiler;
                    if (result == null && IsSingleton)
                    {
                        Debug.Assert(HasDefaultCtor);
                        Debug.Assert(data is Type);

                        // lazily create instance of INodeCompiler
                        data = result = (INodeCompiler)Activator.CreateInstance((Type)data);
                    }
                    return result;
                }
            }

#if DEBUG
            internal void Test()
            {
                var type = data as Type;
                if (type == null) return;
                // determine whether NodeCompilerAttribute should have Singleton = true
                bool hasFields = false;
                for (var t = type; t != null && t != typeof(Object); t = t.BaseType)
                    hasFields |= t.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public).Length != 0;

                if (IsSingleton) Debug.Assert(!hasFields, "Singleton should not have instance fields.");
                else Debug.Assert(hasFields, type.ToString() + " should be marked as Singleton.");
            }
#endif
        }

        /// <summary>
        /// Gets map of <see cref="AstNode"/> types corresponding to <see cref="INodeCompiler"/> types.
        /// </summary>
        internal static Dictionary<Type, NodeCompilerInfo>/*!*/AstNodeExtensionTypes
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
        private static Dictionary<Type, NodeCompilerInfo> _astNodeExtensionTypes = null;
        
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
        /// Creates <see cref="INodeCompiler"/> instance for given <paramref name="node"/>.
        /// </summary>
        /// <param name="node">Corresponding <see cref="AstNode"/> instance.</param>
        /// <returns><see cref="INodeCompiler"/> instance for given <paramref name="node"/>.</returns>
        private static INodeCompiler/*!*/CreateNodeCompiler(AstNode/*!*/node)
        {
            var compilerinfo = AstNodeExtensionTypes[node.GetType()];
            if (compilerinfo.IsSingleton)
                return compilerinfo.NodeCompilerSingleton;

            if (compilerinfo.HasDefaultCtor)
                return (INodeCompiler)Activator.CreateInstance(compilerinfo.NodeCompilerType);
            else
                return (INodeCompiler)Activator.CreateInstance(compilerinfo.NodeCompilerType, node);
        }

        /// <summary>
        /// Gets (or creates) <see cref="IExpressionCompiler"/> associatd with given expression.
        /// </summary>
        private static IExpressionCompiler/*!*/ExpressionCompiler(this Expression/*!*/expr)
        {
            return NodeCompiler<IExpressionCompiler>(expr);
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
