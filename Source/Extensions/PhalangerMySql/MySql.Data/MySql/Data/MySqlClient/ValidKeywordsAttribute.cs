namespace MySql.Data.MySqlClient
{
    using System;
    using System.Globalization;

    internal class ValidKeywordsAttribute : Attribute
    {
        private string keywords;

        public ValidKeywordsAttribute(string keywords)
        {
            this.keywords = keywords.ToLower(CultureInfo.InvariantCulture);
        }

        public string[] Keywords
        {
            get
            {
                return this.keywords.Split(new char[] { ',' });
            }
        }
    }
}

