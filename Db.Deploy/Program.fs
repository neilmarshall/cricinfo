namespace DbCreate

module Program =

    open System.Reflection

    let private basePath = System.IO.FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName

    [<EntryPoint>]
    let main argv =
        Logging.Info "Commencing DataBase creation" |> Logging.logMessage
        try
            System.IO.Path.Combine([|basePath; "scripts"|])
            |> ComputationRoot.scriptExecutor
            |> Async.RunSynchronously
            |> ignore
        with
            | _ as e -> Logging.Error e.Message |> Logging.logMessage
        Logging.Info "DataBase creation completed" |> Logging.logMessage
        0 // return an integer exit code
