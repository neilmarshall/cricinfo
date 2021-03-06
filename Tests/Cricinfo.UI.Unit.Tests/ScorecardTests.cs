using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Cricinfo.UI.ValidationAttributes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cricinfo.UI.Unit.Tests
{
    [TestClass]
    public class ScorecardTests
    {
        private static HttpClient client;

        internal static IEnumerable<KeyValuePair<string, string>> FormContent()
        {
            var generatedNames = string.Join('\n',
                Enumerable.Range(1, SquadValidatorAttribute.NumberOfPlayers)
                          .Select((_, i) => $"player player{i}"));

            yield return new KeyValuePair<string, string>("Venue", "A place");
            yield return new KeyValuePair<string, string>("MatchType", "TestMatch");
            yield return new KeyValuePair<string, string>("DateOfFirstDay", "2019-12-31");
            yield return new KeyValuePair<string, string>("HomeTeam", "Home Team");
            yield return new KeyValuePair<string, string>("AwayTeam", "Away Team");
            yield return new KeyValuePair<string, string>("Result", "Draw");
            yield return new KeyValuePair<string, string>("HomeSquad", generatedNames);
            yield return new KeyValuePair<string, string>("AwaySquad", generatedNames);
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
            var response = await client.GetAsync("Scorecard");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task POST_EndpointWithValidFormReturnsCorrectRedirectResult()
        {
            // Arrange
            var getHTML = await client.GetAsync("Scorecard");
            var token = await Utilities.GetCSRFTokenAsync(getHTML.Content);

            // Act
            var formContent = new FormUrlEncodedContent(
                FormContent().Append(new KeyValuePair<string, string>("__RequestVerificationToken",
                token)));
            var response = await client.PostAsync("Scorecard", formContent);

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
            var getHTML = await client.GetAsync("Scorecard");
            var token = await Utilities.GetCSRFTokenAsync(getHTML.Content);

            // Act
            var formElements = FormContent().ToDictionary(kv => kv.Key, kv => kv.Value);
            formElements[field] = null;
            formElements.Add("__RequestVerificationToken", token);
            var formContent = new FormUrlEncodedContent(formElements);
            var response = await client.PostAsync("Scorecard", formContent);
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
            var getHTML = await client.GetAsync("Scorecard");
            var token = await Utilities.GetCSRFTokenAsync(getHTML.Content);

            // Act
            var formElements = FormContent().ToDictionary(kv => kv.Key, kv => kv.Value);
            formElements[field] = string.Join('\n', Enumerable.Repeat("player player", SquadValidatorAttribute.NumberOfPlayers + 1));
            formElements.Add("__RequestVerificationToken", token);
            var formContent = new FormUrlEncodedContent(formElements);
            var response = await client.PostAsync("Scorecard", formContent);
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
            var getHTML = await client.GetAsync("Scorecard");
            var token = await Utilities.GetCSRFTokenAsync(getHTML.Content);

            // Act
            var formElements = FormContent().ToDictionary(kv => kv.Key, kv => kv.Value);
            formElements[field] = string.Join('\n', Enumerable.Repeat("player", SquadValidatorAttribute.NumberOfPlayers));
            formElements.Add("__RequestVerificationToken", token);
            var formContent = new FormUrlEncodedContent(formElements);
            var response = await client.PostAsync("Scorecard", formContent);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var expectedErrorMessage = $"The field {label} must be a multiline string of { SquadValidatorAttribute.NumberOfPlayers} entries, each formatted as a sigle firstname followed by one or more last names.";
            Assert.IsTrue(new Regex($"<span class=\"text-danger field-validation-error\" data-valmsg-for=\"{field}\" data-valmsg-replace=\"true\">{expectedErrorMessage}</span>").Match(content).Success);
        }

        [TestMethod]
        public async Task POST_EndpointWithDuplicateMatchDetailsReturnsError()
        {
            // Arrange
            var getHTML = await client.GetAsync("Scorecard");
            var token = await Utilities.GetCSRFTokenAsync(getHTML.Content);

            // Act
            var formElements = FormContent().ToDictionary(kv => kv.Key, kv => kv.Value);
            formElements["HomeTeam"] = "duplicate home team";
            formElements["AwayTeam"] = "duplicate away team";
            formElements.Add("__RequestVerificationToken", token);
            var formContent = new FormUrlEncodedContent(formElements);
            var response = await client.PostAsync("Scorecard", formContent);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var expectedErrorMessage = "A record already exists for the specified teams and date.";
            Assert.IsTrue(content.Contains(expectedErrorMessage));
        }
    }
}
