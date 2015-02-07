using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PHP.Core;

namespace PHP.Library.Iconv
{

    public static class PhpNetIconv
    {
        #region Constants

        /// <summary>
        /// The implementation name
        /// </summary>
        [ImplementsConstant("ICONV_IMPL")]
        public const string Implementation = "Phalanger Iconv";

        /// <summary>
        /// The implementation version
        /// </summary>
        [ImplementsConstant("ICONV_VERSION")]
        public const string Version = "";    // TODO: current version, somehow automatically from AssemblyVersion

        public enum DecodeMode : int
        {
            None = 0,

            [ImplementsConstant("ICONV_MIME_DECODE_STRICT")]
            Strict = 1,

            [ImplementsConstant("ICONV_MIME_DECODE_CONTINUE_ON_ERROR")]
            ContinueOnError = 2,
        }

        #endregion

        #region Helper functions

        /// <summary>
        /// An optional string that can be appended to the output encoding name. Causes transliteration of characters that cannot be converted to the output encoding.
        /// </summary>
        private const string TranslitEncOption = "//TRANSLIT";

        /// <summary>
        /// An optional string that can be appended to the output encoding name (before <see cref="TranslitEncOption"/> if both are specified). Causes ignoring of characters that cannot be converted to the output encoding.
        /// </summary>
        private const string IgnoreEncOption = "//IGNORE";

        /// <summary>
        /// Remove optional encoding options such as <see cref="TranslitEncOption"/> or <see cref="IgnoreEncOption"/>.
        /// </summary>
        /// <param name="encoding">Original output encoding stirng.</param>
        /// <param name="transliterate">Is set to <c>true</c> if <see cref="TranslitEncOption"/> was specified.</param>
        /// <param name="discard_ilseq">Is set to <c>true</c> if <see cref="IgnoreEncOption"/> was specified.</param>
        /// <returns><paramref name="encoding"/> without optional options.</returns>
        private static string ParseOutputEncoding(string/*!*/encoding, out bool transliterate, out bool discard_ilseq)
        {
            Debug.Assert(encoding != null);

            if (encoding.EndsWith(TranslitEncOption, StringComparison.Ordinal))
            {
                encoding = encoding.Substring(0, encoding.Length - TranslitEncOption.Length);
                transliterate = true;
            }
            else
                transliterate = false;

            if (encoding.EndsWith(IgnoreEncOption, StringComparison.Ordinal))
            {
                encoding = encoding.Substring(0, encoding.Length - IgnoreEncOption.Length);
                discard_ilseq = true;
            }
            else
                discard_ilseq = false;

            //
            return encoding;
        }

        /// <summary>
        /// Try to find <see cref="Encoding"/> by its PHP name.
        /// </summary>
        /// <param name="encoding">Encoding name.</param>
        /// <returns><see cref="Encoding"/> instance or <c>null</c> if nothing was found.</returns>
        private static Encoding ResolveEncoding(string encoding)
        {
            return PHP.Library.Strings.MultiByteString.GetEncoding(encoding);
        }

        #endregion

        //iconv_get_encoding — Retrieve internal configuration variables of iconv extension
        [ImplementsFunction("iconv_get_encoding")]
        public static object iconv_get_encoding()
        {
            return GetIconvEncodingAll();
        }

        /// <summary>
        /// Retrieve internal configuration variables of iconv extension.
        /// </summary>
        /// <param name="type">
        /// The value of the optional type can be:
        /// - all
        /// - input_encoding
        /// - output_encoding
        /// - internal_encoding
        /// </param>
        /// <returns>Returns the current value of the internal configuration variable if successful or <c>false</c> on failure.
        /// If <paramref name="type"/> is omitted or set to <c>all</c>, iconv_get_encoding() returns an array that stores all these variables.</returns>
        [ImplementsFunction("iconv_get_encoding")]
        public static object iconv_get_encoding(string type /*= "all"*/)
        {
            if (type.EqualsOrdinalIgnoreCase("all"))
                return GetIconvEncodingAll();

            // 
            var local = IconvConfiguration.Local;
            
            if (type.EqualsOrdinalIgnoreCase("input_encoding"))
                return local.InputEncoding;

            if (type.EqualsOrdinalIgnoreCase("output_encoding"))
                return local.OutputEncoding;

