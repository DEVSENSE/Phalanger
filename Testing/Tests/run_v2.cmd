@echo off
..\..\Tools\PhpNetTester.exe /compiler:..\..\Deployment\Debug\phpc.exe /php:..\..\Tools\PHP\php.exe
del /s *.pdb *.exe EmittedNodes.csv LibraryCalls.csv UnknownCalls.csv __input.txt Debug.log > nul

@echo Deleting empty log files...
for /f "delims=" %%i in ('dir /s/b/a-d *.log') do if %~zi==0 del %%i
pause
