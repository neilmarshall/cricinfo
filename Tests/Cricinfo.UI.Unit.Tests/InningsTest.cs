using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
              164-4 (76.2 ovs)	du Plessis\n";

        private IEnumerable<KeyValuePair<string, string>> FormContent()
        {
            yield return new KeyValuePair<string, string>("Team", "A team");
            yield return new KeyValuePair<string, string>("Innings", "1");
            yield return new KeyValuePair<string, string>("Extras", "7");
            yield return new KeyValuePair<string, string>("BattingScorecard", battingScorecard);
            yield return new KeyValuePair<string, string>("BowlingScorecard", bowlingScorecard);
            yield return new KeyValuePair<string, string>("FallOfWicketScorecard", fallOfWicketScorecard);
        }

        [ClassInitialize]
        public static void Initialize(TestContext tc)
        {
            var factory = new Utilities.CustomWebApplicationFactory<Startup>();
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
            var getScorecardHTML = await client.GetAsync("Scorecard/Scorecard");
            var token = await Utilities.GetCSRFTokenAsync(getScorecardHTML.Content);
            var scorecardContent = ScorecardTests.FormContent().Append(new KeyValuePair<string, string>("__RequestVerificationToken", token)).ToDictionary(kv => kv.Key, kv => kv.Value);
            scorecardContent["HomeSquad"] = string.Join(Environment.NewLine, new[] { "_ du Plessis", "_ Elgar", "_ Hamza", "_ Maharaj", "_ Malan", "_ a", "_ b", "_ c", "_ d", "_ e", "_ f" });
            scorecardContent["AwaySquad"] = string.Join(Environment.NewLine, new[] { "_ Anderson", "_ Bess", "_ Broad", "_ Buttler", "_ Curran", "_ Denly", "_ Stokes", "_ a", "_ b", "_ c", "_ d" });
            await client.PostAsync("Scorecard/Scorecard", new FormUrlEncodedContent(scorecardContent));

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
        [DataRow("FallOfWicketScorecard", "Fall of Wicket Scorecard")]
        public async Task POST_EndpointWithMissingFieldsReturnsCorrectValidationMessage(string field, string label)
        {
            // Arrange
            var getScorecardHTML = await client.GetAsync("Scorecard/Scorecard");
            var token = await Utilities.GetCSRFTokenAsync(getScorecardHTML.Content);
            await client.PostAsync("Scorecard/Scorecard",
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
            var getScorecardHTML = await client.GetAsync("Scorecard/Scorecard");
            var token = await Utilities.GetCSRFTokenAsync(getScorecardHTML.Content);
            await client.PostAsync("Scorecard/Scorecard",
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
            var getScorecardHTML = await client.GetAsync("Scorecard/Scorecard");
            var token = await Utilities.GetCSRFTokenAsync(getScorecardHTML.Content);
            await client.PostAsync("Scorecard/Scorecard",
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
            var getScorecardHTML = await client.GetAsync("Scorecard/Scorecard");
            var token = await Utilities.GetCSRFTokenAsync(getScorecardHTML.Content);
            await client.PostAsync("Scorecard/Scorecard",
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
        public async Task POST_EndpointWithNegativeExtrasReturnsCorrectValidationMessage()
        {
            // Arrange
            var getScorecardHTML = await client.GetAsync("Scorecard/Scorecard");
            var token = await Utilities.GetCSRFTokenAsync(getScorecardHTML.Content);
            await client.PostAsync("Scorecard/Scorecard",
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
    }
}