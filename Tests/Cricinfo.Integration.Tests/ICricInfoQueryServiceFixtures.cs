using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Cricinfo.Models;
using Cricinfo.Services.Matchdata;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cricinfo.Integration.Tests
{
    [TestClass]
    public class ICricInfoQueryServiceFixtures
    {
        private static ICricInfoCommandService cricInfoCommandService;
        private static ICricInfoQueryService cricInfoQueryService;

        [ClassInitialize]
        public static void Initialize(TestContext _)
        {
            var dbConnectionString = ConfigurationManager.OpenExeConfiguration(Assembly.GetExecutingAssembly().Location).ConnectionStrings.ConnectionStrings["DbConnectionString"].ConnectionString;
            cricInfoCommandService = new CricInfoCommandService<Match>(dbConnectionString);
            cricInfoQueryService = new CricInfoQueryService<Match>(dbConnectionString);
        }

        [DataTestMethod]
        [DataRow("Cricinfo.Integration.Tests.resources.south_africa-england-26-12-19.json")]
        [DataRow("Cricinfo.Integration.Tests.resources.england-australia-6-9-20.json")]
        [DataRow("Cricinfo.Integration.Tests.resources.england-pakistan-1-9-20.json")]
        [DataRow("Cricinfo.Integration.Tests.resources.england-ireland-30-7-20.json")]
        public async Task GetMatchFixture(string filepath)
        {
            using Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(filepath);
            using StreamReader reader = new StreamReader(s);
            var expected = JsonSerializer.Deserialize<Match>(reader.ReadToEnd());

            if (await cricInfoQueryService.MatchExistsAsync(expected.HomeTeam, expected.AwayTeam, expected.DateOfFirstDay))
            {
                await cricInfoCommandService.DeleteMatchAsync(expected.HomeTeam, expected.AwayTeam, expected.DateOfFirstDay);
            }

            var creationResponse = await cricInfoCommandService.CreateMatchAsync(expected);

            Assert.AreEqual(DataCreationResponse.Success, creationResponse.Item1);
            Assert.IsNotNull(creationResponse.Item2);

            var actual = await cricInfoQueryService.GetMatchAsync((int)creationResponse.Item2);

            Assert.AreEqual(expected.Venue, actual.Venue);
            Assert.AreEqual(expected.MatchType, actual.MatchType);
            Assert.AreEqual(expected.DateOfFirstDay, actual.DateOfFirstDay);
            Assert.AreEqual(expected.HomeTeam, actual.HomeTeam);
            Assert.AreEqual(expected.AwayTeam, actual.AwayTeam);
            Assert.AreEqual(expected.Result, actual.Result);

            CollectionAssert.AreEqual(
                expected.HomeSquad.Select(p => p.Replace("(wk)", "").Replace("(c)", "")).OrderBy(p => p).ToArray(),
                actual.HomeSquad.OrderBy(p => p).ToArray());
            CollectionAssert.AreEqual(
                expected.AwaySquad.Select(p => p.Replace("(wk)", "").Replace("(c)", "")).OrderBy(p => p).ToArray(),
                actual.AwaySquad.OrderBy(p => p).ToArray());

            Assert.AreEqual(expected.Scores.Length, actual.Scores.Length);
            foreach (var (e, a) in expected.Scores.Zip(actual.Scores))
            {
                Assert.AreEqual(e.Team, a.Team);
                Assert.AreEqual(e.Declared, a.Declared);
                Assert.AreEqual(e.Declared, a.Declared);
                Assert.AreEqual(e.Declared, a.Declared);

                CollectionAssert.AreEqual(e.FallOfWicketScorecard, a.FallOfWicketScorecard);

                var mappedNames = Parser.Parse.parseNames(actual.HomeSquad.Union(actual.AwaySquad))
                    .ToDictionary(p => p.Item3, p => string.Concat(p.Item1, " ", p.Item2));
                mappedNames.Add("sub", null);

                Assert.AreEqual(e.BattingScorecard.Length, a.BattingScorecard.Length);
                foreach (var (ebs, abs) in e.BattingScorecard.Zip(a.BattingScorecard))
                {
                    Assert.AreEqual(ebs.Balls, abs.Balls);
                    Assert.AreEqual(ebs.Bowler != null ? mappedNames[ebs.Bowler] : null, abs.Bowler);
                    Assert.AreEqual(ebs.Catcher != null ? mappedNames[ebs.Catcher] : null, abs.Catcher);
                    Assert.AreEqual(ebs.Dismissal, abs.Dismissal);
                    Assert.AreEqual(ebs.Fours, abs.Fours);
                    Assert.AreEqual(ebs.Mins, abs.Mins);
                    Assert.AreEqual(mappedNames[ebs.Name], abs.Name);
                    Assert.AreEqual(ebs.Runs, abs.Runs);
                    Assert.AreEqual(ebs.Sixes, abs.Sixes);
                }

                Assert.AreEqual(e.BowlingScorecard.Length, a.BowlingScorecard.Length);
                foreach (var (ebs, abs) in e.BowlingScorecard.Zip(a.BowlingScorecard))
                {
                    Assert.AreEqual(mappedNames[ebs.Name], abs.Name);
                    Assert.AreEqual(ebs.Maidens, abs.Maidens);
                    Assert.AreEqual(ebs.Overs, abs.Overs);
                    Assert.AreEqual(ebs.Runs, abs.Runs);
                    Assert.AreEqual(ebs.Wickets, abs.Wickets);
                }
            };
        }
    }
}
