/*

 Copyright (c) 2005-2006 Tomas Matousek and Martin Maly.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

/*
not implemented yet:

# mb_ check_ encoding
# mb_ convert_ case
# mb_ convert_ encoding
# mb_ convert_ kana
# mb_ convert_ variables
# mb_ decode_ mimeheader
# mb_ decode_ numericentity
# mb_ detect_ encoding
# mb_ detect_ order
# mb_ encode_ mimeheader
# mb_ encode_ numericentity
# mb_ encoding_ aliases
# mb_ ereg_ match
# mb_ ereg_ replace
# mb_ ereg_ search_ getpos
# mb_ ereg_ search_ getregs
# mb_ ereg_ search_ init
# mb_ ereg_ search_ pos
# mb_ ereg_ search_ regs
# mb_ ereg_ search_ setpos
# mb_ ereg_ search
# mb_ ereg
# mb_ eregi_ replace
# mb_ eregi
# mb_ get_ info
# mb_ http_ input
# mb_ http_ output
# mb_ output_ handler
 
 */

using System;
using System.Data;
using System.Collections;
using System.Text;
using System.Data.SqlClient;

using PHP.Core;
using System.Collections.Generic;

namespace PHP.Library.Strings
{
	/// <summary>
    /// Implements PHP functions provided by multi-byte-string extension.
	/// </summary>
	public static class MultiByteString
	{
        #region Constants
        
        [Flags]
        public enum OverloadConstants
        {
            [ImplementsConstant("MB_OVERLOAD_MAIL")]
            MB_OVERLOAD_MAIL = 1,

            [ImplementsConstant("MB_OVERLOAD_STRING")]
            MB_OVERLOAD_STRING = 2,

            [ImplementsConstant("MB_OVERLOAD_REGEX")]
            MB_OVERLOAD_REGEX = 4,
        }

        [Flags]
        public enum CaseConstants
        {
            [ImplementsConstant("MB_CASE_UPPER")]
            MB_CASE_UPPER = 0,

            [ImplementsConstant("MB_CASE_LOWER")]
            MB_CASE_LOWER = 1,

            [ImplementsConstant("MB_CASE_TITLE")]
            MB_CASE_TITLE = 2,
        }

        #endregion

        #region Encodings

        private static Dictionary<string, Encoding> _encodings = null;
        public static Dictionary<string, Encoding>/*!*/Encodings
        {
            get
            {
                if (_encodings == null)
                {
                    Dictionary<string, Encoding> enc = new Dictionary<string, Encoding>(180, StringComparer.OrdinalIgnoreCase);

                    // encoding names used in PHP

                    //enc["pass"] = Encoding.Default; // TODO: "pass" encoding
                    enc["auto"] = Configuration.Application.Globalization.PageEncoding;
                    enc["wchar"] = Encoding.Unicode;
                    //byte2be
                    //byte2le
                    //byte4be
                    //byte4le
                    //BASE64
                    //UUENCODE
                    //HTML-ENTITIES
                    //Quoted-Printable
                    //7bit
                    //8bit
                    //UCS-4
                    //UCS-4BE
                    //UCS-4LE
                    //UCS-2
                    //UCS-2BE
                    //UCS-2LE
                    //UTF-32
                    //UTF-32BE
                    //UTF-32LE
                    //UTF-16
                    //UTF-16BE
                    enc["UTF-16LE"] = Encoding.Unicode; // alias UTF-16
                    //UTF-8
                    //UTF-7
                    //UTF7-IMAP
                    enc["ASCII"] = Encoding.ASCII;  // alias us-ascii
                    //EUC-JP
                    //SJIS
                    //eucJP-win
                    //SJIS-win
                    //CP51932
                    //JIS
                    //ISO-2022-JP
                    //ISO-2022-JP-MS
                    //Windows-1252
                    //Windows-1254
                    //ISO-8859-1
                    //ISO-8859-2
                    //ISO-8859-3
                    //ISO-8859-4
                    //ISO-8859-5
                    //ISO-8859-6
                    //ISO-8859-7
                    //ISO-8859-8
                    //ISO-8859-9
                    //ISO-8859-10
                    //ISO-8859-13
                    //ISO-8859-14
                    //ISO-8859-15
                    //ISO-8859-16
                    //EUC-CN
                    //CP936
                    //HZ
                    //EUC-TW
                    //BIG-5
                    //EUC-KR
                    //UHC
                    //ISO-2022-KR
                    //Windows-1251
                    //CP866
                    //KOI8-R
                    //KOI8-U
                    //ArmSCII-8
                    //CP850

                    // .NET encodings
                    foreach (var encoding in Encoding.GetEncodings())
                        enc[encoding.Name] = encoding.GetEncoding();

                    _encodings = enc;
                }

                return _encodings;
            }
        }

        /// <summary>
        /// Get encoding based on the PHP name. Can return null is such encoding is not defined.
        /// </summary>
        /// <param name="encodingName"></param>
        /// <returns></returns>
        public static Encoding GetEncoding(string encodingName)
        {
            Encoding encoding;
            if (!Encodings.TryGetValue(encodingName, out encoding))
                return null;

            return encoding;
        }

