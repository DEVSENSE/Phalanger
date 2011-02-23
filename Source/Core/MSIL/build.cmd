rem CHANGED: ilasm /nologo /quiet /dll /key:../Core.snk /output:"%1" Assembly.il Utils.il PhpInterfaces.il
ilasm.exe /nologo /quiet /dll /key:../Core.snk /output:%1 Assembly.il Utils.il PhpInterfaces.il || exit 0

gacutil /u PhpNetCore.IL 1>nul

if %2 == Release (
gacutil /nologo -f -i %1 1>nul
xcopy /q /y %1 ..\..\..\Deployment\Bin 1>nul
rem xcopy /q /y Doc\PhpNetCore.IL.xml ..\..\..\Deployment\Bin
)  
if %2 == Debug (
gacutil /nologo -f -i %1 1>nul
xcopy /q /y %1 ..\..\..\Deployment\Debug 1>nul
)
if %2 == DebugNoGac (
xcopy /q /y %1 ..\..\..\Deployment\Debug 1>nul
)