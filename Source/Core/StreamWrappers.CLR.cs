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
using System.Diagnostics;
using PHP.Core;
using System.Security.Principal;
using System.Security.AccessControl;

namespace PHP.Core
{
	#region Abstract Stream Wrapper

	/// <summary>
	/// Abstract base class for PHP stream wrappers. Descendants define 
	/// methods implementing fopen, stat, unlink, rename, opendir, mkdir and rmdir 
	/// for different stream types.
	/// </summary>
	public abstract partial class StreamWrapper
	{
		#region Optional Wrapper Operations (Warning)

		/// <include file='Doc/Wrappers.xml' path='docs/method[@name="Stat"]/*'/>
        /// <remarks>
		/// <seealso cref="StreamStatOptions"/> for the list of additional options.
		/// </remarks>
        public virtual StatStruct Stat(string path, StreamStatOptions options, StreamContext context, bool streamStat)
		{
			// int (*url_stat)(php_stream_wrapper *wrapper, char *url, int flags, php_stream_statbuf *ssb, php_stream_context *context TSRMLS_DC);
			PhpException.Throw(PhpError.Warning, CoreResources.GetString("wrapper_op_unsupported", "Stat"));
			return new StatStruct();
		}

		#endregion
	}

	#endregion

	#region Local Filesystem Wrapper

	/// <summary>
	/// Derived from <see cref="StreamWrapper"/>, this class provides access to 
	/// the local filesystem files.
	/// </summary>
	public partial class FileStreamWrapper : StreamWrapper
	{
		#region Opening a file

		/// <include file='Doc/Wrappers.xml' path='docs/method[@name="Open"]/*'/>
		public override PhpStream Open(ref string path, string mode, StreamOpenOptions options, StreamContext context)
		{
			Debug.Assert(path != null);
			//Debug.Assert(PhpPath.IsLocalFile(path));

			// Get the File.Open modes from the mode string
			FileMode fileMode;
			FileAccess fileAccess;
			StreamAccessOptions ao;

			if (!ParseMode(mode, options, out fileMode, out fileAccess, out ao)) return null;

			// Open the native stream
			FileStream stream = null;
			try
			{
				// stream = File.Open(path, fileMode, fileAccess, FileShare.ReadWrite);
				stream = new FileStream(path, fileMode, fileAccess, FileShare.ReadWrite | FileShare.Delete);
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
				// Read/Write Append is not supported. Seek to the end of file manually.
				stream.Seek(0, SeekOrigin.End);
			}

			if ((ao & StreamAccessOptions.Temporary) > 0)
			{
				// Set the file attributes to Temporary too.
				File.SetAttributes(path, FileAttributes.Temporary);
			}

			return new NativeStream(stream, this, ao, path, context);
		}


		#endregion

		#region Optional Wrapper Operations Implementations

		#region Stat related methods and Stat caching

