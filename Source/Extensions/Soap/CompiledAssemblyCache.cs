using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Configuration;
using System.Text.RegularExpressions;

namespace PHP.Library.Soap
{
    /// <summary>
    /// Summary description for CompiledAssemblyCache.
    /// </summary>
    internal class CompiledAssemblyCache
    {
        private static string _libPath = "";

        private const string TemporaryFilesSearchPattern = ".*\\#(?<Hash>[0-9a-f]*)"+CodeConstants.TEMPDLLEXTENSION;
        private static Regex reFileStamp = new Regex(TemporaryFilesSearchPattern, RegexOptions.Compiled);

        private CompiledAssemblyCache()
        {
        }


        /// <summary>
        /// Checks the cache.
        /// </summary>
        /// <returns></returns>
        internal static Assembly CheckCacheForAssembly(string wsdl, int contentHash)
        {
            string dir = GetLibTempPath();
            string searchPattern = GetMd5Sum(wsdl) + "#*" + CodeConstants.TEMPDLLEXTENSION;

            foreach (string file in System.IO.Directory.GetFiles(dir, searchPattern))
            {
                Match match = reFileStamp.Match(file);
                
                // not a file we are looking for
                if (!match.Success)
                    continue;

                int fileHash;
                if (!Int32.TryParse((string)match.Groups["Hash"].Value, NumberStyles.AllowHexSpecifier,
                    CultureInfo.InvariantCulture, out fileHash)) continue;

                if (fileHash == contentHash)
                {
                    return Assembly.LoadFrom(file);
                }
            }
            return null;
        }

        ///// <summary>
        ///// Clears the cache.
        ///// </summary>
        ///// <param name="wsdlLocation">WSDL location.</param>
        //internal static void ClearCache(string wsdlLocation)
        //{
        //    // clear the cached assembly file for this WSDL
        //    try
        //    {
        //        string path = GetLibTempPath();

        //        //string path = Path.GetTempPath();
        //        string newFilename = path + GetMd5Sum(wsdlLocation) + CodeConstants.TEMPDLLEXTENSION;

        //        File.Delete(newFilename);
        //    }
        //    catch (Exception ex)
        //    {
        //        //can't delete cache, just leave a notification file so the next time assembly is regenerated

        //        throw new TemporaryCacheException("Problem occured when trying to clear temporary local assembly cache for WSDL: " + wsdlLocation + ".", ex);
        //    }
        //}

        ///// <summary>
        ///// Clears all cached DLLs.
        ///// </summary>
        //internal static void ClearAllCached()
        //{
        //    string path = GetLibTempPath();
        //    DirectoryInfo di = new DirectoryInfo(path);
        //    FileInfo[] dllFiles = di.GetFiles("*" + CodeConstants.TEMPDLLEXTENSION);

        //    foreach (FileInfo fi in dllFiles)
        //    {
        //        try
        //        {
        //            fi.Delete();
        //        }
        //        catch (Exception ex)
        //        {
        //            throw new TemporaryCacheException("Problem occurred when trying to clear temporary local assembly cache.", ex);
        //        }
        //    }
        //}

        /// <summary>
        /// Renames the temp assembly.
        /// </summary>
        /// <param name="pathToAssembly">Path to assembly.</param>
        /// <param name="name">New name for the assembly</param>
        /// <param name="hash">Hash from content of the wsdl file.</param>
        internal static void RenameTempAssembly(string pathToAssembly, string name, int hash)
        {
            string path = Path.GetDirectoryName(pathToAssembly);
            string newFilename = path + @"\" + CompiledAssemblyCache.GetMd5Sum(name) + "#" + hash.ToString("x16") + CodeConstants.TEMPDLLEXTENSION;

            try
            {
                File.Copy(pathToAssembly, newFilename);
            }
            catch
            {
                //do nothing
            }
        }

        /// <summary>
        /// Gets the MD5 sum.
        /// </summary>
        /// <param name="stringToHash">String to hash.</param>
        /// <returns></returns>
        internal static string GetMd5Sum(string stringToHash)
        {
            // First we need to convert the string into bytes, which
            // means using a text encoder
            Encoder enc = Encoding.Unicode.GetEncoder();

            // Create a buffer large enough to hold the string
            byte[] unicodeText = new byte[stringToHash.Length * 2];
            enc.GetBytes(stringToHash.ToCharArray(), 0, stringToHash.Length, unicodeText, 0, true);

            // Now that we have a byte array we can ask the CSP to hash it
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] result = md5.ComputeHash(unicodeText);

            // Build the final string by converting each byte
            // into hex and appending it to a StringBuilder
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < result.Length; i++)
            {
                sb.Append(result[i].ToString("X2", CultureInfo.CurrentCulture));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets the app temp path.
        /// </summary>
        /// <returns></returns>
        internal static string GetLibTempPath()
        {
            string tempPath = _libPath;

            if (tempPath.Length == 0)
                tempPath = ConfigurationManager.AppSettings[CodeConstants.LIBTEMPDIR];
            if (tempPath == null || tempPath.Length == 0)
                tempPath = Path.GetTempPath();

            return tempPath;
        }

        /// <summary>
        /// Sets the lib temp path.
        /// </summary>
        /// <param name="path">Path.</param>
        internal static void SetLibTempPath(string path)
        {
            _libPath = path;
        }
    }
}
