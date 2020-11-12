module private Cricinfo.Services.Matchdata.PostgresDbQueryFunctions

open System
open Cricinfo.Models
open Cricinfo.Models.Enums
open Cricinfo.Services.PostgresDbQueryFunctions

let genericQueryWrapper = genericQueryWrapper

let private getBowlingInfo (connString: string) (scorecardId : int) : Async<(BowlingScorecard * int) option[]> =
    let getBowlingInfo (bowlingId : int) : Async<(BowlingScorecard * int) option> =
        let responseMapper = fun (response : Data.Common.DbDataReader) ->
            async {
                return new BowlingScorecard(
                        Name=response.GetString(0),
                        Overs=response.GetFloat(1),
                        Maidens=response.GetInt32(2),
                        Runs=response.GetInt32(3),
                        Wickets=response.GetInt32(4)),
                    bowlingId
            }
        queryRecord connString "SELECT * FROM matchdata.get_bowling_info (@bowlingId);" (Map.ofList ["bowlingId", bowlingId :> obj]) responseMapper
    async {
        let! bowlingIds = queryRecordSet<int> connString "SELECT id FROM matchdata.bowling_scorecard WHERE innings_id = @scorecardId;" (Map.ofList ["scorecardId", scorecardId :> obj])
        return! bowlingIds |> Seq.map getBowlingInfo |> Async.Sequential
    }

let private getBattingInfo (connString: string) (scorecardId : int) : Async<(BattingScorecard * int) option[]> =
    let getBattingInfo (battingId : int) : Async<(BattingScorecard * int) option> =
        let responseMapper = fun (response : Data.Common.DbDataReader) ->
            async {
                return new BattingScorecard(
                        Name=response.GetString(0),
                        Dismissal=Dismissal.Parse(response.GetString(1)),
                        Catcher=(response.GetString(2) |> (fun s -> if String.IsNullOrWhiteSpace(s) then null else s)),
                        Bowler=(response.GetString(3) |> (fun s -> if String.IsNullOrWhiteSpace(s) then null else s)),
                        Runs=response.GetInt32(4),
                        Mins=response.GetInt32(5),
                        Balls=response.GetInt32(6),
                        Fours=response.GetInt32(7),
                        Sixes=response.GetInt32(8)),
                    battingId
            }
        queryRecord connString "SELECT * FROM matchdata.get_batting_info(@battingId);" (Map.ofList ["battingId", battingId :> obj]) responseMapper
    async {
        let! battingIds = queryRecordSet<int> connString "SELECT id FROM matchdata.batting_scorecard WHERE innings_id = @scorecardId;" (Map.ofList ["scorecardId", scorecardId :> obj])
        return! battingIds |> Seq.map getBattingInfo |> Async.Sequential
    }

let private getScorecardInfo (connString: string) (matchId : int) : Async<(Score * int) option[]> =
    let getScorecardInfo (scorecardId : int) =
        let responseMapper = fun (response : Data.Common.DbDataReader) ->
            async {
                let! battingOptions = getBattingInfo connString scorecardId
                let battingScorecard =
                    battingOptions
                    |> Array.filter Option.isSome
                    |> Array.map Option.get
                    |> Array.sortBy snd
                    |> Array.map fst
                let! bowlingOptions = getBowlingInfo connString scorecardId
                let bowlingScorecard =
                    bowlingOptions
                    |> Array.filter Option.isSome
                    |> Array.map Option.get
                    |> Array.sortBy snd
                    |> Array.map fst
                return new Score(
                        Team=response.GetString(0),
                        Innings=response.GetInt32(1),
                        Extras=response.GetInt32(2),
                        Declared=response.GetBoolean(3),
                        FallOfWicketScorecard=response.GetFieldValue<int[]>(4),
                        BattingScorecard=battingScorecard,
                        BowlingScorecard=bowlingScorecard),
                    scorecardId
            }
        queryRecord connString "SELECT * FROM matchdata.get_scorecard_info(@scorecardId);" (Map.ofList ["scorecardId", scorecardId :> obj]) responseMapper
    async {
        let! scorecardIds = queryRecordSet<int> connString "SELECT id FROM matchdata.innings WHERE match_id = @matchId;" (Map.ofList ["matchId", matchId :> obj])
        return! scorecardIds |> Seq.map getScorecardInfo |> Async.Sequential
    }

let getMatchInfo (connString : string) (matchId : int) : Async<Match option> =
    let responseMapper = fun (response : Data.Common.DbDataReader) ->
        async {
            let! scorecardOptions = getScorecardInfo connString matchId
            let scorecards =
                scorecardOptions
                |> Array.filter Option.isSome
                |> Array.map Option.get
                |> Array.sortBy snd
                |> Array.map fst
            return new Match(
                Venue=response.GetString(0),
                MatchType=MatchType.Parse(response.GetString(1)),
                DateOfFirstDay=response.GetDateTime(2),
                HomeTeam=response.GetString(3),
                AwayTeam=response.GetString(4),
                Result=Result.Parse(response.GetString(5)),
                HomeSquad=response.GetFieldValue<string[]>(6),
                AwaySquad=response.GetFieldValue<string[]>(7),
                Scores=scorecards)
        }
    queryRecord connString "SELECT * FROM matchdata.get_match_info(@matchId);" (Map.ofList ["matchId", matchId :> obj]) responseMapper

let getMatchIds (connString : string) : Async<int[]> =
    queryRecordSet<int> connString "SELECT id FROM matchdata.Match;" Map.empty

let getTeamsAsync (connString : string) : Async<string[]> =
    queryRecordSet<string> connString "SELECT name FROM matchdata.team;" Map.empty

let checkMatchExistsAsync (connString : string) (homeTeam : string) (awayTeam : string) (date : DateTime) : Async<bool> =
    async {
        let query = "SELECT * FROM matchdata.check_match_exists(@home_team, @away_team, @date_of_first_day);"
        let parameters = Map.ofList [ "home_team", homeTeam :> obj; "away_team", awayTeam :> obj; "date_of_first_day", date :> obj ]
        let! response = queryRecordSet<bool> connString query parameters
        return response |> Seq.head
    }
