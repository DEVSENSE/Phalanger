using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PHP.Core;
using PHP.Core.Reflection;

namespace PHP.Core.Tests
{
    [TestClass]
    public class PrimitiveTypeTests
    {
        [TestMethod]
        public void TestGetByName()
        {
            // positive tests
            foreach (var name in new[] {
                QualifiedName.Boolean,
                QualifiedName.Integer, 
                QualifiedName.LongInteger,
                QualifiedName.Double,
                QualifiedName.String,
                QualifiedName.Resource,
                QualifiedName.Array,
                QualifiedName.Object,
                QualifiedName.Callable})
            {
                Assert.IsTrue(name.IsPrimitiveTypeName);
                Assert.IsNotNull(PrimitiveType.GetByName(name));
                Assert.IsNotNull(PrimitiveType.GetByName(new PrimitiveTypeName(name)));
            }

            // false tests
            foreach (var name in new[] {
                QualifiedName.Error,
                QualifiedName.False, 
                QualifiedName.Global,
                QualifiedName.Lambda,
                QualifiedName.Null,
                QualifiedName.True})
            {
                Assert.IsFalse(name.IsPrimitiveTypeName);
                Assert.IsNull(PrimitiveType.GetByName(name));
            }
        }
    }
}
