using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cricinfo.UI.Unit.Tests
{
    [TestClass]
    public class InningsTest
    {
        private WebApplicationFactory<Startup> _factory;
        private HttpClient _client;

        private string battingScorecard =
            @"Malan	c Stokes	b S Curran	84	367	288	3	0	29.17
              Elgar c Buttler b Denly	34	115	78	2	0	43.59
              Hamza c Buttler b Anderson	18	102	59	1	0	30.51
              Maharaj lbw b Anderson	2	24	17	0	0	11.76
              du Plessis  c Denly b Bess	19	78	57	3	0	33.33";

        private string bowlingScorecard =
            @"Anderson	18.0	9	23	2	1.28
              Broad	23.0	8	37	1	1.61
              Bess	33.0	14	57	1	1.73";

        private string fallOfWicketScorecard =
            @"71-1 (28.6 ovs)	Elgar
              123-2 (54.2 ovs)	Hamza
              129-3 (58.5 ovs)	Maharaj
              164-4 (76.2 ovs)	du Plessis";

        private IEnumerable<KeyValuePair<string, string>> FormContent()
        {
            yield return new KeyValuePair<string, string>("Team", "A team");
            yield return new KeyValuePair<string, string>("Innings", "1");
            yield return new KeyValuePair<string, string>("Extras", "7");
            yield return new KeyValuePair<string, string>("BattingScorecard", battingScorecard);
            yield return new KeyValuePair<string, string>("BowlingScorecard", bowlingScorecard);
            yield return new KeyValuePair<string, string>("FallOfWicketScorecard", fallOfWicketScorecard);
        }

        [TestInitialize]
        public void Initialize()
        {
            this._factory = new WebApplicationFactory<Startup>();
            this._client = this._factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        [TestMethod]
        public async Task GET_EndpointReturnsCorrectStatusCode()
        {
            // Act
            var response = await this._client.GetAsync("Scorecard/Innings");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task POST_EndpointWithValidFormReturnsCorrectRedirectResult()
        {
            // Arrange
            var getScorecardHTML = await this._client.GetAsync("Scorecard/Scorecard");
            var token = await Utilities.GetCSRFTokenAsync(getScorecardHTML.Content);
            await this._client.PostAsync("Scorecard/Scorecard",
                new FormUrlEncodedContent(
                    ScorecardTests.FormContent()
                                  .Append(new KeyValuePair<string, string>("__RequestVerificationToken", token))));

            var getInningsHTML = await this._client.GetAsync("Scorecard/Innings");
            token = await Utilities.GetCSRFTokenAsync(getInningsHTML.Content);
            var formContent = new FormUrlEncodedContent(
                    this.FormContent()
                        .Append(new KeyValuePair<string, string>("__RequestVerificationToken", token)));

            // Act + Assert

            // first call to 'Scorecard/Innings' should redirect back to 'Innings'
            var response = await this._client.PostAsync("Scorecard/Innings", formContent);
            Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
            Assert.AreEqual("/Scorecard/Innings", response.Headers.Location.OriginalString.Split('?')[0]);

            // second call to 'Scorecard/Innings' should redirect back to 'Innings'
            response = await this._client.PostAsync("Scorecard/Innings", formContent);
            Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
            Assert.AreEqual("/Scorecard/Innings", response.Headers.Location.OriginalString.Split('?')[0]);

            // third call to 'Scorecard/Innings' should redirect back to 'Innings'
            response = await this._client.PostAsync("Scorecard/Innings", formContent);
            Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
            Assert.AreEqual("/Scorecard/Innings", response.Headers.Location.OriginalString.Split('?')[0]);

            // fourth call to 'Scorecard/Innings' should redirect to 'Verification'
            response = await this._client.PostAsync("Scorecard/Innings", formContent);
            Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
            Assert.AreEqual("/Scorecard/Verification", response.Headers.Location.OriginalString.Split('?')[0]);
        }

        [DataTestMethod]
        [DataRow("BattingScorecard")]
        [DataRow("BowlingScorecard")]
        [DataRow("FallOfWicketScorecard")]
        public async Task POST_EndpointWithMissingFieldsReturnsCorrectValidationMessage(string field)
        {
            // Arrange
            var getScorecardHTML = await this._client.GetAsync("Scorecard/Scorecard");
            var token = await Utilities.GetCSRFTokenAsync(getScorecardHTML.Content);
            await this._client.PostAsync("Scorecard/Scorecard",
                new FormUrlEncodedContent(
                    ScorecardTests.FormContent()
                                  .Append(new KeyValuePair<string, string>("__RequestVerificationToken", token))));

            // Act
            var formElements = FormContent().ToDictionary(kv => kv.Key, kv => kv.Value);
            formElements[field] = null;
            formElements.Add("__RequestVerificationToken", token);
            var formContent = new FormUrlEncodedContent(formElements);
            var response = await this._client.PostAsync("Scorecard/Innings", formContent);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.IsTrue(new Regex($"<span class=\"text-danger field-validation-error\" data-valmsg-for=\"{field}\" data-valmsg-replace=\"true\">The {field} field is required.</span>").Match(content).Success);
        }

        [TestMethod]
        public async Task POST_EndpointWithInvalidBattingScorecardReturnsCorrectValidationMessage()
        {
            // Arrange
            var getScorecardHTML = await this._client.GetAsync("Scorecard/Scorecard");
            var token = await Utilities.GetCSRFTokenAsync(getScorecardHTML.Content);
            await this._client.PostAsync("Scorecard/Scorecard",
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
            var response = await this._client.PostAsync("Scorecard/Innings", formContent);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.IsTrue(new Regex("<span class=\"text-danger field-validation-error\" data-valmsg-for=\"BattingScorecard\" data-valmsg-replace=\"true\">The field BattingScorecard is invalid.</span>").Match(content).Success);
        }

        [TestMethod]
        public async Task POST_EndpointWithInvalidBowlingScorecardReturnsCorrectValidationMessage()
        {
            // Arrange
            var getScorecardHTML = await this._client.GetAsync("Scorecard/Scorecard");
            var token = await Utilities.GetCSRFTokenAsync(getScorecardHTML.Content);
            await this._client.PostAsync("Scorecard/Scorecard",
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
            var response = await this._client.PostAsync("Scorecard/Innings", formContent);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.IsTrue(new Regex("<span class=\"text-danger field-validation-error\" data-valmsg-for=\"BowlingScorecard\" data-valmsg-replace=\"true\">The field BowlingScorecard is invalid.</span>").Match(content).Success);
        }

        [TestMethod]
        public async Task POST_EndpointWithInvalidFallOfWicketScorecardReturnsCorrectValidationMessage()
        {
            // Arrange
            var getScorecardHTML = await this._client.GetAsync("Scorecard/Scorecard");
            var token = await Utilities.GetCSRFTokenAsync(getScorecardHTML.Content);
            await this._client.PostAsync("Scorecard/Scorecard",
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
            var response = await this._client.PostAsync("Scorecard/Innings", formContent);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.IsTrue(new Regex("<span class=\"text-danger field-validation-error\" data-valmsg-for=\"FallOfWicketScorecard\" data-valmsg-replace=\"true\">The field FallOfWicketScorecard is invalid.</span>").Match(content).Success);
        }

        [TestMethod]
        public async Task POST_EndpointWithNegativeExtrasReturnsCorrectValidationMessage()
        {
            // Arrange
            var getScorecardHTML = await this._client.GetAsync("Scorecard/Scorecard");
            var token = await Utilities.GetCSRFTokenAsync(getScorecardHTML.Content);
            await this._client.PostAsync("Scorecard/Scorecard",
                new FormUrlEncodedContent(
                    ScorecardTests.FormContent()
                                  .Append(new KeyValuePair<string, string>("__RequestVerificationToken", token))));

            // Act
            var formElements = FormContent().ToDictionary(kv => kv.Key, kv => kv.Value);
            formElements["Extras"] = "-1";
            formElements.Add("__RequestVerificationToken", token);
            var formContent = new FormUrlEncodedContent(formElements);
            var response = await this._client.PostAsync("Scorecard/Innings", formContent);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.IsTrue(new Regex($"<span class=\"text-danger field-validation-error\" data-valmsg-for=\"Extras\" data-valmsg-replace=\"true\">The field Extras must be between 0 and {int.MaxValue}.</span>").Match(content).Success);
        }
    }
}