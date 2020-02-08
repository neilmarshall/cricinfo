using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Cricinfo.Api.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Cricinfo.UI.Unit.Tests
{
    internal static class Utilities
    {
        internal class CustomWebApplicationFactory<TStartup>
                : WebApplicationFactory<TStartup> where TStartup : class
        {
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
                    services.AddScoped<ICricinfoApiClient, Utilities.MockCricinfoApiClient>();
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

        internal class MockCricinfoApiClient : ICricinfoApiClient
        {
            public Task CreateMatchAsync(Models.Match match)
                => throw new NotImplementedException();

            public Task<Models.Match> GetMatchAsync(int id)
                => throw new NotImplementedException();

            public Task<bool> MatchExistsAsync(string homeTeam, string awayTeam, DateTime _)
            {
                if (homeTeam == "duplicate home team" && awayTeam == "duplicate away team")
                    return Task.Run(() => true);
                return Task.Run(() => false);
            }
        }
    }
}
