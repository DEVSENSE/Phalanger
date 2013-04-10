cd %~dp0
PATH = %PATH%;"C:\Program Files (x86)\Microsoft SDKs\Windows\v7.0A\Bin\NETFX 4.0 Tools"

gacutil /u PhpNetCore
gacutil /u PhpNetClassLibrary

gacutil /u php4ts
gacutil /u php5ts

gacutil /u PhpNetXmlDom
gacutil /u PhpNetMsSql
gacutil /u PhpNetPDO
gacutil /u PhpNetPDOSQLite
gacutil /u PhpNetPDOSQLServer
gacutil /u PhpNetSQLite
gacutil /u PhpNetZip
