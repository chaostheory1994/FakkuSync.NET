using FakkuSync.Core.Interfaces;
using FakkuSync.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FakkuSync.Core
{
    public class FakkuSyncClient
    {
        private readonly ILogger<FakkuSyncClient> _logger;
        private readonly IConfig _config;
        private readonly IFakkuClient _client;

        public FakkuSyncClient(ILogger<FakkuSyncClient> logger, IConfig config, IFakkuClient client)
        {
            _logger = logger;
            _config = config;
            _client = client;
        }

        public async Task Start()
        {
            if (!Directory.Exists(_config.OutputPath))
            {
                _logger.LogError($"The directory {_config.OutputPath} could not be found.");
                return;
            }

            string outputPath = _config.OutputPath;
            string tempPath = GetTempPath(outputPath);

            _client.GetLoginCookies();

            List<Uri> libraryLinks = await _client.GetUserLibrary();

            _logger.LogInformation("Found library urls.");
            _logger.LogInformation($"Found urls for {libraryLinks.Count} items.");

            foreach (Uri link in libraryLinks)
            {
                Book book = await _client.GetDownloadInformation(link);

                if (book == null)
                    continue;

                await _client.DownloadAndUnzip(book, outputPath, tempPath);
            }

            _logger.LogInformation("Finished Syncing. Enjoy the fap!");
        }

        public string GetTempPath(string outputPath)
        {
            string tempPath = Path.Combine(outputPath, "temp");

            if (!Directory.Exists(tempPath))
            {
                _logger.LogInformation("Creating temporary folder for downloads.");
                Directory.CreateDirectory(tempPath);
            }

            _logger.LogInformation($"Using directory {tempPath} for temporary files.");

            return tempPath;
        }
    }
}