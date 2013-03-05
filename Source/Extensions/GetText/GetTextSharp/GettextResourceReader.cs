using System;
using System.Collections;
using System.IO;
using System.Resources;

namespace PHP.Library.GetText.GetTextSharp
{
    public class GettextResourceReader : IResourceReader, IEnumerable, IDisposable
    {
        private Stream stream;
        public GettextResourceReader(Stream stream)
        {
            this.stream = stream;
        }
        public void Close()
        {
            if (this.stream != null)
            {
                this.stream.Close();
            }
        }
        public IDictionaryEnumerator GetEnumerator()
        {
            if (this.stream == null)
            {
                throw new ArgumentNullException("Input stream cannot be null");
            }
            IDictionaryEnumerator result;
            using (StreamReader streamReader = new StreamReader(this.stream))
            {
                result = new PoParser().ParseIntoDictionary(streamReader).GetEnumerator();
            }
            return result;
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
        public void Dispose()
        {
            if (this.stream != null)
            {
                this.stream.Dispose();
            }
        }
    }
}
