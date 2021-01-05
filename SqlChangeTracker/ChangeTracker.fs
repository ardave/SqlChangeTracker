namespace SqlChangeTracker

open System
open System.Collections.Generic

type ChangeVersion = ChangeVersion of int64

type ExpirationStrategy =
| ByAge  of TimeSpan
| ByDate of DateTimeOffset

type ChangeTracker<'tKey, 'tItem> =
    private
        { _items               : Dictionary<'tKey, 'tItem> 
          mutable _version     : ChangeVersion option
          _itemAddedCallback   : ('tKey -> 'tItem -> unit) option
          _itemDeletedCallback : ('tKey -> unit) option
          _expirationStrategy  : ExpirationStrategy option }
    with
        member x.Items   = x._items.Values
        member x.Version = x._version

type Change<'tKey, 'tItem> =
| InitialLoad of ChangeVersion option * 'tKey * 'tItem
| Add         of ChangeVersion * 'tKey * 'tItem
| Update      of ChangeVersion * 'tKey * 'tItem
| Delete      of ChangeVersion * 'tKey

module ChangeTracker =

    let create itemAddedCallback itemDeletedCallback expirationStrategyOption = 
        { _items               = new Dictionary<'tKey, 'tItem>()
          _version             = None
          _itemAddedCallback   = itemAddedCallback
          _itemDeletedCallback = itemDeletedCallback
          _expirationStrategy = expirationStrategyOption }

    let processChanges<'tKey, 'tItem> (changes: Change<'tKey, 'tItem> list) (changeTracker:ChangeTracker<'tKey, 'tItem>) =
        let highestVersion = 
            changes
            |> List.fold(fun startingVersion change ->
                match change with
                | InitialLoad (verOpt, key, item) -> 
                    changeTracker._items.[key] <- item
                    changeTracker._itemAddedCallback |> Option.iter(fun callback -> callback key item)

                    match (startingVersion, verOpt) with
                    | (Some sv, Some v) when v > sv -> Some v
                    | (None, Some v) -> Some v
                    | (_, _) -> startingVersion 
                | Add (ver, key, item) -> 
                    changeTracker._items.[key] <- item
                    changeTracker._itemAddedCallback |> Option.iter(fun callback -> callback key item)

                    match startingVersion with
                    | Some v when ver > v -> Some ver
                    | _ -> startingVersion 
                | Update (ver, key, item) ->
                    changeTracker._items.Remove key |> ignore
                    changeTracker._itemDeletedCallback |> Option.iter(fun callback -> callback key)

                    changeTracker._items.[key] <- item
                    changeTracker._itemAddedCallback |> Option.iter(fun callback -> callback key item)

                    match startingVersion with
                    | Some v when ver > v -> Some ver
                    | _ -> startingVersion 
                | Delete (ver, key) -> 
                    changeTracker._items.Remove key |> ignore
                    changeTracker._itemDeletedCallback |> Option.iter(fun callback -> callback key)

                    match startingVersion with
                    | Some v when ver > v -> Some ver
                    | None                -> Some ver
                    | _                   -> startingVersion 
            ) changeTracker.Version
        changeTracker._version <- highestVersion