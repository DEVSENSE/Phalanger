PATH = %PATH%;"C:\Program Files (x86)\Microsoft SDKs\Windows\v7.0A\Bin\NETFX 4.0 Tools"

gacutil -f -i PhpNetCore.dll
gacutil -f -i PhpNetClassLibrary.dll

gacutil -f -i php4ts.dll
gacutil -f -i php5ts.dll

gacutil -f -i PhpNetXmlDom.dll
gacutil -f -i PhpNetCurl.dll
gacutil -f -i PhpNetGd2.dll
gacutil -f -i PhpNetMbString.dll
gacutil -f -i PhpNetIconv.dll
gacutil -f -i PhpNetSoap.dll
gacutil -f -i PhpNetMsSql.dll
gacutil -f -i PhpNetPDO.dll
gacutil -f -i PhpNetPDOSQLite.dll
gacutil -f -i PhpNetPDOSQLServer.dll
gacutil -f -i PhpNetSQLite.dll
gacutil -f -i PhpNetXml.dll
gacutil -f -i PhpNetZlib.dll
gacutil -f -i PhpNetZip.dll

gacutil -f -i MySql.Data.dll
gacutil -f -i PhpNetMySql.dll
gacutil -f -i PhpNetPDOMySql.dll
