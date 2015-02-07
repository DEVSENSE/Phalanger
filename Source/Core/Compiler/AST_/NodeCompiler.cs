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
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    internal sealed class NodeCompilerAttribute : Attribute
    {
        /// <summary>
        /// Type of <see cref="AstNode"/> which is extended by corresponding <see cref="INodeCompiler"/>.
        /// </summary>
        public Type/*!*/AstNodeType { get { return _astNodeType; } }
        private readonly Type _astNodeType;

        /// <summary>
        /// Whether we need just single instance for all the nodes of provided type.
        /// </summary>
        /// <remarks>This saves memory resources for node compilers holding no additional data.</remarks>
        public bool Singleton { get; set; }

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
}
