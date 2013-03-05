using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace PHP.Library.Curl
{
     internal partial class HttpBitsUploader
    {

         class HttpUploadBitsState
         {
             private Stream WriteStream;
             private byte[] Header;
             private byte[] InnerBuffer;
             private Stream ReadStream;
             private byte[] Footer;
             private bool DoNotCloseWriteStream;

             public bool FileUpload
             {
                 get
                 {
                     return (this.ReadStream != null);
                 }
             }


             public HttpUploadBitsState(Stream readStream, byte[] data, byte[] Header, byte[] Footer, bool DoNotCloseWriteStream)
             {
                 this.ReadStream = readStream;
                 this.InnerBuffer = data;
                 this.Header = Header;
                 this.Footer = Footer;
                 this.DoNotCloseWriteStream = DoNotCloseWriteStream;
             }


             public void SetRequestStream(Stream WriteStream)
             {
                 this.WriteStream = WriteStream;

                 byte[] header = null;
                 if (this.Header != null)
                 {
                     header = this.Header;
                     this.Header = null;
                 }
                 else
                 {
                     header = new byte[0];
                 }
                 this.WriteStream.Write(header, 0, header.Length);
             }


             public bool WriteBytes()
             {
                 byte[] footer = null;
                 int count = 0;

                 if (this.FileUpload)
                 {
                     int num3 = 0;
                     if (this.InnerBuffer != null)
                     {
                         num3 = this.ReadStream.Read(this.InnerBuffer, 0, this.InnerBuffer.Length);
                         if (num3 <= 0)
                         {
                             this.ReadStream.Close();
                             this.InnerBuffer = null;
                         }
                     }
                     if (this.InnerBuffer == null)
                     {
                         if (this.Footer == null)
                         {
                             return true;
                         }
                         count = this.Footer.Length;
                         footer = this.Footer;
                         this.Footer = null;
                     }
                     else
                     {
                         count = num3;
                         footer = this.InnerBuffer;
                     }
                 }
                 else
                 {
                     if (this.InnerBuffer == null)
                     {
                         if (this.Footer == null)
                         {
                             return true;
                         }
                         count = this.Footer.Length;
                         footer = this.Footer;
                         this.Footer = null;
                     }
                     else
                     {
                         footer = this.InnerBuffer;
                         //if (this.ChunkSize != 0)
                         //{
                         //    offset = this.BufferWritePosition;
                         //    this.BufferWritePosition += this.ChunkSize;
                         //    count = this.ChunkSize;
                         //    if (this.BufferWritePosition >= this.InnerBuffer.Length)
                         //    {
                         //        count = this.InnerBuffer.Length - offset;
                         //        this.InnerBuffer = null;
                         //    }
                         //}
                         //else
                         //{
                         count = this.InnerBuffer.Length;
                         this.InnerBuffer = null;
                         //}
                     }
                 }

                 this.WriteStream.Write(footer, 0, count);

                 return false;
             }

             public void Close()
             {
                 this.InnerBuffer = null;
                 this.Header = null;
                 this.Footer = null;

                 if (!DoNotCloseWriteStream && this.WriteStream != null)
                 {
                     this.WriteStream.Close();
                 }
                 if (this.ReadStream != null)
                 {
                     this.ReadStream.Close();
                 }
             }
         }
    }
}
