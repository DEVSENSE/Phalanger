Dim customActionData, scriptMapEntry

' [ action (create|delete), InstallDir\, WindowsDir\, platform ('empty'|64) ]
customActionData = Split(Session.Property("CustomActionData"), "|")

'
' create or delete virtual web directories on IIS6
'
If customActionData(0) = "create" Then

    ' create 
    scriptMapEntry = ".php," & customActionData(2) & "Microsoft.NET\Framework" & customActionData(3) & "\v2.0.50727\aspnet_isapi.dll,5,GET,HEAD,POST,DEBUG"

    ASTCreateVirtualWebDir "localhost", "SimpleScripts", customActionData(1) & "WebRoot\Samples\SimpleScripts", "Simple scripts", scriptMapEntry
    ASTCreateVirtualWebDir "localhost", "Tests", customActionData(1) & "WebRoot\Samples\Tests", "Tests", scriptMapEntry
    ASTCreateVirtualWebDir "localhost", "Extensions", customActionData(1) & "WebRoot\Samples\Extensions", "Extensions", scriptMapEntry

ElseIf customActionData(0) = "delete" Then

    ' delete
    ASTDeleteVirtualWebDir "localhost", "SimpleScripts"
    ASTDeleteVirtualWebDir "localhost", "Tests"
    ASTDeleteVirtualWebDir "localhost", "Extensions"

End If


'---------------------------------------------------------------------------------------------------
'
' This function creates a virtual web directory on a computer using IIS6 (all webs) and sets it up
' for Phalanger (default documents, script map, flags...)
'
'---------------------------------------------------------------------------------------------------
Sub ASTCreateVirtualWebDir(ComputerName, DirName, DirPath, AppName, ScriptMapEntry)
	On Error Resume Next

	Dim webSvc, webSite, vRoot, vDir, providerObj, num, scriptMaps, items, found, WshShell, Directory, defaultDoc
	
	set webSvc = GetObject("IIS://"&ComputerName&"/W3svc")
	if (Err <> 0) then
		exit sub
	end if

	for each webSite in webSvc
		If webSite.class = "IIsWebServer" then
			set vRoot = webSite.GetObject("IIsWebVirtualDir", "Root")
			If Err = 0 Then
				'Create the new virtual directory
				set vDir = vRoot.Create("IIsWebVirtualDir", DirName)
				If Err = 0 Then
					'Set the new virtual directory path and properties
					vDir.AccessFlags = 513 'read, script
					vDir.Name = DirName
					vDir.Path = DirPath
					vDir.EnableDefaultDoc = true
					vDir.DirBrowseFlags = &HC000003E ' browsing, date, time, size, extension, longdate
					
					'Update DefaultDoc
					vDir.DefaultDoc = vDir.DefaultDoc & ",index.php,default.php"

					'Save the changes
					vDir.SetInfo

					'Create web application
					vDir.AppCreate2(2)

					'Set application name
					vDir.AppFriendlyName = AppName
					
					'Update script map
					found = false
					scriptMaps = vDir.ScriptMaps
					For num = Lbound(scriptMaps) To UBound(scriptMaps)
						items = Split(scriptMaps(num), ",")
						If LCase(items(0)) = ".php" Then
							scriptMaps(num) = ScriptMapEntry
							found = true
						End If
					Next
					
					If found = false Then
						Redim preserve scriptMaps(Ubound(scriptMaps) + 1)
						scriptMaps(Ubound(scriptMaps)) = ScriptMapEntry
					End If
					
					vDir.ScriptMaps = scriptMaps
					
					vDir.AppPoolId = "Classic .NET AppPool"
					
					'Save the changes
					vDir.SetInfo
					
				End If
			End If
		End If
	Next
End Sub


'---------------------------------------------------------------------------------------------------
'
' This function deletes a virtual web directory on a computer using IIS6 (all webs).
'
'---------------------------------------------------------------------------------------------------
Sub ASTDeleteVirtualWebDir(ComputerName, DirName)
	On Error Resume Next

	Dim webSvc, webSite, vRoot
	
	set webSvc = GetObject("IIS://"&ComputerName&"/W3svc")
	if (Err <> 0) then
		exit sub
	end if

	for each webSite in webSvc
		if webSite.class = "IIsWebServer" then
			set vRoot = webSite.GetObject("IIsWebVirtualDir", "Root")
			If Err = 0 Then
				'Delete the virtual directory
				vRoot.Delete "IIsWebVirtualDir", DirName
			End If
		End if
	next
End Sub