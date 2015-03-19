@echo off

REM run this batch file with the path of your root folder as the first argument,
REM or from within the root folder.

call "%VS140COMNTOOLS%\vsvars32.bat"

REM when the second argument is blank we will assume we're already in the project root.
if %2=="" (goto :getversion)
REM moving to the root specified in the first argument
cd %1
REM removing the first argument from the list
SHIFT /1

:getversion
REM determining changeset number ...
tf history . /r /noprompt /stopafter:1 /version:W > tf.output 2>nul

if not [%ERRORLEVEL%]==[0] (goto :fail)

SET CHANGESET=0
FOR /F "tokens=1 skip=2" %%N IN (tf.output) DO @SET CHANGESET=%%N
del /F /Q tf.output
if [%CHANGESET%]==[0] (goto :done)

:start
if %1=="" (goto :done)
"%0\..\VersionReplacer.exe" %1 %CHANGESET%
SHIFT /1
goto :start

:done
REM Done.

:fail
REM we hit an error running the tf program. exiting with a nice message
echo "Oh dear, it seems your environment isn't set-up properly for teamfoundation."
