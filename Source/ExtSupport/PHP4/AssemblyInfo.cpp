/*

 Copyright (c) 2004-2006 Ladislav Prosek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

#include "stdafx.h"

using namespace System::Reflection;
using namespace System::Runtime::CompilerServices;

[assembly:AssemblyTitleAttribute("Phalanger PHP4/PHP5 ExtSupport")];
[assembly:AssemblyDescriptionAttribute("Zend API v20100221 Extension Support")];
[assembly:AssemblyConfigurationAttribute("")];
[assembly:AssemblyCompanyAttribute("The Phalanger Project Team")];
[assembly:AssemblyProductAttribute("Phalanger")];
[assembly:AssemblyCopyrightAttribute("Copyright (c) 2004-2010 Tomas Matousek, Ladislav Prosek, Vaclav Novak, Pavel Novak, Jan Benda, Martin Maly")];
[assembly:AssemblyTrademarkAttribute("")];
[assembly:AssemblyCultureAttribute("")];		
[assembly:AssemblyVersionAttribute("3.0.0.0")];

// The .snk file is specified in Linker property pages (it avoids problems with post-processing
// the already-signed assembly with mt.exe).
// http://msdn2.microsoft.com/en-us/library/ms235305.aspx

//[assembly:AssemblyDelaySignAttribute(false)];
//[assembly:AssemblyKeyFileAttribute("ExtSupport.snk")];
//[assembly:AssemblyKeyNameAttribute("")];
