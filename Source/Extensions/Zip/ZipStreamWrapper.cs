using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;

namespace PHP.Library.Zip
{
    public sealed class ZipStreamWrapper : StreamWrapper
    {
        public const string scheme = "zip";

        public override PhpStream Open(ref string path, string mode, StreamOpenOptions options, StreamContext context)
        {
            //From filestreamwrapper
            Debug.Assert(path != null);
            //Debug.Assert(PhpPath.IsLocalFile(path));

            // Get the File.Open modes from the mode string
            FileMode fileMode;
            FileAccess fileAccess;
            StreamAccessOptions ao;

            if (!ParseMode(mode, options, out fileMode, out fileAccess, out ao)) return null;

            string[] arr = path.Split('#');
            string archive = arr[0];
            string entry = arr[1];

            // Open the native stream
            ZipFile zip = null;
            try
            {
                // stream = File.Open(path, fileMode, fileAccess, FileShare.ReadWrite);
                zip = new ZipFile(File.Open(archive, FileMode.Open, FileAccess.Read, FileShare.Read));
            }
            catch (FileNotFoundException)
            {
                // Note: There may still be an URL in the path here.
                PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_file_not_exists",
                    FileSystemUtils.StripPassword(path)));

                return null;
            }
            catch (IOException e)
            {
                if ((ao & StreamAccessOptions.Exclusive) > 0)
                {
                    PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_file_exists",
                        FileSystemUtils.StripPassword(path)));
                }
                else
                {
                    PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_file_io_error",
                        FileSystemUtils.StripPassword(path), PhpException.ToErrorMessage(e.Message)));
                }
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_file_access_denied",
                    FileSystemUtils.StripPassword(path)));
                return null;
            }
            catch (Exception)
            {
                PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_file_invalid",
                    FileSystemUtils.StripPassword(path)));
                return null;
            }

            if ((ao & StreamAccessOptions.SeekEnd) > 0)
            {
                throw new NotSupportedException();
            }

            if ((ao & StreamAccessOptions.Temporary) > 0)
            {
                // Set the file attributes to Temporary too.
                throw new NotSupportedException();
            }

            if (zip == null)
            {
                return null;
            }
            ZipEntry zEntry = zip.GetEntry(entry);
            if (zEntry == null)
            {
                return null;
            }
            Stream s = zip.GetInputStream(zEntry);
            return new NativeStream(s, this, ao, path, context);
        }

        public override string Label { get { return "zipfile"; } }
        public override string Scheme { get { return scheme; } }

        public override bool IsUrl
        {
            get { throw new NotImplementedException(); }
        }
    }
}
