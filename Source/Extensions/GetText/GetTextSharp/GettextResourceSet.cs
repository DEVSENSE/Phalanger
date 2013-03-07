using System;
using System.IO;
using System.Resources;

namespace PHP.Library.GetText.GetTextSharp
{
    public class GettextResourceSet : ResourceSet
    {
        public GettextResourceSet(string filename)
            : base(new GettextResourceReader(File.OpenRead(filename)))
        {
        }
        public GettextResourceSet(Stream stream)
            : base(new GettextResourceReader(stream))
        {
        }
        public override Type GetDefaultReader()
        {
            return typeof(GettextResourceReader);
        }
    }
}