            if (type.EqualsOrdinalIgnoreCase("internal_encoding"))
                return local.InternalEncoding;
            
            return false;
        }

        private static PhpArray/*!*/GetIconvEncodingAll()
        {
            var local = IconvConfiguration.Local;

            var ret = new PhpArray(3);
            ret.Add("input_encoding", local.InputEncoding);
            ret.Add("output_encoding", local.OutputEncoding);
            ret.Add("internal_encoding", local.InternalEncoding);
            return ret;
        }

        /// <summary>
        /// Set current setting for character encoding conversion.
        /// </summary>
        /// <param name="type">The value of type can be any one of these:
        /// - input_encoding
        /// - output_encoding
        /// - internal_encoding
        /// </param>
        /// <param name="charset">The character set.</param>
        /// <returns>Returns <c>TRUE</c> on success or <c>FALSE</c> on failure.</returns>
        [ImplementsFunction("iconv_set_encoding")]
        public static object iconv_set_encoding(string type, string charset)
        {
            var encoding = ResolveEncoding(charset);
            if (encoding == null)
            {
                PhpException.InvalidArgument("charset");    // TODO: PHP error message
                return false;
            }

            // 
            var local = IconvConfiguration.Local;

            if (type.EqualsOrdinalIgnoreCase("input_encoding"))
            {
                local.InputEncoding = charset;
            }
            else if (type.EqualsOrdinalIgnoreCase("output_encoding"))
            {
                local.OutputEncoding = charset;
            }
            else if (type.EqualsOrdinalIgnoreCase("internal_encoding"))
            {
                local.InternalEncoding = charset;
            }
            else
            {
                PhpException.InvalidArgument("type");
                return false;
            }

            return true;
        }

        //iconv_mime_decode_headers — Decodes multiple MIME header fields at once
        //iconv_mime_decode — Decodes a MIME header field
        //iconv_mime_encode — Composes a MIME header field
        
        /// <summary>
        /// Returns the character count of string.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <returns>Returns the character count of str, as an integer.</returns>
        [ImplementsFunction("iconv_strlen")]
        public static int iconv_strlen(object str)
        {
            return iconv_strlen(str, IconvConfiguration.Local.InternalEncoding);
        }

        /// <summary>
        /// Returns the character count of string.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="charset">If charset parameter is omitted, <paramref name="str"/> is assumed to be encoded in iconv.internal_encoding.</param>
        /// <returns>Returns the character count of str, as an integer.</returns>
        [ImplementsFunction("iconv_strlen")]
        public static int iconv_strlen(object str, string charset/*=iconv.internal_encoding*/)
        {
            if (str == null)
                return 0;

            if (str.GetType() == typeof(string))
                return ((string)str).Length;

            if (str.GetType() == typeof(PhpBytes))
            {
                var bytes = (PhpBytes)str;
                if (bytes.Length == 0)
                    return 0;

                var encoding = ResolveEncoding(charset);
                if (encoding == null) throw new NotSupportedException("charset not supported"); // TODO: PHP friendly warning

                return encoding.GetCharCount(bytes.ReadonlyData);
            }

            //
            var strstr = Core.Convert.ObjectToString(str);
            return (strstr != null) ? strstr.Length : 0;
        }

        [ImplementsFunction("iconv_strpos")]
        [return: CastToFalse]
        public static int iconv_strpos(object haystack, object needle)
        {
            return iconv_strpos(haystack, needle, 0);
        }

        [ImplementsFunction("iconv_strpos")]
        [return: CastToFalse]
        public static int iconv_strpos(object haystack, object needle, int offset /*= 0*/)
        {
            return iconv_strpos(haystack, needle, offset, IconvConfiguration.Local.InternalEncoding);
        }

