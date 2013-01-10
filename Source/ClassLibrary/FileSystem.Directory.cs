/*

 Copyright (c) 2004-2006 Jan Benda and Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using PHP.Core;
using PHP.Core.Reflection;
#if SILVERLIGHT
using PHP.CoreCLR;
#endif

namespace PHP.Library
{
	#region Directory user-class

	/// <summary>
	/// User-like class encapsulating enumeration of a Directory. 
	/// Uses the PhpDiretory implementation upon PhpWrapper streams.
	/// </summary>
#if !SILVERLIGHT
	[Serializable]
#endif
	[ImplementsType]
	public class Directory : PhpObject
	{
		#region Fields

		/// <summary>
		/// Reference to the directory listing resource.
		/// </summary>
		public PhpReference handle = new PhpSmartReference();

		/// <summary>
		/// The opened path (accessible from the PHP script).
		/// </summary>
		public PhpReference path = new PhpSmartReference();

		#endregion

		#region Construction

		/// <summary>
		/// Start listing of a directory (intended to be used from C#).
		/// </summary>
		/// <param name="directory">The path to the directory.</param>
		public Directory(string directory)
			: this(ScriptContext.CurrentContext, true)
		{
			this.path = new PhpReference(directory);
			this.handle = new PhpReference(PhpDirectory.Open(directory));
		}

		/// <summary>
		/// For internal purposes only.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public Directory(ScriptContext context, bool newInstance)
			: base(context, newInstance)
		{ }

		/// <summary>
		/// For internal purposes only.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public Directory(ScriptContext context, DTypeDesc caller)
			: base(context, caller)
		{ }

#if !SILVERLIGHT
		/// <summary>Deserializing constructor.</summary>
		protected Directory(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{ }
#endif

		#endregion

		#region read

		/// <summary>
		/// Read next directory entry.
		/// </summary>
		/// <returns>Filename of a contained file (including . and ..).</returns>
		[ImplementsMethod]
        [return:CastToFalse]
		public object read(ScriptContext context, [Optional]object handle)
		{
			PhpResource res = (handle == Arg.Default ? this.handle.Value : handle) as PhpResource;

			if (res == null)
			{
				PhpException.InvalidImplicitCast("handle", PhpResource.PhpTypeName, "read");
				return null;
			}

			return PhpDirectory.Read(res);
		}

		#endregion

		#region rewind

		/// <summary>
		/// Restart the directory listing.
		/// </summary>
		[ImplementsMethod]
		public object rewind(ScriptContext context, [Optional]object handle)
		{
			PhpResource res = (handle == Arg.Default ? this.handle.Value : handle) as PhpResource;

			if (res == null)
			{
				PhpException.InvalidImplicitCast("handle", PhpResource.PhpTypeName, "rewind");
				return null;
			}

			PhpDirectory.Rewind(res);
			return null;
		}

		#endregion

		#region close

		/// <summary>
		/// Finish the directory listing.
		/// </summary>
		[ImplementsMethod]
		public object close(ScriptContext context, [Optional]object handle)
		{
			PhpResource res = (handle == Arg.Default ? this.handle.Value : handle) as PhpResource;

			if (res == null)
			{
				PhpException.InvalidImplicitCast("handle", PhpResource.PhpTypeName, "close");
				return null;
			}

			PhpDirectory.Close(res);
			return null;
		}

		#endregion

		#region Implementation Details

		/// <summary>
		/// Populates the provided <see cref="DTypeDesc"/> with this class's methods and properties.
		/// </summary>
		/// <param name="typeDesc">The type desc to populate.</param>
		private static void __PopulateTypeDesc(PhpTypeDesc typeDesc)
		{
			typeDesc.AddMethod("read", PhpMemberAttributes.Public, read);
			typeDesc.AddMethod("rewind", PhpMemberAttributes.Public, rewind);
			typeDesc.AddMethod("close", PhpMemberAttributes.Public, close);

            typeDesc.AddProperty("handle", PhpMemberAttributes.Public,
                    (instance) => ((Directory)instance).handle,
                    (instance, value) => ((Directory)instance).handle = (PhpReference)value);
            typeDesc.AddProperty("path", PhpMemberAttributes.Public,
                    (instance) => ((Directory)instance).path,
                    (instance, value) => ((Directory)instance).path = (PhpReference)value);
		}

		/// <summary>Arg-less overload.</summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static object read(object instance, PhpStack stack)
		{
			switch (stack.ArgCount)
			{
				case 0:
					{
						stack.RemoveFrame();
                        return ((Directory)instance).read(stack.Context, Arg.Default) ?? false;
					}

				case 1:
					{
						stack.CalleeName = "read";
						object arg = stack.PeekValue(1);
						stack.RemoveFrame();
                        return ((Directory)instance).read(stack.Context, arg) ?? false;
					}

				default:
					{
						stack.RemoveFrame();
						PhpException.InvalidArgumentCount(null, "read");
						return null;
					}
			}
		}

		/// <summary>Arg-less overload.</summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static object rewind(object instance, PhpStack stack)
		{
			switch (stack.ArgCount)
			{
				case 0:
					{
						stack.RemoveFrame();
						((Directory)instance).rewind(stack.Context, Arg.Default);
						break;
					}

				case 1:
					{
						stack.CalleeName = "rewind";
						object arg = stack.PeekValue(1);
						stack.RemoveFrame();
						((Directory)instance).rewind(stack.Context, arg);
						break;
					}

				default:
					{
						stack.RemoveFrame();
						PhpException.InvalidArgumentCount(null, "rewind");
						break;
					}
			}
			return null;
		}

		/// <summary>Arg-less overload.</summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static object close(object instance, PhpStack stack)
		{
			switch (stack.ArgCount)
			{
				case 0:
					{
						stack.RemoveFrame();
						((Directory)instance).close(stack.Context, Arg.Default);
						break;
					}

				case 1:
					{
						stack.CalleeName = "close";
						object arg = stack.PeekValue(1);
						stack.RemoveFrame();
						((Directory)instance).close(stack.Context, arg);
						break;
					}

				default:
					{
						stack.RemoveFrame();
						PhpException.InvalidArgumentCount(null, "close");
						break;
					}
			}
			return null;
		}

		#endregion
	}

	#endregion

	#region DirectoryListing

	/// <summary>
	/// Enumeration class used for PhpDirectory listings - serves as a PhpResource.
	/// Uses the PhpWrapper stream wrappers only to generate the list of contained files.
	/// No actual resources to be released explicitly.
	/// </summary>
	internal sealed class DirectoryListing : PhpResource
	{
		public DirectoryListing(string[] listing)
			: base(DirectoryListingName)
		{
			this.Listing = listing;
			if (listing != null)
			{
				this.Enumerator = listing.GetEnumerator();
				this.Enumerator.Reset();
			}
			else
			{
				this.Close();
				// Invalid resource
			}
		}

        protected override void FreeManaged()
        {
            if (object.ReferenceEquals(this, PhpDirectory.lastDirHandle))
                PhpDirectory.lastDirHandle = null;
        }

		public readonly string[] Listing;
		public readonly System.Collections.IEnumerator Enumerator;

		private const string DirectoryListingName = "stream";
		//private static int DirectoryListingType = PhpResource.RegisterType(DirectoryListingName);
		// Note: PHP uses the stream mechanism listings (opendir etc.)
		// this is the same but a) faster, b) more memory expensive for large directories
		// (and unfinished listings in script)
	}

	#endregion

	/// <summary>
	/// Gives access to the directory manipulation and itereation.
	/// </summary>
	/// <threadsafety static="true"/>
	public static class PhpDirectory
	{
		#region Browsing (getcwd, chdir, NS: chroot)

		/// <summary>Gets the virtual working directory of the current script.</summary>
		/// <remarks></remarks>
		/// <returns>Absolute path to the current directory.</returns>
		[ImplementsFunction("getcwd")]
		public static string GetWorking()
		{
			string result = ScriptContext.CurrentContext.WorkingDirectory;
			return (result != null) ? result : "";
		}

		/// <summary>Changes the virtual working directory for the current script.</summary>
		/// <param name="directory">Absolute or relative path to the new working directory.</param>
		/// <returns>Returns <c>true</c> on success or <c>false</c> on failure.</returns>
		/// <exception cref="PhpException">If the specified directory does not exist.</exception>
		[ImplementsFunction("chdir")]
		public static bool SetWorking(string directory)
		{
			if (directory != null)
			{
				string newPath = PhpPath.AbsolutePath(directory);
				if (System.IO.Directory.Exists(newPath))
				{
					// Note: open_basedir not applied here, URL will not pass through
					ScriptContext.CurrentContext.WorkingDirectory = newPath;
					return true;
				}
			}
			PhpException.Throw(PhpError.Warning, LibResources.GetString("directory_not_found", directory));
			return false;
		}

		/// <summary>
		/// Changes the root directory of the current process to <paramref name="directory"/>.
		/// Not supported.
		/// </summary>
		/// <remarks>
		/// This function is only available if your system supports it 
		/// and you're using the CLI, CGI or Embed SAPI. 
		/// Note: This function is not implemented on Windows platforms.
		/// </remarks>
		/// <param name="directory">The new value of the root directory.</param>
		/// <returns>Returns TRUE on success or FALSE on failure.</returns>
		[ImplementsFunction("chroot", FunctionImplOptions.NotSupported)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static bool SetRoot(string directory)
		{
			PhpException.FunctionNotSupported();
			return false;
		}

		#endregion

		#region Iterating (dir, opendir, readdir, rewinddir, closedir, scandir)

		/// <summary>Returns an object encapsulating the directory listing mechanism on a given
		/// <paramref name="directory"/>.</summary>
		/// <remarks>A pseudo-object oriented mechanism for reading a directory. The given directory is opened. 
		/// Two properties are available once the directory has been opened. The handle property 
		/// can be used with other directory functions such as <c>readdir()</c>, <c>rewinddir()</c> and <c>closedir()</c>. 
		/// The path property is set to path the directory that was opened. 
		/// Three methods are available: <see cref="PHP.Library.Directory.read"/>, 
		/// <see cref="PHP.Library.Directory.rewind"/> and <see cref="PHP.Library.Directory.close"/>.</remarks>
		/// <param name="directory">The path to open for listing.</param>
		/// <returns>An instance of <see cref="PHP.Library.Directory"/>.</returns>
		[ImplementsFunction("dir")]
		public static Directory GetIterator(string directory)
		{
			return new Directory(directory);
		}

        /// <summary>
        /// Last handle opened by <c>opendir</c>.
        /// </summary>
        [ThreadStatic]
        internal static PhpResource lastDirHandle;

		/// <summary>Returns a directory handle to be used in subsequent 
		/// <c>readdir()</c>, <c>rewinddir()</c> and <c>closedir()</c> calls.</summary>
		/// <remarks>
		/// <para>
		/// If path is not a valid directory or the directory can not 
		/// be opened due to permission restrictions or filesystem errors, 
		/// <c>opendir()</c> returns <c>false</c> and generates a PHP error of level <c>E_WARNING</c>. 
		/// </para>
		/// <para>
		/// As of PHP 4.3.0 path can also be any URL which supports directory listing, 
		/// however only the <c>file://</c> url wrapper supports this in PHP 4.3. 
		/// As of PHP 5.0.0, support for the <c>ftp://</c> url wrapper is included as well.
		/// </para>
		/// </remarks>
		/// <param name="directory">The path of the directory to be listed.</param>
		/// <returns>A <see cref="DirectoryListing"/> resource containing the listing.</returns>
		/// <exception cref="PhpException">In case the specified stream wrapper can not be found
		/// or the desired directory can not be opened.</exception>
		[ImplementsFunction("opendir")]
		[return: CastToFalse]
		public static PhpResource Open(string directory)
		{
            lastDirHandle = null;

			StreamWrapper wrapper;
			if (!PhpStream.ResolvePath(ref directory, out wrapper, CheckAccessMode.Directory, CheckAccessOptions.Empty))
				return null;

			string[] listing = wrapper.Listing(directory, 0, null);
			return (listing != null) ? (lastDirHandle = new DirectoryListing(listing)) : null;
		}

        /// <summary>
        /// Reads an entry from a directory handle. Uses last handle opened by <c>opendir</c>.
        /// </summary>
        [ImplementsFunction("readdir")]
        [return: CastToFalse]
        public static string Read()
        {
            return Read(PhpDirectory.lastDirHandle);
        }

		/// <summary>
		/// Reads an entry from a directory handle.
		/// </summary>
		/// <param name="dirHandle">A <see cref="PhpResource"/> returned by <see cref="Open"/>.</param>
		/// <returns>
		/// Returns the path of the next file from the directory. 
		/// The filenames (including . and ..) are returned in the order 
		/// in which they are stored by the filesystem.
		/// </returns>
		[ImplementsFunction("readdir")]
		[return: CastToFalse]
		public static string Read(PhpResource dirHandle)
		{
			IEnumerator enumerator = ValidListing(dirHandle);
			if (enumerator != null && enumerator.MoveNext())
				return enumerator.Current.ToString();
			else
				return null;
		}

        /// <summary>
        /// Rewinds a directory handle. Uses last handle opened by <c>opendir</c>.
        /// </summary>
        [ImplementsFunction("rewinddir")]
        public static void Rewind()
        {
            Rewind(PhpDirectory.lastDirHandle);
        }

		/// <summary>
		/// Rewinds a directory handle.
        /// Function has no return value.
		/// </summary>
		/// <param name="dirHandle">A <see cref="PhpResource"/> returned by <see cref="Open"/>.</param>
		/// <remarks>
		/// Resets the directory stream indicated by <paramref name="dirHandle"/> to the 
		/// beginning of the directory.
		/// </remarks>
		[ImplementsFunction("rewinddir")]
		public static void Rewind(PhpResource dirHandle)
		{
			IEnumerator enumerator = ValidListing(dirHandle);
			if (enumerator == null) return;
			enumerator.Reset();
		}

        /// <summary>
        /// Closes a directory handle. Uses last handle opened by <c>opendir</c>.
		/// </summary>
        [ImplementsFunction("closedir")]
        public static void Close()
        {
            Close(PhpDirectory.lastDirHandle);
        }

		/// <summary>
		/// Closes a directory handle.
        /// Function has no return value.
		/// </summary>
		/// <param name="dirHandle">A <see cref="PhpResource"/> returned by <see cref="Open"/>.</param>
		/// <remarks>
		/// Closes the directory stream indicated by <paramref name="dirHandle"/>. 
		/// The stream must have previously been opened by by <see cref="Open"/>.
		/// </remarks>
		[ImplementsFunction("closedir")]
		public static void Close(PhpResource dirHandle)
		{
			// Note: PHP allows other all stream resources to be closed with closedir().
			IEnumerator enumerator = ValidListing(dirHandle);
			if (enumerator == null) return;
			dirHandle.Close(); // releases the DirectoryListing and sets to invalid.
		}

		/// <summary>Lists files and directories inside the specified <paramref name="directory"/>.</summary>
		/// <remarks>
		/// Returns an array of files and directories from the <paramref name="directory"/>. 
		/// If <paramref name="directory"/> is not a directory, then boolean <c>false</c> is returned, 
		/// and an error of level <c>E_WARNING</c> is generated. 
		/// </remarks>
		/// <param name="directory">The directory to be listed.</param>
		/// <returns>A <see cref="PhpArray"/> of filenames or <c>false</c> in case of failure.</returns>
		[ImplementsFunction("scandir")]
		[return: CastToFalse]
		public static PhpArray Scan(string directory)
		{
			return Scan(directory, 0);
		}

		/// <summary>Lists files and directories inside the specified path.</summary>
		/// <remarks>
		/// Returns an array of files and directories from the <paramref name="directory"/>. 
		/// If <paramref name="directory"/> is not a directory, then boolean <c>false</c> is returned, 
		/// and an error of level <c>E_WARNING</c> is generated. 
		/// </remarks>
		/// <param name="directory">The directory to be listed.</param>
		/// <param name="sorting_order">
		/// By default, the listing is sorted in ascending alphabetical order. 
		/// If the optional sorting_order is used (set to <c>1</c>), 
		/// then sort order is alphabetical in descending order.</param>
		/// <returns>A <see cref="PhpArray"/> of filenames or <c>false</c> in case of failure.</returns>
		/// <exception cref="PhpException">In case the specified stream wrapper can not be found
		/// or the desired directory can not be opened.</exception>
		[ImplementsFunction("scandir")]
		[return: CastToFalse]
		public static PhpArray Scan(string directory, int sorting_order)
		{
			StreamWrapper wrapper;
			if (!PhpStream.ResolvePath(ref directory, out wrapper, CheckAccessMode.Directory, CheckAccessOptions.Empty))
				return null;

			string[] listing = wrapper.Listing(directory, 0, null);

			if (listing != null)
			{
				PhpArray ret = new PhpArray(listing); // create the array from the system one
				if (sorting_order == 1)
				{
					PhpArrays.ReverseSort(ret, ComparisonMethod.String);
				}
				else
				{
					PhpArrays.Sort(ret, ComparisonMethod.String);
				}
				return ret;
			}
			return null; // false
		}

		/// <summary>
		/// Casts the given resource handle to the <see cref="DirectoryListing"/> enumerator.
		/// Throw an exception when a wrong argument is supplied.
		/// </summary>
		/// <param name="dir_handle">The handle passed to a PHP function.</param>
		/// <returns>The enumerator over the files in the DirectoryListing.</returns>
		/// <exception cref="PhpException">When the supplied argument is not a valid <see cref="DirectoryListing"/> resource.</exception>
		private static System.Collections.IEnumerator ValidListing(PhpResource dir_handle)
		{
			DirectoryListing listing = dir_handle as DirectoryListing;
			if (listing != null) return listing.Enumerator;

			PhpException.Throw(PhpError.Warning, LibResources.GetString("invalid_directory_resource"));
			return null;
		}

		#endregion

		#region Manipulating (mkdir, rmdir)

		/// <summary>
		/// Makes a new directory.
		/// </summary>
		/// <param name="pathname">The directory to create.</param>
		/// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
		[ImplementsFunction("mkdir")]
		public static bool MakeDirectory(string pathname)
		{
			return MakeDirectory(pathname, (int)FileModeFlags.ReadWriteExecute, false, StreamContext.Default);
		}

		/// <summary>
		/// Makes a directory or a branch of directories using the specified wrapper.
		/// </summary>
		/// <param name="pathname">The path to create.</param>
		/// <param name="mode">A combination of <see cref="StreamMakeDirectoryOptions"/>.</param>
		/// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
		[ImplementsFunction("mkdir")]
		public static bool MakeDirectory(string pathname, int mode)
		{
			return MakeDirectory(pathname, mode, false, StreamContext.Default);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="pathname"></param>
		/// <param name="mode"></param>
		/// <param name="recursive"></param>
		/// <returns></returns>
		[ImplementsFunction("mkdir")]
		public static bool MakeDirectory(string pathname, int mode, bool recursive)
		{
			return MakeDirectory(pathname, mode, recursive, StreamContext.Default);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="pathname"></param>
		/// <param name="mode"></param>
		/// <param name="recursive"></param>
		/// <param name="context"></param>
		/// <returns></returns>
		[ImplementsFunction("mkdir")]
		public static bool MakeDirectory(string pathname, int mode, bool recursive, PhpResource context)
		{
			StreamWrapper wrapper;
			if (!PhpStream.ResolvePath(ref pathname, out wrapper, CheckAccessMode.Directory, CheckAccessOptions.Empty))
				return false;

			return wrapper.MakeDirectory(pathname, mode, recursive ?
			  StreamMakeDirectoryOptions.Recursive : StreamMakeDirectoryOptions.Empty, StreamContext.Default);
		}

		/// <summary>
		/// Removes a directory.
		/// </summary>
		/// <param name="dirname"></param>
		/// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
		[ImplementsFunction("rmdir")]
		public static bool RemoveDirectory(string dirname)
		{
			return RemoveDirectory(dirname, StreamContext.Default);
		}

		/// <summary>
		/// Removes a directory.
		/// </summary>
		/// <param name="dirname"></param>
		/// <param name="context"></param>
		/// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
		[ImplementsFunction("rmdir")]
		public static bool RemoveDirectory(string dirname, StreamContext context)
		{
			StreamWrapper wrapper;
			if (!PhpStream.ResolvePath(ref dirname, out wrapper, CheckAccessMode.Directory, CheckAccessOptions.Empty))
				return false;

			return wrapper.RemoveDirectory(dirname, StreamRemoveDirectoryOptions.Empty, StreamContext.Default);
		}

		#endregion
	}
}
