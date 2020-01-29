using System;
using System.Text.Json.Serialization;
using Cricinfo.Models.Enums;

namespace Cricinfo.Models
{
    public class Match
    {
        public string Venue { get; set; }
        [JsonConverter(typeof(MatchTypeConverter))]
        public MatchType MatchType { get; set; }
        public DateTime DateOfFirstDay { get; set; }
        public string HomeTeam { get; set; }
        public string AwayTeam { get; set; }
        [JsonConverter(typeof(ResultConverter))]
        public Result Result { get; set; }
        public string[] HomeSquad { get; set; }
        public string[] AwaySquad { get; set; }
        public Score[] Scores { get; set; }
    }
}
