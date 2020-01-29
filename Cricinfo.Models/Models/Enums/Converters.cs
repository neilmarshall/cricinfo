using System;

namespace Cricinfo.Models.Enums
{
    public static class Converters
    {
        public static string ConvertResult(Result result) =>
            result switch
            {
                Result.HomeTeamWin => "Home Team Win",
                Result.AwayTeamWin => "Away Team Win",
                Result.Draw => "Draw",
                Result.Tie => "Tie",
                _ => throw new ArgumentException("bad value for 'Cricinfo.Models.Enums.Result' enum")
            };

        public static string ConvertDismissal(Dismissal dismissal) =>
            dismissal switch
            {
                Dismissal.Bowled => "Bowled",
                Dismissal.Caught => "Caught",
                Dismissal.CaughtAndBowled => "Caught and bowled",
                Dismissal.LBW => "LBW",
                Dismissal.NotOut => "Not out",
                Dismissal.RunOut => "Run out",
                _ => throw new ArgumentException("bad value for 'Cricinfo.Models.Enums.Dismissal' enum")
            };

        public static string ConvertMatchType(MatchType matchType) =>
            matchType switch
            {
                MatchType.TestMatch => "Test Match",
                MatchType.ODI => "One Day International",
                MatchType.T20 => "Twenty-Twenty",
                _ => throw new ArgumentException("bad value for 'Cricinfo.Models.Enums.MatchType' enum")
            };
    }
}
