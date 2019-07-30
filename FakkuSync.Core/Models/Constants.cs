using System;
using System.IO;
using System.Text.RegularExpressions;

namespace FakkuSync.Core.Models
{
    public static class Constants
    {
        public static Uri FakkuBaseUri => new Uri("https://www.fakku.net");

        public static Uri FakkuBooksBaseUri => new Uri("https://books.fakku.net");

        public static Uri FakkuLoginUri => new Uri(FakkuBaseUri, "login");

        public static string CookiesName => "cookies.bin";

        public static string MakeValidFileName(string filename, string replacement)
        {
            Regex removeInvalidChars = new Regex($"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]",
                RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant);

            return removeInvalidChars.Replace(filename, replacement);
        }
    }
}