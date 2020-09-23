using System;
using System.Linq;
using System.Threading.Tasks;
using Cricinfo.Api.Controllers;
using Cricinfo.Models;
using Cricinfo.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cricinfo.Api.Unit.Tests
{
    [TestClass]
    public class MatchControllerTest
    {
        private static MatchController matchController;
        private static TeamsController teamsController;

        [ClassInitialize]
        public static void Initialize(TestContext tc)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
                builder.AddConsole()
            );
            var logger = loggerFactory.CreateLogger<MatchController>();
            matchController = new MatchController(
                Utilities.MoqCricInfoCommandService(),
                Utilities.MoqCricInfoQueryService(),
                logger);
            teamsController = new TeamsController(
                Utilities.MoqCricInfoCommandService(),
                Utilities.MoqCricInfoQueryService(),
                logger);
        }

        [TestMethod]
        public async Task GetMatchAsyncReturns200ForValidId()
        {
            var result = await matchController.GetAsync(42) as OkObjectResult;
            var responseObject = result.Value as Match;

            // assert on status code returned
            Assert.AreEqual(200, result.StatusCode);

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

        [TestMethod]
        public async Task GetMatchAsyncReturns404ForMissingId()
        {
            var result = await matchController.GetAsync(99) as NotFoundResult;
            Assert.AreEqual(404, result.StatusCode);
        }

        [TestMethod]
        public async Task CreateMatchAsyncReturns201ForValidMatch()
        {
            var match = new Match
            {
                Venue = "",
                DateOfFirstDay = new DateTime(),
                HomeTeam = "",
                AwayTeam = ""
            };

            var result = await matchController.PostAsync(match) as CreatedAtActionResult;
            Assert.AreEqual(201, result.StatusCode);
            Assert.AreEqual(43L, result.RouteValues["id"]);
        }

        [TestMethod]
        public async Task CreateMatchAsyncReturns400ForBadInput()
        {
            var result = await matchController.PostAsync(null) as BadRequestResult;
            Assert.AreEqual(400, result.StatusCode);
        } 

        [TestMethod]
        public async Task CreateMatchAsyncReturns409ForDuplicateMatch()
        {
            var match = new Match
            {
                Venue = "",
                DateOfFirstDay = DateTime.Parse("2019-12-26"),
                HomeTeam = "South Africa",
                AwayTeam = "England"
            };

            var result = await matchController.PostAsync(match) as StatusCodeResult;
            Assert.AreEqual(409, result.StatusCode);
        }

        [TestMethod]
        public async Task GetTeamsAsyncReturns200()
        {
            var result = await teamsController.GetAsync() as OkObjectResult;
            var responseObject = result.Value as string[];

            // assert on status code returned
            Assert.AreEqual(200, result.StatusCode);

            // assert on response object returned
            CollectionAssert.AreEqual(new[] { "England", "South Africa" }, responseObject);
        }

        [DataTestMethod]
        [DataRow("New Team", 201)]
        [DataRow("", 400)]
        [DataRow("Duplicate Team", 409)]
        public async Task CreateTeamAsyncReturns201ForValidMatch(string team, int expectedStatusCode)
        {
            var result = await teamsController.PostAsync(team) as StatusCodeResult;
            Assert.AreEqual(expectedStatusCode, result.StatusCode);
        }
    }
}
