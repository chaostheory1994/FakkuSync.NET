using FakkuSync.Core.Interfaces;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace FakkuSync.Core.Common
{
    public class HttpSingleton : IHttpSingleton
    {
        private readonly ILogger<HttpSingleton> _logger;
        private readonly HttpClient _client;
        private readonly CookieContainer _cookieContainer;

        public HttpSingleton(ILogger<HttpSingleton> logger)
        {
            _logger = logger;
            _cookieContainer = new CookieContainer();
            _client = new HttpClient(new HttpClientHandler { CookieContainer = _cookieContainer });
        }

        public void AddCookies(List<Cookie> cookies)
        {
            cookies.ForEach(cookie => _cookieContainer.Add(cookie));
        }

        public async Task<HtmlDocument> GetHtmlDocument(Uri uri)
        {
            string body = await MakeGetRequest(uri);

            if (string.IsNullOrWhiteSpace(body))
                return null;

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(body);

            return document;
        }

        public Task<string> GetStringContent(Uri uri)
        {
            return MakeGetRequest(uri);
        }

        public async Task GetFileDownload(Uri uri, Uri referer, string outFile)
        {
            _logger.LogInformation($"Attempting to download file from {uri}");
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Add("Referer", referer.ToString());

            Uri downloadUri = uri;

            HttpResponseMessage response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (response.StatusCode == HttpStatusCode.Found)
            {
                _logger.LogInformation("Redirect found. Attempting to follow.");

                string newDownloadLocation = response.Headers.GetValues("Location").First();

                if (string.IsNullOrEmpty(newDownloadLocation))
                {
                    _logger.LogError($"Failed to download file!");
                    throw new Exception("Failed to download file!");
                }

                downloadUri = new Uri(newDownloadLocation);
            }
            else if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Download call failed.");
                throw new Exception("Download call failed.");
            }

            WebClient downloadClient = new WebClient();
            downloadClient.Headers.Add(HttpRequestHeader.Cookie, _cookieContainer.GetCookieHeader(uri));
            downloadClient.Headers.Add(HttpRequestHeader.Referer, referer.ToString());

            downloadClient.DownloadFile(downloadUri, outFile);
        }

        private async Task<string> MakeGetRequest(Uri uri)
        {
            _logger.LogInformation($"Getting {uri}");
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
            HttpResponseMessage response = await _client.SendAsync(request);
            return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync() : null;
        }
    }
}