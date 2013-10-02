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

namespace PHP.Core.Compiler.AST
{
    /// <summary>
    /// Container for <see cref="INodeCompiler"/> implementations.
    /// </summary>
    public partial class AstNodeCompilers
    {
        #if DEBUG

        /// <summary>
        /// Checks whether every implementation of <see cref="AstNode"/> has its <see cref="INodeCompiler"/> implementation.
        /// </summary>
        [Test]
        static void TestAstNodeCompilersDefined()
        {
            var dict = AstNodeExtension.AstNodeExtensionTypes;
            Debug.Assert(dict != null);

            var asttypes = typeof(LangElement).Assembly
                .GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && t.IsPublic && typeof(AstNode).IsAssignableFrom(t));

            foreach (var t in asttypes)
            {
                Debug.Assert(dict[t] != null);
            }
        }

        #endif
    }
}
