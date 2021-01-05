module Tests

open System
open Xunit
open SqlChangeTracker

type TestRecord =
    { Id   : Guid
      Name : string
      Age  : int }

let item1 = { Id = Guid.NewGuid(); Name = "Steve";  Age = 25 }
let item2 = { Id = Guid.NewGuid(); Name = "Sheila"; Age = 42 }
let item3 = { Id = Guid.NewGuid(); Name = "Earl";   Age = 17 }

let items = [item1; item2; item3]

let initialLoadItems =
    items
    |> List.map (fun item -> 
        InitialLoad(None, item.Id, item))

[<Fact>]
let ``Initial data load should be reflected in change tracking items`` () : unit  =
    let changeTracker = ChangeTracker.create None None None
    
    changeTracker |> ChangeTracker.processChanges initialLoadItems

    Assert.Equal(3, changeTracker.Items.Count)

[<Fact>]
let ``Change tracker version should be updated to latest version in initial data load``() : unit =
    let changeTracker = ChangeTracker.create None None None

    let initialLoadItems =
        [ InitialLoad(None, item1.Id, item1)
          InitialLoad(Some (ChangeVersion 1L), item2.Id, item2)
          InitialLoad(Some (ChangeVersion 2L), item3.Id, item3)]
    
    changeTracker |> ChangeTracker.processChanges initialLoadItems

    Assert.Equal(Some (ChangeVersion 2L), changeTracker.Version)

[<Fact>]
let ``Initial data load should trigger correct callbacks``() : unit =
    let mutable itemAddedCallbackInvocationCount = 0
    let mutable itemDeletedCallbackInvocationCount = 0

    let itemAddedCallback   = Some (fun _ _ -> itemAddedCallbackInvocationCount   <- itemAddedCallbackInvocationCount   + 1)
    let itemDeletedCallback = Some (fun _   -> itemDeletedCallbackInvocationCount <- itemDeletedCallbackInvocationCount + 1)

    let changeTracker = ChangeTracker.create itemAddedCallback itemDeletedCallback None

    changeTracker |> ChangeTracker.processChanges initialLoadItems

    Assert.Equal(3, itemAddedCallbackInvocationCount)
    Assert.Equal(0, itemDeletedCallbackInvocationCount)

[<Fact>]
let ``Added data Item should be reflected in Change Tracker``() : unit =
    let changeTracker = ChangeTracker.create None None None

    changeTracker |> ChangeTracker.processChanges initialLoadItems

    let addItem = { Id = Guid.NewGuid(); Name = "Mbeki"; Age = 80 }

    changeTracker |> ChangeTracker.processChanges [Add(ChangeVersion 1L, addItem.Id, addItem)]

    Assert.Equal(4, changeTracker.Items.Count)

[<Fact>]
let ``Added data Item should increment Change Tracker Version``() : unit =
    let changeTracker = ChangeTracker.create None None None

    let initialLoadItems =
        [ InitialLoad(None, item1.Id, item1)
          InitialLoad(Some (ChangeVersion 1L), item2.Id, item2)
          InitialLoad(Some (ChangeVersion 2L), item3.Id, item3)]

    let addedItem = { Id = Guid.NewGuid(); Name = "New Guy"; Age = 17 }

    changeTracker |> ChangeTracker.processChanges initialLoadItems
    changeTracker |> ChangeTracker.processChanges [Add((ChangeVersion 11L), addedItem.Id, addedItem)]

    Assert.Equal(Some (ChangeVersion 11L), changeTracker.Version)

[<Fact>]
let ``Adding Item should trigger correct callbacks``() : unit =
    let mutable itemAddedCallbackInvocationCount = 0
    let mutable itemDeletedCallbackInvocationCount = 0

    let itemAddedCallback   = Some (fun _ _ -> itemAddedCallbackInvocationCount   <- itemAddedCallbackInvocationCount   + 1)
    let itemDeletedCallback = Some (fun _   -> itemDeletedCallbackInvocationCount <- itemDeletedCallbackInvocationCount + 1)

    let changeTracker = ChangeTracker.create itemAddedCallback itemDeletedCallback None

    let addItem = { Id = Guid.NewGuid(); Name = "Jorge"; Age = 20}

    changeTracker |> ChangeTracker.processChanges initialLoadItems
    changeTracker |> ChangeTracker.processChanges [Add((ChangeVersion 7L), addItem.Id, addItem)]


    Assert.Equal(4, itemAddedCallbackInvocationCount)
    Assert.Equal(0, itemDeletedCallbackInvocationCount)

