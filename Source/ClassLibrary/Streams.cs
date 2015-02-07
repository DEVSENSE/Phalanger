/*

 Copyright (c) 2004-2005 Jan Benda and Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

/* 
 TODO:
  - Added offset parameter to the stream_copy_to_stream() function. (PHP 5.1.0)
  - Changed stream_filter_(ap|pre)pend() to return resource. (Sara) 
  - Fixed a bug where stream_get_meta_data() did not return the "uri" element for files opened with tmpname(). (Derick) 
  - Fixed crash inside stream_get_line() when length parameter equals 0. (Ilia) 
  - Added (PHP 5.1.0):
      stream_context_get_default() (Wez) 
      stream_wrapper_unregister() (Sara) 
      stream_wrapper_restore() (Sara) 
      stream_filter_remove() (Sara) 
  - Added proxy support to ftp wrapper via http. (Sara) 
  - Added MDTM support to ftp_url_stat. (Sara) 
  - Added zlib stream filter support. (Sara) 
  - Added bz2 stream filter support. (Sara) 
  - Added bindto socket context option. (PHP 5.1.0)
  - Added HTTP/1.1 and chunked encoding support to http:// wrapper. (PHP 5.1.0)
  
 NOTES:
  PhpStream is derived from PhpResource,
  it contains a Stream descendant, a StreamContext (may be empty)
  and an ordered list of PhpFilters. PhpStream may be cast to
  a regular stream (using its RawStream property).

  PhpStream is created by a StreamWrapper on a call to fopen().
  Wrappers are stateless: they provide an instance of PhpStream
  on fopen() and an instance of DirectoryListing on opendir().
  
  PHP Stream functions are implemented as static methods of corresponding
  classes - wrappers are registered by StreamWrapper and so on.
  
  User-defined wrappers are stored in the ScriptContext.
  So are the user-defined filters. And finally a working directory too.
  
  EX: Notification callback: void my_notifier 
    ( int notification_code, int severity, string message, 
    int message_code, int bytes_transferred, int bytes_max)
  It is a parameter of StreamContext.

*/
using System;
using System.IO;
using System.Text;
using System.Net;
//using System.Net.Sockets;
using System.Collections;
using PHP.Core;
using PHP.Core.Reflection;

namespace PHP.Library
{
	#region Enums

	/// <summary>
	/// The "whence" options used in PhpStream.Seek().
	/// </summary>
	public enum SeekOptions
	{
		/// <summary>Seek from the beginning of the file.</summary>
		[ImplementsConstant("SEEK_SET")]
		Set = SeekOrigin.Begin,   // 0 (OK)
		/// <summary>Seek from the current position.</summary>
		[ImplementsConstant("SEEK_CUR")]
		Current = SeekOrigin.Current, // 1 (OK)
		/// <summary>Seek from the end of the file.</summary>
		[ImplementsConstant("SEEK_END")]
		End = SeekOrigin.End      // 2 (OK)
	}

	/// <summary>
	/// Value used as an argument to <c>flock()</c> calls.
	/// Passed to streams using the <see cref="PhpStream.SetParameter"/>
	/// with <c>option</c> set to <see cref="StreamParameterOptions.Locking"/>.
	/// </summary>
	/// <remarks>
	/// Note that not all of these are flags. Only the <see cref="StreamLockOptions.NoBlocking"/> 
	/// may be added to one of the first three values.
	/// </remarks>
	[Flags]
	public enum StreamLockOptions
	{
		/// <summary>
		/// To acquire a shared lock (reader), set operation to LOCK_SH.
		/// </summary>
		[ImplementsConstant("LOCK_SH")]
		Shared = 1,

		/// <summary>
		/// To acquire an exclusive lock (writer), set operation to LOCK_EX.
		/// </summary>
		[ImplementsConstant("LOCK_EX")]
		Exclusive = 2,

		/// <summary>
		/// To release a lock (shared or exclusive), set operation to LOCK_UN.
		/// </summary>
		[ImplementsConstant("LOCK_UN")]
		Unlock = 3,

		/// <summary>
		/// If you don't want flock() to block while locking, add LOCK_NB to operation.
		/// </summary> 
		[ImplementsConstant("LOCK_NB")]
		NoBlocking = 4
	}

	/// <summary>
	/// ImplementsConstant enumeration for various PHP stream-related constants.
	/// </summary>
	[Flags]
	public enum PhpStreamConstants
	{
		/// <summary>Empty option (default)</summary>
		Empty = 0,
		/// <summary>If path is relative, Wrapper will search for the resource using the include_path (1).</summary>
		[ImplementsConstant("STREAM_USE_PATH")]
		UseIncludePath = StreamOptions.UseIncludePath,
		/// <summary>When this flag is set, only the file:// wrapper is considered. (2)</summary>
		[ImplementsConstant("STREAM_IGNORE_URL")]
		IgnoreUrl = StreamOptions.IgnoreUrl,
		/// <summary>Apply the <c>safe_mode</c> permissions check when opening a file (4).</summary>
		[ImplementsConstant("STREAM_ENFORCE_SAFE_MODE")]
		EnforceSafeMode = StreamOptions.EnforceSafeMode,
		/// <summary>If this flag is set, the Wrapper is responsible for raising errors using 
		/// trigger_error() during opening of the stream. If this flag is not set, she should not raise any errors (8).</summary>
		[ImplementsConstant("STREAM_REPORT_ERRORS")]
		ReportErrors = StreamOptions.ReportErrors,
		/// <summary>If you don't need to write to the stream, but really need to 
		/// be able to seek, use this flag in your options (16).</summary>
		[ImplementsConstant("STREAM_MUST_SEEK")]
		MustSeek = StreamOptions.MustSeek,

		/// <summary>Stat the symbolic link itself instead of the linked file (1).</summary>
		[ImplementsConstant("STREAM_URL_STAT_LINK")]
		StatLink = StreamStatOptions.Link,
		/// <summary>Do not complain if the file does not exist (2).</summary>
		[ImplementsConstant("STREAM_URL_STAT_QUIET")]
		StatQuiet = StreamStatOptions.Quiet,

