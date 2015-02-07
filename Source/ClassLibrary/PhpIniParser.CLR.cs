/*

 Copyright (c) 2005-2006 Ladislav Prosek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

TODO:
 Added possibility to access INI variables from within .ini file. (Andrei) (PHP 5.1.0)

*/

using System;
using System.Text;
using PHP.Core;

namespace PHP.Library
{
	/// <summary>
	/// Implements the INI file parsing functionality (see the <c>parse_ini_file</c> PHP function).
	/// </summary>
	/// <remarks>
	/// The parser is implemented by hand using the recursive descent (LL) approach.
    /// Note (jakub): can be implemented using regular expressions ?
	/// </remarks>
	public sealed class PhpIniParser
    {
        /// <summary>
        /// Type of sorting.
        /// </summary>
        public enum ScannerMode
        {
            /// <summary>Normal scanner mode.</summary>
            [ImplementsConstant("INI_SCANNER_NORMAL")]
            Normal = 0,

            /// <summary>Raw scanner mode.</summary>
            [ImplementsConstant("INI_SCANNER_RAW")]
            Raw = 1,
        };

        #region parse_ini_string


        /// <summary>
        /// Parse a configuration string.
        /// </summary>
        /// <param name="ini">The contents of the ini file being parsed. </param>
        /// <returns>The settings are returned as an associative array on success, and FALSE on failure. </returns>
        [ImplementsFunction("parse_ini_string")]
        [return: CastToFalse]
		public static PhpArray ParseString(string ini)
		{
            return ParseString(ini, false, 0);
		}

        /// <summary>
        /// Parse a configuration string.
        /// </summary>
        /// <param name="ini">The contents of the ini file being parsed. </param>
        /// <param name="processSections">By setting the process_sections  parameter to TRUE, you get a multidimensional array, with the section names and settings included. The default for process_sections  is FALSE</param>
        /// <returns>The settings are returned as an associative array on success, and FALSE on failure. </returns>
        [ImplementsFunction("parse_ini_string")]
        [return: CastToFalse]
        public static PhpArray ParseString(string ini, bool processSections)
        {
            return ParseString(ini, processSections, 0);
        }

        /// <summary>
        /// Parse a configuration string.
        /// </summary>
        /// <param name="ini">The contents of the ini file being parsed. </param>
        /// <param name="processSections">By setting the process_sections  parameter to TRUE, you get a multidimensional array, with the section names and settings included. The default for process_sections  is FALSE</param>
        /// <param name="scanner_mode">Can either be INI_SCANNER_NORMAL (default) or INI_SCANNER_RAW. If INI_SCANNER_RAW is supplied, then option values will not be parsed. </param>
        /// <returns>The settings are returned as an associative array on success, and FALSE on failure. </returns>
		[ImplementsFunction("parse_ini_string")]
        [return: CastToFalse]
        public static PhpArray ParseString(string ini, bool processSections, int scanner_mode)
        {
            if (scanner_mode != (int)ScannerMode.Normal)  // TODO: handle value 1
                PhpException.ArgumentValueNotSupported("scanner_mode");

            if (string.IsNullOrEmpty(ini))
                return null;

            ArrayBuilder builder = new ArrayBuilder(ScriptContext.CurrentContext, processSections);

            try
            {
                // parse the stream and let the builder build the resulting array
                Parse(ini, builder);
            }
            catch (ParseException e)
            {
                PhpException.Throw(PhpError.Warning, LibResources.GetString("ini_parse_error", e.LineNumber));
                return null;
            }

            // return what we have read so far - even if a parse error occurred
            return builder.Result;
        }

        #endregion

        #region parse_ini_file

