namespace Cricinfo.Services

open System
open System.Threading.Tasks
open Cricinfo.Models
open SqlServerDbFunctions

type public SqlServerCricInfoRepository(connString : string) =
    
    let sqlServerExceptionCatcher = new Func<exn, bool>(function | :? System.Data.SqlClient.SqlException -> true | _ -> false)

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
                    let insertMatchAsync = insertMatchAsync conn trans
                    let insertSquadAsync = insertSquadAsync conn trans
                    let insertInningsAsync = insertInningsAsync conn trans

                    try
                        let! matchExists = checkMatchExistsAsync mtch.HomeTeam mtch.AwayTeam mtch.DateOfFirstDay
                        if matchExists then
                            return DataCreationResponse.DuplicateContent, Nullable()
                        else
                            let! venueId, homeTeamId, awayTeamId = getIdsAsync mtch.Venue mtch.HomeTeam mtch.AwayTeam
                            let! matchId = insertMatchAsync mtch.DateOfFirstDay venueId homeTeamId awayTeamId mtch.Result mtch.MatchType
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
                        ae.Flatten().Handle(sqlServerExceptionCatcher)
                        return DataCreationResponse.Failure, Nullable()
                with
                | :? AggregateException as ae ->
                    ae.Flatten().Handle(sqlServerExceptionCatcher)
                    return DataCreationResponse.Failure, Nullable()
            } |> Async.StartAsTask


        member this.DeleteMatchAsync (matchId : int) : Task<Unit> = 
            async {
                try
                    use conn = getConnection connString
                    do! conn.OpenAsync() |> Async.AwaitTask
                    use trans = conn.BeginTransaction()
                    try
                        do! executeStoredProcedureNonQueryAsync conn trans "delete_match_by_id" (Map.ofList [ "matchid", box matchId ])
                    with
                    | :? AggregateException as ae ->
                        do! trans.RollbackAsync() |> Async.AwaitTask
                        ae.Flatten().Handle(sqlServerExceptionCatcher); ()
                with
                | :? AggregateException as ae ->
                    ae.Flatten().Handle(sqlServerExceptionCatcher); ()
            } |> Async.StartAsTask


        member this.DeleteMatchAsync (homeTeamId : string, awayTeamId : string, date : DateTime) : Task<Unit> = 
            async {
                try
                    use conn = getConnection connString
                    do! conn.OpenAsync() |> Async.AwaitTask
                    use trans = conn.BeginTransaction()
                    try
                        do!
                            Map.ofList [ "home_team_name", box homeTeamId; "away_team_name", box awayTeamId; "date_of_first_day", box date ]
                            |> executeStoredProcedureNonQueryAsync conn trans "delete_match"
                        do! trans.CommitAsync() |> Async.AwaitTask
                    with
                    | :? AggregateException as ae ->
                        do! trans.RollbackAsync() |> Async.AwaitTask
                        ae.Flatten().Handle(sqlServerExceptionCatcher); ()
                with
                | :? AggregateException as ae ->
                    ae.Flatten().Handle(sqlServerExceptionCatcher); ()
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
                    ae.Flatten().Handle(sqlServerExceptionCatcher)
                    return false
            } |> Async.StartAsTask
