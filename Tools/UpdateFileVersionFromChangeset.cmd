call "%VS100COMNTOOLS%\vsvars32.bat"

REM determining changeset number ...
REM run this batch file from within root folder of your workspace

tf history . /r /noprompt /stopafter:1 /version:W > tf.output 2>nul
SET CHANGESET=0
FOR /F "tokens=1 skip=2" %%N IN (tf.output) DO @SET CHANGESET=%%N
del /F /Q tf.output

:start
if "%1"=="" (goto :main)
"%0\..\VersionReplacer.exe" "%1" %CHANGESET%
SHIFT /1
goto :start
:main
REM Done.