using System;
using PHP.Library;
using PHP.Core;

namespace Testers
{
	class UnitTester
	{
		[STAThread]
		static void Main(string[] args)
		{
			#if DEBUG
            TestUtils.UnitTestCore();
            TestUtils.UnitTest(typeof(PhpArrays).Assembly, Console.Out);
			#endif
		}
	}
}