		/// <summary>Create the whole path leading to the specified directory if necessary (1).</summary>
		[ImplementsConstant("STREAM_MKDIR_RECURSIVE")]
		MakeDirectoryRecursive = StreamMakeDirectoryOptions.Recursive
	}

	public enum StreamEncryption
	{
		[ImplementsConstant("STREAM_CRYPTO_METHOD_SSLv2_CLIENT")]
		ClientSSL2,
		[ImplementsConstant("STREAM_CRYPTO_METHOD_SSLv3_CLIENT")]
		ClientSSL3,
		[ImplementsConstant("STREAM_CRYPTO_METHOD_SSLv23_CLIENT")]
		ClientSSL23,
		[ImplementsConstant("STREAM_CRYPTO_METHOD_TLS_CLIENT")]
		ClientTSL,
		[ImplementsConstant("STREAM_CRYPTO_METHOD_SSLv2_SERVER")]
		ServerSSL2,
		[ImplementsConstant("STREAM_CRYPTO_METHOD_SSLv3_SERVER")]
		ServerSSL3,
		[ImplementsConstant("STREAM_CRYPTO_METHOD_SSLv23_SERVER")]
		ServerSSL23,
		[ImplementsConstant("STREAM_CRYPTO_METHOD_TLS_SERVER")]
		ServerTSL
	}

	#endregion

	#region Steam Filter functions

	#region Options and status

	/// <summary>
	/// The status indicators returned by filter's main method.
	/// </summary>
	public enum FilterStatus
	{
		/// <summary>
		/// Error in data stream (1).
		/// </summary>
		[ImplementsConstant("PSFS_ERR_FATAL")]
		FatalError,

		/// <summary>
		/// Filter needs more data; stop processing chain until more is available (2).
		/// </summary>
		[ImplementsConstant("PSFS_FEED_ME")]
		MoreData,

		/// <summary>
		/// Filter generated output buckets; pass them on to next in chain (3).
		/// </summary>
		[ImplementsConstant("PSFS_PASS_ON")]
		OK
	}

	/// <summary>
	/// Indicates whether the filter is to be attached to the
	/// input/ouput filter-chain or both.
	/// </summary>
	[Flags]
	public enum FilterChains
	{
		/// <summary>
		/// Insert the filter to the read filter chain of the stream (1).
		/// </summary>
		[ImplementsConstant("STREAM_FILTER_READ")]
		Read = FilterChainOptions.Read,

		/// <summary>
		/// Insert the filter to the write filter chain of the stream (2).
		/// </summary>
		[ImplementsConstant("STREAM_FILTER_WRITE")]
		Write = FilterChainOptions.Write,

		/// <summary>
		/// Insert the filter to both the filter chains of the stream (3).
		/// </summary>
		[ImplementsConstant("STREAM_FILTER_ALL")]
		ReadWrite = Read | Write
	}

	#endregion

	/// <summary>
	/// Gives access to the stream filter chains.
	/// </summary>
	/// <threadsafety static="true"/>
	public static class PhpFilters
	{
		#region stream_filter_append, stream_filter_prepend

		/// <include file='Doc/Streams.xml' path='docs/method[@name="Append"]/*'/>
		[ImplementsFunction("stream_filter_append")]
        public static bool Append(PhpResource stream, string filter)
        {
            return Append(stream, filter, (int)FilterChainOptions.ReadWrite, null);
        }

		/// <include file='Doc/Streams.xml' path='docs/method[@name="Append"]/*'/>
		/// <param name="read_write">Combination of the <see cref="FilterChainOptions"/> flags.</param>
		[ImplementsFunction("stream_filter_append")]
		public static bool Append(PhpResource stream, string filter, int read_write)
		{
			return Append(stream, filter, read_write, null);
		}

		/// <include file='Doc/Streams.xml' path='docs/method[@name="Append"]/*'/>
		/// <param name="read_write">Combination of the <see cref="FilterChainOptions"/> flags.</param>
		/// <param name="parameters">Additional parameters for a user filter.</param>
		[ImplementsFunction("stream_filter_append")]
		public static bool Append(PhpResource stream, string filter, int read_write, object parameters)
		{
			PhpStream s = PhpStream.GetValid(stream);
			if (s == null) return false;

			FilterChainOptions where = (FilterChainOptions)read_write & FilterChainOptions.ReadWrite;
			return PhpFilter.AddToStream(s, filter, where | FilterChainOptions.Tail, parameters);
		}


		/// <include file='Doc/Streams.xml' path='docs/method[@name="Prepend"]/*'/>
		[ImplementsFunction("stream_filter_prepend")]
		public static bool Prepend(PhpResource stream, string filter)
		{
			return Prepend(stream, filter, (int)FilterChainOptions.ReadWrite, null);
		}

		/// <include file='Doc/Streams.xml' path='docs/method[@name="Prepend"]/*'/>
		/// <param name="read_write">Combination of the <see cref="FilterChainOptions"/> flags.</param>
		[ImplementsFunction("stream_filter_prepend")]
		public static bool Prepend(PhpResource stream, string filter, int read_write)
		{
			return Prepend(stream, filter, read_write, null);
		}

		/// <include file='Doc/Streams.xml' path='docs/method[@name="Prepend"]/*'/>
		/// <param name="read_write">Combination of the <see cref="FilterChainOptions"/> flags.</param>
		/// <param name="parameters">Additional parameters for a user filter.</param>
		[ImplementsFunction("stream_filter_prepend")]
		public static bool Prepend(PhpResource stream, string filter, int read_write, object parameters)
		{
			PhpStream s = PhpStream.GetValid(stream);
			if (s == null) return false;

			FilterChainOptions where = (FilterChainOptions)read_write & FilterChainOptions.ReadWrite;
			return PhpFilter.AddToStream(s, filter, where | FilterChainOptions.Head, parameters);
		}

		#endregion

		#region stream_filter_register, stream_get_filters

