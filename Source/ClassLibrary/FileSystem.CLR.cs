/*

 Copyright (c) 2004-2006 Jan Benda and Tomas Matousek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

	TODO:
		- Added support for LOCK_EX flag for file_put_contents(). (PHP 5.1.0)
		- Added lchown() and lchgrp() to change user/group ownership of symlinks. (PHP 5.1.3) 
		- Fixed safe_mode check for source argument of the copy() function. (PHP 5.1.3) 
*/

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Runtime.CompilerServices;

using PHP.Core;
using System.Security.AccessControl;
using System.Security.Principal;

#if SILVERLIGHT
using PHP.CoreCLR;
#endif

namespace PHP.Library
{
	/// <summary>
	/// Provides PHP I/O operations using the set of StreamWrappers.
	/// </summary>
	/// <threadsafety static="true"/>
	public static partial class PhpFile
	{
		#region Constructors and Thread Static Stuff

		/// <summary>The most recent <c>stat()</c> result (<c>stat()</c> of the <see cref="statCacheUrl"/> file).</summary>
		[ThreadStatic]
		private static StatStruct statCache;

		/// <summary>The absolute path of the last <c>stat()</c> operation.</summary>
		[ThreadStatic]
		private static string statCacheUrl = null;

		private static void Clear()
		{
			statCache = new StatStruct();
			statCacheUrl = null;
		}

		#endregion

		#region Stat Basics (BuildStatArray, StatInternal, lstat, stat, fstat, clearstatcache; exists, touch)

		/// <summary>
		/// Creates a <see cref="PhpArray"/> from the <see cref="StatStruct"/> 
		/// copying the structure members into the array.
		/// </summary>
		/// <remarks>
		/// The resulting PhpArray has following associative keys in the given order
		/// (each has a corresponding numeric index starting from zero).
		/// As of ordering, first come all the numeric indexes and then come all the associative indexes.
		/// <list type="table">
		/// <item><term>dev</term><term>Drive number of the disk containing the file (same as st_rdev). </term></item>
		/// <item><term>ino</term><term>Number of the information node (the inode) for the file (UNIX-specific). On UNIX file systems, the inode describes the file date and time stamps, permissions, and content. When files are hard-linked to one another, they share the same inode. The inode, and therefore st_ino, has no meaning in the FAT, HPFS, or NTFS file systems. </term></item>
		/// <item><term>mode</term><term>Bit mask for file-mode information. The _S_IFDIR bit is set if path specifies a directory; the _S_IFREG bit is set if path specifies an ordinary file or a device. User read/write bits are set according to the file's permission mode; user execute bits are set according to the path extension. </term></item>
		/// <item><term>nlink</term><term>Always 1 on non-NTFS file systems. </term></item>
		/// <item><term>uid</term><term>Numeric identifier of user who owns file (UNIX-specific). This field will always be zero on Windows NT systems. A redirected file is classified as a Windows NT file. </term></item>
		/// <item><term>gid</term><term>Numeric identifier of group that owns file (UNIX-specific) This field will always be zero on Windows NT systems. A redirected file is classified as a Windows NT file. </term></item>
		/// <item><term>rdev</term><term>Drive number of the disk containing the file (same as st_dev). </term></item>
		/// <item><term>size</term><term>Size of the file in bytes; a 64-bit integer for _stati64 and _wstati64 </term></item>
		/// <item><term>atime</term><term>Time of last access of file. Valid on NTFS but not on FAT formatted disk drives. Gives the same </term></item>
		/// <item><term>mtime</term><term>Time of last modification of file. </term></item>
		/// <item><term>ctime</term><term>Time of creation of file. Valid on NTFS but not on FAT formatted disk drives. </term></item>
		/// <item><term>blksize</term><term>Always -1 on non-NTFS file systems. </term></item>
		/// <item><term>blocks</term><term>Always -1 on non-NTFS file systems. </term></item>
		/// </list>
		/// </remarks>
		/// <param name="stat">A <see cref="StatStruct"/> returned by a stream wrapper.</param>
		/// <returns>A <see cref="PhpArray"/> in the format of the <c>stat()</c> PHP function.</returns>
		internal static PhpArray BuildStatArray(StatStruct stat)
		{
			// An unitialized StatStruct means an error.
			if (stat.st_ctime == 0) return null;
			PhpArray result = new PhpArray(13, 13);

			result.Add(0, (int)stat.st_dev);         // device number 
			result.Add(1, (int)stat.st_ino);         // inode number 
			result.Add(2, (int)stat.st_mode);        // inode protection mode 
			result.Add(3, (int)stat.st_nlink);       // number of links 
			result.Add(4, (int)stat.st_uid);         // userid of owner 
			result.Add(5, (int)stat.st_gid);         // groupid of owner 
			result.Add(6, (int)stat.st_rdev);        // device type, if inode device -1
			result.Add(7, (int)stat.st_size);        // size in bytes (reset by caller)
			result.Add(8, unchecked((int)stat.st_atime));       // time of last access (unix timestamp) 
			result.Add(9, unchecked((int)stat.st_mtime));       // time of last modification (unix timestamp) 
			result.Add(10, unchecked((int)stat.st_ctime));      // time of last change (unix timestamp) 
			result.Add(11, (int)-1);                 // blocksize of filesystem IO (-1)
			result.Add(12, (int)-1);                 // number of blocks allocated  (-1)

			result.Add("dev", (int)stat.st_dev);     // device number 
			result.Add("ino", (int)stat.st_ino);     // inode number 
			result.Add("mode", (int)stat.st_mode);   // inode protection mode 
			result.Add("nlink", (int)stat.st_nlink); // number of links 
			result.Add("uid", (int)stat.st_uid);     // userid of owner 
			result.Add("gid", (int)stat.st_gid);     // groupid of owner 
			result.Add("rdev", (int)stat.st_rdev);   // device type, if inode device -1
			result.Add("size", (int)stat.st_size);   // size in bytes (reset by caller)
			result.Add("atime", unchecked((int)stat.st_atime)); // time of last access (unix timestamp) 
			result.Add("mtime", unchecked((int)stat.st_mtime)); // time of last modification (unix timestamp) 
			result.Add("ctime", unchecked((int)stat.st_ctime)); // time of last change (unix timestamp) 
			result.Add("blksize", (int)-1);          // blocksize of filesystem IO (-1)
			result.Add("blocks", (int)-1);           // number of blocks allocated  (-1)

			return result;
		}

