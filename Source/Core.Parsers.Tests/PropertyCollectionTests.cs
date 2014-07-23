using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PHP.Core.Parsers.Tests
{
    [TestClass]
    public class PropertyCollectionTests
    {
        [TestMethod]
        public void PropertyCollectionTest()
        {
            var collection = new PropertyCollection();

            for (int pass = 1; pass < 100; pass++)
            {
                collection.ClearProperties();
                Assert.AreEqual(collection.Count, 0);

                int count = pass * 2;

                TestAdd(ref collection, count);
                Assert.AreEqual(collection.Count, count);

                TestRemove(ref collection, count);
                Assert.AreEqual(collection.Count, 0);

                TestAdd(ref collection, count);
                Assert.AreEqual(collection.Count, count);

                // delete every second property
                for (int i = 0; i < count; i+=2)
                {
                    collection.RemoveProperty(i);
                    Assert.AreEqual(collection.GetProperty(i), null);
                }
                Assert.AreEqual(collection.Count, count / 2);

                // test property replacement
                for (int i = 1; i < count; i += 2)
                {
                    collection.SetProperty(i, i * 2);
                    Assert.AreEqual(collection.GetProperty(i), i * 2);
                }
                Assert.AreEqual(collection.Count, count / 2); 
            }
        }

        static void TestAdd(ref PropertyCollection collection, int count)
        {
            for (int i = 0; i < count; i++)
            {
                collection.SetProperty(i, i);
                Assert.AreEqual(collection.GetProperty(i), i);
            }            
        }

        static void TestRemove(ref PropertyCollection collection, int count)
        {
            for (int i = 0; i < count; i++)
            {
                collection.RemoveProperty(i);
                Assert.AreEqual(collection.GetProperty(i), null);
            }
        }
    }
}