        /// <summary>
		/// Parses an INI-style configuration file.
		/// </summary>
		/// <param name="fileName">A file designation (may be a local file path, an URL, or whatever is accepted
		/// by <c>fopen</c>).</param>
		/// <returns>An array of key-value pairs read from the INI file.</returns>
		/// <exception cref="PhpException">Parse error (Warning).</exception>
		/// <remarks>
		/// Section names and keys are converted to integers if applicable. The parser recognizes special values
		/// <c>true</c>, <c>on</c> and <c>yes</c> (converted to &quot;1&quot;); and <c>false</c>, <c>off</c>,
		/// <c>no</c> and <c>none</c> (converted to &quot;&quot;).
		/// </remarks>
		[ImplementsFunction("parse_ini_file")]
        [return: CastToFalse]
		public static PhpArray ParseFile(string fileName)
		{
			return ParseFile(fileName, false, 0);
		}

        /// <summary>
        /// Parses an INI-style configuration file.
        /// </summary>
        /// <param name="fileName">A file designation (may be a local file path, an URL, or whatever is accepted
        /// by <c>fopen</c>).</param>
        /// <param name="processSections">If <B>true</B>, the returned array contains nested arrays corresponding
        /// to individual INI sections. If <B>false</B>, section names are completely disregarded and the returned
        /// array contains directly key-value pairs from all sections.</param>
        /// <returns>An array of key-value pairs read from the INI file.</returns>
        /// <exception cref="PhpException">Parse error (Warning).</exception>
        /// <remarks>
        /// Section names and keys are converted to integers if applicable. The parser recognizes special values
        /// <c>true</c>, <c>on</c> and <c>yes</c> (converted to &quot;1&quot;); and <c>false</c>, <c>off</c>,
        /// <c>no</c> and <c>none</c> (converted to &quot;&quot;).
        /// </remarks>
        [ImplementsFunction("parse_ini_file")]
        [return: CastToFalse]
        public static PhpArray ParseFile(string fileName, bool processSections)
        {
            return ParseFile(fileName, processSections, 0);
        }

		/// <summary>
		/// Parses an INI-style configuration file.
		/// </summary>
		/// <param name="fileName">A file designation (may be a local file path, an URL, or whatever is accepted
		/// by <c>fopen</c>).</param>
		/// <param name="processSections">If <B>true</B>, the returned array contains nested arrays corresponding
		/// to individual INI sections. If <B>false</B>, section names are completely disregarded and the returned
		/// array contains directly key-value pairs from all sections.</param>
        /// <param name="scanner_mode">Can either be INI_SCANNER_NORMAL (default) = 0 or INI_SCANNER_RAW = 1. If INI_SCANNER_RAW is supplied, then option values will not be parsed. </param>
		/// <returns>An array of key-value pairs (<paramref name="processSections"/> is <B>false</B>) or an array
		/// of arrays of key-value pairs (<paramref name="processSections"/> is <B>true</B>).</returns>
		/// <exception cref="PhpException">Parse error (Warning).</exception>
		/// <remarks>
		/// Section names and keys are converted to integers if applicable. The parser recognizes special values
		/// <c>true</c>, <c>on</c> and <c>yes</c> (converted to &quot;1&quot;); and <c>false</c>, <c>off</c>,
		/// <c>no</c> and <c>none</c> (converted to &quot;&quot;).
		/// </remarks>
		[ImplementsFunction("parse_ini_file")]
        [return: CastToFalse]
        public static PhpArray ParseFile(string fileName, bool processSections, int scanner_mode)
		{
            if (scanner_mode != (int)ScannerMode.Normal)  // TODO: handle value 1
                PhpException.ArgumentValueNotSupported("scanner_mode");

			// we're using binary mode because CR/LF stuff should be preserved for multiline values
			using (PhpStream stream = PhpStream.Open(fileName, "rb", StreamOpenOptions.ReportErrors,
					   StreamContext.Default))
			{
                if (stream == null) return null;//new PhpArray();

				ArrayBuilder builder = new ArrayBuilder(ScriptContext.CurrentContext, processSections);
				try
				{
					// parse the stream and let the builder build the resulting array
					Parse(stream, builder);
				}
				catch (ParseException e)
				{
					PhpException.Throw(PhpError.Warning, LibResources.GetString("ini_parse_error", e.LineNumber));
                    return null;
				}

				// return what we have read so far - even if a parse error occurred
				return builder.Result;
			}
		}

