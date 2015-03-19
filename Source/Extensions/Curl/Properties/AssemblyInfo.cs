using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PHP.Core;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Phalanger Curl")]
[assembly: AssemblyDescription("Phalanger Curl Extension")]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("47fb135d-f29f-42de-9435-5ee36249648e")]

[assembly: PhpLibrary(typeof(PHP.Library.Curl.CurlLibraryDescriptor), "Curl", new string[] { "curl" })]
