phpc /target:dll /static+ /root:. /recurse:ext/ /out:bin/ext.dll
phpc /target:exe /static+ main.php /r:bin/ext.dll /out:bin/main.exe

pause