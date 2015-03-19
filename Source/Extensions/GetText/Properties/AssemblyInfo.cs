using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PHP.Core;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Phalanger GetText")]
[assembly: AssemblyDescription("Phalanger GetText Extension")]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("c083fd7e-9622-4984-a4fa-9ada254aa4d8")]

[assembly: PhpLibrary(typeof(PHP.Library.GetText.GetTextLibraryDescriptor), "GetText", new string[] { "gettext" })]