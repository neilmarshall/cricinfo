using System.Linq;
using static Cricinfo.Models.Constants;

namespace Cricinfo.Models
{
    public class Score
    {
        public string Team { get; set; }
        public int Innings { get; set; }
        public int Extras { get; set; }
        public bool Declared { get; set; }
        public BattingScorecard[] BattingScorecard { get; set; }
        public BowlingScorecard[] BowlingScorecard { get; set; }
        public int[] FallOfWicketScorecard { get; set; }

        public string RenderBattingScore()
        {
            var totalRuns = BattingScorecard.Sum(bs => bs.Runs) + Extras;
            var totalWickets = BattingScorecard.Where(bs => bs.Dismissal != Enums.Dismissal.NotOut).Count();
            var allOut = !Declared && totalWickets == NumberOfPlayers - 1 && BattingScorecard.Where(bs => bs.Dismissal == Enums.Dismissal.NotOut).Count() == 1;
            return allOut ? $"{totalRuns} all out" : $"{totalRuns}-{totalWickets}{(Declared ? "d" : "")}";
        }
    }
}
