module private Cricinfo.Services.PostgresDbBase

open Microsoft.Extensions.Logging
open System

let logError<'a> (logger : ILogger<'a>) (e : exn) =
    match logger <> null with
    | true -> logger.LogError(e.Message)
    | false -> ()

let postgresExceptionCatcher = new Func<exn, bool>(function | :? Npgsql.PostgresException -> true | _ -> false)