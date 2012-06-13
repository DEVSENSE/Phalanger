using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using System.ComponentModel;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Zip;
using System.IO;
using ICSharpCode.SharpZipLib.Core;

namespace PHP.Library.Zip
{
    [ImplementsType]
    public partial class ZipArchive : PhpObject
    {
        /// <summary>
        /// Real zip archive
        /// </summary>
        private ZipFile m_zip;

        #region Constructor
        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ZipArchive(ScriptContext/*!*/context, bool newInstance)
            : base(context, newInstance)
        { }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ZipArchive(ScriptContext/*!*/context, PHP.Core.Reflection.DTypeDesc caller)
            : base(context, caller)
        { }

        public object __construct(ScriptContext context)
        {
            return null;
        }

        public static object __construct(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ZipArchive)instance).__construct(stack.Context);
        }
        #endregion

        #region Properties
        public int status { get { throw new NotImplementedException(); } }
        public int statusSys { get { throw new NotImplementedException(); } }
        public int numFiles { get { throw new NotImplementedException(); } }
        public string filename { get { throw new NotImplementedException(); } }
        public string comment { get { throw new NotImplementedException(); } }
        #endregion

        #region addEmptyDir
        [ImplementsMethod, PhpVisible]
        public object addEmptyDir(ScriptContext context, object dirname)
        {
            string directoryName = Core.Convert.ObjectToString(dirname);
            this.m_zip.BeginUpdate();
            try
            {
                this.m_zip.AddDirectory(directoryName);
                this.m_zip.CommitUpdate();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ZipArchive::addEmptyDir", ex.Message);
                this.m_zip.AbortUpdate();
                return false;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object addEmptyDir(object instance, PhpStack stack)
        {
            object dirname = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ZipArchive)instance).addEmptyDir(stack.Context, dirname);
        }
        #endregion

        #region addFile
        [ImplementsMethod, PhpVisible]
        public object addFile(ScriptContext context, object filename)
        {
            return this.addFile(context, filename, null, 0, 0);
        }

        [ImplementsMethod, PhpVisible]
        public object addFile(ScriptContext context, object filename, object localname)
        {
            return this.addFile(context, filename, localname, 0, 0);
        }

        [ImplementsMethod, PhpVisible]
        public object addFile(ScriptContext context, object filename, object localname, object start)
        {
            return this.addFile(context, filename, localname, start, 0);
        }

        [ImplementsMethod, PhpVisible]
        public object addFile(ScriptContext context, object filename, object localname, object start, object length)
        {
            string fpath = Core.Convert.ObjectToString(filename);
            string flocal = Core.Convert.ObjectToString(localname);
            int fstart = Core.Convert.ObjectToInteger(start);
            int flength = Core.Convert.ObjectToInteger(length);

            try
            {
                using (PhpStream handle = PhpStream.Open(fpath, "r", StreamOpenOptions.Empty))
                {
                    if (fstart > 0)
                    {
                        PhpFile.Seek(handle, fstart);
                    }

                    FileHandleDataSource src = new FileHandleDataSource(handle, flength);
                    this.m_zip.BeginUpdate();
                    this.m_zip.Add(src, flocal);
                    this.m_zip.CommitUpdate();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ZipArchive::addFile", ex.Message);
                return false;
            }
        }

        public static object addFile(object instance, PhpStack stack)
        {
            object filename = stack.PeekValue(1);
            object localname = stack.PeekValueOptional(2);
            object start = stack.PeekValueOptional(3);
            object length = stack.PeekValueOptional(4);
            stack.RemoveFrame();
            return ((ZipArchive)instance).addFile(stack.Context, filename, localname, start, length);
        }
        #endregion

        #region addFromString
        [ImplementsMethod, PhpVisible]
        public object addFromString(ScriptContext context, object localname, object contents)
        {
            string name = PHP.Core.Convert.ObjectToString(localname);

            this.m_zip.BeginUpdate();
            try
            {
                StringDataSource src = new StringDataSource(contents);
                this.m_zip.Add(src, name);
                this.m_zip.CommitUpdate();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ZipArchive::addFromString", ex.Message);
                return false;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object addFromString(object instance, PhpStack stack)
        {
            object localname = stack.PeekValue(1);
            object contents = stack.PeekValue(2);
            stack.RemoveFrame();
            return ((ZipArchive)instance).addFromString(stack.Context, localname, contents);
        }
        #endregion

        #region close
        [ImplementsMethod, PhpVisible]
        public object close(ScriptContext context)
        {
            this.m_zip.Close();
            this.m_zip = null;
            return true;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object close(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ZipArchive)instance).close(stack.Context);
        }
        #endregion

        #region deleteIndex
        [ImplementsMethod, PhpVisible]
        public object deleteIndex(ScriptContext context, object index)
        {
            int idx = Core.Convert.ObjectToInteger(index);
            this.m_zip.BeginUpdate();
            try
            {
                ZipEntry entry = this.m_zip[idx];
                this.m_zip.Delete(entry);
                this.m_zip.CommitUpdate();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ZipArchive::deleteIndex", ex.Message);
                this.m_zip.AbortUpdate();
                return false;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object deleteIndex(object instance, PhpStack stack)
        {
            object index = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ZipArchive)instance).deleteIndex(stack.Context, index);
        }
        #endregion

        #region deleteName
        [ImplementsMethod, PhpVisible]
        public object deleteName(ScriptContext context, object name)
        {
            string fname = Core.Convert.ObjectToString(name);
            this.m_zip.BeginUpdate();
            try
            {
                ZipEntry entry = this.m_zip.GetEntry(fname);
                this.m_zip.Delete(entry);
                this.m_zip.CommitUpdate();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ZipArchive::deleteName", ex.Message);
                this.m_zip.AbortUpdate();
                return false;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object deleteName(object instance, PhpStack stack)
        {
            object name = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ZipArchive)instance).deleteName(stack.Context, name);
        }
        #endregion

        #region extractTo
        [ImplementsMethod, PhpVisible]
        public object extractTo(ScriptContext context, object destination)
        {
            return this.extractTo(context, destination, null);
        }

        [ImplementsMethod, PhpVisible]
        public object extractTo(ScriptContext context, object destination, object entries)
        {
            string dest = Core.Convert.ObjectToString(destination);
            if (!System.IO.Directory.Exists(dest))
            {
                System.IO.Directory.CreateDirectory(dest);
            }

            if (entries == null)
            {
                //extract all
                for (int i = 0; i < this.m_zip.Count; i++)
                {
                    this.Extract(this.m_zip[i], dest);
                }
                return true;
            }
            if (entries is PhpArray)
            {
                PhpArray arr = (PhpArray)entries;
                foreach (var key in arr.Keys)
                {
                    string name = Core.Convert.ObjectToString(arr[key]);
                    var entry = this.m_zip.GetEntry(name);
                    this.Extract(entry, dest);
                }
                return true;
            }
            string singleName = Core.Convert.ObjectToString(entries);
            ZipEntry singleEntry = this.m_zip.GetEntry(singleName);
            this.Extract(singleEntry, dest);
            return true;
        }

        private void Extract(ZipEntry entry, string dest)
        {
            string entryFileName = entry.Name;
            string fullZipToPath = Path.Combine(dest, entryFileName);
            string directoryName = Path.GetDirectoryName(fullZipToPath);
            if (directoryName.Length > 0)
            {
                System.IO.Directory.CreateDirectory(directoryName);
            }

            using (var zis = this.m_zip.GetInputStream(entry))
            {
                byte[] buffer = new byte[4096];
                using (FileStream fs = File.Create(fullZipToPath, buffer.Length))
                {
                    StreamUtils.Copy(zis, fs, buffer);
                }
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object extractTo(object instance, PhpStack stack)
        {
            object destination = stack.PeekValue(1);
            object entries = stack.PeekValueOptional(2);
            stack.RemoveFrame();
            return ((ZipArchive)instance).extractTo(stack.Context, destination, entries);
        }
        #endregion

        #region getArchiveComment
        [ImplementsMethod, PhpVisible]
        public object getArchiveComment(ScriptContext context)
        {
            return this.getArchiveComment(context, (object)null);
        }

        [ImplementsMethod, PhpVisible]
        public object getArchiveComment(ScriptContext context, object flags)
        {
            return this.m_zip.ZipFileComment;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getArchiveComment(object instance, PhpStack stack)
        {
            object flags = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ZipArchive)instance).getArchiveComment(stack.Context, flags);
        }
        #endregion

        //string getCommentIndex ( int $index [, int $flags ] )
        #region getCommentIndex
        [ImplementsMethod, PhpVisible]
        public object getCommentIndex(ScriptContext context, object index)
        {
            return this.getCommentIndex(context, index, null);
        }

        [ImplementsMethod, PhpVisible]
        public object getCommentIndex(ScriptContext context, object index, object flags)
        {
            int idx = Core.Convert.ObjectToInteger(index);
            return this.m_zip[idx].Comment;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getCommentIndex(object instance, PhpStack stack)
        {
            object index = stack.PeekValue(1);
            object flags = stack.PeekValueOptional(2);
            stack.RemoveFrame();
            return ((ZipArchive)instance).getCommentIndex(stack.Context, index, flags);
        }
        #endregion

        //string getCommentName ( string $name [, int $flags ] )
        #region getCommentName
        [ImplementsMethod, PhpVisible]
        public object getCommentName(ScriptContext context, object name)
        {
            return this.getCommentName(context, name, null);
        }

        [ImplementsMethod, PhpVisible]
        public object getCommentName(ScriptContext context, object name, object flags)
        {
            string n = Core.Convert.ObjectToString(name);
            return this.m_zip.GetEntry(n).Comment;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getCommentName(object instance, PhpStack stack)
        {
            object name = stack.PeekValue(1);
            object flags = stack.PeekValueOptional(2);
            stack.RemoveFrame();
            return ((ZipArchive)instance).getCommentName(stack.Context, name, flags);
        }
        #endregion

        //mixed getFromIndex ( int $index [, int $length = 0 [, int $flags ]] )
        #region getFromIndex
        [ImplementsMethod, PhpVisible]
        public object getFromIndex(ScriptContext context, object index)
        {
            return this.getFromIndex(context, index, 0, null);
        }

        [ImplementsMethod, PhpVisible]
        public object getFromIndex(ScriptContext context, object index, object length)
        {
            return this.getFromIndex(context, index, length, null);
        }

        [ImplementsMethod, PhpVisible]
        public object getFromIndex(ScriptContext context, object index, object length, object flags)
        {
            int idx = Core.Convert.ObjectToInteger(index);
            long len = Core.Convert.ObjectToLongInteger(length);

            ZipEntry entry = this.m_zip[idx];
            if (len <= 0)
            {
                len = -1;
            }
            return this.getFrom(entry, len);
        }

        private byte[] getFrom(ZipEntry entry, long len)
        {
            using (System.IO.MemoryStream ms = new MemoryStream())
            {
                using (var zis = this.m_zip.GetInputStream(entry))
                {
                    byte[] buffer = new byte[4096];
                    StreamUtils.Copy(zis, ms, buffer, DummyProgressHandler, TimeSpan.FromDays(1), this, string.Empty, len);
                }
                return ms.ToArray();
            }
        }

        private static void DummyProgressHandler(object sender, ProgressEventArgs e)
        {
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getFromIndex(object instance, PhpStack stack)
        {
            object index = stack.PeekValue(1);
            object length = stack.PeekValueOptional(2);
            object flags = stack.PeekValueOptional(3);
            stack.RemoveFrame();
            return ((ZipArchive)instance).getFromIndex(stack.Context, index, length, flags);
        }
        #endregion

        //mixed getFromName ( string $name [, int $length = 0 [, int $flags ]] )
        #region getFromName
        [ImplementsMethod, PhpVisible]
        public object getFromName(ScriptContext context, object name)
        {
            return this.getFromName(context, name, 0, null);
        }

        [ImplementsMethod, PhpVisible]
        public object getFromName(ScriptContext context, object name, object length)
        {
            return this.getFromName(context, name, length, null);
        }

        [ImplementsMethod, PhpVisible]
        public object getFromName(ScriptContext context, object name, object length, object flags)
        {
            string n = Core.Convert.ObjectToString(name);
            long len = Core.Convert.ObjectToLongInteger(length);
            ZipEntry entry = this.m_zip.GetEntry(n);
            return this.getFrom(entry, len);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getFromName(object instance, PhpStack stack)
        {
            object name = stack.PeekValue(1);
            object length = stack.PeekValueOptional(2);
            object flags = stack.PeekValueOptional(3);
            stack.RemoveFrame();
            return ((ZipArchive)instance).getFromName(stack.Context, name, length, flags);
        }
        #endregion

        //string getNameIndex ( int $index [, int $flags ] )
        #region getNameIndex
        [ImplementsMethod, PhpVisible]
        public object getNameIndex(ScriptContext context, object index)
        {
            return this.getNameIndex(context, index, null);
        }

        [ImplementsMethod, PhpVisible]
        public object getNameIndex(ScriptContext context, object index, object flags)
        {
            int idx = Core.Convert.ObjectToInteger(index);
            return this.m_zip[idx].Name;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getNameIndex(object instance, PhpStack stack)
        {
            object index = stack.PeekValue(1);
            object flags = stack.PeekValueOptional(2);
            stack.RemoveFrame();
            return ((ZipArchive)instance).getNameIndex(stack.Context, index, flags);
        }
        #endregion

        //string getStatusString ( void )
        #region getStatusString
        [ImplementsMethod, PhpVisible]
        public object getStatusString(ScriptContext context)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getStatusString(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ZipArchive)instance).getStatusString(stack.Context);
        }
        #endregion

        //resource getStream ( string $name )
        #region getStream
        [ImplementsMethod, PhpVisible]
        public object getStream(ScriptContext context, object name)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getStream(object instance, PhpStack stack)
        {
            object name = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ZipArchive)instance).getStream(stack.Context, name);
        }
        #endregion

        //mixed locateName ( string $name [, int $flags ] )
        #region locateName
        [ImplementsMethod, PhpVisible]
        public object locateName(ScriptContext context, object name)
        {
            return this.locateName(context, name, null);
        }

        [ImplementsMethod, PhpVisible]
        public object locateName(ScriptContext context, object name, object flags)
        {
            string n = Core.Convert.ObjectToString(name);
            //TODO : handle flags
            return this.m_zip.FindEntry(n, true);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object locateName(object instance, PhpStack stack)
        {
            object name = stack.PeekValue(1);
            object flags = stack.PeekValueOptional(2);
            stack.RemoveFrame();
            return ((ZipArchive)instance).locateName(stack.Context, name, flags);
        }
        #endregion

        //mixed open ( string $filename [, int $flags ] )
        #region open
        [ImplementsMethod, PhpVisible]
        public object open(ScriptContext context, object filename)
        {
            return this.open(context, filename, null);
        }

        [ImplementsMethod, PhpVisible]
        public object open(ScriptContext context, object filename, object flags)
        {
            string name = Core.Convert.ObjectToString(filename);
            this.m_zip = new ZipFile(name);
            return true;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object open(object instance, PhpStack stack)
        {
            object filename = stack.PeekValue(1);
            object flags = stack.PeekValueOptional(2);
            stack.RemoveFrame();
            return ((ZipArchive)instance).open(stack.Context, filename, flags);
        }
        #endregion

        //bool renameIndex ( int $index , string $newname )
        #region renameIndex
        [ImplementsMethod, PhpVisible]
        public object renameIndex(ScriptContext context, object index, object newname)
        {
            throw new NotImplementedException();

            //int idx = Core.Convert.ObjectToInteger(index);
            //string name = Core.Convert.ObjectToString(newname);

            //this.m_zip.BeginUpdate();
            //try
            //{
            //    this.m_zip[idx].Name = name;
            //}
            //finally
            //{
            //    this.m_zip.CommitUpdate();
            //}
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object renameIndex(object instance, PhpStack stack)
        {
            object index = stack.PeekValue(1);
            object newname = stack.PeekValue(2);
            stack.RemoveFrame();
            return ((ZipArchive)instance).renameIndex(stack.Context, index, newname);
        }
        #endregion

        //bool renameName ( string $name , string $newname )
        #region renameName
        [ImplementsMethod, PhpVisible]
        public object renameName(ScriptContext context, object name, object newname)
        {
            throw new NotImplementedException();

            //string idxName = Core.Convert.ObjectToString(name);
            //string name = Core.Convert.ObjectToString(newname);

            //this.m_zip.BeginUpdate();
            //try
            //{
            // ZipEntry entry = this.m_zip.GetEntry(idxName);
            //    this.m_zip[idx].Name = name;
            //}
            //finally
            //{
            //    this.m_zip.CommitUpdate();
            //}
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object renameName(object instance, PhpStack stack)
        {
            object name = stack.PeekValue(1);
            object newname = stack.PeekValue(2);
            stack.RemoveFrame();
            return ((ZipArchive)instance).renameName(stack.Context, name, newname);
        }
        #endregion

        //mixed setArchiveComment ( string $comment )
        #region setArchiveComment
        [ImplementsMethod, PhpVisible]
        public object setArchiveComment(ScriptContext context, object comment)
        {
            string c = Core.Convert.ObjectToString(comment);
            this.m_zip.BeginUpdate();
            try
            {
                this.m_zip.SetComment(c);
                return true;
            }
            finally
            {
                this.m_zip.CommitUpdate();
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object setArchiveComment(object instance, PhpStack stack)
        {
            object comment = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ZipArchive)instance).setArchiveComment(stack.Context, comment);
        }
        #endregion

        //mixed setCommentIndex ( int $index , string $comment )
        #region setCommentIndex
        [ImplementsMethod, PhpVisible]
        public object setCommentIndex(ScriptContext context, object index, object comment)
        {
            int idx = Core.Convert.ObjectToInteger(index);
            string c = Core.Convert.ObjectToString(comment);
            this.m_zip.BeginUpdate();
            try
            {
                ZipEntry entry = this.m_zip[idx];
                entry.Comment = c;
                return true;
            }
            finally
            {
                this.m_zip.CommitUpdate();
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object setCommentIndex(object instance, PhpStack stack)
        {
            object index = stack.PeekValue(1);
            object comment = stack.PeekValue(2);
            stack.RemoveFrame();
            return ((ZipArchive)instance).setCommentIndex(stack.Context, index, comment);
        }
        #endregion

        //mixed setCommentName ( string $name , string $comment )
        #region setCommentName
        [ImplementsMethod, PhpVisible]
        public object setCommentName(ScriptContext context, object name, object comment)
        {
            string idxName = Core.Convert.ObjectToString(name);
            string c = Core.Convert.ObjectToString(comment);
            this.m_zip.BeginUpdate();
            try
            {
                ZipEntry entry = this.m_zip.GetEntry(idxName);
                entry.Comment = c;
                return true;
            }
            finally
            {
                this.m_zip.CommitUpdate();
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object setCommentName(object instance, PhpStack stack)
        {
            object name = stack.PeekValue(1);
            object comment = stack.PeekValue(2);
            stack.RemoveFrame();
            return ((ZipArchive)instance).setCommentName(stack.Context, name, comment);
        }
        #endregion

        //mixed statIndex ( int $index [, int $flags ] )
        #region statIndex
        [ImplementsMethod, PhpVisible]
        public object statIndex(ScriptContext context, object index)
        {
            return this.statIndex(context, index, null);
        }

        [ImplementsMethod, PhpVisible]
        public object statIndex(ScriptContext context, object index, object flags)
        {
            int idx = Core.Convert.ObjectToInteger(index);
            ZipEntry entry = this.m_zip[idx];
            return this.stat(entry);
        }

        private PhpArray stat(ZipEntry entry)
        {
            PhpArray arr = new PhpArray();
            arr.Add("name", entry.Name);
            arr.Add("index", entry.ZipFileIndex);
            arr.Add("size", entry.Size);
            //arr.Add("comp_method", (int)entry.CompressionMethod);
            arr.Add("crc", entry.Crc);
            return arr;

        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object statIndex(object instance, PhpStack stack)
        {
            object index = stack.PeekValue(1);
            object flags = stack.PeekValueOptional(2);
            stack.RemoveFrame();
            return ((ZipArchive)instance).statIndex(stack.Context, index, flags);
        }
        #endregion

        //mixed statName ( name $name [, int $flags ] )
        #region statName
        [ImplementsMethod, PhpVisible]
        public object statName(ScriptContext context, object name)
        {
            return this.statName(context, name, null);
        }

        [ImplementsMethod, PhpVisible]
        public object statName(ScriptContext context, object name, object flags)
        {
            string idx = Core.Convert.ObjectToString(name);
            ZipEntry entry = this.m_zip.GetEntry(idx);
            return this.stat(entry);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object statName(object instance, PhpStack stack)
        {
            object name = stack.PeekValue(1);
            object flags = stack.PeekValueOptional(2);
            stack.RemoveFrame();
            return ((ZipArchive)instance).statName(stack.Context, name, flags);
        }
        #endregion

        //mixed unchangeAll ( void )
        #region unchangeAll
        [ImplementsMethod, PhpVisible]
        public object unchangeAll(ScriptContext context)
        {
            throw new NotSupportedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object unchangeAll(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ZipArchive)instance).unchangeAll(stack.Context);
        }
        #endregion

        //mixed unchangeArchive ( void )
        #region unchangeArchive
        [ImplementsMethod, PhpVisible]
        public object unchangeArchive(ScriptContext context)
        {
            throw new NotSupportedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object unchangeArchive(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ZipArchive)instance).unchangeArchive(stack.Context);
        }
        #endregion

        //mixed unchangeIndex ( int $index )
        #region unchangeIndex
        [ImplementsMethod, PhpVisible]
        public object unchangeIndex(ScriptContext context, object index)
        {
            throw new NotSupportedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object unchangeIndex(object instance, PhpStack stack)
        {
            object index = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ZipArchive)instance).unchangeIndex(stack.Context, index);
        }
        #endregion

        //mixed unchangeName ( string $name )
        #region unchangeName
        [ImplementsMethod, PhpVisible]
        public object unchangeName(ScriptContext context, object name)
        {
            throw new NotSupportedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object unchangeName(object instance, PhpStack stack)
        {
            object name = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ZipArchive)instance).unchangeName(stack.Context, name);
        }
        #endregion
    }
}
