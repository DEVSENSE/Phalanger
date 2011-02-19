@ilasm /nologo /quiet /dll /output:bin\Test-il.dll Test.il
@peverify /nologo bin\Test-il.dll
@pause