..\..\..\Tools\PhpNetTester\bin\Debug\PhpNetTester.exe /compiler:..\..\..\Deployment\Debug\phpc.exe /php:C:\Programs\Development\php\php-5.2.0\php.exe
del /s *.pdb *.exe EmittedNodes.csv LibraryCalls.csv UnknownCalls.csv __input.txt Debug.log > nul
pause