        #endregion

        #region Object conversion using specified encoding

        private delegate Encoding getEncoding();

        /// <summary>
        /// Converts PhpBytes using specified encoding. If any other object is provided, encoding is not performed.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="encodingGetter"></param>
        /// <returns></returns>
        private static string ObjectToString(object str, getEncoding encodingGetter)
        {
            if (str is PhpBytes)
            {
                PhpBytes bytes = (PhpBytes)str;
                Encoding encoding = encodingGetter();
                if (encoding == null)
                    return null;

                return encoding.GetString(bytes.ReadonlyData, 0, bytes.Length);
            }
            else
            {
                // .NET String should be always UTF-16, given encoding is irrelevant
                return PHP.Core.Convert.ObjectToString(str);
            }
        }

        #endregion

        #region mb_internal_encoding, mb_preferred_mime_name

        /// <summary>
        /// Multi Byte String Internal Encoding.
        /// </summary>
        public static Encoding/*!*/InternalEncoding
        {
            get
            {
                return _internalEncoding ?? Configuration.Application.Globalization.PageEncoding;
            }
            private set
            {
                _internalEncoding = value;
            }
        }
        /// <summary>
        /// Multi Byte String Internal Encoding IANA name.
        /// </summary>
        public static string InternalEncodingName
        {
            get
            {
                return InternalEncoding.WebName;
            }
        }
        [ThreadStatic]
        private static Encoding _internalEncoding = null;

        /// <summary>
        /// Get encoding used by default in the extension.
        /// </summary>
        /// <returns></returns>
        [ImplementsFunction("mb_internal_encoding")]
        public static string GetInternalEncoding()
        {
            return InternalEncoding.WebName;
        }

        /// <summary>
        /// Set the encoding used by the extension.
        /// </summary>
        /// <param name="encodingName"></param>
        /// <returns>True is encoding was set, otherwise false.</returns>
        [ImplementsFunction("mb_internal_encoding")]
        public static bool SetInternalEncoding(string encodingName)
        {
            Encoding enc = GetEncoding(encodingName);

            if (enc != null)
            {
                InternalEncoding = enc;

                return true;
            }
            else
            {
                return false;
            }

        }

        /// <summary>
        /// Get a MIME charset string for a specific encoding. 
        /// </summary>
        /// <param name="encoding_name">The encoding being checked. Its WebName or PHP/Phalanger name.</param>
        /// <returns>The MIME charset string for given character encoding.</returns>
        [ImplementsFunction("mb_preferred_mime_name")]
        public static string GetPreferredMimeName(string encoding_name)
        {
            Encoding encoding;

            if (
                (encoding = Encoding.GetEncoding(encoding_name)) == null && // .NET encodings (by their WebName)
                (encoding = GetEncoding(encoding_name)) == null //try PHP internal encodings too (by PHP/Phalanger name)
                )
            {
                PhpException.ArgumentValueNotSupported("encoding_name", encoding);
                return null;
            }

            return encoding.BodyName;   // it seems to return right MIME
        }

        #endregion

        #region mb_regex_encoding, mb_regex_set_options

        /// <summary>
        /// Multi Byte String Internal Encoding.
        /// </summary>
        public static Encoding/*!*/RegexEncoding
        {
            get
            {
                return _regexEncoding ?? Configuration.Application.Globalization.PageEncoding;
            }
            private set
            {
                _regexEncoding = value;
            }
        }
        /// <summary>
        /// Multi Byte String regex Encoding IANA name.
        /// </summary>
        public static string RegexEncodingName
        {
            get
            {
                return RegexEncoding.WebName;
            }
        }
        [ThreadStatic]
        private static Encoding _regexEncoding = null;

        /// <summary>
        /// Get encoding used by regex in the extension.
        /// </summary>
        /// <returns></returns>
        [ImplementsFunction("mb_regex_encoding")]
        public static string GetRegexEncoding()
        {
            return RegexEncoding.WebName;
        }

        /// <summary>
        /// Set the encoding used by the extension in regex functions.
        /// </summary>
        /// <param name="encodingName"></param>
        /// <returns>True is encoding was set, otherwise false.</returns>
        [ImplementsFunction("mb_regex_encoding")]
        public static bool SetRegexEncoding(string encodingName)
        {
            Encoding enc = GetEncoding(encodingName);

            if (enc != null)
            {
                RegexEncoding = enc;

                return true;
            }
            else
            {
                return false;
            }

        }

        #region regex options

        [Flags]
        private enum RegexOptions
        {
            None = 0,

            /// <summary>
            /// i
            /// </summary>
            AmbiguityMatch = 1,

            /// <summary>
            /// x
            /// </summary>
            ExtendedPatternForm = 2,

            /// <summary>
            /// m
            /// </summary>
            DotMatchesNewLine = 4,

            /// <summary>
            /// s
            /// </summary>
            ConvertMatchBeginEnd = 8,

            /// <summary>
            /// l
            /// </summary>
            FindLongestMatch = 16,

