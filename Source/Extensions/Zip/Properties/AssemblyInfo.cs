using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PHP.Core;

[assembly: AssemblyTitle("Phalanger PDO")]
[assembly: AssemblyDescription("Phalanger Managed Extension - Zip")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Phalanger")]
[assembly: AssemblyCopyright("Copyright (c) 2012 Damien DALY")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: AssemblyVersion("3.0.0.0")]
[assembly: AssemblyDelaySign(false)]

[assembly: PhpLibrary(typeof(PHP.Library.Zip.ZipLibraryDescriptor), "Zip", new string[] { "zip" })]