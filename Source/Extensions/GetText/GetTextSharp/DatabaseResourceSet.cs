using System;
using System.Globalization;
using System.Resources;

namespace PHP.Library.GetText.GetTextSharp
{
    public class DatabaseResourceSet : ResourceSet
    {
        internal DatabaseResourceSet(string dsn, CultureInfo culture)
            : base(new DatabaseResourceReader(dsn, culture))
        {
        }
        internal DatabaseResourceSet(string dsn, CultureInfo culture, string sp)
            : base(new DatabaseResourceReader(dsn, culture, sp))
        {
        }
        public override Type GetDefaultReader()
        {
            return typeof(DatabaseResourceReader);
        }
    }
}