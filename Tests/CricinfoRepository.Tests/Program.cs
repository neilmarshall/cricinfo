using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Cricinfo.Models;
using Cricinfo.Services;
using static System.Console;

namespace CricinfoRepository.Tests
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                var connString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                WriteLine("Reading data...");
                var a = Assembly.GetExecutingAssembly();
                using var s = a.GetManifestResourceStream("CricinfoRepository.Tests.resources.south_africa-england-26-12-18.json");
                using var sr = new StreamReader(s);
                WriteLine("Deserializing data...");
                var match = JsonSerializer.Deserialize<Match>(sr.ReadToEnd());
                WriteLine("Writing data to database...");
                ICricInfoRepository cricInfoRepository = new PostgresCricInfoRepository(connString);
                await cricInfoRepository.DeleteMatchAsync(match.HomeTeam, match.AwayTeam, match.DateOfFirstDay);
                var (response, id) = await cricInfoRepository.CreateMatchAsync(match);
                WriteLine($"Write completed with response '{response}' and id '{id}'");
            }
            catch (Exception e)
            {
                WriteLine(e.Message);
            }
        }
    }
}
