using System;
using System.IO;

namespace ChainGen
{
	class MainClass
	{
		static void BeginFunction()
		{
			FunctionName = "f" + Guid.NewGuid().ToString().Replace('-', 'x');
			sw.WriteLine("function {0}()", FunctionName);
			sw.WriteLine("{{", FunctionName);
			sw.WriteLine("\tglobal $x, $y;");
		}

		static void EndFunction()
		{
			sw.WriteLine("}");
			sw.WriteLine("{0}();", FunctionName);
			sw.WriteLine();
		}

		static void TestChain(string chain)
		{
			if (ExpressionCounter == 0) BeginFunction();

			sw.WriteLine("\t$_chain = \'{0}\';", chain);

			if (chain.IndexOf("[]") < 0)
			{
				sw.WriteLine("\t$_var = {0};", chain);
				sw.WriteLine("\t$_var = A::{0};", chain);
				sw.WriteLine("\t_f({0});", chain);
			}

			sw.WriteLine("\t$_var =& {0};", chain);
			if (chain.StartsWith("$")) sw.WriteLine("\t$_var =& A::{0};", chain);
			sw.WriteLine("\t_fr({0});", chain);
			if (TEST_READ_UNKNOWN) sw.WriteLine("\t_fu({0});", chain);

			if (!chain.EndsWith(")"))
			{
				sw.WriteLine("\t{0} = \"_literal\";", chain);
				sw.WriteLine("\t{0} = $_var;", chain);
				sw.WriteLine("\t{0} =& $_var;", chain);

				int bb_index = chain.IndexOf("[]");
				if (bb_index < 0 || bb_index == chain.Length - 2)
				{
					sw.WriteLine("\t_fr({0} = 1);", chain);
					if (TEST_READ_UNKNOWN) sw.WriteLine("\t_fu({0} = 1);", chain);
				}

				if (chain.StartsWith("$"))
				{
					sw.WriteLine("\tA::{0} = $_var;", chain);
					sw.WriteLine("\tA::{0} =& $_var;", chain);
				}

				if (bb_index < 0)
				{
					sw.WriteLine("\t_fr({0} += 1);", chain);
					if (TEST_READ_UNKNOWN) sw.WriteLine("\t_fu({0} += 1);", chain);

					sw.WriteLine("\tisset({0});", chain);
					sw.WriteLine("\tunset({0});", chain);

					if (chain.StartsWith("$"))
					{
						sw.WriteLine("\tisset(A::{0});", chain);
						sw.WriteLine("\tunset(A::{0});", chain);
					}
				}
			}

			ExpressionCounter++;
			if (ExpressionCounter >= CHAINS_PER_METHOD)
			{
				EndFunction();
				ExpressionCounter = 0;
			}
		}

		static void BuildChain(string chain, bool rdlock)
		{
			int i1 = chain.IndexOf('#');
			int i2 = chain.IndexOf('#', i1 + 1);

			if (i1 >= 0 && i2 > i1)
			{
				int len = Int32.Parse(chain.Substring(i1 + 1, i2 - i1 - 1));

				// expand the non-terminal
				string pre = chain.Substring(0, i1);
				string suf = chain.Substring(i2 + 1);

				if (len <= 1)
				{
					BuildChain(pre + "$x" + suf, rdlock);
					if (!rdlock) BuildChain(pre + "$$y" + suf, true);

					// avoid the illegal f()[]
					if (suf.Length == 0 || suf[0] != '[') BuildChain(pre + "_f(0)" + suf, rdlock);
				}
				else
				{
					BuildChain(pre + "#" + (len - 1) + "#[0]" + suf, rdlock);
					BuildChain(pre + "#" + (len - 1) + "#->a" + suf, rdlock);
					if (!rdlock) BuildChain(pre + "#" + (len - 1) + "#->$x" + suf, true);
					if (!rdlock) BuildChain(pre + "#" + (len - 1) + "#[]" + suf, true);
					BuildChain(pre + "#" + (len - 1) + "#[#" + (len - 2) + "#]" + suf, true);
					BuildChain(pre + "#" + (len - 1) + "#->{#" + (len - 2) + "#}" + suf, true);

					// avoid the illegal f()[]
					if (suf.Length == 0 || suf[0] != '[' && !rdlock)
					{
						BuildChain(pre + "#" + (len - 1) + "#[0](#" + (len - 2) + "#)" + suf, true);
						BuildChain(pre + "#" + (len - 1) + "#->_f(#" + (len - 2) + "#)" + suf, true);
					}

				}
			}
			else TestChain(chain);
		}

		static StreamWriter sw;

		const int MAX_CHAIN_LENGTH = 5;
		const int CHAINS_PER_METHOD = 1;
		const bool TEST_READ_UNKNOWN = true;

		static int ExpressionCounter = 0;
		static string FunctionName;

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			sw = new StreamWriter("chains.php");

			sw.WriteLine("<?");
			sw.WriteLine("function _f($_a) { }");
			sw.WriteLine("function _fr(&$_a) { }");
			sw.WriteLine("class A { static $x; function _f() { } };");

			for (int i = 1; i < MAX_CHAIN_LENGTH; i++)
			{
				BuildChain("#" + i + "#", false);
			}

			if (ExpressionCounter > 0) EndFunction();
			sw.WriteLine("?>");

			sw.Close();
		}
	}
}
