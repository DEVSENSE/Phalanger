/*

 Copyright (c) 2005-2006 Tomas Matousek.  

 This software is distributed under GNU General Public License version 2.
 The use and distribution terms for this software are contained in the file named LICENSE, 
 which can be found in the same directory as this file. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System.Reflection;
using System.Runtime.CompilerServices;
using PHP.Core;

[assembly: AssemblyTitle("Phalanger MySql")]
[assembly: AssemblyDescription("Phalanger Managed Extension - MySql")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("The Phalanger Project Team")]
[assembly: AssemblyProduct("Phalanger")]
[assembly: AssemblyCopyright("Copyright (c) 2005-2011 Tomas Matousek, Jakub Misek")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: AssemblyVersion("2.1.0.0")]
[assembly: AssemblyDelaySign(false)]

//#if DEBUG
[assembly: PhpLibrary(typeof(PHP.Library.Data.MySqlLibraryDescriptor), "MySql", new string[]{"mysql"})]
//#else
//[assembly: PhpLibrary(typeof(PHP.Library.Data.MySqlLibraryDescriptor), "MySql", false, true)]
//#endif