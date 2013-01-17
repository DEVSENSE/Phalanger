using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using System.IO;
using PHP.Core;

namespace PHP.Library.Zip
{
    internal sealed class FileHandleDataSource : IStaticDataSource
    {
        private readonly PhpStream handle;
        private readonly int flength;

        private readonly MemoryStream m_ms;

        public FileHandleDataSource(PhpStream handle, int flength)
        {
            this.handle = handle;
            this.flength = flength;

            //TODO : Replace memorystream with a better reading/seeking class
            PhpBytes data;

            if (flength > 0)
            {
                data = handle.ReadBytes(flength);
            }
            else
            {
                data = handle.ReadBinaryContents(-1);
            }
            this.m_ms = new MemoryStream(data.ReadonlyData);
        }

        public Stream GetSource()
        {
            return this.m_ms;
        }
    }
}
