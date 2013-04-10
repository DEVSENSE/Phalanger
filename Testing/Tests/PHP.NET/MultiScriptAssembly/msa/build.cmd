@rem ..\..\..\..\..\Deployment\Bin\phpc /target:dll /lang:PHP5 /static+ /debug+ /root:%~dp0 /recurse:%~dp0 /out:../bin/msa.dll
cd %~dp0
phpc /target:dll /root:. /recurse:. /out:..\bin\msa.dll /lang:PHP5 /static+ /debug+