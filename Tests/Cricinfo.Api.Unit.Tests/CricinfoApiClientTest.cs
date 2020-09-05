using Cricinfo.Api.Client;
using Cricinfo.Models.Enums;
using Cricinfo.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Cricinfo.Api.Unit.Tests
{
    public class CustomWebApplicationFactory<TStartup>
        : WebApplicationFactory<TStartup> where TStartup : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove the app's ICricInfoCommandService registration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICricInfoCommandService));
                if (descriptor != null) { services.Remove(descriptor); }

                // Add ICricInfoCommandService using a mock repository for testing
                services.AddScoped((_) => Utilities.MoqCricInfoCommandService());

                // Remove the app's ICricInfoQueryService registration
                descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICricInfoQueryService));
                if (descriptor != null) { services.Remove(descriptor); }

                // Add ICricInfoQueryService using a mock repository for testing
                services.AddScoped((_) => Utilities.MoqCricInfoQueryService());
            });
        }
    }

    [TestClass]
    public class CricinfoApiClientTest
    {
        private static CricinfoApiClient cricinfoApiClient;

        [ClassInitialize]
        public static void Initialize(TestContext tc)
        {
            var webApplicationFactory = new CustomWebApplicationFactory<Startup>();
            HttpClient httpclient = webApplicationFactory.CreateClient();
            cricinfoApiClient = new CricinfoApiClient(httpclient);
        }

        [TestMethod]
        public async Task GetMatchAsyncReturnsInstantiatedObject()
        {
            var responseObject = await cricinfoApiClient.GetMatchAsync(42);

            // assert on top-level response object properties returned
            Assert.AreEqual("Supersport Park, Centurion", responseObject.Venue);
            Assert.AreEqual(MatchType.TestMatch, responseObject.MatchType);
            Assert.AreEqual(new DateTime(2019, 12, 26), responseObject.DateOfFirstDay);
            Assert.AreEqual("South Africa", responseObject.HomeTeam);
            Assert.AreEqual("England", responseObject.AwayTeam);
            Assert.AreEqual(Result.HomeTeamWin, responseObject.Result);

            // assert on squads returned
            CollectionAssert.AreEqual(
                new string[] { "Aiden Markram", "Dean Elgar", "Zubayr Hamza",
                    "Rassie van der Dussen", "Faf du Plessis", "Quinton de Kock",
                    "Dwaine Pretorius", "Vernon Philander", "Keshav Maharaj",
                    "Kagiso Rabada", "Anrich Nortje" },
                responseObject.HomeSquad);
            CollectionAssert.AreEqual(
                new string[] { "Rory Burns", "Dominic Sibley", "Joe Denly",
                    "Joe Root", "Ben Stokes", "Jonny Bairstow",
                    "Jos Buttler", "Sam Curran", "Jofra Archer",
                    "James Anderson", "Stuart Broad" },
                responseObject.AwaySquad);

            // assert on high-level features of 'Score' properties of response object
            Assert.AreEqual(4, responseObject.Scores.Count());
            CollectionAssert.AreEqual(
                new string[] { "South Africa", "England", "South Africa", "England" },
                responseObject.Scores.Select(i => i.Team).ToArray());
            CollectionAssert.AreEqual(
                new int[] { 1, 1, 2, 2 },
                responseObject.Scores.Select(i => i.Innings).ToArray());
            CollectionAssert.AreEqual(
                new int[] { 9, 6, 19, 12 },
                responseObject.Scores.Select(i => i.Extras).ToArray());

            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(11, responseObject.Scores[i].BattingScorecard.Count());
                Assert.AreEqual(10, responseObject.Scores[i].FallOfWicketScorecard.Count());
            }

            CollectionAssert.AreEqual(
                new int[] { 6, 5, 5, 5 },
                responseObject.Scores.Select(s => s.BowlingScorecard.Count()).ToArray());

            // assert on detailed properties of a single 'BattingScorecard' object
            Assert.AreEqual("Dwaine Pretorius", responseObject.Scores[0].BattingScorecard[6].Name);
            Assert.AreEqual(Dismissal.Caught, responseObject.Scores[0].BattingScorecard[6].Dismissal);
            Assert.AreEqual("Joe Root", responseObject.Scores[0].BattingScorecard[6].Catcher);
            Assert.AreEqual("Sam Curran", responseObject.Scores[0].BattingScorecard[6].Bowler);
            Assert.AreEqual(33, responseObject.Scores[0].BattingScorecard[6].Runs);
            Assert.AreEqual(86, responseObject.Scores[0].BattingScorecard[6].Mins);
            Assert.AreEqual(45, responseObject.Scores[0].BattingScorecard[6].Balls);
            Assert.AreEqual(4, responseObject.Scores[0].BattingScorecard[6].Fours);
            Assert.AreEqual(1, responseObject.Scores[0].BattingScorecard[6].Sixes);

            // assert on detailed properties of a single 'BattingScorecard' object - checking 'Catcher' and 'Bowler' come through as null if not provided
            Assert.AreEqual("Anrich Nortje", responseObject.Scores[0].BattingScorecard[10].Name);
            Assert.AreEqual(Dismissal.NotOut, responseObject.Scores[0].BattingScorecard[10].Dismissal);
            Assert.IsNull(responseObject.Scores[0].BattingScorecard[10].Catcher);
            Assert.IsNull(responseObject.Scores[0].BattingScorecard[10].Bowler);
            Assert.AreEqual(0, responseObject.Scores[0].BattingScorecard[10].Runs);
            Assert.AreEqual(11, responseObject.Scores[0].BattingScorecard[10].Mins);
            Assert.AreEqual(6, responseObject.Scores[0].BattingScorecard[10].Balls);
            Assert.AreEqual(0, responseObject.Scores[0].BattingScorecard[10].Fours);
            Assert.AreEqual(0, responseObject.Scores[0].BattingScorecard[10].Sixes);

            // assert on detailed properties of a single 'BowlingScorecard' object
            Assert.AreEqual("Stuart Broad", responseObject.Scores[0].BowlingScorecard[1].Name);
            Assert.AreEqual(18.3f, responseObject.Scores[0].BowlingScorecard[1].Overs);
            Assert.AreEqual(4, responseObject.Scores[0].BowlingScorecard[1].Maidens);
            Assert.AreEqual(58, responseObject.Scores[0].BowlingScorecard[1].Runs);
            Assert.AreEqual(4, responseObject.Scores[0].BowlingScorecard[1].Wickets);

            // assert on detailed properties of a single 'FallOfWicketScorecard' object
            CollectionAssert.AreEqual(
                new int[] { 0, 32, 71, 97, 111, 198, 245, 252, 277, 284 },
                responseObject.Scores[0].FallOfWicketScorecard);
        }

        [DataTestMethod]
        [DataRow("duplicate home team", "duplicate away team", true)]
        [DataRow("home team", "away team", false)]
        public async Task CheckMatchReturnsCorrectStatus(
            string homeTeam, string awayTeam, bool expectedValue)
        {
            var actualValue = await cricinfoApiClient.MatchExistsAsync(
                homeTeam, awayTeam, DateTime.Now);
            Assert.AreEqual(expectedValue, actualValue);
        }

        [TestMethod]
        public async Task CheckGetTeamsAsyncReturnsCorrectly()
        {
            var actualValue = (await cricinfoApiClient.GetTeamsAsync()).ToArray();
            CollectionAssert.AreEqual(new[] { "England", "South Africa" }, actualValue);
        }
    }
}
