@echo off
..\..\Tools\PhpNetTester.exe /compiler:..\..\Deployment\Bin\phpc.exe /php:..\..\Tools\PHP\php.exe %*

@pause
@rem Deleting output and temporary files...
del /s *.pdb *.exe *.log EmittedNodes.csv LibraryCalls.csv UnknownCalls.csv *.phpscript > nul
