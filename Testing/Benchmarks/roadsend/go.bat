..\..\..\Deployment\Bin\phpc /debug- /config:App.config test_env.php
..\..\..\Tools\peverify bin\test_env.exe
bin\test_env.exe results.csv

"C:\Program Files (x86)\Microsoft SDKs\Windows\v7.0A\Bin\corflags.exe" /32BIT+ bin\test_env.exe
bin\test_env.exe results.csv

pause
