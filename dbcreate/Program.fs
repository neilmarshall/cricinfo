module DB =

    open System.Configuration
    open Npgsql

    let private ConnString = ConfigurationManager.ConnectionStrings.["DefaultConnection"].ConnectionString

    let getColumn<'T> query =
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

    let insertData query parameters =
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

    let executeScript script =
        async {
            try
                use conn = new NpgsqlConnection(ConnString)
                do! conn.OpenAsync() |> Async.AwaitTask
                use command = new NpgsqlCommand(script, conn)
                do! command.ExecuteNonQueryAsync()|> Async.AwaitTask |> Async.Ignore
            with
            | :? System.Data.Common.DbException as err ->
                err.Message |> sprintf "error occurred accessing database: %s" |> failwith
        }


module Scripts =

    open System.IO

    let private getExecutedScripts =
        seq { yield! DB.getColumn<string> "SELECT filename FROM version;" }

    let private getUnexecutedScripts directory =
        let allScripts = Directory.GetFiles(directory) |> Set.ofSeq
        let executedScripts =
            getExecutedScripts
            |> Seq.map (fun filename -> Path.Combine([|directory; filename|]))
            |> Set.ofSeq
        Set.difference allScripts executedScripts

    let private writeFilenameAsync (filename : string) =
        async {
            let query = "INSERT INTO version (filename, date) VALUES (@filename, @date);"
            let parameters = Map.ofList ["filename", box(filename); "date", box(System.DateTime.Now)]
            do! DB.insertData query parameters |> Async.Ignore
        }

    let executeScriptsAsync directory =
        let executeScriptAsync filename =
            async {
                let script = File.ReadAllText(filename)
                do! DB.executeScript script |> Async.Ignore
                do! writeFilenameAsync (Path.GetFileName(filename)) |> Async.Ignore
           }
        directory |> getUnexecutedScripts extractor |> Seq.map executeScriptAsync |> Async.Parallel


module ComputationRoot =

    let scriptExecutor = Scripts.executeScriptsAsync PostgresDBExecutor.getColumn PostgresDBExecutor.executeScript PostgresDBExecutor.insertData


module Program =

    [<EntryPoint>]
    let main argv =
        printfn "Commencing DataBase creation"
        let basePath = System.IO.FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).Directory.FullName
        System.IO.Path.Combine([|basePath; "scripts"|])
        |> ComputationRoot.scriptExecutor
        |> ignore
        printfn "DataBase creation completed"
        0 // return an integer exit code
