On Error Resume Next

Dim customActionData, oWebAdmin, oApps
Set oWebAdmin = GetObject("winmgmts:root\WebAdministration")

' [ action (create|delete), InstallDir\ ]
customActionData = Split(Session.Property("CustomActionData"), "|")

' Define the SiteName for the new application.
strSiteName = "Default Web Site"

'
' create or delete virtual web directories on IIS7
'
If customActionData(0) = "create" Then

    ' ensure IIS-WebServerManagementTools is installed
    Set WshShell = WScript.CreateObject("WScript.Shell")
    WshShell.Run "pkgmgr /iu:IIS-WebServerManagementTools;IIS-ManagementScriptingTools;IIS-ManagementConsole;", 1, true

    ' create 
    oWebAdmin.Get("Application").Create "/PhalangerTestsSample", strSiteName, customActionData(1) & "WebRoot\Samples\Tests\"
    oWebAdmin.Get("Application").Create "/PhalangerFormsSample", strSiteName, customActionData(1) & "WebRoot\Samples\ASP.NET\FormsAuth\"

    If Err.Number <> 0 Then
        MsgBox "An error occured while configuring IIS7 Web Site. Ensure IIS7 is present and the installer runs with Administrator rights or UAC disabled. The installation process will now continue."
    End If

ElseIf customActionData(0) = "delete" Then

    ' delete
    Set oApps = oWebAdmin.InstancesOf("Application")
    For Each oApp In oApps
        If ( (oApp.Path = "/PhalangerTestsSample" OR oApp.Path = "/PhalangerFormsSample") AND oApp.SiteName = strSiteName) Then
            oApp.Delete_
        End If
    Next    
    

End If