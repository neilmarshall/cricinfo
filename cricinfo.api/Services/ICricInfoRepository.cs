using System;
using System.Threading.Tasks;
using Cricinfo.Api.Models;

namespace Cricinfo.Api.Services
{
    public enum DataCreationResponse
    {
        Success = 0,
        Failure = 1,
        DuplicateContent = 2
    }

    public interface ICricInfoRepository
    {
        public Task<Match> GetMatchAsync(int id);
        public Task<Tuple<DataCreationResponse, int?>> CreateMatchAsync(Match match);
    }
}
