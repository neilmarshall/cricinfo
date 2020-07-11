using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cricinfo.Models.Enums
{
    public enum Result
    {
        HomeTeamWin = 0,
        AwayTeamWin = 1,
        Draw = 2,
        Tie = 3,
        NoResult = 4
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
                "No Result" => Result.NoResult,
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
                Result.NoResult => "No Result",
                _ => throw new ArgumentException("Invalid ENUM value for 'Result'")
            });
        }
    }
}
