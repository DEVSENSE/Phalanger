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
using System.Text.RegularExpressions;
using System.Threading;
using System.Collections;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Runtime.CompilerServices;

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

using PHP.Core;
#if SILVERLIGHT
using PHP.CoreCLR;
#endif

namespace PHP.Library
{
	#region Enums (FileOptions, WriteContentsOptions, ...)

	/// <summary>
	/// Options used in the <c>flags</c> argument of the 'fopen' function.
	/// </summary>
	[Flags]
	public enum FileOpenOptions
	{
		/// <summary>Default option for the <c>flags</c> argument.</summary>
		Empty = 0,
		/// <summary>Search for the file in the <c>include_path</c> too (1).</summary>
		[ImplementsConstant("FILE_USE_INCLUDE_PATH")]
		UseIncludePath = 0x1

		// UNUSED    /// <summary>Do not create a default context if none is provided (16).</summary>
		// UNUSED    [ImplementsConstant("FILE_NO_DEFAULT_CONTEXT")] NoDefaultContext = 0x10
	}

	/// <summary>
	/// Options used in the <c>flags</c> argument of PHP Filesystem functions.
	/// </summary>
	[Flags]
	public enum FileOptions
	{
		/// <summary>
		/// Default.
		/// </summary>
		Empty = 0,

		/// <summary>
		/// Search for the file in the <c>include_path</c> too (1).
		/// </summary>
		UseIncludePath = FileOpenOptions.UseIncludePath,

		/// <summary>
		/// Do not include the line break characters to the result in <c>file()</c> (2).
		/// </summary>
		[ImplementsConstant("FILE_IGNORE_NEW_LINES")]
		TrimLineEndings = 2,

		/// <summary>
		/// Do not include empty lines to the resulting <see cref="PhpArray"/> in <c>file()</c> (4).
		/// </summary>
		[ImplementsConstant("FILE_SKIP_EMPTY_LINES")]
		SkipEmptyLines = 4
	}

	/// <summary>
	/// The options used as the <c>flag</c> argument of <see cref="PhpFile.WriteContents"/>.
	/// </summary>
	[Flags]
	public enum WriteContentsOptions
	{
		/// <summary>
		/// Empty option (default).
		/// </summary>
		Empty = 0,

		/// <summary>
		/// Search for the file in the <c>include_path</c> too (1).
		/// </summary>
		UseIncludePath = FileOptions.UseIncludePath,

		/// <summary>
		/// Append the given data at the end of the file in <c>file_put_contents</c> (8).
		/// </summary>
		[ImplementsConstant("FILE_APPEND")]
		AppendContents = 8,

		/// <summary>
		/// Acquire an exclusive lock on the file.
		/// </summary>
		LockExclusive = StreamLockOptions.Exclusive
	}


	/// <summary>
	/// The flags indicating which fields the <see cref="PhpPath.GetInfo"/>
	/// method should fill in the result array.
	/// </summary>
	[Flags]
	public enum PathInfoOptions
	{
		/// <summary>
		/// Fill the "dirname" field in results.
		/// </summary>
		[ImplementsConstant("PATHINFO_DIRNAME")]
		DirName = 1,

		/// <summary>
		/// Fill the "basename" field in results.
		/// </summary>
		[ImplementsConstant("PATHINFO_BASENAME")]
		BaseName = 2,

		/// <summary>
		/// Fill the "extension" field in results.
		/// </summary>
		[ImplementsConstant("PATHINFO_EXTENSION")]
		Extension = 4,

		/// <summary>
		/// Fill the "filename" field in results. Since PHP 5.2.0.
		/// </summary>
        [ImplementsConstant("PATHINFO_FILENAME")]
		FileName = 8,

		/// <summary>
		/// All the four options result in an array returned by <see cref="PhpPath.GetInfo"/>.
		/// </summary>
		All = DirName | BaseName | Extension | FileName
	}

	#endregion

	/// <summary>
	/// Provides PHP I/O operations using the set of StreamWrappers.
	/// </summary>
	/// <threadsafety static="true"/>
	public static partial class PhpFile
	{
        /// <summary>
        /// Name of variable that is filled with response headers in case of file_get_contents and http protocol.
        /// </summary>
        internal const string HttpResponseHeaderName = "http_response_header";

		#region Constructors 

		/// <summary>
		/// Registers the ClassLibrary filters for the Core streams API.
		/// </summary>
		static PhpFile()
		{
#if !SILVERLIGHT
            RequestContext.RequestEnd += new Action(Clear);
#endif
			PhpFilter.AddSystemFilter(new StringFilterFactory());
			PhpFilter.AddSystemFilter(new EncodingFilterFactory());
			PhpFilter.AddSystemFilter(new DecodingFilterFactory());
		}

		#endregion

		#region make_absolute (Silverlight utility)
#if SILVERLIGHT

		[ImplementsFunction("sl_mkabsolute")]
		public static string MakeAbsoluteUrl(string relative)
		{
			return HttpPathUtils.Combine(System.Windows.Browser.HtmlPage.Document.DocumentUri.AbsoluteUri, "../" + relative);
		}

#endif
		#endregion

        #region fopen, tmpfile, fclose, feof, fflush

        /// <summary>
		/// Opens filename or URL using a registered StreamWrapper.
		/// </summary>
		/// <param name="path">The file to be opened. The schema part of the URL specifies the wrapper to be used.</param>
		/// <param name="mode">The read/write and text/binary file open mode.</param>
		/// <returns>The file resource or false in case of failure.</returns>
		[ImplementsFunction("fopen")]
		[return: CastToFalse]
		public static PhpResource Open(string path, string mode)
		{
			return Open(path, mode, FileOpenOptions.Empty, StreamContext.Default);
		}

		/// <summary>
		/// Opens filename or URL using a registered StreamWrapper.
		/// </summary>
		/// <param name="path">The file to be opened. The schema part of the URL specifies the wrapper to be used.</param>
		/// <param name="mode">The read/write and text/binary file open mode.</param>
		/// <param name="flags">If set to true, then the include path is searched for relative filenames too.</param>
		/// <returns>The file resource or false in case of failure.</returns>
		[ImplementsFunction("fopen")]
		[return: CastToFalse]
		public static PhpResource Open(string path, string mode, FileOpenOptions flags)
		{
			return Open(path, mode, flags, StreamContext.Default);
		}

