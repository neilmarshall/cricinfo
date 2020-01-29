﻿namespace Cricinfo.Parser

module Parse =

    open Cricinfo.Models
    open DomainModel

    let private convertPlayerName = function Player p -> p

    let private convertPlayerNameOption = Option.map convertPlayerName >> function | Some s -> s | None -> null

    let private convertPlayerDismissal = function
        | Bowled _ -> Dismissal.Bowled
        | Caught _ -> Dismissal.Caught
        | CaughtAndBowled _ -> Dismissal.CaughtAndBowled
        | LBW _ -> Dismissal.LBW
        | NotOut _ -> Dismissal.NotOut
        | RunOut _ -> Dismissal.RunOut

    let parseBattingScorecard (scorecard : string) : seq<BattingScorecard> =
        scorecard.Trim().Split('\n')
        |> Seq.map Batsman.Score
        |> Seq.map (fun (batsman : Batsman) ->
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
        scorecard.Trim().Split('\n')
        |> Seq.map Bowler.Score
        |> Seq.map (fun bowler ->
            new BowlingScorecard(
                Name = convertPlayerName bowler.name,
                Overs = float32 bowler.figures.overs,
                Maidens = bowler.figures.maidens,
                Runs = bowler.figures.runs,
                Wickets = bowler.figures.wickets))

    let parseFallOfWicketScorecard (scorecard : string) : int [] =
        scorecard.Trim().Split('\n')
        |> Seq.map FallOfWicket.Parse
        |> Seq.map (fun fow -> fow.runs)
        |> Seq.toArray

    let parseName (name : string) : string * string =
        let stripSuffix (name : string) =
            if name.EndsWith("(c)") then
                name.[..(name.Length - 4)]
            else if name.EndsWith("(wk)") then
                name.[..(name.Length - 5)]
            else
                name
        match name.Trim().Split() |> Array.toList with
        | ["sub"] -> "", "sub"
        | [_] -> name |> sprintf "invalid format for name (%s)" |> Exceptions.PlayerNameException |> raise
        | head::tail -> head, String.concat " " tail |> stripSuffix
        | _ -> name |> sprintf "invalid format for name (%s)" |> Exceptions.PlayerNameException |> raise

    let parseNames (names : seq<string>) : seq<string * string> = Seq.map parseName names

