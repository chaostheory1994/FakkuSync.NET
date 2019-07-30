using System;
using System.Collections.Generic;
using System.Text;

namespace FakkuSync.Core.Models
{
    public class Book
    {
        public Uri OriginalUri { get; set; }

        public Uri DownloadLink { get; set; }

        public string DownloadName { get; set; }

        public bool IsGame { get; set; }

        public List<Chapter> Chapters { get; set; }

        public List<int> MultiPages { get; set; }

        public string Title { get; set; }
    }
}
