module Logging =
    type Logger = | Debug of string | Info of string | Error of string

    let logMessage =
        function
        | Debug msg -> printfn "DEBUG: %s" msg
        | Info msg -> printfn "INFO: %s" msg
        | Error msg -> printfn "ERROR: %s" msg

module PostgresDBExecutor =

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

    let insertData (query : string) (parameters : Map<string, obj>) : Async<unit> =
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

    let executeScript (query : string) : Async<unit> =
        async {
            try
                use conn = new NpgsqlConnection(ConnString)
                do! conn.OpenAsync() |> Async.AwaitTask
                use command = new NpgsqlCommand(query, conn)
                do! command.ExecuteNonQueryAsync()|> Async.AwaitTask |> Async.Ignore
            with
            | :? System.Data.Common.DbException as err ->
                err.Message |> sprintf "error occurred accessing database: %s" |> failwith
        }


module Scripts =

    open System.IO

    let private getExecutedScripts (extractor : string -> seq<'T>) : seq<'T> =
        seq { yield! extractor "SELECT filename FROM version;" }

    let private getUnexecutedScripts (extractor : string -> seq<'T>) directory =
        let allScripts = Directory.GetFiles(directory) |> Set.ofSeq
        let executedScripts =
            getExecutedScripts extractor
            |> Seq.map (fun filename -> Path.Combine([|directory; filename|]))
            |> Set.ofSeq
        Set.difference allScripts executedScripts

    let private writeFilenameAsync (writer : string -> Map<string, obj> -> Async<unit>) (filename : string) =
        async {
            let query = "INSERT INTO version (filename, date) VALUES (@filename, @date);"
            let parameters = Map.ofList ["filename", box(filename); "date", box(System.DateTime.Now)]
            do! writer query parameters |> Async.Ignore
        }

    let executeScriptsAsync
            (extractor : string -> seq<'T>) 
            (scriptExecutor : string -> Async<unit>)
            (writer : string -> Map<string, obj> -> Async<unit>)
            (directory : string)
                : Async<unit []> =
        let executeScriptAsync filename =
            async {
                let! script = File.ReadAllTextAsync(filename) |> Async.AwaitTask
                do! scriptExecutor script |> Async.Ignore
                do! writeFilenameAsync writer (Path.GetFileName(filename)) |> Async.Ignore
           }
        directory |> getUnexecutedScripts extractor |> Seq.map executeScriptAsync |> Async.Parallel


module ComputationRoot =

    let scriptExecutor = Scripts.executeScriptsAsync PostgresDBExecutor.getColumn PostgresDBExecutor.executeScript PostgresDBExecutor.insertData


module Program =

    [<EntryPoint>]
    let main argv =
        Logging.Info "Commencing DataBase creation" |> Logging.logMessage
        let basePath = System.IO.FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).Directory.FullName
        System.IO.Path.Combine([|basePath; "scripts"|])
        |> ComputationRoot.scriptExecutor
        |> ignore
        Logging.Info "DataBase creation completed" |> Logging.logMessage
        0 // return an integer exit code
