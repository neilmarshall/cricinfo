using Cricinfo.UI.ValidationAttributes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Cricinfo.UI.Unit.Tests
{
    [TestClass]
    public class ScorecardTests
    {
        private WebApplicationFactory<Startup> _factory;
        private HttpClient _client;

        internal static IEnumerable<KeyValuePair<string, string>> FormContent()
        {
            yield return new KeyValuePair<string, string>("Venue", "A place");
            yield return new KeyValuePair<string, string>("MatchType", "TestMatch");
            yield return new KeyValuePair<string, string>("DateOfFirstDay", "2019-12-31");
            yield return new KeyValuePair<string, string>("HomeTeam", "Home Team");
            yield return new KeyValuePair<string, string>("AwayTeam", "Away Team");
            yield return new KeyValuePair<string, string>("Result", "Draw");
            yield return new KeyValuePair<string, string>("HomeSquad", string.Join('\n', Enumerable.Repeat("player player", SquadValidatorAttribute.NumberOfPlayers)));
            yield return new KeyValuePair<string, string>("AwaySquad", string.Join('\n', Enumerable.Repeat("player player", SquadValidatorAttribute.NumberOfPlayers)));
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
            var response = await this._client.GetAsync("Scorecard/Scorecard");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task POST_EndpointWithValidFormReturnsCorrectRedirectResult()
        {
            // Arrange
            var getHTML = await this._client.GetAsync("Scorecard/Scorecard");
            var token = await Utilities.GetCSRFTokenAsync(getHTML.Content);

            // Act
            var formContent = new FormUrlEncodedContent(
                FormContent().Append(new KeyValuePair<string, string>("__RequestVerificationToken",
                token)));
            var response = await this._client.PostAsync("Scorecard/Scorecard", formContent);

            // Assert
            Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
            Assert.AreEqual("/Scorecard/Innings", response.Headers.Location.OriginalString.Split('?')[0]);
        }

        [DataTestMethod]
        [DataRow("Venue", "Venue")]
        [DataRow("HomeTeam", "Home Team")]
        [DataRow("AwayTeam", "Away Team")]
        [DataRow("HomeSquad", "Home Squad")]
        [DataRow("AwaySquad", "Away Squad")]
        public async Task POST_EndpointWithMissingFieldsReturnsCorrectValidationMessage(string field, string label)
        {
            // Arrange
            var getHTML = await this._client.GetAsync("Scorecard/Scorecard");
            var token = await Utilities.GetCSRFTokenAsync(getHTML.Content);

            // Act
            var formElements = FormContent().ToDictionary(kv => kv.Key, kv => kv.Value);
            formElements[field] = null;
            formElements.Add("__RequestVerificationToken", token);
            var formContent = new FormUrlEncodedContent(formElements);
            var response = await this._client.PostAsync("Scorecard/Scorecard", formContent);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.IsTrue(new Regex($"<span class=\"text-danger field-validation-error\" data-valmsg-for=\"{field}\" data-valmsg-replace=\"true\">The {label} field is required.</span>").Match(content).Success);
        }

        [DataTestMethod]
        [DataRow("HomeSquad", "Home Squad")]
        [DataRow("AwaySquad", "Away Squad")]
        public async Task POST_EndpointWithInvalidSquadSizeReturnsCorrectValidationMessage(string field, string label)
        {
            // Arrange
            var getHTML = await this._client.GetAsync("Scorecard/Scorecard");
            var token = await Utilities.GetCSRFTokenAsync(getHTML.Content);

            // Act
            var formElements = FormContent().ToDictionary(kv => kv.Key, kv => kv.Value);
            formElements[field] = string.Join('\n', Enumerable.Repeat("player player", SquadValidatorAttribute.NumberOfPlayers + 1));
            formElements.Add("__RequestVerificationToken", token);
            var formContent = new FormUrlEncodedContent(formElements);
            var response = await this._client.PostAsync("Scorecard/Scorecard", formContent);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var expectedErrorMessage = $"The field {label} must be a multiline string of { SquadValidatorAttribute.NumberOfPlayers} entries, each formatted as a sigle firstname followed by one or more last names.";
            Assert.IsTrue(new Regex($"<span class=\"text-danger field-validation-error\" data-valmsg-for=\"{field}\" data-valmsg-replace=\"true\">{expectedErrorMessage}</span>").Match(content).Success);
        }

        [DataTestMethod]
        [DataRow("HomeSquad", "Home Squad")]
        [DataRow("AwaySquad", "Away Squad")]
        public async Task POST_EndpointWithInvalidSquadNamesReturnsCorrectValidationMessage(string field, string label)
        {
            // Arrange
            var getHTML = await this._client.GetAsync("Scorecard/Scorecard");
            var token = await Utilities.GetCSRFTokenAsync(getHTML.Content);

            // Act
            var formElements = FormContent().ToDictionary(kv => kv.Key, kv => kv.Value);
            formElements[field] = string.Join('\n', Enumerable.Repeat("player", SquadValidatorAttribute.NumberOfPlayers));
            formElements.Add("__RequestVerificationToken", token);
            var formContent = new FormUrlEncodedContent(formElements);
            var response = await this._client.PostAsync("Scorecard/Scorecard", formContent);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var expectedErrorMessage = $"The field {label} must be a multiline string of { SquadValidatorAttribute.NumberOfPlayers} entries, each formatted as a sigle firstname followed by one or more last names.";
            Assert.IsTrue(new Regex($"<span class=\"text-danger field-validation-error\" data-valmsg-for=\"{field}\" data-valmsg-replace=\"true\">{expectedErrorMessage}</span>").Match(content).Success);
        }
    }
}