		/// <summary>
		/// Registers a user stream filter.
		/// </summary>
		/// <param name="filter">The name of the filter (may contain wildcards).</param>
		/// <param name="classname">The PHP user class (derived from <c>php_user_filter</c>) implementing the filter.</param>
		/// <returns><c>true</c> if the filter was succesfully added, <c>false</c> if the filter of such name already exists.</returns>
		[ImplementsFunction("stream_filter_register")]
		public static bool Register(string filter, string classname)
		{
			// EX: [stream_filter_register]

			return PhpFilter.AddUserFilter(filter, classname);
		}

		/// <summary>
		/// Retrieves the list of registered filters.
		/// </summary>
		/// <returns>A <see cref="PhpArray"/> containing the names of available filters.</returns>
		[ImplementsFunction("stream_get_filters")]
		public static PhpArray GetFilterNames()
		{
			return PhpFilter.GetFilterNames();
		}

		#endregion
	}

	#region Stream Filter Implementations

	#region String Filters
	/// <summary>
	/// Options provided to the constructor of <see cref="StringFilter"/>
	/// to specify which string conversion to use.
	/// </summary>
	public enum StringFilterOptions
	{
		/// <summary>Use the <c>str_rot13</c> function to alter the stream data.</summary>
		Rotate13 = 1,
		/// <summary>Use the <c>strtoupper</c> function to alter the stream data.</summary>
		ToUpper = 2,
		/// <summary>Use the <c>strtolower</c> function to alter the stream data.</summary>
		ToLower = 3,
		/// <summary>Use the <c>strip_tags</c> function with an additional <c>allowable_tags</c>
		/// argument to alter the stream data.</summary>
		StripTags = 4
	}

	/// <summary>
	/// Encapsulates built-in "string.*" filters. See <see cref="StringFilterOptions"/>
	/// for the list of possible filter operations.
	/// </summary>
	/// <threadsafety static="true"/>
	public sealed class StringFilter : PhpFilter
	{
		/// <summary>Creates a new instance of this filter with additional parameters.</summary>
		public StringFilter(StringFilterOptions operation, object parameters)
			: base(AlterParameters(operation, parameters))
		{
			this.operation = operation;
		}

		private static object AlterParameters(StringFilterOptions operation, object parameters)
		{
			if (operation == StringFilterOptions.StripTags)
			{
				// Convert the given tags to the format expected by strip_tags
				PhpArray tags = (parameters as PhpArray);
				if (tags != null)
				{
					StringBuilder sb = new StringBuilder();
					foreach (object o in tags)
					{
						sb.Append('<');
						sb.Append(Core.Convert.ObjectToString(o));
						sb.Append('>');
					}
					return sb.ToString();
				}
				else
				{
					// The allowable_tags is expected to be a string
					return Core.Convert.ObjectToString(parameters);
				}
			}

			return parameters;
		}

		/// <include file='Doc/Streams.xml' path='docs/method[@name="Filter"]/*'/>
		public override object Filter(object input, bool closing)
		{
			string str = PhpStream.AsText(input);
			switch (operation)
			{
				case StringFilterOptions.Rotate13:
					str = PhpStrings.Rotate13(str);
					break;
				case StringFilterOptions.ToUpper:
					str = str.ToUpper();
					break;
				case StringFilterOptions.ToLower:
					str = str.ToLower();
					break;
				case StringFilterOptions.StripTags:
					str = PhpStrings.StripTags(str, (string)parameters, ref stripTagsState);
					break;
				default:
					Debug.Assert(false);
					break;
			}
			return str;
		}

		/// <summary>The selected operation of this string-filter.</summary>
		private readonly StringFilterOptions operation;

		/// <summary>The stored state of the last <c>strip_tags</c> function call.</summary>
		private int stripTagsState = 0;
	}

	/// <summary>
	/// Factory for string stream filters.
	/// </summary>
	/// <threadsafety static="true"/>
	public sealed class StringFilterFactory : IFilterFactory
	{
		#region Implemented Filters Access
		/// <summary>
		/// Returns the list of filters defined by this PhpFilter.
		/// </summary>
		/// <returns></returns>
		public string[] GetImplementedFilterNames()
		{
			return new string[]
    {
      "string.toupper",
      "string.tolower",
      "string.rot13",
      "string.strip-tags"
    };
		}

		/// <summary>
		/// Returns a filter implementation defined by this PhpFilter.
		/// </summary>
		/// <param name="name">Name of the filter to look for.</param>
		/// <param name="instantiate"><c>true</c> to fill <paramref name="instance"/> with a new instance of that filter.</param>
		/// <param name="instance">Filled with a new instance of an implemented filter if <paramref name="instantiate"/>.</param>
		/// <param name="parameters">Additional parameters provided to the filter constructor.</param>
		/// <returns><c>true</c> if a filter with the given name was found.</returns>
		public bool GetImplementedFilter(string name, bool instantiate, out PhpFilter instance, object parameters)
		{
			instance = null;
			switch (name)
			{
				case "string.toupper":
					if (instantiate) instance = new StringFilter(StringFilterOptions.ToUpper, parameters);
					return true;
				case "string.tolower":
					if (instantiate) instance = new StringFilter(StringFilterOptions.ToLower, parameters);
					return true;
				case "string.rot13":
					if (instantiate) instance = new StringFilter(StringFilterOptions.Rotate13, parameters);
					return true;
				case "string.strip-tags":
					if (instantiate) instance = new StringFilter(StringFilterOptions.StripTags, parameters);
					return true;
			}
			return false;
		}
		#endregion
	}

	#endregion

	#region Conversion Filters
	/// <summary>
	/// Options provided to the constructor of <see cref="EncodingFilter"/>
	/// or <see cref="DecodingFilter"/> to specify which conversion to use.
	/// </summary>
	public enum ConversionFilterOptions
	{
		/// <summary>Use the <c>base-64</c> encoding.</summary>
		Base64 = 1,
		/// <summary>Use the <c>quoted-printable</c> encoding. Only decoding is implemented.</summary>
		QuotedPrintable = 2,
	}

