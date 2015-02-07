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

using PHP.Core.AST;
using PHP.Core.Reflection;
using Conditional = System.Diagnostics.ConditionalAttribute;
using System.Diagnostics;

namespace PHP.Core.Compiler.AST
{
    /// <summary>
    /// Container for <see cref="INodeCompiler"/> implementations.
    /// </summary>
    public partial class NodeCompilers
    {
        /// <summary>
        /// Creates map of <see cref="AstNode"/> types corresponding to <see cref="INodeCompiler"/> types.
        /// </summary>
        /// <returns>Dictionary of <see cref="AstNode"/> types each mapped to <see cref="INodeCompiler"/> type.</returns>
        internal static Dictionary<Type, AstNodeExtension.NodeCompilerInfo>/*!*/CreateNodeExtensionTypes()
        {
            // like MEF, but simpler

            // lists all types within NodeCompilers,
            // maps types defining INodeCompiler with corresponding AstNode type

            var types = typeof(NodeCompilers).GetNestedTypes(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            var dict = new Dictionary<Type, AstNodeExtension.NodeCompilerInfo>(types.Length);
            for (int i = 0; i < types.Length; i++)
            {
                var t = types[i];
                if (!t.IsAbstract && typeof(INodeCompiler).IsAssignableFrom(t))
                {
                    var attrs = t.GetCustomAttributes(typeof(NodeCompilerAttribute), false);
                    if (attrs != null && attrs.Length != 0)
                    {
                        bool hasDefaultCtor = t.GetConstructor(Type.EmptyTypes) != null;
                        foreach (NodeCompilerAttribute attr in attrs)
                        {
                            Type compilertype = (t.ContainsGenericParameters)
                                ? t.MakeGenericType(attr.AstNodeType)
                                : t;

                            dict.Add(attr.AstNodeType, new AstNodeExtension.NodeCompilerInfo(compilertype, hasDefaultCtor, attr.Singleton));
                        }
                    }
                }
            }

            return dict;
        }
    }
}
