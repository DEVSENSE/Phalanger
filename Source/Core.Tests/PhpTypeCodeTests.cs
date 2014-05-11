using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PHP.Core.Reflection;

namespace PHP.Core.Tests
{
    [TestClass]
    public class PhpTypeCodeTests
    {
        [TestMethod]
        public void TestGetByTypeCode()
        {
            // positive tests
            foreach (var typecode in new PhpTypeCode[] {
                PhpTypeCode.Boolean,
                PhpTypeCode.Integer,
                PhpTypeCode.LongInteger,
                PhpTypeCode.Double,
                PhpTypeCode.String,
                PhpTypeCode.PhpResource,
                PhpTypeCode.PhpArray,
                PhpTypeCode.DObject,
                PhpTypeCode.PhpCallable})
            {
                Assert.IsNotNull(PrimitiveType.GetByTypeCode(typecode));
            }
        }
    }
}
