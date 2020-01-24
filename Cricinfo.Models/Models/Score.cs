namespace Cricinfo.Models
{
    public class Score
    {
        public string Team { get; set; }
        public int Innings { get; set; }
        public int Extras { get; set; }
        public BattingScorecard[] BattingScorecard { get; set; }
        public BowlingScorecard[] BowlingScorecard { get; set; }
        public int[] FallOfWicketScorecard { get; set; }
    }
}
