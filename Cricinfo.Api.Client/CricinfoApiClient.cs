using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Cricinfo.Models;

namespace Cricinfo.Api.Client
{
    public class CricinfoApiClient : ICricinfoApiClient
    {
        private readonly HttpClient _httpClient;

        public CricinfoApiClient(HttpClient httpClient)
        {
            this._httpClient = httpClient;
        }

        public async Task CreateMatchAsync(Match match)
        {
            var content = new StringContent(JsonSerializer.Serialize(match),
                System.Text.Encoding.UTF8, "application/json");
            var httpResponse = await _httpClient.PostAsync("/api", content);

            if (httpResponse.StatusCode != HttpStatusCode.Created && httpResponse.StatusCode != HttpStatusCode.Conflict)
            {
                throw new ArgumentException($"failed to create data for match");
            }
        }

        public async Task CreateTeamAsync(string team)
        {
            var httpResponse = await _httpClient.PostAsync($"/api/Teams?team={team}", null);

            if (httpResponse.StatusCode != HttpStatusCode.Created && httpResponse.StatusCode != HttpStatusCode.Conflict)
            {
                throw new ArgumentException($"failed to create data for match");
            }
        }

        public async Task<Match> GetMatchAsync(int id)
        {
            var httpResponse = await _httpClient.GetAsync($"/api/Match/{id}");

            if (httpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new ArgumentException($"failed to evaluate response for id {id}");
            }

            var jsonResponse = await httpResponse.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Match>(jsonResponse,
                new JsonSerializerOptions { PropertyNameCaseInsensitive=true});
        }

        public async Task<Match[]> GetAllMatchesAsync()
        {
            var httpResponse = await _httpClient.GetAsync($"/api/Match");

            if (httpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new ArgumentException($"failed to evaluate response");
            }

            var jsonResponse = await httpResponse.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Match[]>(jsonResponse,
                new JsonSerializerOptions { PropertyNameCaseInsensitive=true});
        }

        public async Task<IEnumerable<string>> GetTeamsAsync()
        {
            var httpResponse = await _httpClient.GetAsync($"/api/Teams");

            if (httpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new ArgumentException("failed to evaluate Teams");
            }

            var jsonResponse = await httpResponse.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<IEnumerable<string>>(jsonResponse);
        }

        public async Task<bool> ExistsAsync(string homeTeam, string awayTeam, DateTime date)
        {
            var httpResponse = await _httpClient.GetAsync($"/api/Match/Exists?homeTeam={homeTeam}&awayTeam={awayTeam}&date={date.Year}-{date.Month}-{date.Day}");

            if (httpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new ArgumentException($"failed to evaluate response for parameters ({homeTeam}, {awayTeam}, {date})");
            }

            var response = await httpResponse.Content.ReadAsStringAsync();
            return bool.Parse(response);
        }
    }
}
