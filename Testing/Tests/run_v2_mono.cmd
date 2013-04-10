@echo off
..\..\Tools\PhpNetTester.exe "/loader:\Program Files\Mono-1.1.17\bin\mono.exe" /compiler:..\..\Deployment\Bin\phpc.exe /php:..\..\Tools\PHP\php.exe %*

@pause
@rem Deleting output and temporary files...
del /s *.pdb *.exe *.log EmittedNodes.csv LibraryCalls.csv UnknownCalls.csv *.phpscript > nul
