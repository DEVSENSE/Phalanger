using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PHP.Core;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Phalanger Soap")]
[assembly: AssemblyDescription("Phalanger Managed Extension - Soap")]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("0f54482b-2272-49df-a07f-c29f60c26389")]

[assembly: PhpLibrary(typeof(PHP.Library.Soap.SoapLibraryDescriptor), "soap", new string[] { "soap" })]