	/// <summary>
	/// Encapsulates built-in "convert.*" filters performing the decoding
	/// (string to binary) conversions. See <see cref="ConversionFilterOptions"/>
	/// for the list of possible filter operations.
	/// </summary>
	/// <threadsafety static="true"/>
	public sealed class DecodingFilter : PhpFilter
	{
		/// <summary>Creates a new instance of this filter.</summary>
		public DecodingFilter(ConversionFilterOptions operation)
			: base(null)
		{
			this.operation = operation;
		}

		/// <include file='Doc/Streams.xml' path='docs/method[@name="Filter"]/*'/>
		public override object Filter(object input, bool closing)
		{
			string str = PhpStream.AsText(input);
			switch (operation)
			{
				case ConversionFilterOptions.Base64:
					return new PhpBytes(System.Convert.FromBase64String(str));
				// PHP5 supports this function with additional parameters too.
				case ConversionFilterOptions.QuotedPrintable:
					return PhpStrings.QuotedPrintableDecode(str);
				// PHP5 supports this function with additional parameters too.
				default:
					Debug.Assert(false);
					break;
			}
			return str;
		}

		/// <summary>The selected operation of this conversion filter.</summary>
		private readonly ConversionFilterOptions operation;
	}

	/// <summary>
	/// Factory for decoding stream filters.
	/// </summary>
	/// <threadsafety static="true"/>
	public sealed class DecodingFilterFactory : IFilterFactory
	{
		#region Implemented Filters Access
		/// <summary>
		/// Returns the list of filters defined by this PhpFilter.
		/// </summary>
		/// <returns></returns>
		public string[] GetImplementedFilterNames()
		{
			return new string[]
    {
      "convert.base64-decode"
      //          "convert.quoted-printable-decode"
    };
		}

		/// <summary>
		/// Returns a filter implementation defined by this PhpFilter.
		/// </summary>
		/// <param name="name">Name of the filter to look for.</param>
		/// <param name="instantiate"><c>true</c> to fill <paramref name="instance"/> with a new instance of that filter.</param>
		/// <param name="instance">Filled with a new instance of an implemented filter if <paramref name="instantiate"/>.</param>
		/// <param name="parameters">Additional parameters provided to the filter constructor.</param>
		/// <returns><c>true</c> if a filter with the given name was found.</returns>
		public bool GetImplementedFilter(string name, bool instantiate, out PhpFilter instance, object parameters)
		{
			instance = null;
			switch (name)
			{
				case "convert.base64-decode":
					if (instantiate) instance = new DecodingFilter(ConversionFilterOptions.Base64);
					return true;
				case "convert.quoted-printable-decode":
					if (instantiate) instance = new DecodingFilter(ConversionFilterOptions.QuotedPrintable);
					return true;
			}
			return false;
		}
		#endregion
	}

	/// <summary>
	/// Encapsulates built-in "convert.*" filters performing the encoding
	/// (binary to string) conversions. See <see cref="ConversionFilterOptions"/>
	/// for the list of possible filter operations.
	/// </summary>
	/// <threadsafety static="true"/>
	public sealed class EncodingFilter : PhpFilter
	{
		/// <summary>Creates a new instance of this filter.</summary>
		public EncodingFilter(ConversionFilterOptions operation)
			: base(null)
		{
			this.operation = operation;
		}

		/// <include file='Doc/Streams.xml' path='docs/method[@name="Filter"]/*'/>
		public override object Filter(object input, bool closing)
		{
			PhpBytes bin = PhpStream.AsBinary(input);
			switch (operation)
			{
				case ConversionFilterOptions.Base64:
                    return System.Convert.ToBase64String(bin.ReadonlyData);
				// EX: PHP5 supports this function with additional parameters.
				case ConversionFilterOptions.QuotedPrintable:
					PhpException.FunctionNotSupported();
					// EX: PHP5 supports this function with additional parameters.
					break;
				default:
					Debug.Assert(false);
					break;
			}
			return bin;
		}

		/// <summary>The selected operation of this conversion filter.</summary>
		private readonly ConversionFilterOptions operation;
	}

	/// <summary>
	/// Factory for encoding stream filters.
	/// </summary>
	/// <threadsafety static="true"/>
	public sealed class EncodingFilterFactory : IFilterFactory
	{
		#region Implemented Filters Access
		/// <summary>
		/// Returns the list of filters defined by this PhpFilter.
		/// </summary>
		/// <returns></returns>
		public string[] GetImplementedFilterNames()
		{
			return new string[]
    {
      "convert.base64-encode"
      // "convert.quoted-printable-encode"
    };
		}

		/// <summary>
		/// Returns a filter implementation defined by this PhpFilter.
		/// </summary>
		/// <param name="name">Name of the filter to look for.</param>
		/// <param name="instantiate"><c>true</c> to fill <paramref name="instance"/> with a new instance of that filter.</param>
		/// <param name="instance">Filled with a new instance of an implemented filter if <paramref name="instantiate"/>.</param>
		/// <param name="parameters">Additional parameters provided to the filter constructor.</param>
		/// <returns><c>true</c> if a filter with the given name was found.</returns>
		public bool GetImplementedFilter(string name, bool instantiate, out PhpFilter instance, object parameters)
		{
			instance = null;
			switch (name)
			{
				case "convert.base64-encode":
					if (instantiate) instance = new EncodingFilter(ConversionFilterOptions.Base64);
					return true;
				//        case "convert.quoted-printable-encode":
				//          if (instantiate) instance = new EncodingFilter(ConversionFilterOptions.QuotedPrintable);
				//          return true;
			}
			return false;
		}
		#endregion
	}

	#endregion

	#endregion

	#endregion

	#region Stream Context functions

	/// <summary>
	/// Class containing implementations of PHP functions accessing the <see cref="StreamContext"/>s.
	/// </summary>
	/// <threadsafety static="true"/>
	public static class PhpContexts
	{
		#region stream_context_create

		[ImplementsFunction("stream_context_create")]
		public static PhpResource CreateContext()
		{
			return CreateContext(null);
		}