        /// <summary>
        /// Finds position of first occurrence of a needle within a haystack.
        /// In contrast to strpos(), the return value of iconv_strpos() is the number of characters that appear before the needle, rather than the offset in bytes to the position where the needle has been found. The characters are counted on the basis of the specified character set charset.
        /// </summary>
        /// <param name="haystack">The entire string.</param>
        /// <param name="needle">The searched substring.</param>
        /// <param name="offset">The optional offset parameter specifies the position from which the search should be performed.</param>
        /// <param name="charset">If charset parameter is omitted, string are assumed to be encoded in iconv.internal_encoding.</param>
        /// <returns>Returns the numeric position of the first occurrence of needle in haystack. If needle is not found, iconv_strpos() will return FALSE.</returns>
        [ImplementsFunction("iconv_strpos")]
        [return: CastToFalse]
        public static int iconv_strpos(object haystack, object needle, int offset /*= 0*/, string charset /*= ini_get("iconv.internal_encoding")*/)
        {   
            if (haystack == null || needle == null)
                return -1;

            var encoding = ResolveEncoding(charset);
            string haystackstr = (haystack.GetType() == typeof(PhpBytes)) ? encoding.GetString(((PhpBytes)haystack).ReadonlyData) : Core.Convert.ObjectToString(haystack);
            string needlestr = (needle.GetType() == typeof(PhpBytes)) ? encoding.GetString(((PhpBytes)needle).ReadonlyData) : Core.Convert.ObjectToString(needle);

            return PHP.Library.PhpStrings.Strpos(haystackstr, needlestr, offset);
        }

        [ImplementsFunction("iconv_strrpos")]
        [return: CastToFalse]
        public static int iconv_strrpos(object haystack, object needle)
        {
            return iconv_strrpos(haystack, needle, IconvConfiguration.Local.InternalEncoding);
        }

        /// <summary>
        /// Finds the last occurrence of a needle within a haystack.
        /// In contrast to strrpos(), the return value of iconv_strrpos() is the number of characters that appear before the needle, rather than the offset in bytes to the position where the needle has been found. The characters are counted on the basis of the specified character set charset.
        /// </summary>
        /// <param name="haystack">The entire string.</param>
        /// <param name="needle">The searched substring.</param>
        /// <param name="charset">If charset parameter is omitted, string are assumed to be encoded in iconv.internal_encoding.</param>
        /// <returns>Returns the numeric position of the last occurrence of needle in haystack. If needle is not found, iconv_strpos() will return FALSE.</returns>
        [ImplementsFunction("iconv_strrpos")]
        [return: CastToFalse]
        public static int iconv_strrpos(object haystack, object needle, string charset /*= ini_get("iconv.internal_encoding")*/)
        {
            if (haystack == null || needle == null)
                return -1;

            var encoding = ResolveEncoding(charset);
            string haystackstr = (haystack.GetType() == typeof(PhpBytes)) ? encoding.GetString(((PhpBytes)haystack).ReadonlyData) : Core.Convert.ObjectToString(haystack);
            string needlestr = (needle.GetType() == typeof(PhpBytes)) ? encoding.GetString(((PhpBytes)needle).ReadonlyData) : Core.Convert.ObjectToString(needle);

            return PHP.Library.PhpStrings.Strrpos(haystackstr, needlestr);
        }

        [ImplementsFunction("iconv_substr")]
        public static object iconv_substr(object/*string*/str, int offset)
        {
            return iconv_substr(str, offset, int.MaxValue);
        }

        [ImplementsFunction("iconv_substr")]
        public static object iconv_substr(object/*string*/str, int offset, int length /*= iconv_strlen($str, $charset)*/)
        {
            return iconv_substr(str, offset, length, IconvConfiguration.Local.InternalEncoding);
        }
        
        /// <summary>
        /// Cuts a portion of <paramref name="str"/> specified by the <paramref name="offset"/> and <paramref name="length"/> parameters.
        /// </summary>
        /// <param name="str">The original string.</param>
        /// <param name="offset">If offset is non-negative, iconv_substr() cuts the portion out of str beginning at offset'th character, counting from zero.
        /// If offset is negative, iconv_substr() cuts out the portion beginning at the position, offset characters away from the end of str.</param>
        /// <param name="length">If length is given and is positive, the return value will contain at most length characters of the portion that begins at offset (depending on the length of string).
        /// If negative length is passed, iconv_substr() cuts the portion out of str from the offset'th character up to the character that is length characters away from the end of the string. In case offset is also negative, the start position is calculated beforehand according to the rule explained above.</param>
        /// <param name="charset">If charset parameter is omitted, string are assumed to be encoded in iconv.internal_encoding.
        /// Note that offset and length parameters are always deemed to represent offsets that are calculated on the basis of the character set determined by charset, whilst the counterpart substr() always takes these for byte offsets.</param>
        /// <returns>Returns the portion of str specified by the offset and length parameters.
        /// If str is shorter than offset characters long, FALSE will be returned.</returns>
        [ImplementsFunction("iconv_substr")]
        public static object iconv_substr(object/*string*/str, int offset, int length /*= iconv_strlen($str, $charset)*/ , string charset /*= ini_get("iconv.internal_encoding")*/)
        {
            if (str == null)
                return false;