		/// <summary>
		/// Parses an INI-style configuration file.
		/// </summary>
		/// <param name="stream">A stream referring to the file to parse. Should be open in binary mode.</param>
		/// <param name="callbacks">Implementation of the parser callbacks invoked during parsing.</param>
		/// <exception cref="ParseException">Parse error.</exception>
		/// <exception cref="ArgumentNullException"><paramref name="stream"/> or <paramref name="callbacks"/> is a <B>null</B> reference.</exception>
		/// <exception cref="ArgumentException">Stream is was not opened as binary.</exception>
		public static void Parse(PhpStream stream, IParserCallbacks callbacks)
		{
			if (stream == null)
				throw new ArgumentNullException("stream");
			if (!stream.IsBinary)
				throw new ArgumentException("Stream must be binary");
			if (callbacks == null)
				throw new ArgumentNullException("callbacks");

			PhpIniParser parser = new PhpIniParser(stream, callbacks);
			parser.TopLevel();
		}

        public static void Parse(string ini, IParserCallbacks callbacks)
        {
            if (ini == null)
                throw new ArgumentNullException("ini");
            if (callbacks == null)
                throw new ArgumentNullException("callbacks");

            PhpIniParser parser = new PhpIniParser(ini, callbacks);
            parser.TopLevel();
        }

		#endregion

		#region IParserCallbacks

		/// <summary>
		/// Groups together methods that are called back by the parser during parsing.
		/// </summary>
		public interface IParserCallbacks
		{
			/// <summary>
			/// Called when an INI section is encountered.
			/// </summary>
			/// <param name="sectionName">The section name (without the enclosing brackets). Either a string or
			/// an integer.</param>
			void ProcessSection(object sectionName);

			/// <summary>
			/// Called when an option (i.e. a key-value pair) is encountered.
			/// </summary>
			/// <param name="key">The key. Either a string or an integer.</param>
			/// <param name="value">The value.</param>
			void ProcessOption(object key, string value);

			/// <summary>
			/// Called when a token, which might possibly denote a constant, should be resolved.
			/// </summary>
			/// <param name="name">The constant name.</param>
			/// <returns>The constant value. Should be either a string or an integer. Usually, when the
			/// implementing method is unable to resolve the constant, it simply returns <paramref name="name"/>.
			/// </returns>
			object GetConstantValue(string name);
		}

		#endregion

		#region ArrayBuilder

		/// <summary>
		/// Provides an array-building implementation of the the parser callbacks.
		/// </summary>
		/// <remarks>
		/// The format of the resulting <see cref="PhpArray"/> complies to the <c>parse_ini_file</c>
		/// return value.
		/// </remarks>
		public class ArrayBuilder : IParserCallbacks
		{
			#region Fields and Properties

			/// <summary>
			/// The resulting array.
			/// </summary>
			private PhpArray result;

			/// <summary>
			/// The section currently being processed.
			/// </summary>
			private PhpArray currentSection;

			/// <summary>
			/// The <see cref="ScriptContext"/> used to lookup constants.
			/// </summary>
			private ScriptContext scriptContext;

			/// <summary>
			/// A flag that affects the way the <see cref="result"/> is built up.
			/// </summary>
			/// <remarks>
			/// If <B>true</B>, the resulting array contains nested arrays corresponding to individual
			/// INI sections. If <B>false</B>, section names are completely disregarded and the resulting
			/// array contains directly key-value pairs from all sections.
			/// </remarks>
			private bool processSections;

			/// <summary>
			/// Returns the resulting array.
			/// </summary>
			public PhpArray Result
			{
				get
				{ return result; }
			}

			#endregion

			#region Construction

