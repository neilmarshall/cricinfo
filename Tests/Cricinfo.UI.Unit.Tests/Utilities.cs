using System;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Cricinfo.Api.Client;
using Microsoft.AspNetCore.TestHost;

namespace Cricinfo.UI.Unit.Tests
{
    internal static class Utilities
    {
        private class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
        {
            private protected TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
                ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
                : base(options, logger, encoder, clock)
            {
            }

            private protected virtual Claim[] Claims { get; }

            protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            {
                var identity = new ClaimsIdentity(Claims, "Test");
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, "Test");

                var result = AuthenticateResult.Success(ticket);

                return Task.FromResult(result);
            }
        }

        private class TestAuthHandlerWithClaims : TestAuthHandler
        {
            public TestAuthHandlerWithClaims(IOptionsMonitor<AuthenticationSchemeOptions> options,
                ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
                : base(options, logger, encoder, clock)
            {
            }

            private protected override Claim[] Claims
            {
                get => new[]
                {
                    new Claim("CanAddTeam", "true"),
                    new Claim("CanAddScorecard", "true")
                };
            }
        }

        private class TestAuthHandlerWithAdminClaims : TestAuthHandler
        {
            public TestAuthHandlerWithAdminClaims(IOptionsMonitor<AuthenticationSchemeOptions> options,
                ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
                : base(options, logger, encoder, clock)
            {
            }

            private protected override Claim[] Claims
            {
                get => new[]
                {
                    new Claim("CanAddUser", "true"),
                    new Claim("CanManagePermissions", "true")
                };
            }
        }

        private class TestAuthHandlerWithoutClaims : TestAuthHandler
        {
            public TestAuthHandlerWithoutClaims(IOptionsMonitor<AuthenticationSchemeOptions> options,
                ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
                : base(options, logger, encoder, clock)
            {
            }

            private protected override Claim[] Claims
            {
                get => new[]
                {
                    new Claim("CanAddTeam", "false"),
                    new Claim("CanAddScorecard", "false")
                };
            }
        }

        internal class CustomWebApplicationFactory<TStartup>
            : WebApplicationFactory<TStartup> where TStartup : class
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.ConfigureServices(services => { });
            }
        }

        internal class AuthenticatedCustomWebApplicationFactory<TStartup>
            : WebApplicationFactory<TStartup> where TStartup : class
        {
            private string authenticationScheme;

            internal AuthenticatedCustomWebApplicationFactory(string authenticationScheme)
            {
                this.authenticationScheme = authenticationScheme;
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the app's ICricinfoApiClient registration.
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType ==
                            typeof(ICricinfoApiClient));

                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add ICricinfoApiClient using a mock repository for testing.
                    services.AddScoped(_ => MoqCricinfoApiClient());

                    // Mock authentication / authorization
                    services.AddAuthentication(options => { options.DefaultAuthenticateScheme = this.authenticationScheme; })
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandlerWithClaims>("TestAuthHandlerWithClaims", options => { })
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandlerWithAdminClaims>("TestAuthHandlerWithAdminClaims", options => { })
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandlerWithoutClaims>("TestAuthHandlerWithoutClaims", options => { });
                });
            }
        }

        internal static async Task<string> GetCSRFTokenAsync(HttpContent content)
        {
            var html = await content.ReadAsStringAsync();
            var regex = new Regex("__RequestVerificationToken\\\" type=\\\"hidden\" value=\\\"(?<token>[^\"]*)\"");
            var match = regex.Match(html);

            if (match.Success)
            {
                return match.Groups["token"].Value;
            }
            else
            {
                throw new ArgumentException("could not find CSRF token");
            }
        }

        private static ICricinfoApiClient MoqCricinfoApiClient()
        {
            var mock = new Mock<ICricinfoApiClient>();

            mock.Setup(ICricinfoApiClient => ICricinfoApiClient
                .ExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns((string homeTeam, string awayTeam, DateTime _) => Task.FromResult(
                    homeTeam == "duplicate home team" && awayTeam == "duplicate away team" ? true : false));

            return mock.Object;
        }
    }
}
