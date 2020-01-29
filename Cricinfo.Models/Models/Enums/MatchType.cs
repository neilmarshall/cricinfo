using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cricinfo.Models.Enums
{
    public enum MatchType
    {
        TestMatch = 0,
        ODI = 1,
        T20 = 2
    }

    public class MatchTypeConverter : JsonConverter<MatchType>
    {
        public override MatchType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetString() switch
            {
                "Test Match" => MatchType.TestMatch,
                "One Day International" => MatchType.ODI,
                "Twenty-Twenty" => MatchType.T20,
                _ => throw new ArgumentException("Invalid JSON value for 'MatchType'")
            };
        }

        public override void Write(Utf8JsonWriter writer, MatchType value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value switch
            {
                MatchType.TestMatch => "Test Match",
                MatchType.ODI => "One Day International",
                MatchType.T20 => "Twenty-Twenty",
                _ => throw new ArgumentException("Invalid ENUM value for 'MatchType'")
            });
        }
    }
}
