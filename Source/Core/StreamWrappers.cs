/*

 Copyright (c) 2004-2005 Jan Benda.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/


using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using PHP.Core;
#if SILVERLIGHT
using PHP.CoreCLR;
#endif

namespace PHP.Core
{
    #region Open-Mode decoded options
    /// <summary>
    /// Flags returned by <see cref="StreamWrapper.ParseMode"/> indicating
    /// additional information to the parsed <see cref="FileMode"/>
    /// and <see cref="FileAccess"/>.
    /// </summary>
    [Flags]
    public enum StreamAccessOptions
    {
        /// <summary>Empty (invalid) value (0).</summary>
        Empty = 0,
        /// <summary>The stream was opened for reading (1).</summary>
        Read = FileAccess.Read,
        /// <summary>The stream was opened for writing (2).</summary>
        Write = FileAccess.Write,
        /// <summary>Use text access to the stream (default is binary) (4).</summary>
        UseText = 0x04,
        /// <summary>Seek to the end of the stream is required (8).</summary>
        /// <remarks>
        /// The given mode requires "a+", which is not supported
        /// by .NET Framework; mode is reset to "r+" and a seek is required.
        /// </remarks>
        SeekEnd = 0x08,
        /// <summary>The mode starts with 'x' which requires 
        /// a Warning if the file already exists. It is not applicable
        /// to remote files (16).</summary>
        Exclusive = 0x10,
        /// <summary>This file may be searched in the include_path
        /// if requested (only the modes opening existing files) (32).</summary>
        FindFile = 0x20,
        /// <summary>When a local file is opened using tmpfile() it should be removed when closed (256).</summary>
        Temporary = 0x100,
        /// <summary>Denotes a persistent version of the stream (2048).</summary>
        Persistent = StreamOptions.Persistent
    }
    #endregion

    #region Stream opening flags
    /// <summary>
    /// Flags passed in the options argument to the <see cref="StreamWrapper.Open"/> method.
    /// </summary>
    [Flags]
    public enum StreamOptions
    {
        /// <summary>Empty option (default)</summary>
        Empty = 0,
        /// <summary>If path is relative, Wrapper will search for the resource using the include_path (1).</summary>
        UseIncludePath = 1,
        /// <summary>When this flag is set, only the file:// wrapper is considered. (2)</summary>
        IgnoreUrl = 2,
        /// <summary>Apply the <c>safe_mode</c> permissions check when opening a file (4).</summary>
        EnforceSafeMode = 4,
        /// <summary>If this flag is set, the Wrapper is responsible for raising errors using 
        /// trigger_error() during opening of the stream. If this flag is not set, she should not raise any errors (8).</summary>
        ReportErrors = 8,
        /// <summary>If you don't need to write to the stream, but really need to 
        /// be able to seek, use this flag in your options (16).</summary>
        MustSeek = 16,

        /// <summary>
        /// If you are going to end up casting the stream into a FILE* or
        /// a socket, pass this flag and the streams/wrappers will not use
        /// buffering mechanisms while reading the headers, so that HTTP wrapped 
        /// streams will work consistently.  If you omit this flag, streams will 
        /// use buffering and should end up working more optimally (32).
        /// </summary>
        WillCast = 32,
        /// <summary> This flag applies to php_stream_locate_url_wrapper (64). </summary>
        LocateWrappersOnly = 64,
        /// <summary> This flag is only used by include/require functions (128).</summary>
        OpenForInclude = 128,
        /// <summary> This flag tells streams to ONLY open urls (256).</summary>
        UseUrl = 256,
        /// <summary> This flag is used when only the headers from HTTP request are to be fetched (512).</summary>
        OnlyGetHeaders = 512,
        /// <summary>Don't apply open_basedir checks (1024).</summary>
        DisableOpenBasedir = 1024,
        /// <summary>Get (or create) a persistent version of the stream (2048).</summary>
        Persistent = 2048
    }

    /// <summary>
    /// <see cref="StreamOptions"/> relevant to the Open method.
    /// </summary>
    [Flags]
    public enum StreamOpenOptions
    {
        /// <summary>Empty option (default)</summary>
        Empty = 0,
        /// <summary>If path is relative, Wrapper will search for the resource using the include_path (1).</summary>
        UseIncludePath = StreamOptions.UseIncludePath,
        /// <summary>Apply the <c>safe_mode</c> permissions check when opening a file (4).</summary>
        EnforceSafeMode = StreamOptions.EnforceSafeMode,
        /// <summary>If this flag is set, the Wrapper is responsible for raising errors using 
        /// trigger_error() during opening of the stream. If this flag is not set, user should not raise any errors (8).</summary>
        ReportErrors = StreamOptions.ReportErrors,
        /// <summary> This flag is only used by include/require functions (128).</summary>
        OpenForInclude = StreamOptions.OpenForInclude,
        /// <summary>Don't apply open_basedir checks (1024).</summary>
        DisableOpenBasedir = StreamOptions.DisableOpenBasedir,
        /// <summary>Get (or create) a persistent version of the stream (2048).</summary>
        Persistent = StreamOptions.Persistent,
        /// <summary>When a local file is opened using tmpfile() it should be removed when closed (256).</summary>
        Temporary = StreamAccessOptions.Temporary
    }

    /// <summary>
    /// <see cref="StreamOptions"/> relevant to the Listing method.
    /// </summary>
    [Flags]
    public enum StreamListingOptions
    {
        /// <summary>Empty option (default)</summary>
        Empty = 0,
        /// <summary>Don't apply open_basedir checks (1024).</summary>
        DisableOpenBasedir = StreamOptions.DisableOpenBasedir
    }

    /// <summary>
    /// <see cref="StreamOptions"/> relevant to the Unlink method.
    /// </summary>
    [Flags]
    public enum StreamUnlinkOptions
    {
        /// <summary>Empty option (default)</summary>
        Empty = 0,
        /// <summary>Apply the <c>safe_mode</c> permissions check when opening a file (4).</summary>
        EnforceSafeMode = StreamOptions.EnforceSafeMode,
        /// <summary>If this flag is set, the Wrapper is responsible for raising errors using 
        /// trigger_error() during opening of the stream. If this flag is not set, she should not raise any errors (8).</summary>
        ReportErrors = StreamOptions.ReportErrors
    }

    /// <summary>
    /// <see cref="StreamOptions"/> relevant to the Rename method.
    /// </summary>
    public enum StreamRenameOptions
    {
        /// <summary>Empty option (default)</summary>
        Empty = 0
    }

    /// <summary>
    /// Specific options of the Stat method.
    /// </summary>
    [Flags]
    public enum StreamStatOptions
    {
        /// <summary>Empty option (default)</summary>
        Empty = 0,
        /// <summary>Stat the symbolic link itself instead of the linked file (1).</summary>
        Link = 0x1,
        /// <summary>Do not complain if the file does not exist (2).</summary>
        Quiet = 0x2,
    }

    /// <summary>
    /// Specific options of the MakeDirectory method.
    /// </summary>
    public enum StreamMakeDirectoryOptions
    {
        /// <summary>Empty option (default)</summary>
        Empty = 0,
        /// <summary>Create the whole path leading to the specified directory if necessary (1).</summary>
        Recursive = 0x1
    }

    /// <summary>
    /// <see cref="StreamOptions"/> relevant to the RemoveDirectory method.
    /// </summary>
    public enum StreamRemoveDirectoryOptions
    {
        /// <summary>Empty option (default)</summary>
        Empty = 0
    }

    /// <summary>
    /// File attribute flags used in fileperms.
    /// </summary>
    [Flags]
    public enum FileModeFlags
    {
        /// <summary>Mask for file type.</summary>
        FileTypeMask = Directory | File | Character | Pipe,
        /// <summary>Regular file.</summary>
        File = 0x8000,
        /// <summary>Directory.</summary>
        Directory = 0x4000,
        /// <summary>Character special.</summary>
        Character = 0x2000,
        /// <summary>FIFO.</summary>
        Pipe = 0x1000,
        /// <summary>Read permissions; owner, group, others.</summary>
        Read = 4 + 4 * 8 + 4 * 8 * 8,
        /// <summary>Write permissions; owner, group, others.</summary>
        Write = 2 + 2 * 8 + 2 * 8 * 8,
        /// <summary>Execute permissions; owner, group, others.</summary>
        Execute = 1 + 8 + 8 * 8,
        /// <summary>All permissions for owner, group and others.</summary>
        ReadWriteExecute = Read | Write | Execute
    }
    #endregion

    #region Abstract Stream Wrapper

    /// <summary>
    /// Abstract base class for PHP stream wrappers. Descendants define 
    /// methods implementing fopen, stat, unlink, rename, opendir, mkdir and rmdir 
    /// for different stream types.
    /// </summary>
    /// <remarks>
    /// Each script has its own copy of registeredWrappers stored in the ScriptContext.
    /// <para>
    /// PhpStream is created by a StreamWrapper on a call to fopen().
    /// Wrappers are stateless: they provide an instance of PhpStream
    /// on fopen() and an instance of DirectoryListing on opendir().
    /// </para>
    /// </remarks>
    public abstract partial class StreamWrapper : IDisposable
    {
        #region Mandatory Wrapper Operations

        /// <include file='Doc/Wrappers.xml' path='docs/method[@name="Open"]/*'/>
        public abstract PhpStream Open(ref string path, string mode, StreamOpenOptions options, StreamContext context);

        /// <include file='Doc/Wrappers.xml' path='docs/property[@name="Label"]/*'/>
        public abstract string Label { get; }

        /// <include file='Doc/Wrappers.xml' path='docs/property[@name="Scheme"]/*'/>
        public abstract string Scheme { get; }

        /// <include file='Doc/Wrappers.xml' path='docs/property[@name="IsUrl"]/*'/>
        public abstract bool IsUrl { get; }

        #endregion

        #region Optional Wrapper Operations (Warning)

        /// <include file='Doc/Wrappers.xml' path='docs/method[@name="Unlink"]/*'/>
        /// <remarks>
        /// <seealso cref="StreamUnlinkOptions"/> for the list of additional options.
        /// </remarks>
        public virtual bool Unlink(string path, StreamUnlinkOptions options, StreamContext context)
        {
            // int (*unlink)(php_stream_wrapper *wrapper, char *url, int options, php_stream_context *context TSRMLS_DC); 
            PhpException.Throw(PhpError.Warning, CoreResources.GetString("wrapper_op_unsupported", "Unlink"));
            return false;
        }

        /// <include file='Doc/Wrappers.xml' path='docs/method[@name="Listing"]/*'/>
        public virtual string[] Listing(string path, StreamListingOptions options, StreamContext context)
        {
            // php_stream *(*dir_opener)(php_stream_wrapper *wrapper, char *filename, char *mode, int options, char **opened_path, php_stream_context *context STREAMS_DC TSRMLS_DC);
            PhpException.Throw(PhpError.Warning, CoreResources.GetString("wrapper_op_unsupported", "Opendir"));
            return null;
        }

        /// <include file='Doc/Wrappers.xml' path='docs/method[@name="Rename"]/*'/>
        public virtual bool Rename(string fromPath, string toPath, StreamRenameOptions options, StreamContext context)
        {
            // int (*rename)(php_stream_wrapper *wrapper, char *url_from, char *url_to, int options, php_stream_context *context TSRMLS_DC);
            PhpException.Throw(PhpError.Warning, CoreResources.GetString("wrapper_op_unsupported", "Rename"));
            return false;
        }

        /// <include file='Doc/Wrappers.xml' path='docs/method[@name="MakeDirectory"]/*'/>
        /// <remarks><seealso cref="StreamMakeDirectoryOptions"/> for the list of additional options.</remarks>
        public virtual bool MakeDirectory(string path, int accessMode, StreamMakeDirectoryOptions options, StreamContext context)
        {
            // int (*stream_mkdir)(php_stream_wrapper *wrapper, char *url, int mode, int options, php_stream_context *context TSRMLS_DC);
            PhpException.Throw(PhpError.Warning, CoreResources.GetString("wrapper_op_unsupported", "Mkdir"));
            return false;
        }

        /// <include file='Doc/Wrappers.xml' path='docs/method[@name="RemoveDirectory"]/*'/>
        public virtual bool RemoveDirectory(string path, StreamRemoveDirectoryOptions options, StreamContext context)
        {
            // int (*stream_rmdir)(php_stream_wrapper *wrapper, char *url, int options, php_stream_context *context TSRMLS_DC);    
            PhpException.Throw(PhpError.Warning, CoreResources.GetString("wrapper_op_unsupported", "Rmdir"));
            return false;
        }

        #endregion

        #region Optional Wrapper Methods (Empty)

        /// <summary>
        /// Wrapper may be notified of closing a stream using this method.
        /// </summary>
        /// <param name="stream"></param>
        public virtual void OnClose(PhpStream stream) { }

        // int (*stream_closer)(php_stream_wrapper *wrapper, php_stream *stream TSRMLS_DC);

        /// <summary>
        /// Wrapper may override the <c>stat()</c>ing of a stream using this method.
        /// </summary>
        /// <param name="stream">The Wrapper-opened stream to be <c>stat()</c>ed.</param>
        /// <returns></returns>
        public virtual PhpArray OnStat(PhpStream stream) { return null; }

        // int (*stream_stat)(php_stream_wrapper *wrapper, php_stream *stream, php_stream_statbuf *ssb TSRMLS_DC);

        #endregion

        #region Helper methods (ParseMode, FileSystemUtils.StripPassword)

        /// <summary>
        /// Parse the <paramref name="mode"/> argument passed to <c>fopen()</c>
        /// and make the appropriate <see cref="FileMode"/> and <see cref="FileAccess"/>
        /// combination.
        /// Integrate the relevant options from <see cref="StreamOpenOptions"/> too.
        /// </summary>
        /// <param name="mode">Mode as passed to <c>fopen()</c>.</param>
        /// <param name="options">The <see cref="StreamOpenOptions"/> passed to <c>fopen()</c>.</param>
        /// <param name="fileMode">Resulting <see cref="FileMode"/> specifying opening mode.</param>
        /// <param name="fileAccess">Resulting <see cref="FileAccess"/> specifying read/write access options.</param>
        /// <param name="accessOptions">Resulting <see cref="StreamAccessOptions"/> giving 
        /// additional information to the stream opener.</param>
        /// <returns><c>true</c> if the given mode was a valid file opening mode, otherwise <c>false</c>.</returns>
        public bool ParseMode(string mode, StreamOpenOptions options, out FileMode fileMode, out FileAccess fileAccess, out StreamAccessOptions accessOptions)
        {
            accessOptions = StreamAccessOptions.Empty;
            bool forceBinary = false; // The user requested a text stream
            bool forceText = false; // Use text access to the stream (default is binary)

            // First check for relevant options in StreamOpenOptions:

            // Search for the file only if mode=='[ra]*' and use_include_path==true.
            // StreamAccessOptions findFile = 0;
            if ((options & StreamOpenOptions.UseIncludePath) > 0)
            {
                // findFile = StreamAccessOptions.FindFile;
                accessOptions |= StreamAccessOptions.FindFile;
            }

            // Copy the AutoRemove option.
            if ((options & StreamOpenOptions.Temporary) > 0)
            {
                accessOptions |= StreamAccessOptions.Temporary;
            }

            // Now do the actual mode parsing:
            fileMode = FileMode.Open;
            fileAccess = FileAccess.Write;
            if (String.IsNullOrEmpty(mode))
            {
                PhpException.Throw(PhpError.Warning, CoreResources.GetString("empty_file_mode"));
                return false;
            }

            switch (mode[0])
            {
                case 'r':
                    // flags = 0;
                    // fileMode is already set to Open
                    fileAccess = FileAccess.Read;
                    //accessOptions |= findFile;
                    break;

                case 'w':
                    // flags = O_TRUNC|O_CREAT;
                    // fileAccess is set to Write
                    fileMode = FileMode.Create;
                    //accessOptions |= findFile;
                    // EX: Note that use_include_path is applicable to all access methods.
                    // Create truncates the existing file to zero length
                    break;

                case 'a':
                    // flags = O_CREAT|O_APPEND;
                    // fileAccess is set to Write
                    fileMode = FileMode.Append;
                    //accessOptions |= findFile;
                    // Note: .NET does not support the "a+" mode, use "r+" and Seek()
                    break;

                case 'x':
                    // flags = O_CREAT|O_EXCL;
                    // fileAccess is set to Write
                    fileMode = FileMode.CreateNew;
                    accessOptions |= StreamAccessOptions.Exclusive;
                    break;

                default:
                    PhpException.Throw(PhpError.Warning, CoreResources.GetString("invalid_file_mode", mode));
                    return false;
            }

            if (mode.IndexOf('+') > -1)
            {
                // flags |= O_RDWR;
                fileAccess = FileAccess.ReadWrite;
            }

            if ((fileMode == FileMode.Append) && (fileAccess == FileAccess.ReadWrite))
            {
                // Note: .NET does not support the "a+" mode, use "r+" and Seek()
                fileMode = FileMode.OpenOrCreate;
                fileAccess = FileAccess.ReadWrite;
                accessOptions |= StreamAccessOptions.SeekEnd;
            }

            if (mode.IndexOf('b') > -1)
            {
                // flags |= O_BINARY;
                forceBinary = true;
            }
            if (mode.IndexOf('t') > -1)
            {
                // flags |= _O_TEXT;
                forceText = true;
            }

            // Exactly one of these options is required.
            if ((forceBinary && forceText) || (!forceBinary && !forceText))
            {
                LocalConfiguration config = Configuration.Local;

                // checks whether default mode is applicable:
                if (config.FileSystem.DefaultFileOpenMode == "b")
                {
                    forceBinary = true;
                }
                else if (config.FileSystem.DefaultFileOpenMode == "t")
                {
                    forceText = true;
                }
                else
                {
                    PhpException.Throw(PhpError.Warning, CoreResources.GetString("ambiguous_file_mode", mode));
                }

                // Binary mode is assumed
            }
            else if (forceText)
            {
                // Default mode is binary (unless the text mode is specified).
                accessOptions |= StreamAccessOptions.UseText;
            }

            // Store the two file-access flags into the access options too.
            accessOptions |= (StreamAccessOptions)fileAccess;

            return true;
        }

        /// <summary>
        /// Overload of <see cref="ParseMode"/> without the <c>out</c> arguments.
        /// </summary>
        /// <param name="mode">Mode as passed to <c>fopen()</c>.</param>
        /// <param name="options">The <see cref="StreamOpenOptions"/> passed to <c>fopen()</c>.</param>
        /// <param name="accessOptions">Resulting <see cref="StreamAccessOptions"/> giving 
        /// additional information to the stream opener.</param>
        /// <returns><c>true</c> if the given mode was a valid file opening mode, otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentException">If the <paramref name="mode"/> is not valid.</exception>
        internal bool ParseMode(string mode, StreamOpenOptions options, out StreamAccessOptions accessOptions)
        {
            FileMode fileMode;
            FileAccess fileAccess;

            return (ParseMode(mode, options, out fileMode, out fileAccess, out accessOptions));
        }

        /// <summary>
        /// Checks whether the supported read/write access matches the reqiured one.
        /// </summary>
        /// <param name="accessOptions">The access options specified by the user.</param>
        /// <param name="supportedAccess">The read/write access options supported by the stream.</param>
        /// <param name="path">The path given by user to report errors.</param>
        /// <returns><c>false</c> if the stream does not support any of the required modes, <c>true</c> otherwise.</returns>
        internal bool CheckOptions(StreamAccessOptions accessOptions, FileAccess supportedAccess, string path)
        {
            FileAccess requiredAccess = (FileAccess)accessOptions & FileAccess.ReadWrite;
            FileAccess faultyAccess = requiredAccess & ~supportedAccess;
            if ((faultyAccess & FileAccess.Read) > 0)
            {
                PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_open_read_unsupported",
                  FileSystemUtils.StripPassword(path)));
                return false;
            }
            else if ((faultyAccess & FileAccess.Write) > 0)
            {
                PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_open_write_unsupported",
                  FileSystemUtils.StripPassword(path)));
                return false;
            }
            return true;
        }

        #endregion

        #region Static wrapper-list handling methods

        /// <summary>
        /// Insert a new wrapper to the list of user StreamWrappers.
        /// </summary>
        /// <remarks>
        /// Each script has its own set of user StreamWrappers registered
        /// by stream_wrapper_register() stored in the ScriptContext.
        /// </remarks>
        /// <param name="protocol">The scheme portion of URLs this wrapper can handle.</param>
        /// <param name="wrapper">An instance of the corresponding StreamWrapper descendant.</param>
        /// <returns>True if succeeds, false if the scheme is already registered.</returns>
        public static bool RegisterUserWrapper(string protocol, StreamWrapper wrapper)
        {
            // Userwrappers may be initialized to null
            if (UserWrappers == null)
                CreateUserWrapperTable();

            UserWrappers.Add(protocol, wrapper);
            return true;
        }

        /// <summary>
        /// Register a new system wrapper
        /// </summary>
        /// <param name="wrapper">An instance of the corresponding StreamWrapper descendant.</param>
        /// <returns>True if succeeds, false if the scheme is already registered.</returns>
        public static bool RegisterSystemWrapper(StreamWrapper wrapper)
        {
            if (!systemStreamWrappers.ContainsKey(wrapper.Scheme))
            {
                systemStreamWrappers.Add(wrapper.Scheme, wrapper);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if a wrapper is already registered for the given scheme.
        /// </summary>
        /// <param name="scheme">The scheme.</param>
        /// <returns><c>true</c> if exists.</returns>
        public static bool Exists(string scheme)
        {
            return GetWrapperInternal(scheme) != null;
        }

        /// <summary>
        /// Retreive the corresponding StreamWrapper respectind the scheme portion 
        /// of the given path. If no scheme is specified, an instance of 
        /// FileStreamWrapper is returned.
        /// </summary>
        /// <param name="scheme">The scheme portion of an URL.</param>
        /// <param name="options">Additional <see cref="StreamOptions"/> having effect on the wrapper retreival.</param>
        /// <returns>An instance of StreamWrapper to be used to open the specified file.</returns>
        /// <exception cref="PhpException">In case when the required wrapper can not be found.</exception>
        public static StreamWrapper GetWrapper(string scheme, StreamOptions options)
        {
            StreamWrapper wrapper = GetWrapperInternal(scheme);

            if (wrapper == null)
            {
                PhpException.Throw(PhpError.Notice, CoreResources.GetString("stream_bad_wrapper", scheme));
                // Notice:  fopen(): Unable to find the wrapper "*" - did you forget to enable it when you configured PHP? in C:\Inetpub\wwwroot\php\index.php on line 23

                wrapper = GetWrapperInternal("file");
                // There should always be the FileStreamWrapper present.
            }

            // EX [GetWrapper]: check for the other StreamOptions here: for example UseUrl, IgnoreUrl

            if (!ScriptContext.CurrentContext.Config.FileSystem.AllowUrlFopen)
            {
                if (wrapper.IsUrl)
                {
                    PhpException.Throw(PhpError.Warning, CoreResources.GetString("url_fopen_disabled"));
                    return null;
                }
            }

            Debug.Assert(wrapper != null);
            return wrapper;
        }

        /// <summary>
        /// Gets the list of built-in stream wrapper schemes.
        /// </summary>
        /// <returns></returns>
        public static ICollection<string> GetSystemWrapperSchemes()
        {
            string[] keys = new string[systemStreamWrappers.Count];
            int i = 0;
            foreach (string key in systemStreamWrappers.Keys)
            {
                keys[i++] = key;
            }
            return keys;
        }

        /// <summary>
        /// Gets the list of user wrapper schemes.
        /// </summary>
        /// <returns></returns>
        public static ICollection<string> GetUserWrapperSchemes()
        {
            if (UserWrappers == null) return ArrayUtils.EmptyStrings;
            return UserWrappers.Keys;
        }

        /// <summary>
        /// Search the lists of registered StreamWrappers to find the 
        /// appropriate wrapper for a given scheme. When the scheme
        /// is empty, the FileStreamWrapper is returned.
        /// </summary>
        /// <param name="scheme">The scheme portion of an URL.</param>
        /// <returns>A StreamWrapper associated with the given scheme.</returns>
        internal static StreamWrapper GetWrapperInternal(string scheme)
        {
            // Note: FileStreamWrapper is returned both for "file" and for "".
            if (scheme == String.Empty)
                scheme = "file";

            // First search the system wrappers (always at least an empty Hashtable)
            if (SystemStreamWrappers.ContainsKey(scheme))
                return (StreamWrapper)SystemStreamWrappers[scheme];

            // Then look if the wrapper is implemented but not instantiated
            switch (scheme)
            {
                case FileStreamWrapper.scheme:
                    return (StreamWrapper)(SystemStreamWrappers[scheme] = new FileStreamWrapper());
                case HttpStreamWrapper.scheme:
                    return (StreamWrapper)(SystemStreamWrappers[scheme] = new HttpStreamWrapper());
                case InputOutputStreamWrapper.scheme:
                    return (StreamWrapper)(SystemStreamWrappers[scheme] = new InputOutputStreamWrapper());
            }

            // Next search the user wrappers (if present)
            if ((UserWrappers != null) && (UserWrappers.ContainsKey(scheme)))
            {
                return (StreamWrapper)UserWrappers[scheme];
            }

#if !SILVERLIGHT
            // And finally look inside the extensions. 
            StreamWrapper externalWrapper =
              ExternalStreamWrapper.GetExternalWrapperByScheme(scheme);

            // Returns either the found external wrapper or null.
            return externalWrapper;
#else
			// External wrappers N/A on SL
			return null;
#endif
        }

        /// <summary>
        /// Make new instance of Hashtable for the userwrappers
        /// in the ScriptContext.
        /// </summary>
        internal static void CreateUserWrapperTable()
        {
            ScriptContext script_context = ScriptContext.CurrentContext;

            Debug.Assert(script_context.UserStreamWrappers == null);
            script_context.UserStreamWrappers = new Dictionary<string, StreamWrapper>(5);
        }

        /// <summary>
        /// Table of user-registered stream wrappers.
        /// Stored as an instance variable in ScriptContext
        /// (for every script there is one, it is initialized
        /// to null - instance is created on first user-wrapper insertion).
        /// </summary>
        internal static Dictionary<string, StreamWrapper> UserWrappers
        {
            get
            {
                return ScriptContext.CurrentContext.UserStreamWrappers;
            }
        }

        /// <summary>
        /// Registered system stream wrappers for all requests.
        /// </summary>
        public static Hashtable SystemStreamWrappers { get { return systemStreamWrappers; } }

#if SILVERLIGHT
        //TODO: It should be synchronized version of Hashtable here.
        private static readonly Hashtable systemStreamWrappers = new Hashtable(5);
#else
        private static readonly Hashtable systemStreamWrappers = Hashtable.Synchronized(new Hashtable(5));
#endif



        #endregion

        #region Optional Dispose

        /// <summary>
        /// Release wrapper resources
        /// </summary>
        public virtual void Dispose() { }

        #endregion
    }

    #endregion

    #region Local Filesystem Wrapper

    /// <summary>
    /// Derived from <see cref="StreamWrapper"/>, this class provides access to 
    /// the local filesystem files.
    /// </summary>
    /// <remarks>
    /// The virtual working directory is handled by the PhpPath class in 
    /// the Class Library. The absolute path resolution (using the working diretory and the <c>include_path</c>
    /// if necessary) and open-basedir check is performed by the <see cref="PhpStream.ResolvePath"/> method.
    /// <newpara>
    /// This wrapper expects the path to be an absolute local filesystem path
    /// without the file:// scheme specifier.
    /// </newpara>
    /// </remarks>
    public partial class FileStreamWrapper : StreamWrapper
    {
        /// <summary>
        /// The protocol portion of URL handled by this wrapper.
        /// </summary>
        public const string scheme = "file";

        #region Mandatory members

        /// <include file='Doc/Wrappers.xml' path='docs/property[@name="Label"]/*'/>
        public override string Label { get { return "plainfile"; } }

        /// <include file='Doc/Wrappers.xml' path='docs/property[@name="Scheme"]/*'/>
        public override string Scheme { get { return scheme; } }

        /// <include file='Doc/Wrappers.xml' path='docs/property[@name="IsUrl"]/*'/>
        public override bool IsUrl { get { return false; } }

        #endregion
    }

    #endregion

    #region HTTP Stream Wrapper

    /// <summary>
    /// Derived from <see cref="StreamWrapper"/>, this class provides access to 
    /// remote files using the http protocol.
    /// </summary>
    public class HttpStreamWrapper : StreamWrapper
    {
        #region StreamWrapper overrides

        /// <include file='Doc/Wrappers.xml' path='docs/method[@name="Open"]/*'/>
        public override PhpStream Open(ref string path, string mode, StreamOpenOptions options, StreamContext context)
        {
            //
            // verify parameters
            //
            Debug.Assert(path != null);

            if (mode[0] != 'r')
            {
                PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_open_write_unsupported"));
                return null;
            }

            StreamAccessOptions ao;
            if (!ParseMode(mode, options, out ao) || !CheckOptions(ao, FileAccess.Read, path))
                return null;

            try
            {
                //
                // create HTTP request
                //
#if SILVERLIGHT
                /*HttpWebRequest request = new System.Windows.Browser.Net.BrowserHttpWebRequest(new Uri(path));*/
                HttpWebRequest request = WebRequest.Create(new Uri(path)) as HttpWebRequest;
                
