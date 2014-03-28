using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PHP.Core;

namespace PHP.Core.Tests
{
    [TestClass]
    public class TypeDescsTests
    {
        [TestMethod]
        public static void TestPhpObjectInterfaces()
        {
            Type[] ifaces = typeof(PhpObject).GetInterfaces();
            Type[] expected = new Type[]
				{ 
					typeof(PHP.Core.IPhpVariable),
					typeof(PHP.Core.IPhpConvertible),
					typeof(PHP.Core.IPhpPrintable),
					typeof(PHP.Core.IPhpCloneable),
					typeof(PHP.Core.IPhpComparable),
					typeof(PHP.Core.IPhpObjectGraphNode),
					typeof(PHP.Core.IPhpEnumerable),
					typeof(System.IDisposable),
                    typeof(System.Dynamic.IDynamicMetaObjectProvider),
#if !SILVERLIGHT
					typeof(System.Runtime.Serialization.ISerializable),
					typeof(System.Runtime.Serialization.IDeserializationCallback) 
#endif
				};

            Assert.AreEqual(ifaces.Length, expected.Length);
            Assert.IsTrue(ifaces.All(iface =>
            {
                return Array.IndexOf(expected, iface) != -1;
            }), "ReflectInterfaces must be updated if PhpObject implements different interfaces than listed");
        }
    }
}
