namespace Cricinfo.Services

open System
open System.Threading.Tasks
open Microsoft.Extensions.Logging
open Cricinfo.Models
open PostgresDbFunctions

type public PostgresCricInfoRepository<'T>(connString : string, logger : ILogger<'T>) =
    
    let postgresExceptionCatcher = new Func<exn, bool>(function | :? Npgsql.PostgresException -> true | _ -> false)
    let logger = if logger <> null then Some logger else None
    let logError (e : exn) =
        match logger with
        | Some logger -> logger.LogError(e.Message)
        | None -> ()

    new (connString : string) =
        PostgresCricInfoRepository(connString, null)

    interface ICricInfoRepository with

        member this.GetMatchAsync (id : int) : Task<Match> =
            async {
                return Match()
            } |> Async.StartAsTask


        member this.CreateMatchAsync (mtch : Match) : Task<DataCreationResponse * Nullable<int64>> =
            async {
                try
                    use conn = getConnection connString
                    do! conn.OpenAsync() |> Async.AwaitTask
                    use trans = conn.BeginTransaction()

                    let checkMatchExistsAsync = checkMatchExistsAsync conn trans
                    let getIdsAsync = getIdsAsync conn trans
                    let getNextMatchIdAsync = getNextMatchIdAsync conn trans
                    let insertMatchAsync = insertMatchAsync conn trans
                    let insertSquadAsync = insertSquadAsync conn trans
                    let insertInningsAsync = insertInningsAsync conn trans

                    try
                        let! matchExists = checkMatchExistsAsync mtch.HomeTeam mtch.AwayTeam mtch.DateOfFirstDay
                        if matchExists then
                            return DataCreationResponse.DuplicateContent, Nullable()
                        else
                            let! venueId, homeTeamId, awayTeamId = getIdsAsync mtch.Venue mtch.HomeTeam mtch.AwayTeam
                            let! matchId = getNextMatchIdAsync
                            do! insertMatchAsync matchId mtch.DateOfFirstDay venueId homeTeamId awayTeamId mtch.Result mtch.MatchType
                            let! homeSquadIds = insertSquadAsync matchId homeTeamId mtch.HomeSquad
                            let! awaySquadIds = insertSquadAsync matchId awayTeamId mtch.AwaySquad
                            let tryGetPlayerId name =
                                seq { yield! homeSquadIds; yield! awaySquadIds }
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


        member this.MatchExistsAsync (homeTeam : string, awayTeam : string, date : DateTime) : Task<bool> =
            async {
                try
                    use conn = getConnection connString
                    do! conn.OpenAsync() |> Async.AwaitTask
                    use trans = conn.BeginTransaction()
                    return! checkMatchExistsAsync conn trans homeTeam awayTeam date
                with
                | :? AggregateException as ae ->
                    ae.InnerExceptions |> Seq.iter logError
                    ae.Flatten().Handle(postgresExceptionCatcher)
                    return false
            } |> Async.StartAsTask


        member this.GetTeamsAsync() =
            async {
                try
                    use conn = getConnection connString
                    do! conn.OpenAsync() |> Async.AwaitTask
                    use trans = conn.BeginTransaction()
                    let! teams = getColumnAsync<string> conn trans "SELECT name FROM TEAM;" Map.empty
                    return teams |> Seq.toArray
                with
                | :? AggregateException as ae ->
                    ae.InnerExceptions |> Seq.iter logError
                    ae.Flatten().Handle(postgresExceptionCatcher)
                    return Array.empty
            } |> Async.StartAsTask
