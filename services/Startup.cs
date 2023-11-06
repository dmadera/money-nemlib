using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Services
{

    public static class Startup
    {
        private static IConfiguration BuildConfiguration()
        {
            var enviroment = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile($"appsettings.{enviroment}.json", true)
                .AddEnvironmentVariables()
                .Build();


            foreach (var (key, value) in config.AsEnumerable())
            {
                try
                {
                    if (key.StartsWith("DatabaseConfig:") && AesOperation.IsBase64String(value))
                    {
                        config[key] = AesOperation.DecryptString(Secret.GetAesKey(), value);
                    }
                }
                catch (Exception error)
                {
                    throw new InvalidDataException($"Unable to decrypt {key} from app config.", error);
                }
            }

            return config;
        }

        public static IServiceProvider BuildServiceProvider()
        {
            var config = BuildConfiguration();

            return new ServiceCollection()
                .AddOptions()
                .Configure<DatabaseConfig>(config.GetSection("DatabaseConfig"))
                .Configure<money_nemlib.ParserConfig>(config.GetSection("ParserConfig"))
                // Add more configuration
                .BuildServiceProvider();
        }
    }



}