			/// <summary>
			/// Creates a new <see cref="ArrayBuilder"/>.
			/// </summary>
			/// <param name="scriptContext">The <see cref="ScriptContext"/> used to lookup constants or a <B>null</B> reference.</param>
			/// <param name="processSections">If <B>true</B>, the resulting array contains nested arrays
			/// corresponding to individual INI sections. If <B>false</B>, section names are completely
			/// disregarded and the resulting array contains directly key-value pairs from all sections.</param>
			/// <exception cref="ArgumentNullException"><paramref name="scriptContext"/> is a <B>null</B> reference.</exception>
			public ArrayBuilder(ScriptContext scriptContext, bool processSections)
			{
				this.scriptContext = scriptContext;
				this.processSections = processSections;
				result = new PhpArray();
				currentSection = result;
			}

			#endregion

			#region IParserCallbacks Members

			/// <summary>
			/// Called when an INI section is encountered.
			/// </summary>
			public void ProcessSection(object sectionName)
			{
				if (processSections)
				{
					currentSection = new PhpArray();
					result[sectionName] = currentSection;
				}
			}

			/// <summary>
			/// Called when an option (i.e. a key-value pair) is encountered.
			/// </summary>
			public void ProcessOption(object key, string value)
			{
				currentSection[key] = value;
			}

			/// <summary>
			/// Called when a token, which might possibly denote a constant, should be resolved.
			/// </summary>
			public object GetConstantValue(string name)
			{
				return scriptContext.GetConstantValue(name, true, true);
			}

			#endregion
		}

		#endregion

		#region Tokens

		/// <summary>
		/// Contains definition of (one-character) tokens that are relevant for INI files.
		/// </summary>
		internal class Tokens
		{
			internal const char BracketOpen = '[';
			internal const char BracketClose = ']';
			internal const char EqualS = '=';
			internal const char Quote = '"';
			internal const char Semicolon = ';';

			internal const char Or = '|';
			internal const char And = '&';
			internal const char Not = '!';
			internal const char Neg = '~';
			internal const char ParOpen = '(';
			internal const char ParClose = ')';

			internal const char EndOfLine = (char)0;
		}

		#endregion

		#region ParseException

		/// <summary>
		/// An exception thrown by the parser when an error occurs.
		/// </summary>
		internal class ParseException : ApplicationException
		{
			/// <summary>
			/// Number of the line where the parse error occured.
			/// </summary>
			private int lineNumber;

			/// <summary>
			/// Returns the number of the line where the parse error occured.
			/// </summary>
			public int LineNumber
			{
				get
				{ return lineNumber; }
			}

			/// <summary>
			/// Creates a new <see cref="ParseException"/>.
			/// </summary>
			/// <param name="lineNumber">Number of the line where the parse error occured.</param>
			public ParseException(int lineNumber)
			{
				this.lineNumber = lineNumber;
			}
		}

		#endregion

        #region Line getter

        /// <summary>
        /// Interface for getting next line from the source.
        /// </summary>
        private abstract class LineGetter
        {
            /// <summary>
            /// Get the next line from the source. Every line must ends with "\n".
            /// </summary>
            /// <returns>Next line or null if you reach the end of the source.</returns>
            public abstract string GetLine();
        }

        /// <summary>
        /// Getting next line from the PhpStream.
        /// </summary>
        private sealed class LineGetterStream:LineGetter
        {
            /// <summary>
            /// A stream representing the input INI file, instead of text.
            /// </summary>
            private PhpStream stream;

            public LineGetterStream(PhpStream stream)
            {
                if (stream == null)
                    throw new ArgumentNullException("stream");

                this.stream = stream;
            }

            public override string GetLine()
            {
                if (stream.Eof) return null;

                return stream.ReadLine(-1, null);
            }
        }

        /// <summary>
        /// Getting  next line from the string.
        /// </summary>
        private sealed class LineGetterString:LineGetter
        {
            private string[] lines;
            private int nextLineIndex = 0;

            public LineGetterString(string text)
            {
                if (text == null)
                    lines = ArrayUtils.EmptyStrings;
                else
                    lines = text.Split(new char[] { '\n' });
            }