            /// <summary>
            /// n
            /// </summary>
            IgnoreEmptyMatch = 32,

            /// <summary>
            /// e
            /// </summary>
            EvalResultingCode = 64,
        }

        private enum RegexSyntaxModes
        {
            /// <summary>
            /// d
            /// </summary>
            POSIXExtendedRegex,
        }

        [ThreadStatic]
        private static RegexOptions _regexOptions = RegexOptions.DotMatchesNewLine | RegexOptions.ConvertMatchBeginEnd;

        [ThreadStatic]
        private static RegexSyntaxModes _regexSyntaxMode = RegexSyntaxModes.POSIXExtendedRegex;

        /// <summary>
        /// Determines if given combination of options is enabled.
        /// </summary>
        /// <param name="opt">Option or mask of options to test.</param>
        /// <returns>True if given option mask is enabled.</returns>
        private static bool OptionEnabled(RegexOptions opt)
        {
            return (_regexOptions & opt) != 0;
        }
        /// <summary>
        /// Determines if given syntax mode is set.
        /// </summary>
        /// <param name="opt">Syntax mode to test.</param>
        /// <returns>True if given syntax mode is enabled.</returns>
        private static bool OptionEnabled(RegexSyntaxModes opt)
        {
            return (_regexSyntaxMode == opt);
        }

        #endregion

        /// <summary>
        /// Get currently set regex options.
        /// </summary>
        /// <returns>Option string.</returns>
        [ImplementsFunction("mb_regex_set_options")]
        public static string GetRegexOptions()
        {
            string optionString = string.Empty;

            if (OptionEnabled(RegexOptions.AmbiguityMatch)) optionString += 'i';
            if (OptionEnabled(RegexOptions.ExtendedPatternForm)) optionString += 'x';
            if (OptionEnabled(RegexOptions.DotMatchesNewLine)) optionString += 'm';
            if (OptionEnabled(RegexOptions.ConvertMatchBeginEnd)) optionString += 's';
            if (OptionEnabled(RegexOptions.FindLongestMatch)) optionString += 'l';
            if (OptionEnabled(RegexOptions.IgnoreEmptyMatch)) optionString += 'n';
            if (OptionEnabled(RegexOptions.EvalResultingCode)) optionString += 'e';

            if (OptionEnabled(RegexSyntaxModes.POSIXExtendedRegex)) optionString += 'd';
            else
                throw new NotImplementedException("syntax mode not catch by mb_regex_set_options()!");

            return optionString;
        }

        /// <summary>
        /// Set new regex options.
        /// </summary>
        /// <param name="options">Option string.</param>
        /// <returns>New option string.</returns>
        /// <remarks>
        /// Regex options:
        /// Option  Meaning
        /// i 	    Ambiguity match on
        /// x 	    Enables extended pattern form
        /// m 	    '.' matches with newlines
        /// s 	    '^' -> '\A', '$' -> '\Z'
        /// p 	    Same as both the m and s options
        /// l 	    Finds longest matches
        /// n 	    Ignores empty matches
        /// e 	    eval() resulting code
        /// 
        /// Regex syntax modes:
        /// Mode 	Meaning
        /// j 	    (not supported) Java (Sun java.util.regex)
        /// u 	    (not supported) GNU regex
        /// g 	    (not supported) grep
        /// c 	    (not supported) Emacs
        /// r 	    (not supported) Ruby
        /// z 	    (not supported) Perl
        /// b 	    (not supported) POSIX Basic regex
        /// d 	    POSIX Extended regex
        /// </remarks>
        [ImplementsFunction("mb_regex_set_options")]
        public static string SetRegexOptions(string options)
        {
            RegexOptions newRegexOptions = RegexOptions.None;
            RegexSyntaxModes newRegexSyntaxModes = RegexSyntaxModes.POSIXExtendedRegex;

            foreach (char c in options)
            {
                switch (c)
                {
                    case 'i':
                        newRegexOptions |= RegexOptions.AmbiguityMatch;
                        break;
                    case 'x':
                        newRegexOptions |= RegexOptions.ExtendedPatternForm;
                        break;
                    case 'm':
                        newRegexOptions |= RegexOptions.DotMatchesNewLine;
                        break;
                    case 's':
                        newRegexOptions |= RegexOptions.ConvertMatchBeginEnd;
                        break;
                    case 'p':
                        newRegexOptions |= RegexOptions.DotMatchesNewLine | RegexOptions.ConvertMatchBeginEnd;
                        break;
                    case 'l':
                        newRegexOptions |= RegexOptions.FindLongestMatch;
                        break;
                    case 'n':
                        newRegexOptions |= RegexOptions.IgnoreEmptyMatch;
                        break;
                    case 'e':
                        newRegexOptions |= RegexOptions.EvalResultingCode;
                        break;

                    case 'd':
                        newRegexSyntaxModes = RegexSyntaxModes.POSIXExtendedRegex;
                        break;

                    default:
                        PhpException.ArgumentValueNotSupported("options", c);
                        break;
                }
            }

            //
            _regexOptions = newRegexOptions;
            _regexSyntaxMode = newRegexSyntaxModes;

            return GetRegexOptions();
        }

