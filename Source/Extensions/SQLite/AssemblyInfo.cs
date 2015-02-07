using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PHP.Core;

[assembly: AssemblyTitle("Phalanger MySql")]
[assembly: AssemblyDescription("Phalanger Managed Extension - SQLite")]

[assembly: PhpLibrary(typeof(PHP.Library.Data.SQLiteLibraryDescriptor), "SQLite", new string[] { "sqlite" })]