/*

 Copyright (c) 2004-2006 Tomas Matousek, Ladislav Prosek, Pavel Novak, Jan Benda.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System.Reflection;
using System.Runtime.CompilerServices;
using PHP.Core;
using System.Security;

#if !SILVERLIGHT
[assembly: AssemblyTitle("Phalanger Base Class Library")]
[assembly: AssemblyDescription("Phalanger Base Class Library")]
[assembly: AssemblyVersion("3.0.0.0")]
//[assembly: AllowPartiallyTrustedCallers]

//#if DEBUG
[assembly: PhpLibrary(
    typeof(PHP.Library.LibraryDescriptor),
    "Base Library",
    new string[]{"standard","Core","session","ctype","tokenizer","date","pcre","ereg","json","hash","SPL","filter"})]
//#else
//[assembly: PhpLibrary(typeof(PHP.Library.LibraryDescriptor), "Base Library", false, true)]
//#endif

#else
[assembly: AssemblyTitle("Phalanger Base Class Library (Silverlight)")]
[assembly: AssemblyDescription("Phalanger Base Class Library (Silverlight)")]
[assembly: AssemblyVersion("3.0.0.0")]
[assembly: PhpLibrary(typeof(PHP.Library.LibraryDescriptor), "Base Library")]
#endif

[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("The Phalanger Project Team")]
[assembly: AssemblyProduct("Phalanger")]
[assembly: AssemblyCopyright("Copyright (c) 2004-2010 Tomas Matousek, Ladislav Prosek, Pavel Novak, Jan Benda, Tomas Petricek, Daniel Balas, Miloslav Beno, Jakub Misek")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]		
[assembly: AssemblyDelaySign(false)]