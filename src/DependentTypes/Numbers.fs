namespace FSharp.DependentTyping

open System
open System.Reflection
open FSharp.Core.CompilerServices
open ProviderImplementation
open ProviderImplementation.ProvidedTypes
open FSharp.Quotations

module BoundedNums =
  let inline createdBoundedNum< ^n when ^n : comparison> asm ns (tyName : string) (bounds : obj array) =
    let lower = (bounds.[0] : obj) :?> ^n
    let upper = (bounds.[1] : obj) :?> ^n
    if lower > upper then failwithf "Lower %A must be less than upper %A" lower upper
    else
    let numType = typeof< ^n>
    let param = new ProvidedParameter("value", numType)
    let constrain (exprs : Expr list) =
      let v = exprs |> List.head
      <@@
      let  i = %%Expr.Coerce(v, numType)
      if   i > upper then failwithf "%A is greater than %A"  i upper
      elif i < lower then failwithf "%A is less than %A"     i lower
      else i
      @@>
    let ctor          = new ProvidedConstructor([param], InvokeCode = constrain)
    let boundedNum    = new ProvidedTypeDefinition(asm, ns, tyName, Some numType)
    let boundedNumOpt = typedefof<_ option>.MakeGenericType(boundedNum)
    let factory       = new ProvidedMethod("TryCreate", [param], boundedNumOpt, IsStaticMethod = true)
    factory.InvokeCode <-
      fun exprs ->
      let v = exprs |> List.head
      <@@
      let  i = (%%Expr.Coerce(v, numType) : 'n)
      if   i > upper then None
      elif i < lower then None
      else Some i
      @@>
    boundedNum.AddMember(ctor)
    boundedNum.AddMember(factory)

    let upperBoundGetter = new ProvidedProperty("MaxValue", numType, IsStatic = true, GetterCode = (fun _ -> <@@ upper @@>))
    let lowerBoundGetter = new ProvidedProperty("MinValue", numType, IsStatic = true, GetterCode = (fun _ -> <@@ lower @@>))
    boundedNum.AddMember(upperBoundGetter)
    boundedNum.AddMember(lowerBoundGetter)
    boundedNum

  let upperBoundParam<'t> ()              = new ProvidedStaticParameter("Upper", typeof<'t>)
  let lowerBoundParam<'t> (v : 't option) = new ProvidedStaticParameter("Lower", typeof<'t>,
                                                                        parameterDefaultValue = defaultArg v (Unchecked.defaultof<'t>))
  let boundedNum<'t> create asm ns (defaultLowerBound : 't option)=
    let name = sprintf "Bounded%s" typeof<'t>.Name
    let b = new ProvidedTypeDefinition(asm, ns, name,  Some typeof<obj>)
    b.DefineStaticParameters([lowerBoundParam defaultLowerBound; upperBoundParam<'t> ()], create asm ns)
    b
  let ns          = "FSharp.DependentTypes.Numbers"
  let asm         = Assembly.GetExecutingAssembly()
  let inline create< ^t when ^t : comparison> () =
    boundedNum< ^t> createdBoundedNum< ^t> asm ns None
  let types = [
    create<sbyte> ()
    create<byte> ()
    create<int16> ()
    create<uint16> ()
    create<int> ()
    create<uint32> ()
    create<unativeint> ()
    create<int64> ()
    create<uint64> ()
    create<single> ()
    create<double> ()
    create<bigint> ()
    create<decimal> () ]

[<TypeProvider>]
type Numbers (_cfg) as tp =
  inherit TypeProviderForNamespaces ()
  do
    tp.AddNamespace("FSharp.DependentTypes.Numbers", BoundedNums.types)
