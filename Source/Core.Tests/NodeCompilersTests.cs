using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PHP.Core.AST;
using System.Diagnostics;

namespace PHP.Core.Tests
{
    [TestClass]
    public class NodeCompilersTests
    {
        [TestMethod]
        public void TestAstNodeCompilersDefined()
        {
            var dict = PHP.Core.Compiler.AST.AstNodeExtension.AstNodeExtensionTypes;
            Assert.IsNotNull(dict);

            var asttypes = typeof(LangElement).Assembly
                .GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && t.IsPublic && typeof(AstNode).IsAssignableFrom(t));

            foreach (var t in asttypes)
            {
                var compilerinfo = dict[t];

#if DEBUG
                compilerinfo.Test();
#endif
            }
        }
    }
}