		/// <summary>
		/// Opens filename or URL using a registered StreamWrapper.
		/// </summary>
		/// <param name="path">The file to be opened. The schema part of the URL specifies the wrapper to be used.</param>
		/// <param name="mode">The read/write and text/binary file open mode.</param>
		/// <param name="flags">If set to true, then the include path is searched for relative filenames too.</param>
		/// <param name="context">A script context to be provided to the StreamWrapper.</param>
		/// <returns>The file resource or false in case of failure.</returns>
		[ImplementsFunction("fopen")]
		[return: CastToFalse]
		public static PhpResource Open(string path, string mode, FileOpenOptions flags, PhpResource context)
		{
			StreamContext sc = StreamContext.GetValid(context);
			if (sc == null) return null;

			if (String.IsNullOrEmpty(path))
			{
				PhpException.Throw(PhpError.Warning, LibResources.GetString("arg:empty", "path"));
				return null;
			}

			if (String.IsNullOrEmpty(mode))
			{
				PhpException.Throw(PhpError.Warning, LibResources.GetString("arg:empty", "mode"));
				return null;
			}

			return PhpStream.Open(path, mode, ProcessOptions(flags), sc);
		}

		/// <summary>
		/// Prevents invalid options from the the options argument for StreamWrapper.Open().
		/// </summary>
		/// <param name="flags">Flags passed to stream opening functions.</param>
		/// <returns>The StreamOpenFlags combination for the given arguments.</returns>
		internal static StreamOpenOptions ProcessOptions(FileOpenOptions flags)
		{
			StreamOpenOptions options = 0;

			if ((flags & FileOpenOptions.UseIncludePath) > 0)
				options |= StreamOpenOptions.UseIncludePath;

			if (!ScriptContext.CurrentContext.ErrorReportingDisabled)
				options |= StreamOpenOptions.ReportErrors;

			return options;
		}

		/// <summary>
		/// Creates a temporary file.
		/// </summary>
		/// <remarks>
		/// Creates a temporary file with an unique name in write mode, 
		/// returning a file handle similar to the one returned by fopen(). 
		/// The file is automatically removed when closed (using fclose()), 
		/// or when the script ends.
		/// </remarks>
		/// <returns></returns>
		[ImplementsFunction("tmpfile")]
		public static PhpResource OpenTemporary()
		{
			string path = PhpPath.GetTemporaryFilename(string.Empty, "php");

			StreamWrapper wrapper;
			if (!PhpStream.ResolvePath(ref path, out wrapper, CheckAccessMode.FileMayExist, CheckAccessOptions.Empty))
				return null;

			return wrapper.Open(ref path, "w+b", StreamOpenOptions.Temporary, StreamContext.Default);
		}

        /// <summary>
		/// Close an open file pointer.
		/// </summary>
		/// <param name="handle">A PhpResource passed to the PHP function.</param>
		/// <returns>True if successful.</returns>
		[ImplementsFunction("fclose")]
		public static bool Close(PhpResource handle)
		{
			PhpStream stream = PhpStream.GetValid(handle);
			if (stream == null) return false;
			if (stream.IsPersistent)
			{
				// Do not close persisten streams (incl. for example STDOUT).
				stream.Flush();
				return true;
			}
			stream.Close();
			return true;
		}

		/// <summary>
		/// Tests for end-of-file on a file pointer.
		/// </summary>
		/// <param name="handle">A PhpResource passed to the PHP function.</param>
		/// <returns>True if successful.</returns>
		[ImplementsFunction("feof")]
		public static bool Eof(PhpResource handle)
		{
			PhpStream stream = PhpStream.GetValid(handle);
			if (stream == null) return false;
			return stream.Eof;
		}

		/// <summary>
		/// Flushes the output to a file.
		/// </summary>
		/// <param name="handle">A PhpResource passed to the PHP function.</param>
		/// <returns>True if successful.</returns>
		[ImplementsFunction("fflush")]
		public static bool Flush(PhpResource handle)
		{
			PhpStream stream = PhpStream.GetValid(handle);
			if (stream == null) return false;
			return stream.Flush();
		}

		#endregion

		#region fprintf, fscanf

		/// <summary>
		/// Writes the string formatted using <c>sprintf</c> to the given stream.
		/// </summary>
		/// <param name="handle">A stream opened for writing.</param>
		/// <param name="format">The format string. For details, see PHP manual.</param>
		/// <param name="arguments">The arguments.
		/// See <A href="http://www.php.net/manual/en/function.sprintf.php">PHP manual</A> for details.
		/// Besides, a type specifier "%C" is applicable. It converts an integer value to Unicode character.</param>
		/// <returns>Number of characters written of <c>false</c> in case of an error.</returns>
		[ImplementsFunction("fprintf")]
		[return: CastToFalse]
		public static int WriteFormatted(PhpResource handle, string format, params object[] arguments)
		{
			string formatted = PhpStrings.Format(format, arguments);
			if (formatted == String.Empty) return 0;
			return WriteInternal(handle, formatted, -1);
		}

		/// <summary>
		/// Parses input from a file according to a format.
		/// </summary>
		/// <param name="handle"></param>
		/// <param name="format"></param>
		/// <returns>A <see cref="PhpArray"/> containing the parsed values.</returns>
		[ImplementsFunction("fscanf")]
		[return: CastToFalse]
		public static PhpArray ReadLineFormat(PhpResource handle, string format)
		{
			PhpStream stream = PhpStream.GetValid(handle);
			if (stream == null) return null;
			string line = stream.ReadLine(-1, null);
			return PhpStrings.ScanFormat(line, format);
		}

		/// <summary>
		/// Parses input from a file according to a format.
		/// </summary>
		/// <param name="handle"></param>
		/// <param name="format"></param>
		/// <param name="arg"></param>
		/// <param name="arguments"></param>
		/// <returns>The number of assigned values.</returns>
		[ImplementsFunction("fscanf")]
		[return: CastToFalse]
		public static int ReadLineFormat(PhpResource handle, string format, PhpReference arg, params PhpReference[] arguments)
		{
			PhpStream stream = PhpStream.GetValid(handle);
			if (stream == null) return -1;
			string line = stream.ReadLine(-1, null);
			return PhpStrings.ScanFormat(line, format, arg, arguments);
		}

		#endregion

        #region fgetcsv, fputcsv, str_getcsv

        private const char DefaultCsvDelimiter = ',';
		private const char DefaultCsvEnclosure = '"';
        private const char DefaultCsvEscape = '\\';

        [ImplementsFunction("str_getcsv")]
        public static PhpArray ReadStrLineCsv( string input  )
        {
            return ReadStrLineCsv(input, DefaultCsvDelimiter, DefaultCsvEnclosure, DefaultCsvEscape);
        }
        [ImplementsFunction("str_getcsv")]
        public static PhpArray ReadStrLineCsv(string input, char delimiter)
        {
            return ReadStrLineCsv(input, delimiter, DefaultCsvEnclosure, DefaultCsvEscape);
        }
        [ImplementsFunction("str_getcsv")]
        public static PhpArray ReadStrLineCsv(string input, char delimiter, char enclosure)
        {
            return ReadStrLineCsv(input, delimiter, enclosure, DefaultCsvEscape);
        }
        [ImplementsFunction("str_getcsv")]
        public static PhpArray ReadStrLineCsv(string input, char delimiter, char enclosure, char escape)
        {
            bool firstLine = true;
            return ReadLineCsv(delegate()
            {
                if (!firstLine)
                    return null;
                
                firstLine = false;
                return input;
            },
            delimiter, enclosure, escape);
        }