        /// <summary>
        /// Check StatInternal input parameters.
        /// </summary>
        /// <param name="path">The path passed to stat().</param>
        /// <param name="quiet">Wheter to suppress warning message if argument is empty.</param>
        /// <param name="wrapper">If passed, it will contain valid StremWrapper to the given <paramref name="path"/>.</param>
        /// <returns>True if check passed.</returns>
        private static bool StatInternalCheck(ref string path, bool quiet, out StreamWrapper wrapper)
        {
            wrapper = null;
            
            if (String.IsNullOrEmpty(path))
            {
                PhpException.Throw(PhpError.Warning, LibResources.GetString("arg:empty", "path"));
                return false;
            }

            CheckAccessOptions options = CheckAccessOptions.Empty;
            if (quiet) options |= CheckAccessOptions.Quiet;
            if (!PhpStream.ResolvePath(ref path, out wrapper, CheckAccessMode.FileOrDirectory, options))
                return false;
            
            // check passed
            return true;
        }

        /// <summary>
        /// Check the cache for given <paramref name="path"/>.
        /// </summary>
        /// <param name="path">Path to lookup in the cache.</param>
        /// <param name="url">Url of <paramref name="path"/>.</param>
        /// <returns>True if given <paramref name="path"/> is in the cache currently.</returns>
        private static bool StatInternalTryCache(string path, out string url)
        {
            // Try to hit the cache first
            url = PhpPath.GetUrl(path);
            return (url == statCacheUrl);
        }

        /// <summary>
        /// Stat the path coming from ResolvePath (file:// wrapper expects path w/o the scheme).
        /// </summary>
        /// <param name="path"></param>
        /// <param name="url"></param>
        /// <param name="wrapper"></param>
        /// <param name="quiet"></param>
        /// <returns>True if stat was successfuly added into cache.</returns>
        private static bool StatInternalStat(string path, string url, StreamWrapper wrapper, bool quiet)
        {
            StatStruct stat = wrapper.Stat(path, quiet ? StreamStatOptions.Quiet : StreamStatOptions.Empty, StreamContext.Default, false);
            if (stat.st_size >= 0)
            {
                statCacheUrl = url;
                statCache = stat;
                return true;
            }
            else
                return false;
        }

		/// <summary>
		/// Stat the given file or directory using stream-wrappers and return the stat structure
		/// using the stat-cache for repetitive calls.
		/// </summary>
		/// <param name="path">The path (absolute or relative or an URL) to the file or directory to stat.</param>
		/// <param name="quiet"><c>true</c> to suppress the display of error messages (for example for <c>exists()</c>).</param>
		/// <returns><c>true</c> if the <see cref="statCache"/> contains a valid 
		/// stat structure for the given URL, <c>false</c> on an error.</returns>
		internal static bool StatInternal(string path, bool quiet)
		{
            StreamWrapper wrapper;
            
            if (StatInternalCheck(ref path, quiet, out wrapper))
            {
                string url;
                if (StatInternalTryCache(path, out url))
                    return true;

                return StatInternalStat(path, url, wrapper, quiet);
            }

			return false;
		}

