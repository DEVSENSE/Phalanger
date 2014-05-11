/*

 Copyright (c) 2012 Jakub Misek

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PHP.Core;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Phalanger Iconv")]
[assembly: AssemblyDescription("Phalanger Managed Extension - Iconv")]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("E19A61F5-A199-4EBB-9F1F-7CF832B20099")]

[assembly: PhpLibrary(typeof(PHP.Library.Iconv.IconvLibraryDescriptor), "iconv", new string[] { "iconv" })]
