using FakkuSync.Core.Common;
using FakkuSync.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FakkuSync.Core.Config
{
    public static class Dependency
    {
        public static IServiceProvider BuildDependencies(IConfiguration config)
        {
            IServiceCollection collection = new ServiceCollection();

            IConfig appConfig = new AppConfig(config);

            collection.AddSingleton(config);
            collection.AddTransient<FakkuSyncClient>();
            collection.AddSingleton(appConfig);
            collection.AddSingleton<IHttpSingleton, HttpSingleton>();
            collection.AddTransient<IFakkuClient, FakkuClient>();

            collection.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                builder.AddNLog();
            });

            var target = (FileTarget)LogManager.Configuration.FindTargetByName("logfile");
            target.FileName = Path.Combine(appConfig.OutputPath, "FakkuSync.log");
            LogManager.ReconfigExistingLoggers();

            return collection.BuildServiceProvider();
        }
    }
}
