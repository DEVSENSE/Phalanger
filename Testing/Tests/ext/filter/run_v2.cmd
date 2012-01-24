@echo off
..\..\..\..\Tools\PhpNetTester.exe /compiler:..\..\..\..\Deployment\Debug\phpc.exe /php:..\..\..\..\Tools\PHP\php.exe
del /s *.pdb *.exe EmittedNodes.csv LibraryCalls.csv UnknownCalls.csv __input.txt Debug.log > nul
pause
