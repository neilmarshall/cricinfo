using System;
using System.Threading.Tasks;
using Cricinfo.Api.Models;

namespace Cricinfo.Api.Services
{
    public class CricInfoRepository : ICricInfoRepository
    {
        public Task<Tuple<DataCreationResponse, int?>> CreateMatchAsync(Match match)
        {
            throw new NotImplementedException();
        }

        public Task<Match> GetMatchAsync(int id)
        {
            return Task.Run(() => new Match());
        }
    }
}
