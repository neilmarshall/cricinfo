namespace Cricinfo.Parser

module private DomainModel =

    open System
    open System.Text.RegularExpressions
    open Exceptions

    let private trim (s : string) = s.Trim()

    type Player = Player of string

    type BattingFigures = { runs : int; mins : int; balls : int; fours : int; sixes : int }

    type Bowled = { name : Player; bowler : Player; figures : BattingFigures }
    type Caught = { name : Player; catcher : Player; bowler : Player; figures : BattingFigures }
    type CaughtAndBowled = { name : Player; catcher : Player; bowler : Player; figures : BattingFigures }
    type LBW = { name : Player; bowler : Player; figures : BattingFigures }
    type NotOut = { name : Player; figures : BattingFigures }

    type Batsman = | Bowled of Bowled | Caught of Caught | CaughtAndBowled of CaughtAndBowled | LBW of LBW | NotOut of NotOut with

        member this.Name =
            match this with
            | Bowled {name = name} | Caught {name = name} | CaughtAndBowled {name = name} | LBW {name = name} | NotOut {name = name}
                -> name

        member this.Catcher =
            match this with
            | Caught {catcher = catcher} | CaughtAndBowled {catcher = catcher} -> Some catcher
            | Bowled _ | LBW _ | NotOut _ -> None

        member this.Bowler =
            match this with
            | Bowled {bowler = bowler} | Caught {bowler = bowler} | CaughtAndBowled {bowler = bowler} | LBW {bowler = bowler} -> Some bowler
            | NotOut _ -> None

        member this.Runs =
            match this with
            | Bowled {figures = {runs = runs}} | Caught {figures = {runs = runs}} | CaughtAndBowled {figures = {runs = runs}} | LBW {figures = {runs = runs}} | NotOut {figures = {runs = runs}}
                -> runs

        member this.Mins =
            match this with
            | Bowled {figures = {mins = mins}} | Caught {figures = {mins = mins}} | CaughtAndBowled {figures = {mins = mins}} | LBW {figures = {mins = mins}} | NotOut {figures = {mins = mins}}
                -> mins

        member this.Balls =
            match this with
            | Bowled {figures = {balls = balls}} | Caught {figures = {balls = balls}} | CaughtAndBowled {figures = {balls = balls}} | LBW {figures = {balls = balls}} | NotOut {figures = {balls = balls}}
                -> balls

        member this.Fours =
            match this with
            | Bowled {figures = {fours = fours}} | Caught {figures = {fours = fours}} | CaughtAndBowled {figures = {fours = fours}} | LBW {figures = {fours = fours}} | NotOut {figures = {fours = fours}}
                -> fours

        member this.Sixes =
            match this with
            | Bowled {figures = {sixes = sixes}} | Caught {figures = {sixes = sixes}} | CaughtAndBowled {figures = {sixes = sixes}} | LBW {figures = {sixes = sixes}} | NotOut {figures = {sixes = sixes}}
                -> sixes

        member this.StrikeRate =
            match this with
            | Bowled {figures = {runs = r; balls = b}} | Caught {figures = {runs = r; balls = b}} | CaughtAndBowled {figures = {runs = r; balls = b}} | LBW {figures = {runs = r; balls = b}} | NotOut {figures = {runs = r; balls = b}}
                -> Math.Round(float r / float b * 100.0, 2)

        static member Score score =
            let parseBattingFigures (inputString : string) =
                match inputString.Trim().Split() |> Seq.filter (System.String.IsNullOrWhiteSpace >> not) |> Seq.map Int32.Parse |> Seq.toList with
                | [runs; mins; balls; fours; sixes] -> { runs = runs; mins = mins; balls = balls; fours = fours; sixes = sixes }
                | _ -> inputString |> sprintf "invalid figures in batting data - '%s'" |> BattingFiguresException |> raise
            let caught =
                let result = Regex("^(?<batsman>[A-Za-z\s]+)\sc\s(?<catcher>[A-Za-z\s]+)\sb\s(?<bowler>[A-Za-z\s]+)\s+(?<figures>(\d+\s+){5})").Match(score)
                if result.Success then
                    Caught { name = result.Groups.["batsman"].Value |> trim |> Player;
                             catcher = result.Groups.["catcher"].Value |> trim |> Player;
                             bowler = result.Groups.["bowler"].Value |> trim |> Player;
                             figures = result.Groups.["figures"].Value |> parseBattingFigures } |> Some
                else None
            let lbw =
                let result = Regex("^(?<batsman>[A-Za-z\s]+)lbw\sb\s(?<bowler>[A-Za-z\s]+)\s+(?<figures>(\d+\s+){5})").Match(score)
                if result.Success then
                    LBW { name = result.Groups.["batsman"].Value |> trim |> Player;
                          bowler = result.Groups.["bowler"].Value |> trim |> Player;
                          figures = result.Groups.["figures"].Value |> parseBattingFigures } |> Some
                else None
            let caughtAndBowled =
                let result = Regex("^(?<batsman>[A-Za-z\s]+)\sc & b\s(?<bowler>[A-Za-z\s]+)\s+(?<figures>(\d+\s+){5})").Match(score)
                if result.Success then
                    CaughtAndBowled { name = result.Groups.["batsman"].Value |> trim |> Player;
                                      catcher = result.Groups.["bowler"].Value |> trim |> Player;
                                      bowler = result.Groups.["bowler"].Value |> trim |> Player;
                                      figures = result.Groups.["figures"].Value |> parseBattingFigures } |> Some
                else None
            let bowled =
                let result = Regex("^(?<batsman>[A-Za-z\s]+)\sb\s(?<bowler>[A-Za-z\s]+)\s+(?<figures>(\d+\s+){5})").Match(score)
                if result.Success then
                    Bowled { name = result.Groups.["batsman"].Value |> trim |> Player;
                             bowler = result.Groups.["bowler"].Value |> trim |> Player;
                             figures = result.Groups.["figures"].Value |> parseBattingFigures } |> Some
                else None
            let notout =
                let result = Regex("^(?<batsman>[A-Za-z\s]+)\s+not out\s+(?<figures>(\d+\s+){5})").Match(score)
                if result.Success then
                    NotOut { name = result.Groups.["batsman"].Value |> trim |> Player;
                             figures = result.Groups.["figures"].Value |> parseBattingFigures } |> Some
                else None
            match seq { yield caught; yield lbw; yield caughtAndBowled; yield bowled; yield notout } |> Seq.tryFind Option.isSome |> Option.flatten with
            | Some dismissal -> dismissal
            | None -> score |> sprintf "invalid data in batting scorecard - '%s'" |> BattingFiguresException |> raise

    type BowlingFigures = { overs : float; maidens : int; runs : int; wickets : int }

    type Bowler = { name : Player; figures : BowlingFigures } with

        member this.Economy =
            let totalBalls = Math.Floor(this.figures.overs) * 6.0 + Math.Round(this.figures.overs - Math.Floor(this.figures.overs), 1) * 10.0
            Math.Round(float this.figures.runs / (totalBalls / 6.0), 2)

        static member Score score =
            let parseBowlingFigures (inputString : string) =
                match inputString.Trim().Split() |> Seq.filter (System.String.IsNullOrWhiteSpace >> not) |> Seq.map Double.Parse |> Seq.toList with
                | [overs; maidens; runs; wickets] -> { overs = overs; maidens = int maidens; runs = int runs; wickets = int wickets }
                | _ -> inputString |> sprintf "invalid figures in bowling data - '%s'" |> BowlingFiguresException |> raise
            let result = Regex("^(?<bowler>[A-Za-z\s]+)(?<figures>([\d\.]+\s+){4})").Match(score)
            if result.Success then
                { name = result.Groups.["bowler"].Value |> trim |> Player;
                  figures = result.Groups.["figures"].Value |> parseBowlingFigures }
            else score |> sprintf "invalid data in bowling scorecard - '%s'" |> BowlingFiguresException |> raise

    type FallOfWicket = { runs : int; wicket : int } with

        static member Parse (inputString : string) =
            let result = Regex("^(?<runs>\d+)-(?<wicket>\d+)").Match(inputString.Trim())
            if result.Success
                then { runs = result.Groups.["runs"].Value |> int; wicket = result.Groups.["wicket"].Value |> int }
                else inputString |> sprintf "invalid figures in input - '%s'" |> FallOfWicketException |> raise
