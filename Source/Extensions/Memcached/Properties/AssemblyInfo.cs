using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PHP.Core;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("PhpNetMemcached extension")]
[assembly: AssemblyDescription("Phalanger Managed Extension - Memcached")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("The Phalanger Project Team")]
[assembly: AssemblyProduct("Phalanger")]
[assembly: AssemblyCopyright("Copyright © Daniel Balas, Jakub Misek")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: AssemblyVersion("2.1.0.0")]
[assembly: AssemblyKeyName("")]

[assembly: PhpLibrary(typeof(PHP.Library.Memcached.MemcachedLibraryDescriptor), "Memcached", new string[] { "memcached" })]