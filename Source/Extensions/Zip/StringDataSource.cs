using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using System.IO;
using PHP.Core;

namespace PHP.Library.Zip
{
    internal sealed class StringDataSource : IStaticDataSource
    {
        private readonly Stream m_src;

        internal StringDataSource(object source)
        {
            this.m_src = new MemoryStream(PhpStream.AsBinary(source).ReadonlyData);
        }

        public Stream GetSource()
        {
            return this.m_src;
        }
    }
}