		/// <summary>
		/// Gives information about a file or symbolic link. 
		/// </summary>
		/// <remarks>
		/// Behaves just like a <see cref="Stat"/> since there are no symbolic links on Windows.
		/// </remarks>
		/// <param name="path">Path to a file to <c>stat</c>.</param>
		/// <returns>A <see cref="PhpArray"/> containing the stat information.</returns>
		[ImplementsFunction("lstat")]
		[return: CastToFalse]
		public static PhpArray LinkStat(string path)
		{
			return Stat(path);
		}

		/// <summary>
		/// Gives information about a file.
		/// </summary>
		/// <param name="path">Path to a file to <c>stat</c>.</param>
		/// <returns>A <see cref="PhpArray"/> containing the stat information.</returns>
		[ImplementsFunction("stat")]
		[return: CastToFalse]
		public static PhpArray Stat(string path)
		{
			if (StatInternal(path, false))
			{
				return BuildStatArray(statCache);
			}
			return null;
		}


		/// <summary>
		/// Gets information about a file using an open file pointer.
		/// </summary>
		/// <param name="handle"></param>
		/// <returns></returns>
		[ImplementsFunction("fstat")]
		public static PhpArray Stat(PhpResource handle)
		{
			// Note: no cache here.
			PhpStream stream = PhpStream.GetValid(handle);
			if (stream == null) return null;
			return BuildStatArray(stream.Stat());
		}

		/// <summary>
		/// Remove all the cached <c>stat()</c> entries.
        /// Function has no return value.
		/// </summary>
		/// <remarks>
		/// The intermediary <see cref="StatStruct"/> used in the last stat-related function call
		/// is cached together with the absolute path or URL to the resource.
		/// The next call to one of the following functions will use the cached
		/// structure unless <see cref="ClearStatCache"/> is called.
		/// <para>
		/// The affected functions are:
		/// <c>stat()</c>, <c>lstat()</c>, <c>file_exists()</c>, <c>is_writable()</c>, <c>is_readable()</c>, <c>
		/// is_executable()</c>, <c>is_file()</c>, <c>is_dir()</c>, <c>is_link()</c>, <c>filectime()</c>, <c>
		/// fileatime()</c>, <c>filemtime()</c>, <c>fileinode()</c>, <c>filegroup()</c>, <c>fileowner()</c>, <c>
		/// filesize()</c>, <c>filetype()</c> <c>and fileperms()</c>. 
		/// </para>
		/// </remarks>
		[ImplementsFunction("clearstatcache")]
		public static void ClearStatCache()
		{
            Clear();
		}

        [ImplementsFunction("clearstatcache")]
        public static void ClearStatCache( bool clear_realpath_cache )
        {
            Clear();   // note: arguments ignored, Phalanger does not cache a lot, caches of .NET and OS are used transparently
        }
        [ImplementsFunction("clearstatcache")]
        public static void ClearStatCache(bool clear_realpath_cache,  string filename  )
        {
            if (!string.IsNullOrEmpty(filename) && !clear_realpath_cache)
            {
                // TODO: throw warning
            }

            Clear();   // note: arguments ignored, Phalanger does not cache a lot, caches of .NET and OS are used transparently
        }

		/// <summary>
		/// Checks whether a file exists
		/// </summary>
		/// <param name="path">The file to be checked.</param>
		/// <returns>True if the file exists.</returns>
		[ImplementsFunction("file_exists")]
		public static bool Exists(string path)
		{
			if (String.IsNullOrEmpty(path)) return false;

            StreamWrapper wrapper;
            if (StatInternalCheck(ref path, true, out wrapper))
            {
                string url;
                if (StatInternalTryCache(path, out url))
                    return true;

                // we can't just call {Directory|File}.Exists since we have to throw warnings
                // also we are not calling full stat(), it is slow
                return FileStreamWrapper.HandleNewFileSystemInfo(false, path, (p) =>
                    new FileInfo(p).Exists || new DirectoryInfo(p).Exists);
            }

            return false;
		}

		/// <summary>
		/// Sets access and modification time of file.
		/// </summary>
		/// <param name="path">The file to touch.</param>
		/// <returns><c>true</c> on success, <c>false</c> on failure.</returns>
		[ImplementsFunction("touch")]
		public static bool Touch(string path)
		{
			return Touch(path, 0, 0);
		}

