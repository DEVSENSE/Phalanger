@echo off
..\..\Tools\PhpNetTester.exe /compiler:..\..\Deployment\Debug\phpc.exe /php:..\..\Tools\PHP\php.exe %*
@pause
@rem Deleting output files...
del /s *.pdb *.exe *.log EmittedNodes.csv LibraryCalls.csv UnknownCalls.csv __input.txt Debug.log > nul
