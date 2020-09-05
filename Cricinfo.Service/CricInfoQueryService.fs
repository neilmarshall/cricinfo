namespace Cricinfo.Services

open System
open System.Threading.Tasks
open Microsoft.Extensions.Logging
open Cricinfo.Models
open PostgresDbQueryFunctions

module private CricInfoQueryServiceHelper =

    let private postgresExceptionCatcher = new Func<exn, bool>(function | :? Npgsql.PostgresException -> true | _ -> false)

    let genericQueryWrapper<'T> (logError : exn -> unit) (f : unit -> Async<'T>) (defaultValue : 'T) : Task<'T> =
        async {
            try
                return! f()
            with
            | :? AggregateException as ae ->
                ae.InnerExceptions |> Seq.iter logError
                ae.Flatten().Handle(postgresExceptionCatcher)
                return defaultValue
        } |> Async.StartAsTask


type public CricInfoQueryService<'T>(connString : string, logger : ILogger<'T>) =
    
    let logger = if logger <> null then Some logger else None
    let logError (e : exn) =
        match logger with
        | Some logger -> logger.LogError(e.Message)
        | None -> ()

    new (connString : string) =
        CricInfoQueryService(connString, null)

    interface ICricInfoQueryService with

        member this.GetMatchAsync (id : int) : Task<Match> =
            CricInfoQueryServiceHelper.genericQueryWrapper
                logError
                (fun () ->
                    async {
                        match! getMatchInfo connString id with
                        | Some m -> return m
                        | None -> return null
                    })
                null

        member this.GetAllMatchesAsync() : Task<Match[]> =
            CricInfoQueryServiceHelper.genericQueryWrapper
                logError
                (fun () ->
                    async {
                        let! ids = getMatchIds connString
                        let! results = ids |> Seq.map (getMatchInfo connString) |> Async.Sequential
                        return results |> Array.filter Option.isSome |> Array.map Option.get
                    })
                Array.empty

        member this.MatchExistsAsync (homeTeam : string, awayTeam : string, date : DateTime) : Task<bool> =
            CricInfoQueryServiceHelper.genericQueryWrapper logError (fun () -> async { return! checkMatchExistsAsync connString homeTeam awayTeam date }) false

        member this.GetTeamsAsync() : Task<string []> =
            CricInfoQueryServiceHelper.genericQueryWrapper
                logError
                (fun () ->
                    async {
                        let! teams = getTeamsAsync connString
                        return teams |> Seq.toArray
                    })
                Array.empty
