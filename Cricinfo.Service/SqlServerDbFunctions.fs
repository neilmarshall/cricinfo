namespace Cricinfo.Services

module private SqlServerDbFunctions =

    open System
    open System.Data
    open Cricinfo.Models
    open Cricinfo.Models.Enums
    open Cricinfo.Parser
    open System.Data.SqlClient

    let internal getConnection connString = new SqlConnection(connString)

    let private executeNonQuery (conn : SqlConnection) (trans : SqlTransaction) (query : string) (parameters : Map<string, obj>) : Unit =
        use command = new SqlCommand(query, conn, trans)
        parameters |> Map.iter (fun k v -> command.Parameters.AddWithValue(k, v) |> ignore)
        do command.ExecuteNonQuery() |> ignore

    let internal executeNonQueryAsync (conn : SqlConnection) (trans : SqlTransaction) (query : string) (parameters : Map<string, obj>) : Async<Unit> =
        async {
            use command = new SqlCommand(query, conn, trans)
            parameters |> Map.iter (fun k v -> command.Parameters.AddWithValue(k, v) |> ignore)
            do! command.ExecuteNonQueryAsync() |> Async.AwaitTask |> Async.Ignore
        }

    let private executeScalar<'T> (conn : SqlConnection) (trans : SqlTransaction) (query : string) (parameters : Map<string, obj>) : 'T =
        use command = new SqlCommand(query, conn, trans)
        parameters |> Map.iter (fun k v -> command.Parameters.AddWithValue(k, v) |> ignore)
        command.ExecuteScalar() :?> 'T

    let private executeScalarAsync<'T> (conn : SqlConnection) (trans : SqlTransaction) (query : string) (parameters : Map<string, obj>) : Async<'T> =
        async {
            use command = new SqlCommand(query, conn, trans)
            parameters |> Map.iter (fun k v -> command.Parameters.AddWithValue(k, v) |> ignore)
            let! response =  command.ExecuteScalarAsync() |> Async.AwaitTask
            return response :?> 'T
        }

    let private executeStoredProcedureScalar<'T>
        (conn : SqlConnection)
        (trans : SqlTransaction)
        (query : string)
        (parameters : Map<string, obj>)
        (outputParameterName : string, outputParameterType : SqlDbType)
            : 'T =
        use command = new SqlCommand(query, conn, trans)
        command.CommandType <- Data.CommandType.StoredProcedure
        parameters |> Map.iter (fun k v -> command.Parameters.AddWithValue(k, v) |> ignore)
        let outputParameter = SqlParameter(outputParameterName, outputParameterType, 1)
        outputParameter.Direction <- ParameterDirection.Output
        command.Parameters.Add(outputParameter) |> ignore
        command.ExecuteScalar() |> ignore
        outputParameter.Value :?> 'T

    let private executeStoredProcedureScalarAsync<'T>
        (conn : SqlConnection)
        (trans : SqlTransaction)
        (query : string)
        (parameters : Map<string, obj>)
        (outputParameterName : string, outputParameterType : SqlDbType)
            : Async<'T> =
        async {
            use command = new SqlCommand(query, conn, trans)
            command.CommandType <- Data.CommandType.StoredProcedure
            parameters |> Map.iter (fun k v -> command.Parameters.AddWithValue(k, v) |> ignore)
            let outputParameter = SqlParameter(outputParameterName, outputParameterType, 1)
            outputParameter.Direction <- ParameterDirection.Output
            command.Parameters.Add(outputParameter) |> ignore
            do! command.ExecuteScalarAsync() |> Async.AwaitTask |> Async.Ignore
            return outputParameter.Value :?> 'T
        }

    let internal executeStoredProcedureNonQueryAsync (conn : SqlConnection) (trans : SqlTransaction) (query : string) (parameters : Map<string, obj>) : Async<Unit> =
        async {
            use command = new SqlCommand(query, conn, trans)
            command.CommandType <- Data.CommandType.StoredProcedure
            parameters |> Map.iter (fun k v -> command.Parameters.AddWithValue(k, v) |> ignore)
            do! command.ExecuteNonQueryAsync() |> Async.AwaitTask |> Async.Ignore
        }

    let checkMatchExistsAsync (conn : SqlConnection) (trans : SqlTransaction) (homeTeam : string) (awayTeam : string) (date : DateTime) : Async<bool> =
        async {
            let query = "check_match_exists"
            let parameters = Map.ofList [ "home_team_name", box homeTeam; "away_team_name", box awayTeam; "date_of_first_day", box date ]
            return! executeStoredProcedureScalarAsync<bool> conn trans query parameters ("exists", SqlDbType.Bit)
        }

    let getIdsAsync (conn : SqlConnection) (trans : SqlTransaction) (venue : string) (homeTeam : string) (awayTeam : string) : Async<int * int * int> = 
        async {
            let! venueId = executeStoredProcedureScalarAsync conn trans "get_id_and_insert_if_not_exists_venue" (Map.ofList [ "venue", box venue ]) ("id", SqlDbType.Int)
            let! homeTeamId = executeStoredProcedureScalarAsync conn trans "get_id_and_insert_if_not_exists_team" (Map.ofList [ "team", box homeTeam ]) ("id", SqlDbType.Int)
            let! awayTeamId = executeStoredProcedureScalarAsync conn trans "get_id_and_insert_if_not_exists_team" (Map.ofList [ "team", box awayTeam ]) ("id", SqlDbType.Int)
            return venueId, homeTeamId, awayTeamId
        }

    let insertMatchAsync
        (conn : SqlConnection)
        (trans : SqlTransaction)
        (date : DateTime)
        (venueId : int)
        (homeTeamId : int)
        (awayTeamId : int)
        (result : Result)
        (matchType : MatchType)
            : Async<int64> =
        async {
            let! resultId = executeScalarAsync<int> conn trans "SELECT id FROM result WHERE type = @result" (Map.ofList [ "result", box (result.ToString()) ])
            let! matchTypeId = executeScalarAsync<int> conn trans "SELECT id FROM match_type WHERE type = @matchType" (Map.ofList [ "matchType", box (matchType.ToString()) ])
            let query = "INSERT INTO match (match_type_id, date_of_first_day, venue_id, hometeam_id, awayteam_id, result_id) VALUES (@matchTypeId, @date, @venue_id, @hometeam_id, @awayteam_id, @result_id);"
            let parameters = Map.ofList [ "matchTypeId", box matchTypeId; "date", box date; "venue_id", box venueId; "hometeam_id", box homeTeamId; "awayteam_id", box awayTeamId; "result_id", box resultId ]
            do! executeNonQueryAsync conn trans query parameters
            let! id = executeScalarAsync<Decimal> conn trans "SELECT IDENT_CURRENT('match');" Map.empty
            return int64(id)
        }

    let insertSquadAsync
        (conn : SqlConnection)
        (trans : SqlTransaction)
        (matchId : int64)
        (teamId : int)
        (squad : seq<string>)
            : Async<Map<string, int>> =
        async {
            let parseSquadMember (name : string * string * string) : string * int =
                let firstName, lastName, lookupCode = name
                let playerId =
                    let parameters = Map.ofList [ "firstname", box firstName; "lastname", box lastName ]
                    executeStoredProcedureScalar conn trans "get_id_and_insert_if_not_exists_player" parameters ("id", SqlDbType.Int)
                Map.ofList [ "match_id", box matchId; "team_id", box teamId; "player_id", box playerId ]
                |> executeScalar conn trans "INSERT INTO squad (match_id, team_id, player_id) VALUES (@match_id, @team_id, @player_id);"
                lookupCode, playerId
            let squadNames = Parse.parseNames squad
            let playerIds = squadNames |> Seq.map parseSquadMember
            return playerIds |> Map.ofSeq
        }

    let private insertBattingScores
        (conn : SqlConnection)
        (trans : SqlTransaction)
        (tryGetPlayerId : string -> int)
        (inningsId : int64)
        (battingScorecards : seq<BattingScorecard>)
            : Unit =
        let insertBattingScorecard (bs : BattingScorecard) : Unit =
            let howOutId = executeScalar<int> conn trans "SELECT id FROM how_out WHERE type = @type" (Map.ofList [ "type", box (bs.Dismissal.ToString()) ])
            let batsmanId = tryGetPlayerId bs.Name
            let catcherId =
                match bs.Dismissal with
                | Dismissal.Caught  -> if bs.Catcher = "sub" then None else tryGetPlayerId bs.Catcher |> Some
                | Dismissal.CaughtAndBowled -> tryGetPlayerId bs.Bowler |> Some
                | _ -> None
            let bowlerId =
                match bs.Dismissal with
                | Dismissal.NotOut | Dismissal.RunOut | Dismissal.Retired -> None
                | _ -> tryGetPlayerId bs.Bowler |> Some
            let query = "INSERT INTO batting_scorecard (innings_id, batsman_id, how_out_id, catcher_id, bowler_id, runs, mins, balls, fours, sixes) VALUES (@innings_id, @batsman_id, @how_out_id, @catcher_id, @bowler_id, @runs, @mins, @balls, @fours, @sixes);"
            let parameters = Map.ofList [ "innings_id", box inningsId; "batsman_id", box batsmanId; "how_out_id", box howOutId;
                                          "catcher_id", match catcherId with | Some id -> box id | None -> box DBNull.Value;
                                          "bowler_id", match bowlerId with | Some id -> box id | None -> box DBNull.Value;
                                          "runs", box bs.Runs; "mins", box bs.Mins; "balls", box bs.Balls; "fours", box bs.Fours ; "sixes", box bs.Sixes ]
            executeNonQuery conn trans query parameters
        battingScorecards |> Seq.iter insertBattingScorecard

    let private insertBowlingScores
        (conn : SqlConnection)
        (trans : SqlTransaction)
        (tryGetPlayerId : string -> int)
        (inningsId : int64)
        (bowlingScorecards : seq<BowlingScorecard>)
            : Unit =
        let insertBowlingScorecard (bs : BowlingScorecard) : Unit =
            let bowlerId = tryGetPlayerId bs.Name
            let query = "INSERT INTO bowling_scorecard (innings_id, bowler_id, overs, maidens, runs_conceded, wickets) VALUES (@innings_id, @bowler_id, @overs, @maidens, @runs_conceded, @wickets);"
            let parameters = Map.ofList [ "innings_id", box inningsId; "bowler_id", box bowlerId; "overs", box bs.Overs; "maidens", box bs.Maidens;
                                          "runs_conceded", box bs.Runs; "wickets", box bs.Wickets; ]
            executeNonQuery conn trans query parameters
        bowlingScorecards |> Seq.iter insertBowlingScorecard

    let private insertFallOfWicket
        (conn : SqlConnection)
        (trans : SqlTransaction)
        (inningsId : int64)
        (fallOfWicket : seq<int>)
            : Unit =
        fallOfWicket
        |> Seq.iteri (fun wickets runs ->
            let query = "INSERT INTO fall_of_wicket_scorecard (innings_id, runs, wickets) VALUES (@innings_id, @runs, @wickets);"
            let parameters = Map.ofList [ "innings_id", box inningsId; "runs", box runs; "wickets", box (wickets + 1); ]
            executeNonQuery conn trans query parameters)

    let insertInningsAsync
        (conn : SqlConnection)
        (trans : SqlTransaction)
        (tryGetPlayerId : string -> int)
        (matchId : int64)
        (innings : seq<Score>)
            : Async<Unit> =
        async {
            let insertSingleInnings (innings : Score) : Unit =
                let teamId = executeStoredProcedureScalar conn trans "get_id_and_insert_if_not_exists_team" (Map.ofList [ "team", box innings.Team ]) ("id", SqlDbType.Int)
                let query = "INSERT INTO innings (match_id, team_id, innings, extras) VALUES (@match_id, @team_id, @innings, @extras);"
                let parameters = Map.ofList [ "match_id", box matchId; "team_id", box teamId; "innings", box innings.Innings;
                                              "extras", box innings.Extras; ]
                let id = executeScalar<Decimal> conn trans "SELECT IDENT_CURRENT('innings');" Map.empty |> int64
                executeNonQuery conn trans query parameters
                insertBattingScores conn trans tryGetPlayerId id innings.BattingScorecard
                insertBowlingScores conn trans tryGetPlayerId id innings.BowlingScorecard
                insertFallOfWicket conn trans id innings.FallOfWicketScorecard
            innings |> Seq.iter insertSingleInnings
        }
