using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cricinfo.Models.Enums
{
    public enum Dismissal
    {
        Caught = 0,
        Bowled = 1,
        CaughtAndBowled = 2,
        LBW = 3,
        NotOut = 4,
        RunOut = 5
    }

    public class DismissalConverter : JsonConverter<Dismissal>
    {
        public override Dismissal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetString() switch
            {
                "c" => Dismissal.Caught,
                "b" => Dismissal.Bowled,
                "c&b" => Dismissal.CaughtAndBowled,
                "lbw" => Dismissal.LBW,
                "not out" => Dismissal.NotOut,
                "run out" => Dismissal.RunOut,
                _ => throw new ArgumentException("Invalid JSON value for 'Dismissal'")
            };
        }

        public override void Write(Utf8JsonWriter writer, Dismissal value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value switch
            {
                Dismissal.Caught => "c",
                Dismissal.Bowled => "b",
                Dismissal.CaughtAndBowled => "c&b",
                Dismissal.LBW => "lbw",
                Dismissal.NotOut => "not out",
                Dismissal.RunOut => "run out",
                _ => throw new ArgumentException("Invalid ENUM value for 'Dismissal'")
            });
        }
    }
}