		/// <summary>
		/// Sets access and modification time of file.
		/// </summary>
		/// <param name="path">The file to touch.</param>
		/// <param name="mtime">The new modification time.</param>
		/// <returns><c>true</c> on success, <c>false</c> on failure.</returns>
		[ImplementsFunction("touch")]
		public static bool Touch(string path, int mtime)
		{
			return Touch(path, mtime, 0);
		}

		/// <summary>
		/// Sets access and modification time of file.
		/// </summary>
		/// <remarks>
		/// Attempts to set the access and modification time of the file named by 
		/// path to the value given by time. If the option time is not given, 
		/// uses the present time. If the third option atime is present, the access 
		/// time of the given path is set to the value of atime. Note that 
		/// the access time is always modified, regardless of the number of parameters. 
		/// If the file does not exist, it is created. 
		/// </remarks>
		/// <param name="path">The file to touch.</param>
		/// <param name="mtime">The new modification time.</param>
		/// <param name="atime">The desired access time.</param>
		/// <returns><c>true</c> on success, <c>false</c> on failure.</returns>
		[ImplementsFunction("touch")]
		public static bool Touch(string path, int mtime, int atime)
		{
			// Create the file if it does not already exist (performs all checks).
			//PhpStream file = (PhpStream)Open(path, "ab");
			//if (file == null) return false;
			StreamWrapper wrapper;
			if (!PhpStream.ResolvePath(ref path, out wrapper, CheckAccessMode.FileMayExist, CheckAccessOptions.Quiet))
				return false;

			if (!Exists(path))
			{
				// Open and close => create new.
				Close(wrapper.Open(ref path, "wb", StreamOpenOptions.Empty, StreamContext.Default));
			}

			DateTime access_time = (atime > 0) ? DateTimeUtils.UnixTimeStampToUtc(atime) : DateTime.UtcNow;
			DateTime modification_time = (mtime > 0) ? DateTimeUtils.UnixTimeStampToUtc(mtime) : DateTime.UtcNow;

			access_time -= DateTimeUtils.GetDaylightTimeDifference(access_time, DateTime.UtcNow);
			modification_time -= DateTimeUtils.GetDaylightTimeDifference(modification_time, DateTime.UtcNow);

			try
			{
				File.SetLastWriteTimeUtc(path, modification_time);
				File.SetLastAccessTimeUtc(path, access_time);

				// Clear the cached stat values
				ClearStatCache();
				return true;
			}
			catch (UnauthorizedAccessException)
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_file_access_denied",
					FileSystemUtils.StripPassword(path)));
			}
			catch (System.Exception e)
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_error",
					FileSystemUtils.StripPassword(path), e.Message));
			}
			return false;
		}

        #endregion

		#region Disk Stats (disk_free_space/diskfreespace, disk_total_space)

		/// <summary>
		/// Given a string containing a directory, this function will return 
		/// the number of free bytes on the corresponding filesystem or disk partition. 
		/// </summary>
		/// <param name="directory">The directory specifying the filesystem or disk partition to be examined.</param>
		/// <returns>Nuber of free bytes available or <c>FALSE</c> on an error.</returns>
		[ImplementsFunction("disk_free_space")]
		public static object GetDiskFreeSpace(string directory)
		{
			return GetDiskFreeSpaceInternal(directory, false);
		}

		/// <summary>
		/// Given a string containing a directory, this function will return 
		/// the number of free bytes on the corresponding filesystem or disk partition. 
		/// </summary>
		/// <param name="directory">The directory specifying the filesystem or disk partition to be examined.</param>
		/// <returns>Nuber of free bytes available or <c>FALSE</c> on an error.</returns>
		[ImplementsFunction("diskfreespace")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static object GetFreeSpace(string directory)
		{
			return GetDiskFreeSpaceInternal(directory, false);
		}

		/// <summary>
		/// Given a string containing a directory, this function will return 
		/// the number of total bytes on the corresponding filesystem or disk partition. 
		/// </summary>
		/// <param name="directory">The directory specifying the filesystem or disk partition to be examined.</param>
		/// <returns>Total nuber of bytes on the specified filesystem or disk partition or <c>FALSE</c> on an error.</returns>
		[ImplementsFunction("disk_total_space")]
		public static object GetDiskTotalSpace(string directory)
		{
			return GetDiskFreeSpaceInternal(directory, true);
		}

		/// <summary>
		/// Given a string containing a directory, this function will return 
		/// the number of bytes (total or free depending on <paramref name="total"/> 
		/// on the corresponding filesystem or disk partition. 
		/// </summary>
		/// <param name="directory">The directory specifying the filesystem or disk partition to be examined.</param>
		/// <param name="total"><c>true</c> to return total space available, <c>false</c> to return free space only.</param>
		/// <returns>Nuber of bytes available or <c>FALSE</c> on an error.</returns>
		internal static object GetDiskFreeSpaceInternal(string directory, bool total)
		{
			long user_free_bytes, total_bytes, total_free_bytes;
			if (!FileSystemUtils.GetDiskFreeSpace(directory, out user_free_bytes, out total_bytes, out total_free_bytes))
			{
                // TODO: Warning: disk_free_space(): SystÚm nem¨×e nalÚzt uvedenou cestu.\n on line....
				return false;
			}
			else
			{
				return total ? (double)total_bytes : (double)user_free_bytes;
			}
		}

		#endregion

		#region Stat Values (file* functions)

		/// <summary>
		/// Gets file type.
		/// </summary>
		/// <remarks>
		/// Returns the type of the file. Possible values are <c>fifo</c>, <c>char</c>, 
		/// <c>dir</c>, <c>block</c>, <c>link</c>, <c>file</c>, and <c>unknown</c>. 
		/// Returns <B>null</B> if an error occurs. 
		/// </remarks>
		/// <param name="path"></param>
		/// <returns></returns>
		[ImplementsFunction("filetype")]
		[return: CastToFalse]
		public static string GetType(string path)
		{
			bool ok = StatInternal(path, false);
			if (!ok) return null;
			FileModeFlags mode = (FileModeFlags)statCache.st_mode & FileModeFlags.FileTypeMask;

			switch (mode)
			{
				case FileModeFlags.Directory:
					return "dir";

				case FileModeFlags.File:
					return "file";

				default:
					PhpException.Throw(PhpError.Notice, LibResources.GetString("unknown_file_type"));
					return "unknown";
			}
		}

		/// <summary>
		/// Returns the time the file was last accessed, or <c>false</c> in case 
		/// of an error. The time is returned as a Unix timestamp.
		/// </summary>
		/// <remarks>
		/// The results of this call are cached.
		/// See <see cref="ClearStatCache"/> for more details.
		/// </remarks>
		/// <param name="path">The file to be probed.</param>
		/// <returns>The file access time or -1 in case of failure.</returns>
		[ImplementsFunction("fileatime")]
		[return: CastToFalse]
		public static int GetAccessTime(string path)
		{
			bool ok = StatInternal(path, false);
			if (!ok) return -1;
			return unchecked((int)statCache.st_atime);
		}

		/// <summary>
		/// Returns the time the file was created, or <c>false</c> in case 
		/// of an error. The time is returned as a Unix timestamp.
		/// </summary>
		/// <remarks>
		/// The results of this call are cached.
		/// See <see cref="ClearStatCache"/> for more details.
		/// <para>
		/// On UNIX systems the <c>filectime</c> value represents 
		/// the last change of the I-node.
		/// </para>
		/// </remarks>
		/// <param name="path">The file to be <c>stat()</c>ed.</param>
		/// <returns>The file size or -1 in case of failure.</returns>
		[ImplementsFunction("filectime")]
		[return: CastToFalse]
		public static int GetCreationTime(string path)
		{
			bool ok = StatInternal(path, false);
			if (!ok) return -1;
			return unchecked((int)statCache.st_ctime);
		}

		/// <summary>
		/// Gets file group.
		/// </summary>
		/// <remarks>
		/// Always returns <c>0</c> for Windows filesystem files.
		/// </remarks>
		/// <param name="path">The file to be <c>stat()</c>ed.</param>
		/// <returns>The file size or <c>false</c> in case of failure.</returns>
		[ImplementsFunction("filegroup")]
		[return: CastToFalse]
		public static int GetGroup(string path)
		{
			bool ok = StatInternal(path, false);
			if (!ok) return -1;
			return (int)statCache.st_gid;
		}

		/// <summary>
		/// Gets file inode.
		/// </summary>
		/// <remarks>
		/// Always returns <c>0</c> for Windows filesystem files.
		/// </remarks>
		/// <param name="path">The file to be <c>stat()</c>ed.</param>
		/// <returns>The file size or <c>false</c> in case of failure.</returns>
		[ImplementsFunction("fileinode")]
		[return: CastToFalse]
		public static int GetINode(string path)
		{
			bool ok = StatInternal(path, false);
			if (!ok) return -1;
			return (int)statCache.st_ino;
		}

		/// <summary>
		/// Returns the time the file was last modified, or <c>false</c> in case 
		/// of an error. The time is returned as a Unix timestamp.
		/// </summary>
		/// <remarks>
		/// The results of this call are cached.
		/// See <see cref="ClearStatCache"/> for more details.
		/// </remarks>
		/// <param name="path">The file to be <c>stat()</c>ed.</param>
		/// <returns>The file modification time or <c>false</c> in case of failure.</returns>
		[ImplementsFunction("filemtime")]
		[return: CastToFalse]
		public static int GetModificationTime(string path)
		{
			bool ok = StatInternal(path, false);
			if (!ok) return -1;
			return unchecked((int)statCache.st_mtime);
		}

		/// <summary>
		/// Gets file owner.
		/// </summary>
		/// <param name="path">The file to be <c>stat()</c>ed.</param>
		/// <returns>The user ID of the owner of the file, or <c>false</c> in case of an error. </returns>
		[ImplementsFunction("fileowner")]
		[return: CastToFalse]
		public static int GetOwner(string path)
		{
			bool ok = StatInternal(path, false);
			if (!ok) return -1;
			return (int)statCache.st_uid;
		}

		/// <summary>
		/// Gets file permissions.
		/// </summary>
		/// <param name="path">The file to be <c>stat()</c>ed.</param>
		/// <returns>Returns the permissions on the file, or <c>false</c> in case of an error. </returns>
		[ImplementsFunction("fileperms")]
		[return: CastToFalse]
		public static int GetPermissions(string path)
		{
			bool ok = StatInternal(path, false);
			if (!ok) return -1;
			return (int)statCache.st_mode;
		}

		/// <summary>
		/// Gets the file size.
		/// </summary>
		/// <remarks>
		/// The results of this call are cached.
		/// See <see cref="ClearStatCache"/> for more details.
		/// </remarks>
		/// <param name="path">The file to be probed.</param>
		/// <returns>The file size or false in case of failure.</returns>
		[ImplementsFunction("filesize")]
		[return: CastToFalse]
		public static int GetSize(string path)
		{
            StreamWrapper wrapper;

            if (StatInternalCheck(ref path, false, out wrapper))
            {
                string url;
                if (StatInternalTryCache(path, out url))
                    return statCache.st_size;

                // we are not calling full stat(), it is slow
                return FileStreamWrapper.HandleNewFileSystemInfo(-1, path, (p) => FileSystemUtils.FileSize(new FileInfo(p)));
            }

            return -1;
            
            //bool ok = StatInternal(path, false);
            //if (!ok) return -1;
            //return statCache.st_size;
		}

		#endregion

		#region Stat Flags (is_* functions)
		/// <summary>
		/// Tells whether the path is a directory.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		[ImplementsFunction("is_dir")]
		public static bool IsDirectory(string path)
		{
            StreamWrapper wrapper;

            if (!string.IsNullOrEmpty(path) && StatInternalCheck(ref path, false, out wrapper)) // do not throw warning if path is null or empty
            {
                string url;
                if (StatInternalTryCache(path, out url))
                    return ((FileModeFlags)statCache.st_mode & FileModeFlags.Directory) != 0;

                // we can't just call Directory.Exists since we have to throw warnings
                // also we are not calling full stat(), it is slow
                return FileStreamWrapper.HandleNewFileSystemInfo(false, path, (p) => new DirectoryInfo(p).Exists);
            }

            return false;

            //bool ok = !string.IsNullOrEmpty(path) && StatInternal(path, false); // do not throw warning if path is null or empty
            //if (!ok) return false;

            //return ((FileModeFlags)statCache.st_mode & FileModeFlags.Directory) > 0;
		}

		/// <summary>
		/// Tells whether the path is executable.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		[ImplementsFunction("is_executable")]
		public static bool IsExecutable(string path)
		{
			bool ok = StatInternal(path, false);
			if (!ok) return false;
			return ((FileModeFlags)statCache.st_mode & FileModeFlags.Execute) > 0;
		}

		/// <summary>
		/// Tells whether the path is a regular file and if it exists.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		[ImplementsFunction("is_file")]
		public static bool IsFile(string path)
		{
            StreamWrapper wrapper;

            if (StatInternalCheck(ref path, false, out wrapper))
            {
                string url;
                if (StatInternalTryCache(path, out url))
                    return ((FileModeFlags)statCache.st_mode & FileModeFlags.File) != 0;

                // we can't just call File.Exists since we have to throw warnings
                // also we are not calling full stat(), it is slow
                return FileStreamWrapper.HandleNewFileSystemInfo(false, path, (p) => new FileInfo(p).Exists);
            }

            return false;
		}

		/// <summary>
		/// Tells whether the path is a symbolic link.
		/// </summary>
		/// <remarks>
		/// Returns always <c>false</c>.
		/// </remarks>
		/// <param name="path"></param>
		/// <returns>Always <c>false</c></returns>
		[ImplementsFunction("is_link")]
		public static bool IsLink(string path)
		{
			return false; // OK
		}

		/// <summary>
		/// Tells whether the path is readable.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		[ImplementsFunction("is_readable")]
		public static bool IsReadable(string path)
		{
			bool ok = StatInternal(path, false);
			if (!ok) return false;
			return ((FileModeFlags)statCache.st_mode & FileModeFlags.Read) > 0;
		}

		/// <summary>
		/// Tells whether the path is writable.
		/// </summary>
		/// <param name="path">The path argument may be a directory name allowing you to check if a directory is writeable. </param>
		/// <returns>Returns TRUE if the path exists and is writable. </returns>
		[ImplementsFunction("is_writeable")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static bool IsWriteable(string path)
		{
			return IsWritable(path);
		}

		/// <summary>
		/// Tells whether the path is writable.
		/// </summary>
		/// <param name="path">The path argument may be a directory name allowing you to check if a directory is writeable. </param>
		/// <returns>Returns TRUE if the path exists and is writable. </returns>
		[ImplementsFunction("is_writable")]
		public static bool IsWritable(string path)
		{
			bool ok = StatInternal(path, false);
			if (!ok) return false;
			return ((FileModeFlags)statCache.st_mode & FileModeFlags.Write) > 0;
		}

		#endregion
	}

	#region NS: Unix Functions

	/// <summary>
	/// Unix-specific PHP functions. Not supported. Implementations are empty.
	/// </summary>
	/// <threadsafety static="true"/>
	public static class UnixFile
	{
		#region Owners, Mode (chgrp, chmod, chown, umask)

		/// <summary>
		/// Changes a group. Not supported.
		/// </summary>
		/// <param name="path">Path to the file to change group.</param>
		/// <param name="group">A <see cref="string"/> or <see cref="int"/>
		/// identifier of the target group.</param>
		/// <returns>Always <B>false</B>.</returns>
		[ImplementsFunction("chgrp")]
		public static bool ChangeFileGroup(string path, object group)
		{
			PhpException.FunctionNotSupported(PhpError.Warning);
			return false;
		}

        #region chmod helpers
 		/// <summary>
		/// Add or revoke specified permission for a given role
		/// </summary>
		/// <param name="role">an equivalent to UNIX's owner, group or public</param>
		/// <param name="permission">~ read, write, search</param>
		/// <param name="add"></param>
		static FileSystemAccessRule GetAccessRule(
				WellKnownSidType role,
				FileSystemRights permission,
				bool add)
		{
			//http://social.msdn.microsoft.com/Forums/en-US/vbgeneral/thread/dc841874-b71b-4e1c-9052-06eb4a87d08f
			IdentityReference identity = new SecurityIdentifier(role, null).Translate(typeof(NTAccount)) as IdentityReference;

			return new FileSystemAccessRule(
					identity,
					permission,
					add ? AccessControlType.Allow : AccessControlType.Deny);
		}

		/// <summary>
		/// Attempt to populate a set of Windows access rules, calculated from a UNIX mode flags
		/// </summary>
		/// <param name="mode"></param>
		/// <returns></returns>
		static FileSystemAccessRule[] ResolveAccessRules(int mode)
		{
			//http://support.microsoft.com/kb/243330
			WellKnownSidType[] roles = new WellKnownSidType[] {
				WellKnownSidType.WorldSid,
				WellKnownSidType.CreatorGroupSid,
				WellKnownSidType.CreatorOwnerSid
			};

			//http://en.wikipedia.org/wiki/File_system_permissions#Octal_notation
			FileSystemRights[] permissions = new FileSystemRights[] {
				FileSystemRights.ReadAndExecute,
				FileSystemRights.Write,
				FileSystemRights.Read
			};

			var rules = new System.Collections.Generic.List<FileSystemAccessRule>();
			
			//Walk all combinations of roles and permissions
			for (int r = 0; r < roles.Length; r++) {
				WellKnownSidType role = roles[r];

				for (int p = 0; p < permissions.Length; p++) {
					rules.Add(GetAccessRule(role, permissions[p], ((mode >> (r * 3)) & (1 << p)) != 0));
				}
			}
			
			return rules.ToArray();
		}
		
		#endregion chmod helpers

		/// <summary>
		/// Changes file permissions. 
		/// </summary>
		/// <remarks>
		/// On Windows platform this function supports only the 
		/// <c>_S_IREAD (0400)</c> and <c>_S_IWRITE (0200)</c>
		/// options (set read / write permissions for the file owner).
		/// Note that the constants are octal numbers.
		/// </remarks>
		/// <param name="path">Path to the file to change group.</param>
		/// <param name="mode">New file permissions (see remarks).</param>
		/// <returns><c>true</c> on success, <c>false</c> on failure.</returns>
		[ImplementsFunction("chmod")]
		public static bool ChangeFileMode(string path, int mode)
		{
			StreamWrapper wrapper;
			if (!PhpStream.ResolvePath(ref path, out wrapper, CheckAccessMode.FileOrDirectory, CheckAccessOptions.Empty))
				return false;
			
            bool isDir = PhpFile.IsDirectory(path);
			FileSystemInfo fInfo = isDir ? (FileSystemInfo)new DirectoryInfo(path) : new FileInfo(path);

            if (!fInfo.Exists)
            {
                PhpException.Throw(PhpError.Warning, CoreResources.GetString("invalid_path", path));
                return false;
            }
            
			//Directories has no equivalent of a readonly flag,
			//instead, their content permission should be adjusted accordingly
			//[http://msdn.microsoft.com/en-us/library/system.security.accesscontrol.directorysecurity.aspx]
			if (isDir)
			{
                //DirectoryInfo dInfo = (DirectoryInfo)fInfo;
                //DirectorySecurity dSecurity = dInfo.GetAccessControl();
				
                //foreach(FileSystemAccessRule rule in ResolveAccessRules(mode))
                //    dSecurity.AddAccessRule(rule);
                //try
                //{
                //    dInfo.SetAccessControl(dSecurity);
                //}
                //catch
                {
                    return false;
                }
			}
			else
			{
				// according to <io.h> and <chmod.c> from C libraries in Visual Studio 2008
				// and PHP 5.3 source codes, which are using standard _chmod() function in C
				// on Windows it only changes the ReadOnly flag of the file
				//
				// see <chmod.c> for more details
				/*
				#define _S_IREAD        0x0100          // read permission, owner
				#define _S_IWRITE       0x0080          // write permission, owner
				#define _S_IEXEC        0x0040          // execute/search permission, owner
				*/

				((FileInfo)fInfo).IsReadOnly = ((mode & 0x0080) == 0);
			}

            return true;
		}

		/// <summary>
		/// Unix-specific function. Not supported.
		/// </summary>
		/// <param name="path">Path to the file to change owner.</param>
		/// <param name="user">A <see cref="string"/> or <see cref="int"/>
		/// identifier of the target group.</param>
		/// <returns>Always <c>false</c>.</returns>
		[ImplementsFunction("chown")]
		public static bool ChangeFileOwner(string path, object user)
		{
			PhpException.FunctionNotSupported(PhpError.Warning);
			return false;
		}

		/// <summary>
		/// Unix-specific function. Not supported.
		/// </summary>
		/// <param name="mask"></param>
		/// <returns></returns>
		[ImplementsFunction("umask")]
		public static int UMask(int mask)
		{
			return UMask();
		}

		/// <summary>
		/// Unix-specific function. Not supported.
		/// </summary>
		/// <returns></returns>
		[ImplementsFunction("umask")]
		public static int UMask()
		{
			return 0;
		}

		#endregion

		#region Links (link, symlink, readlink, linkinfo)

		/// <summary>
		/// Unix-specific function. Not supported.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="link"></param>
		/// <returns></returns>
		[ImplementsFunction("link")]
		public static bool MakeHardLink(string target, string link)
		{
			// Creates a hard link.
			PhpException.FunctionNotSupported(PhpError.Warning);
			return false;
		}

		/// <summary>
		/// Unix-specific function. Not supported.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="link"></param>
		/// <returns></returns>
		[ImplementsFunction("symlink")]
		public static bool MakeSymbolicLink(string target, string link)
		{
			// Creates a symbolic link.
			PhpException.FunctionNotSupported(PhpError.Warning);
			return false;
		}

		/// <summary>
		/// Unix-specific function. Not supported.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		[ImplementsFunction("readlink", FunctionImplOptions.NotSupported)]
		public static string ReadLink(string path)
		{
			// Returns the target of a symbolic link.
			PhpException.FunctionNotSupported(PhpError.Warning);
			return null;
		}

		/// <summary>
		/// Unix-specific function. Not supported.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		[ImplementsFunction("linkinfo")]
		public static int GetLinkInfo(string path)
		{
			// Gets information about a link.
			PhpException.FunctionNotSupported(PhpError.Warning);
			return 0;
		}

		#endregion
	}

	#endregion
}
