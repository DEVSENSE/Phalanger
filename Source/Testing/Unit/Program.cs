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
            TestUtils.UnitTest(typeof(PHP.Core.Parsers.Parser).Assembly, Console.Out);
            TestUtils.UnitTest(typeof(ScriptContext).Assembly, Console.Out);
            TestUtils.UnitTest(typeof(PhpArrays).Assembly, Console.Out);
			#endif
		}
	}
}