using FakkuSync.Core.Interfaces;
using FakkuSync.Core.Models;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FakkuSync.Core.Common
{
    public class FakkuClient : IFakkuClient
    {
        private readonly ILogger<FakkuClient> _logger;

        private readonly IHttpSingleton _client;

        public FakkuClient(ILogger<FakkuClient> logger, IHttpSingleton client)
        {
            _logger = logger;
            _client = client;
        }

        public void GetLoginCookies()
        {
            ChromeDriver driver = new ChromeDriver(".");

            List<System.Net.Cookie> cookies = new List<System.Net.Cookie>();

            try
            {
                if (driver == null)
                {
                    throw new ArgumentNullException();
                }

                _logger.LogInformation("Moving to fakku to load cookies if necessary.");
                driver.Navigate().GoToUrl(Constants.FakkuBaseUri);

                string cookiePath = Path.Combine(Environment.CurrentDirectory, Constants.CookiesName);
                try
                {
                    if (File.Exists(cookiePath))
                    {
                        _logger.LogInformation("Cookie file found. Loading...");
                        foreach (CookieSerializable cookie in ObjectSaver.ReadObject<List<CookieSerializable>>(cookiePath))
                        {
                            driver.Manage().Cookies.AddCookie(cookie.Cookie);
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to load cookies from save file.");
                }

                _logger.LogInformation("Attempting to move to login page.");
                driver.Navigate().GoToUrl(Constants.FakkuLoginUri);

                _logger.LogInformation("Waiting for user to login.");
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromHours(1));
                wait.Until(x => x.FindElement(By.CssSelector("a.my-account-drop.js-my-account-links")));
                _logger.LogInformation("Login successful.");

                _logger.LogInformation("Saving cookies for later use.");
                ObjectSaver.SaveObject(driver.Manage().Cookies.AllCookies.Select(c => new CookieSerializable(c)).ToList(), cookiePath);

                cookies.AddRange(driver.Manage().Cookies.AllCookies.Select(c => new System.Net.Cookie(c.Name, c.Value, c.Path, c.Domain)));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to login.");
            }
            finally
            {
                driver.Close();
            }

            if (!cookies.Any())
            {
                _logger.LogError("Could not successfully get login cookies.");
                throw new AccessViolationException("Could not log into fakku.");
            }

            _client.AddCookies(cookies);
        }

        public async Task<List<Uri>> GetUserLibrary()
        {
            string body = await _client.GetStringContent(Constants.FakkuBaseUri);

            string libraryPath = Regex.Match(body, "/users/.+/library").Value;

            if (string.IsNullOrWhiteSpace(libraryPath))
            {
                _logger.LogError("Could not find library link.");
                return new List<Uri>();
            }

            HtmlDocument document = await _client.GetHtmlDocument(new Uri(Constants.FakkuBaseUri, libraryPath));

            return document
                .DocumentNode
                .SelectNodes("//div[contains(@class,\"owned\") and not(contains(@class, \"not-owned\"))]/div[contains(@class,\"book-title\")]/a")
                .Select(node => new Uri(Constants.FakkuBaseUri, node.Attributes["href"].Value))
                .ToList();
        }

        public async Task<Book> GetDownloadInformation(Uri uri)
        {
            _logger.LogInformation($"Getting download information for {uri}");
            HtmlDocument document = await _client.GetHtmlDocument(uri);

            if (document.DocumentNode.SelectNodes("//a[contains(concat(\" \",normalize-space(@class),\" \"),\"js-download-links\")]")?.Any() ?? false)
            {
                string downloadLinkHtml = document.DocumentNode.SelectSingleNode("//script[@id=\"download-product\"]").InnerText;

                HtmlDocument downloadDoc = new HtmlDocument();
                downloadDoc.LoadHtml(downloadLinkHtml);

                var downloadInfo = downloadDoc.DocumentNode.SelectNodes("//a[contains(@href, \"https://books.fakku.net/download\")]")
                    .Select(node => new
                    {
                        Link = node.Attributes["href"].Value,
                        Name = node.InnerHtml
                    })
                    .OrderByDescending(x => x.Name)
                    .First();

                if (uri.AbsoluteUri.Contains("games"))
                {
                    return new Book
                    {
                        DownloadLink = new Uri(downloadInfo.Link),
                        DownloadName = downloadInfo.Name,
                        IsGame = true,
                        OriginalUri = uri
                    };
                }

                Uri bookMetaUrl = new Uri(Constants.FakkuBooksBaseUri, $"{uri.LocalPath}/read");
                string json = await _client.GetStringContent(bookMetaUrl);

                Book bookInfo = string.IsNullOrEmpty(json) ? new Book() : DeserializeBookInfo(json);

                bookInfo.DownloadLink = new Uri(downloadInfo.Link);
                bookInfo.DownloadName = downloadInfo.Name.Trim();
                bookInfo.OriginalUri = uri;

                if (string.IsNullOrEmpty(bookInfo.Title))
                {
                    _logger.LogWarning("Could not deserialize name from request. Attempting to pull name from webpage.");
                    var titleNode = document.DocumentNode.SelectSingleNode("//div[@class=\"content-name\"]/h1");

                    bookInfo.Title = titleNode?.InnerText;
                }

                return bookInfo;
            }

            _logger.LogError($"{uri} does not have a download.");

            return null;
        }

        public async Task DownloadAndUnzip(Book book, string outputPath, string tempPath)
        {
            _logger.LogInformation($"Downloading {book.DownloadName}");
            string bookName = book.Title ?? book.DownloadName;

            string downloadOutput = Path.Combine(tempPath, Constants.MakeValidFileName(book.DownloadName, string.Empty));
            string output = Path.Combine(outputPath, Constants.MakeValidFileName(Path.GetFileNameWithoutExtension(bookName), string.Empty));

            if (Directory.Exists(output))
            {
                _logger.LogInformation($"{output} already exists. Skipping.");
                return;
            }

            try
            {
                await _client.GetFileDownload(book.DownloadLink, book.OriginalUri, downloadOutput);
            }
            catch (Exception e)
            {
                return;
            }

            _logger.LogInformation("Downlad complete.");
            _logger.LogInformation($"Unzipping to {output}");

            try
            {
                ZipFile.ExtractToDirectory(downloadOutput, output);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to unzip.");
                return;
            }

            File.Delete(downloadOutput);

            if (book.Chapters?.Any() ?? false)
            {
                _logger.LogInformation("Organizing chapters.");

                List<string> files = Directory.GetFiles(output).OrderBy(x => x).ToList();
                List<Chapter> chapters = book.Chapters.OrderBy(x => x.Page).ToList();

                int currPage = 0;

                for (int chapterNumber = 0; chapterNumber < chapters.Count; chapterNumber++)
                {
                    string chapterName =
                        Regex.IsMatch(".*[Cc]hapter [0-9]+.*", chapters[chapterNumber].Title) ? chapters[chapterNumber].Title
                            : $"Chapter {chapterNumber + 1} {chapters[chapterNumber].Title}";

                    chapterName = chapterName.Replace(".", "");

                    if (chapters[chapterNumber].Page - book.MultiPages.Where(x => x < chapters[chapterNumber].Page).Count() > files.Count)
                    {
                        _logger.LogWarning($"Fakku has a bad page number for chapter {chapterName}! Please fix manually and report the issue to fakku support!");
                        _logger.LogWarning($"Fakku Support Message:\n Hello. There is a chapter for book {bookName} called {chapters[chapterNumber].Title} that has a page number that is greater than the number of pages in the book.");
                        continue;
                    }

                    string chapterPath = Path.Combine(output, Constants.MakeValidFileName(chapterName, string.Empty));

                    Directory.CreateDirectory(chapterPath);

                    int chapterPageEnd = chapterNumber == chapters.Count - 1 ?
                        files.Count
                        : (int)chapters[chapterNumber + 1].Page - book.MultiPages.Where(x => x < chapters[chapterNumber + 1].Page).Count() - 1;

                    for (; currPage < chapterPageEnd; currPage++)
                    {
                        string sourcePath = files[currPage];
                        string destinationPath = Path.Combine(chapterPath, Path.GetFileName(files[currPage]));
                        File.Move(sourcePath, destinationPath);
                    }
                }
            }

            _logger.LogInformation($"Finished downloading {book.DownloadName}");
        }

        private Book DeserializeBookInfo(string json)
        {
            JObject root = JObject.Parse(json);

            JArray chapters = root["chapters"] as JArray;

            List<Chapter> deserializedChapters = chapters?.Select(x =>
            {
                JObject currChapter = x as JObject;

                int? page = int.TryParse(currChapter["page"].ToObject<string>(), out int pageNumber) ? pageNumber : (int?)null;

                if (page == null)
                    return null;

                string title = currChapter["title"].ToObject<string>();

                return new Chapter
                {
                    Page = page,
                    Title = title
                };
            }).Where(x => x != null).ToList();

            JArray spreads = root["spreads"] as JArray;

            List<int> multiPages = spreads?.Select(x =>
            {
                try
                {
                    List<int> spread = x.ToObject<List<int>>();

                    int first = spread[0];
                    int second = spread[1];

                    return first != second ? first : (int?)null;
                }
                catch (Exception e)
                {
                    return null;
                }
            }).Where(x => x != null).Select(x => (int)x).ToList();

            string contentName = root["content"]["content_name"].ToObject<string>();

            return new Book
            {
                Chapters = deserializedChapters,
                MultiPages = multiPages,
                Title = contentName
            };
        }
    }
}