[<Fact>]
let ``Deleted item should get removed from Change Tracker``() : unit =
    let changeTracker = ChangeTracker.create None None None 
    
    changeTracker |> ChangeTracker.processChanges initialLoadItems

    changeTracker |> ChangeTracker.processChanges [Delete (ChangeVersion 99L, item1.Id)]

    Assert.Equal(2, changeTracker.Items.Count)
    Assert.Equal(ChangeVersion 99L, changeTracker.Version.Value)

[<Fact>]
let ``Deleting Item should trigger correct callbacks``() : unit =
    let mutable itemAddedCallbackInvocationCount = 0
    let mutable itemDeletedCallbackInvocationCount = 0

    let itemAddedCallback   = Some (fun _ _ -> itemAddedCallbackInvocationCount   <- itemAddedCallbackInvocationCount   + 1)
    let itemDeletedCallback = Some (fun _   -> itemDeletedCallbackInvocationCount <- itemDeletedCallbackInvocationCount + 1)

    let changeTracker = ChangeTracker.create itemAddedCallback itemDeletedCallback None

    changeTracker |> ChangeTracker.processChanges initialLoadItems
    changeTracker |> ChangeTracker.processChanges [Delete((ChangeVersion 8L), item1.Id)]

    Assert.Equal(3, itemAddedCallbackInvocationCount)
    Assert.Equal(1, itemDeletedCallbackInvocationCount)

[<Fact>]
let ``Updated item should get updated in Change Tracker``(): unit =
    let changeTracker = ChangeTracker.create None None None

    changeTracker |> ChangeTracker.processChanges initialLoadItems

    let updatedItem = { item1 with Name = "Billy Bob" }

    changeTracker |> ChangeTracker.processChanges [Update(ChangeVersion 1L, updatedItem.Id, updatedItem)]
    
    Assert.Equal(3, changeTracker.Items.Count)
    Assert.Contains(changeTracker.Items, fun i -> i.Name = "Billy Bob")
    Assert.DoesNotContain(changeTracker.Items, fun i -> i.Name = item1.Name)

[<Fact>]
let ``Updated item should update Change Tracker Version as necessary``() : unit =
    let changeTracker = ChangeTracker.create None None None

    let initialLoadItems =
        [ InitialLoad(None, item1.Id, item1)
          InitialLoad(Some (ChangeVersion 1L), item2.Id, item2)
          InitialLoad(Some (ChangeVersion 2L), item3.Id, item3)]

    changeTracker |> ChangeTracker.processChanges initialLoadItems

    let updatedItem = { item2 with Name = "Linda" }

    changeTracker |> ChangeTracker.processChanges [Update((ChangeVersion 19L), updatedItem.Id, updatedItem)]

    Assert.Equal(Some (ChangeVersion 19L), changeTracker.Version)

[<Fact>]
let ``Updating Item should trigger correct callbacks``() : unit =
    let mutable itemAddedCallbackInvocationCount = 0
    let mutable itemDeletedCallbackInvocationCount = 0

    let itemAddedCallback   = Some (fun _ _ -> itemAddedCallbackInvocationCount   <- itemAddedCallbackInvocationCount   + 1)
    let itemDeletedCallback = Some (fun _   -> itemDeletedCallbackInvocationCount <- itemDeletedCallbackInvocationCount + 1)

    let changeTracker = ChangeTracker.create itemAddedCallback itemDeletedCallback None

    changeTracker |> ChangeTracker.processChanges initialLoadItems

    let updatedItem = { item3 with Age = 99 }

    let _ = changeTracker |> ChangeTracker.processChanges [Update((ChangeVersion 23L), updatedItem.Id, updatedItem)]

    Assert.Equal(4, itemAddedCallbackInvocationCount)
    Assert.Equal(1, itemDeletedCallbackInvocationCount)

