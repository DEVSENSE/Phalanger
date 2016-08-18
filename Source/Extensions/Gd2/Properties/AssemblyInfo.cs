using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PHP.Core;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Phalanger Gd2")]
[assembly: AssemblyDescription("Phalanger Gd2 Extension")]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("2f442039-59e5-498b-918e-14824b80ca10")]

[assembly: PhpLibrary(typeof(PHP.Library.Gd2.GdLibraryDescriptor), "Gd", new string[] { "image", "gd", "exif" })]