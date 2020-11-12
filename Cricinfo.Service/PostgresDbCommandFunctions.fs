module private Cricinfo.Services.PostgresDbCommandFunctions

open Npgsql
open Cricinfo.Services.PostgresDbBase

let logError = logError
let postgresExceptionCatcher = postgresExceptionCatcher

let getConnection connString = new NpgsqlConnection(connString)

let getColumnAsync<'T> (conn : NpgsqlConnection) (trans : NpgsqlTransaction) (query : string) (parameters : Map<string, obj>) : Async<seq<'T>> =
    async {
        use command = new NpgsqlCommand(query, conn, trans)
        parameters |> Map.iter (fun k v -> command.Parameters.AddWithValue(k, v) |> ignore)
        let! response = command.ExecuteReaderAsync() |> Async.AwaitTask
        return seq {
            while response.Read() do
                yield response.GetValue(0) :?> 'T
        }
    }

let executeNonQuery (conn : NpgsqlConnection) (trans : NpgsqlTransaction) (query : string) (parameters : Map<string, obj>) : Unit =
    use command = new NpgsqlCommand(query, conn, trans)
    parameters |> Map.iter (fun k v -> command.Parameters.AddWithValue(k, v) |> ignore)
    do command.ExecuteNonQuery() |> ignore

let executeNonQueryAsync (conn : NpgsqlConnection) (trans : NpgsqlTransaction) (query : string) (parameters : Map<string, obj>) : Async<Unit> =
    async {
        use command = new NpgsqlCommand(query, conn, trans)
        parameters |> Map.iter (fun k v -> command.Parameters.AddWithValue(k, v) |> ignore)
        do! command.ExecuteNonQueryAsync() |> Async.AwaitTask |> Async.Ignore
    }

let executeScalar<'T> (conn : NpgsqlConnection) (trans : NpgsqlTransaction) (query : string) (parameters : Map<string, obj>) : 'T =
    use command = new NpgsqlCommand(query, conn, trans)
    parameters |> Map.iter (fun k v -> command.Parameters.AddWithValue(k, v) |> ignore)
    command.ExecuteScalar() :?> 'T

let executeScalarAsync<'T> (conn : NpgsqlConnection) (trans : NpgsqlTransaction) (query : string) (parameters : Map<string, obj>) : Async<'T> =
    async {
        use command = new NpgsqlCommand(query, conn, trans)
        parameters |> Map.iter (fun k v -> command.Parameters.AddWithValue(k, v) |> ignore)
        let! response =  command.ExecuteScalarAsync() |> Async.AwaitTask
        return response :?> 'T
    }