		[ImplementsFunction("fgetcsv")]
        public static object ReadLineCsv(PhpResource handle)
		{
			return ReadLineCsv(handle, 0, DefaultCsvDelimiter, DefaultCsvEnclosure, DefaultCsvEscape);
		}

		[ImplementsFunction("fgetcsv")]
        public static object ReadLineCsv(PhpResource handle, int length)
		{
            return ReadLineCsv(handle, length, DefaultCsvDelimiter, DefaultCsvEnclosure, DefaultCsvEscape);
		}

        [ImplementsFunction("fgetcsv")]
        public static object ReadLineCsv(PhpResource handle, int length, char delimiter)
        {
            return ReadLineCsv(handle, length, delimiter, DefaultCsvEnclosure, DefaultCsvEscape);
        }

        [ImplementsFunction("fgetcsv")]
        public static object ReadLineCsv(PhpResource handle, int length, char delimiter, char enclosure)
        {
            return ReadLineCsv(handle, length, delimiter, enclosure, DefaultCsvEscape);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="length"></param>
        /// <param name="delimiter"></param>
        /// <param name="enclosure"></param>
        /// <param name="escape_char">The escape character used in the CSV string.</param>
        /// <returns>Returns an indexed array containing the fields read.
        /// fgetcsv() returns NULL if an invalid handle is supplied or FALSE on other errors, including end of file.</returns>
		[ImplementsFunction("fgetcsv")]
		public static object ReadLineCsv(PhpResource handle, int length, char delimiter/*=','*/, char enclosure/*='"'*/, char escape_char /*= '\\'*/ )
		{
            // check arguments
			PhpStream stream = PhpStream.GetValid(handle, FileAccess.Read);
            if (stream == null) return null;
			if (length < 0) PhpException.InvalidArgument("length", LibResources.GetString("arg:negative"));
            if (length <= 0) length = -1;    // no length limit
            if (stream.Eof) return false;

            return ReadLineCsv(() => (stream.Eof ? null : stream.ReadLine(length, null)), delimiter, enclosure, escape_char);
		}

        /// <summary>
        /// CSV data line reader.
        /// In case of stream, it returns stream.GetLine() or null in case of EOF.
        /// In case of string input, it returns string for the first time, then null.
        /// ...
        /// </summary>
        /// <returns>Next line of CSV data or NULL in case of EOF.</returns>
        private delegate string CsvLineReader();

        private static PhpArray ReadLineCsv( CsvLineReader reader, char delimiter/*=','*/, char enclosure/*='"'*/, char escape_char /*= '\\'*/ )
        {
            // collect results
            PhpArray result = new PhpArray();

            int i = 0;  // index of currently scanned char
            string line = reader(); // currently scanned string
            bool eof = false;

            if (line == null)
            {
                result.Add(null);
                return result;
            }

            for (; ; )
            {
                Debug.Assert(i - 1 < line.Length);
                bool previous_field_delimited = (i == 0 || line[i - 1] == delimiter);

                // skip initial whitespace:
                while (i < line.Length && Char.IsWhiteSpace(line[i]) && line[i] != delimiter)
                    ++i;

                if (i >= line.Length)
                {
                    if (result.Count == 0)
                        result.Add(null);
                    else if (previous_field_delimited)
                        result.Add(string.Empty);

                    break;
                }
                else if (line[i] == delimiter)
                {
                    if (previous_field_delimited)
                        result.Add(string.Empty);
                    ++i;
                }
                else if (line[i] == enclosure)
                {
                    // enclosed string follows:
                    int start = ++i;
                    StringBuilder field_builder = new StringBuilder();

                    for (; ; )
                    {
                        // read until enclosure character found:
                        while (i < line.Length && line[i] != enclosure)
                        {
                            if (i + 1 < line.Length && line[i] == escape_char)
                                ++i;// skip escape char

                            ++i;    // skip following char
                        }

                        // end of line:
                        if (i == line.Length)
                        {
                            // append including eoln:
                            field_builder.Append(line, start, line.Length - start);

                            // field continues on the next line:
                            string nextLine = reader();
                            if (nextLine == null)
                            {
                                eof = true;
                                break;
                            }

                            line = nextLine;
                            start = i = 0;
                        }
                        else
                        {
                            Debug.Assert(line[i] == enclosure);
                            i++;

                            if (i < line.Length && line[i] == enclosure)
                            {
                                // escaped enclosure; add previous text including enclosure:
                                field_builder.Append(line, start, i - start);
                                start = ++i;
                            }
                            else
                            {
                                // end of enclosure:
                                field_builder.Append(line, start, i - 1 - start);
                                start = i;
                                break;
                            }
                        }
                    }

                    if (!eof)//if (!stream.Eof)
                    {
                        Debug.Assert(start == i && line.Length > 0);

                        int end = GetCsvDisclosedTextEnd(line, delimiter, ref i, escape_char);

                        field_builder.Append(line, start, end - start);
                    }

                    //result.Add(Core.Convert.Quote(field_builder.ToString(), context));
                    //result.Add(StringUtils.EscapeStringCustom(field_builder.ToString(), charsToEscape, escape));
                    result.Add(field_builder.ToString());
                }
                else
                {
                    // disclosed text:

                    int start = i;
                    int end = GetCsvDisclosedTextEnd(line, delimiter, ref i, escape_char);

                    //result.Add( Core.Convert.Quote(line.Substring(start, end - start), context));
                    //result.Add(StringUtils.EscapeStringCustom(line.Substring(start, end - start), charsToEscape, escape));
                    result.Add(line.Substring(start, end - start));
                }
            }

            return result;
        }
        private static int GetCsvDisclosedTextEnd(string line, char delimiter, ref int i, char escape_char)
		{
			// disclosed text follows enclosed one:
            while (i < line.Length && line[i] != delimiter)
            {
                i++;
            }

			// field ended by eoln or delimiter:
			if (i == line.Length)
			{
				// do not add eoln to the field:
                int dec = 0;
                if (line[i - 1] == '\n')
                {
                    dec++;
                    if (i > 1 && line[i - 2] == '\r')
                        dec++;
                }
                return i - dec;
			}
			else
			{
				Debug.Assert(line[i] == delimiter);

				// skip delimiter:
				return i++;
			}
		}

