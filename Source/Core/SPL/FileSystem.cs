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

        ///// <summary>
        ///// Internal file system object.
        ///// </summary>
        //protected FileSystemInfo _info = null;

        /// <summary>
        /// Absolutized file name corresponding to this object.
        /// </summary>
        protected string FileName { get; private set; }

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
                //
                this.FileName = filenamestr;
            }

            return null;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static new object __construct(object instance, PhpStack stack)
        {
            var filename = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((SplFileInfo)instance).__construct(stack.Context, filename);
        }

        #endregion

        /* Methods */
        //public int getATime ( void )
        //public string getBasename ([ string $suffix ] )
        //public int getCTime ( void )
        //public string getExtension ( void )
        //public SplFileInfo getFileInfo ([ string $class_name ] )
        //public string getFilename ( void )
        //public int getGroup ( void )
        //public int getInode ( void )
        //public string getLinkTarget ( void )
        //public int getMTime ( void )
        //public int getOwner ( void )
        //public string getPath ( void )
        //public SplFileInfo getPathInfo ([ string $class_name ] )
        //public string getPathname ( void )
        //public int getPerms ( void )
        //public string getRealPath ( void )
        //public int getSize ( void )
        //public string getType ( void )
        //public bool isDir ( void )
        //public bool isExecutable ( void )
        //public bool isFile ( void )
        //public bool isLink ( void )
        //public bool isReadable ( void )
        //public bool isWritable ( void )
        //public SplFileObject openFile ([ string $open_mode = r [, bool $use_include_path = false [, resource $context = NULL ]]] )
        //public void setFileClass ([ string $class_name ] )
        //public void setInfoClass ([ string $class_name ] )
        //public void __toString ( void )
    }

    /// <summary>
    /// The DirectoryIterator class provides a simple interface for viewing the contents of filesystem directories.
    /// </summary>
    [ImplementsType]
    public class DirectoryIterator : SplFileInfo, Iterator, Traversable, SeekableIterator
    {
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

        [ImplementsMethod]
        public object __construct(ScriptContext/*!*/context, object path)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static new object __construct(object instance, PhpStack stack)
        {
            var path = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((DirectoryIterator)instance).__construct(stack.Context, path);
        }

        #endregion

        #region interface Iterator

        [ImplementsMethod]
        public virtual object rewind(ScriptContext context)
        {
            throw new NotImplementedException();
        }

        [ImplementsMethod]
        public virtual object next(ScriptContext context)
        {
            throw new NotImplementedException();
        }

        [ImplementsMethod]
        public virtual object valid(ScriptContext context)
        {
            throw new NotImplementedException();
        }

        [ImplementsMethod]
        public virtual object key(ScriptContext context)
        {
            throw new NotImplementedException();
        }

        [ImplementsMethod]
        public virtual object current(ScriptContext context)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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

        public const int CURRENT_AS_PATHNAME = 32 ;
        public const int CURRENT_AS_FILEINFO = 0 ;
        public const int CURRENT_AS_SELF = 16 ;
        public const int CURRENT_MODE_MASK = 240 ;
        public const int KEY_AS_PATHNAME = 0 ;
        public const int KEY_AS_FILENAME = 256 ;
        public const int FOLLOW_SYMLINKS = 512 ;
        public const int KEY_MODE_MASK = 3840 ;
        public const int NEW_CURRENT_AND_KEY = 256 ;
        public const int SKIP_DOTS = 4096 ;
        public const int UNIX_PATHS = 8192 ;

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
        public object __construct(ScriptContext/*!*/context, object/*string*/path , [Optional]object/*int*/flags /*= FilesystemIterator.KEY_AS_PATHNAME | FilesystemIterator.CURRENT_AS_FILEINFO | FilesystemIterator.SKIP_DOTS*/ )
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static new object __construct(object instance, PhpStack stack)
        {
            var path = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((FilesystemIterator)instance).__construct(stack.Context, path);
        }

        #endregion
    }

    /// <summary>
    /// The RecursiveDirectoryIterator provides an interface for iterating recursively over filesystem directories.
    /// </summary>
    [ImplementsType]
    public class RecursiveDirectoryIterator : FilesystemIterator, RecursiveIterator
    {
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
        public object __construct(ScriptContext/*!*/context, object/*string*/path , [Optional]object/*int*/flags /*= FilesystemIterator.KEY_AS_PATHNAME | FilesystemIterator.CURRENT_AS_FILEINFO*/ )
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static new object __construct(object instance, PhpStack stack)
        {
            var path = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((RecursiveDirectoryIterator)instance).__construct(stack.Context, path);
        }

        #endregion

        #region RecursiveIterator

        [ImplementsMethod]
        public virtual object getChildren(ScriptContext context)
        {
            throw new NotImplementedException();
        }

        [ImplementsMethod]
        public virtual object hasChildren(ScriptContext context)
        {
            throw new NotImplementedException();
        }

        #region Arglesses

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getChildren(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((RecursiveDirectoryIterator)instance).getChildren(stack.Context);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object hasChildren(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((RecursiveDirectoryIterator)instance).hasChildren(stack.Context);
        }

        #endregion

        #endregion
    }
}
