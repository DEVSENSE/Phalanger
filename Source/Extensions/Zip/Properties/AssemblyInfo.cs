using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PHP.Core;

[assembly: AssemblyTitle("Phalanger PDO")]
[assembly: AssemblyDescription("Phalanger Managed Extension - Zip")]

[assembly: PhpLibrary(typeof(PHP.Library.Zip.ZipLibraryDescriptor), "Zip", new string[] { "zip" })]