		[ImplementsFunction("fputcsv")]
		public static int WriteLineCsv(PhpResource handle, PhpArray fields)
		{
			return WriteLineCsv(handle, fields, DefaultCsvDelimiter, DefaultCsvEnclosure);
		}

		[ImplementsFunction("fputcsv")]
		public static int WriteLineCsv(PhpResource handle, PhpArray fields, char delimiter)
		{
			return WriteLineCsv(handle, fields, delimiter, DefaultCsvEnclosure);
		}

		/// <remarks>
		/// Affected by run-time quoting (data are unqouted before written)
		/// (<see cref="LocalConfiguration.VariablesSection.QuoteRuntimeVariables"/>).
		/// </remarks>
		[ImplementsFunction("fputcsv")]
		public static int WriteLineCsv(PhpResource handle, PhpArray fields, char delimiter, char enclosure)
		{
			PhpStream stream = PhpStream.GetValid(handle, FileAccess.Write);
			if (stream == null || !stream.CanWrite) return -1;

			ScriptContext context = ScriptContext.CurrentContext;
			char[] special_chars = { delimiter, ' ', '\\', '\t', '\r', '\n' };
			string str_enclosure = enclosure.ToString();
			string str_delimiter = delimiter.ToString();

			int initial_position = stream.WritePosition;
			foreach (object field in fields.Values)
			{
				string str_field = Core.Convert.Unquote(Core.Convert.ObjectToString(field), context);

				if (stream.WritePosition > initial_position)
					stream.WriteString(str_delimiter);

				int special_char_index = str_field.IndexOfAny(special_chars);
				int enclosure_index = str_field.IndexOf(enclosure);

				if (special_char_index >= 0 || enclosure_index >= 0)
				{
					stream.WriteString(str_enclosure);

					if (enclosure_index >= 0)
					{
						// escapes enclosure characters:
						int start = 0;
						for (; ; )
						{
							// writes string starting after the last enclosure and ending by the next one:
							stream.WriteString(str_field.Substring(start, enclosure_index - start + 1));
							stream.WriteString(str_enclosure);

							start = enclosure_index + 1;
							if (start >= str_field.Length) break;

							enclosure_index = str_field.IndexOf(enclosure, start);
							if (enclosure_index < 0)
							{
								// remaining substring: 
								stream.WriteString(str_field.Substring(start));
								break;
							}
						}
					}
					else
					{
						stream.WriteString(str_field);
					}

					stream.WriteString(str_enclosure);
				}
				else
				{
					stream.WriteString(str_field);
				}
			}

			stream.WriteString("\n");

			return (initial_position == -1) ? stream.WritePosition : stream.WritePosition - initial_position;
		}

		#endregion

		#region fread, fgetc, fwrite, fputs, fpassthru, readfile

		/// <summary>
		/// Binary-safe file read.
		/// </summary>
		/// <param name="handle">A file stream opened for reading.</param>
		/// <param name="length">Number of bytes to be read.</param>
		/// <returns>
		/// The <see cref="string"/> or <see cref="PhpBytes"/>
		/// of the specified length depending on file access mode.
		/// </returns>
		/// <remarks>
		/// Result is affected by run-time quoting 
		/// (<see cref="LocalConfiguration.VariablesSection.QuoteRuntimeVariables"/>).
		/// </remarks>
		[ImplementsFunction("fread")]
		public static object Read(PhpResource handle, int length)
		{
			// returns an object (string or PhpBytes depending on fopen mode)
			PhpStream stream = PhpStream.GetValid(handle);
			if (stream == null) return null;

			if (stream.IsText)
				return Core.Convert.Quote(stream.ReadString(length), ScriptContext.CurrentContext);
			else
				return Core.Convert.Quote(stream.ReadBytes(length), ScriptContext.CurrentContext);
		}

		/// <summary>
		/// Gets character from file pointer.
		/// </summary>
		/// <param name="handle">A file stream opened for reading.</param>
		/// <returns>A <see cref="string"/> or <see cref="PhpBytes"/> containing one character from the 
		/// given stream or <c>false</c> on EOF.</returns>
		[ImplementsFunction("fgetc")]
		[return: CastToFalse]
		public static object ReadChar(PhpResource handle)
		{
			if (Eof(handle)) return null;
			return Read(handle, 1);
		}

		/// <summary>
		/// Binary-safe file write.
		/// </summary>
		/// <param name="handle">The file stream (opened for writing). </param>
		/// <param name="data">The data to be written.</param>
		/// <returns>Returns the number of bytes written, or FALSE on error. </returns>
		/// <remarks>
		/// Affected by run-time quoting (data are unqouted before written)
		/// (<see cref="LocalConfiguration.VariablesSection.QuoteRuntimeVariables"/>).
		/// </remarks>
		[ImplementsFunction("fwrite")]
		[return: CastToFalse]
		public static int Write(PhpResource handle, object data)
		{
			return Write(handle, data, -1);
		}

		/// <summary>
		/// Binary-safe file write.
		/// </summary>
		/// <param name="handle">The file stream (opened for writing). </param>
		/// <param name="data">The data to be written.</param>
		/// <param name="length">
		/// If the length argument is given, writing will stop after length bytes 
		/// have been written or the end of string is reached, whichever comes first.
		/// </param>
		/// <returns>Returns the number of bytes written, or FALSE on error. </returns>
		/// <remarks>
		/// Affected by run-time quoting (data are unqouted before written)
		/// (<see cref="LocalConfiguration.VariablesSection.QuoteRuntimeVariables"/>).
		/// </remarks>
		[ImplementsFunction("fwrite")]
		[return: CastToFalse]
		public static int Write(PhpResource handle, object data, int length)
		{
			data = Core.Convert.Unquote(data, ScriptContext.CurrentContext);
			return WriteInternal(handle, data, length);
		}

		/// <summary>
		/// Binary-safe file write. Alias for <see cref="Write(PhpResource, object)"/>.
		/// </summary>
		/// <param name="handle">The file stream (opened for writing). </param>
		/// <param name="data">The data to be written.</param>
		/// <returns>Returns the number of bytes written, or FALSE on error. </returns>
		[ImplementsFunction("fputs")]
		[return: CastToFalse]
		public static int Write2(PhpResource handle, object data)
		{
			return Write(handle, data);
		}

		/// <summary>
		/// Binary-safe file write. Alias for <see cref="Write(PhpResource, object, int)"/>.
		/// </summary>
		/// <param name="handle">The file stream (opened for writing). </param>
		/// <param name="data">The data to be written.</param>
		/// <param name="length">If the length argument is given, writing will stop after length bytes 
		/// have been written or the end of string is reached, whichever comes first. </param>
		/// <returns>Returns the number of bytes written, or FALSE on error. </returns>
		[ImplementsFunction("fputs")]
		[return: CastToFalse]
		public static int Write2(PhpResource handle, object data, int length)
		{
			return Write(handle, data, length);
		}