#else
                HttpWebRequest request = WebRequest.Create(path) as HttpWebRequest;
#endif
                if (request == null)
                {
                    // Not a HTTP URL.
                    PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_url_invalid",
                      FileSystemUtils.StripPassword(path)));
                    return null;
                }

                //
                // apply stream context parameters
                //
                ApplyContext(request, context);

                //
                // get response synchronously
                //
                HttpWebResponse httpResponse = null;
                Stream httpStream = null;

#if SILVERLIGHT
                System.Threading.AutoResetEvent evt1 = new System.Threading.AutoResetEvent(false);
                request.BeginGetResponse(delegate(IAsyncResult ar)
                {
                    httpResponse = (HttpWebResponse)request.EndGetResponse(ar);
                    httpStream = httpResponse.GetResponseStream();
                    evt1.Set();
                }, null);
                evt1.WaitOne();

#else
                httpResponse = (HttpWebResponse)request.GetResponse();
                httpStream = httpResponse.GetResponseStream();
#endif

                //
                // create the PhpStream
                //
                return new NativeStream(httpStream, this, ao, path, context)
                {
                    WrapperSpecificData = CreateWrapperData(httpResponse)
                };

                // EX: check for StreamAccessOptions.Exclusive (N/A)
            }
            catch (UriFormatException)
            {
                PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_url_invalid",
                  FileSystemUtils.StripPassword(path)));
            }
            catch (NotSupportedException)
            {
                // "Any attempt is made to access the method, when the method is not overridden in a descendant class."
                PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_url_method_invalid",
                  FileSystemUtils.StripPassword(path)));
            }
            catch (Exception e)
            {
                PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_error",
                  FileSystemUtils.StripPassword(path), e.Message));
            }

            return null;
        }

        /// <summary>
        /// Init the parameters of the HttpWebRequest, use the StreamCOntext and/or default values.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        private static void ApplyContext(HttpWebRequest request, StreamContext context)
        {
            LocalConfiguration config = ScriptContext.CurrentContext.Config;

#if !SILVERLIGHT

            //
            // timeout.
            //
            object timeout = context.GetOption(scheme, "timeout");
            double dtimeout = (timeout != null) ? Convert.ObjectToDouble(timeout) : (double)config.FileSystem.DefaultSocketTimeout;
            request.ReadWriteTimeout = (int)(dtimeout * 1000);

            //
            // max_redirects
            //
            object max_redirects = context.GetOption(scheme, "max_redirects");
            int imax_redirects = (max_redirects != null) ? Convert.ObjectToInteger(max_redirects) : 20;// default: 20
            if (imax_redirects > 1)
                request.MaximumAutomaticRedirections = imax_redirects;
            else
                request.AllowAutoRedirect = false;

            //
            // protocol_version
            //
            object protocol_version = context.GetOption(scheme, "protocol_version");
            double dprotocol_version = (protocol_version != null) ? Convert.ObjectToDouble(protocol_version) : 1.0;// default: 1.0
            request.ProtocolVersion = new Version(dprotocol_version.ToString("F01", System.Globalization.CultureInfo.InvariantCulture));
#endif
            //
            // method - GET, POST, or any other HTTP method supported by the remote server.
            //
            string method = PhpVariable.AsString(context.GetOption(scheme, "method"));
            if (method != null) request.Method = method;

#if !SILVERLIGHT

            //
            // user_agent - Value to send with User-Agent: header. This value will only be used if user-agent is not specified in the header context option above.  php.ini setting: user_agent  
            //
            string agent = PhpVariable.AsString(context.GetOption(scheme, "user_agent"));
            if (agent != null)
                request.UserAgent = agent;
            else if (config.FileSystem.UserAgent != null)
                request.UserAgent = config.FileSystem.UserAgent;

#endif

            // TODO: proxy - URI specifying address of proxy server. (e.g. tcp://proxy.example.com:5100 ).    
            // TODO: request_fulluri - When set to TRUE, the entire URI will be used when constructing the request. (i.e. GET http://www.example.com/path/to/file.html HTTP/1.0). While this is a non-standard request format, some proxy servers require it.  FALSE 
            // TODO: ssl -> array(verify_peer,verify_host)
            //
            // header - Additional headers to be sent during request. Values in this option will override other values (such as User-agent:, Host:, and Authentication:).    
            //
            string header = PhpVariable.AsString(context.GetOption(scheme, "header"));
            if (header != null)
            {
                // EX: Use the individual headers, respect the system restricted-header list?
                string[] lines = header.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    int separator = line.IndexOf(':');
                    if (separator <= 0) continue;
                    string name = line.Substring(0, separator).Trim().ToLowerInvariant();
                    string value = line.Substring(separator + 1, line.Length - separator - 1).Trim();

                    switch (name)
                    {
                        case "content-type":
                            request.ContentType = value;
                            break;
                        case "content-length":
                            request.ContentLength = long.Parse(value);
                            break;
                        case "user-agent":
                            request.UserAgent = value;
                            break;
                        case "accept":
                            request.Accept = value;
                            break;
                        case "connection":
                            request.Connection = value;
                            break;
                        case "expect":
                            request.Expect = value;
                            break;
                        case "date":
                            request.Headers["Date"] =
                                DateTime.Parse(value, System.Globalization.CultureInfo.InvariantCulture)
                                .ToUniversalTime()
                                .ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                            break;
                        case "host":
                            request.Host = value;
                            break;
                        case "if-modified-since":
                            request.IfModifiedSince = System.Convert.ToDateTime(value);
                            break;
                        case "range":
                            request.AddRange(System.Convert.ToInt32(value));
                            break;
                        case "referer":
                            request.Referer = value;
                            break;
                        case "transfer-encoding":
                            request.TransferEncoding = value;
                            break;

                        default:
                            request.Headers.Add(name, value);
                            break;
                    }
                }
            }

            //
            // content - Additional data to be sent after the headers. Typically used with POST or PUT requests.    
            //
            string content = PhpVariable.AsString(context.GetOption(scheme, "content"));
            if (content != null)
            {
                // Review - encoding?
                UTF8Encoding encoding = new UTF8Encoding();
                byte[] formBytes = encoding.GetBytes(content);

                Stream body;

#if SILVERLIGHT
                    System.Threading.AutoResetEvent evt = new System.Threading.AutoResetEvent(false);
                    request.BeginGetRequestStream(delegate(IAsyncResult ar)
                    {
                        body = request.EndGetRequestStream(ar);
                        body.Write(formBytes, 0, formBytes.Length);
                        body.Close();
                        evt.Set();
                    }, null);
                    evt.WaitOne();
#else
                body = request.GetRequestStream();
                body.Write(formBytes, 0, formBytes.Length);
                body.Close();
#endif

            }
        }

        /// <summary>
        /// see stream_get_meta_data()["wrapper_data"]
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        private static object CreateWrapperData(HttpWebResponse response)
        {
            if (response == null)
                return null;

            PhpArray array = new PhpArray();
#if !SILVERLIGHT
            array.Add("HTTP/" + response.ProtocolVersion.ToString() + " " + (int)response.StatusCode + " " + response.StatusDescription);
#else
            array.Add("HTTP/1.0 " + (int)response.StatusCode + " " + response.StatusDescription); // We don't have ProtocolVersion available, just return HTTP/1.0
            //TODO: return real protocol version when we know how
#endif

            foreach (string key in response.Headers.AllKeys)
            {
                array.Add(key + ": " + response.Headers[key]);
            }

            return array;
        }

        /// <include file='Doc/Wrappers.xml' path='docs/property[@name="Label"]/*'/>
        public override string Label { get { return "HTTP"; } }

        /// <include file='Doc/Wrappers.xml' path='docs/property[@name="Scheme"]/*'/>
        public override string Scheme { get { return scheme; } }

        /// <include file='Doc/Wrappers.xml' path='docs/property[@name="IsUrl"]/*'/>
        public override bool IsUrl { get { return true; } }

        /// <summary>
        /// The protocol portion of URL handled by this wrapper.
        /// </summary>
        public const string scheme = "http";
        #endregion
    }

    #endregion

    #region Input/Output Stream Wrapper

    /// <summary>
    /// Derived from <see cref="StreamWrapper"/>, this class provides access to the PHP input/output streams.
    /// </summary>
    public partial class InputOutputStreamWrapper : StreamWrapper
    {
        #region StreamWrapper overrides

        /// <include file='Doc/Wrappers.xml' path='docs/method[@name="Open"]/*'/>
        public override PhpStream Open(ref string path, string mode, StreamOpenOptions options, StreamContext context)
        {
            Stream native = null;

            StreamAccessOptions accessOptions;
            if (!ParseMode(mode, options, out accessOptions))
                return null;

            // Do not close the system I/O streams.
            accessOptions |= StreamAccessOptions.Persistent;

            // EX: Use a cache of persistent streams (?) instead of static properties.

            FileAccess supportedAccess;
            switch (path)
            {
                // Standard IO streams are not available on Silverlight
                // stdin/stdout/input/stderr, the only supported is 'output'
#if !SILVERLIGHT
                case "php://stdin":
                    //rv = InputOutputStreamWrapper.In;
                    native = Console.OpenStandardInput();
                    supportedAccess = FileAccess.Read;
                    break;

                case "php://stdout":
                    // rv = InputOutputStreamWrapper.Out;
                    native = Console.OpenStandardOutput();
                    supportedAccess = FileAccess.Write;
                    break;

                case "php://stderr":
                    // rv = InputOutputStreamWrapper.Error;
                    native = Console.OpenStandardError();
                    supportedAccess = FileAccess.Write;
                    break;

                case "php://input":
                    // rv = InputOutputStreamWrapper.ScriptInput;
                    native = OpenScriptInput();
                    supportedAccess = FileAccess.Read;
                    break;
#endif

                case "php://output":
                    // rv = InputOutputStreamWrapper.ScriptOutput;
                    native = OpenScriptOutput();
                    supportedAccess = FileAccess.Write;
                    break;

                default:
                    const string filter_uri = "php://filter/";
                    const string resource_param = "/resource=";

                    // The only remaining option is the "php://filter"
                    if (path.StartsWith(filter_uri))
                    {
                        int pos = path.IndexOf(resource_param, filter_uri.Length - 1);
                        if (pos > 0)
                        {
                            string arguments = path.Substring(filter_uri.Length, pos - filter_uri.Length);
                            path = path.Substring(pos + resource_param.Length);
                            return OpenFiltered(path, arguments, mode, options, context);
                        }

                        // No URL resource specified.
                        PhpException.Throw(PhpError.Warning, CoreResources.GetString("url_resource_missing"));
                        return null;
                    }
                    else
                    {
                        // Unrecognized php:// stream name
                        PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_file_invalid",
                          FileSystemUtils.StripPassword(path)));
                        return null;
                    }
            }

            if (!CheckOptions(accessOptions, supportedAccess, path))
                return null;

            if (native == null)
            {
                PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_file_invalid",
                  FileSystemUtils.StripPassword(path)));
                return null;
            }

            PhpStream rv = new NativeStream(native, this, accessOptions, path, context);
            rv.IsReadBuffered = rv.IsWriteBuffered = false;
            return rv;
        }

        /// <summary>
        /// Opens a PhpStream and appends the stream filters.
        /// </summary>
        /// <param name="path">The URL resource.</param>
        /// <param name="arguments">String containig '/'-separated options.</param>
        /// <param name="mode">Original mode.</param>
        /// <param name="options">Original options.</param>
        /// <param name="context">Original context.</param>
        /// <returns></returns>
        private PhpStream OpenFiltered(string path, string arguments, string mode, StreamOpenOptions options, StreamContext context)
        {
            PhpStream rv = PhpStream.Open(path, mode, options, context);
            if (rv == null) return null;

            // Note that only the necessary read/write chain is updated (depending on the StreamAccessOptions)
            foreach (string arg in arguments.Split('/'))
            {
                if (String.Compare(arg, 0, "read=", 0, "read=".Length) == 0)
                {
                    foreach (string filter in arg.Substring("read=".Length).Split('|'))
                        PhpFilter.AddToStream(rv, filter, FilterChainOptions.Tail | FilterChainOptions.Read, null);
                }
                else if (String.Compare(arg, 0, "write=", 0, "write=".Length) == 0)
                {
                    foreach (string filter in arg.Substring("read=".Length).Split('|'))
                        PhpFilter.AddToStream(rv, filter, FilterChainOptions.Tail | FilterChainOptions.Write, null);
                }
                else
                {
                    foreach (string filter in arg.Split('|'))
                        PhpFilter.AddToStream(rv, filter, FilterChainOptions.Tail | FilterChainOptions.ReadWrite, null);
                }
            }

            return rv;
        }

        /// <include file='Doc/Wrappers.xml' path='docs/property[@name="Label"]/*'/>
        public override string Label { get { return "InputOutput"; } }

        /// <include file='Doc/Wrappers.xml' path='docs/property[@name="Scheme"]/*'/>
        public override string Scheme { get { return scheme; } }

        /// <include file='Doc/Wrappers.xml' path='docs/property[@name="IsUrl"]/*'/>
        public override bool IsUrl { get { return false; } }

