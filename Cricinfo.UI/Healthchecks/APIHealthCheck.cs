using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cricinfo.UI.Healthchecks
{
    public class APIHealthCheck : IHealthCheck
    {
        private readonly string apiEndpoint;

        public APIHealthCheck(string apiEndpoint)
        {
            this.apiEndpoint = apiEndpoint;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            using var httpClient = new HttpClient { BaseAddress = new Uri(apiEndpoint) };
            try
            {
                var response = await httpClient.GetAsync("/api/health");
                return response.IsSuccessStatusCode ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy();
            }
            catch (Exception)
            {
                return HealthCheckResult.Unhealthy();
            }
        }
    }
}