		/// <summary>
		/// Creates a <see cref="StatStruct"/> from the <see cref="StatStruct"/> filling the common
		/// members (for files and directories) from the given <see cref="FileSystemInfo"/> class.
		/// The <c>size</c> member (numeric index <c>7</c>) may be filled by the caller
		/// for when <paramref name="info"/> is a <see cref="FileInfo"/>.
		/// </summary>
		/// <remarks>
		/// According to these outputs (PHP Win32):
		/// <code>
		/// fstat(somefile.txt):
		///    [dev] => 0
		///    [ino] => 0
		///    [mode] => 33206
		///    [nlink] => 1
		///    [uid] => 0
		///    [gid] => 0
		///    [rdev] => 0
		///    [size] => 24
		///    [atime] => 1091131360
		///    [mtime] => 1091051699
		///    [ctime] => 1091051677
		///    [blksize] => -1
		///    [blocks] => -1
		/// 
		/// stat(somefile.txt):
		///    [dev] => 2
		///    [ino] => 0
		///    [mode] => 33206 // 0100666
		///    [nlink] => 1
		///    [uid] => 0
		///    [gid] => 0
		///    [rdev] => 2
		///    [size] => 24
		///    [atime] => 1091129621
		///    [mtime] => 1091051699
		///    [ctime] => 1091051677
		///    [blksize] => -1
		///    [blocks] => -1
		///    
		/// stat(somedir):
		///    [st_dev] => 2
		///    [st_ino] => 0
		///    [st_mode] => 16895 // 040777
		///    [st_nlink] => 1
		///    [st_uid] => 0
		///    [st_gid] => 0
		///    [st_rdev] => 2
		///    [st_size] => 0
		///    [st_atime] => 1091109319
		///    [st_mtime] => 1091044521
		///    [st_ctime] => 1091044521
		///    [st_blksize] => -1
		///    [st_blocks] => -1
		/// </code>
		/// </remarks>
		/// <param name="info">A <see cref="FileInfo"/> or <see cref="DirectoryInfo"/>
		/// of the <c>stat()</c>ed filesystem entry.</param>
		/// <param name="attributes">The file or directory attributes.</param>
		/// <param name="path">The path to the file / directory.</param>
		/// <returns>A <see cref="StatStruct"/> for use in the <c>stat()</c> related functions.</returns>    
		internal static StatStruct BuildStatStruct(FileSystemInfo info, FileAttributes attributes, string path)
		{
			StatStruct result;//  = new StatStruct();
			uint device = unchecked((uint)(char.ToLower(info.FullName[0]) - 'a')); // index of the disk

            ushort mode = (ushort)BuildMode(info, attributes, path);

			long atime,mtime,ctime;
            atime = ToStatUnixTimeStamp(info, (_info) => _info.LastAccessTimeUtc);
            mtime = ToStatUnixTimeStamp(info, (_info) => _info.LastWriteTimeUtc);
            ctime = ToStatUnixTimeStamp(info, (_info) => _info.CreationTimeUtc);

			result.st_dev = device;         // device number 
			result.st_ino = 0;              // inode number 
			result.st_mode = mode;          // inode protection mode 
			result.st_nlink = 1;            // number of links 
			result.st_uid = 0;              // userid of owner 
			result.st_gid = 0;              // groupid of owner 
			result.st_rdev = device;        // device type, if inode device -1
			result.st_size = 0;             // size in bytes

            FileInfo file_info = info as FileInfo;
            if (file_info != null) 
                result.st_size = FileSystemUtils.FileSize(file_info);

			result.st_atime = atime;        // time of last access (unix timestamp) 
			result.st_mtime = mtime;        // time of last modification (unix timestamp) 
			result.st_ctime = ctime;        // time of last change (unix timestamp) 
			//result.st_blksize = -1;   // blocksize of filesystem IO (-1)
			//result.st_blocks = -1;    // number of blocks allocated  (-1)

			return result;
		}

		/// <summary>
		/// Adjusts UTC time of a file by adding Daylight Saving Time difference.
		/// Makes file times working in the same way as in PHP and Windows Explorer.
		/// </summary>
        /// <param name="info"><see cref="FileSystemInfo"/> object reference. Used to avoid creating of closure when passing <paramref name="utcTimeFunc"/>.</param>
        /// <param name="utcTimeFunc">Function obtaining specific <see cref="DateTime"/> from given <paramref name="info"/>.</param>
        private static long ToStatUnixTimeStamp(FileSystemInfo info, Func<FileSystemInfo, DateTime> utcTimeFunc)
		{
            DateTime utcTime;

            try
            {
                utcTime = utcTimeFunc(info);
            }
            catch (ArgumentOutOfRangeException)
            {
                //On Linux this exception might be thrown if a file metadata are corrupted
                //just catch it and return 0;
                return 0;
            }

			return DateTimeUtils.UtcToUnixTimeStamp(utcTime + DateTimeUtils.GetDaylightTimeDifference(utcTime, DateTime.UtcNow));
		}