#if !SILVERLIGHT
        /// <summary>
        /// Represents the script input stream (containing the raw POST data).
        /// </summary>
        /// <remarks>
        /// It is a persistent binary stream. This means that it is never closed
        /// by <c>fclose()</c> and no EOLN mapping is performed.
        /// </remarks>
        public static PhpStream ScriptInput
        {
            get
            {
                if (input == null)
                {
                    input = new NativeStream(OpenScriptInput(), null,
                      StreamAccessOptions.Read | StreamAccessOptions.Persistent, "php://input", StreamContext.Default);
                    input.IsReadBuffered = false;
                    // EX: cache this as a persistent stream
                }
                return input;
            }
        }

        [ThreadStatic]
        private static PhpStream input = null;
#endif

        /// <summary>
        /// Represents the script output stream (alias php://output).
        /// </summary>
        /// <remarks>
        /// It is a persistent binary stream. This means that it is never closed
        /// by <c>fclose()</c> and no EOLN mapping is performed.
        /// </remarks>
        public static PhpStream ScriptOutput
        {
            get
            {
                Stream currentScriptOutput = OpenScriptOutput();
                if (bytesink != currentScriptOutput)
                {
                    bytesink = currentScriptOutput;
                    if (output != null) output.Close();
                    output = new NativeStream(currentScriptOutput, null,
                      StreamAccessOptions.Write | StreamAccessOptions.Persistent, "php://output", StreamContext.Default);
                    output.IsWriteBuffered = false;
                    // EX: cache this as a persistent stream
                }
                return output;
            }
        }
