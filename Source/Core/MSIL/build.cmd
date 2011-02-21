rem CHANGED: ilasm /nologo /quiet /dll /key:../Core.snk /output:"%1" Assembly.il Utils.il PhpInterfaces.il
ilasm.exe /nologo /quiet /dll /key:../Core.snk /output:%1 Assembly.il Utils.il PhpInterfaces.il || exit 0

if %2 == Release (
gacutil /nologo -f -i %1
xcopy /q /y %1 ..\..\..\Deployment\Bin
rem xcopy /q /y Doc\PhpNetCore.IL.xml ..\..\..\Deployment\Bin
)  
if %2 == Debug (
gacutil /nologo -f -i %1
xcopy /q /y %1 ..\..\..\Deployment\Debug
)
if %2 == DebugNoGac (
xcopy /q /y %1 ..\..\..\Deployment\DebugNoGac
)