using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Cricinfo.Api.Controllers;
using Cricinfo.Api.Models;
using Cricinfo.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cricinfo.Api.unit.tests
{
    [TestClass]
    public class MatchControllerTest
    {
        private class MockCricInfoRepository : ICricInfoRepository
        {
            private Dictionary<int, Match> matches;

            public MockCricInfoRepository()
            {
                Assembly a = Assembly.GetExecutingAssembly();
                using Stream s = a.GetManifestResourceStream("Cricinfo.Api.Unit.Tests.resources.south_africa-england-26-12-18.json");
                using StreamReader reader = new StreamReader(s);
                var matchData = reader.ReadToEnd();
                var match = JsonSerializer.Deserialize<Match>(matchData);

                matches = new Dictionary<int, Match>
                {
                    { 42, match }
                };
            }

            public Task<Tuple<DataCreationResponse, int?>> CreateMatchAsync(Match match)
            {
                return Task.Run(() =>
                {
                    if (matches.Values.Any(m => m.DateOfFirstDay == match.DateOfFirstDay && m.HomeTeam == match.HomeTeam && m.AwayTeam == match.AwayTeam))
                    {
                        return Tuple.Create<DataCreationResponse, int?>(DataCreationResponse.DuplicateContent, null);
                    }

                    try
                    {
                        var key = matches.Keys.Max() + 1;
                        matches.Add(key, match);
                        return Tuple.Create<DataCreationResponse, int?>(DataCreationResponse.Success, key);
                    }
                    catch (Exception)
                    {
                        return Tuple.Create<DataCreationResponse, int?>(DataCreationResponse.Failure, null);
                    }
                });
            }

            public Task<Match> GetMatchAsync(int id)
            {
                return Task.Run(() => matches.ContainsKey(id) ? matches[id] : null);
            }
        }

        private MatchController matchController;
        private Match match;

        [TestInitialize]
        public void Initialize()
        {
            this.matchController = new MatchController(new MockCricInfoRepository());
            this.match = new Match
            {
                Venue = "",
                DateOfFirstDay = new DateTime(),
                HomeTeam = "",
                AwayTeam = ""
            };
        }

        [TestMethod]
        public async Task GetMatchAsyncReturns200ForValidId()
        {
            var result = await matchController.GetMatchAsync(42) as OkObjectResult;
            var responseObject = result.Value as Match;

            // assert on status code returned
            Assert.AreEqual(200, result.StatusCode);

            // assert on top-level response object properties returned
            Assert.AreEqual("Supersport Park, Centurion", responseObject.Venue);
            Assert.AreEqual(new DateTime(2018, 12, 26), responseObject.DateOfFirstDay);
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
            var result = await matchController.GetMatchAsync(99) as NotFoundResult;
            Assert.AreEqual(404, result.StatusCode);
        }

        [TestMethod]
        public async Task CreateMatchAsyncReturns201ForValidMatch()
        {
            var result = await matchController.CreateMatchAsync(this.match) as CreatedAtActionResult;
            Assert.AreEqual(201, result.StatusCode);
            Assert.AreEqual(43, result.RouteValues["id"]);
        }

        [TestMethod]
        public async Task CreateMatchAsyncReturns400ForBadInput()
        {
            var result = await matchController.CreateMatchAsync(null) as BadRequestResult;
            Assert.AreEqual(400, result.StatusCode);
        } 

        [TestMethod]
        public async Task CreateMatchAsyncReturns409ForDuplicateMatch()
        {
            this.match.DateOfFirstDay = DateTime.Parse("2018-12-26");
            this.match.HomeTeam = "South Africa";
            this.match.AwayTeam = "England";
            var result = await matchController.CreateMatchAsync(this.match) as StatusCodeResult;
            Assert.AreEqual(409, result.StatusCode);
        }
    }
}
