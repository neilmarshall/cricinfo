using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Cricinfo.UI.Unit.Tests
{
    [TestClass]
    public class AuthenticationFixtures
    {
        private static HttpClient client;

        [ClassInitialize]
        public static void Initialize(TestContext tc)
        {
            var factory = new Utilities.CustomWebApplicationFactory<Startup>();
            client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        [DataTestMethod]
        [DataRow("Scorecard")]
        [DataRow("Scorecard/")]
        [DataRow("Scorecard/Index")]
        [DataRow("Scorecard/Innings")]
        [DataRow("Scorecard/Verification")]
        [DataRow("Teams")]
        [DataRow("Teams/")]
        [DataRow("Teams/Index")]
        [DataRow("Identity/Account/Register")]
        [DataRow("Identity/Account/ManagePermissions")]
        public async Task RoutesRequiringAuthenticationRedirectIfUnauthenticated(string endpoint)
        {
            var response = await client.GetAsync(endpoint);

            Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
            Assert.AreEqual("/Identity/Account/Login", response.Headers.Location.LocalPath);
        }
    }

    [TestClass]
    public class AuthorizationFixtures
    {
        private static HttpClient client;

        [ClassInitialize]
        public static void Initialize(TestContext tc)
        {
            var factory = new Utilities.AuthenticatedCustomWebApplicationFactory<Startup>("TestAuthHandlerWithoutClaims");
            client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        [DataTestMethod]
        [DataRow("Scorecard")]
        [DataRow("Scorecard/")]
        [DataRow("Scorecard/Index")]
        [DataRow("Scorecard/Innings")]
        [DataRow("Scorecard/Verification")]
        [DataRow("Teams")]
        [DataRow("Teams/")]
        [DataRow("Teams/Index")]
        [DataRow("Identity/Account/Register")]
        [DataRow("Identity/Account/ManagePermissions")]
        public async Task RoutesRequiringAuthorizationRedirectIfUnauthorized(string endpoint)
        {
            var response = await client.GetAsync(endpoint);

            Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
            Assert.AreEqual("/Unauthorized", response.Headers.Location.LocalPath);
        }
    }

    [TestClass]
    public class AdminAuthorizationFixtures
    {
        private static HttpClient client;

        [ClassInitialize]
        public static void Initialize(TestContext tc)
        {
            var factory = new Utilities.AuthenticatedCustomWebApplicationFactory<Startup>("TestAuthHandlerWithAdminClaims");
            client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        [DataTestMethod]
        [DataRow("Identity/Account/Register")]
        [DataRow("Identity/Account/ManagePermissions")]
        public async Task RoutesRequiringAdminAuthorizationAllowAccessIfAuthorized(string endpoint)
        {
            var response = await client.GetAsync(endpoint);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
