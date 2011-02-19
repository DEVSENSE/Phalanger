'
' Removes funny start menu icons in a supplied package
' Called as a postbuild action for Phalanger.msi
'

Dim Arg, WI, DB, View, Record

Arg = WScript.Arguments(0)

Set WI = CreateObject("WindowsInstaller.Installer")
Set DB = WI.OpenDatabase(Arg, 1)

WScript.Echo "Removing shortcut icons in '" & Arg & "'..."

Set View = DB.OpenView("SELECT `Icon_` FROM `Shortcut`")
View.Execute

Do
	Set Record = View.Fetch
	If Record Is Nothing Then Exit Do

    If (Record.StringData(1) <> "") Then
        Record.StringData(1) = ""
        View.Modify 4, Record
    End If
Loop

View.Close
DB.Commit
