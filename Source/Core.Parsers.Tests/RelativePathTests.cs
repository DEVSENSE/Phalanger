using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PHP.Core;

namespace Core.Parsers.Tests
{
    [TestClass]
    public class RelativePathTests
    {
        [TestMethod]
        public void TestPaths()
        {
            FullPath root;
            FullPath full;
            RelativePath rel;
            string str1, str2;

            string[,] cases = 
      {
          // root:        // full:          // full canonical:    // relative canonical:
        { @"C:\a/b/c/",   @"D:\a/b/",       @"D:\a\b\",           @"D:\a\b\" },
                                            
        { @"C:\a\b\c",    @"C:\a\b\c",      @"C:\a\b\c",          @"" },
        { @"C:\a\b\c",    @"C:\a\b\c\",     @"C:\a\b\c\",         @"" },
        { @"C:\a\b\c\",   @"C:\a\b\c",      @"C:\a\b\c",          @"" },
        { @"C:\a\b\c\",   @"C:\a\b\c\",     @"C:\a\b\c\",         @"" },
                                            
        { @"C:\a\b\c",    @"C:\a\b",        @"C:\a\b",            @".." },
        { @"C:\a\b\c\",   @"C:\a\b",        @"C:\a\b",            @".." },
                                            
        { @"C:\a\b\c",    @"C:\",           @"C:\",                @"..\..\.." },
        { @"C:\a\b\c\",   @"C:\",           @"C:\",                @"..\..\.." },
                                            
        { @"C:\a\b\c\",   @"C:\a\b\x\y\z",  @"C:\a\b\x\y\z",      @"..\x\y\z" },
        { @"C:\a\b\cd\",  @"C:\a\b\c",      @"C:\a\b\c",          @"..\c" },
        { @"C:\a\b\cd",   @"C:\a\b\c",      @"C:\a\b\c",          @"..\c" },
        { @"C:\a\b\cd\",  @"C:\a\b\c\d",    @"C:\a\b\c\d",          @"..\c\d" },
        { @"C:\a\b\cd",   @"C:\a\b\c\d",    @"C:\a\b\c\d",          @"..\c\d" },
      };

            for (int i = 0; i < cases.GetLength(0); i++)
            {
                root = new FullPath(cases[i, 0]);
                full = new FullPath(cases[i, 1]);
                rel = new RelativePath(root, full);

                Assert.AreEqual(full.ToString(), cases[i, 2]);

                Assert.AreEqual(rel.ToString(), cases[i, 3]);

                str1 = full;
                if (str1[str1.Length - 1] == '\\') str1 = str1.Substring(0, str1.Length - 1);

                str2 = rel.ToFullPath(root);
                if (str2[str2.Length - 1] == '\\') str2 = str2.Substring(0, str2.Length - 1);

                Assert.AreEqual(str1, str2);

                Assert.AreEqual(RelativePath.ParseCanonical(cases[i, 3]).ToString(), cases[i, 3]);
            }
        }
    }
}