            public override string GetLine()
            {
                if (nextLineIndex < lines.Length)
                    return lines[nextLineIndex++] + "\n";
                else
                    return null;
            }
        }

        #endregion

        #region Fields

        /// <summary>
        /// Object getting next lines of INI source.
        /// </summary>
        private LineGetter lineGetter;

        /// <summary>
		/// Implementation of the parser callbacks invoked during parsing.
		/// </summary>
		private IParserCallbacks callbacks;

		/// <summary>
        /// The line read from <see cref="lineGetter"/> that is currently being processed.
		/// </summary>
		private string line;

		/// <summary>
		/// Current line number.
		/// </summary>
		private int lineNumber = 0;

		/// <summary>
		/// The position within the current <see cref="line"/> (0-based column).
		/// </summary>
		/// <remarks>
		/// <c>line[linePos]</c> denotes a lookahead symbol.
		/// </remarks>
		private int linePos;

		#endregion

		#region Construction

		/// <summary>
		/// Creates a new <see cref="PhpIniParser"/> operating on a given input stream.
		/// </summary>
		/// <param name="stream">The input stream. Should be open in binary mode.</param>
		/// <param name="callbacks">Implementation of the parser callbacks invoked during parsing.</param>
		private PhpIniParser(PhpStream stream, IParserCallbacks callbacks)
		{
            this.lineGetter = new LineGetterStream(stream);
			this.callbacks = callbacks;
		}

        /// <summary>
        /// Creates a new <see cref="PhpIniParser"/> operating on a given input stream.
        /// </summary>
        /// <param name="text">The input INI file content.</param>
        /// <param name="callbacks">Implementation of the parser callbacks invoked during parsing.</param>
        private PhpIniParser(string text, IParserCallbacks callbacks)
        {
            this.lineGetter = new LineGetterString(text);
            this.callbacks = callbacks;
        }

		#endregion

		#region Parser helpers: LoadLine, LookAhead, Consume*, AddValue

		/// <summary>
        /// Loads the next line from <see cref="lineGetter"/>, and updates <see cref="line"/> and <see cref="linePos"/>.
		/// </summary>
		/// <returns><B>true</B> if a line was successfully loaded, <B>false</B> if end-of-file was reached.</returns>
		private bool LoadLine()
		{
            line = lineGetter.GetLine();
            linePos = 0;

            if (line != null)
            {
                lineNumber++;
                return true;
            }
            else
            {
                return false;
            }
		}

		/// <summary>
		/// Returns the lookahead symbol which is either <c>line[linePos]</c> or <see cref="Tokens.EndOfLine"/>
		/// if there are no more characters in the current line.
		/// </summary>
		private char LookAhead
		{
			get
			{
				return (linePos < line.Length ? line[linePos] : Tokens.EndOfLine);
			}
		}

		/// <summary>
		/// Returns the lookahead symbol and cosumes it, i.e. advances the <see cref="linePos"/>.
		/// <seealso cref="LookAhead"/>
		/// </summary>
		/// <returns>The original lookahead (before advancing).</returns>
		private char Consume()
		{
			return (linePos < line.Length ? line[linePos++] : Tokens.EndOfLine);
		}

		/// <summary>
		/// Consumes the current lookahead symbol and compares it to a given character.
		/// </summary>
		/// <param name="ch">The character that is expected as the current lookahead symbol. If the characters
		/// do not match, a <see cref="ParseException"/> is thrown.</param>
		private void Consume(char ch)
		{
			if (Consume() != ch) throw new ParseException(lineNumber);
		}

		/// <summary>
		/// Keeps consuming the current lookahead as long as it is categorized as a white space.
		/// </summary>
        private void ConsumeWhiteSpace()
        {
            while (linePos < line.Length && Char.IsWhiteSpace(line, linePos)) linePos++;
        }

