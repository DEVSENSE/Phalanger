using System;

namespace PHP.Library.GetText.GetTextSharp
{
    public interface IGettextParserRequestor
    {
        void Handle(string key, string value);
    }
}