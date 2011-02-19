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
			Debug.UnitTestCore();
			Debug.UnitTest(typeof(PhpArrays).Assembly, Console.Out);
			#endif
		}
	}
}