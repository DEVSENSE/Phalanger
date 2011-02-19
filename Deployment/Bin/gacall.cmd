PATH = %PATH%;"C:\Program Files\Microsoft SDKs\Windows\v6.0A\Bin\"

gacutil -f -i PhpNetClassLibrary.dll
gacutil -f -i PhpNetCore.dll
gacutil -f -i PhpNetCore.IL.dll
gacutil -f -i PhpNetMsSql.dll
gacutil -f -i PhpNetXmlDom.dll
gacutil -f -i PhpNetMemcached.dll
REM gacutil -f -i PhpNetMbstring.dll
gacutil -f -i ShmChannel.dll
gacutil -f -i php4ts.dll
gacutil -f -i php5ts.dll
