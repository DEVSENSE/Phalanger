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
using System.IO.IsolatedStorage;

namespace PHP.Core
{
	#region Local Filesystem Wrapper

	/// <summary>
	/// Derived from <see cref="StreamWrapper"/>, this class provides access to 
	/// the local filesystem files.
	/// </summary>
	public partial class FileStreamWrapper : StreamWrapper
	{
		#region Opening a file

		private IsolatedStorageFile storageFile;

		/// <summary>
		/// Dispose - close the isolated storage handle
		/// </summary>
		public override void Dispose()
		{
			storageFile.Dispose();
		}

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
			this.storageFile = IsolatedStorageFile.GetUserStoreForApplication();
			FileStream stream = null;
			try
			{
				stream = new IsolatedStorageFileStream(path, fileMode, fileAccess, FileShare.ReadWrite | FileShare.Delete, storageFile);
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
	}

	#endregion
}