﻿namespace Cricinfo.Services

open System
open System.Threading.Tasks
open Microsoft.Extensions.Logging
open Cricinfo.Models
open PostgresDbCommandFunctions

type public CricInfoCommandService<'T>(connString : string, logger : ILogger<'T>) =
    
    let postgresExceptionCatcher = new Func<exn, bool>(function | :? Npgsql.PostgresException -> true | _ -> false)
    let logger = if logger <> null then Some logger else None
    let logError (e : exn) =
        match logger with
        | Some logger -> logger.LogError(e.Message)
        | None -> ()

    new (connString : string) =
        CricInfoCommandService(connString, null)

    interface ICricInfoCommandService with

        member this.CreateMatchAsync (mtch : Match) : Task<DataCreationResponse * Nullable<int64>> =
            async {
                try
                    use conn = getConnection connString
                    do! conn.OpenAsync() |> Async.AwaitTask
                    use trans = conn.BeginTransaction()

                    let getIdsAsync = getIdsAsync conn trans
                    let getNextMatchIdAsync = getNextMatchIdAsync conn trans
                    let insertMatchAsync = insertMatchAsync conn trans
                    let insertSquadAsync = insertSquadAsync conn trans
                    let insertInningsAsync = insertInningsAsync conn trans

                    try
                        let! matchExists = PostgresDbQueryFunctions.checkMatchExistsAsync connString mtch.HomeTeam mtch.AwayTeam mtch.DateOfFirstDay
                        if matchExists then
                            return DataCreationResponse.DuplicateContent, Nullable()
                        else
                            let! venueId, homeTeamId, awayTeamId = getIdsAsync mtch.Venue mtch.HomeTeam mtch.AwayTeam
                            let! matchId = getNextMatchIdAsync
                            do! insertMatchAsync matchId mtch.DateOfFirstDay venueId homeTeamId awayTeamId mtch.Result mtch.MatchType
                            let homeSquad = Seq.zip (Seq.replicate (Seq.length mtch.HomeSquad) homeTeamId) mtch.HomeSquad
                            let awaySquad = Seq.zip (Seq.replicate (Seq.length mtch.AwaySquad) awayTeamId) mtch.AwaySquad
                            let! squadIds = insertSquadAsync matchId (Seq.append homeSquad awaySquad)
                            let tryGetPlayerId name =
                                squadIds
                                |> Seq.fold (fun state x -> Map.add x.Key x.Value state) Map.empty
                                |> Map.find name
                            do! insertInningsAsync tryGetPlayerId matchId mtch.Scores

                            do! trans.CommitAsync() |> Async.AwaitTask

                            return DataCreationResponse.Success, Nullable(matchId)
                    with
                    | :? AggregateException as ae ->
                        do! trans.RollbackAsync() |> Async.AwaitTask
                        ae.InnerExceptions |> Seq.iter logError
                        ae.Flatten().Handle(postgresExceptionCatcher)
                        return DataCreationResponse.Failure, Nullable()
                with
                | :? AggregateException as ae ->
                    ae.InnerExceptions |> Seq.iter logError
                    ae.Flatten().Handle(postgresExceptionCatcher)
                    return DataCreationResponse.Failure, Nullable()
            } |> Async.StartAsTask


        member this.CreateTeamAsync (team : string) : Task<DataCreationResponse> =
            async {
                try
                    use conn = getConnection connString
                    do! conn.OpenAsync() |> Async.AwaitTask
                    use trans = conn.BeginTransaction()

                    do! insertTeamAsync conn trans team |> Async.Ignore

                    do! trans.CommitAsync() |> Async.AwaitTask

                    return DataCreationResponse.Success
                with
                | :? AggregateException as ae ->
                    ae.InnerExceptions |> Seq.iter logError
                    ae.Flatten().Handle(postgresExceptionCatcher)
                    return DataCreationResponse.Failure
            }|> Async.StartAsTask


        member this.DeleteMatchAsync (matchId : int) : Task<Unit> = 
            async {
                try
                    use conn = getConnection connString
                    do! conn.OpenAsync() |> Async.AwaitTask
                    use trans = conn.BeginTransaction()
                    try
                        do! executeNonQueryAsync conn trans "SELECT delete_match(@matchId);" (Map.ofList [ "matchId", box matchId ])
                    with
                    | :? AggregateException as ae ->
                        do! trans.RollbackAsync() |> Async.AwaitTask
                        ae.InnerExceptions |> Seq.iter logError
                        ae.Flatten().Handle(postgresExceptionCatcher); ()
                with
                | :? AggregateException as ae ->
                    ae.InnerExceptions |> Seq.iter logError
                    ae.Flatten().Handle(postgresExceptionCatcher); ()
            } |> Async.StartAsTask


        member this.DeleteMatchAsync (homeTeamId : string, awayTeamId : string, date : DateTime) : Task<Unit> = 
            async {
                try
                    use conn = getConnection connString
                    do! conn.OpenAsync() |> Async.AwaitTask
                    use trans = conn.BeginTransaction()
                    try
                        do!
                            Map.ofList [ "homeTeamId", box homeTeamId; "awayTeamId", box awayTeamId; "date", box date ]
                            |> executeNonQueryAsync conn trans "SELECT delete_match(@homeTeamId, @awayTeamId, @date);"
                        do! trans.CommitAsync() |> Async.AwaitTask
                    with
                    | :? AggregateException as ae ->
                        do! trans.RollbackAsync() |> Async.AwaitTask
                        ae.InnerExceptions |> Seq.iter logError
                        ae.Flatten().Handle(postgresExceptionCatcher); ()
                with
                | :? AggregateException as ae ->
                    ae.InnerExceptions |> Seq.iter logError
                    ae.Flatten().Handle(postgresExceptionCatcher); ()
            } |> Async.StartAsTask