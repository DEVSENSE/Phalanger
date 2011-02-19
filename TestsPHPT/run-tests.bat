@echo off

"C:\Windows\Microsoft.NET\Framework\v2.0.50727\phpc.exe" /target:exe run-tests.php /out:run-tests.exe
"C:\Program Files (x86)\Microsoft SDKs\Windows\v7.0A\Bin\corflags.exe" /32BIT+ run-tests.exe

del *.log *.csv 

REM run-tests.exe --web "http://localhost/phpt"

Pause
