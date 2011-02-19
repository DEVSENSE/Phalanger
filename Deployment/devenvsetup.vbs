'---------------------------------------------------------------------------------------------------
'
' Executes devenv.exe /setup
' Path to devenv.exe comes in Session.Property("CustomActionData").
'
'---------------------------------------------------------------------------------------------------

Dim shell

Set shell = CreateObject("WScript.Shell")
shell.Run Chr(34) & Session.Property("CustomActionData") & "devenv.exe" & Chr(34) & " /setup", 0, True
