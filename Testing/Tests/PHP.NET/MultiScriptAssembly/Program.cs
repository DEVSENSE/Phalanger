﻿using System;
using System.IO;
using System.Text;

namespace UseMultiScriptAssembly
{
    using PHP.Core;

    class Program
    {
        static void Main(string[] args)
        {
            ScriptContext context = ScriptContext.InitApplication(ApplicationContext.Default, null, null, null);

            var sb = new StringBuilder();
            using (TextWriter tw = new StringWriter(sb))
            {
                context.Output = tw;
                context.OutputStream = Console.OpenStandardOutput(); //TODO: Should also redirect binary output.

                context.Include("main.php", true);

                var klass = (PhpObject)context.NewObject("Klass", new object[] { "yipppy" });
                var foo = new PhpCallback(klass, "foo");
                foo.Invoke(null, new object[] { "param" });

                tw.Close();
            }

            string output = sb.ToString();
            const string EXPECTED = "yipppyparam";
            if (output != EXPECTED)
            {
                Console.WriteLine("FAIL");
                Console.Write("Expected: " + EXPECTED);
                Console.Write("Got: ");
                Console.WriteLine(output);
            }
            else
            {
                Console.WriteLine("PASS");
            }
        }
    }
}
