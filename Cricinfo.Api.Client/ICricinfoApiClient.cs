using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cricinfo.Models;

namespace Cricinfo.Api.Client
{
    public interface ICricinfoApiClient
    {
        public Task CreateMatchAsync(Match match);
        public Task CreateTeamAsync(string team);
        public Task<Match> GetMatchAsync(int id);
        public Task<Match[]> GetAllMatchesAsync();
        public Task<IEnumerable<string>> GetTeamsAsync();
        public Task<bool> ExistsAsync(string homeTeam, string awayTeam, DateTime date);
    }
}
