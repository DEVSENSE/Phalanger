' This project is here in order to test PHP CodeDOM implementation.
' This project will be deleted after implementation of PHP CodeDOM will be finished and tested.

Imports System.CodeDom, System.CodeDom.Compiler
Imports PHP.Core.CodeDom

Module Test
    ''' <summary>Path of parsed file</summary>
    Private file$ = "Test.php"
    ''' <summary>Parser</summary>
    Private cu As CodeCompileUnit
    ''' <summary>PHP provider</summary>
    Private ProviderPHP As CodeDomProvider = New PhpCodeProvider

    ''' <summary>Program needs the file 'Test.php' in its directory (if -i is not used)</summary>
    ''' <remarks>
    ''' Command line can be
    ''' <list type="table">
    ''' <item><term>'?'</term><description>Ask for language</description></item>
    ''' <item><term>'??'</term><description>Ask for languages in loop</description></item>
    ''' <item><term>empty</term><description>PHP</description></item>
    ''' <item><term>Language [Option]</term><description>Language name and optional option (options only for C++ and F#)</description></item>
    ''' <item><term>'-?' or '-h' or '/?' or '/h'</term><description>help</description></item>
    ''' </list>
    ''' Use -? or see Resources\Help.txt for more
    ''' </remarks>
    Sub Main()

        Console.Title = "Phalanger CodeDOM parser test"
        If My.Application.CommandLineArgs.Count > 0 AndAlso New List(Of String)(New String() {"-h", "-?", "/h", "/?"}).Contains(My.Application.CommandLineArgs(0)) Then
            Console.WriteLine(My.Resources.Help)
            If My.Application.CommandLineArgs.Count > 0 AndAlso ( _
                    My.Application.CommandLineArgs(My.Application.CommandLineArgs.Count - 1) = "wait" OrElse _
                    My.Application.CommandLineArgs(0).StartsWith("?")) Then _
                Console.ReadKey()
            Return
        End If
        If My.Application.CommandLineArgs.Count >= 2 AndAlso My.Application.CommandLineArgs(My.Application.CommandLineArgs.Count - 2).StartsWith("-i") AndAlso My.Application.CommandLineArgs(My.Application.CommandLineArgs.Count - 1) = "wait" Then
            file = My.Application.CommandLineArgs(My.Application.CommandLineArgs.Count - 2).Substring(2)
        ElseIf My.Application.CommandLineArgs.Count >= 1 AndAlso My.Application.CommandLineArgs(My.Application.CommandLineArgs.Count - 1).StartsWith("-i") Then
            file = My.Application.CommandLineArgs(My.Application.CommandLineArgs.Count - 1).Substring(2)
        End If

        If Not Parse() Then
            If My.Application.CommandLineArgs.Count > 0 AndAlso ( _
                    My.Application.CommandLineArgs(My.Application.CommandLineArgs.Count - 1) = "wait" OrElse _
                    My.Application.CommandLineArgs(0).StartsWith("?")) Then _
                Console.ReadKey()
            Environment.Exit(1)
            End
        End If

        Do
            'Reconstruct code back (optionally in different language)
            Try
                Dim tb As New Text.StringBuilder
                Dim OldFore As ConsoleColor = Console.ForegroundColor
                Dim OldBackground As ConsoleColor = Console.BackgroundColor
                Try
                    GetProvider().GenerateCodeFromCompileUnit(cu, New IO.StringWriter(tb), Nothing)
                    Console.Write(tb.ToString)
                Finally
                    Console.ForegroundColor = OldFore
                    Console.BackgroundColor = OldBackground
                End Try
            Catch ex As Exception
                Console.WriteLine()
                Console.WriteLine("There was an exception {0} when generating code: {1}", ex.GetType.FullName, ex.Message)
                Console.WriteLine(ex.StackTrace)
                Console.WriteLine()
            End Try
            If My.Application.CommandLineArgs.Count > 0 AndAlso My.Application.CommandLineArgs(0) = "??" Then Console.WriteLine()
        Loop While My.Application.CommandLineArgs.Count > 0 AndAlso My.Application.CommandLineArgs(0) = "??"
        If My.Application.CommandLineArgs.Count > 0 AndAlso My.Application.CommandLineArgs(My.Application.CommandLineArgs.Count - 1) = "wait" Then Console.ReadKey()
    End Sub
    Private Function ParseAlternative() As Boolean
        Static ParsedFile$
        If ParsedFile Is Nothing Then ParsedFile = file
AskForParser:
        Console.WriteLine("Chose parser (py; otherwise php):")
        Dim ParserName As String = Console.ReadLine
        Console.WriteLine("Type path to file to parse")
        Console.WriteLine("Or press enter to use {0}", ParsedFile)
        Console.WriteLine("Or type '?' to browse for file")
        Dim pFile As String = Console.ReadLine
        If pFile = "?" Then
            Dim dlg As New System.Windows.Forms.OpenFileDialog
            dlg.FileName = ParsedFile
            If dlg.ShowDialog <> Windows.Forms.DialogResult.OK Then GoTo AskForParser
            ParsedFile = dlg.FileName
        ElseIf pFile <> "" Then
            ParsedFile = pFile
        End If
        Dim ParserProvider As CodeDomProvider
        Select Case ParserName.ToLower
            Case "py", "python", "iron python", "ironpython"
                ParserProvider = New IronPython.CodeDom.PythonProvider
            Case Else 'php
                ParserProvider = ProviderPHP
        End Select
        Console.WriteLine("Parsing...")
        Try
            Using r As IO.StreamReader = My.Computer.FileSystem.OpenTextFileReader(ParsedFile)
                cu = ParserProvider.Parse(r)
            End Using
            Console.WriteLine(" done")
        Catch ex As Exception
            Console.WriteLine("Error {0} while parsing {1}", ex.GetType.Name, file)
            Console.WriteLine(ex.Message)
            'Console.WriteLine(ex.StackTrace)
            Return False
        End Try
        Return True
    End Function
    Private Function Parse() As Boolean
        Console.Write("Parsing...")
        Try
            Using r As IO.StreamReader = My.Computer.FileSystem.OpenTextFileReader(file)
                cu = ProviderPHP.Parse(r)
            End Using
            Console.WriteLine(" done")
        Catch ex As Exception
            Console.WriteLine("Error {0} while parsing {1}", ex.GetType.Name, file)
            Console.WriteLine(ex.Message)
            'Console.WriteLine(ex.StackTrace)
            Return False
        End Try
        Return True
    End Function
    ''' <summary>Return <see cref="CodeDomProvider"/> depending on command line</summary>
    Private Function GetProvider() As CodeDomProvider
        Dim C1$ = "//", C2$ = ""
        GetProvider = Nothing
        Dim Provider As Providers
        Try
            If My.Application.CommandLineArgs.Count = 0 Then
                Provider = Providers.Pahalanger
                Return New PhpCodeProvider
            Else
                Dim Arguments As List(Of String)
                If My.Application.CommandLineArgs(0).StartsWith("?") Then