		/// <summary>
		/// Binary-safe file write implementation.
		/// </summary>
		/// <param name="handle">The file stream (opened for writing). </param>
		/// <param name="data">The data to be written.</param>
		/// <param name="length">The number of characters to write or <c>-1</c> to use the whole <paramref name="data"/>.</param>
		/// <returns>Returns the number of bytes written, or FALSE on error. </returns>
		internal static int WriteInternal(PhpResource handle, object data, int length)
		{
			PhpStream stream = PhpStream.GetValid(handle);
			if (stream == null) return -1;

			if (data == null) return 0;

			// Note: Any data type is converted using implicit conversion in AsText/AsBinary.
			if (stream.IsText)
			{
				// If file OpenMode is text then use string access methods.
				string sub;
				if (length > 0) sub = PhpStream.AsText(data, length);
				else sub = PhpStream.AsText(data);

				return stream.WriteString(sub);
			}
			else
			{
				// File OpenMode is binary.
				PhpBytes sub;
				if (length > 0) sub = PhpStream.AsBinary(data, length);
				else sub = PhpStream.AsBinary(data);

				return stream.WriteBytes(sub);
			}
		}


		/// <summary>
		/// Outputs all remaining data on a file pointer.
		/// </summary>
		/// <param name="handle">The file stream (opened for reading). </param>
		/// <returns>Number of bytes written.</returns>
		[ImplementsFunction("fpassthru")]
		[return: CastToFalse]
		public static int PassThrough(PhpResource handle)
		{
			PhpStream stream = PhpStream.GetValid(handle);
			if (stream == null) return -1;
			if (stream.IsText)
			{
				// Use the text output buffers.
				int rv = 0;
				while (!stream.Eof)
				{
					string str = stream.ReadMaximumString();
					ScriptContext.CurrentContext.Output.Write(str);
					rv += str.Length;
				}
				return rv;
			}
			else
			{
				// Write directly to the binary output buffers.
				return PhpStreams.Copy(stream, InputOutputStreamWrapper.ScriptOutput);
			}
		}

		/// <summary>
		/// Reads a file and writes it to the output buffer.
		/// </summary>
		/// <param name="path">The file to open.</param>
		/// <returns>Returns the number of bytes read from the file. If an error occurs, <c>false</c> is returned.</returns>
		[ImplementsFunction("readfile")]
		[return: CastToFalse]
		public static int ReadFile(string path)
		{
			return ReadFile(path, FileOpenOptions.Empty, StreamContext.Default);
		}

		/// <summary>
		/// Reads a file and writes it to the output buffer.
		/// </summary>
		/// <param name="path">The file to open.</param>
		/// <param name="flags">Searches for the file in the <c>include_path</c> if set to <c>true</c>.</param>
		/// <returns>Returns the number of bytes read from the file. If an error occurs, <c>false</c> is returned.</returns>
		[ImplementsFunction("readfile")]
		[return: CastToFalse]
		public static int ReadFile(string path, FileOpenOptions flags)
		{
			return ReadFile(path, flags, StreamContext.Default);
		}

		/// <summary>
		/// Reads a file and writes it to the output buffer.
		/// </summary>
		/// <param name="path">The file to open.</param>
		/// <param name="flags">Searches for the file in the <c>include_path</c> if set to <c>1</c>.</param>
		/// <param name="context">A <see cref="StreamContext"/> resource with additional information for the stream.</param>
		/// <returns>Returns the number of bytes read from the file. If an error occurs, <c>false</c> is returned.</returns>
		[ImplementsFunction("readfile")]
		[return: CastToFalse]
		public static int ReadFile(string path, FileOpenOptions flags, PhpResource context)
		{
            StreamContext sc = StreamContext.GetValid(context, true);
			if (sc == null) return -1;

			using (PhpStream res = PhpStream.Open(path, "rb", ProcessOptions(flags), sc))
			{
				if (res == null) return -1;

				// Note: binary file access is the most efficient (no superfluous filtering
				// and no conversions - PassThrough will write directly to the OutputStream).
				return PassThrough(res);
			}
		}

		#endregion

		#region fgets, fgetss

		/// <summary>
		/// Gets one line of text from file pointer including the end-of-line character. 
		/// </summary>
		/// <param name="handle">The file stream opened for reading.</param>
		/// <returns>A <see cref="string"/> or <see cref="PhpBytes"/> containing the line of text or <c>false</c> in case of an error.</returns>
		/// <remarks>
		/// <para>
		///   Result is affected by run-time quoting 
		///   (<see cref="LocalConfiguration.VariablesSection.QuoteRuntimeVariables"/>).
		/// </para>
		/// </remarks>
		[ImplementsFunction("fgets")]
		[return: CastToFalse]
		public static object ReadLine(PhpResource handle)
		{
			PhpStream stream = PhpStream.GetValid(handle);
			if (stream == null) return null;

			// Use the default accessor to the stream breaking at \n, no superfluous conversion.
			return Core.Convert.Quote(stream.ReadData(-1, true), ScriptContext.CurrentContext);
		}

		/// <summary>
		/// Gets one line of text from file pointer including the end-of-line character. 
		/// </summary>
		/// <param name="length">Maximum length of the returned text.</param>
		/// <param name="handle">The file stream opened for reading.</param>
		/// <returns>A <see cref="string"/> or <see cref="PhpBytes"/> containing the line of text or <c>false</c> in case of an error.</returns>
		/// <remarks>
		/// <para>
		///   Returns a string of up to <paramref name="length"/><c> - 1</c> bytes read from 
		///   the file pointed to by <paramref name="handle"/>.
		/// </para>
		/// <para>
		///   The <paramref name="length"/> parameter became optional in PHP 4.2.0, if omitted, it would
		///   assume 1024 as the line length. As of PHP 4.3, omitting <paramref name="length"/> will keep
		///   reading from the stream until it reaches the end of the line. 
		///   If the majority of the lines in the file are all larger than 8KB, 
		///   it is more resource efficient for your script to specify the maximum line length.
		/// </para>
		/// <para>
		///   Result is affected by run-time quoting 
		///   (<see cref="LocalConfiguration.VariablesSection.QuoteRuntimeVariables"/>).
		/// </para>
		/// </remarks>
		[ImplementsFunction("fgets")]
		[return: CastToFalse]
		public static object ReadLine(PhpResource handle, int length)
		{
			if (length <= 0)
			{
				PhpException.Throw(PhpError.Warning, LibResources.GetString("arg:negative", "Length"));
				return null;
			}

