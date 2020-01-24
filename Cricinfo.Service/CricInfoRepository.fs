namespace Cricinfo.Services

open System
open System.Threading.Tasks
open Cricinfo.Models
open DbFunctions
open Npgsql

type public CricInfoRepository(connString : string) =
    
    let postgresExceptionCatcher = new Func<exn, bool>(function | :? Npgsql.PostgresException -> true | _ -> false)

    interface ICricInfoRepository with

        member this.GetMatchAsync (id : int) : Task<Match> =
            async {
                return Match()
            } |> Async.StartAsTask


        member this.CreateMatchAsync (mtch : Match) : Task<DataCreationResponse * Nullable<int64>> =

            async {
                try
                    use conn = new NpgsqlConnection(connString)
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
                            do! insertMatchAsync matchId mtch.DateOfFirstDay venueId homeTeamId awayTeamId mtch.Result
                            let! homeSquadIds = insertSquadAsync matchId homeTeamId mtch.HomeSquad
                            let! awaySquadIds = insertSquadAsync matchId awayTeamId mtch.AwaySquad
                            let tryGetPlayerId name =
                                seq { yield! homeSquadIds; yield! awaySquadIds }
                                |> Seq.fold (fun state x -> Map.add x.Key x.Value state) Map.empty
                                |> Map.find name
                            let! inningsIds = insertInningsAsync tryGetPlayerId matchId mtch.Scores

                            do! trans.CommitAsync() |> Async.AwaitTask

                            return DataCreationResponse.Success, Nullable(matchId)
                    with
                    | :? AggregateException as ae ->
                        do! trans.RollbackAsync() |> Async.AwaitTask
                        ae.Flatten().Handle(postgresExceptionCatcher)
                        return DataCreationResponse.Failure, Nullable()
                with
                | :? AggregateException as ae ->
                    ae.Flatten().Handle(postgresExceptionCatcher)
                    return DataCreationResponse.Failure, Nullable()
            } |> Async.StartAsTask


        member this.DeleteMatchAsync (matchId : int) : Task<Unit> = 
            async {
                try
                    use conn = new NpgsqlConnection(connString)
                    do! conn.OpenAsync() |> Async.AwaitTask
                    use trans = conn.BeginTransaction()
                    try
                        do! executeNonQueryAsync conn trans "SELECT delete_match(@matchId);" (Map.ofList [ "matchId", box matchId ])
                    with
                    | :? AggregateException as ae ->
                        do! trans.RollbackAsync() |> Async.AwaitTask
                        ae.Flatten().Handle(postgresExceptionCatcher); ()
                with
                | :? AggregateException as ae ->
                    ae.Flatten().Handle(postgresExceptionCatcher); ()
            } |> Async.StartAsTask


        member this.DeleteMatchAsync (homeTeamId : string, awayTeamId : string, date : DateTime) : Task<Unit> = 
            async {
                try
                    use conn = new NpgsqlConnection(connString)
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
                        ae.Flatten().Handle(postgresExceptionCatcher); ()
                with
                | :? AggregateException as ae ->
                    ae.Flatten().Handle(postgresExceptionCatcher); ()
            } |> Async.StartAsTask
