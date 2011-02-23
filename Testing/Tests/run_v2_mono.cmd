@echo off
..\Tools\PhpNetTester\bin\Debug\PhpNetTester.exe "/loader:\Program Files\Mono-1.1.17\bin\mono.exe" /compiler:..\Deployment\Debug\phpc.exe /php:..\Tools\PHP\php.exe
del /s *.mdb *.exe EmittedNodes.csv LibraryCalls.csv UnknownCalls.csv __input.txt Debug.log > nul
pause
