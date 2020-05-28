namespace Cricinfo.Services

module private PostgresDbFunctions =

    open System
    open Npgsql
    open Cricinfo.Models
    open Cricinfo.Models.Enums
    open Cricinfo.Parser

    let internal getConnection connString = new NpgsqlConnection(connString)

    let private executeNonQuery (conn : NpgsqlConnection) (trans : NpgsqlTransaction) (query : string) (parameters : Map<string, obj>) : Unit =
        use command = new NpgsqlCommand(query, conn, trans)
        parameters |> Map.iter (fun k v -> command.Parameters.AddWithValue(k, v) |> ignore)
        do command.ExecuteNonQuery() |> ignore

    let internal executeNonQueryAsync (conn : NpgsqlConnection) (trans : NpgsqlTransaction) (query : string) (parameters : Map<string, obj>) : Async<Unit> =
        async {
            use command = new NpgsqlCommand(query, conn, trans)
            parameters |> Map.iter (fun k v -> command.Parameters.AddWithValue(k, v) |> ignore)
            do! command.ExecuteNonQueryAsync() |> Async.AwaitTask |> Async.Ignore
        }

    let private executeScalar<'T> (conn : NpgsqlConnection) (trans : NpgsqlTransaction) (query : string) (parameters : Map<string, obj>) : 'T =
        use command = new NpgsqlCommand(query, conn, trans)
        parameters |> Map.iter (fun k v -> command.Parameters.AddWithValue(k, v) |> ignore)
        command.ExecuteScalar() :?> 'T

    let private executeScalarAsync<'T> (conn : NpgsqlConnection) (trans : NpgsqlTransaction) (query : string) (parameters : Map<string, obj>) : Async<'T> =
        async {
            use command = new NpgsqlCommand(query, conn, trans)
            parameters |> Map.iter (fun k v -> command.Parameters.AddWithValue(k, v) |> ignore)
            let! response =  command.ExecuteScalarAsync() |> Async.AwaitTask
            return response :?> 'T
        }

    let checkMatchExistsAsync (conn : NpgsqlConnection) (trans : NpgsqlTransaction) (homeTeam : string) (awayTeam : string) (date : DateTime) : Async<bool> =
        async {
            let query = "SELECT * FROM check_match_exists(@home_team, @away_team, @date_of_first_day);"
            let parameters = Map.ofList [ "home_team", box homeTeam; "away_team", box awayTeam; "date_of_first_day", box date ]
            return! executeScalarAsync conn trans query parameters
        }

    let getIdsAsync (conn : NpgsqlConnection) (trans : NpgsqlTransaction) (venue : string) (homeTeam : string) (awayTeam : string) : Async<int * int * int> = 
        async {
            let! venueId = executeScalarAsync conn trans "SELECT * FROM get_id_and_insert_if_not_exists_venue(@venue);" (Map.ofList [ "venue", box venue ])
            let! homeTeamId = executeScalarAsync conn trans "SELECT * FROM get_id_and_insert_if_not_exists_team(@team);" (Map.ofList [ "team", box homeTeam ])
            let! awayTeamId = executeScalarAsync conn trans "SELECT * FROM get_id_and_insert_if_not_exists_team(@team);" (Map.ofList [ "team", box awayTeam ])
            return venueId, homeTeamId, awayTeamId
        }

    let getNextMatchIdAsync (conn : NpgsqlConnection) (trans : NpgsqlTransaction) : Async<int64> =
        async {
            return! executeScalarAsync conn trans "SELECT NEXTVAL('match_id_seq');" Map.empty
        }

    let insertMatchAsync
        (conn : NpgsqlConnection)
        (trans : NpgsqlTransaction)
        (matchId : int64)
        (date : DateTime)
        (venueId : int)
        (homeTeamId : int)
        (awayTeamId : int)
        (result : Result)
        (matchType : MatchType)
            : Async<unit> =
        async {
            let! resultId = executeScalarAsync<int> conn trans "SELECT id FROM result WHERE type = @result" (Map.ofList [ "result", box (result.ToString()) ])
            let! matchTypeId = executeScalarAsync<int> conn trans "SELECT id FROM match_type WHERE type = @matchType" (Map.ofList [ "matchType", box (matchType.ToString()) ])
            let query = "INSERT INTO match (id, match_type_id, date_of_first_day, venue_id, hometeam_id, awayteam_id, result_id) VALUES (@id, @matchTypeId, @date, @venue_id, @hometeam_id, @awayteam_id, @result_id);"
            let parameters = Map.ofList [ "id", box matchId; "matchTypeId", box matchTypeId; "date", box date; "venue_id", box venueId; "hometeam_id", box homeTeamId; "awayteam_id", box awayTeamId; "result_id", box resultId ]
            do! executeNonQueryAsync conn trans query parameters
        }

    let insertSquadAsync
        (conn : NpgsqlConnection)
        (trans : NpgsqlTransaction)
        (matchId : int64)
        (teamId : int)
        (squad : seq<string>)
            : Async<Map<string, int>> =
        async {
            let parseSquadMember (name : string * string * string) : string * int =
                let firstName, lastName, lookupCode = name
                let playerId =
                    Map.ofList [ "firstname", box firstName; "lastname", box lastName ]
                    |> executeScalar conn trans "SELECT * FROM get_id_and_insert_if_not_exists_player(@firstname, @lastname);"
                Map.ofList [ "match_id", box matchId; "team_id", box teamId; "player_id", box playerId ]
                |> executeScalar conn trans "INSERT INTO squad (match_id, team_id, player_id) VALUES (@match_id, @team_id, @player_id);"
                lookupCode, playerId
            let squadNames = Parse.parseNames squad
            let playerIds = squadNames |> Seq.map parseSquadMember
            return playerIds |> Map.ofSeq
        }

    let private insertBattingScores
        (conn : NpgsqlConnection)
        (trans : NpgsqlTransaction)
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
        (conn : NpgsqlConnection)
        (trans : NpgsqlTransaction)
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
        (conn : NpgsqlConnection)
        (trans : NpgsqlTransaction)
        (inningsId : int64)
        (fallOfWicket : seq<int>)
            : Unit =
        fallOfWicket
        |> Seq.iteri (fun wickets runs ->
            let query = "INSERT INTO fall_of_wicket_scorecard (innings_id, runs, wickets) VALUES (@innings_id, @runs, @wickets);"
            let parameters = Map.ofList [ "innings_id", box inningsId; "runs", box runs; "wickets", box (wickets + 1); ]
            executeNonQuery conn trans query parameters)

    let insertInningsAsync
        (conn : NpgsqlConnection)
        (trans : NpgsqlTransaction)
        (tryGetPlayerId : string -> int)
        (matchId : int64)
        (innings : seq<Score>)
            : Async<Unit> =
        async {
            let insertSingleInnings (innings : Score) : Unit =
                let id = executeScalar conn trans "SELECT NEXTVAL('innings_id_seq');" Map.empty
                let teamId = executeScalar<int> conn trans "SELECT * FROM get_id_and_insert_if_not_exists_team(@team);" (Map.ofList [ "team", box innings.Team ])
                let query = "INSERT INTO innings (id, match_id, team_id, innings, extras) VALUES (@id, @match_id, @team_id, @innings, @extras);"
                let parameters = Map.ofList [ "id", box id; "match_id", box matchId; "team_id", box teamId; "innings", box innings.Innings;
                                              "extras", box innings.Extras; ]
                executeNonQuery conn trans query parameters
                insertBattingScores conn trans tryGetPlayerId id innings.BattingScorecard
                insertBowlingScores conn trans tryGetPlayerId id innings.BowlingScorecard
                insertFallOfWicket conn trans id innings.FallOfWicketScorecard
            innings |> Seq.iter insertSingleInnings
        }
