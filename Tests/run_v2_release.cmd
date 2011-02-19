@echo off
..\Tools\PhpNetTester\bin\Debug\PhpNetTester.exe /compiler:..\Deployment\Bin\phpc.exe /php:..\Tools\PHP\php.exe
del /s *.pdb *.exe EmittedNodes.csv LibraryCalls.csv UnknownCalls.csv __input.txt Debug.log > nul
pause
