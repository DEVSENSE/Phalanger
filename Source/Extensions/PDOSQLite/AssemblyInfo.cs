using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PHP.Core;

[assembly: AssemblyTitle("Phalanger PDO SQLite")]
[assembly: AssemblyDescription("Phalanger Managed Extension - PDO SQLite")]

[assembly: PhpLibrary(typeof(PHP.Library.Data.PDOSQLiteLibraryDescriptor), "PDO SQLite", new string[] { "pdo_sqlite" })]