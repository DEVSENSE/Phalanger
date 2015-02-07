#light
module SilverlightSecurityVerifier.Reflection

#nowarn "57"
open System
open System.Reflection
open System.Reflection.Emit
open System.Collections.Generic

// --------------------------------------------------------------------------------------

/// Iterates over all types in a specified assembly
let iterTypes f (asm:Assembly) =
  asm.GetTypes() |> Array.iter f
  
/// Iterates over all methods in a type
/// including constructors & property getters/setters  
let iterMethods (f) (ty:Type) =
  let flags = BindingFlags.DeclaredOnly ||| BindingFlags.Instance ||| BindingFlags.Static 
              ||| BindingFlags.Public ||| BindingFlags.NonPublic 
  for ci in ty.GetConstructors(flags) do
    f (ci :> MethodBase)
  for mi in ty.GetMethods(flags) do
    f (mi :> MethodBase)
  for pi in ty.GetProperties(flags) do
    if (pi.CanRead) then
      f ((pi.GetGetMethod(true)) :> MethodBase)
    elif (pi.CanWrite) then
      f ((pi.GetSetMethod(true)) :> MethodBase)
      
// --------------------------------------------------------------------------------------
   
// initialization of data structures with opcodes
   
let values = typeof<OpCodes>.GetFields()
let opcodes = values |> Array.map (fun v -> v.GetValue(null) :?> OpCode)
let opSingle = new Dictionary<byte, OpCode>()
let opDouble = new Dictionary<uint16, OpCode>()
for o in opcodes do
  match o.Size with
  | 1 -> if (o.Value < (int16 0xf8) || o.Value > (int16 0xff)) then // skip prefix1 .. prefix7, prefixref opcodes
           opSingle.Add(byte o.Value, o)
  | 2 -> opDouble.Add(uint16 o.Value, o)
  | _ -> failwith "Unexpected opcode size!"

// --------------------------------------------------------------------------------------

// generates active patterns declared in 'types.fs'

let niceName (s:string) = 
  let str = 
    [| let lastDot = ref true
       for c in s.ToCharArray() do
         if (!lastDot) then
           yield c.ToString().ToUpper().[0]
         elif (c <> '.') then
           yield c
         do lastDot := (c = '.') |]
  new String(str)         
  
let printFSharpOpCodeType () =    
  opcodes |> Array.sort (fun oa ob -> compare oa.Name ob.Name)
  for o in opcodes do
    printfn "let (|%s|_|) (o:OpCodeInfo) = if (o.OpCode.Value = %is) then Some(o.OpCode, o.Operand) else None" (niceName o.Name) o.Value
  Console.ReadLine()

//printFSharpOpCodeType ()

// --------------------------------------------------------------------------------------

type OperandInternal = 
  | InlineSwitch of int[]
  | InlineBrTarget of int
  | InlineField of FieldInfo
  | InlineI of int       
  | InlineI8 of int64      
  | InlineMethod of MethodBase 
  | InlineNone     
  | InlinePhi      
  | InlineR of float
  | InlineSig      
  | InlineString of string  
  | InlineTok      
  | InlineType of Type    
  | InlineVar      
  | ShortInlineBrTarget of int8
  | ShortInlineI of int8  
  | ShortInlineR of float32  
  | ShortInlineVar of int8
  
// --------------------------------------------------------------------------------------
  
