using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Cricinfo.Models;

namespace Cricinfo.Api.Client
{
    public class CricinfoApiClient : ICricinfoApiClient
    {
        private readonly string _url;
        private readonly HttpClient _httpClient;

        public CricinfoApiClient(string url, HttpClient httpClient)
        {
            this._url = url;
            this._httpClient = httpClient;
        }

        public CricinfoApiClient(string url)
            : this(url, new HttpClient())
        {
        }

        public async Task CreateMatchAsync(Match match)
        {
            var content = new StringContent(JsonSerializer.Serialize(match),
                System.Text.Encoding.UTF8, "application/json");
            var httpResponse = await _httpClient.PostAsync($"{_url}/api", content);

            if (httpResponse.StatusCode != HttpStatusCode.Created && httpResponse.StatusCode != HttpStatusCode.Conflict)
            {
                throw new ArgumentException($"failed to create data for match");
            }
        }

        public async Task<Match> GetMatchAsync(int id)
        {
            var httpResponse = await _httpClient.GetAsync($"{_url}/api/{id}");

            if (httpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new ArgumentException($"failed to evaluate response for id {id}");
            }

            var jsonResponse = await httpResponse.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Match>(jsonResponse,
                new JsonSerializerOptions { PropertyNameCaseInsensitive=true});
        }
    }
}
