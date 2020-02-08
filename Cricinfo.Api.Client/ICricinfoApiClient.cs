using System;
using System.Threading.Tasks;
using Cricinfo.Models;

namespace Cricinfo.Api.Client
{
    public interface ICricinfoApiClient
    {
        public Task<Match> GetMatchAsync(int id);
        public Task CreateMatchAsync(Match match);
        public Task<bool> MatchExistsAsync(string homeTeam, string awayTeam, DateTime date);
    }
}
