namespace DbCreate

module PostgresDataManager =

    open System.Configuration
    open Npgsql

    let private ConnString = ConfigurationManager.ConnectionStrings.["DefaultConnection"].ConnectionString

    let getColumn<'T> : DataExtractor<'T> = fun query ->
        try
            seq {
                use conn = new NpgsqlConnection(ConnString)
                conn.Open()
                use command = new NpgsqlCommand(query, conn)
                use reader = command.ExecuteReader()
                while reader.Read() do
                    yield reader.GetValue(0) :?> 'T
            }
        with
        | :? System.InvalidCastException as err ->
            err.Message |> sprintf "error occurred casting column to required type: %s" |> failwith
        | :? System.Data.Common.DbException as err ->
            err.Message |> sprintf "error occurred accessing database: %s" |> failwith

    let executeScript : ScriptExecutor = fun query ->
        async {
            try
                use conn = new NpgsqlConnection(ConnString)
                do! conn.OpenAsync() |> Async.AwaitTask
                use command = new NpgsqlCommand(query, conn)
                do! command.ExecuteNonQueryAsync() |> Async.AwaitTask |> Async.Ignore
            with
            | :? System.Data.Common.DbException as err ->
                err.Message |> sprintf "error occurred accessing database: %s" |> failwith
        }

    let insertData : DataWriter = fun query parameters ->
        async {
            try
                use conn = new NpgsqlConnection(ConnString)
                do! conn.OpenAsync() |> Async.AwaitTask
                use command = new NpgsqlCommand(query, conn)
                Map.iter (fun (k : string) (v : obj) -> command.Parameters.AddWithValue(k, v) |> ignore) parameters
                do! command.ExecuteNonQueryAsync() |> Async.AwaitTask |> Async.Ignore
            with
            | :? System.Data.Common.DbException as err ->
                err.Message |> sprintf "error occurred accessing database: %s" |> failwith
        }