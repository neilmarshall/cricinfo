using System.Threading.Tasks;
using Cricinfo.Models;

namespace Cricinfo.Api.Client
{
    public interface ICricinfoApiClient
    {
        public Task<Match> GetMatchAsync(int id);
        public Task CreateMatchAsync(Match match);
    }
}
