using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cricinfo.Models
{
    public enum Dismissal
    {
        Caught = 0,
        Bowled = 1,
        CaughtAndBowled = 2,
        LBW = 3,
        NotOut = 4
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
                _ => throw new ArgumentException("Invalid ENUM value for 'Dismissal'")
            });
        }
    }

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
