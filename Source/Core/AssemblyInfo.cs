/*

 Copyright (c) 2005-2006 Tomas Matousek, Ladislav Prosek, Vaclav Novak, Pavel Novak, Jan Benda and Martin Maly.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System.Reflection;
using System.Security;
using System.Runtime.CompilerServices;
using System;

[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("The Phalanger Project Team")]
[assembly: AssemblyProduct("Phalanger")]
[assembly: AssemblyCopyright("Copyright (c) 2004-2010 Tomas Matousek, Ladislav Prosek, Vaclav Novak, Pavel Novak, Jan Benda, Martin Maly, Tomas Petricek, Daniel Balas, Miloslav Beno, Jakub Misek")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
//[assembly: CLSCompliant(true)]

#if !SILVERLIGHT
[assembly: AssemblyTitle("Phalanger Core")]
[assembly: AssemblyDescription("Phalanger Core Functionality")]
//[assembly: AllowPartiallyTrustedCallers]
#else
[assembly: AssemblyTitle("Phalanger Core (Silverlight)")]
[assembly: AssemblyDescription("Phalanger Core Functionality (Silverlight)")]
#endif

[assembly: AssemblyVersion("3.0.0.0")]
[assembly: AssemblyFileVersion("3.0.0.3238")]

[assembly: InternalsVisibleTo("Phalanger.LanguageService, PublicKey=0024000004800000940000000602000000240000525341310004000001000100b74f6114dcb75b60485b38820a516d1592ba89587f8449feac300570596bddac07226721e06178a9d2f8fbf0887bde659421378186cf0bfa31908b8f1965cc2cebeba22c9b232fb6cf5183eb12588bbdd61f0df0b390352f8be981f950642fedb8ad7cb241808f233cecb8ebaa2eb45b657744e95200c51ec39b686c66ad2eb6")]