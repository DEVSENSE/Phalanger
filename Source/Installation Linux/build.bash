#!/bin/bash
phalanger_version="3.0.0"
package_version=""
architecture="i386"
cp "..\..\Deployment\Bin\PhpNetCore.IL.dll" "Debian package\usr\lib\phalanger\bin"
cp "..\..\Deployment\Bin\PhpNetCore.dll" "Debian package\usr\lib\phalanger\bin"
cp "..\..\Deployment\Bin\PhpNetClassLibrary.dll" "Debian package\usr\lib\phalanger\bin"
cp "..\..\Deployment\Bin\PhpNetXmlDom.dll" "Debian package\usr\lib\phalanger\bin"
dpkg -b "Debian package" "Deployment\Bin\Package\phalanger_$phalanger_version-$architecture"