#if SILVERLIGHT
		//TODO: Silverlight doesn't have ThreadStatic, it should be done in different way... now output is just a normal static field
		private static PhpStream output;
#else
        [ThreadStatic]
        private static PhpStream output = null;
#endif
        private static Stream bytesink = null;

#if !SILVERLIGHT
        /// <summary>
        /// Opens the script input (containing raw POST data).
        /// </summary>
        /// <returns>The corresponding native stream opened for reading.</returns>
        private static Stream OpenScriptInput()
        {
            System.Web.HttpContext http_context = System.Web.HttpContext.Current;

            if (http_context != null)
                return http_context.Request.InputStream;
            return Console.OpenStandardInput();
        }
#endif

        /// <summary>
        /// Opens the script output (binary output sink of the script).
        /// </summary>
        /// <returns>The corresponding native stream opened for writing.</returns>
        private static Stream OpenScriptOutput()
        {
            return ScriptContext.CurrentContext.OutputStream;
        }

        /// <summary>
        /// The protocol portion of URL handled by this wrapper.
        /// </summary>
        public const string scheme = "php";

        #endregion
    }

    #endregion

    #region FTP Stream Wrapper
    /*
    /// <summary>
  /// Derived from <see cref="StreamWrapper"/>, this class provides access to 
  /// remote files using the ftp protocol.
  /// </summary>
  public class FtpStreamWrapper : StreamWrapper
  {
  #region StreamWrapper overrides
    /// <include file='Doc/Wrappers.xml' path='docs/method[@name="Open"]/*'/>
    public override PhpStream Open(string path, string mode, StreamOpenOptions options, out string opened_path, PhpResource context)
    {
      opened_path = path;
      return null;
    }
    
      /// <include file='Doc/Wrappers.xml' path='docs/property[@name="Label"]/*'/>
    public abstract string Label { get { return "FTP"; } }

    /// <include file='Doc/Wrappers.xml' path='docs/property[@name="Scheme"]/*'/>
    public abstract string Scheme { get { return scheme; } }



    /// <include file='Doc/Wrappers.xml' path='docs/method[@name="Stat"]/*'/>
    public override StatStruct Stat(string path)
    {
      return null;
    }

    /// <include file='Doc/Wrappers.xml' path='docs/method[@name="Unlink"]/*'/>
    public override bool Unlink(string path, int options, StreamContext context)
    {
      return false;
    }

    /// <include file='Doc/Wrappers.xml' path='docs/method[@name="Listing"]/*'/>
    public override string[] Listing(string path, int options, StreamContext context)
    {
      return null;
    }

    /// <summary>
    /// The protocol portion of URL handled by this wrapper.
    /// </summary>
    public const string scheme = "ftp";
  #endregion
  }
/**/
    #endregion

    #region User-space Stream Wrapper

    /// <summary>
    /// Derived from <see cref="StreamWrapper"/>, this class is built
    /// using reflection upon a user-defined stream wrapper.
    /// A PhpStream descendant is defined upon the instance methods of 
    /// the given PHP class.
    /// </summary>
    public class UserStreamWrapper : StreamWrapper
    {
        private readonly ScriptContext/*!*/context;
        private readonly string scheme;
        private readonly Reflection.DTypeDesc/*!*/wrapperTypeDesc;
        private readonly bool isUrl;

        #region Wrapper methods invocation

        /// <summary>
        /// Lazily instantiated <see cref="wrapperTypeDesc"/>. PHP instantiates the wrapper class when used for the first time.
        /// </summary>
        protected Reflection.DObject/*!*/WrapperTypeInstance
        {
            get
            {
                if (_wrapperTypeInstance == null)
                    _wrapperTypeInstance = (Reflection.DObject)wrapperTypeDesc.New(context);

                return _wrapperTypeInstance;
            }
        }
        private Reflection.DObject _wrapperTypeInstance; // lazily instantiated wrapper type

        /// <summary>
        /// Invoke wrapper method on wrapper instance.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public object InvokeWrapperMethod(string method, params object[] args)
        {
            if (args == null || args.Length == 0)
                context.Stack.AddFrame();
            else
                context.Stack.AddFrame(args);

            return WrapperTypeInstance.InvokeMethod(method, null, context);
        }

        #endregion

        public UserStreamWrapper(ScriptContext/*!*/context, string protocol, Reflection.DTypeDesc/*!*/wrapperTypeDesc, bool isUrl)
        {
            Debug.Assert(wrapperTypeDesc != null);
            Debug.Assert(!string.IsNullOrEmpty(protocol));

            // Create a new PhpWrapper instance above the given class (reflection)
            // Note: when a member is not defined (Error): "Call to unimplemented method:
            // variablestream::stream_write is not implemented!"

            this.context = context;
            this.scheme = protocol;
            this.wrapperTypeDesc = wrapperTypeDesc;
            this.isUrl = isUrl;
        }

        #region StreamWrapper overrides

        /// <include file='Doc/Wrappers.xml' path='docs/property[@name="Label"]/*'/>
        public override string Label { get { return "user-space"; } }

        /// <include file='Doc/Wrappers.xml' path='docs/property[@name="Scheme"]/*'/>
        public override string Scheme { get { return scheme; } }

        public override bool IsUrl { get { return isUrl; } }

        public override PhpStream Open(ref string path, string mode, StreamOpenOptions options, StreamContext context)
        {
            var opened_path = new PhpReference(path);
            object result = InvokeWrapperMethod(PhpUserStream.USERSTREAM_OPEN, path, mode, (int)options, opened_path);

            if (Convert.ObjectToBoolean(result))
            {
                string opened_path_str = PhpVariable.AsString(opened_path.Value);
                if (opened_path_str != null) path = opened_path_str;

                FileMode fileMode;
                FileAccess fileAccess;
                StreamAccessOptions ao;

                if (!ParseMode(mode, options, out fileMode, out fileAccess, out ao)) return null;
                return new PhpUserStream(this, ao, path, context);
            }

            return null;
        }

        public override void OnClose(PhpStream stream)
        {
            // stream_close:
            var result = InvokeWrapperMethod(PhpUserStream.USERSTREAM_CLOSE);

            if (_wrapperTypeInstance != null) // always
            {
                _wrapperTypeInstance.Dispose();
                _wrapperTypeInstance = null;
            }

            //
            base.OnClose(stream);
        }

        public override PhpArray OnStat(PhpStream stream)
        {
            return base.OnStat(stream);
        }

        public override bool RemoveDirectory(string path, StreamRemoveDirectoryOptions options, StreamContext context)
        {
            return base.RemoveDirectory(path, options, context);
        }

        public override bool Rename(string fromPath, string toPath, StreamRenameOptions options, StreamContext context)
        {
            return base.Rename(fromPath, toPath, options, context);
        }

        public override StatStruct Stat(string path, StreamStatOptions options, StreamContext context, bool streamStat)
        {
            PhpArray arr = (streamStat ?
                this.InvokeWrapperMethod(PhpUserStream.USERSTREAM_STAT) :
                this.InvokeWrapperMethod(PhpUserStream.USERSTREAM_STATURL, path, options)) as PhpArray;

            if (arr != null)
            {
                return new StatStruct()
                {
                    st_dev = (uint)Convert.ObjectToLongInteger(arr["dev"]),
                    st_ino = (ushort)Convert.ObjectToLongInteger(arr["ino"]),
                    st_mode = (ushort)Convert.ObjectToLongInteger(arr["mode"]),
                    st_nlink = (short)Convert.ObjectToLongInteger(arr["nlink"]),
                    st_uid = (short)Convert.ObjectToLongInteger(arr["uid"]),
                    st_gid = (short)Convert.ObjectToLongInteger(arr["gid"]),
                    st_rdev = (uint)Convert.ObjectToLongInteger(arr["rdev"]),
                    st_size = (int)Convert.ObjectToLongInteger(arr["size"]),

                    st_atime = (long)Convert.ObjectToLongInteger(arr["atime"]),
                    st_mtime = (long)Convert.ObjectToLongInteger(arr["mtime"]),
                    st_ctime = (long)Convert.ObjectToLongInteger(arr["ctime"]),

                    //st_blksize = (long)Convert.ObjectToLongInteger(arr["blksize"]),
                    //st_blocks = (long)Convert.ObjectToLongInteger(arr["blocks"]),
                };
            }

            return new StatStruct();
        }

        public override bool Unlink(string path, StreamUnlinkOptions options, StreamContext context)
        {
            return Convert.ObjectToBoolean(this.InvokeWrapperMethod(PhpUserStream.USERSTREAM_UNLINK, path));
        }

        public override string[] Listing(string path, StreamListingOptions options, StreamContext context)
        {
            return base.Listing(path, options, context);
        }

        public override bool MakeDirectory(string path, int accessMode, StreamMakeDirectoryOptions options, StreamContext context)
        {
            return base.MakeDirectory(path, accessMode, options, context);
        }

        #endregion
    }

    #endregion
}
