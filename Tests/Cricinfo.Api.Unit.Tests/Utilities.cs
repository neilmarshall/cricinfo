using Cricinfo.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Match = Cricinfo.Models.Match;

namespace Cricinfo.Api.Unit.Tests
{
    internal static class Utilities
    {
        internal static ICricInfoRepository MoqCricInfoRepository()
        {
            Assembly a = Assembly.GetExecutingAssembly();
            using Stream s = a.GetManifestResourceStream("Cricinfo.Api.Unit.Tests.resources.south_africa-england-26-12-18.json");
            using StreamReader reader = new StreamReader(s);
            var matches = new Dictionary<int, Match> { { 42, JsonSerializer.Deserialize<Match>(reader.ReadToEnd()) } };

            var mock = new Mock<ICricInfoRepository>();

            mock.Setup(ICricInfoRepository => ICricInfoRepository
                .GetMatchAsync(It.Is<int>(i => i == 42)))
                .Returns(() => Task.FromResult(matches[42]));

            mock.Setup(ICricInfoRepository => ICricInfoRepository
                .MatchExistsAsync(
                    It.Is<string>(homeTeam => homeTeam == "duplicate home team"),
                    It.Is<string>(awayTeam => awayTeam == "duplicate away team"),
                    It.IsAny<DateTime>()))
                .Returns(Task.FromResult(true));

            mock.Setup(ICricInfoRepository => ICricInfoRepository
                .CreateMatchAsync(
                    It.Is<Match>(match => !matches.Values.Any(m => m.DateOfFirstDay == match.DateOfFirstDay && m.HomeTeam == match.HomeTeam && m.AwayTeam == match.AwayTeam))))
                .Returns(Task.FromResult(Tuple.Create<DataCreationResponse, long?>(DataCreationResponse.Success, matches.Keys.Max() + 1)));

            mock.Setup(ICricInfoRepository => ICricInfoRepository
                .CreateMatchAsync(
                    It.Is<Match>(match => matches.Values.Any(m => m.DateOfFirstDay == match.DateOfFirstDay && m.HomeTeam == match.HomeTeam && m.AwayTeam == match.AwayTeam))))
                .Returns(Task.FromResult(Tuple.Create<DataCreationResponse, long?>(DataCreationResponse.DuplicateContent, null)));

            return mock.Object;
        }
    }
}