        /// <summary>
        /// Gets the ACL of a file and converts it into UNIX-like file mode
        /// </summary>
        public static FileModeFlags GetFileMode(FileInfo info)
        {
            System.Security.AccessControl.AuthorizationRuleCollection acl;

            try
            {
                // Get the collection of authorization rules that apply to the given directory
                acl = info.GetAccessControl().GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier));
            }
            catch (UnauthorizedAccessException)
            {
                //we don't want to throw this exception from getting access list
                return 0;
            }

            return GetFileMode(acl);
        }

        /// <summary>
        ///  Gets the ACL of a directory and converts it ACL into UNIX-like file mode
        /// </summary>
        public static FileModeFlags GetFileMode(DirectoryInfo info)
        {
            System.Security.AccessControl.AuthorizationRuleCollection acl;

            try
            {
                // Get the collection of authorization rules that apply to the given directory
                acl = info.GetAccessControl().GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier));
            }
            catch(UnauthorizedAccessException)
            {
                //we don't want to throw this exception from getting access list
                return 0;
            }

            return GetFileMode(acl);

        }

        /// <summary>
        /// Converts ACL into UNIX-like file mode
        /// </summary>
        private static FileModeFlags GetFileMode(System.Security.AccessControl.AuthorizationRuleCollection rules)
        {
            WindowsIdentity user = System.Security.Principal.WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(user);
            FileModeFlags result;

            // These are set to true if either the allow read or deny read access rights are set
            bool allowRead = false;
            bool denyRead = false;
            bool allowWrite = false;
            bool denyWrite = false;
            bool allowExecute = false;
            bool denyExecute = false;


            foreach (FileSystemAccessRule currentRule in rules)
            {
                // If the current rule applies to the current user
                if (user.User.Equals(currentRule.IdentityReference) || principal.IsInRole((SecurityIdentifier)currentRule.IdentityReference))
                {
                    switch (currentRule.AccessControlType)
                    {
                        case AccessControlType.Deny:

                            denyRead |= (currentRule.FileSystemRights & FileSystemRights.ListDirectory | FileSystemRights.Read) != 0;
                            denyWrite |= (currentRule.FileSystemRights & FileSystemRights.Write) != 0;
                            denyExecute |= (currentRule.FileSystemRights & FileSystemRights.ExecuteFile) != 0;

                            break;

                        case AccessControlType.Allow:

                            allowRead |= (currentRule.FileSystemRights & FileSystemRights.ListDirectory | FileSystemRights.Read) != 0;
                            allowWrite |= (currentRule.FileSystemRights & FileSystemRights.Write) != 0;
                            allowExecute |= (currentRule.FileSystemRights & FileSystemRights.ExecuteFile) != 0;

                            break;
                    }
                }
            }

            result = (allowRead & !denyRead) ? FileModeFlags.Read : 0;
            result |= (allowWrite & !denyWrite) ? FileModeFlags.Write : 0;
            result |= (allowExecute & !denyExecute) ? FileModeFlags.Execute : 0;

            return result;
        }

		/// <summary>
		/// Creates the UNIX-like file mode depending on the file or directory attributes.
		/// </summary>
        /// <param name="info">Information about file system object.</param>
		/// <param name="attributes">Attributes of the file.</param>
		/// <param name="path">Paths to the file.</param>
		/// <returns>UNIX-like file mode.</returns>
		private static FileModeFlags BuildMode(FileSystemInfo/*!*/info, FileAttributes attributes, string path)
        {
            // TODO: remove !EnvironmentUtils.IsDotNetFramework branches;
            // use mono.unix.native.stat on Mono instead of BuildStatStruct(), http://docs.go-mono.com/?link=M%3aMono.Unix.Native.Syscall.stat

            // TODO: use Win32 stat on Windows

			// Simulates the UNIX file mode.
			FileModeFlags rv;

            if ((attributes & FileAttributes.Directory) != 0)
            {
                // a directory:
                rv = FileModeFlags.Directory;

                if (EnvironmentUtils.IsDotNetFramework)
                {
                    rv |= GetFileMode((DirectoryInfo)info);

                    // PHP on Windows always shows that directory isn't executable
                    rv &= ~FileModeFlags.Execute;
                }
                else
                {
                    rv |= FileModeFlags.Read | FileModeFlags.Execute | FileModeFlags.Write;
                }
            }
            else
            {
                // a file:
                rv = FileModeFlags.File;

                if (EnvironmentUtils.IsDotNetFramework)
                {
                    rv |= GetFileMode((FileInfo)info);

                    if ((attributes & FileAttributes.ReadOnly) != 0 && (rv & FileModeFlags.Write) != 0)
                        rv &= ~FileModeFlags.Write;

                    if ((rv & FileModeFlags.Execute) == 0)
                    {
                        // PHP on Windows checks the file internaly wheather it is executable
                        // we just look on the extension

                        string ext = Path.GetExtension(path);
                        if ((ext.EqualsOrdinalIgnoreCase(".exe")) || (ext.EqualsOrdinalIgnoreCase(".com")) || (ext.EqualsOrdinalIgnoreCase(".bat")))
                            rv |= FileModeFlags.Execute;
                    }
                }
                else
                {
                    rv |= FileModeFlags.Read; // | FileModeFlags.Execute;

                    if ((attributes & FileAttributes.ReadOnly) == 0)
                        rv |= FileModeFlags.Write;
                }
            }

            //
			return rv;
		}

		/// <include file='Doc/Wrappers.xml' path='docs/method[@name="Stat"]/*'/>
        public override StatStruct Stat(string path, StreamStatOptions options, StreamContext context, bool streamStat)
        {
            StatStruct invalid = new StatStruct();
            invalid.st_size = -1;
            Debug.Assert(path != null);

            // Note: path is already absolute w/o the scheme, the permissions have already been checked.
            return HandleNewFileSystemInfo(invalid, path, (p) =>
                {
                    FileSystemInfo info = null;

                    info = new DirectoryInfo(p);
                    if (!info.Exists)
                    {
                        info = new FileInfo(p);
                        if (!info.Exists)
                        {
                            return invalid;
                        }
                    }

                    return BuildStatStruct(info, info.Attributes, p);
                });
        }

        /// <summary>
        /// Try the new FileSystemInfo based operation and hamdle exceptions properly.
        /// </summary>
        /// <typeparam name="T">The return value type.</typeparam>
        /// <param name="invalid">Invalid value.</param>
        /// <param name="path">Path to the resource passed to the <paramref name="action"/>. Also used for error control.</param>
        /// <param name="action">Action to try. The first argument is the path.</param>
        /// <returns>The value of <paramref name="action"/>() or <paramref name="invalid"/>.</returns>
        public static T HandleNewFileSystemInfo<T>(T invalid, string path, Func<string,T>/*!*/action)
        {
            try
            {
                return action(path);
            }
            catch (ArgumentException)
            {
                PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_stat_invalid_path",
                    FileSystemUtils.StripPassword(path)));
            }
            catch (PathTooLongException)
            {
                PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_stat_invalid_path",
                    FileSystemUtils.StripPassword(path)));
            }
            catch (Exception e)
            {
                PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_error",
                    FileSystemUtils.StripPassword(path), e.Message));
            }

            return invalid;
        }

		#endregion

		/// <include file='Doc/Wrappers.xml' path='docs/method[@name="Unlink"]/*'/>
		public override bool Unlink(string path, StreamUnlinkOptions options, StreamContext context)
		{
			Debug.Assert(path != null);
			Debug.Assert(Path.IsPathRooted(path));

			try
			{
				File.Delete(path);
				return true;
			}
			catch (DirectoryNotFoundException)
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_unlink_file_not_found",
					FileSystemUtils.StripPassword(path)));
			}
			catch (UnauthorizedAccessException)
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_file_access_denied",
					FileSystemUtils.StripPassword(path)));
			}
			catch (IOException e)
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_unlink_io_error",
					FileSystemUtils.StripPassword(path), PhpException.ToErrorMessage(e.Message)));
			}
			catch (Exception)
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_unlink_error",
					FileSystemUtils.StripPassword(path)));
			}

			return false;
		}

		/// <include file='Doc/Wrappers.xml' path='docs/method[@name="Listing"]/*'/>
		public override string[] Listing(string path, StreamListingOptions options, StreamContext context)
		{
			Debug.Assert(path != null);
			Debug.Assert(Path.IsPathRooted(path));

			try
			{
				string[] listing = Directory.GetFileSystemEntries(path);
				bool root = Path.GetPathRoot(path) == path;
				int index = root ? 0 : 2;
				string[] rv = new string[listing.Length + index];

				// Remove the absolute path information (PHP returns only filenames)
				int pathLength = path.Length;
				if (path[pathLength - 1] != Path.DirectorySeparatorChar) pathLength++;

				// Check for the '.' and '..'; they should be present
				if (!root)
				{
					rv[0] = ".";
					rv[1] = "..";
				}
				for (int i = 0; i < listing.Length; i++)
				{
					rv[index++] = listing[i].Substring(pathLength);
				}
				return rv;
			}
			catch (DirectoryNotFoundException)
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_bad_directory",
					FileSystemUtils.StripPassword(path)));
			}
			catch (UnauthorizedAccessException)
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_file_access_denied",
					FileSystemUtils.StripPassword(path)));
			}
			catch (Exception e)
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_error",
					FileSystemUtils.StripPassword(path), e.Message));
			}
			return null;
		}

		/// <include file='Doc/Wrappers.xml' path='docs/method[@name="Rename"]/*'/>
		public override bool Rename(string fromPath, string toPath, StreamRenameOptions options, StreamContext context)
		{
			try
			{
				File.Move(fromPath, toPath);
				return true;
			}
			catch (UnauthorizedAccessException)
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_file_access_denied",
					FileSystemUtils.StripPassword(fromPath)));
			}
			catch (IOException)
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_rename_file_exists",
					FileSystemUtils.StripPassword(fromPath), FileSystemUtils.StripPassword(toPath)));
			}
			catch (Exception e)
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_error",
					FileSystemUtils.StripPassword(fromPath), e.Message));
			}
			return false;
		}

		/// <include file='Doc/Wrappers.xml' path='docs/method[@name="MakeDirectory"]/*'/>
		public override bool MakeDirectory(string path, int accessMode, StreamMakeDirectoryOptions options, StreamContext context)
		{
			if ((path == null) || (path == string.Empty))
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("path_argument_empty"));
				return false;
			}

			try
			{
				// Default Framework MakeDirectory is RECURSIVE, check for other intention. 
				if ((options & StreamMakeDirectoryOptions.Recursive) == 0)
				{
					int pos = path.Length - 1;
					if (path[pos] == Path.DirectorySeparatorChar) pos--;
					pos = path.LastIndexOf(Path.DirectorySeparatorChar, pos);
					if (pos <= 0)
					{
						PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_directory_make_root",
							FileSystemUtils.StripPassword(path)));
						return false;
					}

					// Parent must exist if not recursive.
					string parent = path.Substring(0, pos);
					if (!Directory.Exists(parent))
					{
						PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_directory_make_parent",
							FileSystemUtils.StripPassword(path)));
						return false;
					}
				}

				// Creates the whole path
				Directory.CreateDirectory(path);
				return true;
			}
			catch (UnauthorizedAccessException)
			{
				// The caller does not have the required permission.
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_directory_access_denied",
					FileSystemUtils.StripPassword(path)));
			}
			catch (IOException)
			{
				// The directory specified by path is read-only or is not empty.
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_directory_error",
					FileSystemUtils.StripPassword(path)));
			}
			catch (Exception e)
			{
				// The specified path is invalid, such as being on an unmapped drive ...
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_error",
					FileSystemUtils.StripPassword(path), e.Message));
			}
			return false;
		}

		/// <include file='Doc/Wrappers.xml' path='docs/method[@name="RemoveDirectory"]/*'/>
		public override bool RemoveDirectory(string path, StreamRemoveDirectoryOptions options, StreamContext context)
		{
			try
			{
				// Deletes the directory (but not the contents - must be empty)
				Directory.Delete(path, false);
				return true;
			}
			catch (UnauthorizedAccessException)
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_file_access_denied",
					FileSystemUtils.StripPassword(path)));
			}
			catch (IOException)
			{
				// Directory not empty.
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_rmdir_io_error",
					FileSystemUtils.StripPassword(path)));
			}
			return false;
		}

		#endregion
	}

	#endregion

	#region Input/Output Stream Wrapper

	/// <summary>
	/// Derived from <see cref="StreamWrapper"/>, this class provides access to the PHP input/output streams.
	/// </summary>
	public partial class InputOutputStreamWrapper : StreamWrapper
	{
		/// <summary>
		/// Represents the console input stream (alias php://stdin).
		/// </summary>
		/// <remarks>
		/// It is a persistent text stream. This means that it is never closed
		/// by <c>fclose()</c> and <c>\r\n</c> is converted to <c>\n</c>.
		/// </remarks>
		public static PhpStream In
		{
			get
			{
				if (stdin == null)
				{
					stdin = new NativeStream(Console.OpenStandardInput(), null, StreamAccessOptions.Read | StreamAccessOptions.UseText | StreamAccessOptions.Persistent, "php://stdin", StreamContext.Default);
					stdin.IsReadBuffered = false;
					// EX: cache this as a persistent stream (incl. path and options)
				}
				return stdin;
			}
		}
		private static PhpStream stdin = null;

		/// <summary>
		/// Represents the console output stream (alias php://stdout).
		/// </summary>
		/// <remarks>
		/// It is a persistent text stream. This means that it is never closed
		/// by <c>fclose()</c> and <c>\n</c> is converted to <c>\r\n</c>.
		/// </remarks>
		public static PhpStream Out
		{
			get
			{
				if (stdout == null)
				{
					stdout = new NativeStream(Console.OpenStandardOutput(), null, StreamAccessOptions.Write | StreamAccessOptions.UseText | StreamAccessOptions.Persistent, "php://stdout", StreamContext.Default);
					stdout.IsWriteBuffered = false;
					// EX: cache this as a persistent stream
				}
				return stdout;
			}
		}
		private static PhpStream stdout = null;

		/// <summary>
		/// Represents the console error stream (alias php://error).
		/// </summary>
		/// <remarks>
		/// It is a persistent text stream. This means that it is never closed
		/// by <c>fclose()</c> and <c>\n</c> is converted to <c>\r\n</c>.
		/// </remarks>
		public static PhpStream Error
		{
			get
			{
				if (stderr == null)
				{
					stderr = new NativeStream(Console.OpenStandardInput(), null,
						StreamAccessOptions.Write | StreamAccessOptions.UseText | StreamAccessOptions.Persistent, "php://stderr", StreamContext.Default);
					stderr.IsWriteBuffered = false;
					// EX: cache this as a persistent stream
				}
				return stderr;
			}
		}
		private static PhpStream stderr = null;
	}

	#endregion

	#region Extenal streams wrapper
	/// <summary>
	/// Represents a native PHP stream wrapper that lives in <c>ExtManager</c>.
	/// </summary>
	public class ExternalStreamWrapper : StreamWrapper
	{
		/// <summary>A MBRO proxy of the (remote) native stream wrapper.</summary>
		private IExternalStreamWrapper proxy;

		/// <summary>The protocol portion of URL handled by this wrapper.</summary>
		private readonly string scheme;

		/// <summary>
		/// Tries to find an <see cref="ExternalStreamWrapper"/> a given <paramref name="scheme"/>.
		/// </summary>
		/// <param name="scheme">The scheme portion of an URL.</param>
		/// <returns>An <see cref="ExternalStreamWrapper"/> associated with the given <paramref name="scheme"/>
		/// or <c>null</c> if the wrapper was not found.</returns>
		public static ExternalStreamWrapper GetExternalWrapperByScheme(string scheme)
		{
			IExternalStreamWrapper proxy = Externals.GetStreamWrapper(scheme);
			return (proxy == null ? null : new ExternalStreamWrapper(proxy, scheme));
		}

		/// <summary>
		/// Creates a new <see cref="ExternalStreamWrapper"/> with the given proxy object.
		/// </summary>
		/// <param name="proxy">Instance of a class derived from <see cref="MarshalByRefObject"/> that should
		/// serve as a proxy for this <see cref="ExternalStreamWrapper"/>.</param>
		/// <param name="scheme">The protocol portion of URL handled by this wrapper.</param>
		protected ExternalStreamWrapper(IExternalStreamWrapper proxy, string scheme)
		{
			this.proxy = proxy;
			this.scheme = scheme;
		}

		/// <include file='Doc/Wrappers.xml' path='docs/method[@name="Open"]/*'/>
		public override PhpStream Open(ref string path, string mode, StreamOpenOptions options, StreamContext context)
		{
			StreamAccessOptions accessOptions;
			if (!ParseMode(mode, options, out accessOptions)) return null;
			string opened_path;
			IExternalStream stream = proxy.Open(path, mode, (int)options, out opened_path, null);
			path = opened_path;
			return (stream == null ? null :
				new ExternalStream(stream, this, accessOptions, opened_path, context));
		}

		/// <include file='Doc/Wrappers.xml' path='docs/property[@name="Label"]/*'/>
		public override string Label
		{
			get
			{
				return proxy.Label;
			}
		}

		/// <include file='Doc/Wrappers.xml' path='docs/property[@name="Scheme"]/*'/>
		public override string Scheme
		{
			get
			{
				return scheme;
			}
		}

		/// <include file='Doc/Wrappers.xml' path='docs/property[@name="IsUrl"]/*'/>
		public override bool IsUrl
		{
			get
			{
				return proxy.IsUrl;
			}
		}


		/// <include file='Doc/Wrappers.xml' path='docs/method[@name="Stat"]/*'/>
        public override StatStruct Stat(string path, StreamStatOptions options, StreamContext context, bool streamStat)
		{
			return proxy.Stat(path, (int)options, null, streamStat);
		}

		/// <include file='Doc/Wrappers.xml' path='docs/method[@name="Unlink"]/*'/>
		public override bool Unlink(string path, StreamUnlinkOptions options, StreamContext context)
		{
			return proxy.Unlink(path, (int)options, null);
		}

		/// <include file='Doc/Wrappers.xml' path='docs/method[@name="Listing"]/*'/>
		public override string[] Listing(string path, StreamListingOptions options, StreamContext context)
		{
			return proxy.Listing(path, (int)options, null);
		}

		/// <include file='Doc/Wrappers.xml' path='docs/method[@name="Rename"]/*'/>
		public override bool Rename(string fromPath, string toPath, StreamRenameOptions options, StreamContext context)
		{
			return proxy.Rename(fromPath, toPath, (int)options, null);
		}

		/// <include file='Doc/Wrappers.xml' path='docs/method[@name="MakeDirectory"]/*'/>
		public override bool MakeDirectory(string path, int accessMode, StreamMakeDirectoryOptions options, StreamContext context)
		{
			return proxy.MakeDirectory(path, accessMode, (int)options, null);
		}

		/// <include file='Doc/Wrappers.xml' path='docs/method[@name="RemoveDirectory"]/*'/>
		public override bool RemoveDirectory(string path, StreamRemoveDirectoryOptions options, StreamContext context)
		{
			return proxy.RemoveDirectory(path, (int)options, context);
		}
	}
	#endregion
}