        #endregion

        #region mb_substr, mb_strcut

        #region mb_substr implementation

        [ImplementsFunction("mb_substr")]
        [return: CastToFalse]
        public static string SubString(object str, int start)
        {
            return SubString(str, start, -1, () => InternalEncoding);
        }

        [ImplementsFunction("mb_substr")]
        [return: CastToFalse]
        public static string SubString(object str, int start, int length)
        {
            return SubString(str, start, length, () => InternalEncoding);
        }

        [ImplementsFunction("mb_substr")]
        [return:CastToFalse]
        public static string SubString(object str, int start, int length, string encoding)
        {
            return SubString(str, start, length, () => GetEncoding(encoding));
        }

        #endregion

        #region mb_strcut (in PHP it behaves differently, but in .NET it is an alias for mb_substr)

        [ImplementsFunction("mb_strcut")]
        [return: CastToFalse]
        public static string CutString(object str, int start)
        {
            return SubString(str, start, -1, () => InternalEncoding);
        }

        [ImplementsFunction("mb_strcut")]
        [return: CastToFalse]
        public static string CutString(object str, int start, int length)
        {
            return SubString(str, start, length, () => InternalEncoding);
        }

        [ImplementsFunction("mb_strcut")]
        [return: CastToFalse]
        public static string CutString(object str, int start, int length, string encoding)
        {
            return SubString(str, start, length, () => GetEncoding(encoding));
        }

        #endregion

        private static string SubString(object str, int start, int length, getEncoding encodingGetter)
        {
            // get the Unicode representation of the string
            string ustr = ObjectToString(str, encodingGetter);

            if (ustr == null)
                return null;

            // start counting from the end of the string
            if (start < 0)
                start = ustr.Length + start;    // can result in negative start again -> invalid

            if (length == -1)
                length = ustr.Length;

            // check boundaries
            if (start >= ustr.Length || length < 0 || start < 0)
                return null;

            if (length == 0)
                return string.Empty;

            // return the substring
            return (start + length > ustr.Length) ? ustr.Substring(start) : ustr.Substring(start, length);
        }

        #endregion

        #region mb_substr_count

        [ImplementsFunction("mb_substr_count")]
        public static int SubStringCount(string haystack  , string needle )
        {
            return SubStringCount(haystack, needle, () => InternalEncoding);
        }

        [ImplementsFunction("mb_substr_count")]
        public static int SubStringCount(string haystack, string needle, string encoding)
        {
            return SubStringCount(haystack, needle, () => GetEncoding(encoding));
        }

        private static int SubStringCount(string haystack, string needle, getEncoding encodingGetter)
        {
            string uhaystack = ObjectToString(haystack, encodingGetter);
            string uneedle = ObjectToString(needle, encodingGetter);

            if (uhaystack == null || uneedle == null)
                return 0;

            return PhpStrings.SubstringCount(uhaystack, uneedle);
        }

        #endregion

        #region mb_substitute_character

        [ImplementsFunction("mb_substitute_character")]
        public static object GetSubstituteCharacter()
        {
            PhpException.FunctionNotSupported();
            return false;
        }

        [ImplementsFunction("mb_substitute_character")]
        public static object SetSubstituteCharacter(object substrchar)
        {
            PhpException.FunctionNotSupported();
            return "none";
        }

        #endregion

        #region mb_strwidth, mb_strimwidth

        /// <summary>
        /// Determines the char width.
        /// </summary>
        /// <param name="c">Character.</param>
        /// <returns>The width of the character.</returns>
        private static int CharWidth(char c)
        {
            //Chars  	            Width
            //U+0000 - U+0019 	0
            //U+0020 - U+1FFF 	1
            //U+2000 - U+FF60 	2
            //U+FF61 - U+FF9F 	1
            //U+FFA0 - 	        2

            if (c <= 0x0019) return 0;
            else if (c <= 0x1fff) return 1;
            else if (c <= 0xff60) return 2;
            else if (c <= 0xff9f) return 1;
            else return 2;
        }

