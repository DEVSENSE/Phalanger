/*

 Copyright (c) 2005-2006 Tomas Matousek, Ladislav Prosek, Vaclav Novak, Pavel Novak, Jan Benda and Martin Maly.
 Copyright (c) 2006-2012 DEVSENSE s.r.o.

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
[assembly: AssemblyFileVersion("3.0.0.0")]

//[assembly: InternalsVisibleTo("PhpNetClasslibrary, PublicKey=0024000004800000940000000602000000240000525341310004000001000100611b1c313d77d51b5ac4d5b309e8712919634a716ae826dd133e722fe5e4f10012a8b96c40b7098d669ac5f78581b83cfa412d1a436a65450fac212d0d2dca824f8b1ab51b98af6d44d14ffd9a7aacd21e23557971564886df047070ca34d51869f3eddfb343739ee014e1b117772885fbc0758232461c5db7c659ca98b981a9")]
[assembly: InternalsVisibleTo("Phalanger.LanguageService, PublicKey=0024000004800000940000000602000000240000525341310004000001000100b74f6114dcb75b60485b38820a516d1592ba89587f8449feac300570596bddac07226721e06178a9d2f8fbf0887bde659421378186cf0bfa31908b8f1965cc2cebeba22c9b232fb6cf5183eb12588bbdd61f0df0b390352f8be981f950642fedb8ad7cb241808f233cecb8ebaa2eb45b657744e95200c51ec39b686c66ad2eb6")]
[assembly: InternalsVisibleTo("ControlFlow, PublicKey=00240000048000009400000006020000002400005253413100040000010001007f76493bf62c3a11fc4aedde33c9767dc38c5300dd9c3e7df13a766f4a0abc85fdee440584ba1d122cc9a0d220a78dd3532b4d7e4a4365d5347d183fc9bceacdc18336c66bb71b5f0a02ede53a080136f39f44482c35a4f96c8dece4a9953c4ec7234e6609d57754c1069efcd96f1c3e32be0cb8f3a97f48a426472e5e0c02ba")]
[assembly: InternalsVisibleTo("ControlFlow.Tests, PublicKey=00240000048000009400000006020000002400005253413100040000010001003d5731df28b262d8074a2f2b55dcf9a636fc1f2d3728dcad1f79f62d6f1bbccf2e3c958db6d2fe63091addd056ffb2bed42f585b8e1b15e2d86d6514946bd97a9e52e43ac6c93282e68e6253e3491fd300f07d817b523bd772697fd40e1ed488cf7808724dfaacb8bfead864a084a62ed13a367ef066d69649e5a427ea8d49be")]
