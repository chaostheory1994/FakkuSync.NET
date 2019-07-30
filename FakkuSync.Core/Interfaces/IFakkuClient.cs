using FakkuSync.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FakkuSync.Core.Interfaces
{
    public interface IFakkuClient
    {
        void GetLoginCookies();

        Task<List<Uri>> GetUserLibrary();

        Task<Book> GetDownloadInformation(Uri uri);

        Task DownloadAndUnzip(Book book, string outputPath, string tempPath);
    }
}