Chose:              Console.WriteLine("Type language (vb, c#, c++, js, j#, msil, f#, py; otherwise php; empty for immediate exit)")
                    Console.WriteLine("Hold Shift to reload and reparse default input; CapsLock to change parser and input.")
                    Arguments = New List(Of String)
                    Arguments.Add(Console.ReadLine())
                    Dim Shift As Boolean = My.Computer.Keyboard.ShiftKeyDown
                    Dim Caps As Boolean = My.Computer.Keyboard.CapsLock
                    If Caps Then
                        If Not ParseAlternative() Then GoTo Chose
                    ElseIf Shift Then
                        If Not Parse() Then GoTo Chose
                    End If
                    If Arguments(0) = "" Then Environment.Exit(0)
                    Select Case Arguments(0)
                        Case "c++", "cpp", "vc", "vc++", "vcpp", "c"
                            Console.WriteLine("Type additional langaage features")
                            Console.WriteLine(vbTab & "7" & vbTab & GetType(Microsoft.VisualC.CppCodeProvider7).FullName)
                            Console.WriteLine(vbTab & "vs" & vbTab & GetType(Microsoft.VisualC.VSCodeProvider).FullName)
                            Console.WriteLine(vbTab & "<other>" & vbTab & GetType(Microsoft.VisualC.CppCodeProvider).FullName)
                            Arguments.Add(Console.ReadLine())
                        Case "f#", "fs"
                            Console.WriteLine("Type additional language features")
                            Console.WriteLine(vbTab & "asp" & vbTab & GetType(Microsoft.FSharp.Compiler.CodeDom.FSharpAspNetCodeProvider).FullName)
                            Console.WriteLine(vbTab & "<other>" & vbTab & GetType(Microsoft.FSharp.Compiler.CodeDom.FSharpCodeProvider).FullName)
                            Arguments.Add(Console.ReadLine())
                    End Select
                    Else
                        Arguments = New List(Of String)(My.Application.CommandLineArgs)
                    End If
                    Select Case Arguments(0).ToLower
                        Case "vb", "visual basic", "basic"
                            'Standard part of .NET
                            C1 = "'" : C2 = ""
                            Provider = Providers.VisualBasic
                            Return New Microsoft.VisualBasic.VBCodeProvider
                        Case "cs", "c#"
                            'Standard part of .NET
                            Provider = Providers.CSharp
                            Return New Microsoft.CSharp.CSharpCodeProvider
                        Case "py", "python", "ironpython", "iron python"
                            'Part of VS SDK
                            C1 = "#" : C2 = ""
                            Provider = Providers.IronPython
                            Return New IronPython.CodeDom.PythonProvider
                        Case "c++", "cpp", "vc", "vc++", "vcpp", "c"
                            'C:\Program Files\Microsoft Visual Studio 8\Common7\IDE\PublicAssemblies\Microsoft.VisualC.VSCodeProvider.dll
                            'C:\Program Files\Microsoft Visual Studio 8\Common7\IDE\PublicAssemblies\CppCodeProvider.dll
                            Provider = Providers.CPP_CLI
                            If Arguments.Count = 1 Then
                                Return New Microsoft.VisualC.CppCodeProvider7
                            Else
                                Select Case Arguments(1).ToLower
                                    Case "7" : Return New Microsoft.VisualC.CppCodeProvider7
                                    Case "vs" : Return New Microsoft.VisualC.VSCodeProvider
                                    Case Else : Return New Microsoft.VisualC.CppCodeProvider
                                End Select
                            End If
                        Case "jscript", "javascript", "java script", "js"
                            'C:\WINDOWS\Microsoft.NET\Framework\v2.0.50727\Microsoft.JScript.dll
                            Provider = Providers.JavaScript
                            Return New Microsoft.JScript.JScriptCodeProvider
                        Case "j#", "jsl"
                            'C:\WINDOWS\Microsoft.NET\Framework\v2.0.50727\VJSharpCodeProvider.dll
                            Provider = Providers.JSharp
                            Return New Microsoft.VJSharp.VJSharpCodeProvider
                        Case "msil", "cil"
                            'Can be downloaded from http://www.microsoft.com/downloads/details.aspx?familyid=7e979ed3-416b-43b6-993b-308a160831b6&displaylang=en
                            Provider = Providers.MSIL
                            Return New Microsoft.Msil.MsilCodeProvider
                        Case "f#", "fs"
                            'Can be downloaded from http://research.microsoft.com/fsharp
                            C1 = "(*" : C2 = "*)"
                            Provider = Providers.FSharp
                            If Arguments.Count >= 2 AndAlso Arguments(1) = "asp" Then Return New Microsoft.FSharp.Compiler.CodeDom.FSharpAspNetCodeProvider
                            Return New Microsoft.FSharp.Compiler.CodeDom.FSharpCodeProvider
                        Case Else
                            Provider = Providers.Pahalanger
                            Return New PhpCodeProvider
                    End Select
                End If
        Finally
            If My.Application.CommandLineArgs.Count > 0 AndAlso My.Application.CommandLineArgs(0) = "??" Then Console.WriteLine()
            SetConsoleColor(Provider)
            Console.WriteLine("{0}Using {1}{2}", C1, CObj(GetProvider).GetType.FullName, C2)
        End Try
    End Function
    ''' <summary>Changes color of console by langiage</summary>
    ''' <param name="Provider">Language</param>
    Private Sub SetConsoleColor(ByVal Provider As Providers)
        Console.BackgroundColor = ConsoleColor.White
        Select Case Provider
            Case Providers.CPP_CLI
                Console.ForegroundColor = ConsoleColor.DarkMagenta
                Console.Title = "C++/CLI"
            Case Providers.CSharp
                Console.ForegroundColor = ConsoleColor.DarkGreen
                Console.Title = "C#"
            Case Providers.FSharp
                Console.ForegroundColor = ConsoleColor.DarkRed
            Case Providers.IronPython
                Console.Title = "Iron Python"
                Console.ForegroundColor = ConsoleColor.DarkGray
            Case Providers.JavaScript
                Console.Title = "Java Script"
                Console.ForegroundColor = ConsoleColor.DarkBlue
            Case Providers.JSharp
                Console.Title = "J#"
                Console.ForegroundColor = ConsoleColor.Red
            Case Providers.MSIL
                Console.Title = "MSIL (CIL)"
                Console.ForegroundColor = ConsoleColor.Black
            Case Providers.Pahalanger
                Console.Title = "Phalanger"
                Console.ForegroundColor = ConsoleColor.DarkYellow
            Case Providers.VisualBasic
                Console.Title = "Visual Basic"
                Console.ForegroundColor = ConsoleColor.Blue
        End Select
    End Sub
    ''' <summary>Recognized language</summary>
    Private Enum Providers
        Pahalanger
        VisualBasic
        CSharp
        CPP_CLI
        JavaScript
        IronPython
        MSIL
        FSharp
        JSharp
    End Enum
End Module
