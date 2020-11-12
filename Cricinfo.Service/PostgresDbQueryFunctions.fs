module private Cricinfo.Services.PostgresDbQueryFunctions

open System
open System.Threading.Tasks
open Microsoft.Extensions.Logging
open Npgsql
open Cricinfo.Services.PostgresDbBase

let genericQueryWrapper (logger : ILogger<'a>) (f : unit -> Async<'b>) (defaultValue : 'b) : Task<'b> =
    let logError = logError logger
    async {
        try
            return! f()
        with
        | :? AggregateException as ae ->
            ae.InnerExceptions |> Seq.iter logError
            ae.Flatten().Handle(postgresExceptionCatcher)
            return defaultValue
    } |> Async.StartAsTask

let private getConnection connString = new NpgsqlConnection(connString)

let queryRecord<'T>
    (connString : string)
    (query : string)
    (parameters : Map<string, obj>)
    (responseMapper : Data.Common.DbDataReader -> Async<'T>)
        : Async<'T option> =
    async {
        use conn = getConnection connString
        do! conn.OpenAsync() |> Async.AwaitTask
        use command = new NpgsqlCommand(query, conn)
        parameters |> Map.iter (fun k v -> command.Parameters.AddWithValue(k, v) |> ignore)
        use! response = command.ExecuteReaderAsync() |> Async.AwaitTask
        if response.HasRows then
            do! response.ReadAsync() |> Async.AwaitTask |> Async.Ignore
            let! outValue = response |> responseMapper
            return outValue |> Some
        else
            return None
    }

let queryRecordSet<'T>
    (connString : string)
    (query : string)
    (parameters : Map<string, obj>)
        : Async<'T[]> =
    async {
        use conn = getConnection connString
        do! conn.OpenAsync() |> Async.AwaitTask
        use command = new NpgsqlCommand(query, conn)
        parameters |> Map.iter (fun k v -> command.Parameters.AddWithValue(k, v) |> ignore)
        use! response = command.ExecuteReaderAsync() |> Async.AwaitTask
        return seq {
            while response.Read() do
                yield response.GetValue(0) :?> 'T
        } |> Seq.toArray
    }
