using OpenQA.Selenium;
using System;

namespace FakkuSync.Core.Models
{
    [Serializable]
    public class CookieSerializable
    {
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string Value
        {
            get { return _value; }
        }

        public string Domain
        {
            get { return _domain; }
        }

        public string Path
        {
            get { return _path; }
        }

        public DateTime? ExpirationDate
        {
            get { return _expirationDate; }
        }

        private string _name;
        private string _value;
        private string _path;
        private string _domain;
        private DateTime? _expirationDate;

        public Cookie Cookie => new Cookie(_name, _value, _domain, _path, _expirationDate);

        public CookieSerializable(Cookie cookie)
        {
            _name = cookie.Name;
            _value = cookie.Value;
            _path = cookie.Path;
            _domain = cookie.Domain;
            _expirationDate = cookie.Expiry;
        }
    }
}