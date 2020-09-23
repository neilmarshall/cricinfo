using System;
using System.Configuration;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Cricinfo.Models;
using Cricinfo.Services;

namespace Cricinfo.ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length < 2) { return; }

            if (!int.TryParse(args[0], out int n)) { return; }

            try
            {
                var connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"];
                ICricInfoQueryService repository = new CricInfoQueryService<Match>(connectionString.ConnectionString);

                var filename = args[1];
                var match = await repository.GetMatchAsync(n);

                using (var jw = new Utf8JsonWriter(File.Create(filename), new JsonWriterOptions { Indented=true }))
                {
                    JsonSerializer.Serialize(jw, match);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