        private static int StringWidth(string str)
        {
            if (str == null)
                return 0;

            int width = 0;

            foreach (char c in str)
                width += CharWidth(c);

            return width;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <param name="width">Characters remaining.</param>
        /// <returns></returns>
        private static string StringTrimByWidth(string/*!*/str, ref int width)
        {
            if (str == null)
                return null;

            int i = 0;

            foreach (char c in str)
            {
                int w = CharWidth(c);

                if (w < width)
                {
                    ++i;
                    width -= w;
                }
                else if (w == width)
                {
                    ++i;
                    width = 0;
                    break;
                }
                else
                    break;
            }

            return (i < str.Length) ? str.Remove(i) : str;
        }

        #region mb_strwidth implementation

        [ImplementsFunction("mb_strwidth")]
        public static int StringWidth(object str)
        {
            return StringWidth(str, () => InternalEncoding);
        }

        /// <summary>
        /// The string width. Not the string length.
        ///  Multi-byte characters are usually twice the width of single byte characters.
        /// </summary>
        /// <param name="str">The string being decoded. </param>
        /// <param name="encoding">The encoding parameter is the character encoding in case of PhpBytes is used. If it is omitted, the internal character encoding value will be used.</param>
        /// <returns>The width of string str.</returns>
        /// <remarks>
        /// Chars 	            Width
        /// U+0000 - U+0019 	0
        /// U+0020 - U+1FFF 	1
        /// U+2000 - U+FF60 	2
        /// U+FF61 - U+FF9F 	1
        /// U+FFA0 - 	        2
        /// </remarks>
        [ImplementsFunction("mb_strwidth")]
        public static int StringWidth(object str, string encoding)
        {
            return StringWidth(str, () =>GetEncoding(encoding));
        }

        private static int StringWidth(object str, getEncoding encodingGetter)
        {
            return StringWidth(ObjectToString(str, encodingGetter));
        }

        #endregion

        #region mb_strimwidth implementation

        [ImplementsFunction("mb_strimwidth")]
        public static string STrimWidth(object str, int start, int width)
        {
            return StringTrimByWidth(str, start, width, null, () => InternalEncoding);
        }
        [ImplementsFunction("mb_strimwidth")]
        public static string STrimWidth(object str, int start, int width, string trimmarker)
        {
            return StringTrimByWidth(str, start, width, trimmarker, () => InternalEncoding);
        }
        [ImplementsFunction("mb_strimwidth")]
        public static string STrimWidth(object str, int start, int width, string trimmarker, string encoding)
        {
            return StringTrimByWidth(str, start, width, trimmarker, () => GetEncoding(encoding));
        }

        private static string StringTrimByWidth(object str, int start, int width, string trimmarker, getEncoding encodingGetter)
        {
            string ustr = ObjectToString(str, encodingGetter);
            
            if (start >= ustr.Length)
                return string.Empty;

            ustr = ustr.Substring(start);
            int ustrWidth = StringWidth(ustr);

            if (ustrWidth <= width)
                return ustr;

            // trim the string
            int trimmarkerWidth = StringWidth(trimmarker);

            width -= trimmarkerWidth;
            string trimmedStr = StringTrimByWidth(ustr, ref width);
            width += trimmarkerWidth;
            string trimmedTrimMarker = StringTrimByWidth(trimmarker, ref width);

            //
            return trimmedStr + trimmedTrimMarker;
        }

        #endregion

        #endregion

        #region mb_strtoupper, mb_strtolower

        [ImplementsFunction("mb_strtoupper")]
        public static string StrToUpper(object str)
        {
            return StrToUpper(str, () => InternalEncoding);
        }
        [ImplementsFunction("mb_strtoupper")]
        public static string StrToUpper(object str, string encoding)
        {
            return StrToUpper(str, () => GetEncoding(encoding));
        }
        private static string StrToUpper(object str, getEncoding encodingGetter)
        {
            string ustr = ObjectToString(str, encodingGetter);
            return ustr.ToUpperInvariant();
        }

        [ImplementsFunction("mb_strtolower")]
        public static string StrToLower(object str)
        {
            return StrToLower(str, () => InternalEncoding);
        }
        [ImplementsFunction("mb_strtolower")]
        public static string StrToLower(object str, string encoding)
        {
            return StrToLower(str, () => GetEncoding(encoding));
        }
        private static string StrToLower(object str, getEncoding encodingGetter)
        {
            string ustr = ObjectToString(str, encodingGetter);
            return ustr.ToLowerInvariant();
        }

        #endregion

        #region mb_strstr, mb_stristr

        [ImplementsFunction("mb_strstr")]
        [return:CastToFalse]
        public static string StrStr(object haystack, object needle)
        {
            return StrStr(haystack, needle, false, () => InternalEncoding, false);
        }

        [ImplementsFunction("mb_strstr")]
        [return: CastToFalse]
        public static string StrStr(object haystack, object needle, bool part/*=FALSE*/)
        {
            return StrStr(haystack, needle, part, () => InternalEncoding, false);
        }

        [ImplementsFunction("mb_strstr")]
        [return: CastToFalse]
        public static string StrStr(object haystack, object needle, bool part/*=FALSE*/, string encoding)
        {
            return StrStr(haystack, needle, part, () => GetEncoding(encoding), false);
        }

        [ImplementsFunction("mb_stristr")]
        [return: CastToFalse]
        public static string StriStr(object haystack, object needle)
        {
            return StrStr(haystack, needle, false, () => InternalEncoding, true);
        }

        [ImplementsFunction("mb_stristr")]
        [return: CastToFalse]
        public static string StriStr(object haystack, object needle, bool part/*=FALSE*/)
        {
            return StrStr(haystack, needle, part, () => InternalEncoding, true);
        }

        [ImplementsFunction("mb_stristr")]
        [return: CastToFalse]
        public static string StriStr(object haystack, object needle, bool part/*=FALSE*/, string encoding)
        {
            return StrStr(haystack, needle, part, () => GetEncoding(encoding), true);
        }

        /// <summary>
        /// mb_strstr() finds the first occurrence of needle in haystack  and returns the portion of haystack. If needle is not found, it returns FALSE. 
        /// </summary>
        /// <param name="haystack">The string from which to get the first occurrence of needle</param>
        /// <param name="needle">The string to find in haystack</param>
        /// <param name="part">Determines which portion of haystack  this function returns. If set to TRUE, it returns all of haystack  from the beginning to the first occurrence of needle. If set to FALSE, it returns all of haystack  from the first occurrence of needle to the end.</param>
        /// <param name="encodingGetter">Character encoding name to use. If it is omitted, internal character encoding is used. </param>
        /// <param name="ignoreCase">Case insensitive.</param>
        /// <returns>Returns the portion of haystack, or FALSE (-1) if needle is not found.</returns>
        private static string StrStr(object haystack, object needle, bool part/* = false*/  , getEncoding encodingGetter, bool ignoreCase)
        {
            string uhaystack = ObjectToString(haystack, encodingGetter);
            string uneedle = ObjectToString(needle, encodingGetter);

            if (uhaystack == null || uneedle == null)   // never happen
                return null;

            if (uneedle == String.Empty)
            {
                PhpException.InvalidArgument("needle", LibResources.GetString("arg:empty"));
                return null;
            }

            int index = (ignoreCase) ? uhaystack.ToLower().IndexOf(uneedle.ToLower()) : uhaystack.IndexOf(uneedle);
            return (index == -1) ? null : (part ? uhaystack.Substring(0, index) : uhaystack.Substring(index));
        }

        #endregion

        #region mb_strpos, mb_stripos, mb_strrpos, mb_strripos

        #region mb_strpos stub

        [ImplementsFunction("mb_strpos")]
        [return: CastToFalse]
        public static int Strpos(object haystack, object needle)
        {
            return Strpos(haystack, needle, 0, () => InternalEncoding, false);
        }
        [ImplementsFunction("mb_strpos")]
        [return: CastToFalse]
        public static int Strpos(object haystack, object needle, int offset)
        {
            return Strpos(haystack, needle, offset, () => InternalEncoding, false);
        }
        [ImplementsFunction("mb_strpos")]
        [return:CastToFalse]
        public static int Strpos(object haystack, object needle, int offset, string encoding)
        {
            return Strpos(haystack, needle, offset, () => GetEncoding(encoding), false);
        }

        #endregion
        #region mb_stripos stub

        [ImplementsFunction("mb_stripos")]
        [return: CastToFalse]
        public static int Stripos(object haystack, object needle)
        {
            return Strpos(haystack, needle, 0, () => InternalEncoding, true);
        }
        [ImplementsFunction("mb_stripos")]
        [return: CastToFalse]
        public static int Stripos(object haystack, object needle, int offset)
        {
            return Strpos(haystack, needle, offset, () => InternalEncoding, true);
        }
        [ImplementsFunction("mb_stripos")]
        [return: CastToFalse]
        public static int Stripos(object haystack, object needle, int offset, string encoding)
        {
            return Strpos(haystack, needle, offset, () => GetEncoding(encoding), true);
        }

        #endregion
        #region mb_strrpos stub

        [ImplementsFunction("mb_strrpos")]
        [return: CastToFalse]
        public static int Strrpos(object haystack, object needle)
        {
            return Strrpos(haystack, needle, 0, () => InternalEncoding, false);
        }
        [ImplementsFunction("mb_strrpos")]
        [return: CastToFalse]
        public static int Strrpos(object haystack, object needle, int offset)
        {
            return Strrpos(haystack, needle, offset, () => InternalEncoding, false);
        }
        [ImplementsFunction("mb_strrpos")]
        [return: CastToFalse]
        public static int Strrpos(object haystack, object needle, int offset, string encoding)
        {
            return Strrpos(haystack, needle, offset, () => GetEncoding(encoding), false);
        }

        #endregion
        #region mb_strripos stub

        [ImplementsFunction("mb_strripos")]
        [return: CastToFalse]
        public static int Strripos(object haystack, object needle)
        {
            return Strrpos(haystack, needle, 0, () => InternalEncoding, true);
        }
        [ImplementsFunction("mb_strripos")]
        [return: CastToFalse]
        public static int Strripos(object haystack, object needle, int offset)
        {
            return Strrpos(haystack, needle, offset, () => InternalEncoding, true);
        }
        [ImplementsFunction("mb_strripos")]
        [return: CastToFalse]
        public static int Strripos(object haystack, object needle, int offset, string encoding)
        {
            return Strrpos(haystack, needle, offset, () => GetEncoding(encoding), true);
        }

        #endregion

        /// <summary>
        /// Implementation of <c>mb_str[i]pos</c> functions.
        /// </summary>
        private static int Strpos(object haystack, object needle, int offset, getEncoding encodingGetter, bool ignoreCase)
        {
            string uhaystack = ObjectToString(haystack, encodingGetter);
            string uneedle = ObjectToString(needle, encodingGetter);

            if (uhaystack == null || uneedle == null)
                return -1;

            if (offset < 0 || offset >= uhaystack.Length)
            {
                if (offset != uhaystack.Length)
                    PhpException.InvalidArgument("offset", LibResources.GetString("arg:out_of_bounds"));
                return -1;
            }

                if (uneedle == String.Empty)
                {
                    PhpException.InvalidArgument("needle", LibResources.GetString("arg:empty"));
                    return -1;
                }

            if (ignoreCase)
                return uhaystack.ToLower().IndexOf(uneedle.ToLower(), offset);
            else
                return uhaystack.IndexOf(uneedle, offset);
        }

        /// <summary>
        /// Implementation of <c>mb_strr[i]pos</c> functions.
        /// </summary>
        private static int Strrpos(object haystack, object needle, int offset, getEncoding encodingGetter, bool ignoreCase)
        {
            string uhaystack = ObjectToString(haystack, encodingGetter);
            string uneedle = ObjectToString(needle, encodingGetter);

            if (uhaystack == null || uneedle == null)
                return -1;

            int end = uhaystack.Length - 1;
            if (offset > end || offset < -end - 1)
            {
                PhpException.InvalidArgument("offset", LibResources.GetString("arg:out_of_bounds"));
                return -1;
            }

            if (offset < 0)
            {
                end += uneedle.Length + offset;
                offset = 0;
            }

                if (uneedle.Length == 0)
                {
                    PhpException.InvalidArgument("needle", LibResources.GetString("arg:empty"));
                    return -1;
                }

                if (ignoreCase)
                    return uhaystack.ToLower().LastIndexOf(uneedle.ToLower(), end, end - offset + 1);
                else
                    return uhaystack.LastIndexOf(uneedle, end, end - offset + 1);
        }

        #endregion

        #region mb_strlen

        [ImplementsFunction("mb_strlen")]
        public static int StrLen(object str)
        {
            return StrLen(str, () => InternalEncoding);
        }

        [ImplementsFunction("mb_strlen")]
        public static int StrLen(object str, string encoding)
        {
            return StrLen(str, ()=>GetEncoding(encoding));
        }

        /// <summary>
        /// Counts characters in a Unicode string or multi-byte string in PhpBytes.
        /// </summary>
        /// <param name="str">String or PhpBytes to use.</param>
        /// <param name="encodingGetter">Encoding used to encode PhpBytes</param>
        /// <returns>Number of unicode characters in given object.</returns>
        private static int StrLen(object str, getEncoding encodingGetter)
        {
            if (str == null)
                return 0;

            if (str.GetType() == typeof(string))
            {
                return ((string)str).Length;
            }
            else if (str.GetType() == typeof(PhpBytes))
            {
                Encoding encoding = encodingGetter();
                if (encoding == null)
                    throw new NotImplementedException();

                return encoding.GetCharCount(((PhpBytes)str).ReadonlyData);
            }
            else
            {
                return (ObjectToString(str, encodingGetter) ?? string.Empty).Length;
            }
        }

        #endregion

        #region mb_strrchr, mb_strrichr

        #region mb_strrchr stub
        [ImplementsFunction("mb_strrchr")]
        [return: CastToFalse]
        public static string StrrChr(object haystack, object needle)
        {
            return StrrChr(haystack, needle, false, () => InternalEncoding, false);
        }
        [ImplementsFunction("mb_strrchr")]
        [return: CastToFalse]
        public static string StrrChr(object haystack, object needle, bool part/*=false*/)
        {
            return StrrChr(haystack, needle, part, () => InternalEncoding, false);
        }
        [ImplementsFunction("mb_strrchr")]
        [return:CastToFalse]
        public static string StrrChr(object haystack, object needle, bool part/*=false*/, string encoding)
        {
            return StrrChr(haystack, needle, part, () => GetEncoding(encoding), false);
        }
        #endregion
        #region mb_strrichr stub
        [ImplementsFunction("mb_strrichr")]
        [return: CastToFalse]
        public static string StrriChr(object haystack, object needle)
        {
            return StrrChr(haystack, needle, false, () => InternalEncoding, true);
        }
        [ImplementsFunction("mb_strrichr")]
        [return: CastToFalse]
        public static string StrriChr(object haystack, object needle, bool part/*=false*/)
        {
            return StrrChr(haystack, needle, part, () => InternalEncoding, true);
        }
        [ImplementsFunction("mb_strrichr")]
        [return: CastToFalse]
        public static string StrriChr(object haystack, object needle, bool part/*=false*/, string encoding)
        {
            return StrrChr(haystack, needle, part, () => GetEncoding(encoding), true);
        }
        #endregion

        private static string StrrChr(object haystack, object needle, bool beforeNeedle/*=false*/, getEncoding encodingGetter, bool ignoreCase)
        {
            string uhaystack = ObjectToString(haystack, encodingGetter);
            char cneedle;
            {
                string uneedle;

                if (needle is string) uneedle = (string)needle;
                else if (needle is PhpString) uneedle = ((IPhpConvertible)needle).ToString();
                else if (needle is PhpBytes)
                {
                    Encoding encoding = encodingGetter();
                    if (encoding == null)
                        return null;

                    PhpBytes bytes = (PhpBytes)needle;
                    uneedle = encoding.GetString(bytes.ReadonlyData, 0, bytes.Length);
                }
                else
                {   // needle as a character number
                    Encoding encoding = encodingGetter();
                    if (encoding == null)
                        return null;

                    uneedle = encoding.GetString(new byte[] { unchecked((byte)Core.Convert.ObjectToInteger(needle)) }, 0, 1);
                }

                if (string.IsNullOrEmpty(uneedle))
                    return null;

                cneedle = uneedle[0];
            }

            int index = (ignoreCase) ? uhaystack.ToLower().LastIndexOf(char.ToLower(cneedle)) : uhaystack.LastIndexOf(cneedle);
            if (index < 0)
                return null;

            return (beforeNeedle) ? uhaystack.Remove(index) : uhaystack.Substring(index);

        }

        #endregion

        #region mb_split

        [ImplementsFunction("mb_split")]
        public static PhpArray Split(object pattern, object str)
        {
            return Split(pattern,str,-1);
        }

        [ImplementsFunction("mb_split")]
        public static PhpArray Split(object pattern, object str, int limit /*= -1*/)
        {
            return Library.PosixRegExp.DoSplit(
                ObjectToString(pattern, ()=>RegexEncoding),
                ObjectToString(str, ()=>RegexEncoding),
                limit,
                limit != -1,
                false);
        }

        #endregion

        #region mb_language, mb_send_mail

        /*/// <summary>
        /// Multi Byte String language used by mail functions.
        /// </summary>
        public static string MailLanguage
        {
            get
            {
                return _mailLanguage ?? "uni";
            }
            set
            {
                _mailLanguage = value;  // TODO: check the value
            }

        }
        [ThreadStatic]
        private static string _mailLanguage = null;*/

        /// <summary>
        /// Get language used by mail functions.
        /// </summary>
        /// <returns></returns>
        [ImplementsFunction("mb_language")]
        public static string GetMailLanguage()
        {
            return "uni";//MailLanguage;
        }

        /// <summary>
        /// Set the language used by mail functions.
        /// </summary>
        /// <param name="language"></param>
        /// <returns>True if language was set, otherwise false.</returns>
        [ImplementsFunction("mb_language")]
        public static bool SetMailLanguage(string language)
        {
            PhpException.FunctionNotSupported();
            return false;
            //try
            //{
            //    MailLanguage = language;
            //    return true;
            //}
            //catch
            //{
            //    return false;
            //}
        }

        #region mb_send_mail(), TODO: use mb_language

        [ImplementsFunction("mb_send_mail")]
        public static bool SendMail(string to, string subject, string message )
        {
            return PHP.Library.Mailer.Mail(to, subject, message);
        }
        [ImplementsFunction("mb_send_mail")]
        public static bool SendMail(string to, string subject, string message, string additional_headers /*= NULL*/ )
        {
            return PHP.Library.Mailer.Mail(to, subject, message, additional_headers);
        }
        [ImplementsFunction("mb_send_mail")]
        public static bool SendMail( string to, string subject, string message  , string additional_headers /*= NULL*/, string additional_parameter /*= NULL*/ )
        {
            return PHP.Library.Mailer.Mail(to, subject, message, additional_headers, additional_parameter);
        }

        #endregion

        #endregion

        #region mb_parse_str

        [ImplementsFunction("mb_parse_str")]
        public static bool ParseStr(string encoded_string, PhpReference array)
        {
            try
            {
                PhpArray result = new PhpArray();

                foreach (var x in ParseUrlEncodedGetParameters(encoded_string))
                    result.Add(x.Key, x.Value);
                
                array.Value = result;

                return true;
            }
            catch
            {
                return false;
            }
        }

        [ImplementsFunction("mb_parse_str")]
        public static bool ParseStr(string encoded_string)
        {
            try
            {
                PhpArray result = ScriptContext.CurrentContext.GlobalVariables;

                foreach (var x in ParseUrlEncodedGetParameters(encoded_string))
                    result.Add(x.Key, x.Value);
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Decodes URL encoded string and parses GET parameters.
        /// </summary>
        /// <param name="getParams">URL encoded GET parameters string.</param>
        /// <returns>Enumerator of decoded and parsed GET parameters as pairs of (name, value).</returns>
        private static IEnumerable<KeyValuePair<string,string>> ParseUrlEncodedGetParameters(string getParams)
        {
            foreach (var str in System.Web.HttpUtility.UrlDecode(getParams).Split(new char[]{'&'}))
            {
                if (str.Length == 0)
                    continue;

                int eqPos = str.IndexOf('=');

                if (eqPos > 0)
                {
                    // name = value
                    yield return new KeyValuePair<string, string>(str.Substring(0, eqPos), str.Substring(eqPos + 1));
                }
                else if (eqPos == 0)
                {
                    // ignore, no variable name
                }
                else
                {
                    // just variable name
                    yield return new KeyValuePair<string, string>(str, null);
                }
            }
        }

        #endregion
    }
}