			PhpStream stream = PhpStream.GetValid(handle);
			if (stream == null) return null;

			// Use the default accessor to the stream breaking at \n, no superfluous conversion.
			return Core.Convert.Quote(stream.ReadData(length, true), ScriptContext.CurrentContext);
		}

		/// <summary>
		/// Gets a whole line from file pointer and strips HTML tags.
		/// </summary>
		[ImplementsFunction("fgetss")]
		[return: CastToFalse]
		public static string ReadLineStripTags(PhpResource handle)
		{
			return ReadLineStripTagsInternal(handle, -1, null);
		}

		/// <summary>
		/// Gets a line from file pointer and strips HTML tags.
		/// </summary>
		[ImplementsFunction("fgetss")]
		[return: CastToFalse]
		public static string ReadLineStripTags(PhpResource handle, int length)
		{
			return ReadLineStripTags(handle, length, null);
		}

		/// <summary>
		/// Gets one line from file pointer and strips HTML tags.
		/// </summary>
		/// <param name="handle"></param>
		/// <param name="length"></param>
		/// <param name="allowableTags"></param>
		/// <returns></returns>
		[ImplementsFunction("fgetss")]
		[return: CastToFalse]
		public static string ReadLineStripTags(PhpResource handle, int length, string allowableTags)
		{
			if (length <= 0)
			{
				PhpException.Throw(PhpError.Warning, LibResources.GetString("arg:negative", "Length"));
				return null;
			}

			return ReadLineStripTagsInternal(handle, length, allowableTags);
		}

		internal static string ReadLineStripTagsInternal(PhpResource handle, int length, string allowableTags)
		{
			PhpStream stream = PhpStream.GetValid(handle);
			if (stream == null) return null;
			string line = PhpStream.AsText(stream.ReadLine(length, null));

			if (line != null)
			{
				int state = stream.StripTagsState;
				line = PhpStrings.StripTags(line, allowableTags, ref state);
				stream.StripTagsState = state;
			}
			return line;
		}

		#endregion

		#region file, file_get_contents, file_put_contents

		/// <summary>
		/// Reads entire file into an array.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		[ImplementsFunction("file")]
		[return: CastToFalse]
		public static PhpArray ReadArray(string path)
		{
			return ReadArray(path, 0, StreamContext.Default);
		}

		/// <summary>
		/// Reads entire file into an array.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="flags"></param>
		/// <returns></returns>
		[ImplementsFunction("file")]
		[return: CastToFalse]
		public static PhpArray ReadArray(string path, FileOptions flags)
		{
			return ReadArray(path, flags, StreamContext.Default);
		}

		/// <summary>
		/// Reads entire file into an array.
		/// </summary>
		/// <remarks>
		/// </remarks>
		/// <para>
		///   The input file is split at '\n' and the separator is included in every line.
		/// </para>
		/// <para>
		///   Result is affected by run-time quoting 
		///   (<see cref="LocalConfiguration.VariablesSection.QuoteRuntimeVariables"/>).
		/// </para>
		[ImplementsFunction("file")]
		[return: CastToFalse]
		public static PhpArray ReadArray(string path, FileOptions flags, PhpResource context)
		{
            StreamContext sc = StreamContext.GetValid(context, true);
			if (sc == null) return null;

			ScriptContext script_context = ScriptContext.CurrentContext;

			using (PhpStream stream = PhpStream.Open(path, "rt", ProcessOptions((FileOpenOptions)flags), sc))
			{
				if (stream == null) return null;

				PhpArray rv = new PhpArray();

				while (!stream.Eof)
				{
					// Note: The last line does not contain the \n delimiter, but may be empty
					string line = PhpStream.AsText(stream.ReadData(-1, true));

                    if ((flags & FileOptions.TrimLineEndings) > 0)
                    {
                        int len = line.Length;
                        if ((len > 0) && (line[len - 1] == '\n'))
                            line = line.Substring(0, len - 1);
                    }
                    if ((flags & FileOptions.SkipEmptyLines) > 0)
					{
						if (line.Length == 0) continue;
					}

					rv.Add(Core.Convert.Quote(line, script_context));
				}

				return rv;
			}
		}

		/// <summary>
		/// Reads entire file into a string.
		/// </summary>
        [ImplementsFunction("file_get_contents", FunctionImplOptions.NeedsVariables)]
		[return: CastToFalse]
        public static object ReadContents(ScriptContext scriptcontext, System.Collections.Generic.Dictionary<string, object> definedVariables, string path)
		{
            return ReadContents(scriptcontext, definedVariables, path, FileOpenOptions.Empty, StreamContext.Default, -1, -1);
		}

		/// <summary>
		/// Reads entire file into a string.
		/// </summary>
        [ImplementsFunction("file_get_contents", FunctionImplOptions.NeedsVariables)]
		[return: CastToFalse]
        public static object ReadContents(ScriptContext scriptcontext, System.Collections.Generic.Dictionary<string, object> definedVariables, string path, FileOpenOptions flags)
		{
            return ReadContents(scriptcontext, definedVariables, path, flags, StreamContext.Default, -1, -1);
		}

		/// <summary>
		/// Reads entire file into a string.
		/// </summary>
        [ImplementsFunction("file_get_contents", FunctionImplOptions.NeedsVariables)]
		[return: CastToFalse]
        public static object ReadContents(ScriptContext scriptcontext, System.Collections.Generic.Dictionary<string, object> definedVariables, string path, FileOpenOptions flags, PhpResource context)
		{
            return ReadContents(scriptcontext, definedVariables, path, flags, context, -1, -1);
		}

		/// <summary>
		/// Reads entire file into a string.
		/// </summary>
        [ImplementsFunction("file_get_contents", FunctionImplOptions.NeedsVariables)]
		[return: CastToFalse]
        public static object ReadContents(ScriptContext scriptcontext, System.Collections.Generic.Dictionary<string, object> definedVariables, string path, FileOpenOptions flags, PhpResource context,
		  int offset)
		{
            return ReadContents(scriptcontext, definedVariables, path, flags, context, offset, -1);
		}

