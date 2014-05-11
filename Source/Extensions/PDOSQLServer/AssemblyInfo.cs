using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PHP.Core;

[assembly: AssemblyTitle("Phalanger PDO SQLServer")]
[assembly: AssemblyDescription("Phalanger Managed Extension - PDO SQLServer")]

[assembly: PhpLibrary(typeof(PHP.Library.Data.PDOSQLServerLibraryDescriptor), "PDO SQLServer", new string[] { "pdo_sqlsrv" })]