		/// <summary>Create a new stream context.</summary>
		/// <param name="data">The 2-dimensional array in format "options[wrapper][option]".</param>
		[ImplementsFunction("stream_context_create")]
		public static PhpResource CreateContext(PhpArray data)
		{
			if (data == null)
				return StreamContext.Default;

			// OK, data lead to a valid stream-context.
			if (CheckContextData(data))
				return new StreamContext(data);

			// Otherwise..
			PhpException.Throw(PhpError.Warning, LibResources.GetString("invalid_context_resource"));
			return null;
		}

		/// <summary>
		/// Check whether the provided argument is a valid stream-context data array.
		/// </summary>
		/// <param name="data">The data to be stored into context.</param>
		/// <returns></returns>
		private static bool CheckContextData(PhpArray data)
		{
			// Check if the supplied data are correctly formed.
			foreach (object o in data.Values)
			{
				if (!(o is PhpArray))
					return false;
				// Invalid resource - not an array of arrays
			}
			return true;
		}

		/// <summary>
		/// Get the StreamContext from a handle representing either an isolated context or a PhpStream.
		/// </summary>
		/// <param name="stream_or_context">The PhpResource of either PhpStream or StreamContext type.</param>
		/// <param name="createContext">If true then a new context will be created at the place of <see cref="StreamContext.Default"/>.</param>
		/// <returns>The respective StreamContext.</returns>
		/// <exception cref="PhpException">If the first argument is neither a stream nor a context.</exception>
		private static StreamContext FromResource(PhpResource stream_or_context, bool createContext)
		{
			if ((stream_or_context != null) && (stream_or_context.IsValid))
			{
				// Get the context out of the stream
				PhpStream stream = stream_or_context as PhpStream;
				if (stream != null)
				{
					Debug.Assert(stream.Context != null);
					stream_or_context = stream.Context;
				}

				StreamContext context = stream_or_context as StreamContext;
				if (context == StreamContext.Default)
				{
					if (!createContext) return null;
					context = new StreamContext();
				}
				return context;
			}
			PhpException.Throw(PhpError.Warning, CoreResources.GetString("context_expected"));
			return null;
		}

		private static PhpArray GetContextData(PhpResource stream_or_context)
		{
			// Always create a new context if there is the Default one.
			StreamContext context = FromResource(stream_or_context, true);

			// Now create the data if this is a "lazy context".
			if (context != null)
			{
				if (context.Data == null)
					context.Data = new PhpArray(0, 4);
				return context.Data;
				// Now it is OK.
			}
			return null;
		}

		#endregion

		#region stream_context_get_options, stream_context_set_option, stream_context_set_params

		/// <summary>
		/// Retrieve options for a stream-wrapper or a context itself.
		/// </summary>  
		/// <param name="stream_or_context">The PhpResource of either PhpStream or StreamContext type.</param>
		/// <returns>The contained PhpArray of options.</returns>
		[ImplementsFunction("stream_context_get_options")]
		public static PhpArray GetContextOptions(PhpResource stream_or_context)
		{
			// Do not create a new context if there is the Default one.
			StreamContext context = FromResource(stream_or_context, false);
			return context != null ? context.Data : null;
		}

		/// <summary>
		/// Sets an option for a stream/wrapper/context.
		/// </summary> 
		/// <param name="stream_or_context">The PhpResource of either PhpStream or StreamContext type.</param>
		/// <param name="wrapper">The first-level index to the options array.</param>
		/// <param name="option">The second-level index to the options array.</param>
		/// <param name="data">The data to be stored to the options array.</param>
		/// <returns>True on success.</returns>
		[ImplementsFunction("stream_context_set_option")]
		public static bool SetContextOption(PhpResource stream_or_context, string wrapper, string option, object data)
		{
			// OK, creates the context if Default, so that Data is always a PhpArray.
			// Fails only if the first argument is not a stream nor context.
			PhpArray context_data = GetContextData(stream_or_context);
			if (context_data == null) return false;

			if (context_data.ContainsKey(wrapper))
			{
				// Inserts the option key if necessary.
				(context_data[wrapper] as PhpArray)[option] = data;
			}
			else
			{
				// Create the second-level array and fill it with the given data.
				PhpArray options = new PhpArray(0, 4);
				options.Add(option, data);
				context_data.Add(wrapper, options);
			}
			return true;
		}

		/// <summary>
		/// Set parameters for a stream/wrapper/context.
		/// </summary>
		[ImplementsFunction("stream_context_set_params")]
		public static bool SetContextParameters(PhpResource stream_or_context, PhpArray parameters)
		{
			// Create the context if the stream does not have one.
			StreamContext context = FromResource(stream_or_context, true);

			if ((context != null) && (context.IsValid))
			{
				context.Parameters = parameters;
				return true;
			}
			return false;
		}

		#endregion
	}

	#endregion

	#region Stream Wrapper functions

	/// <summary>
	/// Class containing implementations of PHP functions accessing the <see cref="StreamWrapper"/>s.
	/// </summary>
	/// <threadsafety static="true"/>
	public static class PhpWrappers
	{
		#region stream_wrapper_register, stream_register_wrapper, stream_get_wrappers

        /// <summary>
        /// Optional flag for <c>stream_wrapper_register</c> function.
        /// </summary>
        public enum StreamWrapperRegisterFlags : int
        {
            Default = 0,

            [ImplementsConstant("STREAM_IS_URL")]
            IsUrl = 1
        }

        /// <summary>
		/// Registers a user-wrapper specified by the name of a defining user-class.
		/// </summary>
        /// <param name="caller">The class context.</param>
        /// <param name="protocol">The schema to be associated with the given wrapper.</param>
        /// <param name="classname">Name of the user class implementing the wrapper functions.</param>
		/// <returns>False in case of failure (ex. schema already occupied).</returns>
        [ImplementsFunction("stream_wrapper_register", FunctionImplOptions.NeedsClassContext)]
        public static bool RegisterUserWrapperByName(DTypeDesc caller, string protocol, string classname)
        {
            return RegisterUserWrapperByName(caller, protocol, classname, StreamWrapperRegisterFlags.Default);
        }

