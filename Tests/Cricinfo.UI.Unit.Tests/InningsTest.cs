using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Cricinfo.Parser;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Cricinfo.Models.Constants;

namespace Cricinfo.UI.Unit.Tests
{
    [TestClass]
    public class InningsTest
    {
        private static HttpClient client;

        private string battingScorecard =
            @"Malan	c Stokes	b Curran	84	367	288	3	0	29.17
              Elgar c Buttler b Denly	34	115	78	2	0	43.59
              Hamza c Buttler b Anderson	18	102	59	1	0	30.51
              Maharaj lbw b Anderson	2	24	17	0	0	11.76
              du Plessis  c Denly b Bess	19	78	57	3	0	33.33\n";

        private string bowlingScorecard =
            @"Anderson	18.0	9	23	2	1.28
              Broad	23.0	8	37	1	1.61
              Bess	33.0	14	57	1	1.73\n";

        private string fallOfWicketScorecard =
            @"71-1 (28.6 ovs)	Elgar
              123-2 (54.2 ovs)	Hamza
              129-3 (58.5 ovs)	Maharaj
              129-4 (58.5 ovs)	Malan
              164-5 (76.2 ovs)	du Plessis\n";

        private IEnumerable<KeyValuePair<string, string>> FormContent()
        {
            yield return new KeyValuePair<string, string>("Team", "A team");
            yield return new KeyValuePair<string, string>("Innings", "1");
            yield return new KeyValuePair<string, string>("Extras", "7");
            yield return new KeyValuePair<string, string>("BattingScorecard", battingScorecard);
            yield return new KeyValuePair<string, string>("BowlingScorecard", bowlingScorecard);
            yield return new KeyValuePair<string, string>("FallOfWicketScorecard", fallOfWicketScorecard);
            yield return new KeyValuePair<string, string>("Declared", "false");
        }

