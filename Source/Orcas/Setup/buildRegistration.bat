:: Copyright (c) Microsoft Corporation. All rights reserved.
:: This code is licensed under the Visual Studio SDK license terms.
:: THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
:: ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
:: IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
:: PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

:: Build the binaries then extract their registry attributes as a WiX include file

MSBuild "..\Phalanger Orcas.sln" /p:Configuration=Release /p:RegisterOutputPackage=false

:: we don't have console window!
:: ..\..\..\Tools\Bin\regpkg.exe /codebase /root:Software\Microsoft\VisualStudio\9.0 /wixfile:.\IronPythonConsoleWindow.generated.wxi ..\bin\Release\IronPythonConsoleWindow.dll 

..\..\..\Tools\VsSdk\regpkg.exe /codebase /root:Software\Microsoft\VisualStudio\9.0 /wixfile:.\Phalanger.LanguageService.generated.wxi ..\bin\Release\Phalanger.LanguageService.dll
..\..\..\Tools\VsSdk\regpkg.exe /codebase /root:Software\Microsoft\VisualStudio\9.0 /wixfile:.\Phalanger.generated.wxi ..\bin\Release\PhalangerProject.dll