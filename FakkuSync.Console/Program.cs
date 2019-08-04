using FakkuSync.Core;
using FakkuSync.Core.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace FakkuSync.Console
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            IServiceProvider provider = Dependency.BuildDependencies(config);

            await provider.GetService<FakkuSyncClient>().Start();
        }
    }
}