		/// <summary>
		/// Adds a key-value pair into a <see cref="PhpArray"/>. If a value with the same key already exists,
		/// a nested <see cref="PhpArray"/> containing all values with this key is created.
		/// </summary>
		/// <param name="array">The array to add the pair to.</param>
		/// <param name="key">The key.</param>
		/// <param name="value">The value.</param>
		private static void AddValue(PhpArray array, object key, string value)
		{
			if (!array.Contains(key)) array.Add(key, value);
			else
			{
				object old_value = array[key];
				PhpArray nested_array = old_value as PhpArray;

				if (nested_array != null) nested_array.Add(value);
				else
				{
					nested_array = new PhpArray();
					nested_array.Add(old_value);
					nested_array.Add(value);

					array[key] = nested_array;
				}
			}
		}

		#endregion

		#region Conversions

		/// <summary>
		/// Converts a substring of the current <see cref="line"/> into the array key representing a section name
		/// (the interior of <c>[ ]</c>) or an option name (on the left of <c>=</c>).
		/// </summary>
		/// <param name="start">The start index of the substring (within the current <see cref="line"/>).</param>
		/// <param name="length">The length of the substring.</param>
		/// <returns>The key found at the position given by <paramref name="start"/> and <paramref name="length"/>.
		/// This may be either a string or an integer (decimal, hexadecimal and octal numbers are decoded).</returns>
		private object SubstringToKey(int start, int length)
		{
			Debug.Assert(start >= 0 && start <= line.Length && (start + length) <= line.Length && length > 0);

			int radix = 10;
			if (line[start] == '0')
			{
				if (length > 2 && (line[start + 1] == 'x' || line[start + 1] == 'X'))
				{
					radix = 16;

					// skip the "0x" prefix
					start += 2;
					length -= 2;
				}
				else radix = 8;
			}

			int pos = start;
			int res = (int)Core.Convert.SubstringToLongStrict(line, length, radix, Int32.MaxValue, ref pos);

			if (pos == start + length) return res;
			else return line.Substring(start, length);
		}

		/// <summary>
		/// Converts a substring of the current <see cref="line"/> into the array value representing an option
		/// value (on the right of <c>=</c>).
		/// </summary>
		/// <param name="start">The start index of the substring (within the current <see cref="line"/>).</param>
		/// <param name="length">The length of the substring.</param>
		/// <returns>The value found at the position given by <paramref name="start"/> and <paramref name="length"/>.
		/// This may be either a string or an integer (decimal numbers are decoded and constants are looked up).
		/// </returns>
		private object SubstringToValue(int start, int length)
		{
			Debug.Assert(start >= 0 && start <= line.Length && (start + length) <= line.Length);

			if (length == 0) return String.Empty;

			// check for decimal number
			int pos = start;
			int res = Core.Convert.SubstringToInteger(line, length, ref pos);
			if (pos == start + length) return res;

			string val = line.Substring(start, length);

			// check for predefined "INI constants"
			switch (val.ToUpper())
			{
				case "ON":
				case "YES": return "1";

				case "OFF":
				case "NO":
				case "NONE": return String.Empty;

				default:
					{
						object obj = callbacks.GetConstantValue(val);
						if (obj is int) return obj;
						else return Core.Convert.ObjectToString(obj);
					}
			}
		}

		#endregion

		#region Parser

		/// <summary>
		/// Top level parsing method. 
		/// </summary>
		/// <remarks>
		/// Reads and processes lines from the input stream until the end-of-file is reached. Invokes the
		/// <see cref="callbacks"/> during parsing.
		/// </remarks>
		private void TopLevel()
		{
			StringBuilder val = null;
			bool multiline = false;
			object key = null;

			while (LoadLine())
			{
				if (multiline)
				{
					// this is the next line of a multi-line value
					val.Append(Value(ref multiline));

					// it is the last line of multi-line value, save the entire value
					if (!multiline) callbacks.ProcessOption(key, val.ToString());
				}
				else
				{
					ConsumeWhiteSpace();
					char la = LookAhead;

					// check for end-of-line and comment (line starting with ;)
					if (la == Tokens.EndOfLine || la == Tokens.Semicolon) continue;

					if (la == Tokens.BracketOpen)
					{
						// this line denotes a section
						callbacks.ProcessSection(Section());
					}
					else if ((key = Key()) != null)
                    {
                        // this line denotes an ordinary entry
						string s = Value(ref multiline);
						if (!multiline) callbacks.ProcessOption(key, s);
						else val = new StringBuilder(s);
					}
				}
			}

			// check for an unterminated multi-line value
			if (multiline) throw new ParseException(lineNumber);
		}

