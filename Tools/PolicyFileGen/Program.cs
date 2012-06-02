using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Reflection;
using System.Diagnostics;

namespace PolicyFileGen
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Phalanger Publisher Policy File generator");

            if (args.Length != 2 || (args.Length == 1 && (args[0] == "/?" || args[0] == "-?")))
            {
                Console.WriteLine("This tool generates publisher policy file from an auto-versioned assembly.");
                Console.WriteLine("This allows to increase versions of strongly signed assemblies while keeping the same configuration.");
                Console.WriteLine();
                Console.WriteLine("Usage: PolicyFileGen.exe AssemblyFile /out:PolicyFile");

                return;
            }

            // parse args:
            string assemblyFile = args[0];
            string policyFile = args[1];
            if (policyFile.StartsWith("/out:")) policyFile = policyFile.Substring("/out:".Length);

            assemblyFile = assemblyFile.Trim();
            policyFile = policyFile.Trim();

            // generate policy file
            GeneratePolicyFile(System.IO.Path.GetFullPath(assemblyFile), policyFile);
        }

        private static void GeneratePolicyFile(string assemblyFile, string outputPolicyFile)
        {
            var/*!*/ass = System.Reflection.Assembly.ReflectionOnlyLoadFrom(assemblyFile);
            var/*!*/pub = Resources.pub;

            var assName = ass.GetName(false);

            // PhpNetCore, Version=3.0.4402.27373, Culture=neutral, PublicKeyToken=0a8e8c4c76728c71
            var fullName = assName.FullName;
            var fullNameParts = fullName.Split(new char[] { ',', ' ', '=' }, StringSplitOptions.RemoveEmptyEntries);

            // pub.config parameters:
            var name = assName.Name;
            var newVersion = assName.Version;
            var oldVersion = new Version(newVersion.Major, 0, 0, 0);
            var culture = fullNameParts.GetItemAfter(x => x == "Culture");
            var publicKeyToken = fullNameParts.GetItemAfter(x => x == "PublicKeyToken");
            var architecture = assName.ProcessorArchitecture.ToString().Replace("MSIL", "anycpu");

            // replace the parameters:
            pub = pub
                .Replace("{name}", name)
                .Replace("{newVersion}", newVersion.ToString())
                .Replace("{oldVersion}", oldVersion.ToString())
                .Replace("{publicKeyToken}", publicKeyToken)
                .Replace("{culture}", culture);

            Debug.Assert(!pub.Contains("{"), "Some parameter was not replaced!");

            // save the pub.config
            System.IO.File.WriteAllText(outputPolicyFile, pub);
        }
    }

    internal static class ExtMethods
    {
        public static int FindFirstIndex<T>(this IEnumerable<T>/*!*/enumerator, Predicate<T>/*!*/predicate)
        {
            int i = 0;
            foreach (var x in enumerator)
            {
                if (predicate(x))
                    return i;

                i++;
            }

            return -1;
        }

        /// <summary>
        /// Enumerate through the <paramref name="enumerator"/> and return the element after first match.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerator"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static T GetItemAfter<T>(this IEnumerable<T>/*!*/enumerator, Predicate<T>/*!*/predicate)
        {
            bool found = false;
            foreach (var x in enumerator)
            {
                if (found)
                    return x;

                if (predicate(x))
                    found = true;
            }

            return default(T);
        }
    }
}
