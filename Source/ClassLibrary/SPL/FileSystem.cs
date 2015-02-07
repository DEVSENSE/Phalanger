using System;
using PHP.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using PHP.Core.Reflection;
using System.IO;
using System.Runtime.InteropServices;

namespace PHP.Library.SPL
{
    /// <summary>
    /// The SplFileInfo class offers a high-level object oriented interface to information for an individual file.
    /// </summary>
    [ImplementsType]
    public class SplFileInfo : PhpObject
    {
        #region Fields & Properties

        /// <summary>
        /// Internal file system entry.
        /// </summary>
        protected FileSystemInfo fs_info = null;

        [PhpVisible]
        private string pathName { get { return getPathnameInternal(this.fs_info); } }

        [PhpVisible]
        private string fileName { get { return getFilenameInternal(fs_info); } }

        ///// <summary>
        ///// <see cref="_info"/> as <see cref="FileInfo"/>.
        ///// </summary>
        //protected FileInfo FileInfo { get { return this._info as FileInfo; } }

        ///// <summary>
        ///// <see cref="_info"/> as <see cref="DirectoryInfo"/>.
        ///// </summary>
        //protected DirectoryInfo DirectoryInfo { get { return this._info as DirectoryInfo; } }

        #endregion

        #region Constructor

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public SplFileInfo(ScriptContext/*!*/context, bool newInstance)
            : base(context, newInstance)
        {
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public SplFileInfo(ScriptContext/*!*/context, DTypeDesc caller)
            : base(context, caller)
        {
        }

        /// <summary>
        /// Creates a new SplFileInfo object for the <paramref name="filename"/> specified. The file does not need to exist, or be readable.
        /// </summary>
        /// <param name="context">Current <see cref="ScriptContext"/>.</param>
        /// <param name="filename">File or directory name.</param>
        [ImplementsMethod]
        public object __construct(ScriptContext/*!*/context, object filename)
        {
            // check arguments
            string filenamestr = PhpVariable.AsString(filename);
            if (filenamestr == null)
            {
                PhpException.InvalidArgumentType("filename", PhpVariable.TypeNameString);
            }
            else
            {
                // TODO                
            }

            return null;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object __construct(object instance, PhpStack stack)
        {
            var filename = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((SplFileInfo)instance).__construct(stack.Context, filename);
        }

        #endregion

        #region Methods

        //public int getATime ( void )
        //public string getBasename ([ string $suffix ] )
        //public int getCTime ( void )
        //public string getExtension ( void )
        //public SplFileInfo getFileInfo ([ string $class_name ] )

        protected static string getPathnameInternal(FileSystemInfo info)
        {
            if (info == null)
                return string.Empty;

            // handle . and ..
            var originalPath = info.ToString();
            if (originalPath.EndsWith("."))
            {
                string fname = Path.GetFileName(originalPath);
                if (fname == "." || fname == "..")
                    return originalPath;
            }

            // otherwise use FullName
            return info.FullName;
        }

        protected static string getFilenameInternal(FileSystemInfo info)
        {
            return (info != null) ? Path.GetFileName(info.ToString()) : string.Empty;   // we need original path, including "." and ".."
        }

        [ImplementsMethod]
        public virtual object/*string*/getFilename(ScriptContext context)
        {
            return getFilenameInternal(this.fs_info);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getFilename(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((SplFileInfo)instance).getFilename(stack.Context);
        }

        //public int getGroup ( void )
        //public int getInode ( void )
        //public string getLinkTarget ( void )
        //public int getMTime ( void )
        //public int getOwner ( void )

        protected static string getPathInternal(FileSystemInfo/*!*/info)
        {
            Debug.Assert(info != null);
            DirectoryInfo dir = info as DirectoryInfo ?? ((FileInfo)info).Directory;
            return dir.FullName;
        }

        [ImplementsMethod]
        public virtual object/*string*/getPath(ScriptContext context)
        {
            return getPathInternal(this.fs_info);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getPath(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((SplFileInfo)instance).getPath(stack.Context);
        }

        //public SplFileInfo getPathInfo ([ string $class_name ] )

        [ImplementsMethod]
        public virtual object/*string*/getPathname(ScriptContext context)
        {
            return getPathnameInternal(this.fs_info);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getPathname(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((SplFileInfo)instance).getPathname(stack.Context);
        }

        //public int getPerms ( void )
        //public string getRealPath ( void )

        [ImplementsMethod]
        public virtual object/*bool*/getSize(ScriptContext context)
        {
            if (this.fs_info is FileInfo)
                return ((FileInfo)this.fs_info).Length;

            return false;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getSize(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((SplFileInfo)instance).getSize(stack.Context);
        }

        //public string getType ( void )

        [ImplementsMethod]
        public virtual object/*bool*/isDir(ScriptContext context)
        {
            return this.fs_info is DirectoryInfo;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object isDir(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((SplFileInfo)instance).isDir(stack.Context);
        }

        //public bool isExecutable ( void )
        
        [ImplementsMethod]
        public virtual object/*bool*/isFile(ScriptContext context)
        {
            return this.fs_info is FileInfo;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object isFile(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((SplFileInfo)instance).isFile(stack.Context);
        }

        //public bool isLink ( void )
        //public bool isReadable ( void )
        //public bool isWritable ( void )
        //public SplFileObject openFile ([ string $open_mode = r [, bool $use_include_path = false [, resource $context = NULL ]]] )
        //public void setFileClass ([ string $class_name ] )
        //public void setInfoClass ([ string $class_name ] )
        
        [ImplementsMethod]
        public virtual object/*string*/__toString(ScriptContext context)
        {
            return this.fs_info != null ? this.fs_info.ToString() : string.Empty;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object __toString(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((SplFileInfo)instance).__toString(stack.Context);
        }

        #endregion
    }

    /// <summary>
    /// The DirectoryIterator class provides a simple interface for viewing the contents of filesystem directories.
    /// </summary>
    [ImplementsType]
    public class DirectoryIterator : SplFileInfo, Iterator, Traversable, SeekableIterator
    {
        #region Fields

        /// <summary>
        /// Internal fs enumerator.
        /// </summary>
        protected IEnumerator<FileSystemInfo> dir_enumerator = null;

        /// <summary>
        /// Internal fs enumerator item index.
        /// </summary>
        protected int dir_enumerator_key = -1;

        #endregion

        #region Constructor

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public DirectoryIterator(ScriptContext/*!*/context, bool newInstance)
            : base(context, newInstance)
        {
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public DirectoryIterator(ScriptContext/*!*/context, DTypeDesc caller)
            : base(context, caller)
        {
        }

        /// <summary>
        /// Gets enumeration of file system entries for this iterator.
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<FileSystemInfo>/*!*/EnumerateFileSystemInfos()
        {
            var dir = this.fs_info as DirectoryInfo;
            return dir != null ? dir.EnumerateFileSystemInfos() : new FileSystemInfo[0];
        }

        /// <summary>
        /// Initializes <see cref="dir_enumerator"/>.
        /// </summary>
        private void CreateEnumeratorInternal()
        {
            var dir = this.fs_info as DirectoryInfo;
            if (dir != null)
            {
                var enumerable = this.EnumerateFileSystemInfos();

                if (dir.Root != dir)
                {
                    // prepend ., ..
                    var dots = new FileSystemInfo[] { new DirectoryInfo(dir.FullName + "\\."), new DirectoryInfo(dir.FullName + "\\..") };
                    enumerable = dots.Concat(enumerable);
                }

                this.dir_enumerator = enumerable.GetEnumerator();
            }
            else
            {
                this.dir_enumerator = null;
            }
        }

        protected void ConstructDirectoryIteratorInternal(ScriptContext/*!*/context, object path)
        {
            string pathstr = PhpVariable.AsString(path);
            if (string.IsNullOrEmpty(pathstr))
            {
                RuntimeException.ThrowSplException(c => new RuntimeException(c, true), context, @"Directory name must not be empty.", 0, null);
                return;
            }

            string errmessage = null;

            try
            {
                this.fs_info = new DirectoryInfo(Path.Combine(context.WorkingDirectory, pathstr));
                this.CreateEnumeratorInternal();
            }
            catch (System.Exception ex)
            {
                errmessage = ex.Message;
            }

            if (errmessage != null)
            {
                UnexpectedValueException.ThrowSplException(c => new UnexpectedValueException(c, true), context, errmessage, 0, null);
            }
        }

        [ImplementsMethod]
        public virtual new object __construct(ScriptContext/*!*/context, object path)
        {
            ConstructDirectoryIteratorInternal(context, path);
            return null;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static new object __construct(object instance, PhpStack stack)
        {
            var path = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((DirectoryIterator)instance).__construct(stack.Context, path);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Whether current entry represents "." or "..".
        /// </summary>
        protected bool isDotInternal()
        {
            string str;
            return
                //this.dir_enumerator_key < 2 &&  // . and .. are first 2 entries
                validInternal() && (str = this.dir_enumerator.Current.ToString()).EndsWith(".", StringComparison.Ordinal) &&
                ((str = Path.GetFileName(str)) == ".." || str == ".");
        }

        [ImplementsMethod]
        public virtual object/*bool*/isDot(ScriptContext context)
        {
            return isDotInternal();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object isDot(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((DirectoryIterator)instance).isDot(stack.Context);
        }

        //public int getATime ( void )
        //public string getBasename ([ string $suffix ] )
        //public int getCTime ( void )
        //public string getExtension ( void )

        [ImplementsMethod]
        public override object/*string*/getFilename(ScriptContext context)
        {
            return validInternal() ? getFilenameInternal(this.dir_enumerator.Current) : string.Empty;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public new static object getFilename(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((DirectoryIterator)instance).getFilename(stack.Context);
        }

        //public int getGroup ( void )
        //public int getInode ( void )
        //public int getMTime ( void )
        //public int getOwner ( void )

        [ImplementsMethod]
        public override object/*string*/getPath(ScriptContext context)
        {
            return validInternal() ? getPathInternal(this.dir_enumerator.Current) : string.Empty;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public new static object getPath(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((DirectoryIterator)instance).getPath(stack.Context);
        }

        [ImplementsMethod]
        public override object/*string*/getPathname(ScriptContext context)
        {
            return validInternal() ? getPathnameInternal(this.dir_enumerator.Current) : string.Empty;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public new static object getPathname(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((DirectoryIterator)instance).getPathname(stack.Context);
        }

        //public int getPerms ( void )

        [ImplementsMethod]
        public override object/*bool*/getSize(ScriptContext context)
        {
            if (validInternal())
            {
                var info = this.dir_enumerator.Current as FileInfo;
                if (info != null)
                    return info.Length;
            }

            return false;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public new static object getSize(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((DirectoryIterator)instance).getSize(stack.Context);
        }

        //public string getType ( void )

        [ImplementsMethod]
        public override object/*bool*/isDir(ScriptContext context)
        {
            return validInternal() && this.dir_enumerator.Current is DirectoryInfo;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public new static object isDir(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((DirectoryIterator)instance).isDir(stack.Context);
        }

        //public bool isExecutable ( void )

        [ImplementsMethod]
        public virtual object/*bool*/isFile(ScriptContext context)
        {
            return validInternal() && this.dir_enumerator.Current is FileInfo;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object isFile(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((DirectoryIterator)instance).isFile(stack.Context);
        }

        //public bool isLink ( void )
        //public bool isReadable ( void )
        //public bool isWritable ( void )
        //public string __toString ( void )

        #endregion

        #region interface Iterator

        [ImplementsMethod]
        public virtual object rewind(ScriptContext context)
        {
            if (this.dir_enumerator == null || this.dir_enumerator_key >= 0)
            {
                this.dir_enumerator_key = -1;
                this.CreateEnumeratorInternal();
            }

            return this.next(context);  // move to first item
        }

        [ImplementsMethod]
        public virtual object next(ScriptContext context)
        {
            if (this.dir_enumerator == null)
                return false;

            if (this.dir_enumerator.MoveNext())
                this.dir_enumerator_key++;
            else
                this.dir_enumerator = null;

            return null;
        }

        protected bool validInternal()
        {
            return
                this.dir_enumerator_key >= 0 &&
                this.dir_enumerator != null;
        }

        [ImplementsMethod]
        public virtual object valid(ScriptContext context)
        {
            return validInternal();
        }

        [ImplementsMethod]
        public virtual object key(ScriptContext context)
        {
            return this.dir_enumerator_key;
        }

        [ImplementsMethod]
        public virtual object current(ScriptContext context)
        {
            if (validInternal())
            {
                return new DirectoryIterator(context, true)
                {
                    fs_info = this.dir_enumerator.Current
                };
            }

            return null;
        }

        #region Arglesses

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object rewind(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((DirectoryIterator)instance).rewind(stack.Context);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object next(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((DirectoryIterator)instance).next(stack.Context);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object valid(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((DirectoryIterator)instance).valid(stack.Context);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object key(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((DirectoryIterator)instance).key(stack.Context);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object current(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((DirectoryIterator)instance).current(stack.Context);
        }

        #endregion

        #endregion

        #region interface SeekableIterator

        [ImplementsMethod]
        public object seek(ScriptContext context, object position)
        {
            int i = Core.Convert.ObjectToInteger(position);

            if (i < 0)
                return this.rewind(context);

            if (this.dir_enumerator_key == -1 || this.dir_enumerator_key > i)
            {
                // newly constructed
                 this.rewind(context);   // we have to rewind and iterate to <i>
                // ->
            }
            else if (validInternal())
            {
                if (this.dir_enumerator_key == i)
                    return null;    // done

                // else dir_enumerator_key < i, we have to iterate to <i>
                // ->
            }
            else
            {
                this.rewind(context);
                // ->
            }

            // ->
            while (validInternal() && this.dir_enumerator_key < i)
                this.next(context);

            return null;
        }

        #region Arglesses

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object seek(object instance, PhpStack stack)
        {
            object position = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((DirectoryIterator)instance).seek(stack.Context, position);
        }

        #endregion

        #endregion
    }

    /// <summary>
    /// The Filesystem iterator.
    /// </summary>
    [ImplementsType]
    public class FilesystemIterator : DirectoryIterator, Iterator, Traversable, SeekableIterator
    {
        #region Constants

        public const int CURRENT_AS_PATHNAME = 32;
        public const int CURRENT_AS_FILEINFO = 0;
        public const int CURRENT_AS_SELF = 16;
        public const int CURRENT_MODE_MASK = 240;
        public const int KEY_AS_PATHNAME = 0;
        public const int KEY_AS_FILENAME = 256;
        public const int FOLLOW_SYMLINKS = 512;
        public const int KEY_MODE_MASK = 3840;
        public const int NEW_CURRENT_AND_KEY = 256;
        public const int SKIP_DOTS = 4096;
        public const int UNIX_PATHS = 8192;

        #endregion

        #region Fields

        /// <summary>
        /// Flags of the iterator. See <see cref="FilesystemIterator"/> constants.
        /// </summary>
        protected int flags = 0;

        protected bool CurrentAsPathName { get { return (this.flags & CURRENT_AS_PATHNAME) != 0; } }
        protected bool CurrentAsFileInfo { get { return (this.flags & CURRENT_MODE_MASK) == CURRENT_AS_FILEINFO; } }
        protected bool CurrentAsSelf { get { return (this.flags & CURRENT_AS_SELF) != 0; } }

        protected bool KeyAsPathName { get { return (this.flags & KEY_MODE_MASK) == KEY_AS_PATHNAME; } }
        protected bool KeyAsFileName { get { return (this.flags & KEY_AS_FILENAME) != 0; } }        

        #endregion

        #region Constructor

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public FilesystemIterator(ScriptContext/*!*/context, bool newInstance)
            : base(context, newInstance)
        {
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public FilesystemIterator(ScriptContext/*!*/context, DTypeDesc caller)
            : base(context, caller)
        {
        }

        [ImplementsMethod]
        public virtual object __construct(ScriptContext/*!*/context, object/*string*/path, [Optional]object/*int*/flags /*= FilesystemIterator.KEY_AS_PATHNAME | FilesystemIterator.CURRENT_AS_FILEINFO | FilesystemIterator.SKIP_DOTS*/ )
        {
            if (flags == Arg.Default)
                this.flags = FilesystemIterator.KEY_AS_PATHNAME | FilesystemIterator.CURRENT_AS_FILEINFO | FilesystemIterator.SKIP_DOTS;
            else
                this.flags = Core.Convert.ObjectToInteger(flags);

            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static new object __construct(object instance, PhpStack stack)
        {
            var path = stack.PeekValue(1);
            var flags = stack.PeekValueOptional(2);
            stack.RemoveFrame();
            return ((FilesystemIterator)instance).__construct(stack.Context, path, flags);
        }

        #endregion

        #region interface Iterator

        [ImplementsMethod]
        public override object key(ScriptContext context)
        {
            if (validInternal())
            {
                if (KeyAsFileName)
                    return dir_enumerator.Current.Name;
                else// if (KeyAsPathName)
                    return dir_enumerator.Current.FullName;
            }

            return false;
        }

        [ImplementsMethod]
        public override object current(ScriptContext context)
        {
            if (validInternal())
            {
                if (CurrentAsSelf)
                    return this;
                else if (CurrentAsPathName)
                    return dir_enumerator.Current.FullName;
                else //if (CurrentAsFileInfo)
                    return new FilesystemIterator(context, true)
                    {
                        fs_info = dir_enumerator.Current
                    };
            }

            return false;
        }

        #region Arglesses

        [EditorBrowsable(EditorBrowsableState.Never)]
        public new static object key(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((FilesystemIterator)instance).key(stack.Context);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public new static object current(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((FilesystemIterator)instance).current(stack.Context);
        }

        #endregion

        #endregion
    }

    /// <summary>
    /// The RecursiveDirectoryIterator provides an interface for iterating recursively over filesystem directories.
    /// </summary>
    [ImplementsType]
    public class RecursiveDirectoryIterator : FilesystemIterator, RecursiveIterator
    {
        #region Fields & Properties

        /// <summary>
        /// Sub path used internally to track nesting of the iterator.
        /// </summary>
        private string sub_path = null;

        protected string SubPathname
        {
            get
            {
                var fname = (this.dir_enumerator != null) ? this.dir_enumerator.Current.Name : null;
                if (this.sub_path != null)
                {
                    return string.Concat(this.sub_path, Path.DirectorySeparatorChar.ToString(), fname);
                }
                else
                {
                    return fname ?? string.Empty;
                }
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public RecursiveDirectoryIterator(ScriptContext/*!*/context, bool newInstance)
            : base(context, newInstance)
        {
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public RecursiveDirectoryIterator(ScriptContext/*!*/context, DTypeDesc caller)
            : base(context, caller)
        {
        }

        [ImplementsMethod]
        public virtual new object __construct(ScriptContext/*!*/context, object/*string*/path , [Optional]object/*int*/flags /*= FilesystemIterator.KEY_AS_PATHNAME | FilesystemIterator.CURRENT_AS_FILEINFO*/ )
        {
            // setup flags
            if (flags == Arg.Default)
                this.flags = FilesystemIterator.KEY_AS_PATHNAME | FilesystemIterator.CURRENT_AS_FILEINFO;
            else
                this.flags = Core.Convert.ObjectToInteger(flags);

            // init path and enumerator
            ConstructDirectoryIteratorInternal(context, path);

            return null;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static new object __construct(object instance, PhpStack stack)
        {
            var path = stack.PeekValue(1);
            var flags = stack.PeekValueOptional(2);
            stack.RemoveFrame();
            return ((RecursiveDirectoryIterator)instance).__construct(stack.Context, path, flags);
        }

        protected override IEnumerable<FileSystemInfo> EnumerateFileSystemInfos()
        {
            var dir = this.fs_info as DirectoryInfo;
            return dir != null ? dir.EnumerateFileSystemInfos("*", SearchOption.AllDirectories) : new FileSystemInfo[0];
        }

        #endregion

        #region Methods

        [ImplementsMethod]
        public virtual object/*string*/getSubPath(ScriptContext/*!*/context)
        {
            return this.sub_path ?? string.Empty;
        }

        [ImplementsMethod]
        public virtual object/*string*/getSubPathname(ScriptContext/*!*/context)
        {
            return this.SubPathname;
        }

        //public string getSubPathname ( void )
        //public bool hasChildren ([ bool $allow_links = false ] )
        //public string key ( void )
        //public void next ( void )
        //public void rewind ( void )

        #endregion

        #region RecursiveIterator

        [ImplementsMethod]
        public virtual object getChildren(ScriptContext context)
        {
            if (CurrentAsPathName)
                return this.getPath(context);

            if (validInternal())
            {
                return new RecursiveDirectoryIterator(context, true)
                {
                    fs_info = this.dir_enumerator.Current,
                    sub_path = this.SubPathname,
                };
            }

            return null;
        }

        [ImplementsMethod]
        public virtual object hasChildren(ScriptContext context/*, [Optional]object allowLinks/*=false*/)
        {
            //bool bAllowLinks = (allowLinks == null || allowLinks == Arg.Default) ? false : Core.Convert.ObjectToBoolean(allowLinks);
            return this.validInternal() && !this.isDotInternal() && this.dir_enumerator.Current is DirectoryInfo;
        }

        #region Arglesses

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getSubPath(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((RecursiveDirectoryIterator)instance).getSubPath(stack.Context);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getSubPathname(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((RecursiveDirectoryIterator)instance).getSubPathname(stack.Context);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getChildren(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((RecursiveDirectoryIterator)instance).getChildren(stack.Context);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object hasChildren(object instance, PhpStack stack)
        {
            var allowLinks = stack.PeekValueOptional(1);
            stack.RemoveFrame();
            return ((RecursiveDirectoryIterator)instance).hasChildren(stack.Context/*, allowLinks*/);
        }

        #endregion

        #endregion
    }
}
