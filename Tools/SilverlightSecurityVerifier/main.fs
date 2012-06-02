#light

open System
open System.IO
open System.Reflection
open System.Reflection.Emit
open System.Collections.Generic

open SilverlightSecurityVerifier
open SilverlightSecurityVerifier.Reflection
open SilverlightSecurityVerifier.Patterns

let slPath = @"C:\Tomas\Projects\Phalanger\Source\Tools\SilverlightSecurityVerifier\bin"
let slAssemblies =
  [| for fn in [| "agclr.dll"; "mscorlib.dll"; "system.dll"; "system.core.dll"; 
                  "system.silverlight.dll"; "system.xml.core.dll" |] 
     -> Assembly.LoadFile(Path.Combine(slPath, fn)) |]
     
let isSilverlightAssembly (asm:Assembly) =
  slAssemblies |> Array.exists (fun sla -> asm = sla)
   
let callsCount = ref 0      
let clsCount = ref 0
let methCount = ref 0
do 
  for path in [| @"C:\Tomas\Projects\Phalanger\Source\Tools\SilverlightSecurityVerifier\bin\PhpNetCore.dll"
                 @"C:\Tomas\Projects\Phalanger\Source\Tools\SilverlightSecurityVerifier\bin\PhpNetClassLibrary.dll" |] do
    let asm = Assembly.LoadFile(path)
    asm |> Reflection.iterTypes (fun ty ->
      clsCount := !clsCount + 1
      printfn "Analyzing class '%s'" ty.FullName
      ty |> Reflection.iterMethods (fun mi ->
        if (mi.Name = "InitConstants") then
          printf "Yep!"
        methCount := !methCount + 1
        let body = mi.GetMethodBody()
        if (body <> null) then
          mi |> Reflection.iterOpCodes (function
            | Call(op, InlineMethod(mc)) 
            | Calli(op, InlineMethod(mc))
            | Callvirt(op, InlineMethod(mc)) -> 
                if (isSilverlightAssembly mc.DeclaringType.Assembly) then
                  callsCount := !callsCount + 1
                  () //printfn " '%s' calling: %s, %s" mi.Name mc.Name mc.DeclaringType.Assembly.FullName
            | _ -> ()
          )
        ) 
      )
printf "Classes: %i\nMethods: %i\nCalls: %i" (!clsCount) (!methCount) (!callsCount)      
Console.ReadLine()    