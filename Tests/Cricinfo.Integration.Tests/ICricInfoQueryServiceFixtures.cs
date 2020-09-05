using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Cricinfo.Models;
using Cricinfo.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cricinfo.Integration.Tests
{
    [TestClass]
    public class ICricInfoQueryServiceFixtures
    {
        private static ICricInfoQueryService cricInfoQueryService;
        private static Match expected;

        [ClassInitialize]
        public static void Initialize(TestContext _)
        {
            var dbConnectionString = ConfigurationManager.OpenExeConfiguration(Assembly.GetExecutingAssembly().Location).ConnectionStrings.ConnectionStrings["DbConnectionString"].ConnectionString;
            cricInfoQueryService = new CricInfoQueryService<Match>(dbConnectionString);

            var a = Assembly.GetExecutingAssembly();
            using Stream s = a.GetManifestResourceStream("Cricinfo.Integration.Tests.resources.south_africa-england-26-12-19.json");
            using StreamReader reader = new StreamReader(s);
            expected = JsonSerializer.Deserialize<Match>(reader.ReadToEnd());
        }

        [TestMethod]
        public async Task GetMatchFixture()
        {
            var actual = await cricInfoQueryService.GetMatchAsync(1);

            Assert.AreEqual(expected.Venue, actual.Venue);
            Assert.AreEqual(expected.MatchType, actual.MatchType);
            Assert.AreEqual(expected.DateOfFirstDay, actual.DateOfFirstDay);
            Assert.AreEqual(expected.HomeTeam, actual.HomeTeam);
            Assert.AreEqual(expected.AwayTeam, actual.AwayTeam);
            Assert.AreEqual(expected.Result, actual.Result);

            CollectionAssert.AreEqual(expected.HomeSquad, actual.HomeSquad);
            CollectionAssert.AreEqual(expected.AwaySquad, actual.AwaySquad);

            Assert.AreEqual(expected.Scores.Length, actual.Scores.Length);
            foreach (var (e, a) in expected.Scores.Zip(actual.Scores))
            {
                Assert.AreEqual(e.Team, a.Team);
                Assert.AreEqual(e.Declared, a.Declared);
                Assert.AreEqual(e.Declared, a.Declared);
                Assert.AreEqual(e.Declared, a.Declared);

                CollectionAssert.AreEqual(e.FallOfWicketScorecard, a.FallOfWicketScorecard);

                Assert.AreEqual(e.BattingScorecard.Length, a.BattingScorecard.Length);
                foreach (var (ebs, abs) in e.BattingScorecard.Zip(a.BattingScorecard))
                {
                    Assert.AreEqual(ebs.Balls, abs.Balls);
                    Assert.AreEqual(ebs.Bowler, abs.Bowler);
                    Assert.AreEqual(ebs.Catcher, abs.Catcher);
                    Assert.AreEqual(ebs.Dismissal, abs.Dismissal);
                    Assert.AreEqual(ebs.Fours, abs.Fours);
                    Assert.AreEqual(ebs.Mins, abs.Mins);
                    Assert.AreEqual(ebs.Name, abs.Name);
                    Assert.AreEqual(ebs.Runs, abs.Runs);
                    Assert.AreEqual(ebs.Sixes, abs.Sixes);
                }

                Assert.AreEqual(e.BowlingScorecard.Length, a.BowlingScorecard.Length);
                foreach (var (ebs, abs) in e.BowlingScorecard.Zip(a.BowlingScorecard))
                {
                    Assert.AreEqual(ebs.Name, abs.Name);
                    Assert.AreEqual(ebs.Maidens, abs.Maidens);
                    Assert.AreEqual(ebs.Overs, abs.Overs);
                    Assert.AreEqual(ebs.Runs, abs.Runs);
                    Assert.AreEqual(ebs.Wickets, abs.Wickets);
                }
            };
        }
    }
}
