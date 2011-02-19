/*

 Copyright (c) 2004-2005 Jan Benda.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace PHP.Core
{
	#region PhpStream (partial) class

	/// <summary>
	/// Abstraction of streaming behavior for PHP.
	/// PhpStreams are opened by StreamWrappers on a call to fopen().
	/// </summary>
	public abstract partial class PhpStream : PhpResource
	{
		#region Opening utilities
		
		/// <summary>
		/// Merges the path with the current working directory
		/// to get a canonicalized absolute pathname representing the same file.
		/// </summary>
		/// <remarks>
		/// This method is an analogy of <c>main/safe_mode.c: php_checkuid</c>.
		/// Looks for the file in the <c>include_path</c> and checks for <c>open_basedir</c> restrictions.
		/// </remarks>
		/// <param name="path">An absolute or relative path to a file.</param>
		/// <param name="wrapper">The wrapper found for the specified file or <c>null</c> if the path resolution fails.</param>
		/// <param name="mode">The checking mode of the <see cref="CheckAccess"/> method (file, directory etc.).</param>
		/// <param name="options">Additional options for the <see cref="CheckAccess"/> method.</param>
		/// <returns><c>true</c> if all the resolution and checking passed without an error, <b>false</b> otherwise.</returns>
		/// <exception cref="PhpException">Security violation - when the target file 
		/// lays outside the tree defined by <c>open_basedir</c> configuration option.</exception>
		public static bool ResolvePath(ref string path, out StreamWrapper wrapper, CheckAccessMode mode, CheckAccessOptions options)
		{
			// Path will contain the absolute path without file:// or the complete URL; filename is the relative path.
			string filename, scheme = GetSchemeInternal(path, out filename);
			wrapper = StreamWrapper.GetWrapper(scheme, (StreamOptions)options);
			if (wrapper == null) return false;

			if (wrapper.IsUrl)
			{
				// Note: path contains the whole URL, filename the same without the scheme:// portion.
				// What to check more?
			}
			else if (scheme != "php")
			{
				// SILVERLIGHT: ?? what to do here ??
				// PhpException.Throw(PhpError.Warning, CoreResources.GetString("stream_file_access_denied", path));
			}

			return true;
		}

		#endregion
	}

	#endregion
}
