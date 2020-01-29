using System.Text.Json.Serialization;
using Cricinfo.Models.Enums;

namespace Cricinfo.Models
{
    public class BattingScorecard
    {
        public string Name { get; set; }
        [JsonConverter(typeof(DismissalConverter))]
        public Dismissal Dismissal { get; set; }
        public string Catcher { get; set; }
        public string Bowler { get; set; }
        public int Runs { get; set; }
        public int Mins { get; set; }
        public int Balls { get; set; }
        public int Fours { get; set; }
        public int Sixes { get; set; }
    }
}
