using System;
using System.Collections.Generic;

namespace PHP.Library.GetText.GetTextSharp
{
    public class DictionaryGettextParserRequestor : Dictionary<string, string>, IGettextParserRequestor
    {
        public void Handle(string key, string value)
        {
            base[key] = value;
        }
    }
}