		/// <summary>
		/// Parses an INI section (<c>[&lt;section_name&gt;&lt;whitespace&gt;]&lt;whitespace&gt;</c>).
		/// </summary>
		/// <returns>The section name (either a string or an integer).</returns>
		private object Section()
		{
			Consume(Tokens.BracketOpen);
			//ConsumeWhiteSpace();

			int start = linePos, end = linePos;
			char ch;
			while ((ch = Consume()) != Tokens.BracketClose)
			{
				if (ch == Tokens.EndOfLine) throw new ParseException(lineNumber);

				// remember the last non-whitespace
				/*if (!Char.IsWhiteSpace(ch)) */end = linePos;  // include white-spaces too, as it is in PHP 5.3.1
			}

			ConsumeWhiteSpace();

			// section name must not be empty and we must have reached end-of-line by now
			if (end == start || (LookAhead != Tokens.EndOfLine && LookAhead != Tokens.Semicolon))
			{
				throw new ParseException(lineNumber);
			}

			return SubstringToKey(start, end - start);
		}

		/// <summary>
		/// Parses an INI option name (<c>&lt;option_name&gt;&lt;whitespace&gt;=</c>).
		/// </summary>
		/// <returns>The option name (either a string or an integer).</returns>
		private object Key()
		{
			int start = linePos, end = linePos, whitespace = start - 1;
			char ch;
			while ((ch = Consume()) != Tokens.EqualS)
			{
				if (ch == Tokens.EndOfLine || ch == Tokens.Semicolon) return null;

				// remember the last non-whitespace and whitespace
                if (!Char.IsWhiteSpace(ch))
                {
                    end = linePos;

                    if (linePos == (whitespace + 1))  // new word starts, ignore the words before
                        start = linePos - 1;
                }
                else
                {
                    whitespace = linePos;
                }
			}

			// option name must not be empty
			if (end == start) throw new ParseException(lineNumber);

			return SubstringToKey(start, end - start);
		}

		/// <summary>
		/// Parses an INI option value (<c>&quot;&lt;quoted_value&gt;</c> or <c>&lt;expression&gt;</c>).
		/// </summary>
		/// <param name="multiline">If <B>true</B>, next line of a multi-line value is expected; if <B>false</B>
		/// otherwise. Receives <B>true</B> if more lines should follow and <B>false</B> if this was the last one
		/// or the only one line.</param>
		/// <returns>The option value (always a string). If <paramref name="multiline"/> receives <B>true</B>,
		/// the return values is just a fragment and should be concatenated with subsequent return values to 
		/// obtain the entire option value.</returns>
		private string Value(ref bool multiline)
		{
			if (multiline) return QuotedValue(out multiline);

			// this is the first line (just after the =)
			ConsumeWhiteSpace();
			if (LookAhead == Tokens.Quote)
			{
				// quoted string starts here
				Consume();
				return QuotedValue(out multiline);
			}

			// no quotes - let's parse an expression
			string result = Core.Convert.ObjectToString(Expression());

			// must have reached end-of-line
			if (LookAhead != Tokens.EndOfLine && LookAhead != Tokens.Semicolon) throw new ParseException(lineNumber);

			return result;
		}

