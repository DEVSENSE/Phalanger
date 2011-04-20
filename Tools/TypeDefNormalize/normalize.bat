@echo off
if [%1]==[/?] goto :Usage
if [%1]==[] goto :Usage
if [%2]==[] goto :Usage

:Main
for /f %%a IN ('dir /b %1\*.xml') do call xslttrans sort.xslt %1\%%a %2\%%a
goto :eof

:Usage
	echo.
	echo.   %~n0: Normalize typedef xml files (sorts class/function definitins)
	echo.	Usage: %~n0 [typedef_directory] [normalized_typedef_directory]
	echo.	Ex.:   %~n0 Typedefs NormalizedTypedefs
	echo.
	goto :eof
