using System;

namespace UseMultiScriptAssembly
{
    using PHP.Core;

    class Program
    {
        static void Main(string[] args)
        {
            ScriptContext context = ScriptContext.InitApplication(ApplicationContext.Default, null, null, null);

            // redirect PHP output to the console:
            context.Output = Console.Out; // Unicode text output
            context.OutputStream = Console.OpenStandardOutput(); // byte stream output

            context.Include("main.php", true);

            var klass = (PhpObject)context.NewObject("Klass", new object[] { "yipppy" });
            var foo = new PhpCallback(klass, "foo");
            foo.Invoke(null, new object[] { "param" });
        }
    }
}
