using System;
using PHP.Core;
using PHP.Library;

namespace ExtensionSamples
{
	/// <summary>
	/// Uses the php_zlib extension to create a .gz file in current directory and
	/// compress a string.
	/// </summary>
	class ZlibSample
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			string filename = "zlibtest.gz";
			string s = "Only a test, test, test, test, test, test, test, test!\n";

			// open file for writing with maximum compression
			PhpResource zp = Zlib.gzopen(new PhpBytes(filename), new PhpBytes("w9"));

			// write string to file
			Zlib.gzwrite(zp, new PhpBytes(s));

			// close file
			Zlib.gzclose(zp);

			// open file for reading
			zp = Zlib.gzopen(new PhpBytes(filename), new PhpBytes("r"));

			// output contents of the file and close it
			Console.WriteLine(Zlib.gzread(zp, 128));
			Zlib.gzclose(zp);

			Console.ReadLine();
		}
	}
}
