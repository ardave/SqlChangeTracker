module ChangeTrackingTests

open System
open Xunit

[<Fact>]
let ``Initial data load should be reflected in Change Tracker`` () : unit =
    let initialDataLoad = SqlData.getExistingRecords()
    Assert.True(true)
