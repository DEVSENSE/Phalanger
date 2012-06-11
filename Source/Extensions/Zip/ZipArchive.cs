using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using System.ComponentModel;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Zip;
using System.IO;

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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
        //mixed getFromName ( string $name [, int $length = 0 [, int $flags ]] )
        //string getNameIndex ( int $index [, int $flags ] )
        //string getStatusString ( void )
        //resource getStream ( string $name )
        //mixed locateName ( string $name [, int $flags ] )
        //mixed open ( string $filename [, int $flags ] )
        //bool renameIndex ( int $index , string $newname )
        //bool renameName ( string $name , string $newname )
        //mixed setArchiveComment ( string $comment )
        //mixed setCommentIndex ( int $index , string $comment )
        //mixed setCommentName ( string $name , string $comment )
        //mixed statIndex ( int $index [, int $flags ] )
        //mixed statName ( name $name [, int $flags ] )
        //mixed unchangeAll ( void )
        //mixed unchangeArchive ( void )
        //mixed unchangeIndex ( int $index )
        //mixed unchangeName ( string $name )
    }
}