		/// <summary>
		/// Parses an INI option quoted value (<c>&lt;option_value&gt;</c> or
		/// <c>&lt;option_value&gt;&quot;&lt;whitespace&gt;</c>).
		/// </summary>
		/// <param name="moreLinesFollow">Receives <B>true</B> if this value consists of more lines that
		/// should follow (i.e. right quote not found yet), <B>false</B> otherwise.</param>
		/// <returns>The option value (always a string). If <paramref name="moreLinesFollow"/> receives
		/// <B>true</B>, the return values is just a fragment and should be concatenated with subsequent
		/// return values to obtain the entire option value.</returns>
		private string QuotedValue(out bool moreLinesFollow)
		{
			char ch;
			int start = linePos;

			// reading next line of a multiline quoted string
			while ((ch = Consume()) != Tokens.EndOfLine)
			{
				if (ch == Tokens.Quote)
				{
					// right quote
					moreLinesFollow = false;
					int end = linePos - 1;

					ConsumeWhiteSpace();
					if (LookAhead != Tokens.EndOfLine && LookAhead != Tokens.Semicolon)
					{
						throw new ParseException(lineNumber);
					}

					return line.Substring(start, end - start);
				}
			}

			// the string shall continue on the following line
			moreLinesFollow = true;
			return (start == 0 ? line : line.Substring(start));
		}

		/// <summary>
		/// Parses an INI option value expression (<c>&lt;literal&gt;(&amp;/|&lt;literal&gt;)*</c>).
		/// </summary>
		/// <returns>The expression value (either a string or an integer).</returns>
		private object Expression()
		{
			object result = Literal();

			while (LookAhead != Tokens.EndOfLine && LookAhead != Tokens.Semicolon && LookAhead != Tokens.ParClose)
			{
				// expecting either & or |
				char op = Consume();
				if (op != Tokens.And && op != Tokens.Or) throw new ParseException(lineNumber);

				// both operands must be converted to an integer
				int lhs = Core.Convert.ObjectToInteger(result);
				int rhs = Core.Convert.ObjectToInteger(Literal());

				// perform the operation eagerly (like a stupid calculator)
				result = (op == Tokens.And ? (int)(lhs & rhs) : (int)(lhs | rhs));
			}

			return result;
		}

		/// <summary>
		/// Parses an INI option value literal (<c>&lt;whitespace&gt;(&lt;expression&gt;)&lt;whitespace&gt;</c>,
		/// <c>&lt;whitespace&gt;!&lt;literal&gt;</c>, <c>&lt;whitespace&gt;~&lt;literal&gt;</c> or
		/// <c>&lt;whitespace&gt;&lt;literal_value&gt;&lt;whitespace&gt;</c>).
		/// </summary>
		/// <returns>The literal value (either a string or an integer).</returns>
		private object Literal()
		{
			ConsumeWhiteSpace();

			switch (LookAhead)
			{
				case Tokens.ParOpen:
					{
						// literal ::= ( expression )
						Consume();
						object result = Expression();
						Consume(Tokens.ParClose);
						ConsumeWhiteSpace();
						return result;
					}

				case Tokens.Not:
					{
						// literal ::= ! literal
						Consume();
						return (Core.Convert.ObjectToBoolean(Literal()) ? "0" : "1");
					}

				case Tokens.Neg:
					{
						// literal ::= ~ literal
						Consume();
						return (int)~Core.Convert.ObjectToInteger(Literal());
					}

				default:
					{
						// literal ::= alphanum
						int start = linePos, end = start;

						while (true)
						{
							char la = LookAhead;
							switch (la)
							{
								case Tokens.EqualS:
								case Tokens.Quote:
								case Tokens.Or:
								case Tokens.And:
								case Tokens.Not:
								case Tokens.Neg:
								case Tokens.ParOpen:
								case Tokens.ParClose:
								case Tokens.Semicolon:
								case Tokens.EndOfLine:
									{
										return SubstringToValue(start, end - start);
									}

								default:
									{
										Consume();

										// remember the last non-whitespace
										if (!Char.IsWhiteSpace(la)) end = linePos;
										break;
									}
							}
						}
					}
			}
		}
	}

		#endregion
}
