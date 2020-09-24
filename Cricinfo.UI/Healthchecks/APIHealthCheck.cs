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
        private readonly string healthcheckEndpoint;

        public APIHealthCheck(string apiEndpoint, string healthcheckEndpoint)
        {
            this.apiEndpoint = apiEndpoint;
            this.healthcheckEndpoint = healthcheckEndpoint;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            using var httpClient = new HttpClient { BaseAddress = new Uri(apiEndpoint) };
            try
            {
                var response = await httpClient.GetAsync(healthcheckEndpoint);
                return response.IsSuccessStatusCode ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy();
            }
            catch (Exception)
            {
                return HealthCheckResult.Unhealthy();
            }
        }
    }
}
