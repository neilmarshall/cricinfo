namespace Cricinfo.Services.Matchdata

open System
open System.Threading.Tasks
open Microsoft.Extensions.Logging
open Cricinfo.Models
open PostgresDbQueryFunctions

type CricInfoQueryService<'T>(connString : string, logger : ILogger<'T>) =

    new (connString : string) =
        new CricInfoQueryService<'T>(connString, null)

    interface ICricInfoQueryService with

        member this.GetMatchAsync (id : int) : Task<Match> =
            genericQueryWrapper
                logger
                (fun () ->
                    async {
                        match! getMatchInfo connString id with
                        | Some m -> return m
                        | None -> return null
                    })
                null

        member this.GetAllMatchesAsync() : Task<Match[]> =
            genericQueryWrapper
                logger
                (fun () ->
                    async {
                        let! ids = getMatchIds connString
                        let! results = ids |> Seq.map (getMatchInfo connString) |> Async.Sequential
                        return results |> Array.filter Option.isSome |> Array.map Option.get
                    })
                Array.empty

        member this.MatchExistsAsync (homeTeam : string, awayTeam : string, date : DateTime) : Task<bool> =
            genericQueryWrapper logger (fun () -> async { return! checkMatchExistsAsync connString homeTeam awayTeam date }) false

        member this.GetTeamsAsync() : Task<string []> =
            genericQueryWrapper
                logger
                (fun () ->
                    async {
                        let! teams = getTeamsAsync connString
                        return teams |> Seq.toArray
                    })
                Array.empty
