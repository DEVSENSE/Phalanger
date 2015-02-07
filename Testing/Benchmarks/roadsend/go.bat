..\..\..\Deployment\Bin\phpc /debug- /config:App.config test_env.php
..\..\..\Tools\peverify bin\test_env.exe

REM x64 test run:
bin\test_env.exe

"C:\Program Files (x86)\Microsoft SDKs\Windows\v7.0A\Bin\corflags.exe" /32BIT+ bin\test_env.exe
REM x86 test run:
bin\test_env.exe

pause
