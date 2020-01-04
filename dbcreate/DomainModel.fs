namespace DbCreate

[<AutoOpen>]
module DomainModel =
    type DataExtractor<'T> = string -> seq<'T>
    type ScriptExecutor = string -> Async<unit>
    type DataWriter = string -> Map<string, obj> -> Async<unit>
