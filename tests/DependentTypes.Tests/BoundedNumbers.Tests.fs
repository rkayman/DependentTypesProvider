module BoundedNumbers.Tests

open FSharp.DependentTyping
open FSharp.DependentTypes.Numbers
open NUnit.Framework

type ``10 to 20`` = BoundedInt32<Lower=10, Upper=20>
type I = ``10 to 20``

[<Test>]
let ``can create a bounded int32 in the specified range`` () =
  let i = I(18)
  Assert.AreEqual(18, i)

[<Test>]
let ``cannot create a bounded int32 greater than the specified range`` () =
  Assert.Throws(fun () -> let i = I(21) in printfn "%d" i) |> ignore
  Assert.Throws(fun () -> let i = I(9)  in printfn "%d" i) |> ignore

[<Test>]
let ``bounded int should have upper and lower bound properties`` () =
  Assert.IsNotNullOrEmpty(sprintf "Upper %d and lower %d" (I.MaxValue) (I.MinValue))
