PATH = %PATH%;"C:\Program Files\Microsoft SDKs\Windows\v6.0A\Bin\"

for %%i in (*.dll) do @gacutil -f -i %%i
