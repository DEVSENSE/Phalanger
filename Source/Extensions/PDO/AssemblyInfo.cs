using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PHP.Core;

[assembly: AssemblyTitle("Phalanger PDO")]
[assembly: AssemblyDescription("Phalanger Managed Extension - PDO")]

[assembly: PhpLibrary(typeof(PHP.Library.Data.PDOLibraryDescriptor), "PDO", new string[] { "pdo" })]