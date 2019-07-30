using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace FakkuSync.Core.Interfaces
{
    public interface IHttpSingleton
    {
        void AddCookies(List<Cookie> cookies);

        Task<HtmlDocument> GetHtmlDocument(Uri uri);

        Task<string> GetStringContent(Uri uri);

        Task GetFileDownload(Uri uri, Uri referer, string outFile);
    }
}