            if (str.GetType() == typeof(PhpBytes))
            {
                var bytes = (PhpBytes)str;
                if (bytes.Length == 0)
                    return string.Empty;

                var encoding = ResolveEncoding(charset);
                if (encoding == null) throw new NotSupportedException("charset not supported"); // TODO: PHP friendly warning

                return PHP.Library.PhpStrings.Substring(encoding.GetString(bytes.ReadonlyData), offset, length);
            }

            return PHP.Library.PhpStrings.Substring(Core.Convert.ObjectToString(str), offset, length);
        }

        /// <summary>
        /// Performs a character set conversion on the string str from in_charset to out_charset.
        /// </summary>
        /// <param name="in_charset">The input charset.</param>
        /// <param name="out_charset">The output charset.
        /// 
        /// If you append the string //TRANSLIT to out_charset transliteration is activated.
        /// This means that when a character can't be represented in the target charset,
        /// it can be approximated through one or several similarly looking characters.
        /// 
        /// If you append the string //IGNORE, characters that cannot be represented in the target
        /// charset are silently discarded. Otherwise, <paramref name="str"/> is cut from the first
        /// illegal character and an E_NOTICE is generated.</param>
        /// <param name="str"></param>
        /// <returns></returns>
        [ImplementsFunction("iconv")]
        [return: CastToFalse]
        public static PhpBytes iconv(string in_charset, string out_charset, object str)
        {
            // check args
            if (str == null)
            {
                PhpException.ArgumentNull("str");
                return null;
            }
            if (out_charset == null)
            {
                PhpException.ArgumentNull("out_charset");
                return null;
            }

            // resolve out_charset
            bool transliterate, discard_ilseq;
            out_charset = ParseOutputEncoding(out_charset, out transliterate, out discard_ilseq);
            var out_encoding = ResolveEncoding(out_charset);
            if (out_encoding == null)
            {
                PhpException.Throw(PhpError.Notice, string.Format(Strings.wrong_charset, out_charset, in_charset, out_charset));
                return null;
            }

            // out_encoding.Clone() ensures it is NOT readOnly
            // then set EncoderFallback to catch handle unconvertable characters

            out_encoding = (Encoding)out_encoding.Clone();

            var out_result = new EncoderResult();

            if (transliterate)
                out_encoding.EncoderFallback = new TranslitEncoderFallback();   // transliterate unknown characters
            else if (discard_ilseq)
                out_encoding.EncoderFallback = new IgnoreEncoderFallback();    // ignore character and continue
            else
                out_encoding.EncoderFallback = new StopEncoderFallback(out_result);    // throw notice and discard all remaining characters

            try
            {
                //
                if (str.GetType() == typeof(PhpBytes))
                {
                    // resolve in_charset
                    if (in_charset == null)
                    {
                        PhpException.ArgumentNull("in_charset");
                        return null;
                    }
                    var in_encoding = ResolveEncoding(in_charset);
                    if (in_encoding == null)
                    {
                        PhpException.Throw(PhpError.Notice, string.Format(Strings.wrong_charset, in_charset, in_charset, out_charset));
                        return null;
                    }

                    // TODO: in_encoding.Clone() ensures it is NOT readOnly, then set DecoderFallback to catch invalid byte sequences

                    // convert <in_charset> to <out_charset>
                    return new PhpBytes(out_encoding.GetBytes(in_encoding.GetString(((PhpBytes)str).ReadonlyData)));
                }

                if (str.GetType() == typeof(string) || (str = Core.Convert.ObjectToString(str)) != null)
                {
                    // convert UTF16 to <out_charset>
                    return new PhpBytes(out_encoding.GetBytes((string)str));
                }
            }
            finally
            {
                if (out_result.firstFallbackCharIndex >= 0)
                {
                    // Notice: iconv(): Detected an illegal character in input string
                    PHP.Core.PhpException.Throw(Core.PhpError.Notice, Strings.illegal_character);
                }
            }
            
            return null;
        }

        //ob_iconv_handler — Convert character encoding as output buffer handler
    }
}