		/// <summary>
		/// Reads entire file into a string.
		/// </summary>
		/// <remarks>
		/// Result is affected by run-time quoting 
		/// (<see cref="LocalConfiguration.VariablesSection.QuoteRuntimeVariables"/>).
		/// </remarks>
        [ImplementsFunction("file_get_contents", FunctionImplOptions.NeedsVariables)]
		[return: CastToFalse]
		public static object ReadContents(ScriptContext scriptcontext, System.Collections.Generic.Dictionary<string, object> definedVariables, string path, FileOpenOptions flags, PhpResource context,
		  int offset, int maxLength)
		{
            StreamContext sc = StreamContext.GetValid(context, true);
            if (sc == null)
                return null;
            
			using (PhpStream stream = PhpStream.Open(path, "rb", ProcessOptions(flags), sc))
			{
				if (stream == null) return null;

                // when HTTP protocol requested, store responded headers into local variable $http_response_header:
                // NOTE: (J) this should be applied by HTTP wrapper itself, not only by this function.
                if (string.Compare(stream.Wrapper.Scheme, "http", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    var headers = stream.WrapperSpecificData as PhpArray;
                    Operators.SetVariable(scriptcontext, definedVariables, HttpResponseHeaderName, headers);                    
                }

                //
				return Core.Convert.Quote(stream.ReadContents(maxLength, offset), ScriptContext.CurrentContext);
			}
		}

		[ImplementsFunction("file_put_contents")]
		[return: CastToFalse]
		public static int WriteContents(string path, object data)
		{
			return WriteContents(path, data, 0, StreamContext.Default);
		}

		[ImplementsFunction("file_put_contents")]
		[return: CastToFalse]
		public static int WriteContents(string path, object data, WriteContentsOptions flags)
		{
			return WriteContents(path, data, flags, StreamContext.Default);
		}

		[ImplementsFunction("file_put_contents")]
		[return: CastToFalse]
		public static int WriteContents(string path, object data, WriteContentsOptions flags, PhpResource context)
		{
            StreamContext sc = StreamContext.GetValid(context, true);
			if (sc == null) return -1;

			string mode = (flags & WriteContentsOptions.AppendContents) > 0 ? "ab" : "wb";
			using (PhpStream to = PhpStream.Open(path, mode, ProcessOptions((FileOpenOptions)flags), sc))
			{
				if (to == null) return -1;

				// passing array is equivalent to file_put_contents($filename, join('', $array))
				PhpArray array = data as PhpArray;
				if (array != null)
				{
					int total = 0;

					foreach (object o in array.Values)
					{
						int written = to.WriteBytes(Core.Convert.ObjectToPhpBytes(o));
						if (written == -1) return total;
						total += written;
					}

					return total;
				}

				// as of PHP 5.1.0, you may also pass a stream resource to the data parameter
				PhpResource resource = data as PhpResource;
				if (resource != null)
				{
					PhpStream from = PhpStream.GetValid(resource);
					if (from == null) return -1;

					return PhpStreams.Copy(from, to);
				}

				return to.WriteBytes(Core.Convert.ObjectToPhpBytes(data));
			}
		}

		#endregion
        
		#region Seek (fseek, rewind, ftell, ftruncate)

		/// <summary>
		/// Seeks on a file pointer.
		/// </summary>
		/// <param name="handle">The file stream resource.</param>
		/// <param name="offset">The number of bytes to seek.</param>
		/// <returns>Upon success, returns 0; otherwise, returns -1. 
		/// Note that seeking past EOF is not considered an error.</returns>
		[ImplementsFunction("fseek")]
		public static int Seek(PhpResource handle, int offset)
		{
			return Seek(handle, offset, (int)SeekOptions.Set);
		}

		/// <summary>
		/// Seeks on a file pointer.
		/// </summary>
		/// <param name="handle">A file stream resource.</param>
		/// <param name="offset">The number of bytes to seek.</param>
		/// <param name="whence">The position in stream to seek from.
		/// May be one of the SeekOptions flags.</param>
		/// <returns>Upon success, returns 0; otherwise, returns -1. 
		/// Note that seeking past EOF is not considered an error.</returns>
		[ImplementsFunction("fseek")]
		public static int Seek(PhpResource handle, int offset, int whence)
		{
			PhpStream stream = PhpStream.GetValid(handle);
			if (stream == null) return -1;
			return stream.Seek(offset, (SeekOrigin)whence) ? 0 : -1;
		}

		/// <summary>
		/// Rewind the position of a file pointer.
		/// </summary>
		/// <param name="handle"></param>
		/// <returns></returns>
		[ImplementsFunction("rewind")]
		public static bool Rewind(PhpResource handle)
		{
			PhpStream stream = PhpStream.GetValid(handle);
			if (stream == null) return false;
			return stream.Seek(0, SeekOrigin.Begin);
		}

		/// <summary>
		/// Tells file pointer read/write position.
		/// </summary>
		/// <param name="handle">A file stream resource.</param>
		/// <returns></returns>
		[ImplementsFunction("ftell")]
		[return: CastToFalse]
		public static int Tell(PhpResource handle)
		{
			PhpStream stream = PhpStream.GetValid(handle);
			if (stream == null) return -1;
			return stream.Tell();
		}

		/// <summary>
		/// Truncates a file to a given length.
		/// </summary>
		/// <param name="handle"></param>
		/// <param name="size"></param>
		/// <returns></returns>
		[ImplementsFunction("ftruncate")]
		public static bool Truncate(PhpResource handle, int size)
		{
            PhpStream stream = PhpStream.GetValid(handle);
            if (stream == null) return false;

            if (stream.RawStream != null && stream.RawStream.CanWrite && stream.RawStream.CanSeek)
            {
                stream.RawStream.SetLength(size);
                return true;
            }

            return false;
		}

		#endregion

		#region FileSystem Access (copy, rename, unlink, mkdir, rmdir, flock)

		/// <summary>
		/// Copies a file (even accross different stream wrappers).
		/// </summary>
		/// <remarks>
		/// If the destination file already exists, it will be overwritten. 
		/// <para>
		/// Note: As of PHP 4.3.0, both source and dest may be URLs if the 
		/// "fopen wrappers" have been enabled. See <c>fopen()</c> for more details. 
		/// If dest is an URL, the copy operation may fail if the wrapper does 
		/// not support overwriting of existing files. 
		/// </para> 
		/// </remarks>
		/// <param name="source">Source URL.</param>
		/// <param name="dest">Destination URL.</param>
		/// <returns><c>true</c> on success or <c>false</c> on failure.</returns>
		[ImplementsFunction("copy")]
		public static bool Copy(string source, string dest)
		{
			StreamWrapper reader, writer;
			if ((!PhpStream.ResolvePath(ref source, out reader, CheckAccessMode.FileExists, CheckAccessOptions.Empty))
				|| (!PhpStream.ResolvePath(ref dest, out writer, CheckAccessMode.FileExists, CheckAccessOptions.Empty)))
				return false;

			if ((reader.Scheme == "file") && (writer.Scheme == "file"))
			{
				// Copy the file.
				try
				{
					File.Copy(source, dest, true);
					return true;
				}
				catch (System.Exception)
				{
					return false;
				}
			}
			else
			{
				// Copy the two files using the appropriate stream wrappers.
				using (PhpResource from = reader.Open(ref source, "rb", StreamOpenOptions.Empty, StreamContext.Default))
				{
					if (from == null) return false;
					using (PhpResource to = writer.Open(ref dest, "wb", StreamOpenOptions.Empty, StreamContext.Default))
					{
						if (to == null) return false;

						int copied = PhpStreams.Copy(from, to);
						return copied >= 0;
					}
				}
			}
		}

		/// <summary>
		/// Renames a file.
		/// </summary>
		/// <remarks>
		/// Both the <paramref name="oldpath"/> and the <paramref name="newpath"/>
		/// must be handled by the same wrapper.
		/// </remarks>
		/// <param name="oldpath"></param>
		/// <param name="newpath"></param>
		/// <returns></returns>
		[ImplementsFunction("rename")]
		public static bool Rename(string oldpath, string newpath)
		{
			StreamWrapper oldwrapper, newwrapper;
			if ((!PhpStream.ResolvePath(ref oldpath, out oldwrapper, CheckAccessMode.FileExists, CheckAccessOptions.Empty))
				|| (!PhpStream.ResolvePath(ref newpath, out newwrapper, CheckAccessMode.FileMayExist, CheckAccessOptions.Empty)))
				return false;

			if (oldwrapper != newwrapper)
			{
				PhpException.Throw(PhpError.Warning, LibResources.GetString("wrappers_must_match"));
			}

			return oldwrapper.Rename(oldpath, newpath, StreamRenameOptions.Empty, StreamContext.Default);
		}

		/// <summary>
		/// Deletes a file using a StreamWrapper corresponding to the given URL.
		/// </summary>
		/// <param name="path">An URL of a file to be deleted.</param>
		/// <returns>True in case of success.</returns>
		[ImplementsFunction("unlink")]
		public static bool Delete(string path)
		{
            return Delete(path, null);
		}

        /// <summary>
        /// Deletes a file using a StreamWrapper corresponding to the given URL.
        /// </summary>
        /// <param name="path">An URL of a file to be deleted.</param>
        /// <param name="context">StreamContext.</param>
        /// <returns>True in case of success.</returns>
        [ImplementsFunction("unlink")]
        public static bool Delete(string path, PhpResource context)
        {
            if (String.IsNullOrEmpty(path))
            {
                PhpException.Throw(PhpError.Warning, LibResources.GetString("arg:empty", "path"));
                return false;
            }

            StreamContext sc = StreamContext.GetValid(context, true);
            if (sc == null) // PHP warning is thrown by StreamContext.GetValid
                return false;
            
            StreamWrapper wrapper;
            if (!PhpStream.ResolvePath(ref path, out wrapper, CheckAccessMode.FileExists, CheckAccessOptions.Empty))
                return false;

            // Clear the cache (the currently deleted file may have been cached)
#if !SILVERLIGHT
            ClearStatCache();
#endif
            return wrapper.Unlink(path, 0, sc);
        }


		/// <summary>
		/// 
		/// </summary>
		/// <param name="handle"></param>
		/// <param name="operation"></param>
		/// <returns></returns>
        [ImplementsFunction("flock")]
		public static bool Lock(PhpResource handle, int operation)
		{
			int dummy = 0;
			return Lock(handle, operation, ref dummy);
		}

        /// <summary>
        /// Portable advisory file locking.
        /// </summary>
        /// <param name="handle">A file system pointer resource that is typically created using fopen().</param>
        /// <param name="operation">Operation is one of the following:
        /// <c>LOCK_SH</c> to acquire a shared lock (reader).
        /// <c>LOCK_EX</c> to acquire an exclusive lock (writer).
        /// <c>LOCK_UN</c> to release a lock (shared or exclusive).
        /// 
        /// It is also possible to add <c>LOCK_NB</c> as a bitmask to one of the above operations if you don't want flock() to block while locking. (not supported on Windows)
        /// </param>
        /// <param name="wouldblock">The optional third argument is set to TRUE if the lock would block (EWOULDBLOCK errno condition). (not supported on Windows)</param>
        /// <returns>Returns <c>true</c> on success or <c>false</c> on failure.</returns>
        [ImplementsFunction("flock")]
        public static bool Lock(PhpResource handle, int operation, ref int wouldblock)
        {
            // Get the native file handle for the PHP resource
            var phpStream = PhpStream.GetValid(handle);
            if (phpStream == null) return false;

            var fileStream = phpStream.RawStream as FileStream;
            if (fileStream == null) return false;

            //
            if (EnvironmentUtils.IsDotNetFramework)
            {
                return Lock_dotNET(fileStream, (StreamLockOptions)operation);
            }
            else
            {
                PhpException.FunctionNotSupported();
                return false;
            }
        }

        #region flock (Windows)

        // Constants passed to LockFileEx for the flags
        private const uint LOCKFILE_FAIL_IMMEDIATELY = 0x00000001;
        private const uint LOCKFILE_EXCLUSIVE_LOCK = 0x00000002;

        [DllImport("kernel32.dll")]
        static extern bool LockFileEx(SafeFileHandle hFile, uint dwFlags, uint dwReserved, uint nNumberOfBytesToLockLow, uint nNumberOfBytesToLockHigh, [In] ref NativeOverlapped lpOverlapped);

        [DllImport("kernel32.dll")]
        static extern bool UnlockFileEx(SafeFileHandle hFile, uint dwReserved, uint nNumberOfBytesToUnlockLow, uint nNumberOfBytesToUnlockHigh, [In] ref NativeOverlapped lpOverlapped);

        private static bool Lock_dotNET(FileStream/*!*/fileStream, StreamLockOptions op)
        {
            Debug.Assert(fileStream != null);

            var hFile = fileStream.SafeFileHandle;

            // Set up some parameters
            uint low = 1, high = 0;
            var offset = new NativeOverlapped();
            bool noBlocking = (op & StreamLockOptions.NoBlocking) != 0;

            // bug for bug compatible with Unix 
            UnlockFileEx(hFile, 0, low, high, ref offset);

            //
            switch (op & ~StreamLockOptions.NoBlocking)
            {
                case StreamLockOptions.Exclusive:
                    // Exclusive lock
                    return LockFileEx(hFile, LOCKFILE_EXCLUSIVE_LOCK + (noBlocking ? LOCKFILE_FAIL_IMMEDIATELY : 0), 0, low, high, ref offset);

                case StreamLockOptions.Shared:
                    // Shared lock
                    return LockFileEx(hFile, (noBlocking ? LOCKFILE_FAIL_IMMEDIATELY : 0), 0, low, high, ref offset);

                case StreamLockOptions.Unlock:
                    // Unlock always succeeds
                    return true;
            }

            // Bad call
            return false;
        }

        #endregion

        #endregion
    }
}