		/// <summary>
		/// Registers a user-wrapper specified by the name of a defining user-class.
		/// </summary>
        /// <param name="caller">The class context.</param>
		/// <param name="protocol">The schema to be associated with the given wrapper.</param>
        /// <param name="classname">Name of the user class implementing the wrapper functions.</param>
        /// <param name="flags">Should be set to STREAM_IS_URL if protocol is a URL protocol. Default is 0, local stream.</param>
		/// <returns>False in case of failure (ex. schema already occupied).</returns>
		[ImplementsFunction("stream_wrapper_register", FunctionImplOptions.NeedsClassContext)]
        public static bool RegisterUserWrapperByName(DTypeDesc caller, string protocol, string classname, StreamWrapperRegisterFlags flags/*=0*/)
		{
			// check if the scheme is already registered:
			if (string.IsNullOrEmpty(protocol) || StreamWrapper.Exists(protocol))
			{
				// TODO: Warning?
				return false;
			}

            DTypeDesc wrapperClass = ScriptContext.CurrentContext.ResolveType(classname, null, caller, null, ResolveTypeFlags.UseAutoload | ResolveTypeFlags.ThrowErrors);
            if (wrapperClass == null)
                return false;

			// EX: [stream_wrapper_register]: create the user wrapper
            StreamWrapper wrapper = new UserStreamWrapper(ScriptContext.CurrentContext, protocol, wrapperClass, flags == StreamWrapperRegisterFlags.IsUrl);
			return StreamWrapper.RegisterUserWrapper(protocol, wrapper);
		}

		/// <summary>
		/// Registers a user-wrapper specified by the name of a defining user-class.
		/// </summary>
        /// <param name="caller">The class context.</param>
        /// <param name="protocol">The schema to be associated with the given wrapper.</param>
		/// <param name="userWrapperName">Name of the user class implementing the wrapper functions.</param>
		/// <returns>False in case of failure (ex. schema already occupied).</returns>
        [ImplementsFunction("stream_register_wrapper", FunctionImplOptions.NeedsClassContext)]
        public static bool RegisterUserWrapperByName2(DTypeDesc caller, string protocol, string userWrapperName)
		{
			return RegisterUserWrapperByName(caller, protocol, userWrapperName);
		}

        /// <summary>
        /// Registers a user-wrapper specified by the name of a defining user-class.
        /// </summary>
        /// <param name="caller">The class context.</param>
        /// <param name="protocol">The schema to be associated with the given wrapper.</param>
        /// <param name="userWrapperName">Name of the user class implementing the wrapper functions.</param>
        /// <param name="flags">Should be set to STREAM_IS_URL if protocol is a URL protocol. Default is 0, local stream.</param>
        /// <returns>False in case of failure (ex. schema already occupied).</returns>
        [ImplementsFunction("stream_register_wrapper", FunctionImplOptions.NeedsClassContext)]
        public static bool RegisterUserWrapperByName2(DTypeDesc caller, string protocol, string userWrapperName, StreamWrapperRegisterFlags flags/*=0*/)
        {
            return RegisterUserWrapperByName(caller, protocol, userWrapperName, flags);
        }


		///<summary>Retrieve list of registered streams (only the names)</summary>  
		[ImplementsFunction("stream_get_wrappers")]
		public static PhpArray GetWrapperSchemes()
		{
			PhpArray ret = new PhpArray(8, 0);

			// First add the internal built-in wrappers.
			var internals = StreamWrapper.GetSystemWrapperSchemes();
			foreach (string scheme in internals)
			{
				ret.Add(scheme);
			}

#if !SILVERLIGHT
			// Then get the external wrapper schemes list.
			ICollection externals = Externals.GetStreamWrapperSchemes();
			foreach (string scheme in externals)
			{
				ret.Add(scheme);
			}
#endif

			// Now add the indexes (schemes) of User wrappers.
			foreach (string scheme in StreamWrapper.GetUserWrapperSchemes())
			{
				ret.Add(scheme);
			}
			return ret;
		}

		#endregion
	}

	#endregion

	#region PHP Stream functions

	/// <summary>
	/// A class encapsulating the static implementations of PHP Stream functions.
	/// </summary>
	/// <threadsafety static="true"/>
	public static class PhpStreams
	{
		#region stream_copy_to_stream

        /// <summary>
        /// Copies data from one stream to another.
        /// </summary>
        /// <param name="source">Stream to copy data from. Opened for reading.</param>
        /// <param name="dest">Stream to copy data to. Opened for writing.</param> 
		[ImplementsFunction("stream_copy_to_stream")]
		public static int Copy(PhpResource source, PhpResource dest)
		{
			return Copy(source, dest, -1, 0);
		}

        /// <summary>
        /// Copies data from one stream to another.
        /// </summary>
        /// <param name="source">Stream to copy data from. Opened for reading.</param>
        /// <param name="dest">Stream to copy data to. Opened for writing.</param>
        /// <param name="maxlength">The maximum count of bytes to copy (<c>-1</c> to copy entire <paramref name="source"/> stream.</param> 
        [ImplementsFunction("stream_copy_to_stream")]
        public static int Copy(PhpResource source, PhpResource dest, int maxlength)
        {
            return Copy(source, dest, maxlength, 0);
        }

