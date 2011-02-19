#light

open System
open System.IO
open System.Reflection
open System.Reflection.Emit
open System.Collections.Generic

open SilverlightSecurityVerifier
open SilverlightSecurityVerifier.Reflection
open SilverlightSecurityVerifier.Patterns

let classes = new List<_>()
do 
  for path in [| @"C:\tomas\Projects\Phalanger\Source\Deployment\SilverlightBin\PhpNetCore.dll"
                 @"C:\tomas\Projects\Phalanger\Source\Deployment\SilverlightBin\PhpNetClassLibrary.dll" |] do
    let asm = Assembly.LoadFile(path)
    let totalAsmSize = ref 0
    asm |> Reflection.iterTypes (fun ty ->
      let totalSize = ref 0
      ty |> Reflection.iterMethods (fun mi ->
        let body = mi.GetMethodBody()
        if (body <> null) then
          totalSize := (!totalSize) + body.GetILAsByteArray().Length
      ) 
      classes.Add( (!totalSize, ty.FullName) )
      totalAsmSize := (!totalAsmSize) + (!totalSize)
      //printfn "Class '%s' has size '%d'" ty.FullName (!totalSize)
    )
    let res = asm.GetManifestResourceNames()
    let totalRes = res |> Array.fold_left(fun tot r -> tot + int (asm.GetManifestResourceStream(r).Length)) 0
    let fileSize = use sr = File.OpenRead(path) in sr.Length
    Console.WriteLine("Assembly: {0}\n - IL size:    {1,12}\n - Resources:  {2,12}\n - File size:  {3,12}",
                      Path.GetFileName(asm.CodeBase), !totalAsmSize, totalRes, fileSize)
    
  printfn "\n===== Class report ====="
  classes.Sort()
  for i = 0 to 100 do
    let size,name = classes.[classes.Count - 1 - i]
    printfn "%d %s" size name 
    
      
//printf "Classes: %i\nMethods: %i\nCalls: %i" (!clsCount) (!methCount) (!callsCount)      
Console.ReadLine()    