using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cricinfo.Models;

namespace Cricinfo.Api.Client
{
    public interface ICricinfoApiClient
    {
        public Task CreateMatchAsync(Match match);
        public Task<Match> GetMatchAsync(int id);
        public Task<IEnumerable<string>> GetTeamsAsync();
        public Task<bool> MatchExistsAsync(string homeTeam, string awayTeam, DateTime date);
    }
}
