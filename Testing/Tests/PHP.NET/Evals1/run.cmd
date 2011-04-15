..\..\..\..\Deployment\bin\phpc /pure /target:exe Test1.php /r:bin/Lib.dll /nowarn:20,24 >NUL
peverify bin\Test1.exe /nologo
bin\Test1.exe

pause