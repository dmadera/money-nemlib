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
                    var postfix = "FilePath*";
                    if (key.EndsWith(postfix))
                    {
                        var newKey = key.Remove(key.Length - postfix.Length);
                        var filePath = Path.GetFullPath(value);
                        config[newKey] = File.ReadAllText(filePath);
                    }
                }
                catch (IOException error)
                {
                    throw new IOException($"File not found. Configuration: {config}, Key: {key}, FilePath: {value}", error);
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
                .BuildServiceProvider();
        }
    }



}