		/// <summary>
		/// Copies data from one stream to another.
		/// </summary>
		/// <param name="source">Stream to copy data from. Opened for reading.</param>
		/// <param name="destination">Stream to copy data to. Opened for writing.</param>
		/// <param name="maxlength">The maximum count of bytes to copy (<c>-1</c> to copy entire <paramref name="source"/> stream.</param>
        /// <param name="offset">The offset where to start to copy data.</param>
		[ImplementsFunction("stream_copy_to_stream")]
		[return: CastToFalse]
		public static int Copy(PhpResource source, PhpResource destination, int maxlength, int offset)
		{
			PhpStream from = PhpStream.GetValid(source);
			PhpStream to = PhpStream.GetValid(destination);
			if (from == null || to == null) return -1;
            if (offset < 0) return -1;
			if (maxlength == 0) return 0;
            
			// Compatibility (PHP streams.c: "in the event that the source file is 0 bytes, 
			// return 1 to indicate success because opening the file to write had already 
			// created a copy"
			if (from.Eof) return 1;

            // If we have positive offset, we will skip the data
            if ( offset > 0 ) 
            {                
                int haveskipped = 0;

                while (haveskipped != offset)
                {
                    object data = null;

                    int toskip = offset - haveskipped;
                    if (toskip > from.GetNextDataLength())
                    {
                        data = from.ReadMaximumData();
                        if (data == null) break;
                    }
                    else
                    {
                        data = from.ReadData(toskip, false);
                        if (data == null) break; // EOF or error.
                        Debug.Assert(PhpStream.GetDataLength(data) <= toskip);
                    }

                    Debug.Assert(haveskipped <= offset);
                }
            }

			// Copy entire stream.
			int haveread = 0, havewritten = 0;
			while (haveread != maxlength)
			{
				object data = null;

				// Is is safe to read a whole block?
				int toread = maxlength - haveread;
				if ((maxlength == -1) || (toread > from.GetNextDataLength()))
				{
					data = from.ReadMaximumData();
					if (data == null) break; // EOF or error.
				}
				else
				{
					data = from.ReadData(toread, false);
					if (data == null) break; // EOF or error.
					Debug.Assert(PhpStream.GetDataLength(data) <= toread);
				}

				Debug.Assert((data is string) || (data is PhpBytes));
				haveread += PhpStream.GetDataLength(data);
				Debug.Assert((maxlength == -1) || (haveread <= maxlength));

				int written = to.WriteData(data);
				if (written <= 0)
				{
					// Warning already thrown at PhpStream.WriteData.
					return (havewritten > 0) ? haveread : -1;
				}
				havewritten += written;
			}

			return haveread;
		}

		#endregion

		#region stream_get_line, stream_get_meta_data

		/// <summary>Gets line from stream resource up to a given delimiter</summary> 
		/// <param name="handle">A handle to a stream opened for reading.</param>
		/// <param name="ending">A string containing the end-of-line delimiter.</param>
		/// <param name="length">Maximum length of the return value.</param>
		/// <returns>One line from the stream <b>without</b> the <paramref name="ending"/> string at the end.</returns>
		[ImplementsFunction("stream_get_line")]
		[return: CastToFalse]
		public static string ReadLine(PhpResource handle, int length, string ending)
		{
			PhpStream stream = PhpStream.GetValid(handle);
			if (stream == null) return null;

			if (length <= 0)
			{
				PhpException.Throw(PhpError.Warning, LibResources.GetString("arg:negative", "length"));
				return null;
			}

			if (String.IsNullOrEmpty(ending))
			{
				PhpException.Throw(PhpError.Warning, LibResources.GetString("arg:empty", "ending"));
				return null;
			}

			// The ending is not included in the returned data.
			string rv = stream.ReadLine(length, ending);
			if (rv == null) return null;
			if (rv.Length >= ending.Length)
				rv = rv.Substring(rv.Length - ending.Length);
			return rv;
		}

		/// <summary>
		/// Retrieves header/meta data from streams/file pointers
		/// </summary>
        /// <remarks>
        /// The result array contains the following items:
        /// * timed_out (bool) - TRUE if the stream timed out while waiting for data on the last call to fread() or fgets().
        /// * blocked (bool) - TRUE if the stream is in blocking IO mode. See stream_set_blocking().
        /// * eof (bool) - TRUE if the stream has reached end-of-file. Note that for socket streams this member can be TRUE even when unread_bytes is non-zero. To determine if there is more data to be read, use feof() instead of reading this item.
        /// * unread_bytes (int) - the number of bytes currently contained in the PHP's own internal buffer.
        /// * stream_type (string) - a label describing the underlying implementation of the stream.
        /// * wrapper_type (string) - a label describing the protocol wrapper implementation layered over the stream. See List of Supported Protocols/Wrappers for more information about wrappers.
        /// * wrapper_data (mixed) - wrapper specific data attached to this stream. See List of Supported Protocols/Wrappers for more information about wrappers and their wrapper data.
        /// * filters (array) - and array containing the names of any filters that have been stacked onto this stream. Documentation on filters can be found in the Filters appendix.
        /// * mode (string) - the type of access required for this stream (see Table 1 of the fopen() reference)
        /// * seekable (bool) - whether the current stream can be seeked.
        /// * uri (string) - the URI/filename associated with this stream.
        /// </remarks>
		[ImplementsFunction("stream_get_meta_data")]
		public static PhpArray GetMetaData(PhpResource resource)
		{
			PhpStream stream = PhpStream.GetValid(resource);
            if (stream == null) return null;

            PhpArray result = new PhpArray(0, 10);
            
            // TODO: timed_out (bool) - TRUE if the stream timed out while waiting for data on the last call to fread() or fgets().
            // TODO: blocked (bool) - TRUE if the stream is in blocking IO mode. See stream_set_blocking().
            result.Add("blocked", true);
            // eof (bool) - TRUE if the stream has reached end-of-file. Note that for socket streams this member can be TRUE even when unread_bytes is non-zero. To determine if there is more data to be read, use feof() instead of reading this item.
            result.Add("eof", stream.Eof);
            // TODO: unread_bytes (int) - the number of bytes currently contained in the PHP's own internal buffer.
            result.Add("unread_bytes", 0);
            // TODO: stream_type (string) - a label describing the underlying implementation of the stream.
            result.Add("stream_type", (stream.Wrapper != null) ? stream.Wrapper.Label : string.Empty);
            // wrapper_type (string) - a label describing the protocol wrapper implementation layered over the stream. See List of Supported Protocols/Wrappers for more information about wrappers.
            result.Add("wrapper_type", (stream.Wrapper != null) ? stream.Wrapper.Scheme : string.Empty);
            // wrapper_data (mixed) - wrapper specific data attached to this stream. See List of Supported Protocols/Wrappers for more information about wrappers and their wrapper data.
            if (stream.WrapperSpecificData != null)
                result.Add("wrapper_data", stream.WrapperSpecificData);
            // filters (array) - and array containing the names of any filters that have been stacked onto this stream. Documentation on filters can be found in the Filters appendix.
            result.Add("filters", GetFiltersName(stream));
            // mode (string) - the type of access required for this stream (see Table 1 of the fopen() reference)
            result.Add("mode", stream.CanRead ? (stream.CanWrite ? "r+" : "r") : (stream.CanWrite ? "w" : string.Empty));
            // seekable (bool) - whether the current stream can be seeked.
            result.Add("seekable", stream.CanSeek);
            // uri (string) - the URI/filename associated with this stream.
            result.Add("uri", stream.OpenedPath);

			return result;
		}

