namespace DbCreate

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
        directory |> getUnexecutedScripts |> Seq.map executeScriptAsync |> Async.Sequential