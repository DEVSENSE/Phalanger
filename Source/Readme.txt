This directory contains Phalanger source files.

Building Phalanger without VS integration
-----------------------------------------

1. Open Visual Studio with administration rights ( or turn off UAC )
2. Open Phalanger (Orcas).sln
3.  Build solution as:
     - Debug ... find the result in $/Deployment/Debug
     - Release ... find the result in $/Deployment/bin
4. Add configuration lines from $/Deployment/machine.config (Follow comentaries in this file!) 
   into C:\Windows\Microsoft.NET\Framework\v2.0.50727\CONFIG\machine.config file. 
   
   (when Phalanger runs on Windows x64)
   into C:\Windows\Microsoft.NET\Framework64\v2.0.50727\CONFIG\machine.config file. 
   



To enable debugging of compiler phpc in Visual Studio
-----------------------------------------

1. Set start up project to phpc 
2. Select phpc and open it's properties
3. Select Debug and fill into command line arguments:
    /target:exe test.php
4. Set the working directory to the location of your "test.php"
5. F5 to run Phalanger with debugging



To enable tests
---------------

1. Open project $/Tools/PhpNetTester/PhpNetTester.csproj
2. If you don't have F# extension for visual studio you will see error:

$\Tools\SilverlightSecurityverifie
r\SilverlightSecurityverifier.fsharpp cannot be opened because its
project type (Ash arpp) is not supported by this version of visual Studio.
To open it, please use a version that supports this type of project.

If you don't want to test Phalanger Silverlight it doesn't matter, click OK.

3. Batch Build PhpNetTester project
4. If you want to use your PHP, make sure php.ini has 
     short_open_tag = on
     display_errors = on
5. Alter $/Tests/run_v2.cmd so it has correct path en /php: argument to php.exe file
6. Run $/Tests/run_v2.cmd file
