using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Cricinfo.Api.Models;
using Cricinfo.Api.Services;

namespace Cricinfo.Api.Unit.Tests
{
    public class Utilities
    {
        internal class MockCricInfoRepository : ICricInfoRepository
        {
            private Dictionary<int, Match> matches;

            public MockCricInfoRepository()
            {
                Assembly a = Assembly.GetExecutingAssembly();
                using Stream s = a.GetManifestResourceStream("Cricinfo.Api.Unit.Tests.resources.south_africa-england-26-12-18.json");
                using StreamReader reader = new StreamReader(s);
                var matchData = reader.ReadToEnd();
                var match = JsonSerializer.Deserialize<Match>(matchData);

                matches = new Dictionary<int, Match>
                {
                    { 42, match }
                };
            }

            public Task<Tuple<DataCreationResponse, int?>> CreateMatchAsync(Match match)
            {
                return Task.Run(() =>
                {
                    if (matches.Values.Any(m => m.DateOfFirstDay == match.DateOfFirstDay && m.HomeTeam == match.HomeTeam && m.AwayTeam == match.AwayTeam))
                    {
                        return Tuple.Create<DataCreationResponse, int?>(DataCreationResponse.DuplicateContent, null);
                    }

                    try
                    {
                        var key = matches.Keys.Max() + 1;
                        matches.Add(key, match);
                        return Tuple.Create<DataCreationResponse, int?>(DataCreationResponse.Success, key);
                    }
                    catch (Exception)
                    {
                        return Tuple.Create<DataCreationResponse, int?>(DataCreationResponse.Failure, null);
                    }
                });
            }

            public Task<Match> GetMatchAsync(int id)
            {
                return Task.Run(() => matches.ContainsKey(id) ? matches[id] : null);
            }
        }
    }
}
