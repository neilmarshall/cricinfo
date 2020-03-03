using System.Linq;
using Cricinfo.Parser;
using static Cricinfo.Parser.Exceptions;

namespace Cricinfo.UI.ValidationAttributes
{
    public static class DataValidator
    {
        public static int NumberOfPlayers { get => 11; }

        public static bool BattingScorecardIsValid(string scorecard)
        {
            if (string.IsNullOrEmpty(scorecard)) { return false; }

            try
            {
                Parse.parseBattingScorecard(scorecard).ToArray();
                return true;
            }
            catch (BattingFiguresException)
            {
                return false;
            }
        }

        public static bool BowlingScorecardIsValid(string scorecard)
        {
            if (string.IsNullOrEmpty(scorecard)) { return false; }

            try
            {
                Parse.parseBowlingScorecard(scorecard).ToArray();
                return true;
            }
            catch (BowlingFiguresException)
            {
                return false;
            }
        }

        public static bool FallOfWicketScorecardIsValid(string scorecard)
        {
            if (string.IsNullOrEmpty(scorecard)) { return false; }

            try
            {
                Parse.parseFallOfWicketScorecard(scorecard).ToArray();
                return true;
            }
            catch (FallOfWicketException)
            {
                return false;
            }
        }

        public static bool SquadIsValid(string squad)
        {
            if (string.IsNullOrEmpty(squad)) { return false; }

            var squadMembers = squad.Trim().Split('\n');

            if (squadMembers.Count() < NumberOfPlayers) { return false; }

            try
            {
                Parse.parseNames(squadMembers).ToArray();
                return true;
            }
            catch (PlayerNameException)
            {
                return false;
            }
        }
    }
}
