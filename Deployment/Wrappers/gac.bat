PATH = %PATH%;"C:\Program Files\Microsoft SDKs\Windows\v7.0A\Bin\"
PATH = %PATH%;"..\..\Tools"

for %%i in (*.dll) do @gacutil -f -i %%i
