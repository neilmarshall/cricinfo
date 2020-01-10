using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace cricinfo.api.Models
{
    public enum Result
    {
        HomeTeamWin = 0,
        AwayTeamWin = 1,
        Draw = 2,
        Tie = 3
    }

    public class ResultConverter : JsonConverter<Result>
    {
        public override Result Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetString() switch
            {
                "Home Team Win" => Result.HomeTeamWin,
                "Away Team Win" => Result.AwayTeamWin,
                "Draw" => Result.Draw,
                "Tie" => Result.Tie,
                _ => throw new ArgumentException("Invalid JSON value for 'Result'")
            };
        }

        public override void Write(Utf8JsonWriter writer, Result value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value switch
            {
                Result.HomeTeamWin => "Home Team Win",
                Result.AwayTeamWin => "Away Team Win",
                Result.Draw => "Draw",
                Result.Tie => "Tie",
                _ => throw new ArgumentException("Invalid ENUM value for 'Result'")
            });
        }
    }

    public class Match
    {
        public string Venue { get; set; }
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
