@echo off

IF [%1]==[] GOTO NOARG

MD %0\..\tests
CD %0\..\tests

xcopy "%1\tests" "tests\" /S /Y
xcopy "%1\zend\tests" "zend\tests\" /S /Y

FOR /F %%G IN ('"dir %1\ext\ /A:D /B"') DO XCOPY "%1\ext\%%G\tests" "ext\%%G\tests\" /S /Y

pause
EXIT 1

:NOARG
ECHO ERROR: Please drag and drop whole PHP source directory onto this BAT file.
PAUSE
EXIT -1