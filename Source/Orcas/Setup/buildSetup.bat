:: Copyright (c) Microsoft Corporation. All rights reserved.
:: This code is licensed under the Visual Studio SDK license terms.
:: THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
:: ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
:: IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
:: PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

:: Build the binaries then build the WiX sources that install them

MSBuild "..\Phalanger Orcas.sln" /p:Configuration=Release /p:RegisterOutputPackage=false

set WIXDIR=..\..\..\Tools\Wix
set ObjDir=%~dp0Obj
set VariablesFile=%~dp0Variables.wxi

if not exist "%ObjDir%" mkdir "%ObjDir%"

%WIXDIR%\candle.exe -dVariablesFile="%VariablesFile%" -dProductLanguage=1033 -out "%ObjDir%\\" Integration.wxs PhalangerBinaries.wxs Product.wxs
%WIXDIR%\light.exe "%ObjDir%\Integration.wixobj" "%ObjDir%\PhalangerBinaries.wixobj" "%ObjDir%\Product.wixobj" %WIXDIR%\wixui.wixlib -out VSIIP.msi -loc %WIXDIR%\WixUI_en-us.wxl

copy VSIIP.msi ..\..\..\Deployment\Bin\Package\