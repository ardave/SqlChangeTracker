namespace SqlChangeTracker

open System.Collections.Generic

type ChangeVersion = ChangeVersion of int64

type ChangeTracker<'tKey, 'tItem> =
    { Items               : Dictionary<'tKey, 'tItem> 
      Version             : ChangeVersion option
      ItemAddedCallback   : ('tKey -> 'tItem -> unit) option
      ItemDeletedCallback : ('tKey -> unit) option }

type Change<'tKey, 'tItem> =
| InitialLoad of ChangeVersion option * 'tKey * 'tItem
| Add         of ChangeVersion * 'tKey * 'tItem
| Update      of ChangeVersion * 'tKey * 'tItem
| Delete      of ChangeVersion * 'tKey

module ChangeTracker =

    let create() = 
        { Items               = new Dictionary<'tKey, 'tItem>()
          Version             = None
          ItemAddedCallback   = None
          ItemDeletedCallback = None }

    let processChanges<'tKey, 'tItem> (changes: Change<'tKey, 'tItem> list) (changeTracker:ChangeTracker<'tKey, 'tItem>) : ChangeTracker<'tKey, 'tItem> =
        let highestVersion = 
            changes
            |> List.fold(fun startingVersion change ->
                match change with
                | InitialLoad (verOpt, key, item) -> 
                    changeTracker.Items.[key] <- item
                    changeTracker.ItemAddedCallback |> Option.iter(fun callback -> callback key item)

                    match (startingVersion, verOpt) with
                    | (Some sv, Some v) when v > sv -> Some v
                    | (None, Some v) -> Some v
                    | (_, _) -> startingVersion 
                | Add (ver, key, item) -> 
                    changeTracker.Items.[key] <- item
                    changeTracker.ItemAddedCallback |> Option.iter(fun callback -> callback key item)

                    match startingVersion with
                    | Some v when ver > v -> Some ver
                    | _ -> startingVersion 
                | Update (ver, key, item) ->
                    changeTracker.Items.Remove key |> ignore
                    changeTracker.ItemDeletedCallback |> Option.iter(fun callback -> callback key)

                    changeTracker.Items.[key] <- item
                    changeTracker.ItemAddedCallback |> Option.iter(fun callback -> callback key item)

                    match startingVersion with
                    | Some v when ver > v -> Some ver
                    | _ -> startingVersion 
                | Delete (ver, key) -> 
                    changeTracker.Items.Remove key |> ignore
                    changeTracker.ItemDeletedCallback |> Option.iter(fun callback -> callback key)

                    match startingVersion with
                    | Some v when ver > v -> Some ver
                    | None                -> Some ver
                    | _                   -> startingVersion 
            ) changeTracker.Version
        { changeTracker with Version = highestVersion }