using FakkuSync.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FakkuSync.Core.Common
{
    public class AppConfig : IConfig
    {
        private readonly IConfiguration _config;

        public string OutputPath => _config["outputPath"];

        public AppConfig(IConfiguration config)
        {
            _config = config;
        }
    }
}