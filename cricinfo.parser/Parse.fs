namespace Cricinfo.Parser

module Parse =
    
    open Cricinfo.Api.Models
    open DomainModel

    let private convertPlayerName = function Player p -> p

    let private convertPlayerNameOption = Option.map convertPlayerName >> function | Some s -> s | None -> null

    let private convertPlayerDismissal = function
        | Bowled _ -> Dismissal.Bowled
        | Caught _ -> Dismissal.Caught
        | CaughtAndBowled _ -> Dismissal.CaughtAndBowled
        | LBW _ -> Dismissal.LBW
        | NotOut _ -> Dismissal.NotOut

    let parseBattingScorecard (scorecard : string) : seq<BattingScorecard> =
        scorecard.Split('\n')
        |> Seq.map Batsman.Score
        |> Seq.map (fun batsman ->
            new BattingScorecard(
                Name = convertPlayerName batsman.Name,
                Dismissal = convertPlayerDismissal batsman,
                Catcher = convertPlayerNameOption batsman.Catcher,
                Bowler = convertPlayerNameOption batsman.Bowler,
                Runs = batsman.Runs,
                Mins = batsman.Mins,
                Balls = batsman.Balls,
                Fours = batsman.Fours,
                Sixes = batsman.Sixes))

    let parseBowlingScorecard (scorecard : string) : seq<BowlingScorecard> =
        scorecard.Split('\n')
        |> Seq.map Bowler.Score
        |> Seq.map (fun bowler ->
            new BowlingScorecard(
                Name = convertPlayerName bowler.name,
                Overs = float32 bowler.figures.overs,
                Maidens = bowler.figures.maidens,
                Runs = bowler.figures.runs,
                Wickets = bowler.figures.wickets))

    let parseFallOfWicketScorecard (scorecard : string) : int [] =
        scorecard.Split('\n')
        |> Seq.map FallOfWicket.Parse
        |> Seq.map (fun fow -> fow.runs)
        |> Seq.toArray