        /// <summary>
        /// filters (array)
        /// - array containing the names of any filters that have been stacked onto this stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        private static PhpArray GetFiltersName(PhpStream/*!*/stream)
        {
            PhpArray array = new PhpArray();

            foreach (PhpFilter f in stream.StreamFilters)
                array.Add(f.FilterName);

            return array;
        }

		#endregion

		#region stream_get_contents

		/// <summary>
		/// Reads entire content of the stream.
		/// </summary>
		[ImplementsFunction("stream_get_contents")]
		[return: CastToFalse]
		public static object ReadContents(PhpResource handle)
		{
			return ReadContents(handle, -1, -1);
		}

		/// <summary>
		/// Reads entire content of the stream.
		/// </summary>
		[ImplementsFunction("stream_get_contents")]
		[return: CastToFalse]
		public static object ReadContents(PhpResource handle, int maxLength)
		{
			return ReadContents(handle, maxLength, -1);
		}

		/// <summary>
		/// Reads entire content of the stream.
		/// </summary>
		[ImplementsFunction("stream_get_contents")]
		[return: CastToFalse]
		public static object ReadContents(PhpResource handle, int maxLength, int offset)
		{
			PhpStream stream = PhpStream.GetValid(handle, FileAccess.Read);
			if (stream == null) return null;

			return stream.ReadContents(maxLength, offset);
		}

		#endregion

		#region stream_set_blocking, stream_set_timeout, set_file_buffer, stream_set_write_buffer

		/// <summary>Set blocking/non-blocking (synchronous/asynchronous I/O operations) mode on a stream.</summary>
		/// <param name="resource">A handle to a stream resource.</param>
		/// <param name="mode"><c>1</c> for blocking, <c>0</c> for non-blocking.</param>
		/// <returns><c>true</c> if the operation is supported and was successful, <c>false</c> otherwise.</returns>
		[ImplementsFunction("stream_set_blocking")]
		public static bool SetBlocking(PhpResource resource, int mode)
		{
			PhpStream stream = PhpStream.GetValid(resource);
			if (stream == null) return false;

			bool block = mode > 0;
			return stream.SetParameter(StreamParameterOptions.BlockingMode, block);
		}

		/// <summary>Set timeout period on a stream</summary>
		/// <param name="resource">A handle to a stream opened for reading.</param>
		/// <param name="seconds">The number of seconds.</param>
		/// <returns><c>true</c> if the operation is supported and was successful, <c>false</c> otherwise.</returns>
		[ImplementsFunction("stream_set_timeout")]
		public static bool SetTimeout(PhpResource resource, int seconds)
		{
			return SetTimeout(resource, seconds, 0);
		}

		/// <summary>Set timeout period on a stream</summary>
		/// <param name="resource">A handle to a stream opened for reading.</param>
		/// <param name="seconds">The number of seconds.</param>
		/// <param name="microseconds">The number of microseconds.</param>
		/// <returns><c>true</c> if the operation is supported and was successful, <c>false</c> otherwise.</returns>
		[ImplementsFunction("stream_set_timeout")]
		public static bool SetTimeout(PhpResource resource, int seconds, int microseconds)
		{
			PhpStream stream = PhpStream.GetValid(resource);
			if (stream == null) return false;

			double timeout = seconds + (microseconds / 1000000.0);
			if (timeout < 0.0) timeout = 0.0;
			return stream.SetParameter(StreamParameterOptions.ReadTimeout, timeout);
		}

		/// <summary>Sets file buffering on the given stream.</summary>   
		/// <param name="resource">The stream to set write buffer size to.</param>
		/// <param name="buffer">Number of bytes the output buffer holds before 
		/// passing to the underlying stream.</param>
		/// <returns><c>true</c> on success.</returns>
		[ImplementsFunction("set_file_buffer")]
		public static bool SetFileBuffer(PhpResource resource, int buffer)
		{
			return SetWriteBuffer(resource, buffer);
		}

		/// <summary>Sets file buffering on the given stream.</summary>   
		/// <param name="resource">The stream to set write buffer size to.</param>
		/// <param name="buffer">Number of bytes the output buffer holds before 
		/// passing to the underlying stream.</param>
		/// <returns><c>true</c> on success.</returns>
		[ImplementsFunction("stream_set_write_buffer")]
		public static bool SetWriteBuffer(PhpResource resource, int buffer)
		{
			PhpStream stream = PhpStream.GetValid(resource);
			if (stream == null) return false;

			if (buffer < 0) buffer = 0;
			return stream.SetParameter(StreamParameterOptions.WriteBufferSize, buffer);
		}

		#endregion

		#region TODO: stream_select

		/// <summary>
		/// Runs the equivalent of the select() system call on the given arrays of streams 
		/// with a timeout specified by tv_sec and tv_usec.
		/// </summary> 
		[ImplementsFunction("stream_select")]
		public static bool Select(PhpArray read, PhpArray write, PhpArray except, int tv_sec)
		{
			return Select(read, write, except, tv_sec, 0);
		}

		/// <summary>Runs the equivalent of the select() system call on the given arrays of streams with a timeout specified by tv_sec and tv_usec </summary>   
        [ImplementsFunction("stream_select", FunctionImplOptions.NotSupported)]
		public static bool Select(PhpArray read, PhpArray write, PhpArray except, int tv_sec, int tv_usec)
		{
			PhpException.FunctionNotSupported();
			return false;
		}

		#endregion
	}

	#endregion
}
