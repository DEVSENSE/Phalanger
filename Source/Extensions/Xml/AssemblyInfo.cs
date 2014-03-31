/*

 Copyright (c) 2005-2006 Tomas Matousek and Martin Maly.  

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
[assembly: AssemblyTitle("Phalanger Xml")]
[assembly: AssemblyDescription("Phalanger Managed Extension - Xml")]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("d28c1dec-ca2a-45d1-81dd-3288deb053a5")]

[assembly: PhpLibrary(typeof(PHP.Library.Xml.XmlLibraryDescriptor), "xml", new string[] { "xml" })]
