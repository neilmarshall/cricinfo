using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using DbUp;

namespace Db.Deploy.SqlServer
{
    class Program
    {
        private static string GetConnectionString(string connString)
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.DEVELOPMENT.json")
                .Build();

            return config.GetConnectionString(connString);
        }

        static int Main(string[] args)
        {
            var connectionString = GetConnectionString("DefaultConnection");

            EnsureDatabase.For.SqlDatabase(connectionString);

            var upgrader =
                DeployChanges.To
                    .SqlDatabase(connectionString)
                    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
                    .LogToConsole()
                    .Build();

            var result = upgrader.PerformUpgrade();

            if (!result.Successful)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(result.Error);
                Console.ResetColor();

                #if DEBUG
                Console.ReadLine();
                #endif

                return -1;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Success!");
            Console.ResetColor();
            return 0;
        }
    }
}