        [ClassInitialize]
        public static void Initialize(TestContext tc)
        {
            var factory = new Utilities.AuthenticatedCustomWebApplicationFactory<Startup>("TestAuthHandlerWithClaims");
            client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        [TestMethod]
        public async Task GET_EndpointReturnsCorrectStatusCode()
        {
            // Act
            var response = await client.GetAsync("Scorecard/Innings");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task POST_EndpointWithValidFormReturnsCorrectRedirectResult()
        {
            // Arrange
            var getScorecardHTML = await client.GetAsync("Scorecard");
            var token = await Utilities.GetCSRFTokenAsync(getScorecardHTML.Content);
            var scorecardContent = ScorecardTests.FormContent().Append(new KeyValuePair<string, string>("__RequestVerificationToken", token)).ToDictionary(kv => kv.Key, kv => kv.Value);
            scorecardContent["HomeSquad"] = string.Join(Environment.NewLine, new[] { "_ du Plessis", "_ Elgar", "_ Hamza", "_ Maharaj", "_ Malan", "_ a", "_ b", "_ c", "_ d", "_ e", "_ f" });
            scorecardContent["AwaySquad"] = string.Join(Environment.NewLine, new[] { "_ Anderson", "_ Bess", "_ Broad", "_ Buttler", "_ Curran", "_ Denly", "_ Stokes", "_ a", "_ b", "_ c", "_ d" });
            await client.PostAsync("Scorecard", new FormUrlEncodedContent(scorecardContent));

            var getInningsHTML = await client.GetAsync("Scorecard/Innings");
            token = await Utilities.GetCSRFTokenAsync(getInningsHTML.Content);
            var formContent = new FormUrlEncodedContent(
                    this.FormContent()
                        .Append(new KeyValuePair<string, string>("__RequestVerificationToken", token)));

            // Act + Assert

            // first call to 'Scorecard/Innings' should redirect back to 'Innings'
            var response = await client.PostAsync("Scorecard/Innings?handler=AddAnotherInnings", formContent);
            Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
            Assert.AreEqual("/Scorecard/Innings", response.Headers.Location.OriginalString.Split('?')[0]);

            // second call to 'Scorecard/Innings' should redirect back to 'Innings'
            response = await client.PostAsync("Scorecard/Innings?handler=AddAnotherInnings", formContent);
            Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
            Assert.AreEqual("/Scorecard/Innings", response.Headers.Location.OriginalString.Split('?')[0]);

            // third call to 'Scorecard/Innings' should redirect back to 'Innings'
            response = await client.PostAsync("Scorecard/Innings?handler=AddAnotherInnings", formContent);
            Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
            Assert.AreEqual("/Scorecard/Innings", response.Headers.Location.OriginalString.Split('?')[0]);

            // fourth call to 'Scorecard/Innings' should redirect to 'Verification'
            response = await client.PostAsync("Scorecard/Innings?handler=SubmitAllInnings", formContent);
            Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
            Assert.AreEqual("/Scorecard/Verification", response.Headers.Location.OriginalString.Split('?')[0]);
        }

        [DataTestMethod]
        [DataRow("BattingScorecard", "Batting Scorecard")]
        [DataRow("BowlingScorecard", "Bowling Scorecard")]
        public async Task POST_EndpointWithMissingFieldsReturnsCorrectValidationMessage(string field, string label)
        {
            // Arrange
            var getScorecardHTML = await client.GetAsync("Scorecard");
            var token = await Utilities.GetCSRFTokenAsync(getScorecardHTML.Content);
            await client.PostAsync("Scorecard",
                new FormUrlEncodedContent(
                    ScorecardTests.FormContent()
                                  .Append(new KeyValuePair<string, string>("__RequestVerificationToken", token))));

            // Act
            var formElements = FormContent().ToDictionary(kv => kv.Key, kv => kv.Value);
            formElements[field] = null;
            formElements.Add("__RequestVerificationToken", token);
            var formContent = new FormUrlEncodedContent(formElements);
            var response = await client.PostAsync("Scorecard/Innings?handler=AddAnotherInnings", formContent);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.IsTrue(new Regex($"<span class=\"text-danger field-validation-error\" data-valmsg-for=\"{field}\" data-valmsg-replace=\"true\">The {label} field is required.</span>").Match(content).Success);
        }

        [TestMethod]
        public async Task POST_EndpointWithInvalidBattingScorecardReturnsCorrectValidationMessage()
        {
            // Arrange
            var getScorecardHTML = await client.GetAsync("Scorecard");
            var token = await Utilities.GetCSRFTokenAsync(getScorecardHTML.Content);
            await client.PostAsync("Scorecard",
                new FormUrlEncodedContent(
                    ScorecardTests.FormContent()
                                  .Append(new KeyValuePair<string, string>("__RequestVerificationToken", token))));

            string invalidBattingScorecard =
                @"Malan	c Stokes	b S Curran	84	367	288	3	0	29.17
                  Elgar c Buttler b Denly	34	115	78	2	0	43.59
                  Hamza c Buttler 18	102	59	1	0	30.51
                  Maharaj lbw b Anderson	2	24	17	0	0	11.76
                  du Plessis  c Denly b Bess	19	78	57	3	0	33.33";

            // Act
            var formElements = FormContent().ToDictionary(kv => kv.Key, kv => kv.Value);
            formElements["BattingScorecard"] = invalidBattingScorecard;
            formElements.Add("__RequestVerificationToken", token);
            var formContent = new FormUrlEncodedContent(formElements);
            var response = await client.PostAsync("Scorecard/Innings?handler=AddAnotherInnings", formContent);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.IsTrue(new Regex("<span class=\"text-danger field-validation-error\" data-valmsg-for=\"BattingScorecard\" data-valmsg-replace=\"true\">The field Batting Scorecard is invalid.</span>").Match(content).Success);
        }

        [TestMethod]
        public async Task POST_EndpointWithInvalidBowlingScorecardReturnsCorrectValidationMessage()
        {
            // Arrange
            var getScorecardHTML = await client.GetAsync("Scorecard");
            var token = await Utilities.GetCSRFTokenAsync(getScorecardHTML.Content);
            await client.PostAsync("Scorecard",
                new FormUrlEncodedContent(
                    ScorecardTests.FormContent()
                                  .Append(new KeyValuePair<string, string>("__RequestVerificationToken", token))));

            string invalidBowlingScorecard =
                @"Anderson	18.0	9	23	2	1.28
                  Broad	23.0	8	37
                  Bess	33.0	14	57	1	1.73";

            // Act
            var formElements = FormContent().ToDictionary(kv => kv.Key, kv => kv.Value);
            formElements["BowlingScorecard"] = invalidBowlingScorecard;
            formElements.Add("__RequestVerificationToken", token);
            var formContent = new FormUrlEncodedContent(formElements);
            var response = await client.PostAsync("Scorecard/Innings?handler=AddAnotherInnings", formContent);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.IsTrue(new Regex("<span class=\"text-danger field-validation-error\" data-valmsg-for=\"BowlingScorecard\" data-valmsg-replace=\"true\">The field Bowling Scorecard is invalid.</span>").Match(content).Success);
        }

        [TestMethod]
        public async Task POST_EndpointWithInvalidFallOfWicketScorecardReturnsCorrectValidationMessage()
        {
            // Arrange
            var getScorecardHTML = await client.GetAsync("Scorecard");
            var token = await Utilities.GetCSRFTokenAsync(getScorecardHTML.Content);
            await client.PostAsync("Scorecard",
                new FormUrlEncodedContent(
                    ScorecardTests.FormContent()
                                  .Append(new KeyValuePair<string, string>("__RequestVerificationToken", token))));

            string invalidFallOfWicketScorecard =
                @"71-1 (28.6 ovs)	Elgar
                  123-2 (54.2 ovs)	Hamza
                  1abc29-3 (58.5 ovs)	Maharaj
                  164-4 (76.2 ovs)	du Plessis";

            // Act
            var formElements = FormContent().ToDictionary(kv => kv.Key, kv => kv.Value);
            formElements["FallOfWicketScorecard"] = invalidFallOfWicketScorecard;
            formElements.Add("__RequestVerificationToken", token);
            var formContent = new FormUrlEncodedContent(formElements);
            var response = await client.PostAsync("Scorecard/Innings?handler=AddAnotherInnings", formContent);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.IsTrue(new Regex("<span class=\"text-danger field-validation-error\" data-valmsg-for=\"FallOfWicketScorecard\" data-valmsg-replace=\"true\">The field Fall of Wicket Scorecard is invalid.</span>").Match(content).Success);
        }

        [TestMethod]
        public async Task POST_EndpointWithValidScorecardReturnsCorrectly()
        {
            // Arrange
            var getScorecardHTML = await client.GetAsync("Scorecard");
            var token = await Utilities.GetCSRFTokenAsync(getScorecardHTML.Content);
            var scorecardContent = ScorecardTests.FormContent().Append(new KeyValuePair<string, string>("__RequestVerificationToken", token)).ToDictionary(kv => kv.Key, kv => kv.Value);
            scorecardContent["HomeSquad"] = string.Join(Environment.NewLine, new[] { "Rohit Sharma", "Shubman Gill", "Cheteshwar Pujara", "Virat Kohli(c)", "Ajinkya Rahane", "Rishabh Pant(wk)", "Ravichandran Ashwin", "Axar Patel", "Washington Sundar", "Mohammed Siraj", "Ishant Sharma" });
            scorecardContent["AwaySquad"] = string.Join(Environment.NewLine, new[] { "Dominic Sibley", "Zak Crawley", "Jonny Bairstow", "Joe Root(c)", "Ben Stokes", "Ollie Pope", "Dan Lawrence", "Ben Foakes(wk)", "Dom Bess", "Jack Leach", "James Anderson" });
            await client.PostAsync("Scorecard", new FormUrlEncodedContent(scorecardContent));

            string battingScorecard =
                @"Gill	lbw	b Anderson	0	4	3	0	0	0.00
                  Rohit Sharma	lbw	b Stokes	49	238	144	7	0	34.03
                  Pujara	lbw	b Leach	17	107	66	1	0	25.76
                  Kohli	c Foakes	b Stokes	0	11	8	0	0	0.00
                  Rahane	c Stokes	b Anderson	27	54	45	4	0	60.00
                  Pant	c Root	b Anderson	101	226	118	13	2	85.59
                  R Ashwin	c Pope	b Leach	13	45	32	2	0	40.62
                  Sundar	not out		96	257	174	10	1	55.17
                  A Patel	run out (Bairstow)		43	134	97	5	1	44.33
                  I Sharma	lbw	b Stokes	0	3	1	0	0	0.00
                  Siraj		b Stokes	0	4	3	0	0	0.00";

            string bowlingScorecard =
                @"Anderson	25.0	14	44	3	1.76
                  Stokes	27.4	6	89	4	3.22
                  Leach	27.0	5	89	2	3.30
                  Bess	17.0	1	71	0	4.18
                  Root	18.0	1	56	0	3.11";

            string fallOfWicketScorecard =
                @"0-1 (0.3 ovs)	Gill
                  40-2 (23.6 ovs)	Pujara
                  41-3 (26.4 ovs)	Kohli
                  80-4 (37.5 ovs)	Rahane
                  121-5 (49.6 ovs)	Rohit Sharma
                  146-6 (58.1 ovs)	R Ashwin
                  259-7 (84.1 ovs)	Pant
                  365-8 (113.6 ovs)	A Patel
                  365-9 (114.1 ovs)	I Sharma
                  365-10 (114.4 ovs)	Siraj";

            // Act
            var formElements = FormContent().ToDictionary(kv => kv.Key, kv => kv.Value);
            formElements["BattingScorecard"] = battingScorecard;
            formElements["BowlingScorecard"] = bowlingScorecard;
            formElements["FallOfWicketScorecard"] = fallOfWicketScorecard;
            formElements.Add("__RequestVerificationToken", token);
            var formContent = new FormUrlEncodedContent(formElements);

            // Assert -

            // call to 'Scorecard/Innings' should redirect back to 'Innings'
            var response = await client.PostAsync("Scorecard/Innings?handler=AddAnotherInnings", formContent);
            var content = await response.Content.ReadAsStringAsync();
            Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
            Assert.AreEqual("/Scorecard/Innings", response.Headers.Location.OriginalString.Split('?')[0]);

            // call to 'Scorecard/Innings' should redirect to 'Verification'
            response = await client.PostAsync("Scorecard/Innings?handler=SubmitAllInnings", formContent);
            Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
            Assert.AreEqual("/Scorecard/Verification", response.Headers.Location.OriginalString.Split('?')[0]);
        }

        [TestMethod]
        public async Task POST_EndpointWithMissingFallOfWicketScorecardDataReturnsCorrectValidationMessage()
        {
            // Arrange
            var getScorecardHTML = await client.GetAsync("Scorecard");
            var token = await Utilities.GetCSRFTokenAsync(getScorecardHTML.Content);
            await client.PostAsync("Scorecard",
                new FormUrlEncodedContent(
                    ScorecardTests.FormContent()
                                  .Append(new KeyValuePair<string, string>("__RequestVerificationToken", token))));

            string invalidFallOfWicketScorecard =
                @"71-1 (28.6 ovs)	Elgar
                  123-2 (54.2 ovs)	Hamza
                  129-3 (58.5 ovs)	Maharaj\n";

            // Act
            var formElements = FormContent().ToDictionary(kv => kv.Key, kv => kv.Value);
            formElements["FallOfWicketScorecard"] = invalidFallOfWicketScorecard;
            formElements.Add("__RequestVerificationToken", token);
            var formContent = new FormUrlEncodedContent(formElements);
            var response = await client.PostAsync("Scorecard/Innings?handler=AddAnotherInnings", formContent);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var errMsg = "Missing fall of wicket data - expected 4 entries, found 3";
            Assert.IsTrue(new Regex($"<span class=\"text-danger field-validation-error\" data-valmsg-for=\"FallOfWicketScorecard\" data-valmsg-replace=\"true\">{errMsg}</span>").Match(content).Success);
        }

        [TestMethod]
        public async Task POST_EndpointWithNegativeExtrasReturnsCorrectValidationMessage()
        {
            // Arrange
            var getScorecardHTML = await client.GetAsync("Scorecard");
            var token = await Utilities.GetCSRFTokenAsync(getScorecardHTML.Content);
            await client.PostAsync("Scorecard",
                new FormUrlEncodedContent(
                    ScorecardTests.FormContent()
                                  .Append(new KeyValuePair<string, string>("__RequestVerificationToken", token))));

            // Act
            var formElements = FormContent().ToDictionary(kv => kv.Key, kv => kv.Value);
            formElements["Extras"] = "-1";
            formElements.Add("__RequestVerificationToken", token);
            var formContent = new FormUrlEncodedContent(formElements);
            var response = await client.PostAsync("Scorecard/Innings?handler=AddAnotherInnings", formContent);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.IsTrue(new Regex($"<span class=\"text-danger field-validation-error\" data-valmsg-for=\"Extras\" data-valmsg-replace=\"true\">The field Extras must be between 0 and {int.MaxValue}.</span>").Match(content).Success);
        }

        [TestMethod]
        public void InningsSummaryTextRendersCorrectly()
        {
            var data = this.FormContent().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            var innings = new Models.Score
            {
                BattingScorecard = Parse.parseBattingScorecard(data["BattingScorecard"]).ToArray(),
                BowlingScorecard = Parse.parseBowlingScorecard(data["BowlingScorecard"]).ToArray(),
                Declared = false,
                Extras = int.Parse(data["Extras"]),
                FallOfWicketScorecard = Parse.parseFallOfWicketScorecard(data["FallOfWicketScorecard"]).ToArray(),
                Innings = int.Parse(data["Innings"]),
                Team = data["Team"]
            };

            // check basic case
            Assert.AreEqual("164-5", innings.RenderBattingScore());

            // check parses declarations
            innings.Declared = true;
            Assert.AreEqual("164-5d", innings.RenderBattingScore());

            // check doesn't count 'not outs' in dismissals
            innings.BattingScorecard.First().Dismissal = Models.Enums.Dismissal.NotOut;
            Assert.AreEqual("164-4d", innings.RenderBattingScore());

            // check 'all out' renders correctly
            innings.Declared = false;
            innings.BattingScorecard = innings.BattingScorecard
                .Concat(Enumerable.Repeat(innings.BattingScorecard.Last(), NumberOfPlayers - innings.BattingScorecard.Length))
                .ToArray();
            Assert.AreEqual("278 all out", innings.RenderBattingScore());
        }
    }
}
