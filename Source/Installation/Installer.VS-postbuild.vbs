'
' Adjusts the install sequence and adds a new nested MSI custom action
' to install ProjectAggregator2.msi placed in the substorage of a supplied package
' Called as a postbuild action for VSIntegration.msi
'

Dim Arg, WI, DB, View, Record

Arg = WScript.Arguments(0)
ProjectRoot = WScript.Arguments(1)

Set Shell = CreateObject("WScript.Shell")

WScript.Echo "Embedding ProjectAggregator2.msi..."
Shell.Run Chr(34) & ProjectRoot & "Tools\MsiDb" & Chr(34) & " -d " & Chr(34) & Arg & Chr(34) _
    & " -r " & Chr(34) & ProjectRoot & "Deployment\ProjectAggregator2.msi" & Chr(34)


' WAP NOT NEEDED FOR SP1
'
'
 MsgBox "WAP"
' WScript.Echo "Embedding WebApplicationProjectSetup.msi..."
' Shell.Run Chr(34) & ProjectRoot & "Tools\MsiDb" & Chr(34) & " -d " & Chr(34) & Arg & Chr(34) _
'    & " -r " & Chr(34) & ProjectRoot & "Deployment\WebApplicationProjectSetup.msi" & Chr(34)

 MsgBox Arg
Set WI = CreateObject("WindowsInstaller.Installer")
Set DB = WI.OpenDatabase(Arg, 1)

WScript.Echo "Resequencing devenv /setup uninstall action..."
Set View = DB.OpenView("SELECT `Sequence` FROM `InstallExecuteSequence` WHERE `Condition`='REMOVE~=" _
    & Chr(34) & "ALL" & Chr(34) & " AND ProductState <> 1'")
View.Execute

Do
    Set Record = View.Fetch
    If Record Is Nothing Then Exit Do

    Record.IntegerData(1) = Record.IntegerData(1) + 4298
    View.Modify 4, Record
Loop
View.Close

WScript.Echo "Adding ProjectAggregator2.msi nested install action..."
Set View = DB.OpenView("INSERT INTO `CustomAction` (`Action`,`Type`,`Source`) " _
    & "VALUES ('InstallAggregator','7','ProjectAggregator2.msi')")
View.Execute
View.Close

Set View = DB.OpenView("INSERT INTO `InstallExecuteSequence` (`Action`,`Condition`,`Sequence`)" _
    & "VALUES ('InstallAggregator','NOT REMOVE~=" & Chr(34) & "ALL" & Chr(34) & "','5994')")
View.Execute
View.Close

'
' WAP NOT NEEDED FOR SP1
'
'
' WScript.Echo "Adding WebApplicationProjectSetup.msi nested install action..."
' Set View = DB.OpenView("INSERT INTO `CustomAction` (`Action`,`Type`,`Source`) " _
'     & "VALUES ('InstallWebApplicationProject','7','WebApplicationProjectSetup.msi')")
' View.Execute
' View.Close
 
' Set View = DB.OpenView("INSERT INTO `InstallExecuteSequence` (`Action`,`Condition`,`Sequence`)" _
'     & "VALUES ('InstallWebApplicationProject','LEGACYWEB AND NOT REMOVE~=" & Chr(34) & "ALL" & Chr(34) & "','5995')")
' View.Execute
' View.Close

DB.Commit