type OpCodeInfo(size:int, oper:OperandInternal, opc:OpCode) =

  static member Parse(opc:OpCode, i:int, il:byte[], meth:MethodBase, genMetA, genTyA) =
    let readInt32() = 
      ((int32 il.[i+3]) <<< 24) + ((int32 il.[i+2]) <<< 16) + 
      ((int32 il.[i+1]) <<< 8) + (int32 il.[i+0])
    let readInt64() = 
      ((int64 il.[i+7]) <<< 56) + ((int64 il.[i+6]) <<< 48) + 
      ((int64 il.[i+5]) <<< 40) + ((int64 il.[i+4]) <<< 32) + (int64 (readInt32()))
    let readDouble() =
      let ms = new IO.MemoryStream(il)
      ms.Seek(int64 i, IO.SeekOrigin.Begin) |> ignore
      (new IO.BinaryReader(ms)).ReadDouble()
    let readFloat() =
      let ms = new IO.MemoryStream(il)
      ms.Seek(int64 i, IO.SeekOrigin.Begin) |> ignore
      (new IO.BinaryReader(ms)).ReadSingle()
      
    let size, op =  
      match opc.OperandType with
      | OperandType.InlineBrTarget -> 4, InlineBrTarget(readInt32())
      | OperandType.InlineField    -> 4, InlineField(meth.Module.ResolveField(readInt32(), genTyA, genMetA))
      | OperandType.InlineI        -> 4, InlineI(readInt32())
      | OperandType.InlineI8       -> 8, InlineI8(readInt64())
      | OperandType.InlineMethod   -> 
          4, try 
               let a = meth.Module
               let t = readInt32()
               let mi = a.ResolveMethod(t, genTyA, genMetA)
               InlineMethod(mi)
             with e -> 
               printf "%A" e; InlineNone
      | OperandType.InlineNone     -> 0, InlineNone
      | OperandType.InlinePhi      -> failwith "!"
      | OperandType.InlineR        -> 8, InlineR(readDouble())
      | OperandType.InlineSig      -> failwith "!"
      | OperandType.InlineString   -> 4, InlineString(meth.Module.ResolveString(readInt32()))
      | OperandType.InlineSwitch   -> 
          let num = readInt32()
          let ar = [| let n = ref num 
                      while (!n <> 0) do 
                        do n := (!n) - 1
                        yield readInt32() |]
          (1 + num) * 4, InlineSwitch(ar)          
      | OperandType.InlineTok      -> 
          4, 
            (let tok = readInt32()
            try InlineType(meth.Module.ResolveType(tok, genTyA, genMetA)) with _ ->
            try InlineField(meth.Module.ResolveField(tok, genTyA, genMetA)) with _ ->
            try InlineMethod(meth.Module.ResolveMethod(tok, genTyA, genMetA)) with _ ->
            try InlineString(meth.Module.ResolveString(tok)) with _ ->
              failwith "Unexpected Token")
      | OperandType.InlineType     -> 4, InlineType(meth.Module.ResolveType(readInt32(), genTyA, genMetA))
      | OperandType.InlineVar      -> failwith "!"
      | OperandType.ShortInlineBrTarget -> 1, ShortInlineBrTarget(sbyte (il.[i]))
      | OperandType.ShortInlineI   -> 1, ShortInlineI(sbyte (il.[i]))
      | OperandType.ShortInlineR   -> 4, ShortInlineR(readFloat())
      | OperandType.ShortInlineVar -> 1, ShortInlineVar(sbyte (il.[i]))
      | _ -> failwith "!"
    OpCodeInfo(size, op, opc)
    
  override x.ToString() = 
    match oper with
      | InlineBrTarget(i) -> sprintf "target:%d" i
      | InlineField(fi) -> sprintf "field:%s" fi.Name 
      | InlineI(i) -> sprintf "int32:%d" i
      | InlineI8(i) -> sprintf "int64:%dx" i
      | InlineMethod(mi) -> sprintf "method:%s" mi.Name
      | InlineNone -> "(none)"     
      | InlinePhi -> ""      
      | InlineR(f) -> sprintf "double:%f" f
      | InlineSig -> ""      
      | InlineString(s) -> sprintf "string:%A" s
      | InlineSwitch(ar) -> "switch ( ... )"   
      | InlineTok -> ""      
      | InlineType(ty) -> sprintf "type:%s" ty.Name
      | InlineVar -> "" 
      | ShortInlineBrTarget(s) -> sprintf "target:%d" s
      | ShortInlineI(s) -> sprintf "int32:%d" s
      | ShortInlineR(f) -> sprintf "float:%f" f
      | ShortInlineVar(v) -> sprintf "var:%d" v

  member x.Size = size
  member x.OpCode = opc
  member x.Operand = oper

let rec iterOpCodesAux f meth gta gma il i =
  let b = il.[i]
  let succ, op1 = opSingle.TryGetValue(b)
  let opc = 
    if (succ) then op1
    else
      let b2 = ((uint16 b) <<< 8) + (uint16 (il.[i+1]))
      let succ, op2 = opDouble.TryGetValue(b2)
      if (succ) then op2
      else failwith "Invalid IL code?"
      
  let ops = OpCodeInfo.Parse(opc, i + opc.Size, il, meth, gma, gta)
  f ops
  let i = i + opc.Size + ops.Size
  if (i < il.Length) then
    iterOpCodesAux f meth gta gma il i
    
let iterOpCodes f (mi:MethodBase) =
  let gma = if (mi.IsGenericMethod) then mi.GetGenericArguments() else [| |]
  let gta = if (mi.DeclaringType.IsGenericType) then mi.DeclaringType.GetGenericArguments() else [| |]
  let body = mi.GetMethodBody()
  if (body <> null) then
    let il = body.GetILAsByteArray()
    iterOpCodesAux f mi gta gma il 0 

      