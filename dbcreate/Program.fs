module Logging =
    type Logger = | Debug of string | Info of string | Error of string

    let logMessage =
        function
        | Debug msg -> printfn "DEBUG: %s" msg
        | Info msg -> printfn "INFO: %s" msg
        | Error msg -> printfn "ERROR: %s" msg


[<AutoOpen>]
module DomainModel =
    type DataExtractor<'T> = string -> seq<'T>
    type ScriptExecutor = string -> Async<unit>
    type DataWriter = string -> Map<string, obj> -> Async<unit>


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
        

module ScriptExecutor =

    open System.IO

    let executeScriptsAsync
            (extractor : DataExtractor<string>) 
            (scriptExecutor : ScriptExecutor)
            (writer : DataWriter)
            (directory : string)
                : Async<unit []> =

        let getUnexecutedScripts directory =
            let getExecutedScripts =
                seq { yield! extractor "SELECT filename FROM version;" }
            let allScripts = Directory.GetFiles(directory) |> Set.ofSeq
            let executedScripts =
                getExecutedScripts
                |> Seq.map (fun filename -> Path.Combine([|directory; filename|]))
                |> Set.ofSeq
            Set.difference allScripts executedScripts

        let executeScriptAsync filename =
            let writeFilenameAsync filename =
                async {
                    let query = "INSERT INTO version (filename, date) VALUES (@filename, @date);"
                    let parameters = Map.ofList ["filename", box(filename); "date", box(System.DateTime.Now)]
                    do! writer query parameters |> Async.Ignore
                }
            async {
                filename |> sprintf "Executing script '%s'" |> Logging.Info |> Logging.logMessage
                let! script = File.ReadAllTextAsync(filename) |> Async.AwaitTask
                do! scriptExecutor script |> Async.Ignore
                do! Path.GetFileName(filename) |> writeFilenameAsync |> Async.Ignore
           }
        directory |> getUnexecutedScripts |> Seq.map executeScriptAsync |> Async.Parallel


module ComputationRoot =

    let scriptExecutor =
        ScriptExecutor.executeScriptsAsync
            PostgresDataManager.getColumn
            PostgresDataManager.executeScript
            PostgresDataManager.insertData


module Program =

    open System.Reflection

    [<EntryPoint>]
    let main argv =
        Logging.Info "Commencing DataBase creation" |> Logging.logMessage
        let basePath = System.IO.FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName
        System.IO.Path.Combine([|basePath; "scripts"|])
        |> ComputationRoot.scriptExecutor
        |> Async.RunSynchronously
        |> ignore
        Logging.Info "DataBase creation completed" |> Logging.logMessage
        0 // return an integer exit code
