/*

 Copyright (c) 2004-2006 Tomas Matousek and Ladislav Prosek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

/*
  GENERICS:
    Generic char map and hashtable will allow to handle all Unicode characters and get rid of the errors:
    <exception cref="PhpException"><paramref name="whiteSpaceCharacters"/> contains Unicode characters greater than '\u0800'.</exception>
    
  TODO:
		- PHP6 - new functions hash($alg,...) hash_file($alg, ...)
		- Added overflow checks to wordwrap() function. (5.1.3) 
    - Fixed offset/length parameter validation in substr_compare() function. (5.1.3)
		- (strncmp & strncasecmp do not return false on negative string length). (5.1.3) 

*/
using System;
using PHP.Core;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.ComponentModel;

#if SILVERLIGHT
using PHP.CoreCLR;
using System.Windows.Browser;
#else
using System.Web;
using System.Diagnostics;
#endif

namespace PHP.Library
{
    #region Enumerations

    /// <summary>Quote conversion options.</summary>
    [Flags]
    public enum QuoteStyle
    {
        /// <summary>
        /// Default quote style for <c>htmlentities</c>.
        /// </summary>
        HtmlEntitiesDefault = QuoteStyle.Compatible | QuoteStyle.Html401,

        /// <summary>Single quotes.</summary>
        SingleQuotes = 1,

        /// <summary>Double quotes.</summary>
        DoubleQuotes = 2,

        /// <summary>
        /// No quotes.
        /// Will leave both double and single quotes unconverted.
        /// </summary>
        [ImplementsConstant("ENT_NOQUOTES")]
        NoQuotes = 0,

        /// <summary>
        /// Will convert double-quotes and leave single-quotes alone.
        /// </summary>
        [ImplementsConstant("ENT_COMPAT")]
        Compatible = DoubleQuotes,

        /// <summary>
        /// Both single and double quotes.
        /// Will convert both double and single quotes.
        /// </summary>
        [ImplementsConstant("ENT_QUOTES")]
        BothQuotes = DoubleQuotes | SingleQuotes,

        /// <summary>
        /// Silently discard invalid code unit sequences instead of
        /// returning an empty string. Using this flag is discouraged
        /// as it may have security implications.
        /// </summary>
        [ImplementsConstant("ENT_IGNORE")]
        Ignore = 4,

        /// <summary>
        /// Replace invalid code unit sequences with a Unicode
        /// Replacement Character U+FFFD (UTF-8) or &amp;#FFFD;
        /// (otherwise) instead of returning an empty string.
        /// </summary>
        [ImplementsConstant("ENT_SUBSTITUTE")]  //	8
        Substitute = 8,

        /// <summary>
        /// Handle code as HTML 4.01.
        /// </summary>
        [ImplementsConstant("ENT_HTML401")]     //	0
        Html401 = NoQuotes,

        /// <summary>
        /// Handle code as XML 1.
        /// </summary>
        [ImplementsConstant("ENT_XML1")]        //	16
        XML1 = 16,

        /// <summary>
        /// Handle code as XHTML.
        /// </summary>
        [ImplementsConstant("ENT_XHTML")]       //	32
        XHTML = 32,

        /// <summary>
        /// Handle code as HTML 5.
        /// </summary>
        [ImplementsConstant("ENT_HTML5")]       //	(16|32)
        HTML5 = XML1 | XHTML,

        /// <summary>
        /// Replace invalid code points for the given document type
        /// with a Unicode Replacement Character U+FFFD (UTF-8) or &amp;#FFFD;
        /// (otherwise) instead of leaving them as is.
        /// This may be useful, for instance, to ensure the well-formedness
        /// of XML documents with embedded external content.
        /// </summary>
        [ImplementsConstant("ENT_DISALLOWED")]  //	128
        Disallowed = 128,
    };

    /// <summary>Types of HTML entities tables.</summary>
    public enum HtmlEntitiesTable
    {
        /// <summary>Table containing special characters only.</summary>
        [ImplementsConstant("HTML_SPECIALCHARS")]
        SpecialChars = 0,

        /// <summary>Table containing all entities.</summary>
        [ImplementsConstant("HTML_ENTITIES")]
        AllEntities = 1
    };

    /// <summary>
    /// Type of padding.
    /// </summary>
    public enum PaddingType
    {
        /// <summary>Pad a string from the left.</summary>
        [ImplementsConstant("STR_PAD_LEFT")]
        Left = 0,

        /// <summary>Pad a string from the right.</summary>
        [ImplementsConstant("STR_PAD_RIGHT")]
        Right = 1,

        /// <summary>Pad a string from both sides.</summary>
        [ImplementsConstant("STR_PAD_BOTH")]
        Both = 2
    }

    /// <summary>
    /// Format of a return value of <see cref="PhpStrings.CountWords"/> method. Constants are not named in PHP.
    /// </summary>                   
    public enum WordCountResult
    {
        /// <summary>
        /// Return number of words in string.
        /// </summary>
        WordCount = 0,

        /// <summary>
        /// Return array of words.
        /// </summary>
        WordsArray = 1,

        /// <summary>
        /// Return positions to words mapping.
        /// </summary>
        PositionsToWordsMapping = 2
    }

    #endregion

    /// <summary>
    /// Manipulates strings.
    /// </summary>
    /// <threadsafety static="true"/>
    public static class PhpStrings
    {
        #region Character map

#if !SILVERLIGHT
        [ThreadStatic]
#endif
        private static CharMap _charmap;

        /// <summary>
        /// Get clear <see cref="CharMap"/> to be used by current thread. <see cref="_charmap"/>.
        /// </summary>
        internal static CharMap InitializeCharMap()
        {
            CharMap result = _charmap;

            if (result == null)
                _charmap = result = new CharMap(0x0800);
            else
                result.ClearAll();

            return result;
        }

        #endregion


        #region Binary Data Functions

        #region ord, chr, bin2hex, ord_unicode, chr_unicode, bin2hex_unicode, to_binary

        /// <summary>
        /// Returns ASCII code of the first character of a string of bytes.
        /// </summary>
        /// <param name="bytes">The string of bytes which the first byte will be returned.</param>
        /// <returns>The ASCII code of <paramref name="bytes"/>[0] or zero if null or empty.</returns>
        [ImplementsFunction("ord")]
        [PureFunction]
        public static int Ord(PhpBytes bytes)
        {
            return (bytes == null || bytes.Length == 0) ? 0 : (int)bytes[0];
        }

        /// <summary>
        /// Returns Unicode ordinal number of the first character of a string.
        /// </summary>
        /// <param name="str">The string which the first character's ordinal number is returned.</param>
        /// <returns>The ordinal number of <paramref name="str"/>[0].</returns>
        [ImplementsFunction("ord_unicode")]
        [PureFunction]
        public static int OrdUnicode(string str)
        {
            return (str == null || str == String.Empty) ? 0 : (int)str[0];
        }

        /// <summary>
        /// Converts ordinal number of character to a binary string containing that character.
        /// </summary>
        /// <param name="charCode">The ASCII code.</param>
        /// <returns>The character with <paramref name="charCode"/> ASCIT code.</returns>
        /// <remarks>Current code-page is determined by the <see cref="ApplicationConfiguration.GlobalizationSection.PageEncoding"/> property.</remarks>
        [ImplementsFunction("chr")]
        [PureFunction]
        public static PhpBytes Chr(int charCode)
        {
            return new PhpBytes(unchecked((byte)charCode));
        }

        /// <summary>
        /// Converts ordinal number of Unicode character to a string containing that character.
        /// </summary>
        /// <param name="charCode">The ordinal number of character.</param>
        /// <returns>The character with <paramref name="charCode"/> ordnial number.</returns>
        [ImplementsFunction("chr_unicode")]
        [PureFunction]
        public static string ChrUnicode(int charCode)
        {
            return unchecked((char)charCode).ToString();
        }

        /// <summary>
        /// Converts a string of bytes into hexadecimal representation.
        /// </summary>
        /// <param name="bytes">The string of bytes.</param>
        /// <returns>Concatenation of hexadecimal values of bytes of <paramref name="bytes"/>.</returns>
        /// <example>
        /// The string "01A" is converted into string "303140" because ord('0') = 0x30, ord('1') = 0x31, ord('A') = 0x40.
        /// </example>
        [ImplementsFunction("bin2hex")]
        [PureFunction]
        public static string BinToHex(PhpBytes bytes)
        {
            return (bytes == null) ? String.Empty : StringUtils.BinToHex(bytes.ReadonlyData, null);
        }

        /// <summary>
        /// Converts a string into hexadecimal representation.
        /// </summary>
        /// <param name="str">The string to be converted.</param>
        /// <returns>
        /// The concatenated four-characters long hexadecimal numbers each representing one character of <paramref name="str"/>.
        /// </returns>
        [ImplementsFunction("bin2hex_unicode")]
        [PureFunction]
        public static string BinToHex(string str)
        {
            if (str == null) return null;

            int length = str.Length;
            StringBuilder result = new StringBuilder(length * 4, length * 4);
            result.Length = length * 4;

            const string hex_digs = "0123456789abcdef";

            for (int i = 0; i < length; i++)
            {
                int c = (int)str[i];
                result[4 * i + 0] = hex_digs[(c & 0xf000) >> 12];
                result[4 * i + 1] = hex_digs[(c & 0x0f00) >> 8];
                result[4 * i + 2] = hex_digs[(c & 0x00f0) >> 4];
                result[4 * i + 3] = hex_digs[(c & 0x000f)];
            }

            return result.ToString();
        }

        /// <summary>
        /// Converts a variable to a string of binary data.
        /// </summary>
        /// <param name="var">A variable.</param>
        /// <returns>Binary data.</returns>
        [ImplementsFunction("to_binary")]
        [PureFunction]
        public static PhpBytes ToBinary(PhpBytes var)
        {
            return var;
        }

        #endregion


        #region convert_cyr_string

        #region cyrWin1251 (1251), cyrCp866 (20866), cyrIso88595 (28595), cyrMac (10007) conversion tables

        /// <summary>
        /// Cyrillic translation table for Windows CP1251 character set.
        /// </summary>
        private static readonly byte[] cyrWin1251 = new byte[]
	{
		0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,
		16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,
		32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,
		48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,
		64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,
		80,81,82,83,84,85,86,87,88,89,90,91,92,93,94,95,
		96,97,98,99,100,101,102,103,104,105,106,107,108,109,110,111,
		112,113,114,115,116,117,118,119,120,121,122,123,124,125,126,127,
      
		46,46,46,46,46,46,46,46,46,46,46,46,46,46,46,46,
		46,46,46,46,46,46,46,46,46,46,46,46,46,46,46,46,
		154,174,190,46,159,189,46,46,179,191,180,157,46,46,156,183,
		46,46,182,166,173,46,46,158,163,152,164,155,46,46,46,167,
		225,226,247,231,228,229,246,250,233,234,235,236,237,238,239,240,
		242,243,244,245,230,232,227,254,251,253,255,249,248,252,224,241,
		193,194,215,199,196,197,214,218,201,202,203,204,205,206,207,208,
		210,211,212,213,198,200,195,222,219,221,223,217,216,220,192,209,
      
		0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,
		16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,
		32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,
		48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,
		64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,
		80,81,82,83,84,85,86,87,88,89,90,91,92,93,94,95,
		96,97,98,99,100,101,102,103,104,105,106,107,108,109,110,111,
		112,113,114,115,116,117,118,119,120,121,122,123,124,125,126,127,
      
		32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
		32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
		32,32,32,184,186,32,179,191,32,32,32,32,32,180,162,32,
		32,32,32,168,170,32,178,175,32,32,32,32,32,165,161,169,
		254,224,225,246,228,229,244,227,245,232,233,234,235,236,237,238,
		239,255,240,241,242,243,230,226,252,251,231,248,253,249,247,250,
		222,192,193,214,196,197,212,195,213,200,201,202,203,204,205,206,
		207,223,208,209,210,211,198,194,220,219,199,216,221,217,215,218,
		};

        /// <summary>
        /// Cyrillic translation table for CP866 character set.
        /// </summary>
        private static readonly byte[] cyrCp866 = new byte[]
	{ 
		0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,
		16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,
		32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,
		48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,
		64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,
		80,81,82,83,84,85,86,87,88,89,90,91,92,93,94,95,
		96,97,98,99,100,101,102,103,104,105,106,107,108,109,110,111,
		112,113,114,115,116,117,118,119,120,121,122,123,124,125,126,127,
      
		225,226,247,231,228,229,246,250,233,234,235,236,237,238,239,240,
		242,243,244,245,230,232,227,254,251,253,255,249,248,252,224,241,
		193,194,215,199,196,197,214,218,201,202,203,204,205,206,207,208,
		35,35,35,124,124,124,124,43,43,124,124,43,43,43,43,43,
		43,45,45,124,45,43,124,124,43,43,45,45,124,45,43,45,
		45,45,45,43,43,43,43,43,43,43,43,35,35,124,124,35,
		210,211,212,213,198,200,195,222,219,221,223,217,216,220,192,209,
		179,163,180,164,183,167,190,174,32,149,158,32,152,159,148,154,
      
		0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,
		16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,
		32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,
		48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,
		64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,
		80,81,82,83,84,85,86,87,88,89,90,91,92,93,94,95,
		96,97,98,99,100,101,102,103,104,105,106,107,108,109,110,111,
		112,113,114,115,116,117,118,119,120,121,122,123,124,125,126,127,
      
		32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
		32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
		205,186,213,241,243,201,32,245,187,212,211,200,190,32,247,198,
		199,204,181,240,242,185,32,244,203,207,208,202,216,32,246,32,
		238,160,161,230,164,165,228,163,229,168,169,170,171,172,173,174,
		175,239,224,225,226,227,166,162,236,235,167,232,237,233,231,234,
		158,128,129,150,132,133,148,131,149,136,137,138,139,140,141,142,
		143,159,144,145,146,147,134,130,156,155,135,152,157,153,151,154,
		};

        /// <summary>
        /// Cyrillic translation table for ISO88595 character set.
        /// </summary>
        private static readonly byte[] cyrIso88595 = new byte[]
	{
		0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,
		16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,
		32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,
		48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,
		64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,
		80,81,82,83,84,85,86,87,88,89,90,91,92,93,94,95,
		96,97,98,99,100,101,102,103,104,105,106,107,108,109,110,111,
		112,113,114,115,116,117,118,119,120,121,122,123,124,125,126,127,
      
		32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
		32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
		32,179,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
		225,226,247,231,228,229,246,250,233,234,235,236,237,238,239,240,
		242,243,244,245,230,232,227,254,251,253,255,249,248,252,224,241,
		193,194,215,199,196,197,214,218,201,202,203,204,205,206,207,208,
		210,211,212,213,198,200,195,222,219,221,223,217,216,220,192,209,
		32,163,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
      
		0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,
		16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,
		32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,
		48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,
		64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,
		80,81,82,83,84,85,86,87,88,89,90,91,92,93,94,95,
		96,97,98,99,100,101,102,103,104,105,106,107,108,109,110,111,
		112,113,114,115,116,117,118,119,120,121,122,123,124,125,126,127,
      
		32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
		32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,
		32,32,32,241,32,32,32,32,32,32,32,32,32,32,32,32,
		32,32,32,161,32,32,32,32,32,32,32,32,32,32,32,32,
		238,208,209,230,212,213,228,211,229,216,217,218,219,220,221,222,
		223,239,224,225,226,227,214,210,236,235,215,232,237,233,231,234,
		206,176,177,198,180,181,196,179,197,184,185,186,187,188,189,190,
		191,207,192,193,194,195,182,178,204,203,183,200,205,201,199,202,
		};

        /// <summary>
        /// Cyrillic translation table for Mac character set.
        /// </summary>
        private static readonly byte[] cyrMac = new byte[]
	{
		0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,
		16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,
		32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,
		48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,
		64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,
		80,81,82,83,84,85,86,87,88,89,90,91,92,93,94,95,
		96,97,98,99,100,101,102,103,104,105,106,107,108,109,110,111,
		112,113,114,115,116,117,118,119,120,121,122,123,124,125,126,127,
      
		225,226,247,231,228,229,246,250,233,234,235,236,237,238,239,240,
		242,243,244,245,230,232,227,254,251,253,255,249,248,252,224,241,
		160,161,162,163,164,165,166,167,168,169,170,171,172,173,174,175,
		176,177,178,179,180,181,182,183,184,185,186,187,188,189,190,191,
		128,129,130,131,132,133,134,135,136,137,138,139,140,141,142,143,
		144,145,146,147,148,149,150,151,152,153,154,155,156,179,163,209,
		193,194,215,199,196,197,214,218,201,202,203,204,205,206,207,208,
		210,211,212,213,198,200,195,222,219,221,223,217,216,220,192,255,
      
		0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,
		16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,
		32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,
		48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,
		64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,
		80,81,82,83,84,85,86,87,88,89,90,91,92,93,94,95,
		96,97,98,99,100,101,102,103,104,105,106,107,108,109,110,111,
		112,113,114,115,116,117,118,119,120,121,122,123,124,125,126,127,
      
		192,193,194,195,196,197,198,199,200,201,202,203,204,205,206,207,
		208,209,210,211,212,213,214,215,216,217,218,219,220,221,222,223,
		160,161,162,222,164,165,166,167,168,169,170,171,172,173,174,175,
		176,177,178,221,180,181,182,183,184,185,186,187,188,189,190,191,
		254,224,225,246,228,229,244,227,245,232,233,234,235,236,237,238,
		239,223,240,241,242,243,230,226,252,251,231,248,253,249,247,250,
		158,128,129,150,132,133,148,131,149,136,137,138,139,140,141,142,
		143,159,144,145,146,147,134,130,156,155,135,152,157,153,151,154,
		};

        #endregion

        /// <summary>
        /// Returns a Cyrillic translation table for a specified character set,
        /// </summary>
        /// <param name="code">The character set code. Can be one of 'k', 'w', 'i', 'a', 'd', 'm'.</param>
        /// <returns>The translation table or null if no table is associated with given charset code.</returns>
        internal static byte[] GetCyrTableInternal(char code)
        {
            switch (Char.ToUpper(code))
            {
                case 'W':
                    return cyrWin1251;

                case 'A':
                case 'D':
                    return cyrCp866;

                case 'I':
                    return cyrIso88595;

                case 'M':
                    return cyrMac;

                case 'K':
                    return null;

                default:
                    return ArrayUtils.EmptyBytes;
            }
        }

        /// <include file='Doc/Strings.xml' path='docs/method[@name="ConvertCyrillic"]/*'/>
        /// <exception cref="PhpException">Thrown if source or destination charset is invalid. </exception>
        [ImplementsFunction("convert_cyr_string")]
        public static PhpBytes ConvertCyrillic(PhpBytes bytes, string srcCharset, string dstCharset)
        {
            if (bytes == null) return null;
            if (bytes.Length == 0) return PhpBytes.Empty;

            // checks srcCharset argument:
            if (srcCharset == null || srcCharset == String.Empty)
            {
                PhpException.InvalidArgument("srcCharset", LibResources.GetString("arg:null_or_empty"));
                return PhpBytes.Empty;
            }

            // checks dstCharset argument:
            if (dstCharset == null || dstCharset == String.Empty)
            {
                PhpException.InvalidArgument("dstCharset", LibResources.GetString("arg:null_or_empty"));
                return PhpBytes.Empty;
            }

            // get and check source charset table:
            byte[] fromTable = GetCyrTableInternal(srcCharset[0]);
            if (fromTable != null && fromTable.Length < 256)
            {
                PhpException.Throw(PhpError.Warning, LibResources.GetString("invalid_src_charser"));
                return PhpBytes.Empty;
            }

            // get and check destination charset table:
            byte[] toTable = GetCyrTableInternal(dstCharset[0]);
            if (toTable != null && toTable.Length < 256)
            {
                PhpException.Throw(PhpError.Warning, LibResources.GetString("invalid_dst_charser"));
                return PhpBytes.Empty;
            }

            byte[] data = bytes.ReadonlyData;
            byte[] result = new byte[data.Length];

            // perform conversion:
            if (fromTable == null)
            {
                if (toTable != null)
                {
                    for (int i = 0; i < data.Length; i++) result[i] = toTable[data[i] + 256];
                }
            }
            else
            {
                if (toTable == null)
                {
                    for (int i = 0; i < data.Length; i++) result[i] = fromTable[data[i]];
                }
                else
                {
                    for (int i = 0; i < data.Length; i++) result[i] = toTable[fromTable[data[i]] + 256];
                }
            }

            return new PhpBytes(result);
        }

        #endregion


        #region count_chars

        /// <summary>
        /// Creates a histogram of Unicode character occurence in the given string.
        /// </summary>
        /// <param name="str">The string to be processed.</param>
        /// <returns>The array of characters frequency (unsorted).</returns>
        [ImplementsFunction("count_chars_unicode")]
        public static PhpArray CountChars(string str)
        {
            PhpArray count = new PhpArray();

            for (int i = str.Length - 1; i >= 0; i--)
            {
                int j = (int)str[i];
                object c = count[j];
                count[j] = (c == null) ? 1 : (int)c + 1;
            }

            return count;
        }

        /// <summary>
        /// Creates a histogram of byte occurence in the given array of bytes.
        /// </summary>
        /// <param name="bytes">The array of bytes to be processed.</param>
        /// <returns>The array of bytes frequency.</returns>
        public static int[] CountBytes(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes");

            int[] count = new int[256];

            for (int i = bytes.Length - 1; i >= 0; i--)
                count[bytes[i]]++;

            return count;
        }

        /// <summary>
        /// Creates a histogram of byte occurrence in specified string of bytes.
        /// </summary>
        /// <param name="bytes">Bytes to be processed.</param>
        /// <returns>The array of characters frequency.</returns>
        [ImplementsFunction("count_chars")]
        public static PhpArray CountChars(PhpBytes bytes)
        {
            return (bytes == null) ? new PhpArray() : new PhpArray(CountBytes(bytes.ReadonlyData), 0, 256);
        }

        /// <summary>
        /// Creates a histogram of character occurence in a string or string of bytes.
        /// </summary>
        /// <param name="data">The string or bytes to be processed.</param>
        /// <param name="mode">Determines the type of result.</param>
        /// <returns>Depending on <paramref name="mode"/> the following is returned:
        /// <list type="bullet">
        /// <item><term>0</term><description>an array with the character ordinals as key and their frequency as value,</description></item> 
        /// <item><term>1</term><description>same as 0 but only characters with a frequency greater than zero are listed,</description></item>
        /// <item><term>2</term><description>same as 0 but only characters with a frequency equal to zero are listed,</description></item> 
        /// <item><term>3</term><description>a string containing all used characters is returned,</description></item> 
        /// <item><term>4</term><description>a string containing all not used characters is returned.</description></item>
        /// </list>
        /// </returns>
        /// <exception cref="PhpException">The <paramref name="mode"/> is invalid.</exception>
        /// <exception cref="PhpException">The <paramref name="data"/> contains Unicode characters greater than '\u0800'.</exception>
        [ImplementsFunction("count_chars")]
        public static object CountChars(object data, int mode)
        {
            try
            {
                switch (mode)
                {
                    case 0: return new PhpArray(CountBytes(Core.Convert.ObjectToPhpBytes(data).ReadonlyData), 0, 256);
                    case 1: return new PhpArray(CountBytes(Core.Convert.ObjectToPhpBytes(data).ReadonlyData), 0, 256, 0, true);
                    case 2: return new PhpArray(CountBytes(Core.Convert.ObjectToPhpBytes(data).ReadonlyData), 0, 256, 0, false);
                    case 3: return GetBytesContained(Core.Convert.ObjectToPhpBytes(data), 0, 255);
                    case 4: return GetBytesNotContained(Core.Convert.ObjectToPhpBytes(data), 0, 255);
                    default: PhpException.InvalidArgument("mode"); return null;
                }
            }
            catch (IndexOutOfRangeException)
            {
                // thrown by char map:
                PhpException.Throw(PhpError.Warning, LibResources.GetString("too_big_unicode_character"));
                return null;
            }
        }

        /// <summary>
        /// Returns a <see cref="String"/> containing all characters used in the specified <see cref="String"/>.
        /// </summary>
        /// <param name="str">The string to process.</param>
        /// <param name="lower">The lower limit for returned chars.</param>
        /// <param name="upper">The upper limit for returned chars.</param>
        /// <returns>
        /// The string containing characters used in <paramref name="str"/> which are sorted according to their ordinal values.
        /// </returns>
        /// <exception cref="IndexOutOfRangeException"><paramref name="str"/> contains characters greater than '\u0800'.</exception>
        public static string GetCharactersContained(string str, char lower, char upper)
        {
            CharMap charmap = InitializeCharMap();

            charmap.Add(str);
            return charmap.ToString(lower, upper, false);
        }

        /// <summary>
        /// Returns a <see cref="String"/> containing all characters used in the specified <see cref="String"/>.
        /// </summary>
        /// <param name="str">The string to process.</param>
        /// <param name="lower">The lower limit for returned chars.</param>
        /// <param name="upper">The upper limit for returned chars.</param>
        /// <returns>
        /// The string containing characters used in <paramref name="str"/> which are sorted according to their ordinal values.
        /// </returns>
        /// <exception cref="IndexOutOfRangeException"><paramref name="str"/> contains characters greater than '\u0800'.</exception>
        public static string GetCharactersNotContained(string str, char lower, char upper)
        {
            CharMap charmap = InitializeCharMap();

            charmap.Add(str);
            return charmap.ToString(lower, upper, true);
        }

        private static BitArray CreateByteMap(PhpBytes/*!*/ bytes, out int count)
        {
            BitArray map = new BitArray(256);
            map.Length = 256;

            count = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                if (!map[bytes[i]])
                {
                    map[bytes[i]] = true;
                    count++;
                }
            }
            return map;
        }

        public static PhpBytes GetBytesContained(PhpBytes bytes, byte lower, byte upper)
        {
            if (bytes == null) bytes = PhpBytes.Empty;

            int count;
            BitArray map = CreateByteMap(bytes, out count);

            byte[] result = new byte[count];
            int j = 0;
            for (int i = lower; i <= upper; i++)
            {
                if (map[i]) result[j++] = (byte)i;
            }

            return new PhpBytes(result);
        }

        public static PhpBytes GetBytesNotContained(PhpBytes bytes, byte lower, byte upper)
        {
            if (bytes == null) bytes = PhpBytes.Empty;

            int count;
            BitArray map = CreateByteMap(bytes, out count);

            byte[] result = new byte[map.Length - count];
            int j = 0;
            for (int i = lower; i <= upper; i++)
            {
                if (!map[i]) result[j++] = (byte)i;
            }

            return new PhpBytes(result);
        }

        #endregion


        #region crypt (CLR only)
#if !SILVERLIGHT

        /// <summary>
        /// Specifies whether standard DES algorithm is implemented.
        /// We set it to 1, but it's not really true - our DES encryption is nothing like PHP's, so the values will be different
        /// If you want key compatibility with PHP, use CRYPT_MD5 by passing in a key starting with "?1?"
        /// </summary>
        [ImplementsConstant("CRYPT_STD_DES")]
        public const int CryptStandardDES = 1;

        /// <summary>
        /// Specifies whether extended DES algorithm is implemented.
        /// </summary>
        [ImplementsConstant("CRYPT_EXT_DES")]
        public const int CryptExtendedDES = 0;

        /// <summary>
        /// Specifies whether MD5 algorithm is implemented.
        /// </summary>
        [ImplementsConstant("CRYPT_MD5")]
        public const int CryptMD5 = 1;

        /// <summary>
        /// Specifies whether Blowfish encryption is implemented.
        /// </summary>
        [ImplementsConstant("CRYPT_BLOWFISH")]
        public const int CryptBlowfish = 0;

        /// <summary>
        /// Specifies the length of the salt applicable to the <see cref="Encrypt"/> method.
        /// </summary>
        [ImplementsConstant("CRYPT_SALT_LENGTH")]
        public const int CryptSaltLength = 9;

        /// <summary>
        /// Encrypts a string (one-way) with a random key.
        /// </summary>
        /// <param name="str">The string to encrypt.</param>
        /// <returns>The encrypted string.</returns>
        [ImplementsFunction("crypt")]
        public static PhpBytes Encrypt(PhpBytes str)
        {
            return Encrypt(str, null);
        }

        private const int MaxMD5Key = 12;
        private const int InternalMD5Key = 8;
        private const int MaxKeyLength = MaxMD5Key;
        private const int MaxDESKey = 8;

        public static bool ByteArrayEquals(byte[] array1, byte[] array2, int compareLength)
        {
            // If the other object is null, of a diffent type, or   
            // of an array of a different length then skip out now.   
            if ((array2 == null) || (array1 == null) || (compareLength <= 0 && (array1.Length != array2.Length)))
                return false;

            int minArray = Math.Min(array1.Length, array2.Length);
            if (compareLength <= 0)
                compareLength = minArray;
            else
                compareLength = Math.Min(minArray, compareLength);

            // If any of the elements are not equal, skip out.   
            for (int i = 0; i < compareLength; ++i)
                if (array1[i] != array2[i])
                    return false;

            // They're both the same length and the elements are all   
            // equal so consider the arrays to be equal.   
            return true;
        }

        //PHP's non-standard base64 used in converting md5 binary crypt() into chars
        //0 ... 63 => ascii - 64
        //aka bin_to_ascii ((c) >= 38 ? ((c) - 38 + 'a') : (c) >= 12 ? ((c) - 12 + 'A') : (c) + '.')
        private static byte[] itoa64 = System.Text.Encoding.ASCII.GetBytes("./0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");

        private static void to64(MemoryStream stream, UInt32 v, int n)
        {
            while (--n >= 0)
            {
                stream.WriteByte(itoa64[v & 0x3f]);
                v >>= 6;
            }
        }

        private static byte[] MD5MagicString = System.Text.Encoding.ASCII.GetBytes("$1$");
        private static PhpBytes DoMD5Password(byte[] key, PhpBytes password)
        {
            MD5CryptoServiceProvider ctx = new MD5CryptoServiceProvider(), ctx1 = new MD5CryptoServiceProvider();
            MemoryStream result = new MemoryStream();
            byte[] final = new byte[16];

            int startOffset = 0, endOffset = 0;
            /* If it starts with the magic string, then skip that */
            if (ByteArrayEquals(key, MD5MagicString, MD5MagicString.Length))
                startOffset += MD5MagicString.Length;

            /* It stops at the first '$', max InternalMD5Key chars */
            for (endOffset = startOffset; key[endOffset] != '\0' && key[endOffset] != '$' && endOffset < (startOffset + InternalMD5Key); ++endOffset)
                continue;
            int keyLength = endOffset - startOffset;

            // PHP puts the relevant salt characters in the beginning
            result.Write(MD5MagicString, 0, MD5MagicString.Length);
            result.Write(key, startOffset, keyLength);
            result.Write(System.Text.Encoding.ASCII.GetBytes(new char[] { '$' }), 0, 1);

            ctx.Initialize();
            /* The password first, since that is what is most unknown */
            ctx.TransformBlock(password.ReadonlyData, 0, password.Length, null, 0);
            ctx.TransformBlock(MD5MagicString, 0, MD5MagicString.Length, null, 0);
            ctx.TransformBlock(key, startOffset, keyLength, null, 0);

            ctx1.Initialize();
            /* Then just as many characters of the MD5(pw,salt,pw) */
            ctx1.TransformBlock(password.ReadonlyData, 0, password.Length, null, 0);
            ctx1.TransformBlock(key, startOffset, keyLength, null, 0);
            ctx1.TransformFinalBlock(password.ReadonlyData, 0, password.Length);
            Array.Copy(ctx1.Hash, final, final.Length);

            for (int pl = password.Length; pl > 0; pl -= 16)
                ctx.TransformBlock(final, 0, pl > 16 ? 16 : pl, null, 0);

            //Clear the data
            for (int i = 0; i < final.Length; ++i)
                final[i] = 0;

            // "Then something really weird...", per zend PHP - what a ridiculous waste of CPU cycles
            byte[] zeroByte = new byte[1] { 0 };
            for (int i = password.Length; i != 0; i >>= 1)
            {
                if ((i & 1) != 0)
                    ctx.TransformBlock(zeroByte, 0, 1, null, 0);
                else
                    ctx.TransformBlock(password.ReadonlyData, 0, 1, null, 0);
            }

            ctx.TransformFinalBlock(ArrayUtils.EmptyBytes, 0, 0);
            Array.Copy(ctx.Hash, final, final.Length);

            /* Per md5crypt.c, again ridiculous but we want to keep consistent "
            * And now, just to make sure things don't run too fast. On a 60 MHz
            * Pentium this takes 34 msec, so you would need 30 seconds to build
            * a 1000 entry dictionary... "
            */
            for (int i = 0; i < 1000; ++i)
            {
                ctx1.Initialize();

                if ((i & 1) != 0)
                    ctx1.TransformBlock(password.ReadonlyData, 0, password.Length, null, 0);
                else
                    ctx1.TransformBlock(final, 0, final.Length, null, 0);

                if ((i % 3) != 0)
                    ctx1.TransformBlock(key, startOffset, keyLength, null, 0);

                if ((i % 7) != 0)
                    ctx1.TransformBlock(password.ReadonlyData, 0, password.Length, null, 0);

                if ((i & 1) != 0)
                    ctx1.TransformFinalBlock(final, 0, final.Length);
                else
                    ctx1.TransformFinalBlock(password.ReadonlyData, 0, password.Length);

                Array.Copy(ctx1.Hash, final, final.Length);
            }

            to64(result, ((UInt32)final[0] << 16) | ((UInt32)final[6] << 8) | (UInt32)final[12], 4);
            to64(result, ((UInt32)final[1] << 16) | ((UInt32)final[7] << 8) | (UInt32)final[13], 4);
            to64(result, ((UInt32)final[2] << 16) | ((UInt32)final[8] << 8) | (UInt32)final[14], 4);
            to64(result, ((UInt32)final[3] << 16) | ((UInt32)final[9] << 8) | (UInt32)final[15], 4);
            to64(result, ((UInt32)final[4] << 16) | ((UInt32)final[10] << 8) | (UInt32)final[5], 4);
            to64(result, (UInt32)final[11], 2);

            return new PhpBytes(result.ToArray());
        }

        /// <summary>
        /// Encrypts a string (one-way) with given key.
        /// </summary>
        /// <param name="str">The string of bytes to encrypt</param>
        /// <param name="salt">The key.</param>
        /// <returns>The encrypted string.</returns>
        [ImplementsFunction("crypt")]
        public static PhpBytes Encrypt(PhpBytes str, PhpBytes salt)
        {
            if (str == null) str = PhpBytes.Empty;

            Stream stream = new System.IO.MemoryStream(str.ReadonlyData);

            bool usemd5 = (salt == null) || (salt.Length == 0) || ByteArrayEquals(salt.ReadonlyData, MD5MagicString, MD5MagicString.Length);
            int requiredKeyLength = usemd5 ? MaxMD5Key : MaxDESKey;

            byte[] key = new byte[requiredKeyLength];
            int saltLength = requiredKeyLength;

            DES des = new DESCryptoServiceProvider();

            // prepare the key if salt is provided:
            if ((salt != null) && (salt.Length > 0))
            {
                //Fill with $'s first, same as zend PHP
                Array.Copy(System.Text.Encoding.ASCII.GetBytes(new String('$', requiredKeyLength)), key, requiredKeyLength);

                saltLength = System.Math.Min(requiredKeyLength, salt.Length);
                Array.Copy(salt.ReadonlyData, key, saltLength);
            }
            else
                Array.Copy(des.Key, key, InternalMD5Key);	//Random 8-byte sequence

            if (usemd5)
            {
                return DoMD5Password(key, str);
            }
            else
            {
                MemoryStream result = new MemoryStream();
                des.IV = new byte[8];
                des.Key = key;

                ICryptoTransform transform = des.CreateEncryptor(des.Key, des.IV);
                CryptoStream cs = new CryptoStream(stream, transform, CryptoStreamMode.Read);

                // PHP puts the relevant salt characters in the beginning
                result.Write(key, 0, saltLength);

                byte[] buffer = new byte[256];
                int rd;

                while ((rd = cs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    int i;
                    for (i = 0; i < rd; ++i)
                    {
                        switch (i % 3)
                        {
                            case 0:
                                result.WriteByte(itoa64[buffer[i] >> 2]);
                                break;
                            case 1:
                                result.WriteByte(itoa64[((buffer[i - 1] & 0x3) << 4) | (buffer[i] >> 4)]);
                                break;
                            case 2:
                                result.WriteByte(itoa64[((buffer[i - 1] & 0xF) << 2) | (buffer[i] >> 6)]);
                                result.WriteByte(itoa64[buffer[i] & 0x3F]);
                                break;
                        }
                    }
                    //Leftover bits
                    switch (i % 3)
                    {
                        case 1:
                            result.WriteByte(itoa64[((buffer[i - 1] & 0x3) << 4)]);
                            break;
                        case 2:
                            result.WriteByte(itoa64[((buffer[i - 1] & 0xF) << 2)]);
                            break;
                    }
                }

                return new PhpBytes(result.ToArray());
            }
        }

#endif
        #endregion

        #endregion


        #region strrev, strspn, strcspn

        /// <summary>
        /// Reverses the given string.
        /// </summary>
        /// <param name="obj">The string to be reversed.</param>
        /// <returns>The reversed string or empty string if <paramref name="obj"/> is null.</returns>
        [ImplementsFunction("strrev")]
        [PureFunction]
        public static object Reverse(object obj)
        {
            PhpBytes bytes;
            if ((bytes = obj as PhpBytes) != null)
            {
                return Reverse(bytes);
            }
            else
            {
                return Reverse(PHP.Core.Convert.ObjectToString(obj));
            }
        }

        internal static PhpBytes Reverse(PhpBytes bytes)
        {
            int length;
            if ((length = bytes.Length) == 0)
                return PhpBytes.Empty;

            byte[] reversed = new byte[length];
            byte[] data = bytes.ReadonlyData;

            for (int i = 0, j = length - 1; j >= 0; j--, i++)
                reversed[i] = data[j];

            return new PhpBytes(reversed);
        }

        internal static string Reverse(string str)
        {
            if (String.IsNullOrEmpty(str))
                return String.Empty;

            int length = str.Length;
            StringBuilder result = new StringBuilder(length, length);
            result.Length = length;

            for (int i = 0, j = length - 1; j >= 0; j--, i++)
                result[i] = str[j];

            return result.ToString();
        }

        /// <summary>
        /// Finds a length of an initial segment consisting entirely of specified characters.
        /// </summary>
        /// <param name="str">The string to be searched in.</param>
        /// <param name="acceptedChars">Accepted characters.</param>
        /// <returns>
        /// The length of the initial segment consisting entirely of characters in <paramref name="acceptedChars"/>
        /// or zero if any argument is null.
        /// </returns>
        [ImplementsFunction("strspn")]
        public static int StrSpn(string str, string acceptedChars)
        {
            return StrSpnInternal(str, acceptedChars, 0, int.MaxValue, false);
        }

        /// <summary>
        /// Finds a length of a segment consisting entirely of specified characters.
        /// </summary>
        /// <param name="str">The string to be searched in.</param>
        /// <param name="acceptedChars">Accepted characters.</param>
        /// <param name="offset">The relativized offset of the first item of the slice.</param>
        /// <returns>
        /// The length of the substring consisting entirely of characters in <paramref name="acceptedChars"/> or 
        /// zero if any argument is null. Search starts from absolutized <paramref name="offset"/>
        /// (see <see cref="PhpMath.AbsolutizeRange"/> where <c>length</c> is infinity).
        /// </returns>
        [ImplementsFunction("strspn")]
        public static int StrSpn(string str, string acceptedChars, int offset)
        {
            return StrSpnInternal(str, acceptedChars, offset, int.MaxValue, false);
        }

        /// <summary>
        /// Finds a length of a segment consisting entirely of specified characters.
        /// </summary>
        /// <param name="str">The string to be searched in.</param>
        /// <param name="acceptedChars">Accepted characters.</param>
        /// <param name="offset">The relativized offset of the first item of the slice.</param>
        /// <param name="length">The relativized length of the slice.</param>
        /// <returns>
        /// The length of the substring consisting entirely of characters in <paramref name="acceptedChars"/> or 
        /// zero if any argument is null. Search starts from absolutized <paramref name="offset"/>
        /// (see <see cref="PhpMath.AbsolutizeRange"/> and takes at most absolutized <paramref name="length"/> characters.
        /// </returns>
        [ImplementsFunction("strspn")]
        public static int StrSpn(string str, string acceptedChars, int offset, int length)
        {
            return StrSpnInternal(str, acceptedChars, offset, length, false);
        }

        /// <summary>
        /// Finds a length of an initial segment consisting entirely of any characters excpept for specified ones.
        /// </summary>
        /// <param name="str">The string to be searched in.</param>
        /// <param name="acceptedChars">Accepted characters.</param>
        /// <returns>
        /// The length of the initial segment consisting entirely of characters not in <paramref name="acceptedChars"/>
        /// or zero if any argument is null.
        /// </returns>
        [ImplementsFunction("strcspn")]
        public static int StrCSpn(string str, string acceptedChars)
        {
            return StrSpnInternal(str, acceptedChars, 0, int.MaxValue, true);
        }

        /// <summary>
        /// Finds a length of a segment consisting entirely of any characters excpept for specified ones.
        /// </summary>
        /// <param name="str">The string to be searched in.</param>
        /// <param name="acceptedChars">Accepted characters.</param>
        /// <param name="offset">The relativized offset of the first item of the slice.</param>
        /// <returns>
        /// The length of the substring consisting entirely of characters not in <paramref name="acceptedChars"/> or 
        /// zero if any argument is null. Search starts from absolutized <paramref name="offset"/>
        /// (see <see cref="PhpMath.AbsolutizeRange"/> where <c>length</c> is infinity).
        /// </returns>
        [ImplementsFunction("strcspn")]
        public static int StrCSpn(string str, string acceptedChars, int offset)
        {
            return StrSpnInternal(str, acceptedChars, offset, int.MaxValue, true);
        }

        /// <summary>
        /// Finds a length of a segment consisting entirely of any characters except for specified ones.
        /// </summary>
        /// <param name="str">The string to be searched in.</param>
        /// <param name="acceptedChars">Accepted characters.</param>
        /// <param name="offset">The relativized offset of the first item of the slice.</param>
        /// <param name="length">The relativized length of the slice.</param>
        /// <returns>
        /// The length of the substring consisting entirely of characters not in <paramref name="acceptedChars"/> or 
        /// zero if any argument is null. Search starts from absolutized <paramref name="offset"/>
        /// (see <see cref="PhpMath.AbsolutizeRange"/> and takes at most absolutized <paramref name="length"/> characters.
        /// </returns>
        [ImplementsFunction("strcspn")]
        public static int StrCSpn(string str, string acceptedChars, int offset, int length)
        {
            return StrSpnInternal(str, acceptedChars, offset, length, true);
        }

        /// <summary>
        /// Internal version of <see cref="StrSpn"/> (complement off) and <see cref="StrCSpn"/> (complement on).
        /// </summary>
        internal static int StrSpnInternal(string str, string acceptedChars, int offset, int length, bool complement)
        {
            if (str == null || acceptedChars == null) return 0;

            PhpMath.AbsolutizeRange(ref offset, ref length, str.Length);

            char[] chars = acceptedChars.ToCharArray();
            Array.Sort(chars);

            int j = offset;

            if (complement)
            {
                while (length > 0 && ArrayUtils.BinarySearch(chars, str[j]) < 0) { j++; length--; }
            }
            else
            {
                while (length > 0 && ArrayUtils.BinarySearch(chars, str[j]) >= 0) { j++; length--; }
            }

            return j - offset;
        }

        #endregion


        #region explode, implode

        /// <summary>
        /// Splits a string by string separators.
        /// </summary>
        /// <param name="separator">The substrings separator. Must not be empty.</param>
        /// <param name="str">The string to be split.</param>
        /// <returns>The array of strings.</returns>
        [ImplementsFunction("explode")]
        [return: CastToFalse]
        public static PhpArray Explode(string separator, string str)
        {
            return Explode(separator, str, Int32.MaxValue);
        }

        /// <summary>
        /// Splits a string by string separators with limited resulting array size.
        /// </summary>
        /// <param name="separator">The substrings separator. Must not be empty.</param>
        /// <param name="str">The string to be split.</param>
        /// <param name="limit">
        /// The maximum number of elements in the resultant array. Zero value is treated in the same way as 1.
        /// If negative, then the number of separators found in the string + 1 is added to the limit.
        /// </param>
        /// <returns>The array of strings.</returns>
        /// <remarks>
        /// If <paramref name="str"/> is empty an array consisting of exacty one empty string is returned.
        /// If <paramref name="limit"/> is zero
        /// </remarks>
        /// <exception cref="PhpException">Thrown if the <paramref name="separator"/> is null or empty or if <paramref name="limit"/>is not positive nor -1.</exception>
        [ImplementsFunction("explode")]
        [return: CastToFalse]
        public static PhpArray Explode(string separator, string str, int limit)
        {
            // validate parameters:
            if (String.IsNullOrEmpty(separator))
            {
                PhpException.InvalidArgument("separator", LibResources.GetString("arg:null_or_empty"));
                return null;
            }

            if (str == null) str = String.Empty;

            bool last_part_is_the_rest = limit >= 0;

            if (limit == 0)
                limit = 1;
            else if (limit < 0)
                limit += SubstringCountInternal(str, separator, 0, str.Length) + 2;
            
            // splits <str> by <separator>:
            int sep_len = separator.Length;
            int i = 0;                        // start searching at this position
            int pos;                          // found separator's first character position
            PhpArray result = new PhpArray(); // creates integer-keyed array with default capacity

            var/*!*/compareInfo = System.Globalization.CultureInfo.InvariantCulture.CompareInfo;

            while (--limit > 0)
            {
                pos = compareInfo.IndexOf(str, separator, i, str.Length - i, System.Globalization.CompareOptions.Ordinal);

                if (pos < 0) break; // not found

                result.AddToEnd(str.Substring(i, pos - i)); // faster than Add()
                i = pos + sep_len;
            }

            // Adds last chunk. If separator ends the string, it will add empty string (as PHP do).
            if (i <= str.Length && last_part_is_the_rest)
            {
                result.AddToEnd(str.Substring(i));
            }

            return result;
        }

        /// <summary>
        /// Concatenates items of an array into a string separating them by a glue.
        /// </summary>
        /// <param name="pieces">The array to be impleded.</param>
        /// <returns>The glued string.</returns>
        [ImplementsFunction("join")]
        public static object JoinGeneric(PhpArray pieces)
        {
            return ImplodeGeneric(pieces);
        }

        /// <summary>
        /// Concatenates items of an array into a string separating them by a glue.
        /// </summary>
        /// <param name="pieces">The array to be impleded.</param>
        /// <param name="glue">The glue string.</param>
        /// <returns>The glued string.</returns>
        /// <exception cref="PhpException">Thrown if neither <paramref name="glue"/> nor <paramref name="pieces"/> is not null and of type <see cref="PhpArray"/>.</exception>
        [ImplementsFunction("join")]
        public static object JoinGeneric(object glue, object pieces)
        {
            return ImplodeGeneric(glue, pieces);
        }

        /// <summary>
        /// Concatenates items of an array into a string.
        /// </summary>
        /// <param name="pieces">The <see cref="PhpArray"/> to be imploded.</param>
        /// <returns>The glued string.</returns>
        [ImplementsFunction("implode")]
        public static object ImplodeGeneric(PhpArray pieces)
        {
            if (pieces == null)
            {
                PhpException.ArgumentNull("pieces");
                return null;
            }

            return Implode("", pieces);
        }

        /// <summary>
        /// Concatenates items of an array into a string separating them by a glue.
        /// </summary>
        /// <param name="glue">The glue of type <see cref="string"/> or <see cref="PhpArray"/> to be imploded.</param>
        /// <param name="pieces">The <see cref="PhpArray"/> to be imploded or glue of type <see cref="string"/>.</param>
        /// <returns>The glued string.</returns>
        /// <exception cref="PhpException">Thrown if neither <paramref name="glue"/> nor <paramref name="pieces"/> is not null and of type <see cref="PhpArray"/>.</exception>
        [ImplementsFunction("implode")]
        public static object ImplodeGeneric(object glue, object pieces)
        {
            if (pieces != null && pieces.GetType() == typeof(PhpArray))
                return Implode(glue, (PhpArray)pieces);

            if (glue != null && glue.GetType() == typeof(PhpArray))
                return Implode(pieces, (PhpArray)glue);

            return ImplodeGenericEnumeration(glue, pieces);
        }

        private static object ImplodeGenericEnumeration(object glue, object pieces)
        {
            Core.Reflection.DObject dobj;
            IEnumerable enumerable;

            if ((dobj = pieces as Core.Reflection.DObject) != null && (enumerable = dobj.RealObject as IEnumerable) != null)
                return Implode(glue, new PhpArray(enumerable));

            if ((dobj = glue as Core.Reflection.DObject) != null && (enumerable = dobj.RealObject as IEnumerable) != null)
                return Implode(pieces, new PhpArray(enumerable));

            //
            PhpException.InvalidArgument("pieces");
            return null;
        }

        /// <summary>
        /// Concatenates items of an array into a string separating them by a glue.
        /// </summary>
        /// <param name="glue">The glue string.</param>
        /// <param name="pieces">The enumeration to be imploded.</param>
        /// <returns>The glued string.</returns>           
        /// <remarks>
        /// Items of <paramref name="pieces"/> are converted to strings in the manner of PHP 
        /// (i.e. by <see cref="PHP.Core.Convert.ObjectToString"/>).
        /// </remarks>
        /// <exception cref="PhpException">Thrown if <paramref name="pieces"/> is null.</exception>
        public static object Implode(object glue, PhpArray/*!*/pieces)
        {
            Debug.Assert(pieces != null);

            // handle empty pieces:
            if (pieces.Count == 0)
                return string.Empty;

            // check whether we have to preserve a binary string
            bool binary = glue != null && glue.GetType() == typeof(PhpBytes);
            if (!binary)    // try to find any binary string within pieces:
                using (var x = pieces.GetFastEnumerator())
                    while (x.MoveNext())
                        if (x.CurrentValue != null && x.CurrentValue.GetType() == typeof(PhpBytes))
                        {
                            binary = true;
                            break;
                        }

            // concatenate pieces and glue:

            bool not_first = false;                       // not the first iteration

            if (binary)
            {
                Debug.Assert(pieces.Count > 0);

                PhpBytes gluebytes = PHP.Core.Convert.ObjectToPhpBytes(glue);
                PhpBytes[] piecesBytes = new PhpBytes[pieces.Count + pieces.Count - 1]; // buffer of PhpBytes to be concatenated
                int p = 0;
                
                 using (var x = pieces.GetFastEnumerator())
                     while (x.MoveNext())
                     {
                         if (not_first) piecesBytes[p++] = gluebytes;
                         else not_first = true;

                         piecesBytes[p++] = PHP.Core.Convert.ObjectToPhpBytes(x.CurrentValue);
                     }

                 return PhpBytes.Concat(piecesBytes, 0, piecesBytes.Length);
            }
            else
            {
                string gluestr = PHP.Core.Convert.ObjectToString(glue);

                StringBuilder result = new StringBuilder();

                using (var x = pieces.GetFastEnumerator())
                    while (x.MoveNext())
                    {
                        if (not_first) result.Append(gluestr);
                        else not_first = true;

                        result.Append(PHP.Core.Convert.ObjectToString(x.CurrentValue));
                    }

                return result.ToString();
            }            
        }

        #endregion


        #region strtr, str_rot13

        /// <summary>
        /// Replaces specified characters in a string with another ones.
        /// </summary>
        /// <param name="str">A string where to do the replacement.</param>
        /// <param name="from">Characters to be replaced.</param>
        /// <param name="to">Characters to replace those in <paramref name="from"/> with.</param>
        /// <returns>
        /// A copy of <paramref name="str"/> with all occurrences of each character in <paramref name="from"/> 
        /// replaced by the corresponding character in <paramref name="to"/>.
        /// </returns>
        /// <remarks>
        /// <para>If <paramref name="from"/> and <paramref name="to"/> are different lengths, the extra characters 
        /// in the longer of the two are ignored.</para>
        /// </remarks>
        [ImplementsFunction("strtr")]
        [PureFunction]
        public static string Translate(string str, string from, string to)
        {
            if (String.IsNullOrEmpty(str) || from == null || to == null) return String.Empty;

            int min_length = Math.Min(from.Length, to.Length);
            Dictionary<char, char> ht = new Dictionary<char, char>(min_length);

            // adds chars to the hashtable:
            for (int i = 0; i < min_length; i++)
                ht[from[i]] = to[i];

            // creates result builder:
            StringBuilder result = new StringBuilder(str.Length, str.Length);
            result.Length = str.Length;

            // translates:
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                char h;
                result[i] = ht.TryGetValue(c, out h) ? h : c;

                // obsolete:
                // object h = ht[c];
                // result[i] = (h==null) ? c : h;
            }

            return result.ToString();
        }

        /// <summary>
        /// Compares objects according to the length of their string representation
        /// as the primary criteria and the alphabetical order as the secondary one.
        /// </summary>
        private sealed class StringLengthComparer : IComparer<string>
        {
            /// <summary>
            /// Performs length and alphabetical comparability backwards (longer first).
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <returns></returns>
            public int Compare(string x, string y)
            {
                int rv = x.Length - y.Length;
                if (rv == 0) return -string.CompareOrdinal(x, y);
                else return -rv;
            }
        }

        /// <summary>
        /// Replaces substrings according to a dictionary.
        /// </summary>
        /// <param name="str">Input string.</param>
        /// <param name="replacePairs">
        /// An dictionary that contains <see cref="string"/> to <see cref="string"/> replacement mapping.
        /// </param>
        /// <returns>A copy of str, replacing all substrings (looking for the longest possible match).</returns>
        /// <remarks>This function will not try to replace stuff that it has already worked on.</remarks>
        /// <exception cref="PhpException">Thrown if the <paramref name="replacePairs"/> argument is null.</exception>
        [ImplementsFunction("strtr")]
        [return: CastToFalse]
        public static string Translate(string str, PhpArray replacePairs)
        {
            if (replacePairs == null)
            {
                PhpException.ArgumentNull("replacePairs");
                return null;
            }

            if (string.IsNullOrEmpty(str))
                return String.Empty;

            // sort replacePairs according to the key length, longer first
            var count = replacePairs.Count;
            var sortedkeys = new string[count];
            var sortedValues = new string[count];

            int i = 0;
            var replacePairsEnum = replacePairs.GetFastEnumerator();
            while (replacePairsEnum.MoveNext())
            {
                string key = replacePairsEnum.CurrentKey.ToString();
                string value = Core.Convert.ObjectToString(replacePairsEnum.CurrentValue);

                if (key.Length == 0)
                {
                    // TODO: an exception ?
                    return null;
                }

                sortedkeys[i] = key;
                sortedValues[i] = value;
                i++;
            }
            Array.Sort<string, string>(sortedkeys, sortedValues, new StringLengthComparer());   // perform quick sort, much faster than SortedList

            // perform replacement
            StringBuilder result = new StringBuilder(str);
            StringBuilder temp = new StringBuilder(str);
            int length = str.Length;
            int[] offset = new int[length];

            for (i = 0; i < sortedkeys.Length; i++)
            {
                var key = sortedkeys[i];
                int index = 0;

                while ((index = temp.ToString().IndexOf(key, index, StringComparison.Ordinal)) >= 0)   // ordinal search, because of exotic Unicode characters are find always at the beginning of the temp
                {
                    var value = sortedValues[i];
                    var keyLength = key.Length;
                    int replaceAtIndex = index + offset[index];

                    // replace occurrence in result
                    result.Replace(index + offset[index], keyLength, value);

                    // Pack the offset array (drop the items removed from temp)
                    for (int j = index + keyLength; j < offset.Length; j++)
                        offset[j - keyLength] = offset[j];

                    // Ensure that we don't replace stuff that we already have worked on by
                    // removing the replaced substring from temp.
                    temp.Remove(index, keyLength);
                    for (int j = index; j < length; j++) offset[j] += value.Length;
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// GetUserEntryPoint encode a string by shifting every letter (a-z, A-Z) by 13 places in the alphabet.
        /// </summary>
        /// <param name="str">The string to be encoded.</param>
        /// <returns>The string with characters rotated by 13 places.</returns>
        [ImplementsFunction("str_rot13")]
        [PureFunction]
        public static string Rotate13(string str)
        {
            return Translate(str,
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ",
                "nopqrstuvwxyzabcdefghijklmNOPQRSTUVWXYZABCDEFGHIJKLM");
        }

        #endregion


        #region substr, str_repeat

        /// <summary>
        /// Retrieves a substring from the given string.
        /// </summary>
        /// <param name="str">The source string (unicode or binary).</param>
        /// <param name="offset">The relativized offset of the first item of the slice.</param>
        /// <returns>The substring of the <paramref name="str"/>.</returns>
        /// <remarks>
        /// See <see cref="PhpMath.AbsolutizeRange"/> for details about <paramref name="offset"/> where <c>length</c> is infinity.
        /// </remarks>
        [ImplementsFunction("substr")]
        [PureFunction]
        [return: CastToFalse]
        public static object Substring(object str, int offset)
        {
            return Substring(str, offset, int.MaxValue);
        }

        /// <summary>
        /// Retrieves a substring from the given string.
        /// </summary>
        /// <param name="str">The source string (unicode or binary).</param>
        /// <param name="offset">The relativized offset of the first item of the slice.</param>
        /// <param name="length">The relativized length of the slice.</param>
        /// <returns>The substring of the <paramref name="str"/>.</returns>
        /// <remarks>
        /// See <see cref="PhpMath.AbsolutizeRange"/> for details about <paramref name="offset"/> and <paramref name="length"/>.
        /// </remarks>
        [ImplementsFunction("substr")]
        [PureFunction]
        [return: CastToFalse]
        public static object Substring(object str, int offset, int length)
        {
            PhpBytes binstr = str as PhpBytes;
            if (binstr != null)
            {
                if (binstr.Length == 0) return null;

                PhpMath.AbsolutizeRange(ref offset, ref length, binstr.Length);

                // string is shorter than offset to start substring
                if (offset == binstr.Length) return null;

                if (length == 0) return PhpBytes.Empty;

                byte[] substring = new byte[length];

                Buffer.BlockCopy(binstr.ReadonlyData, offset, substring, 0, length);

                return new PhpBytes(substring);
            }

            string unistr = Core.Convert.ObjectToString(str);
            if (unistr != null)
            {
                if (unistr == String.Empty) return null;

                PhpMath.AbsolutizeRange(ref offset, ref length, unistr.Length);

                // string is shorter than offset to start substring
                if (offset == unistr.Length) return null;

                if (length == 0) return String.Empty;

                return unistr.Substring(offset, length);
            }

            return null;
        }

        /// <summary>
        /// Repeats a string.
        /// </summary>
        /// <param name="str">The input string, can be both binary and unicode.</param>
        /// <param name="count">The number of times <paramref name="str"/> should be repeated.</param>
        /// <returns>The string where <paramref name="str"/> is repeated <paramref name="count"/> times.</returns>
        /// <remarks>If <paramref name="str"/> is <b>null</b> reference, the function will return an empty string.</remarks>   
        /// <remarks>If <paramref name="count"/> is set to 0, the function will return <b>null</b> reference.</remarks>   
        /// <exception cref="PhpException">Thrown if <paramref name="count"/> is negative.</exception>
        [ImplementsFunction("str_repeat")]
        [PureFunction]
        public static object Repeat(object str, int count)
        {
            if (str == null) return String.Empty;

            if (count < 0)
            {
                PhpException.Throw(PhpError.Warning, LibResources.GetString("number_of_repetitions_negative"));
                return null;
            }
            if (count == 0) return null;

            PhpBytes binstr = str as PhpBytes;
            if (binstr != null)
            {
                byte[] result = new byte[binstr.Length * count];

                for (int i = 0; i < count; i++) Buffer.BlockCopy(binstr.ReadonlyData, 0, result, binstr.Length * i, binstr.Length);

                return new PhpBytes(result);
            }

            string unistr = Core.Convert.ObjectToString(str);
            if (unistr != null)
            {
                StringBuilder result = new StringBuilder(count * unistr.Length);
                while (count-- > 0) result.Append(unistr);

                return result.ToString();
            }

            return null;
        }

        #endregion


        #region substr_count, substr_replace, substr_compare

        #region substr_count internals

        private static bool SubstringCountInternalCheck(string needle)
        {
            if (String.IsNullOrEmpty(needle))
            {
                PhpException.InvalidArgument("needle", LibResources.GetString("arg:null_or_empty"));
                return false;
            }

            return true;
        }
        private static bool SubstringCountInternalCheck(string haystack, int offset)
        {
            if (offset < 0)
            {
                PhpException.InvalidArgument("offset", LibResources.GetString("substr_count_offset_zero"));
                return false;
            }
            if (offset > haystack.Length)
            {
                PhpException.InvalidArgument("offset", LibResources.GetString("substr_count_offset_exceeds", offset));
                return false;
            }

            return true;
        }
        private static bool SubstringCountInternalCheck(string haystack, int offset, int length)
        {
            if (!SubstringCountInternalCheck(haystack, offset))
                return false;

            if (length == 0)
            {
                PhpException.InvalidArgument("length", LibResources.GetString("substr_count_zero_length"));
                return false;
            }
            if (offset + length > haystack.Length)
            {
                PhpException.InvalidArgument("length", LibResources.GetString("substr_count_length_exceeds", length));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Count the number of substring occurrences. Expects correct argument values.
        /// </summary>
        internal static int SubstringCountInternal(string/*!*/ haystack, string/*!*/ needle, int offset, int end)
        {
            int result = 0;

            if (needle.Length == 1)
            {
                while (offset < end)
                {
                    if (haystack[offset] == needle[0]) result++;
                    offset++;
                }
            }
            else
            {
                while ((offset = haystack.IndexOf(needle, offset, end - offset)) != -1)
                {
                    offset += needle.Length;
                    result++;
                }
            }
            return result;
        }


        #endregion

        /// <summary>
        /// See <see cref="SubstringCount(string,string,int,int)"/>.
        /// </summary>
        [ImplementsFunction("substr_count")]
        [PureFunction]
        [return: CastToFalse]
        public static int SubstringCount(string haystack, string needle)
        {
            if (String.IsNullOrEmpty(haystack)) return 0;
            if (!SubstringCountInternalCheck(needle)) return -1;

            return SubstringCountInternal(haystack, needle, 0, haystack.Length);
        }

        /// <summary>
        /// See <see cref="SubstringCount(string,string,int,int)"/>.
        /// </summary>
        [ImplementsFunction("substr_count")]
        [PureFunction]
        [return: CastToFalse]
        public static int SubstringCount(string haystack, string needle, int offset)
        {
            if (String.IsNullOrEmpty(haystack)) return 0;
            if (!SubstringCountInternalCheck(needle)) return -1;
            if (!SubstringCountInternalCheck(haystack, offset)) return -1;

            return SubstringCountInternal(haystack, needle, offset, haystack.Length);
        }

        /// <summary>
        /// Count the number of substring occurrences.
        /// </summary>
        /// <param name="haystack">The string.</param>
        /// <param name="needle">The substring.</param>
        /// <param name="offset">The relativized offset of the first item of the slice. Zero if missing in overloads</param>
        /// <param name="length">The relativized length of the slice. Infinity if missing in overloads.</param>
        /// <returns>The number of <paramref name="needle"/> occurences in <paramref name="haystack"/>.</returns>
        /// <example>"aba" has one occurence in "ababa".</example>
        /// <remarks>
        /// See <see cref="PhpMath.AbsolutizeRange"/> for details about <paramref name="offset"/> and <paramref name="length"/>.
        /// </remarks>
        /// <exception cref="PhpException">Thrown if <paramref name="needle"/> is null.</exception>
        [ImplementsFunction("substr_count")]
        [PureFunction]
        [return: CastToFalse]
        public static int SubstringCount(string haystack, string needle, int offset, int length)
        {
            if (String.IsNullOrEmpty(haystack)) return 0;
            if (!SubstringCountInternalCheck(needle)) return -1;
            if (!SubstringCountInternalCheck(haystack, offset, length)) return -1;

            return SubstringCountInternal(haystack, needle, offset, offset + length);
        }

        /// <summary>
        /// See <see cref="SubstringReplace(object,object,object,object)"/>.
        /// </summary>
        [ImplementsFunction("substr_replace")]
        [PureFunction]
        public static object SubstringReplace(object subject, object replacement, object offset)
        {
            return SubstringReplace(subject, replacement, offset, int.MaxValue);
        }

        /// <summary>
        /// Replaces a portion of a string or multiple strings with another string.
        /// </summary>
        /// <param name="subject">The subject of replacement (can be an array of subjects).</param>
        /// <param name="replacement">The replacement string (can be array of replacements).</param>
        /// <param name="offset">The relativized offset of the first item of the slice (can be array of offsets).</param>
        /// <param name="length">The relativized length of the slice (can be array of lengths).</param>
        /// <returns>
        /// Either the <paramref name="subject"/> with a substring replaced by <paramref name="replacement"/> if it is a string
        /// or an array containing items of the <paramref name="subject"/> with substrings replaced by <paramref name="replacement"/>
        /// and indexed by integer keys starting from 0. If <paramref name="replacement"/> is an array, multiple replacements take place.
        /// </returns>
        /// <remarks>
        /// See <see cref="PhpMath.AbsolutizeRange"/> for details about <paramref name="offset"/> and <paramref name="length"/>.
        /// Missing <paramref name="length"/> is considered to be infinity.
        /// If <paramref name="offset"/> and <paramref name="length"/> conversion results in position
        /// less than or equal to zero and greater than or equal to string length, the replacement is prepended and appended, respectively.
        /// </remarks>
        [ImplementsFunction("substr_replace")]
        [PureFunction]
        public static object SubstringReplace(object subject, object replacement, object offset, object length)
        {
            IDictionary dict_subject, dict_replacement, dict_offset, dict_length;
            string[] replacements = null, subjects = null;
            int[] offsets = null, lengths = null;
            int int_offset = 0, int_length = 0;
            string str_replacement = null;

            // prepares string array of subjects:
            if ((dict_subject = subject as IDictionary) != null)
            {
                subjects = new string[dict_subject.Count];
                int i = 0;
                foreach (object item in dict_subject.Values)
                    subjects[i++] = Core.Convert.ObjectToString(item);
            }
            else
            {
                subjects = new string[] { Core.Convert.ObjectToString(subject) };
            }

            // prepares string array of replacements:
            if ((dict_replacement = replacement as IDictionary) != null)
            {
                replacements = new string[dict_replacement.Count];
                int i = 0;
                foreach (object item in dict_replacement.Values)
                    replacements[i++] = Core.Convert.ObjectToString(item);
            }
            else
            {
                str_replacement = Core.Convert.ObjectToString(replacement);
            }

            // prepares integer array of offsets:
            if ((dict_offset = offset as IDictionary) != null)
            {
                offsets = new int[dict_offset.Count];
                int i = 0;
                foreach (object item in dict_offset.Values)
                    offsets[i++] = Core.Convert.ObjectToInteger(item);
            }
            else
            {
                int_offset = Core.Convert.ObjectToInteger(offset);
            }

            // prepares integer array of lengths:
            if ((dict_length = length as IDictionary) != null)
            {
                lengths = new int[dict_length.Count];
                int i = 0;
                foreach (object item in dict_length.Values)
                    lengths[i++] = Core.Convert.ObjectToInteger(item);
            }
            else
            {
                int_length = Core.Convert.ObjectToInteger(length);
            }

            for (int i = 0; i < subjects.Length; i++)
            {
                if (dict_offset != null) int_offset = (i < offsets.Length) ? offsets[i] : 0;
                if (dict_length != null) int_length = (i < lengths.Length) ? lengths[i] : subjects[i].Length;
                if (dict_replacement != null) str_replacement = (i < replacements.Length) ? replacements[i] : "";

                subjects[i] = SubstringReplace(subjects[i], str_replacement, int_offset, int_length);
            }

            if (dict_subject != null)
                return new PhpArray(subjects);
            else
                return subjects[0];
        }

        /// <summary>
        /// Performs substring replacements on subject.
        /// </summary>
        private static string SubstringReplace(string subject, string replacement, int offset, int length)
        {
            PhpMath.AbsolutizeRange(ref offset, ref length, subject.Length);
            return new StringBuilder(subject).Remove(offset, length).Insert(offset, replacement).ToString();
        }

        /// <summary>
        /// Case sensitive comparison of <paramref name="mainStr"/> from position <paramref name="offset"/> 
        /// with <paramref name="str"/>. 
        /// </summary>
        /// <seealso cref="SubstringCompare(string,string,int,int,bool)"/>.
        [ImplementsFunction("substr_compare")]
        [PureFunction]
        public static int SubstringCompare(string mainStr, string str, int offset)
        {
            return SubstringCompare(mainStr, str, offset, Int32.MaxValue, false);
        }

        /// <summary>
        /// Case sensitive comparison of <paramref name="mainStr"/> from position <paramref name="offset"/> 
        /// with <paramref name="str"/> up to the <paramref name="length"/> characters. 
        /// </summary>
        /// <seealso cref="SubstringCompare(string,string,int,int,bool)"/>.
        [ImplementsFunction("substr_compare")]
        [PureFunction]
        public static int SubstringCompare(string mainStr, string str, int offset, int length)
        {
            return SubstringCompare(mainStr, str, offset, length, false);
        }

        /// <summary>
        /// Compares substrings.
        /// </summary>
        /// <param name="mainStr">A string whose substring to compare with <paramref name="str"/>.</param>
        /// <param name="str">The second operand of the comparison.</param>
        /// <param name="offset">An offset in <paramref name="mainStr"/> where to start. Negative value means zero. Offsets beyond <paramref name="mainStr"/> means its length.</param>
        /// <param name="length">A maximal number of characters to compare. Non-positive values means infinity.</param>
        /// <param name="ignoreCase">Whether to ignore case.</param>
        [ImplementsFunction("substr_compare")]
        [PureFunction]
        public static int SubstringCompare(string mainStr, string str, int offset, int length, bool ignoreCase)
        {
            if (mainStr == null) mainStr = "";
            if (str == null) str = "";
            if (length <= 0) length = Int32.MaxValue;
            if (offset < 0) offset = 0;
            if (offset > mainStr.Length) offset = mainStr.Length;

            return String.Compare(mainStr, offset, str, 0, length, ignoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture);
        }

        #endregion


        #region str_replace, str_ireplace

        #region ReplaceInternal

        /// <summary>
        /// A class that enables customized replacement of substrings.
        /// Optimized for multiple replacements.
        /// </summary>
        internal class SubstringReplacer
        {
            private Regex regex;
            private int count;
            private MatchEvaluator evaluator;
            private string replacement;
            private string search;

            public SubstringReplacer(string/*!*/ search, string/*!*/ replacement, bool ignoreCase)
            {
                Debug.Assert(!string.IsNullOrEmpty(search), "Searched string shouln't be empty");

                if (ignoreCase)
                {
                    this.regex = new Regex(Regex.Escape(search), RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    this.evaluator = new MatchEvaluator(Evaluator);
                }
                this.search = search;
                this.replacement = replacement;
            }

            /// <summary>
            /// Called for each matched substring.
            /// </summary>
            /// <returns>Replacement.</returns>
            private string Evaluator(Match match)
            {
                count++;
                return replacement;
            }

            /// <summary>
            /// Replaces all substrings of <paramref name="subject"/> specified in constructor parameter <c>search</c> 
            /// with <see cref="replacement"/>. If <paramref name="replacementCount"/> is non-negative,
            /// advances it by the number of replacements. Retuns resulting string.
            /// </summary>
            public string Replace(string/*!*/ subject, ref int replacementCount)
            {
                string result;
                if (regex == null)
                {
                    if (replacementCount >= 0)
                        replacementCount += SubstringCountInternal(subject, search, 0, subject.Length);

                    if (search.Length != 1 || replacement.Length != 1)
                        result = subject.Replace(search, replacement);
                    else
                        result = subject.Replace(search[0], replacement[0]);
                }
                else
                {
                    this.count = 0;
                    result = regex.Replace(subject, evaluator);

                    if (replacementCount >= 0)
                        replacementCount += this.count;
                }
                return result;
            }
        }

        /*
        /// <summary>
        /// Get enumeration of given parameter.
        /// </summary>
        /// <param name="objOrDictionary">Single object or IDictionary of objects.</param>
        /// <returns>IEnumerable of object/objects.</returns>
        private static IEnumerable ValuesEnumerator(object objOrDictionary)
        {
            IDictionary dict;
            if ((dict = objOrDictionary as IDictionary) != null)
                return dict.Values;
            else
                return new object[] { objOrDictionary ?? string.Empty };
        }
        */

        private class InifiniteEnumerator : IEnumerator
        {
            private readonly object obj;
            public InifiniteEnumerator(object obj)
            {
                this.obj = obj;
            }

            #region IEnumerator Members

            public object Current
            {
                get { return obj; }
            }

            public bool MoveNext()
            {
                return true;
            }

            public void Reset()
            {

            }

            #endregion
        }

        internal static string ReplaceInternal(string/*!*/search, string replace, string/*!*/subject, bool ignoreCase, ref int count)
        {
            SubstringReplacer replacer = new SubstringReplacer(search, replace, ignoreCase);
            return replacer.Replace(subject, ref count);
        }

        internal static string ReplaceInternal(IEnumerable searches, IEnumerator replacements_enum, string/*!*/subject, bool ignoreCase, ref int count)
        {
            //IEnumerator replacements_enum = replacements.GetEnumerator();
            foreach (object s in searches)
            {
                string search_str = Core.Convert.ObjectToString(s);
                string replacement_str = (replacements_enum.MoveNext()) ? Core.Convert.ObjectToString(replacements_enum.Current) : string.Empty;

                if (search_str != string.Empty)
                {
                    SubstringReplacer replacer = new SubstringReplacer(search_str, replacement_str, ignoreCase);
                    subject = replacer.Replace(subject, ref count);

                    if (subject == string.Empty)
                        break;
                }
            }

            return subject;
        }

        internal static PhpArray ReplaceInternal(string search, string replace, ref DictionaryEntry[] subjects, bool ignoreCase, ref int count)
        {
            SubstringReplacer replacer = new SubstringReplacer(search, replace, ignoreCase);

            PhpArray result = new PhpArray();

            foreach (var entry in subjects)
            {
                // subjects has already been converted to CLR strings:
                string subject_str = entry.Value as string;
                result.Add(entry.Key, string.IsNullOrEmpty(subject_str) ? entry.Value : replacer.Replace(subject_str, ref count));
            }

            return result;
        }

        internal static PhpArray ReplaceInternal(IEnumerable searches, IEnumerator replacements_enum, ref DictionaryEntry[] subjects, bool ignoreCase, ref int count)
        {
            // performs replacement - subjects are enumerated once per each search-replacement pair;
            // this order of loops enables to reuse instances of SubstringReplacer:
            //IEnumerator replacements_enum = replacements.GetEnumerator();
            foreach (object s in searches)
            {
                string search_str = Core.Convert.ObjectToString(s);
                string replacement_str = (replacements_enum.MoveNext()) ? Core.Convert.ObjectToString(replacements_enum.Current) : string.Empty;

                // skips empty strings:
                if (search_str != string.Empty)
                {
                    SubstringReplacer replacer = new SubstringReplacer(search_str, replacement_str, ignoreCase);

                    for (int i = 0; i < subjects.Length; i++)
                    {
                        // subjects has already been converted to CLR strings:
                        string subject_str = subjects[i].Value as string;
                        if (subject_str != null)
                        {
                            subjects[i].Value = replacer.Replace(subject_str, ref count);
                        }
                    }
                }
            }

            // copy into PhpArray
            return ToPhpArray(ref subjects);
        }

        /// <summary>
        /// Convert array of DictionaryEntry into PhpArray.
        /// </summary>
        /// <param name="subjects"></param>
        /// <returns></returns>
        internal static PhpArray ToPhpArray(ref DictionaryEntry[]/*!*/subjects)
        {
            Debug.Assert(subjects != null);

            var result = new PhpArray(subjects.Length);
            foreach (var entry in subjects)
                result.Add(entry.Key, entry.Value);
            return result;
        }

        ///// <summary>
        ///// Returns first item from values of given collection converted to string or empty string.
        ///// </summary>
        ///// <param name="dict"></param>
        ///// <returns></returns>
        //internal static string FirstOrEmpty(IDictionary/*!*/dict)
        //{
        //    if (dict.Count > 0)
        //    {
        //        var dict_enum = dict.Values.GetEnumerator();

        //        if (dict_enum.MoveNext())
        //            return Core.Convert.ObjectToString(dict_enum.Current);
        //    }

        //    return string.Empty;
        //}


        /// <summary>
        /// Implements <c>str_replace</c> and <c>str_ireplace</c> functions.
        /// </summary>
        internal static object ReplaceInternal(object search, object replace, object subject, bool ignoreCase, ref int count)
        {
            if (subject == null)
                return null;

            IDictionary searches = search as IDictionary;
            IDictionary replacements = replace as IDictionary;
            IDictionary subjects = subject as IDictionary;

            //
            // several cases of search/replace/subject combinations
            //

            if (subjects == null)
            {
                // string str_replace(..., ... , {string}, ...)
                string subject_str = Core.Convert.ObjectToString(subject);
                if (subject_str == string.Empty)
                    return string.Empty;

                if (searches == null)
                {
                    string search_str = Core.Convert.ObjectToString(search);
                    if (search_str == string.Empty)
                        return subject_str;

                    ////
                    //if (replacements == null)// str_replace({string},{string},{string},...);
                    //    return ReplaceInternal(search_str, Core.Convert.ObjectToString(replace), subject_str, ignoreCase, ref count);
                    //else// str_replace({string},{array}[0],{string},...);
                    //    return ReplaceInternal(search_str, FirstOrEmpty(replacements), subject_str, ignoreCase, ref count);
                    return ReplaceInternal(search_str, Core.Convert.ObjectToString(replace), subject_str, ignoreCase, ref count);
                }
                else
                {
                    if (replacements == null)// str_replace({array},{string[1]},{string},...);
                        return ReplaceInternal(searches.Values, new InifiniteEnumerator(Core.Convert.ObjectToString(replace)), subject_str, ignoreCase, ref count);
                    else// str_replace({array},{array},{string},...);
                        return ReplaceInternal(searches.Values, replacements.Values.GetEnumerator(), subject_str, ignoreCase, ref count);
                }
            }
            else
            {
                // converts scalars (and nulls) to strings:
                var subjectEntries = new DictionaryEntry[subjects.Count];
                int i = 0;
                foreach (DictionaryEntry entry in subjects)
                {
                    subjectEntries[i] = entry;

                    if (PhpVariable.IsScalar(entry.Value))
                        subjectEntries[i].Value = Core.Convert.ObjectToString(entry.Value);
                    else if (entry.Value == null)
                        subjectEntries[i].Value = string.Empty;

                    i++;
                }

                // PhpArray str_replace(..., ... , {array}, ...)
                if (searches == null)
                {
                    string search_str = Core.Convert.ObjectToString(search);
                    if (search_str == string.Empty)
                        return ToPhpArray(ref subjectEntries);

                    ////
                    //if (replacements == null)// str_replace({string},{string},{array},...);
                    //    return ReplaceInternal(search_str, Core.Convert.ObjectToString(replace), ref subjectEntries, ignoreCase, ref count);
                    //else// str_replace({string},{array}[0],{array},...);
                    //    return ReplaceInternal(search_str, FirstOrEmpty(replacements), ref subjectEntries, ignoreCase, ref count);
                    return ReplaceInternal(search_str, Core.Convert.ObjectToString(replace), ref subjectEntries, ignoreCase, ref count);
                }
                else
                {
                    if (replacements == null)// str_replace({array},{string[1]},{array},...);
                        return ReplaceInternal(searches.Values, new InifiniteEnumerator(Core.Convert.ObjectToString(replace)), ref subjectEntries, ignoreCase, ref count);
                    else// str_replace({array},{array},{array},...);
                        return ReplaceInternal(searches.Values, replacements.Values.GetEnumerator(), ref subjectEntries, ignoreCase, ref count);
                }
            }

            /*
            
            //
            // previous (common) implementation:
            //
            
			// assembles a dictionary of subject strings:
			bool return_array;
            DictionaryEntry[] subjects;
            
            IDictionary dict;
            if ((dict = subject as IDictionary) != null)
			{
				subjects = new DictionaryEntry[dict.Count];

				// converts scalars to strings:
				int i = 0;
				foreach (DictionaryEntry entry in dict)
				{
					subjects[i] = entry;

					if (PhpVariable.IsScalar(entry.Value))
						subjects[i].Value = Core.Convert.ObjectToString(entry.Value);

					i++;
				}
				return_array = true;
			}
			else
			{
				subjects = new DictionaryEntry[] { new DictionaryEntry(string.Empty, Core.Convert.ObjectToString(subject)) };
				return_array = false;
			}

			// performs replacement - subjects are enumerated once per each search-replacement pair;
			// this order of loops enables to reuse instances of SubstringReplacer:
			IEnumerator replacements_enum = replacements.GetEnumerator();
			foreach (object s in searches)
			{
				string search_str = Core.Convert.ObjectToString(s);
				string replacement_str = (replacements_enum.MoveNext()) ? Core.Convert.ObjectToString(replacements_enum.Current) : string.Empty;

                ReplaceInplace(search_str, replacement_str, ref subjects, ignoreCase, ref count);
			}

			// constructs resulting array or single item from subjects:
			if (return_array)
			{
				PhpArray result = new PhpArray();

				foreach (DictionaryEntry entry in subjects)
					result.Add(entry.Key, entry.Value);

				return result;
			}
			else
			{
				return subjects[0].Value;
			}*/
        }

        #endregion

        /// <summary>
        /// Replaces all occurrences of the <paramref name="searched"/> string 
        /// with the <paramref name="replacement"/> string counting the number of occurrences.
        /// </summary>
        /// <param name="searched">
        /// The substring(s) to replace. Can be string or <see cref="IDictionary"/> of strings.
        /// </param>
        /// <param name="replacement">
        /// The string(s) to replace <paramref name="searched"/>. Can be string or <see cref="IDictionary"/> of strings.
        /// </param>
        /// <param name="subject">
        /// The string or <see cref="IDictionary"/> of strings to perform the search and replace with.
        /// </param>
        /// <param name="count">
        /// The number of matched and replaced occurrences.
        /// </param>
        /// <returns>
        /// A string or an <see cref="IDictionary"/> with all occurrences of 
        /// <paramref name="searched"/> in <paramref name="subject"/> replaced
        /// with the given <paramref name="replacement"/> value.
        /// </returns>
        [ImplementsFunction("str_replace")]
        [PureFunction]
        public static object Replace(object searched, object replacement, object subject, out int count)
        {
            count = 0;
            return ReplaceInternal(searched, replacement, subject, false, ref count);
        }

        /// <summary>
        /// Replaces all occurrences of the <paramref name="searched"/> string 
        /// with the <paramref name="replacement"/> string.
        /// <seealso cref="Replace(object,object,object,out int)"/>
        /// </summary>
        [ImplementsFunction("str_replace")]
        [PureFunction]
        public static object Replace(object searched, object replacement, object subject)
        {
            int count = -1;
            return ReplaceInternal(searched, replacement, subject, false, ref count);
        }

        /// <summary>
        /// Case insensitive version of <see cref="Replace(object,object,object,out int)"/>.
        /// </summary>
        [ImplementsFunction("str_ireplace")]
        [PureFunction]
        public static object ReplaceInsensitively(object searched, object replacement, object subject, out int count)
        {
            count = 0;
            return ReplaceInternal(searched, replacement, subject, true, ref count);
        }

        /// <summary>
        /// Case insensitive version of <see cref="Replace(object,object,object)"/>.
        /// </summary>
        [ImplementsFunction("str_ireplace")]
        [PureFunction]
        public static object ReplaceInsensitively(object searched, object replacement, object subject)
        {
            int count = -1;
            return ReplaceInternal(searched, replacement, subject, true, ref count);
        }

        #endregion


        #region str_shuffle, str_split

        /// <summary>
        /// Randomly shuffles a string.
        /// </summary>
        /// <param name="str">The string to shuffle.</param>
        /// <returns>One random permutation of <paramref name="str"/>.</returns>
        [ImplementsFunction("str_shuffle")]
        public static string Shuffle(string str)
        {
            if (str == null) return String.Empty;

            Random generator = PhpMath.Generator;

            int count = str.Length;
            if (count <= 1) return str;

            StringBuilder newstr = new StringBuilder(str);

            // Takes n-th character from the string at random with probability 1/i
            // and exchanges it with the one on the i-th position.
            // Thus a random permutation is formed in the second part of the string (from i to count)
            // and the set of remaining characters is stored in the first part.
            for (int i = count - 1; i > 0; i--)
            {
                int n = generator.Next(i + 1);

                char ch = newstr[i];
                newstr[i] = newstr[n];
                newstr[n] = ch;
            }

            return newstr.ToString();
        }

        /// <summary>
        /// Converts a string to an array.
        /// </summary>
        /// <param name="str">The string to split.</param>
        /// <returns>An array with keys being character indeces and values being characters.</returns>
        [ImplementsFunction("str_split")]
        [return: CastToFalse]
        public static PhpArray Split(string str)
        {
            return Split(str, 1);
        }

        /// <summary>
        /// Converts a string to an array.
        /// </summary>
        /// <param name="obj">The string to split.</param>
        /// <param name="splitLength">Length of chunks <paramref name="obj"/> should be split into.</param>
        /// <returns>An array with keys being chunk indeces and values being chunks of <paramref name="splitLength"/>
        /// length.</returns>
        /// <exception cref="PhpException">The <paramref name="splitLength"/> parameter is not positive (Warning).</exception>
        [ImplementsFunction("str_split")]
        [return: CastToFalse]
        public static PhpArray Split(object obj, int splitLength)
        {
            if (splitLength < 1)
            {
                PhpException.Throw(PhpError.Warning, LibResources.GetString("segment_length_not_positive"));
                return null;
            }
            if (obj == null)
                return new PhpArray();

            PhpBytes bytes;
            if ((bytes = obj as PhpBytes) != null)
            {
                int length = bytes.Length;
                PhpArray result = new PhpArray(length / splitLength + 1, 0);

                // add items of length splitLength
                int i;
                for (i = 0; i < (length - splitLength + 1); i += splitLength)
                {
                    byte[] chunk = new byte[splitLength];
                    Array.Copy(bytes.ReadonlyData, i, chunk, 0, chunk.Length);
                    result.Add(new PhpBytes(chunk));
                }

                // add the last item
                if (i < length)
                {
                    byte[] chunk = new byte[length - i];
                    Array.Copy(bytes.ReadonlyData, i, chunk, 0, chunk.Length);
                    result.Add(new PhpBytes(chunk));
                }

                return result;
            }
            else
            {
                return Split(PHP.Core.Convert.ObjectToString(obj), splitLength);
            }
        }

        private static PhpArray Split(string str, int splitLength)
        {
            int length = str.Length;
            PhpArray result = new PhpArray(length / splitLength + 1, 0);

            // add items of length splitLength
            int i;
            for (i = 0; i < (length - splitLength + 1); i += splitLength)
            {
                result.Add(str.Substring(i, splitLength));
            }

            // add the last item
            if (i < length) result.Add(str.Substring(i));

            return result;
        }

        #endregion


        #region quoted_printable_decode, quoted_printable_encode

        /// <summary>
        /// Maximum length of line according to quoted-printable specification.
        /// </summary>
        internal const int PHP_QPRINT_MAXL = 75;

        /// <summary>
        /// Converts a quoted-printable string into (an 8-bit) string.
        /// </summary>
        /// <param name="str">The quoted-printable string.</param>
        /// <returns>The 8-bit string corresponding to the decoded <paramref name="str"/>.</returns>
        /// <remarks>Based on the implementation in quot_print.c PHP source file.</remarks>
        [ImplementsFunction("quoted_printable_decode")]
        public static string QuotedPrintableDecode(string str)
        {
            if (str == null) return String.Empty;

            Encoding encoding = Configuration.Application.Globalization.PageEncoding;
            MemoryStream stream = new MemoryStream();
            StringBuilder result = new StringBuilder(str.Length / 2);

            int i = 0;
            while (i < str.Length)
            {
                char c = str[i];

                if (c == '=')
                {
                    if (i + 2 < str.Length && Uri.IsHexDigit(str[i + 1]) && Uri.IsHexDigit(str[i + 2]))
                    {
                        stream.WriteByte((byte)((Uri.FromHex(str[i + 1]) << 4) + Uri.FromHex(str[i + 2])));
                        i += 3;
                    }
                    else  // check for soft line break according to RFC 2045
                    {
                        int k = 1;

                        // Possibly, skip spaces/tabs at the end of line
                        while (i + k < str.Length && (str[i + k] == ' ' || str[i + k] == '\t')) k++;

                        // End of line reached
                        if (i + k >= str.Length)
                        {
                            i += k;
                        }
                        else if (str[i + k] == '\r' && i + k + 1 < str.Length && str[i + k + 1] == '\n')
                        {
                            // CRLF
                            i += k + 2;
                        }
                        else if (str[i + k] == '\r' || str[i + k] == '\n')
                        {
                            // CR or LF
                            i += k + 1;
                        }
                        else
                        {
                            // flush stream
                            if (stream.Position > 0)
                            {
                                result.Append(encoding.GetChars(stream.GetBuffer(), 0, (int)stream.Position));
                                stream.Seek(0, SeekOrigin.Begin);
                            }
                            result.Append(str[i++]);
                        }
                    }
                }
                else
                {
                    // flush stream
                    if (stream.Position > 0)
                    {
                        result.Append(encoding.GetChars(stream.GetBuffer(), 0, (int)stream.Position));
                        stream.Seek(0, SeekOrigin.Begin);
                    }

                    result.Append(c);
                    i++;
                }
            }

            // flush stream
            if (stream.Position > 0)
            {
                result.Append(encoding.GetChars(stream.GetBuffer(), 0, (int)stream.Position));
                stream.Seek(0, SeekOrigin.Begin);
            }

            return result.ToString();
        }

        /// <summary>
        /// Convert a 8 bit string to a quoted-printable string
        /// </summary>
        /// <param name="str">The input string.</param>
        /// <returns>The quoted-printable string.</returns>
        /// <remarks>Based on the implementation in quot_print.c PHP source file.</remarks>
        [ImplementsFunction("quoted_printable_encode")]
        public static string QuotedPrintableEncode(string str)
        {
            if (str == null) return String.Empty;

            Encoding encoding = Configuration.Application.Globalization.PageEncoding;
            MemoryStream stream = new MemoryStream();

            StringBuilder result = new StringBuilder(3 * str.Length + 3 * (((3 * str.Length) / PHP_QPRINT_MAXL) + 1));
            string hex = "0123456789ABCDEF";

            byte[] bytes = new byte[encoding.GetMaxByteCount(1)];
            int encodedChars;


            int i = 0;
            int j = 0;
            int charsOnLine = 0;
            char c;
            while (i < str.Length)
            {
                c = str[i];

                if (c == '\r' && i + 1 < str.Length && str[i + 1] == '\n')
                {
                    result.Append("\r\n");
                    charsOnLine = 0;
                    i += 2;
                }
                else
                {
                    
                    if (char.IsControl(c) ||
                        c >= 0x7F || // is not ascii char
                        (c == '=') ||
                        ((c == ' ') && i + 1 < str.Length && (str[i + 1] == '\r')))
                    {

                        if ((charsOnLine += 3) > PHP_QPRINT_MAXL)
                        {
                            result.Append("=\r\n");
                            charsOnLine = 3;
                        }

                        // encode c(==str[i])
                        encodedChars = encoding.GetBytes(str, i, 1, bytes, 0);

                        for (j = 0; j < encodedChars; ++j)
                        {
                            result.Append('=');
                            result.Append(hex[bytes[j] >> 4]);
                            result.Append(hex[bytes[j] & 0xf]);
                        }
                    }
                    else
                    {

                        if ((++charsOnLine) > PHP_QPRINT_MAXL)
                        {
                            result.Append("=\r\n");
                            charsOnLine = 1;
                        }
                        result.Append(c);
                    }

                    ++i;
                }
            }
            return result.ToString();
        }

        #endregion


        #region addslashes, addcslashes, quotemeta

        /// <summary>
        /// Adds backslashes before characters depending on current configuration.
        /// </summary>
        /// <param name="str">Data to process.</param>
        /// <returns>
        /// The string or string of bytes where some characters are preceded with the backslash character.
        /// </returns>
        /// <remarks>
        /// If <see cref="LocalConfiguration.VariablesSection.QuoteInDbManner"/> ("magic_quotes_sybase" in PHP) option 
        /// is set then '\0' characters are slashed and single quotes are replaced with two single quotes. Otherwise,
        /// '\'', '"', '\\' and '\0 characters are slashed.
        /// </remarks>
        [ImplementsFunction("addslashes")]
        public static string AddSlashes(string str)
        {
            ScriptContext context = ScriptContext.CurrentContext;

            if (context.Config.Variables.QuoteInDbManner)
                return StringUtils.AddDbSlashes(str);
            else
                return StringUtils.AddCSlashes(str, true, true);
        }

        /// <include file='Doc/Strings.xml' path='docs/method[@name="AddCSlashes"]/*'/>
        /// <exception cref="PhpException">Thrown if <paramref name="str"/> interval is invalid.</exception>
        [ImplementsFunction("addcslashes_unicode")]
        public static string AddCSlashes(string str, string mask)
        {
            if (str == null) return String.Empty;
            if (mask == null) return str;

            return AddCSlashesInternal(str, str, mask);
        }

        /// <include file='Doc/Strings.xml' path='docs/method[@name="AddCSlashes"]/*'/>
        /// <exception cref="PhpException">Thrown if <paramref name="str"/> interval is invalid.</exception>
        [ImplementsFunction("addcslashes")]
        public static string AddCSlashesAscii(string str, string mask)
        {
            if (string.IsNullOrEmpty(str)) return String.Empty;
            if (string.IsNullOrEmpty(mask)) return str;

            //Encoding encoding = Configuration.Application.Globalization.PageEncoding;

            //// to guarantee the same result both the string and the mask has to be converted to bytes:
            //string c = ArrayUtils.ToString(encoding.GetBytes(mask));
            //string s = ArrayUtils.ToString(encoding.GetBytes(str));

            string c = Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(mask));
            string s = Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(str));

            // the result contains ASCII characters only, so there is no need to conversions:
            return AddCSlashesInternal(str, s, c);
        }

        /// <param name="translatedStr">A sequence of chars or ints from which to take character codes.</param>
        /// <param name="translatedMask">A mask containing codes.</param>
        /// <param name="str">A string to be slashed.</param>
        /// <exception cref="PhpException"><paramref name="translatedStr"/> interval is invalid.</exception>
        /// <exception cref="PhpException"><paramref name="translatedStr"/> contains Unicode characters greater than '\u0800'.</exception>
        internal static string AddCSlashesInternal(string str, string translatedStr, string translatedMask)
        {
            Debug.Assert(str != null && translatedMask != null && translatedStr != null && str.Length == translatedStr.Length);

            // prepares the mask:
            CharMap charmap = InitializeCharMap();
            try
            {
                charmap.AddUsingMask(translatedMask);
            }
            catch (IndexOutOfRangeException)
            {
                PhpException.Throw(PhpError.Warning, LibResources.GetString("too_big_unicode_character"));
                return null;
            }

            const string cslashed_chars = "abtnvfr";

            StringBuilder result = new StringBuilder();
            for (int i = 0; i < str.Length; i++)
            {
                //char c = translatedStr[i];

                if (charmap.Contains(translatedStr[i]))
                {
                    result.Append('\\');

                    char c = str[i];    // J: translatedStr and translatedMask are used only in context of CharMap, later we are working with original str only

                    // performs conversion to C representation:
                    if (c < '\u0020' || c > '\u007f')
                    {
                        if (c >= '\u0007' && c <= '\u000d')
                            result.Append(cslashed_chars[c - '\u0007']);
                        else
                            result.Append(System.Convert.ToString((int)c, 8));  // 0x01234567
                    }
                    else
                        result.Append(c);
                }
                else
                    result.Append(str[i]);
            }

            return result.ToString();
        }

        /// <summary>
        /// A map of following characters: {'.', '\', '+', '*', '?', '[', '^', ']', '(', '$', ')'}.
        /// </summary>
        internal static readonly CharMap metaCharactersMap = new CharMap(new uint[] { 0, 0x08f20001, 0x0000001e });

        /// <summary>
        /// Adds backslashes before following characters: {'.', '\', '+', '*', '?', '[', '^', ']', '(', '$', ')'}
        /// </summary>
        /// <param name="str">The string to be processed.</param>
        /// <returns>The string where said characters are backslashed.</returns>
        [ImplementsFunction("quotemeta")]
        public static string QuoteMeta(string str)
        {
            if (str == null) return String.Empty;

            int length = str.Length;
            StringBuilder result = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                char c = str[i];
                if (metaCharactersMap.Contains(c)) result.Append('\\');
                result.Append(c);
            }

            return result.ToString();
        }

        #endregion


        #region stripslashes, stripcslashes

        /// <summary>
        /// Unquote string quoted with <see cref="AddSlashes"/>.
        /// </summary>
        /// <param name="str">The string to unquote.</param>
        /// <returns>The unquoted string.</returns>
        [ImplementsFunction("stripslashes")]
        public static string StripSlashes(string str)
        {
            ScriptContext context = ScriptContext.CurrentContext;

            if (context.Config.Variables.QuoteInDbManner)
                return StringUtils.StripDbSlashes(str);
            else
                return StringUtils.StripCSlashes(str);
        }

        /// <summary>
        /// Returns a string with backslashes stripped off. Recognizes \a, \b, \f, \n, \r, \t, \v, \\, octal
        /// and hexadecimal representation.
        /// </summary>
        /// <param name="str">The string to strip.</param>
        /// <returns>The stripped string.</returns>
        [ImplementsFunction("stripcslashes")]
        public static string StripCSlashesAscii(string str)
        {
            if (str == null) return String.Empty;

            Encoding encoding = Configuration.Application.Globalization.PageEncoding;
            const char escape = '\\';
            int length = str.Length;
            StringBuilder result = new StringBuilder(length);
            bool state1 = false;
            byte[] bA1 = new byte[1];

            for (int i = 0; i < length; i++)
            {
                char c = str[i];
                if (c == escape && state1 == false)
                {
                    state1 = true;
                    continue;
                }

                if (state1 == true)
                {
                    switch (c)
                    {
                        case 'a': result.Append('\a'); break;
                        case 'b': result.Append('\b'); break;
                        case 'f': result.Append('\f'); break;
                        case 'n': result.Append('\n'); break;
                        case 'r': result.Append('\r'); break;
                        case 't': result.Append('\t'); break;
                        case 'v': result.Append('\v'); break;
                        case '\\': result.Append('\\'); break;

                        // hex ASCII code
                        case 'x':
                            {
                                int code = 0;
                                if (i + 1 < length && Uri.IsHexDigit(str[i + 1])) // first digit
                                {
                                    code = Uri.FromHex(str[i + 1]);
                                    i++;
                                    if (i + 1 < length && Uri.IsHexDigit(str[i + 1])) // second digit
                                    {
                                        code = (code << 4) + Uri.FromHex(str[i + 1]);
                                        i++;
                                    }

                                    bA1[0] = (byte)code;
                                    result.Append(encoding.GetChars(bA1)[0]);
                                    break;
                                }
                                goto default;
                            }

                        // octal ASCII code
                        default:
                            {
                                int code = 0, j = 0;
                                for (; j < 3 && i < length && str[i] >= '0' && str[i] <= '8'; i++, j++)
                                {
                                    code = (code << 3) + (str[i] - '0');
                                }

                                if (j > 0)
                                {
                                    i--;
                                    bA1[0] = (byte)code;
                                    result.Append(encoding.GetChars(bA1)[0]);
                                }
                                else result.Append(c);
                                break;
                            }
                    }

                    state1 = false;
                }
                else result.Append(c);
            }

            return result.ToString();
        }

        #endregion


        #region htmlspecialchars, htmlspecialchars_decode

        /// <summary>
        /// Converts special characters to HTML entities.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <returns>The converted string.</returns>
        [ImplementsFunction("htmlspecialchars")]
        public static string HtmlSpecialCharsEncode(string str)
        {
            return HtmlSpecialCharsEncode(str, 0, str.Length, QuoteStyle.Compatible, "ISO-8859-1");
        }

        /// <summary>
        /// Converts special characters to HTML entities.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <param name="quoteStyle">Quote conversion.</param>
        /// <returns>The converted string.</returns>
        [ImplementsFunction("htmlspecialchars")]
        public static string HtmlSpecialCharsEncode(string str, QuoteStyle quoteStyle)
        {
            return HtmlSpecialCharsEncode(str, 0, str.Length, quoteStyle, "ISO-8859-1");
        }

        /// <summary>
        /// Converts special characters to HTML entities.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <param name="quoteStyle">Quote conversion.</param>
        /// <param name="charSet">The character set used in conversion. This parameter is ignored.</param>
        /// <returns>The converted string.</returns>
        [ImplementsFunction("htmlspecialchars")]
        public static string HtmlSpecialCharsEncode(string str, QuoteStyle quoteStyle, string charSet)
        {
            return HtmlSpecialCharsEncode(str, 0, str.Length, quoteStyle, charSet);
        }

        /// <summary>
        /// Converts special characters to HTML entities.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <param name="quoteStyle">Quote conversion.</param>
        /// <param name="charSet">The character set used in conversion. This parameter is ignored.</param>
        /// <param name="doubleEncode">When double_encode is turned off PHP will not encode existing html entities, the default is to convert everything.</param>
        /// <returns>The converted string.</returns>
        [ImplementsFunction("htmlspecialchars")]
        public static string HtmlSpecialCharsEncode(string str, QuoteStyle quoteStyle, string charSet, bool doubleEncode /* = true */)
        {
            if (!doubleEncode)
                PhpException.ArgumentValueNotSupported("doubleEncode", doubleEncode); // TODO: is doubleEncode is false

            return HtmlSpecialCharsEncode(str, 0, str.Length, quoteStyle, charSet);
        }

        /// <summary>
        /// Converts special characters of substring to HTML entities.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="index">First character of the string to covert.</param>
        /// <param name="length">Length of the substring to covert.</param>
        /// <returns>The converted substring.</returns>
        internal static string HtmlSpecialChars(string str, int index, int length)
        {
            return HtmlSpecialCharsEncode(str, index, length, QuoteStyle.Compatible, "ISO-8859-1");
        }

        /// <summary>
        /// Converts special characters of substring to HTML entities.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="index">First character of the string to covert.</param>
        /// <param name="length">Length of the substring to covert.</param>
        /// <param name="quoteStyle">Quote conversion.</param>
        /// <param name="charSet">The character set used in conversion. This parameter is ignored.</param>
        /// <returns>The converted substring.</returns>
        internal static string HtmlSpecialCharsEncode(string str, int index, int length, QuoteStyle quoteStyle, string charSet)
        {
            if (str == null) return String.Empty;

            Debug.Assert(index + length <= str.Length);

            StringBuilder result = new StringBuilder(length);

            // quote style is anded to emulate PHP behavior (any value is allowed):
            string single_quote = (quoteStyle & QuoteStyle.SingleQuotes) != 0 ? "&#039;" : "'";
            string double_quote = (quoteStyle & QuoteStyle.DoubleQuotes) != 0 ? "&quot;" : "\"";

            for (int i = index; i < index + length; i++)
            {
                char c = str[i];
                switch (c)
                {
                    case '&': result.Append("&amp;"); break;
                    case '"': result.Append(double_quote); break;
                    case '\'': result.Append(single_quote); break;
                    case '<': result.Append("&lt;"); break;
                    case '>': result.Append("&gt;"); break;
                    default: result.Append(c); break;
                }
            }

            return result.ToString();
        }


        /// <summary>
        /// Converts HTML entities (&amp;amp;, &amp;quot;, &amp;lt;, and &amp;gt;) 
        /// in a specified string to the respective characters. 
        /// </summary>
        /// <param name="str">The string to be converted.</param>
        /// <returns>String with converted entities.</returns>
        [ImplementsFunction("htmlspecialchars_decode")]
        public static string HtmlSpecialCharsDecode(string str)
        {
            return HtmlSpecialCharsDecode(str, QuoteStyle.Compatible);
        }

        /// <summary>
        /// Converts HTML entities (&amp;amp;, &amp;lt;, &amp;gt;, and optionally &amp;quot; and &amp;#039;) 
        /// in a specified string to the respective characters. 
        /// </summary>
        /// <param name="str">The string to be converted.</param>
        /// <param name="quoteStyle">Which quote entities to convert.</param>
        /// <returns>String with converted entities.</returns>
        [ImplementsFunction("htmlspecialchars_decode")]
        public static string HtmlSpecialCharsDecode(string str, QuoteStyle quoteStyle)
        {
            if (str == null) return null;

            StringBuilder result = new StringBuilder(str.Length);

            bool dq = (quoteStyle & QuoteStyle.DoubleQuotes) != 0;
            bool sq = (quoteStyle & QuoteStyle.SingleQuotes) != 0;

            int i = 0;
            while (i < str.Length)
            {
                char c = str[i];
                if (c == '&')
                {
                    i++;
                    if (i + 4 < str.Length && str[i + 4] == ';')                   // quot; #039;
                    {
                        if (dq && str[i] == 'q' && str[i + 1] == 'u' && str[i + 2] == 'o' && str[i + 3] == 't') { i += 5; result.Append('"'); continue; }
                        if (sq && str[i] == '#' && str[i + 1] == '0' && str[i + 2] == '3' && str[i + 3] == '9') { i += 5; result.Append('\''); continue; }
                    }

                    if (i + 3 < str.Length && str[i + 3] == ';')                   // amp; #39;
                    {
                        if (str[i] == 'a' && str[i + 1] == 'm' && str[i + 2] == 'p') { i += 4; result.Append('&'); continue; }
                        if (sq && str[i] == '#' && str[i + 1] == '3' && str[i + 2] == '9') { i += 4; result.Append('\''); continue; }
                    }

                    if (i + 2 < str.Length && str[i + 2] == ';' && str[i + 1] == 't')  // lt; gt;
                    {
                        if (str[i] == 'l') { i += 3; result.Append('<'); continue; }
                        if (str[i] == 'g') { i += 3; result.Append('>'); continue; }
                    }
                }
                else
                {
                    i++;
                }

                result.Append(c);
            }

            return result.ToString();
        }

        #endregion


        #region htmlentities, get_html_translation_table, html_entity_decode

        /// <summary>
        /// Default <c>encoding</c> used in <c>htmlentities</c>.
        /// </summary>
        private const string DefaultHtmlEntitiesCharset = "UTF-8";

        /// <summary>
        /// Converts special characters to HTML entities.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <returns>The converted string.</returns>
        /// <remarks>This method is identical to <see cref="HtmlSpecialChars"/> in all ways, except with
        /// <b>htmlentities</b> (<see cref="EncodeHtmlEntities"/>), all characters that have HTML character entity equivalents are
        /// translated into these entities.</remarks>
        [ImplementsFunction("htmlentities")]
        public static string EncodeHtmlEntities(object str)
        {
            return EncodeHtmlEntities(str, QuoteStyle.HtmlEntitiesDefault, DefaultHtmlEntitiesCharset, true);
        }

        /// <summary>
        /// Converts special characters to HTML entities.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <param name="quoteStyle">Quote conversion.</param>
        /// <returns>The converted string.</returns>
        /// <remarks>This method is identical to <see cref="HtmlSpecialChars"/> in all ways, except with
        /// <b>htmlentities</b> (<see cref="EncodeHtmlEntities"/>), all characters that have HTML character entity equivalents are
        /// translated into these entities.</remarks>
        [ImplementsFunction("htmlentities")]
        public static string EncodeHtmlEntities(object str, QuoteStyle quoteStyle)
        {
            return EncodeHtmlEntities(str, quoteStyle, DefaultHtmlEntitiesCharset, true);
        }

        /// <summary>
        /// Converts special characters to HTML entities.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <param name="quoteStyle">Quote conversion.</param>
        /// <param name="charSet">The character set used in conversion.</param>
        /// <returns>The converted string.</returns>
        /// <remarks>This method is identical to <see cref="HtmlSpecialChars"/> in all ways, except with
        /// <b>htmlentities</b> (<see cref="EncodeHtmlEntities"/>), all characters that have HTML character entity equivalents are
        /// translated into these entities.</remarks>
        [ImplementsFunction("htmlentities")]
        public static string EncodeHtmlEntities(object str, QuoteStyle quoteStyle, string charSet)
        {
            return EncodeHtmlEntities(str, quoteStyle, charSet, true);
        }

        /// <summary>
        /// Converts special characters to HTML entities.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <param name="quoteStyle">Quote conversion.</param>
        /// <param name="charSet">The character set used in conversion. This parameter is ignored.</param>
        /// <param name="doubleEncode">When it is turned off existing HTML entities will not be encoded. The default is to convert everything.</param>
        /// <returns>The converted string.</returns>
        /// <remarks>This method is identical to <see cref="HtmlSpecialChars"/> in all ways, except with
        /// <b>htmlentities</b> (<see cref="EncodeHtmlEntities"/>), all characters that have HTML character entity equivalents are
        /// translated into these entities.</remarks>
        [ImplementsFunction("htmlentities")]
        public static string EncodeHtmlEntities(object str, QuoteStyle quoteStyle, string charSet, bool doubleEncode)
        {
            try
            {
                var s = ObjectToString(str, charSet);
                return EncodeHtmlEntities(s, quoteStyle, doubleEncode);
            }
            catch (ArgumentException ex)
            {
                PhpException.Throw(PhpError.Warning, ex.Message);
                return string.Empty;
            }
        }

        private static string EncodeHtmlEntities(string str, QuoteStyle quoteStyle, bool doubleEncode)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;

            if (!doubleEncode)
            {   // existing HTML entities will not be double encoded // TODO: do it nicely
                str = DecodeHtmlEntities(str, quoteStyle);
            }

            // if only double quotes should be encoded, we can use HttpUtility.HtmlEncode right away:
            if ((quoteStyle & QuoteStyle.BothQuotes) == QuoteStyle.DoubleQuotes)
            {
                return HttpUtility.HtmlEncode(str);
            }

            // quote style is anded to emulate PHP behavior (any value is allowed):
            string single_quote = (quoteStyle & QuoteStyle.SingleQuotes) != 0 ? "&#039;" : "'";
            string double_quote = (quoteStyle & QuoteStyle.DoubleQuotes) != 0 ? "&quot;" : "\"";

            StringBuilder str_builder = new StringBuilder(str.Length);
            StringWriter result = new StringWriter(str_builder);

            // convert ' and " manually, rely on HttpUtility.HtmlEncode for everything else
            char[] quotes = new char[] { '\'', '\"' };
            int old_index = 0, index = 0;
            while (index < str.Length && (index = str.IndexOfAny(quotes, index)) >= 0)
            {
                result.Write(HttpUtility.HtmlEncode(str.Substring(old_index, index - old_index)));

                if (str[index] == '\'') result.Write(single_quote);
                else result.Write(double_quote);

                old_index = ++index;
            }
            if (old_index < str.Length) result.Write(HttpUtility.HtmlEncode(str.Substring(old_index)));

            result.Flush();
            return str_builder.ToString();
        }

        /// <summary>
        /// Returns the translation table used by <see cref="HtmlSpecialChars"/> and <see cref="EncodeHtmlEntities"/>. 
        /// </summary>
        /// <param name="table">Type of the table that should be returned.</param>
        /// <returns>The table.</returns>
        [ImplementsFunction("get_html_translation_table")]
        public static PhpArray GetHtmlTranslationTable(HtmlEntitiesTable table)
        {
            return GetHtmlTranslationTable(table, QuoteStyle.Compatible);
        }

        /// <summary>
        /// Returns the translation table used by <see cref="HtmlSpecialChars"/> and <see cref="EncodeHtmlEntities"/>. 
        /// </summary>
        /// <param name="table">Type of the table that should be returned.</param>
        /// <param name="quoteStyle">Quote conversion.</param>
        /// <returns>The table.</returns>
        [ImplementsFunction("get_html_translation_table")]
        public static PhpArray GetHtmlTranslationTable(HtmlEntitiesTable table, QuoteStyle quoteStyle)
        {
            PhpArray result = new PhpArray();
            if (table == HtmlEntitiesTable.SpecialChars)
            {
                // return the table used with HtmlSpecialChars
                if ((quoteStyle & QuoteStyle.SingleQuotes) != 0) result.Add("\'", "&#039;");
                if ((quoteStyle & QuoteStyle.DoubleQuotes) != 0) result.Add("\"", "&quot;");

                result.Add("&", "&amp;");
                result.Add("<", "&lt;");
                result.Add(">", "&gt;");
            }
            else
            {
                // return the table used with HtmlEntities
                if ((quoteStyle & QuoteStyle.SingleQuotes) != 0) result.Add("\'", "&#039;");
                if ((quoteStyle & QuoteStyle.DoubleQuotes) != 0) result.Add("\"", "&quot;");

                for (char ch = (char)0; ch < 0x100; ch++)
                {
                    if (ch != '\'' && ch != '\"')
                    {
                        string str = ch.ToString();
                        string enc = HttpUtility.HtmlEncode(str);

                        // if the character was encoded:
                        if (str != enc) result.Add(str, enc);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Converts all HTML entities to their applicable characters. 
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <returns>The converted string.</returns>
        [ImplementsFunction("html_entity_decode")]
        public static string DecodeHtmlEntities(object str)
        {
            return DecodeHtmlEntities(str, QuoteStyle.Compatible, DefaultHtmlEntitiesCharset);
        }

        /// <summary>
        /// Converts all HTML entities to their applicable characters.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <param name="quoteStyle">Quote conversion.</param>
        /// <returns>The converted string.</returns>
        [ImplementsFunction("html_entity_decode")]
        public static string DecodeHtmlEntities(object str, QuoteStyle quoteStyle)
        {
            return DecodeHtmlEntities(str, quoteStyle, DefaultHtmlEntitiesCharset);
        }

        /// <summary>
        /// Converts all HTML entities to their applicable characters.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <param name="quoteStyle">Quote conversion.</param>
        /// <param name="charSet">The character set used in conversion.</param>
        /// <returns>The converted string.</returns>
        [ImplementsFunction("html_entity_decode")]
        public static string DecodeHtmlEntities(object str, QuoteStyle quoteStyle, string charSet)
        {
            try
            {
                string s = ObjectToString(str, charSet);
                return DecodeHtmlEntities(s, quoteStyle);
            }
            catch (ArgumentException ex)
            {
                PhpException.Throw(PhpError.Warning, ex.Message);
                return string.Empty;
            }
        }

        private static string DecodeHtmlEntities(string str, QuoteStyle quoteStyle)
        {
            if (str == null) return String.Empty;

            // if both quotes should be decoded, we can use HttpUtility.HtmlDecode right away:
            if ((quoteStyle & QuoteStyle.BothQuotes) == QuoteStyle.BothQuotes)
            {
                return HttpUtility.HtmlDecode(str);
            }

            StringBuilder str_builder = new StringBuilder(str.Length);
            StringWriter result = new StringWriter(str_builder);

            // convert &#039;, &#39; and &quot; manually, rely on HttpUtility.HtmlDecode for everything else
            int old_index = 0, index = 0;
            while (index < str.Length && (index = str.IndexOf('&', index)) >= 0)
            {
                // &quot;
                if ((quoteStyle & QuoteStyle.DoubleQuotes) == 0 && index < str.Length - 5 &&
                    str[index + 1] == 'q' && str[index + 2] == 'u' &&
                    str[index + 3] == 'o' && str[index + 4] == 't' &&
                    str[index + 5] == ';')
                {
                    result.Write(HttpUtility.HtmlDecode(str.Substring(old_index, index - old_index)));
                    result.Write("&quot;");
                    old_index = (index += 6);
                    continue;
                }

                if ((quoteStyle & QuoteStyle.SingleQuotes) == 0)
                {
                    // &#039;
                    if (index < str.Length - 5 && str[index + 1] == '#' &&
                        str[index + 2] == '0' && str[index + 3] == '3' &&
                        str[index + 4] == '9' && str[index + 5] == ';')
                    {
                        result.Write(HttpUtility.HtmlDecode(str.Substring(old_index, index - old_index)));
                        result.Write("&#039;");
                        old_index = (index += 6);
                        continue;
                    }

                    // &#39;
                    if (index < str.Length - 4 && str[index + 1] == '#' &&
                        str[index + 2] == '3' && str[index + 3] == '9' && str[index + 4] == ';')
                    {
                        result.Write(HttpUtility.HtmlDecode(str.Substring(old_index, index - old_index)));
                        result.Write("&#39;");
                        old_index = (index += 5);
                        continue;
                    }
                }

                index++; // for the &
            }
            if (old_index < str.Length) result.Write(HttpUtility.HtmlDecode(str.Substring(old_index)));

            result.Flush();
            return str_builder.ToString();
        }

        #endregion


        #region strip_tags, nl2br

        /// <summary>
        /// Strips HTML and PHP tags from a string.
        /// </summary>
        /// <param name="str">The string to strip tags from.</param>
        /// <returns>The result.</returns>
        [ImplementsFunction("strip_tags")]
        public static string StripTags(string str)
        {
            return StripTags(str, null);
        }

        /// <summary>
        /// Strips HTML and PHP tags from a string.
        /// </summary>
        /// <param name="str">The string to strip tags from.</param>
        /// <param name="allowableTags">Tags which should not be stripped in the following format:
        /// &lt;tag1&gt;&lt;tag2&gt;&lt;tag3&gt;.</param>
        /// <returns>The result.</returns>
        /// <remarks>This is a slightly modified php_strip_tags which can be found in PHP sources.</remarks>
        [ImplementsFunction("strip_tags")]
        public static string StripTags(string str, string allowableTags)
        {
            int state = 0;
            return StripTags(str, allowableTags, ref state);
        }

        /// <summary>
        /// Strips tags allowing to set automaton start state and read its accepting state.
        /// </summary>
        internal static string StripTags(string str, string allowableTags, ref int state)
        {
            if (str == null) return String.Empty;

            int br = 0, i = 0, depth = 0, length = str.Length;
            char lc = '\0';

            // Simple state machine. State 0 is the output state, State 1 means we are inside a
            // normal html tag and state 2 means we are inside a php tag.
            //
            // lc holds the last significant character read and br is a bracket counter.
            // When an allowableTags string is passed in we keep track of the string in
            // state 1 and when the tag is closed check it against the allowableTags string
            // to see if we should allow it.

            StringBuilder result = new StringBuilder(), tagBuf = new StringBuilder();
            if (allowableTags != null) allowableTags = allowableTags.ToLower();

            while (i < length)
            {
                char c = str[i];

                switch (c)
                {
                    case '<':
                        if (i + 1 < length && Char.IsWhiteSpace(str[i + 1])) goto default;
                        if (state == 0)
                        {
                            lc = '<';
                            state = 1;
                            if (allowableTags != null)
                            {
                                tagBuf.Length = 0;
                                tagBuf.Append(c);
                            }
                        }
                        else if (state == 1) depth++;
                        break;

                    case '(':
                        if (state == 2)
                        {
                            if (lc != '"' && lc != '\'')
                            {
                                lc = '(';
                                br++;
                            }
                        }
                        else if (allowableTags != null && state == 1) tagBuf.Append(c);
                        else if (state == 0) result.Append(c);
                        break;

                    case ')':
                        if (state == 2)
                        {
                            if (lc != '"' && lc != '\'')
                            {
                                lc = ')';
                                br--;
                            }
                        }
                        else if (allowableTags != null && state == 1) tagBuf.Append(c);
                        else if (state == 0) result.Append(c);
                        break;

                    case '>':
                        if (depth > 0)
                        {
                            depth--;
                            break;
                        }

                        switch (state)
                        {
                            case 1: /* HTML/XML */
                                lc = '>';
                                state = 0;
                                if (allowableTags != null)
                                {
                                    // find out whether this tag is allowable or not
                                    tagBuf.Append(c);

                                    StringBuilder normalized = new StringBuilder();

                                    bool done = false;
                                    int tagBufLen = tagBuf.Length, substate = 0;

                                    // normalize the tagBuf by removing leading and trailing whitespace and turn
                                    // any <a whatever...> into just <a> and any </tag> into <tag>
                                    for (int j = 0; j < tagBufLen; j++)
                                    {
                                        char d = Char.ToLower(tagBuf[j]);
                                        switch (d)
                                        {
                                            case '<':
                                                normalized.Append(d);
                                                break;

                                            case '>':
                                                done = true;
                                                break;

                                            default:
                                                if (!Char.IsWhiteSpace(d))
                                                {
                                                    if (substate == 0)
                                                    {
                                                        substate = 1;
                                                        if (d != '/') normalized.Append(d);
                                                    }
                                                    else normalized.Append(d);
                                                }
                                                else if (substate == 1) done = true;
                                                break;
                                        }
                                        if (done) break;
                                    }

                                    normalized.Append('>');
                                    if (allowableTags.IndexOf(normalized.ToString()) >= 0) result.Append(tagBuf);

                                    tagBuf.Length = 0;
                                }
                                break;

                            case 2: /* PHP */
                                if (br == 0 && lc != '\"' && i > 0 && str[i] == '?') state = 0;
                                {
                                    state = 0;
                                    tagBuf.Length = 0;
                                }
                                break;

                            case 3:
                                state = 0;
                                tagBuf.Length = 0;
                                break;

                            case 4: /* JavaScript/CSS/etc... */
                                if (i >= 2 && str[i - 1] == '-' && str[i - 2] == '-')
                                {
                                    state = 0;
                                    tagBuf.Length = 0;
                                }
                                break;

                            default:
                                result.Append(c);
                                break;
                        }
                        break;

                    case '"':
                        goto case '\'';

                    case '\'':
                        if (state == 2 && i > 0 && str[i - 1] != '\\')
                        {
                            if (lc == c) lc = '\0';
                            else if (lc != '\\') lc = c;
                        }
                        else if (state == 0) result.Append(c);
                        else if (allowableTags != null && state == 1) tagBuf.Append(c);
                        break;

                    case '!':
                        /* JavaScript & Other HTML scripting languages */
                        if (state == 1 && i > 0 && str[i - 1] == '<')
                        {
                            state = 3;
                            lc = c;
                        }
                        else
                        {
                            if (state == 0) result.Append(c);
                            else if (allowableTags != null && state == 1) tagBuf.Append(c);
                        }
                        break;

                    case '-':
                        if (state == 3 && i >= 2 && str[i - 1] == '-' && str[i - 2] == '!') state = 4;
                        else goto default;
                        break;

                    case '?':
                        if (state == 1 && i > 0 && str[i - 1] == '<')
                        {
                            br = 0;
                            state = 2;
                            break;
                        }
                        goto case 'e';

                    case 'E':
                        goto case 'e';

                    case 'e':
                        /* !DOCTYPE exception */
                        if (state == 3 && i > 6
                            && Char.ToLower(str[i - 1]) == 'p' && Char.ToLower(str[i - 2]) == 'y'
                            && Char.ToLower(str[i - 3]) == 't' && Char.ToLower(str[i - 4]) == 'c'
                            && Char.ToLower(str[i - 5]) == 'o' && Char.ToLower(str[i - 6]) == 'd')
                        {
                            state = 1;
                            break;
                        }
                        goto case 'l';

                    case 'l':

                        /*
                          If we encounter '<?xml' then we shouldn't be in
                          state == 2 (PHP). Switch back to HTML.
                        */

                        if (state == 2 && i > 2 && str[i - 1] == 'm' && str[i - 2] == 'x')
                        {
                            state = 1;
                            break;
                        }
                        goto default;

                    /* fall-through */
                    default:
                        if (state == 0) result.Append(c);
                        else if (allowableTags != null && state == 1) tagBuf.Append(c);
                        break;
                }
                i++;
            }

            return result.ToString();
        }

        /// <summary>
        /// Inserts HTML line breaks before all newlines in a string.
        /// </summary>
        /// <param name="str">The input string.</param>
        /// <returns>The output string.</returns>
        /// <remarks>Inserts "&lt;br/&gt;" before each "\n", "\n\r", "\r", "\r\n".</remarks>
        [ImplementsFunction("nl2br")]
        public static string NewLinesToBreaks(string str)
        {
            return NewLinesToBreaks(str, true);
        }

        /// <summary>
        /// Inserts HTML line breaks before all newlines in a string.
        /// </summary>
        /// <param name="str">The input string.</param>
        /// <param name="isXHTML">Whenever to use XHTML compatible line breaks or not. </param>
        /// <returns>The output string.</returns>
        /// <remarks>Inserts "&lt;br/&gt;" before each "\n", "\n\r", "\r", "\r\n".</remarks>
        [ImplementsFunction("nl2br")]
        public static string NewLinesToBreaks(string str, bool isXHTML/*=true*/ )
        {
            if (string.IsNullOrEmpty(str))
                return String.Empty;

            StringReader reader = new StringReader(str);
            StringWriter writer = new StringWriter(new StringBuilder(str.Length));

            NewLinesToBreaks(reader, writer, isXHTML ? "<br />" : "<br>");

            return writer.ToString();
        }

        public static void NewLinesToBreaks(TextReader/*!*/ input, TextWriter/*!*/ output, string lineBreakString)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            if (output == null)
                throw new ArgumentNullException("output");

            for (; ; )
            {
                int d = input.Read();
                if (d == -1) break;

                char c = (char)d;
                if (c == '\r' || c == '\n')
                {
                    output.Write(lineBreakString);

                    d = input.Peek();
                    if (d != -1)
                    {
                        char c1 = (char)d;
                        if ((c == '\r' && c1 == '\n') || (c == '\n' && c1 == '\r'))
                        {
                            output.Write(c);
                            c = c1;
                            input.Read();
                        }
                    }
                }

                output.Write(c);
            }
        }

        #endregion


        #region chunk_split

        /// <summary>
        /// Splits a string into chunks 76 characters long separated by "\r\n".
        /// </summary>
        /// <param name="str">The string to split.</param>
        /// <returns>The splitted string.</returns>
        /// <remarks>"\r\n" is also appended after the last chunk.</remarks>
        [ImplementsFunction("chunk_split")]
        [return: CastToFalse]
        public static string ChunkSplit(string str)
        {
            return ChunkSplit(str, 76, "\r\n");
        }

        /// <summary>
        /// Splits a string into chunks of a specified length separated by "\r\n".
        /// </summary>
        /// <param name="str">The string to split.</param>
        /// <param name="chunkLength">The chunk length.</param>
        /// <returns>The splitted string.</returns>
        /// <remarks>"\r\n" is also appended after the last chunk.</remarks>
        [ImplementsFunction("chunk_split")]
        [return: CastToFalse]
        public static string ChunkSplit(string str, int chunkLength)
        {
            return ChunkSplit(str, chunkLength, "\r\n");
        }

        /// <summary>
        /// Splits a string into chunks of a specified length separated by a specified string.
        /// </summary>
        /// <param name="str">The string to split.</param>
        /// <param name="chunkLength">The chunk length.</param>
        /// <param name="endOfChunk">The chunk separator.</param>
        /// <returns><paramref name="endOfChunk"/> is also appended after the last chunk.</returns>
        [ImplementsFunction("chunk_split")]
        [return: CastToFalse]
        public static string ChunkSplit(string str, int chunkLength, string endOfChunk)
        {
            if (str == null) return String.Empty;

            if (chunkLength <= 0)
            {
                PhpException.InvalidArgument("chunkLength", LibResources.GetString("arg:negative_or_zero"));
                return null;
            }

            int length = str.Length;
            StringBuilder result = new StringBuilder(length + (length / chunkLength + 1) * endOfChunk.Length);

            // append the chunks one by one to the result
            for (int i = 0, j = length - chunkLength; i < length; i += chunkLength)
            {
                if (i > j) result.Append(str, i, length - i); else result.Append(str, i, chunkLength);
                result.Append(endOfChunk);
            }

            return result.ToString();
        }

        #endregion


        #region soundex, metaphone, levenshtein, similar_text

        /// <summary>
        /// A map of following characters: {'A', 'E', 'I', 'Y', 'O', 'U', 'a', 'e', 'i', 'y', 'o', 'u'}.
        /// </summary>
        internal static readonly CharMap vowelsMap = new CharMap(new uint[] { 0, 0, 0x44410440, 0x44410440 });

        /// <summary>
        /// Indicates whether a character is recognized as an English vowel.
        /// </summary>
        /// <param name="c">The character.</param>
        /// <returns>True iff recognized as an English vowel.</returns>
        public static bool IsVowel(char c)
        {
            return vowelsMap.Contains(c);
        }

        /// <summary>
        /// Calculates the soundex key of a string.
        /// </summary>
        /// <param name="str">The string to calculate soundex key of.</param>
        /// <returns>The soundex key of <paramref name="str"/>.</returns>
        [ImplementsFunction("soundex")]
        public static string Soundex(string str)
        {
            if (str == null || str == String.Empty) return String.Empty;

            int length = str.Length;
            const string sound = "01230120022455012623010202";

            char[] result = new char[4];
            int resPos = 0;
            char lastIdx = '0';

            for (int i = 0; i < length; i++)
            {
                char c = Char.ToUpper(str[i]);
                if (c >= 'A' && c <= 'Z')
                {
                    char idx = sound[(int)(c - 'A')];
                    if (resPos == 0)
                    {
                        result[resPos++] = c;
                        lastIdx = idx;
                    }
                    else
                    {
                        if (idx != '0' && idx != lastIdx)
                        {
                            result[resPos] = idx;
                            if (++resPos >= 4) return new string(result);
                        }

                        // Some soundex algorithm descriptions say that the following condition should
                        // be in effect...
                        /*if (c != 'W' && c != 'H')*/
                        lastIdx = idx;
                    }
                }
            }

            // pad with '0'
            do
            {
                result[resPos] = '0';
            }
            while (++resPos < 4);

            return new string(result);
        }

        /// <summary>
        /// Calculates the metaphone key of a string.
        /// </summary>
        /// <param name="str">The string to calculate metaphone key of.</param>
        /// <returns>The metaphone key of <paramref name="str"/>.</returns>
        [ImplementsFunction("metaphone")]
        public static string Metaphone(string str)
        {
            if (str == null) return String.Empty;

            int length = str.Length;
            const int padL = 4, padR = 3;

            StringBuilder sb = new StringBuilder(str.Length + padL + padR);
            StringBuilder result = new StringBuilder();

            // avoid index out of bounds problem when looking at previous and following characters
            // by padding the string at both sides
            sb.Append('\0', padL);
            sb.Append(str.ToUpper());
            sb.Append('\0', padR);

            int i = padL;
            char c = sb[i];

            // transformations at the beginning of the string
            if ((c == 'A' && sb[i + 1] == 'E') ||
                (sb[i + 1] == 'N' && (c == 'G' || c == 'K' || c == 'P')) ||
                (c == 'W' && sb[i + 1] == 'R')) i++;

            if (c == 'X') sb[i] = 'S';

            if (c == 'W' && sb[i + 1] == 'H') sb[++i] = 'W';

            // if the string starts with a vowel it is copied to output
            if (IsVowel(sb[i])) result.Append(sb[i++]);

            int end = length + padL;
            while (i < end)
            {
                c = sb[i];

                if (c == sb[i - 1] && c != 'C')
                {
                    i++;
                    continue;
                }

                // transformations of consonants (vowels as well as other characters are ignored)
                switch (c)
                {
                    case 'B':
                        if (sb[i - 1] != 'M') result.Append('B');
                        break;

                    case 'C':
                        if (sb[i + 1] == 'I' || sb[i + 1] == 'E' || sb[i + 1] == 'Y')
                        {
                            if (sb[i + 2] == 'A' && sb[i + 1] == 'I') result.Append('X');
                            else if (sb[i - 1] == 'S') break;
                            else result.Append('S');
                        }
                        else if (sb[i + 1] == 'H')
                        {
                            result.Append('X');
                            i++;
                        }
                        else result.Append('K');
                        break;

                    case 'D':
                        if (sb[i + 1] == 'G' && (sb[i + 2] == 'E' || sb[i + 2] == 'Y' ||
                            sb[i + 2] == 'I'))
                        {
                            result.Append('J');
                            i++;
                        }
                        else result.Append('T');
                        break;

                    case 'F':
                        result.Append('F');
                        break;

                    case 'G':
                        if (sb[i + 1] == 'H')
                        {
                            if (sb[i - 4] == 'H' || (sb[i - 3] != 'B' && sb[i - 3] != 'D' && sb[i - 3] != 'H'))
                            {
                                result.Append('F');
                                i++;
                            }
                            else break;
                        }
                        else if (sb[i + 1] == 'N')
                        {
                            if (sb[i + 2] < 'A' || sb[i + 2] > 'Z' ||
                                (sb[i + 2] == 'E' && sb[i + 3] == 'D')) break;
                            else result.Append('K');
                        }
                        else if ((sb[i + 1] == 'E' || sb[i + 1] == 'I' || sb[i + 1] == 'Y') && sb[i - 1] != 'G')
                        {
                            result.Append('J');
                        }
                        else result.Append('K');
                        break;

                    case 'H':
                        if (IsVowel(sb[i + 1]) && sb[i - 1] != 'C' && sb[i - 1] != 'G' &&
                            sb[i - 1] != 'P' && sb[i - 1] != 'S' && sb[i - 1] != 'T') result.Append('H');
                        break;

                    case 'J':
                        result.Append('J');
                        break;

                    case 'K':
                        if (sb[i - 1] != 'C') result.Append('K');
                        break;

                    case 'L':
                        result.Append('L');
                        break;

                    case 'M':
                        result.Append('M');
                        break;

                    case 'N':
                        result.Append('N');
                        break;

                    case 'P':
                        if (sb[i + 1] == 'H') result.Append('F');
                        else result.Append('P');
                        break;

                    case 'Q':
                        result.Append('K');
                        break;

                    case 'R':
                        result.Append('R');
                        break;

                    case 'S':
                        if (sb[i + 1] == 'I' && (sb[i + 2] == 'O' || sb[i + 2] == 'A')) result.Append('X');
                        else if (sb[i + 1] == 'H')
                        {
                            result.Append('X');
                            i++;
                        }
                        else result.Append('S');
                        break;

                    case 'T':
                        if (sb[i + 1] == 'I' && (sb[i + 2] == 'O' || sb[i + 2] == 'A')) result.Append('X');
                        else if (sb[i + 1] == 'H')
                        {
                            result.Append('0');
                            i++;
                        }
                        else result.Append('T');
                        break;

                    case 'V':
                        result.Append('F');
                        break;

                    case 'W':
                        if (IsVowel(sb[i + 1])) result.Append('W');
                        break;

                    case 'X':
                        result.Append("KS");
                        break;

                    case 'Y':
                        if (IsVowel(sb[i + 1])) result.Append('Y');
                        break;

                    case 'Z':
                        result.Append('S');
                        break;
                }

                i++;
            }

            return result.ToString();
        }

        /// <summary>
        /// Calculates the Levenshtein distance between two strings.
        /// </summary>
        /// <param name="src">The first string.</param>
        /// <param name="dst">The second string.</param>
        /// <returns>The Levenshtein distance between <paramref name="src"/> and <paramref name="dst"/> or -1 if any of the
        /// strings is longer than 255 characters.</returns>
        [ImplementsFunction("levenshtein")]
        public static int Levenshtein(string src, string dst)
        {
            return Levenshtein(src, dst, 1, 1, 1);
        }

        /// <summary>
        /// Calculates the Levenshtein distance between two strings given the cost of insert, replace
        /// and delete operations.
        /// </summary>
        /// <param name="src">The first string.</param>
        /// <param name="dst">The second string.</param>
        /// <param name="insertCost">Cost of the insert operation.</param>
        /// <param name="replaceCost">Cost of the replace operation.</param>
        /// <param name="deleteCost">Cost of the delete operation.</param>
        /// <returns>The Levenshtein distance between <paramref name="src"/> and <paramref name="dst"/> or -1 if any of the
        /// strings is longer than 255 characters.</returns>
        /// <remarks>See <A href="http://www.merriampark.com/ld.htm">http://www.merriampark.com/ld.htm</A> for description of the algorithm.</remarks>
        [ImplementsFunction("levenshtein")]
        public static int Levenshtein(string src, string dst, int insertCost, int replaceCost, int deleteCost)
        {
            if (src == null) src = String.Empty;
            if (dst == null) dst = String.Empty;

            int n = src.Length;
            int m = dst.Length;

            if (n > 255 || m > 255) return -1;

            if (n == 0) return m * insertCost;
            if (m == 0) return n * deleteCost;

            int[,] matrix = new int[n + 1, m + 1];

            for (int i = 0; i <= n; i++) matrix[i, 0] = i * deleteCost;
            for (int j = 0; j <= m; j++) matrix[0, j] = j * insertCost;

            for (int i = 1; i <= n; i++)
            {
                char cs = src[i - 1];

                for (int j = 1; j <= m; j++)
                {
                    char cd = dst[j - 1];

                    matrix[i, j] = System.Math.Min(System.Math.Min(
                        matrix[i - 1, j] + deleteCost,
                        matrix[i, j - 1] + insertCost),
                        matrix[i - 1, j - 1] + (cs == cd ? 0 : replaceCost));
                }
            }

            return matrix[n, m];
        }

        /// <summary>
        /// Calculates the similarity between two strings. Internal recursive function.
        /// </summary>
        /// <param name="first">The first string.</param>
        /// <param name="second">The second string.</param>
        /// <returns>The number of matching characters in both strings.</returns>
        /// <remarks>Algorithm description is supposed to be found 
        /// <A href="http://citeseer.nj.nec.com/oliver93decision.html">here</A>.</remarks>
        internal static int SimilarTextInternal(string first, string second)
        {
            Debug.Assert(first != null && second != null);

            int posF = 0, lengthF = first.Length;
            int posS = 0, lengthS = second.Length;
            int maxK = 0;

            for (int i = 0; i < lengthF; i++)
            {
                for (int j = 0; j < lengthS; j++)
                {
                    int k;
                    for (k = 0; i + k < lengthF && j + k < lengthS && first[i + k] == second[j + k]; k++) ;
                    if (k > maxK)
                    {
                        maxK = k;
                        posF = i;
                        posS = j;
                    }
                }
            }

            int sum = maxK;
            if (sum > 0)
            {
                if (posF > 0 && posS > 0)
                {
                    sum += SimilarTextInternal(first.Substring(0, posF), second.Substring(0, posS));
                }
                if (posF + maxK < lengthF && posS + maxK < lengthS)
                {
                    sum += SimilarTextInternal(first.Substring(posF + maxK), second.Substring(posS + maxK));
                }
            }

            return sum;
        }

        /// <summary>
        /// Calculates the similarity between two strings.
        /// </summary>
        /// <param name="first">The first string.</param>
        /// <param name="second">The second string.</param>
        /// <returns>The number of matching characters in both strings.</returns>
        [ImplementsFunction("similar_text")]
        public static int SimilarText(string first, string second)
        {
            if (first == null || second == null) return 0;
            return SimilarTextInternal(first, second);
        }

        /// <summary>
        /// Calculates the similarity between two strings.
        /// </summary>
        /// <param name="first">The first string.</param>
        /// <param name="second">The second string.</param>
        /// <param name="percent">Will become the similarity in percent.</param>
        /// <returns>The number of matching characters in both strings.</returns>
        [ImplementsFunction("similar_text")]
        public static int SimilarText(string first, string second, out double percent)
        {
            if (first == null || second == null) { percent = 0; return 0; }

            int sum = SimilarTextInternal(first, second);
            percent = (200.0 * sum) / (first.Length + second.Length);

            return sum;
        }

        #endregion


        #region strtok

        /// <summary>
        /// Holds a context of <see cref="Tokenize"/> method.
        /// </summary>
        private class TokenizerContext
        {
            /// <summary>
            /// The <b>str</b> parameter of last <see cref="Tokenize"/> method call.
            /// </summary>
            public string String;

            /// <summary>
            /// Current position in <see cref="TokenizerContext"/>.
            /// </summary>
            public int Position;

            /// <summary>
            /// The length of <see cref="TokenizerContext"/>.
            /// </summary>
            public int Length;

            /// <summary>
            /// A context associated with the current thread.
            /// </summary>
            public static TokenizerContext/*!*/CurrentContext
            {
                get
                {
                    var ctx = ScriptContext.CurrentContext;

                    TokenizerContext tctx;
                    if (ctx.Properties.TryGetProperty<TokenizerContext>(out tctx) == false)
                        ctx.Properties.SetProperty<TokenizerContext>(tctx = new TokenizerContext());
                    
                    //
                    return tctx;
                }
            }
        }

        /// <summary>
        /// Splits a string into tokens using given set of delimiter characters. Tokenizes the string
        /// that was passed to a previous call of the two-parameter version.
        /// </summary>
        /// <param name="delimiters">Set of delimiters.</param>
        /// <returns>The next token or a <B>null</B> reference.</returns>
        /// <remarks>This method implements the behavior introduced with PHP 4.1.0, i.e. empty tokens are
        /// skipped and never returned.</remarks>
        [ImplementsFunction("strtok")]
        [return: CastToFalse]
        public static string Tokenize(string delimiters)
        {
            TokenizerContext context = TokenizerContext.CurrentContext;

            if (context.Position >= context.Length) return null;
            if (delimiters == null) delimiters = String.Empty;

            int index;
            char[] delChars = delimiters.ToCharArray();
            while ((index = context.String.IndexOfAny(delChars, context.Position)) == context.Position)
            {
                if (context.Position == context.Length - 1) return null; // last char is delimiter
                context.Position++;
            }

            string token;
            if (index == -1) // delimiter not found
            {
                token = context.String.Substring(context.Position);
                context.Position = context.Length;
                return token;
            }

            token = context.String.Substring(context.Position, index - context.Position);
            context.Position = index + 1;
            return token;
        }

        /// <summary>
        /// Splits a string into tokens using given set of delimiter characters.
        /// </summary>
        /// <param name="str">The string to tokenize.</param>
        /// <param name="delimiters">Set of delimiters.</param>
        /// <returns>The first token or null. Call one-parameter version of this method to get next tokens.
        /// </returns>
        /// <remarks>This method implements the behavior introduced with PHP 4.1.0, i.e. empty tokens are
        /// skipped and never returned.</remarks>
        [ImplementsFunction("strtok")]
        [return: CastToFalse]
        public static string Tokenize(string str, string delimiters)
        {
            if (str == null) str = String.Empty;

            TokenizerContext context = TokenizerContext.CurrentContext;

            context.String = str;
            context.Length = str.Length;
            context.Position = 0;

            return Tokenize(delimiters);
        }

        #endregion


        #region trim, rtrim, ltrim, chop

        /// <summary>
        /// Strips whitespace characters from the beginning and end of a string.
        /// </summary>
        /// <param name="str">The string to trim.</param>
        /// <returns>The trimmed string.</returns>
        /// <remarks>This one-parameter version trims '\0', '\t', '\n', '\r', '\x0b' and ' ' (space).</remarks>
        [ImplementsFunction("trim")]
        public static string Trim(string str)
        {
            return Trim(str, "\0\t\n\r\x0b\x20");
        }

        /// <summary>
        /// Strips given characters from the beginning and end of a string.
        /// </summary>
        /// <param name="str">The string to trim.</param>
        /// <param name="whiteSpaceCharacters">The characters to strip from <paramref name="str"/>. Can contain ranges
        /// of characters, e.g. "\0x00..\0x1F".</param>
        /// <returns>The trimmed string.</returns>
        /// <exception cref="PhpException"><paramref name="whiteSpaceCharacters"/> is invalid char mask. Multiple errors may be printed out.</exception>
        /// <exception cref="PhpException"><paramref name="str"/> contains Unicode characters greater than '\u0800'.</exception>
        [ImplementsFunction("trim")]
        public static string Trim(string str, string whiteSpaceCharacters)
        {
            if (str == null) return String.Empty;

            // As whiteSpaceCharacters may contain intervals, I see two possible implementations:
            // 1) Call CharMap.AddUsingMask and do the trimming "by hand".
            // 2) Write another version of CharMap.AddUsingMask that would return char[] of characters
            // that fit the mask, and do the trimming with String.Trim(char[]).
            // I have chosen 1).

            CharMap charmap = InitializeCharMap();

            // may throw an exception:
            try
            {
                charmap.AddUsingMask(whiteSpaceCharacters);
            }
            catch (IndexOutOfRangeException)
            {
                PhpException.Throw(PhpError.Warning, LibResources.GetString("unicode_characters"));
                return null;
            }

            int length = str.Length, i = 0, j = length - 1;

            // finds the new beginning:
            while (i < length && charmap.Contains(str[i])) i++;

            // finds the new end:
            while (j >= 0 && charmap.Contains(str[j])) j--;

            return (i <= j) ? str.Substring(i, j - i + 1) : String.Empty;
        }

        /// <summary>Characters treated as blanks by the PHP.</summary>
        private static char[] phpBlanks = new char[] { '\0', '\t', '\n', '\r', '\u000b', ' ' };

        /// <summary>
        /// Strips whitespace characters from the beginning of a string.
        /// </summary>
        /// <param name="str">The string to trim.</param>
        /// <returns>The trimmed string.</returns>
        /// <remarks>This one-parameter version trims '\0', '\t', '\n', '\r', '\u000b' and ' ' (space).</remarks>
        [ImplementsFunction("ltrim")]
        public static string TrimStart(string str)
        {
            return (str != null) ? str.TrimStart(phpBlanks) : String.Empty;
        }

        /// <summary>
        /// Strips given characters from the beginning of a string.
        /// </summary>
        /// <param name="str">The string to trim.</param>
        /// <param name="whiteSpaceCharacters">The characters to strip from <paramref name="str"/>. Can contain ranges
        /// of characters, e.g. \0x00..\0x1F.</param>
        /// <returns>The trimmed string.</returns>
        /// <exception cref="PhpException"><paramref name="whiteSpaceCharacters"/> is invalid char mask. Multiple errors may be printed out.</exception>
        /// <exception cref="PhpException"><paramref name="whiteSpaceCharacters"/> contains Unicode characters greater than '\u0800'.</exception>
        [ImplementsFunction("ltrim")]
        public static string TrimStart(string str, string whiteSpaceCharacters)
        {
            if (str == null) return String.Empty;

            CharMap charmap = InitializeCharMap();

            // may throw an exception:
            try
            {
                charmap.AddUsingMask(whiteSpaceCharacters);
            }
            catch (IndexOutOfRangeException)
            {
                PhpException.Throw(PhpError.Warning, LibResources.GetString("unicode_characters"));
                return null;
            }

            int length = str.Length, i = 0;

            while (i < length && charmap.Contains(str[i])) i++;

            if (i < length) return str.Substring(i);
            return String.Empty;
        }

        /// <summary>
        /// Strips whitespace characters from the end of a string.
        /// </summary>
        /// <param name="str">The string to trim.</param>
        /// <returns>The trimmed string.</returns>
        /// <remarks>This one-parameter version trims '\0', '\t', '\n', '\r', '\u000b' and ' ' (space).</remarks>
        [ImplementsFunction("rtrim")]
        public static string TrimEnd(string str)
        {
            return (str != null) ? str.TrimEnd(phpBlanks) : String.Empty;
        }

        /// <summary>
        /// Strips given characters from the end of a string.
        /// </summary>
        /// <param name="str">The string to trim.</param>
        /// <param name="whiteSpaceCharacters">The characters to strip from <paramref name="str"/>. Can contain ranges
        /// of characters, e.g. \0x00..\0x1F.</param>
        /// <returns>The trimmed string.</returns>
        /// <exception cref="PhpException"><paramref name="whiteSpaceCharacters"/> is invalid char mask. Multiple errors may be printed out.</exception>
        /// <exception cref="PhpException"><paramref name="whiteSpaceCharacters"/> contains Unicode characters greater than '\u0800'.</exception>
        [ImplementsFunction("rtrim")]
        public static string TrimEnd(string str, string whiteSpaceCharacters)
        {
            if (str == null) return String.Empty;

            CharMap charmap = InitializeCharMap();

            try
            {
                charmap.AddUsingMask(whiteSpaceCharacters);
            }
            catch (IndexOutOfRangeException)
            {
                PhpException.Throw(PhpError.Warning, LibResources.GetString("unicode_characters"));
                return null;
            }

            int j = str.Length - 1;

            while (j >= 0 && charmap.Contains(str[j])) j--;

            return (j >= 0) ? str.Substring(0, j + 1) : String.Empty;
        }

        /// <summary>
        /// Strips whitespace characters from the end of a string.
        /// </summary>
        /// <param name="str">The string to trim.</param>
        /// <returns>The trimmed string.</returns>
        /// <remarks>This one-parameter version trims '\0', '\t', '\n', '\r', '\u000b' and ' ' (space).</remarks>
        [ImplementsFunction("chop")]
        public static string Chop(string str)
        {
            return TrimEnd(str);
        }

        /// <summary>
        /// Strips given characters from the end of a string.
        /// </summary>
        /// <param name="str">The string to trim.</param>
        /// <param name="whiteSpaceCharacters">The characters to strip from <paramref name="str"/>. Can contain ranges
        /// of characters, e.g. \0x00..\0x1F.</param>
        /// <returns>The trimmed string.</returns>
        /// <exception cref="PhpException">Thrown if <paramref name="whiteSpaceCharacters"/> is invalid char mask. Multiple errors may be printed out.</exception>
        [ImplementsFunction("chop")]
        public static string Chop(string str, string whiteSpaceCharacters)
        {
            return TrimEnd(str, whiteSpaceCharacters);
        }

        #endregion


        #region ucfirst, lcfirst, ucwords

        /// <summary>
        /// Makes a string's first character uppercase.
        /// </summary>
        /// <param name="str">The input string.</param>
        /// <returns><paramref name="str"/> with the first character converted to uppercase.</returns>
        [ImplementsFunction("ucfirst")]
        public static string UpperCaseFirst(string str)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;

            return Char.ToUpper(str[0]) + str.Substring(1);
        }

        /// <summary>
        /// Returns a string with the first character of str , lowercased if that character is alphabetic.
        /// Note that 'alphabetic' is determined by the current locale. For instance, in the default "C" locale characters such as umlaut-a (�) will not be converted. 
        /// </summary>
        /// <param name="str">The input string.</param>
        /// <returns>Returns the resulting string.</returns>
        [ImplementsFunction("lcfirst")]
        public static string LowerCaseFirst(string str)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;

            // first character to lower case
            return Char.ToLower(str[0]) + str.Substring(1);
        }

        /// <summary>
        /// Makes the first character of each word in a string uppercase.
        /// </summary>
        /// <param name="str">The input string.</param>
        /// <returns><paramref name="str"/> with the first character of each word in a string converted to 
        /// uppercase.</returns>
        [ImplementsFunction("ucwords")]
        public static string UpperCaseWords(string str)
        {
            if (str == null) return String.Empty;

            int length = str.Length;
            StringBuilder result = new StringBuilder(str);

            bool state = true;
            for (int i = 0; i < length; i++)
            {
                if (Char.IsWhiteSpace(result[i])) state = true;
                else
                {
                    if (state)
                    {
                        result[i] = Char.ToUpper(result[i]);
                        state = false;
                    }
                }
            }

            return result.ToString();
        }

        #endregion


        #region sprintf, vsprintf

        /// <summary>
        /// Default number of decimals when formatting floating-point numbers (%f in printf).
        /// </summary>
        internal const int printfFloatPrecision = 6;

        /// <summary>
        /// Returns a formatted string.
        /// </summary>
        /// <param name="format">The format string. 
        /// See <A href="http://www.php.net/manual/en/function.sprintf.php">PHP manual</A> for details.
        /// Besides, a type specifier "%C" is applicable. It converts an integer value to Unicode character.</param>
        /// <param name="arguments">The arguments.</param>
        /// <returns>The formatted string or null if there is too few arguments.</returns>
        /// <remarks>Assumes that either <paramref name="format"/> nor <paramref name="arguments"/> is null.</remarks>
        internal static string FormatInternal(string format, object[] arguments)
        {
            Debug.Assert(format != null && arguments != null);

            Encoding encoding = Configuration.Application.Globalization.PageEncoding;
            StringBuilder result = new StringBuilder();
            int state = 0, width = 0, precision = -1, seqIndex = 0, swapIndex = -1;
            bool leftAlign = false;
            bool plusSign = false;
            char padChar = ' ';

            // process the format string using a 6-state finite automaton
            int length = format.Length;
            for (int i = 0; i < length; i++)
            {
                char c = format[i];

            Lambda:
                switch (state)
                {
                    case 0: // the initial state
                        {
                            if (c == '%')
                            {
                                width = 0;
                                precision = -1;
                                swapIndex = -1;
                                leftAlign = false;
                                plusSign = false;
                                padChar = ' ';
                                state = 1;
                            }
                            else result.Append(c);
                            break;
                        }

                    case 1: // % character encountered, expecting format
                        {
                            switch (c)
                            {
                                case '-': leftAlign = true; break;
                                case '+': plusSign = true; break;
                                case ' ': padChar = ' '; break;
                                case '\'': state = 2; break;
                                case '.': state = 4; break;
                                case '%': result.Append(c); state = 0; break;
                                case '0': padChar = '0'; state = 3; break;

                                default:
                                    {
                                        if (Char.IsDigit(c)) state = 3;
                                        else state = 5;
                                        goto Lambda;
                                    }
                            }
                            break;
                        }

                    case 2: // ' character encountered, expecting padding character
                        {
                            padChar = c;
                            state = 1;
                            break;
                        }

                    case 3: // number encountered, expecting width or argument number
                        {
                            switch (c)
                            {
                                case '$':
                                    {
                                        swapIndex = width;
                                        if (swapIndex == 0)
                                        {
                                            PhpException.Throw(PhpError.Warning, LibResources.GetString("zero_argument_invalid"));
                                            return result.ToString();
                                        }

                                        width = 0;
                                        state = 1;
                                        break;
                                    }

                                case '.':
                                    {
                                        state = 4;
                                        break;
                                    }

                                default:
                                    {
                                        if (Char.IsDigit(c)) width = width * 10 + (int)Char.GetNumericValue(c);
                                        else
                                        {
                                            state = 5;
                                            goto Lambda;
                                        }
                                        break;
                                    }
                            }
                            break;
                        }

                    case 4: // number after . encountered, expecting precision
                        {
                            if (precision == -1) precision = 0;
                            if (Char.IsDigit(c)) precision = precision * 10 + (int)Char.GetNumericValue(c);
                            else
                            {
                                state = 5;
                                goto case 5;
                            }
                            break;
                        }

                    case 5: // expecting type specifier
                        {
                            int index = (swapIndex <= 0 ? seqIndex++ : swapIndex - 1);
                            if (index >= arguments.Length)
                            {
                                // few arguments:
                                return null;
                            }

                            object obj = arguments[index];
                            string app = null;
                            char sign = '\0';

                            switch (c)
                            {
                                case 'b': // treat as integer, present as binary number without a sign
                                    app = System.Convert.ToString(Core.Convert.ObjectToInteger(obj), 2);
                                    break;

                                case 'c': // treat as integer, present as character
                                    app = encoding.GetString(new byte[] { unchecked((byte)Core.Convert.ObjectToInteger(obj)) }, 0, 1);
                                    break;

                                case 'C': // treat as integer, present as Unicode character
                                    app = new String(unchecked((char)Core.Convert.ObjectToInteger(obj)), 1);
                                    break;

                                case 'd': // treat as integer, present as signed decimal number
                                    {
                                        // use long to prevent overflow in Math.Abs:
                                        long ivalue = Core.Convert.ObjectToInteger(obj);
                                        if (ivalue < 0) sign = '-'; else if (ivalue >= 0 && plusSign) sign = '+';

                                        app = Math.Abs((long)ivalue).ToString();
                                        break;
                                    }

                                case 'u': // treat as integer, present as unsigned decimal number, without sign
                                    app = unchecked((uint)Core.Convert.ObjectToInteger(obj)).ToString();
                                    break;

                                case 'e':
                                    {
                                        double dvalue = Core.Convert.ObjectToDouble(obj);
                                        if (dvalue < 0) sign = '-'; else if (dvalue >= 0 && plusSign) sign = '+';

                                        string f = String.Concat("0.", new String('0', precision == -1 ? printfFloatPrecision : precision), "e+0");
                                        app = Math.Abs(dvalue).ToString(f);
                                        break;
                                    }

                                case 'f': // treat as float, present locale-aware floating point number
                                    {
                                        double dvalue = Core.Convert.ObjectToDouble(obj);
                                        if (dvalue < 0) sign = '-'; else if (dvalue >= 0 && plusSign) sign = '+';

                                        app = Math.Abs(dvalue).ToString("F" + (precision == -1 ? printfFloatPrecision : precision));
                                        break;
                                    }

                                case 'F': // treat as float, present locale-unaware floating point number with '.' decimal separator (PHP 5.0.3+ feature)
                                    {
                                        double dvalue = Core.Convert.ObjectToDouble(obj);
                                        if (dvalue < 0) sign = '-'; else if (dvalue >= 0 && plusSign) sign = '+';

                                        app = Math.Abs(dvalue).ToString("F" + (precision == -1 ? printfFloatPrecision : precision),
                                          System.Globalization.NumberFormatInfo.InvariantInfo);
                                        break;
                                    }

                                case 'o': // treat as integer, present as octal number without sign
                                    app = System.Convert.ToString(Core.Convert.ObjectToInteger(obj), 8);
                                    break;

                                case 'x': // treat as integer, present as hex number (lower case) without sign
                                    app = Core.Convert.ObjectToInteger(obj).ToString("x");
                                    break;

                                case 'X': // treat as integer, present as hex number (upper case) without sign
                                    app = Core.Convert.ObjectToInteger(obj).ToString("X");
                                    break;

                                case 's': // treat as string, present as string
                                    {
                                        if (obj != null)
                                        {
                                            app = Core.Convert.ObjectToString(obj);

                                            // undocumented feature:
                                            if (precision != -1) app = app.Substring(0, Math.Min(precision, app.Length));
                                        }
                                        break;
                                    }
                            }

                            if (app != null)
                            {
                                // pad:
                                if (leftAlign)
                                {
                                    if (sign != '\0') result.Append(sign);
                                    result.Append(app);
                                    for (int j = width - app.Length; j > ((sign != '\0') ? 1 : 0); j--)
                                        result.Append(padChar);
                                }
                                else
                                {
                                    if (sign != '\0' && padChar == '0')
                                        result.Append(sign);

                                    for (int j = width - app.Length; j > ((sign != '\0') ? 1 : 0); j--)
                                        result.Append(padChar);

                                    if (sign != '\0' && padChar != '0')
                                        result.Append(sign);

                                    result.Append(app);
                                }
                            }

                            state = 0;
                            break;
                        }
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Returns a formatted string.
        /// </summary>
        /// <param name="format">The format string. For details, see PHP manual.</param>
        /// <param name="arguments">The arguments.
        /// See <A href="http://www.php.net/manual/en/function.sprintf.php">PHP manual</A> for details.
        /// Besides, a type specifier "%C" is applicable. It converts an integer value to Unicode character.</param>
        /// <returns>The formatted string.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="arguments"/> parameter is null.</exception>
        /// <exception cref="PhpException">Thrown when there is less arguments than expeceted by formatting string.</exception>
        [ImplementsFunction("sprintf")]
        [return: CastToFalse]
        public static string Format(string format, params object[] arguments)
        {
            if (format == null) return String.Empty;

            // null arguments would be compiler's error (or error of the user):
            if (arguments == null) throw new ArgumentNullException("arguments");

            string result = FormatInternal(format, arguments);
            if (result == null)
                PhpException.Throw(PhpError.Warning, LibResources.GetString("too_few_arguments"));
            return result;
        }

        /// <summary>
        /// Returns a formatted string.
        /// </summary>
        /// <param name="format">The format string. For details, see PHP manual.</param>
        /// <param name="arguments">The arguments.</param>
        /// <returns>The formatted string.</returns>
        /// <exception cref="PhpException">Thrown when there is less arguments than expeceted by formatting string.</exception>
        [ImplementsFunction("vsprintf")]
        [return: CastToFalse]
        public static string Format(string format, PhpArray arguments)
        {
            if (format == null) return String.Empty;

            object[] array;
            if (arguments != null)
            {
                array = new object[arguments.Count];
                arguments.Values.CopyTo(array, 0);
            }
            else
                array = ArrayUtils.EmptyObjects;

            string result = FormatInternal(format, array);
            if (result == null)
                PhpException.Throw(PhpError.Warning, LibResources.GetString("too_few_arguments"));
            return result;
        }

        #endregion


        #region sscanf

        /// <summary>
        /// Parses input from a string according to a format. 
        /// </summary>
        /// <param name="str">The string to be parsed.</param>
        /// <param name="format">The format. See <c>sscanf</c> C function for details.</param>
        /// <param name="arg">A PHP reference which value is set to the first parsed value.</param>
        /// <param name="arguments">PHP references which values are set to the next parsed values.</param>
        /// <returns>The number of parsed values.</returns>
        /// <remarks><seealso cref="ParseString"/>.</remarks>
        [ImplementsFunction("sscanf")]
        public static int ScanFormat(string str, string format, PhpReference arg, params PhpReference[] arguments)
        {
            if (arg == null)
                throw new ArgumentNullException("arg");
            if (arguments == null)
                throw new ArgumentNullException("arguments");

            // assumes capacity same as the number of arguments:
            ArrayList result = new ArrayList(arguments.Length + 1);

            // parses string and fills the result with parsed values:
            ParseString(str, format, result);

            // the number of specifiers differs from the number of arguments:
            if (result.Count != arguments.Length + 1)
            {
                PhpException.Throw(PhpError.Warning, LibResources.GetString("different_variables_and_specifiers", arguments.Length + 1, result.Count));
                return -1;
            }

            // the number of non-null parsed values:
            int count = 0;

            if (result[0] != null)
            {
                arg.Value = result[0];
                count = 1;
            }

            for (int i = 0; i < arguments.Length; i++)
            {
                if (arguments[i] != null && result[i + 1] != null)
                {
                    arguments[i].Value = result[i + 1];
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Parses input from a string according to a format. 
        /// </summary>
        /// <param name="str">The string to be parsed.</param>
        /// <param name="format">The format. See <c>sscanf</c> C function for details.</param>
        /// <returns>A new instance of <see cref="PhpArray"/> containing parsed values indexed by integers starting from 0.</returns>
        /// <remarks><seealso cref="ParseString"/>.</remarks>
        [ImplementsFunction("sscanf")]
        public static PhpArray ScanFormat(string str, string format)
        {
            return (PhpArray)ParseString(str, format, new PhpArray());
        }

        /// <summary>
        /// Parses a string according to a specified format.
        /// </summary>
        /// <param name="str">The string to be parsed.</param>
        /// <param name="format">The format. See <c>sscanf</c> C function for details.</param>
        /// <param name="result">A list which to fill with results.</param>
        /// <returns><paramref name="result"/> for convenience.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="result"/> is a <B>null</B> reference.</exception>
        /// <exception cref="PhpException">Invalid formatting specifier.</exception>
        public static IList ParseString(string str, string format, IList result)
        {
            if (result == null)
                throw new ArgumentNullException("result");

            if (str == null || format == null)
                return result;

            int s = 0, f = 0;

            while (f < format.Length)
            {
                char c = format[f++];

                if (c == '%')
                {
                    if (f == format.Length) break;

                    int width;   // max. parsed characters
                    bool store;  // whether to store parsed item to the result

                    // checks for asterisk which means matching value is not stored:
                    if (format[f] == '*')
                    {
                        f++;
                        if (f == format.Length) break;
                        store = false;
                    }
                    else
                    {
                        store = true;
                    }

                    // parses width (a sequence of digits without sign):
                    if (format[f] >= '0' && format[f] <= '9')
                    {
                        width = (int)Core.Convert.SubstringToLongStrict(format, -1, 10, Int32.MaxValue, ref f);
                        if (width == 0) width = Int32.MaxValue;

                        // format string ends with "%number"
                        if (f == format.Length)
                        {
                            PhpException.Throw(PhpError.Warning, LibResources.GetString("invalid_scan_conversion_character", "null"));
                            return null;
                        }
                    }
                    else
                    {
                        width = Int32.MaxValue;
                    }

                    // adds null if string parsing has been finished:
                    if (s == str.Length)
                    {
                        if (store)
                        {
                            if (format[f] == 'n')
                                result.Add(s);
                            else if (format[f] != '%')
                                result.Add(null);
                        }
                        continue;
                    }

                    // parses the string according to the format specifier:
                    object item = ParseSubstring(format[f], width, str, ref s);

                    // unknown specifier:
                    if (item == null)
                    {
                        if (format[f] == '%')
                        {
                            // stops string parsing if characters don't match:
                            if (str[s++] != '%') s = str.Length;
                        }
                        else if (format[f] == '[')
                        {
                            bool complement;
                            CharMap charmap = ParseRangeSpecifier(format, ref f, out complement);
                            if (charmap != null)
                            {
                                int start = s;

                                // skip characters contained in the specifier:
                                if (complement)
                                {
                                    while (s < str.Length && !charmap.Contains(str[s])) s++;
                                }
                                else
                                {
                                    while (s < str.Length && charmap.Contains(str[s])) s++;
                                }

                                item = str.Substring(start, s - start);
                            }
                            else
                            {
                                PhpException.Throw(PhpError.Warning, LibResources.GetString("unmatched_separator"));
                                return null;
                            }
                        }
                        else
                        {
                            PhpException.Throw(PhpError.Warning, LibResources.GetString("invalid_scan_conversion_character", c));
                            return null;
                        }
                    }

                    // stores the parsed value:
                    if (store && item != null)
                        result.Add(item);

                    // shift:
                    f++;
                }
                else if (Char.IsWhiteSpace(c))
                {
                    // skips additional white space in the format:
                    while (f < format.Length && Char.IsWhiteSpace(format[f])) f++;

                    // skips white space in the string:
                    while (s < str.Length && Char.IsWhiteSpace(str[s])) s++;
                }
                else if (s < str.Length && c != str[s++])
                {
                    // stops string parsing if characters don't match:
                    s = str.Length;
                }
            }

            return result;
        }

        /// <summary>
        /// Extracts a range specifier from the formatting string.
        /// </summary>
        /// <param name="format">The formatting string.</param>
        /// <param name="f">The position if the string pointing to the '[' at the beginning and to ']' at the end.</param>
        /// <param name="complement">Whether '^' was stated as the first character in the specifier.</param>
        /// <returns>
        /// <see cref="CharMap"/> containing the characters belonging to the range or a <B>null</B> reference on error.
        /// </returns>
        /// <remarks>
        /// Specifier should be enclosed to brackets '[', ']' and can contain complement character '^' at the beginning.
        /// The first character after '[' or '^' can be ']'. In such a case the specifier continues to the next ']'.
        /// </remarks>
        private static CharMap ParseRangeSpecifier(string format, ref int f, out bool complement)
        {
            Debug.Assert(format != null && f > 0 && f < format.Length && format[f] == '[');

            complement = false;

            f++;
            if (f < format.Length)
            {
                if (format[f] == '^')
                {
                    complement = true;
                    f++;
                }

                if (f + 1 < format.Length)
                {
                    // search for ending bracket (the first symbol can be the bracket so skip it):
                    int end = format.IndexOf(']', f + 1);
                    if (end >= 0)
                    {
                        CharMap result = InitializeCharMap();
                        result.AddUsingRegularMask(format, f, end, '-');
                        f = end;
                        return result;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Parses a string according to a given specifier.
        /// </summary>
        /// <param name="specifier">The specifier.</param>
        /// <param name="width">A width of the maximal parsed substring.</param>
        /// <param name="str">The string to be parsed.</param>
        /// <param name="s">A current position in the string.</param>
        /// <returns>The parsed value or a <B>null</B> reference on error.</returns>
        private static object ParseSubstring(char specifier, int width, string str, ref int s)
        {
            Debug.Assert(width >= 0 && str != null && s < str.Length);

            object result;
            int limit = (width < str.Length - s) ? s + width : str.Length;

            switch (specifier)
            {
                case 'S':  // string
                case 's':
                    {
                        // skips initial white spaces:
                        while (s < limit && Char.IsWhiteSpace(str[s])) s++;

                        int i = s;

                        // skips black spaces:
                        while (i < limit && !Char.IsWhiteSpace(str[i])) i++;

                        // if s = length then i = s and substring returns an empty string:
                        result = str.Substring(s, i - s);

                        // moves behind the substring:
                        s = i;

                    } break;

                case 'C': // character
                case 'c':
                    {
                        result = str[s++].ToString();
                        break;
                    }

                case 'X': // hexadecimal integer: [0-9A-Fa-f]*
                case 'x':
                    result = Core.Convert.SubstringToLongStrict(str, width, 16, Int32.MaxValue, ref s);
                    break;

                case 'o': // octal integer: [0-7]*
                    result = Core.Convert.SubstringToLongStrict(str, width, 8, Int32.MaxValue, ref s);
                    break;

                case 'd': // decimal integer: [+-]?[0-9]*
                    result = Core.Convert.SubstringToLongStrict(str, width, 10, Int32.MaxValue, ref s);
                    break;

                case 'u': // unsigned decimal integer [+-]?[1-9][0-9]*
                    result = unchecked((uint)Core.Convert.SubstringToLongStrict(str, width, 10, Int32.MaxValue, ref s));
                    break;

                case 'i': // decimal (no prefix), hexadecimal (0[xX]...), or octal (0...) integer 
                    {
                        // sign:
                        int sign = 0;
                        if (str[s] == '-') { sign = -1; s++; }
                        else
                            if (str[s] == '+') { sign = +1; s++; }

                        // string ends 
                        if (s == limit)
                        {
                            result = 0;
                            break;
                        }

                        if (str[s] != '0')
                        {
                            if (sign != 0) s--;
                            result = (int)Core.Convert.SubstringToLongStrict(str, width, 10, Int32.MaxValue, ref s);
                            break;
                        }
                        s++;

                        // string ends 
                        if (s == limit)
                        {
                            result = 0;
                            break;
                        }

                        int number = 0;

                        if (str[s] == 'x' || str[s] == 'X')
                        {
                            s++;

                            // reads unsigned hexadecimal number starting from the next position:
                            if (s < limit && str[s] != '+' && str[s] != '-')
                                number = (int)Core.Convert.SubstringToLongStrict(str, width, 16, Int32.MaxValue, ref s);
                        }
                        else
                        {
                            // reads unsigned octal number starting from the current position:
                            if (str[s] != '+' && str[s] != '-')
                                number = (int)Core.Convert.SubstringToLongStrict(str, width, 8, Int32.MaxValue, ref s);
                        }

                        // minus sign has been stated:
                        result = (sign >= 0) ? +number : -number;
                        break;
                    }

                case 'e': // float
                case 'E':
                case 'g':
                case 'G':
                case 'f':
                    result = Core.Convert.SubstringToDouble(str, width, ref s);
                    break;

                case 'n': // the number of read characters is placed into result:
                    result = s;
                    break;

                default:
                    result = null;
                    break;
            }

            return result;
        }

        #endregion


        #region wordwrap

        /// <summary>
        /// Wraps a string to 75 characters using new line as the break character.
        /// </summary>
        /// <param name="str">The string to word-wrap.</param>
        /// <returns>The word-wrapped string.</returns>
        /// <remarks>The only "break-point" character is space (' '). If a word is longer than 75 characers
        /// it will stay uncut.</remarks>
        [ImplementsFunction("wordwrap")]
        [return: CastToFalse]
        public static string WordWrap(string str)
        {
            return WordWrap(str, 75, "\n", false);
        }

        /// <summary>
        /// Wraps a string to a specified number of characters using new line as the break character.
        /// </summary>
        /// <param name="str">The string to word-wrap.</param>
        /// <param name="width">The desired line length.</param>
        /// <returns>The word-wrapped string.</returns>
        /// <remarks>The only "break-point" character is space (' '). If a word is longer than <paramref name="width"/> 
        /// characers it will stay uncut.</remarks>
        [ImplementsFunction("wordwrap")]
        [return: CastToFalse]
        public static string WordWrap(string str, int width)
        {
            return WordWrap(str, width, "\n", false);
        }

        /// <summary>
        /// Wraps a string to a specified number of characters using a specified string as the break string.
        /// </summary>
        /// <param name="str">The string to word-wrap.</param>
        /// <param name="width">The desired line length.</param>
        /// <param name="lineBreak">The break string.</param>
        /// <returns>The word-wrapped string.</returns>
        /// <remarks>The only "break-point" character is space (' '). If a word is longer than <paramref name="width"/> 
        /// characers it will stay uncut.</remarks>
        [ImplementsFunction("wordwrap")]
        [return: CastToFalse]
        public static string WordWrap(string str, int width, string lineBreak)
        {
            return WordWrap(str, width, lineBreak, false);
        }

        /// <summary>
        /// Wraps a string to a specified number of characters using a specified string as the break string.
        /// </summary>
        /// <param name="str">The string to word-wrap.</param>
        /// <param name="width">The desired line length.</param>
        /// <param name="lineBreak">The break string.</param>
        /// <param name="cut">If true, words longer than <paramref name="width"/> will be cut so that no line is longer
        /// than <paramref name="width"/>.</param>
        /// <returns>The word-wrapped string.</returns>
        /// <remarks>The only "break-point" character is space (' ').</remarks>
        /// <exception cref="PhpException">Thrown if the combination of <paramref name="width"/> and <paramref name="cut"/> is invalid.</exception>
        [ImplementsFunction("wordwrap")]
        [return: CastToFalse]
        public static string WordWrap(string str, int width, string lineBreak, bool cut)
        {
            if (width == 0 && cut)
            {
                PhpException.Throw(PhpError.Warning, LibResources.GetString("cut_forced_with_zero_width"));
                return null;
            }
            if (str == null) return null;

            int length = str.Length;
            StringBuilder result = new StringBuilder(length);

            // mimic the strange PHP behaviour when width < 0 and cut is true
            if (width < 0 && cut)
            {
                result.Append(lineBreak);
                width = 1;
            }

            int lastSpace = -1, lineStart = 0;
            for (int i = 0; i < length; i++)
            {
                if (str[i] == ' ')
                {
                    lastSpace = i;
                    if (i - lineStart >= width + 1)
                    {
                        // cut is false if we get here
                        if (lineStart == 0)
                        {
                            result.Append(str, 0, i);
                        }
                        else
                        {
                            result.Append(lineBreak);
                            result.Append(str, lineStart, i - lineStart);
                        }

                        lineStart = i + 1;
                        continue;
                    }
                }

                if (i - lineStart >= width)
                {
                    // we reached the specified width

                    if (lastSpace > lineStart) // obsolete: >=
                    {
                        if (lineStart > 0) result.Append(lineBreak);
                        result.Append(str, lineStart, lastSpace - lineStart);
                        lineStart = lastSpace + 1;
                    }
                    else if (cut)
                    {
                        if (lineStart > 0) result.Append(lineBreak);
                        result.Append(str, lineStart, width);
                        lineStart = i;
                    }
                }
            }

            // process the rest of str
            if (lineStart < length || lastSpace == length - 1)
            {
                if (lineStart > 0) result.Append(lineBreak);
                result.Append(str, lineStart, length - lineStart);
            }

            return result.ToString();
        }

        #endregion


        #region number_format, NS: money_format

        /// <summary>
        /// Formats a number with grouped thousands.
        /// </summary>
        /// <param name="number">The number to format.</param>
        /// <returns>String representation of the number without decimals (rounded) with comma between every group
        /// of thousands.</returns>
        [ImplementsFunction("number_format")]
        public static string FormatNumber(double number)
        {
            return FormatNumber(number, 0, ".", ",");
        }

        /// <summary>
        /// Formats a number with grouped thousands and with given number of decimals.
        /// </summary>
        /// <param name="number">The number to format.</param>
        /// <param name="decimals">The number of decimals.</param>
        /// <returns>String representation of the number with <paramref name="decimals"/> decimals with a dot in front, and with 
        /// comma between every group of thousands.</returns>
        [ImplementsFunction("number_format")]
        public static string FormatNumber(double number, int decimals)
        {
            return FormatNumber(number, decimals, ".", ",");
        }

        /// <summary>
        /// Formats a number with grouped thousands, with given number of decimals, with given decimal point string
        /// and with given thousand separator.
        /// </summary>
        /// <param name="number">The number to format.</param>
        /// <param name="decimals">The number of decimals within range 0 to 99.</param>
        /// <param name="decimalPoint">The string to separate integer part and decimals.</param>
        /// <param name="thousandsSeparator">The character to separate groups of thousands. Only the first character
        /// of <paramref name="thousandsSeparator"/> is used.</param>
        /// <returns>
        /// String representation of the number with <paramref name="decimals"/> decimals with <paramref name="decimalPoint"/> in 
        /// front, and with <paramref name="thousandsSeparator"/> between every group of thousands.
        /// </returns>
        /// <remarks>
        /// The <b>number_format</b> (<see cref="FormatNumber"/>) PHP function requires <paramref name="decimalPoint"/> and <paramref name="thousandsSeparator"/>
        /// to be of length 1 otherwise it uses default values (dot and comma respectively). As this behavior does
        /// not make much sense, this method has no such limitation except for <paramref name="thousandsSeparator"/> of which
        /// only the first character is used (documented feature).
        /// </remarks>
        [ImplementsFunction("number_format")]
        public static string FormatNumber(double number, int decimals, string decimalPoint, string thousandsSeparator)
        {
            System.Globalization.NumberFormatInfo format = new System.Globalization.NumberFormatInfo();

            if ((decimals >= 0) && (decimals <= 99))
            {
                format.NumberDecimalDigits = decimals;
            }
            else
            {
                PhpException.InvalidArgument("decimals", LibResources.GetString("arg:out_of_bounds", decimals));
            }

            if (!String.IsNullOrEmpty(decimalPoint))
            {
                format.NumberDecimalSeparator = decimalPoint;
            }

            if (thousandsSeparator == null) thousandsSeparator = String.Empty;

            switch (thousandsSeparator.Length)
            {
                case 0: format.NumberGroupSeparator = String.Empty; break;
                case 1: format.NumberGroupSeparator = thousandsSeparator; break;
                default: format.NumberGroupSeparator = thousandsSeparator.Substring(0, 1); break;
            }

            return number.ToString("N", format);
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        [ImplementsFunction("money_format", FunctionImplOptions.NotSupported)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static string FormatMoney(string format, double number)
        {
            PhpException.FunctionNotSupported();
            return null;
        }

        #endregion


        #region hebrev, hebrevc

        /// <summary>
        /// Indicates whether a character is recognized as Hebrew letter.
        /// </summary>
        /// <param name="c">The character.</param>
        /// <returns>
        /// Whether the <paramref name="c"/> is a Hebrew letter according to 
        /// the <A href="http://www.unicode.org/charts/PDF/U0590.pdf">Unicode 4.0 standard</A>.
        /// </returns>
        public static bool IsHebrew(char c)
        {
            return c >= '\u05d0' && c <= '\u05ea';
        }

        /// <summary>
        /// Indicates whether a character is a space or tab.
        /// </summary>
        /// <param name="c">The character.</param>
        /// <returns>True iff space or tab.</returns>
        internal static bool IsBlank(char c)
        {
            return c == ' ' || c == '\t';
        }

        /// <summary>
        /// Indicates whether a character is new line or carriage return.
        /// </summary>
        /// <param name="c">The character.</param>
        /// <returns>True iff new line or carriage return.</returns>
        internal static bool IsNewLine(char c)
        {
            return c == '\n' || c == '\r';
        }

        /// <summary>
        /// Converts logical Hebrew text to visual text.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <param name="maxCharactersPerLine">If &gt;0, maximum number of characters per line. If 0,
        /// there is no maximum.</param>
        /// <param name="convertNewLines">Whether to convert new lines '\n' to "&lt;br/&gt;".</param>
        /// <returns>The converted string.</returns>
        internal static string HebrewReverseInternal(string str, int maxCharactersPerLine, bool convertNewLines)
        {
            if (str == null || str == String.Empty) return str;
            int length = str.Length, blockLength = 0, blockStart = 0, blockEnd = 0;

            StringBuilder hebStr = new StringBuilder(length);
            hebStr.Length = length;

            bool blockTypeHeb = IsHebrew(str[0]);
            int source = 0, target = length - 1;

            do
            {
                if (blockTypeHeb)
                {
                    while (source + 1 < length && (IsHebrew(str[source + 1]) || IsBlank(str[source + 1]) ||
                        Char.IsPunctuation(str[source + 1]) || str[source + 1] == '\n') && blockEnd < length - 1)
                    {
                        source++;
                        blockEnd++;
                        blockLength++;
                    }

                    for (int i = blockStart; i <= blockEnd; i++)
                    {
                        switch (str[i])
                        {
                            case '(': hebStr[target] = ')'; break;
                            case ')': hebStr[target] = '('; break;
                            case '[': hebStr[target] = ']'; break;
                            case ']': hebStr[target] = '['; break;
                            case '{': hebStr[target] = '}'; break;
                            case '}': hebStr[target] = '{'; break;
                            case '<': hebStr[target] = '>'; break;
                            case '>': hebStr[target] = '<'; break;
                            case '\\': hebStr[target] = '/'; break;
                            case '/': hebStr[target] = '\\'; break;
                            default: hebStr[target] = str[i]; break;
                        }
                        target--;
                    }
                    blockTypeHeb = false;
                }
                else
                {
                    // blockTypeHeb == false

                    while (source + 1 < length && !IsHebrew(str[source + 1]) && str[source + 1] != '\n' &&
                        blockEnd < length - 1)
                    {
                        source++;
                        blockEnd++;
                        blockLength++;
                    }
                    while ((IsBlank(str[source]) || Char.IsPunctuation(str[source])) && str[source] != '/' &&
                        str[source] != '-' && blockEnd > blockStart)
                    {
                        source--;
                        blockEnd--;
                    }
                    for (int i = blockEnd; i >= blockStart; i--)
                    {
                        hebStr[target] = str[i];
                        target--;
                    }
                    blockTypeHeb = true;
                }

                blockStart = blockEnd + 1;

            } while (blockEnd < length - 1);

            StringBuilder brokenStr = new StringBuilder(length);
            brokenStr.Length = length;
            int begin = length - 1, end = begin, charCount, origBegin;
            target = 0;

            while (true)
            {
                charCount = 0;
                while ((maxCharactersPerLine == 0 || charCount < maxCharactersPerLine) && begin > 0)
                {
                    charCount++;
                    begin--;
                    if (begin <= 0 || IsNewLine(hebStr[begin]))
                    {
                        while (begin > 0 && IsNewLine(hebStr[begin - 1]))
                        {
                            begin--;
                            charCount++;
                        }
                        break;
                    }
                }

                if (charCount == maxCharactersPerLine)
                {
                    // try to avoid breaking words
                    int newCharCount = charCount, newBegin = begin;

                    while (newCharCount > 0)
                    {
                        if (IsBlank(hebStr[newBegin]) || IsNewLine(hebStr[newBegin])) break;

                        newBegin++;
                        newCharCount--;
                    }
                    if (newCharCount > 0)
                    {
                        charCount = newCharCount;
                        begin = newBegin;
                    }
                }
                origBegin = begin;

                if (IsBlank(hebStr[begin])) hebStr[begin] = '\n';

                while (begin <= end && IsNewLine(hebStr[begin]))
                {
                    // skip leading newlines
                    begin++;
                }

                for (int i = begin; i <= end; i++)
                {
                    // copy content
                    brokenStr[target] = hebStr[i];
                    target++;
                }

                for (int i = origBegin; i <= end && IsNewLine(hebStr[i]); i++)
                {
                    brokenStr[target] = hebStr[i];
                    target++;
                }

                begin = origBegin;
                if (begin <= 0) break;

                begin--;
                end = begin;
            }

            if (convertNewLines) brokenStr.Replace("\n", "<br/>\n");
            return brokenStr.ToString();
        }

        /// <summary>
        /// Converts logical Hebrew text to visual text.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <returns>The comverted string.</returns>
        /// <remarks>Although PHP returns false if <paramref name="str"/> is null or empty there is no reason to do so.</remarks>
        [ImplementsFunction("hebrev")]
        public static string HebrewReverse(string str)
        {
            return HebrewReverseInternal(str, 0, false);
        }

        /// <summary>
        /// Converts logical Hebrew text to visual text.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <param name="maxCharactersPerLine">Maximum number of characters per line.</param>
        /// <returns>The comverted string.</returns>
        /// <remarks>Although PHP returns false if <paramref name="str"/> is null or empty there is no reason to do so.</remarks>
        [ImplementsFunction("hebrev")]
        public static string HebrewReverse(string str, int maxCharactersPerLine)
        {
            return HebrewReverseInternal(str, maxCharactersPerLine, false);
        }

        /// <summary>
        /// Converts logical Hebrew text to visual text and also converts new lines '\n' to "&lt;br/&gt;".
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <returns>The converted string.</returns>
        /// <remarks>Although PHP returns false if <paramref name="str"/> is null or empty there is no reason to do so.</remarks>
        [ImplementsFunction("hebrevc")]
        public static string HebrewReverseWithNewLines(string str)
        {
            return HebrewReverseInternal(str, 0, true);
        }

        /// <summary>
        /// Converts logical Hebrew text to visual text and also converts new lines '\n' to "&lt;br/&gt;".
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <param name="maxCharactersPerLine">Maximum number of characters per line.</param>
        /// <returns>The comverted string.</returns>
        /// <remarks>Although PHP returns false if <paramref name="str"/> is null or empty there is no reason to do so.</remarks>
        [ImplementsFunction("hebrevc")]
        public static string HebrewReverseWithNewLines(string str, int maxCharactersPerLine)
        {
            return HebrewReverseInternal(str, maxCharactersPerLine, true);
        }

        #endregion


        #region strnatcmp, strnatcasecmp

        /// <summary>
        /// Compares two strings using the natural ordering.
        /// </summary>
        /// <example>NaturalCompare("page155", "page16") returns 1.</example>
        /// <include file='Doc/../../Core/Doc/Common.xml' path='docs/method[@name="Compare(x,y)"]/*'/>
        [ImplementsFunction("strnatcmp")]
        public static int NaturalCompare(string x, string y)
        {
            return PhpNaturalComparer.Default.Compare(x, y);
        }

        /// <summary>
        /// Compares two strings using the natural ordering. Ignores the case.
        /// </summary>
        /// <include file='Doc/../../Core/Doc/Common.xml' path='docs/method[@name="Compare(x,y)"]/*'/>
        [ImplementsFunction("strnatcasecmp")]
        public static int NaturalCompareIgnoringCase(string x, string y)
        {
            return PhpNaturalComparer.CaseInsensitive.Compare(x, y);
        }

        #endregion


        #region str_pad

        /// <summary>
        /// Pads a string to a certain length with spaces.
        /// </summary>
        /// <param name="str">The string to pad.</param>
        /// <param name="totalWidth">Desired length of the returned string.</param>
        /// <returns><paramref name="str"/> padded on the right with spaces.</returns>
        [ImplementsFunction("str_pad")]
        public static object Pad(object str, int totalWidth)
        {
            if (str is PhpBytes) return Pad(str, totalWidth, new PhpBytes(32));
            else return Pad(str, totalWidth, " ");
        }

        /// <summary>
        /// Pads a string to certain length with another string.
        /// </summary>
        /// <param name="str">The string to pad.</param>
        /// <param name="totalWidth">Desired length of the returned string.</param>
        /// <param name="paddingString">The string to use as the pad.</param>
        /// <returns><paramref name="str"/> padded on the right with <paramref name="paddingString"/>.</returns>
        /// <exception cref="PhpException">Thrown if <paramref name="paddingString"/> is null or empty.</exception>
        [ImplementsFunction("str_pad")]
        public static object Pad(object str, int totalWidth, object paddingString)
        {
            return Pad(str, totalWidth, paddingString, PaddingType.Right);
        }

        /// <summary>
        /// Pads a string to certain length with another string.
        /// </summary>
        /// <param name="str">The string to pad.</param>
        /// <param name="totalWidth">Desired length of the returned string.</param>
        /// <param name="paddingString">The string to use as the pad.</param>
        /// <param name="paddingType">Specifies whether the padding should be done on the left, on the right,
        /// or on both sides of <paramref name="str"/>.</param>
        /// <returns><paramref name="str"/> padded with <paramref name="paddingString"/>.</returns>
        /// <exception cref="PhpException">Thrown if <paramref name="paddingType"/> is invalid or <paramref name="paddingString"/> is null or empty.</exception>
        [ImplementsFunction("str_pad")]
        public static object Pad(object str, int totalWidth, object paddingString, PaddingType paddingType)
        {
            PhpBytes binstr = str as PhpBytes;
            if (str is PhpBytes)
            {
                PhpBytes binPaddingString = Core.Convert.ObjectToPhpBytes(paddingString);

                if (binPaddingString == null || binPaddingString.Length == 0)
                {
                    PhpException.InvalidArgument("paddingString", LibResources.GetString("arg:null_or_empty"));
                    return null;
                }
                if (binstr == null) binstr = PhpBytes.Empty;

                int length = binstr.Length;
                if (totalWidth <= length) return binstr;

                int pad = totalWidth - length, padLeft = 0, padRight = 0;

                switch (paddingType)
                {
                    case PaddingType.Left: padLeft = pad; break;
                    case PaddingType.Right: padRight = pad; break;

                    case PaddingType.Both:
                        padLeft = pad / 2;
                        padRight = pad - padLeft;
                        break;

                    default:
                        PhpException.InvalidArgument("paddingType");
                        break;
                }

                // if paddingString has length 1, use String.PadLeft and String.PadRight
                int padStrLength = binPaddingString.Length;

                // else build the resulting string manually
                byte[] result = new byte[totalWidth];

                int position = 0;

                // pad left
                while (padLeft > padStrLength)
                {
                    Buffer.BlockCopy(binPaddingString.ReadonlyData, 0, result, position, padStrLength);
                    padLeft -= padStrLength;
                    position += padStrLength;
                }

                if (padLeft > 0)
                {
                    Buffer.BlockCopy(binPaddingString.ReadonlyData, 0, result, position, padLeft);
                    position += padLeft;
                }

                Buffer.BlockCopy(binstr.ReadonlyData, 0, result, position, binstr.Length);
                position += binstr.Length;

                // pad right
                while (padRight > padStrLength)
                {
                    Buffer.BlockCopy(binPaddingString.ReadonlyData, 0, result, position, padStrLength);
                    padRight -= padStrLength;
                    position += padStrLength;
                }

                if (padRight > 0)
                {
                    Buffer.BlockCopy(binPaddingString.ReadonlyData, 0, result, position, padRight);
                    position += padRight;
                }

                return new PhpBytes(result);
            }

            string unistr = Core.Convert.ObjectToString(str);
            if (unistr != null)
            {
                string uniPaddingString = Core.Convert.ObjectToString(paddingString);

                if (String.IsNullOrEmpty(uniPaddingString))
                {
                    PhpException.InvalidArgument("paddingString", LibResources.GetString("arg:null_or_empty"));
                    return null;
                }
                if (unistr == null) unistr = String.Empty;

                int length = unistr.Length;
                if (totalWidth <= length) return unistr;

                int pad = totalWidth - length, padLeft = 0, padRight = 0;

                switch (paddingType)
                {
                    case PaddingType.Left: padLeft = pad; break;
                    case PaddingType.Right: padRight = pad; break;

                    case PaddingType.Both:
                        padLeft = pad / 2;
                        padRight = pad - padLeft;
                        break;

                    default:
                        PhpException.InvalidArgument("paddingType");
                        break;
                }

                // if paddingString has length 1, use String.PadLeft and String.PadRight
                int padStrLength = uniPaddingString.Length;
                if (padStrLength == 1)
                {
                    char c = uniPaddingString[0];
                    if (padLeft > 0) unistr = unistr.PadLeft(length + padLeft, c);
                    if (padRight > 0) unistr = unistr.PadRight(totalWidth, c);

                    return unistr;
                }

                // else build the resulting string manually
                StringBuilder result = new StringBuilder(totalWidth);

                // pad left
                while (padLeft > padStrLength)
                {
                    result.Append(uniPaddingString);
                    padLeft -= padStrLength;
                }
                if (padLeft > 0) result.Append(uniPaddingString.Substring(0, padLeft));

                result.Append(unistr);

                // pad right
                while (padRight > padStrLength)
                {
                    result.Append(uniPaddingString);
                    padRight -= padStrLength;
                }
                if (padRight > 0) result.Append(uniPaddingString.Substring(0, padRight));

                return result.ToString();
            }

            return null;
        }

        #endregion


        #region str_word_count

        /// <summary>
        /// Counts the number of words inside a string.
        /// </summary>
        /// <param name="str">The string containing words to count.</param>
        /// <returns>Then number of words inside <paramref name="str"/>. </returns>
        [ImplementsFunction("str_word_count")]
        public static int CountWords(string str)
        {
            return CountWords(str, WordCountResult.WordCount, null, null);
        }

        /// <summary>
        /// Splits a string into words.
        /// </summary>
        /// <param name="str">The string to split.</param>
        /// <param name="format">If <see cref="WordCountResult.WordsArray"/>, the method returns an array containing all
        /// the words found inside the string. If <see cref="WordCountResult.PositionsToWordsMapping"/>, the method returns 
        /// an array, where the key is the numeric position of the word inside the string and the value is the 
        /// actual word itself.</param>
        /// <returns>Array of words. Keys are just numbers starting with 0 (when <paramref name="format"/> is 
        /// WordCountResult.WordsArray) or positions of the words inside <paramref name="str"/> (when
        /// <paramref name="format"/> is <see cref="WordCountResult.PositionsToWordsMapping"/>).</returns>
        /// <exception cref="PhpException">Thrown if <paramref name="format"/> is invalid.</exception>
        [ImplementsFunction("str_word_count")]
        public static object CountWords(string str, WordCountResult format)
        {
            return CountWords(str, format, null);
        }

        [ImplementsFunction("str_word_count")]
        public static object CountWords(string str, WordCountResult format, string addWordChars)
        {
            PhpArray words = (format != WordCountResult.WordCount) ? new PhpArray() : null;

            int count = CountWords(str, format, addWordChars, words);

            if (count == -1)
                return false;

            if (format == WordCountResult.WordCount)
                return count;
            else
            {
                if (words != null)
                    return words;
                else
                    return false;
            }
        }

        private static bool IsWordChar(char c, CharMap map)
        {
            return Char.IsLetter(c) || map != null && map.Contains(c);
        }

        public static int CountWords(string str, WordCountResult format, string addWordChars, IDictionary words)
        {
            if (str == null)
                return 0;
            if (format != WordCountResult.WordCount && words == null)
                throw new ArgumentNullException("words");

            CharMap charmap = null;

            if (!String.IsNullOrEmpty(addWordChars))
            {
                charmap = InitializeCharMap();
                charmap.Add(addWordChars);
            }

            // find the end
            int last = str.Length - 1;
            if (last > 0 && str[last] == '-' && !IsWordChar(str[last], charmap)) last--;

            // find the beginning
            int pos = 0;
            if (last >= 0 && (str[0] == '-' || str[0] == '\'') && !IsWordChar(str[0], charmap)) pos++;

            int word_count = 0;

            while (pos <= last)
            {
                if (IsWordChar(str[pos], charmap) || str[pos] == '\'' || str[pos] == '-')
                {
                    // word started - read it whole:
                    int word_start = pos++;
                    while (pos <= last &&
                        (IsWordChar(str[pos], charmap) ||
                         str[pos] == '\'' || str[pos] == '-'))
                    {
                        pos++;
                    }

                    switch (format)
                    {
                        case WordCountResult.WordCount:
                            break;

                        case WordCountResult.WordsArray:
                            words.Add(word_count, str.Substring(word_start, pos - word_start));
                            break;

                        case WordCountResult.PositionsToWordsMapping:
                            words.Add(word_start, str.Substring(word_start, pos - word_start));
                            break;

                        default:
                            PhpException.InvalidArgument("format");
                            return -1;
                    }

                    word_count++;
                }
                else pos++;
            }
            return word_count;
        }

        #endregion


        #region strcmp, strcasecmp, strncmp, strncasecmp

        /// <summary>
        /// Compares two specified strings, honoring their case, using culture invariant comparison.
        /// </summary>
        /// <param name="str1">A string.</param>
        /// <param name="str2">A string.</param>
        /// <returns>Returns -1 if <paramref name="str1"/> is less than <paramref name="str2"/>; +1 if <paramref name="str1"/> is greater than <paramref name="str2"/>,
        /// and 0 if they are equal.</returns>
        [ImplementsFunction("strcmp")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static int Compare(string str1, string str2)
        {
            return String.CompareOrdinal(str1, str2);
        }

        /// <summary>
        /// Compares two specified strings, ignoring their case, using culture invariant comparison.
        /// </summary>
        /// <param name="str1">A string.</param>
        /// <param name="str2">A string.</param>
        /// <returns>Returns -1 if <paramref name="str1"/> is less than <paramref name="str2"/>; +1 if <paramref name="str1"/> is greater than <paramref name="str2"/>,
        /// and 0 if they are equal.</returns>
        [ImplementsFunction("strcasecmp")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static int CompareIgnoringCase(string str1, string str2)
        {
#if SILVERLIGHT
			return String.Compare(str1, str2, System.Globalization.CultureInfo.InvariantCulture,System.Globalization.CompareOptions.IgnoreCase);
#else
            return String.Compare(str1, str2, true, System.Globalization.CultureInfo.InvariantCulture);
#endif
        }

        /// <summary>
        /// Compares parts of two specified strings, honoring their case, using culture invariant comparison.
        /// </summary>
        /// <param name="str1">The lesser string.</param>
        /// <param name="str2">The greater string.</param>
        /// <param name="length">The upper limit of the length of parts to be compared.</param>
        /// <returns>Returns -1 if <paramref name="str1"/> is less than <paramref name="str2"/>; +1 if <paramref name="str1"/> is greater than <paramref name="str2"/>,
        /// and 0 if they are equal.</returns>
        [ImplementsFunction("strncmp")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object Compare(string str1, string str2, int length)
        {
            if (length < 0)
            {
                PhpException.Throw(PhpError.Warning, LibResources.GetString("must_be_positive", "Length"));
                return false;
            }

            return String.CompareOrdinal(str1, 0, str2, 0, length);
        }

        /// <summary>
        /// Compares parts of two specified strings, honoring their case, using culture invariant comparison.
        /// </summary>
        /// <param name="str1">A string.</param>
        /// <param name="str2">A string.</param>
        /// <param name="length">The upper limit of the length of parts to be compared.</param>
        /// <returns>Returns -1 if <paramref name="str1"/> is less than <paramref name="str2"/>; +1 if <paramref name="str1"/> is greater than <paramref name="str2"/>,
        /// and 0 if they are equal.</returns>
        [ImplementsFunction("strncasecmp")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object CompareIgnoringCase(string str1, string str2, int length)
        {
            if (length < 0)
            {
                PhpException.Throw(PhpError.Warning, LibResources.GetString("must_be_positive", "Length"));
                return false;
            }

#if SILVERLIGHT
			return String.Compare(str1, 0, str2, 0, length, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.CompareOptions.IgnoreCase);
#else
            return String.Compare(str1, 0, str2, 0, length, true, System.Globalization.CultureInfo.InvariantCulture);
#endif
        }

        #endregion


        #region strpos, strrpos, stripos, strripos

        #region Stubs

        /// <summary>
        /// Retrieves the index of the first occurrence of the <paramref name="needle"/> in the <paramref name="haystack"/>.
        /// </summary>
        /// <remarks>See <see cref="Strpos(string,object,int)"/> for details.</remarks>
        /// <exception cref="PhpException">Thrown if <paramref name="needle"/> is empty string.</exception>
        [ImplementsFunction("strpos"), EditorBrowsable(EditorBrowsableState.Never)]
        [return: CastToFalse]
        public static int Strpos(string haystack, object needle)
        {
            return Strpos(haystack, needle, 0, false);
        }

        /// <summary>
        /// Retrieves the index of the first occurrence of the <paramref name="needle"/> in the <paramref name="haystack"/>.
        /// The search starts at the specified character position.
        /// </summary>
        /// <param name="haystack">The string to search in.</param>
        /// <param name="needle">
        /// The string or the ordinal value of character to search for. 
        /// If non-string is passed as a needle then it is converted to an integer (modulo 256) and the character
        /// with such ordinal value (relatively to the current encoding set in the configuration) is searched.</param>
        /// <param name="offset">
        /// The position where to start searching. Should be between 0 and a length of the <paramref name="haystack"/> including.
        /// </param>
        /// <returns>Non-negative integer on success, -1 otherwise.</returns>
        /// <exception cref="PhpException"><paramref name="offset"/> is out of bounds or <paramref name="needle"/> is empty string.</exception>
        [ImplementsFunction("strpos"), EditorBrowsable(EditorBrowsableState.Never)]
        [return: CastToFalse]
        public static int Strpos(string haystack, object needle, int offset)
        {
            return Strpos(haystack, needle, offset, false);
        }

        /// <summary>
        /// Retrieves the index of the first occurrence of the <paramref name="needle"/> in the <paramref name="haystack"/>
        /// (case insensitive).
        /// </summary>
        /// <remarks>See <see cref="Strpos(string,object,int)"/> for details.</remarks>
        /// <exception cref="PhpException">Thrown if <paramref name="needle"/> is empty string.</exception>
        [ImplementsFunction("stripos"), EditorBrowsable(EditorBrowsableState.Never)]
        [return: CastToFalse]
        public static int Stripos(string haystack, object needle)
        {
            return Strpos(haystack, needle, 0, true);
        }

        /// <summary>
        /// Retrieves the index of the first occurrence of the <paramref name="needle"/> in the <paramref name="haystack"/>
        /// (case insensitive).
        /// </summary>
        /// <remarks>See <see cref="Strpos(string,object,int)"/> for details.</remarks>
        /// <exception cref="PhpException">Thrown if <paramref name="offset"/> is out of bounds or <paramref name="needle"/> is empty string.</exception>
        [ImplementsFunction("stripos"), EditorBrowsable(EditorBrowsableState.Never)]
        [return: CastToFalse]
        public static int Stripos(string haystack, object needle, int offset)
        {
            return Strpos(haystack, needle, offset, true);
        }

        /// <summary>
        /// Retrieves the index of the last occurrence of the <paramref name="needle"/> in the <paramref name="haystack"/>.
        /// </summary>
        /// <remarks>See <see cref="Strrpos(string,object,int)"/> for details.</remarks>
        [ImplementsFunction("strrpos"), EditorBrowsable(EditorBrowsableState.Never)]
        [return: CastToFalse]
        public static int Strrpos(string haystack, object needle)
        {
            return Strrpos(haystack, needle, 0, false);
        }

        /// <summary>
        /// Retrieves the index of the last occurrence of the <paramref name="needle"/> in the <paramref name="haystack"/>.
        /// The search starts at the specified character position.
        /// </summary>
        /// <param name="haystack">The string to search in.</param>
        /// <param name="needle">The string or the ordinal value of character to search for. 
        /// If non-string is passed as a needle then it is converted to an integer (modulo 256) and the character
        /// with such ordinal value (relatively to the current encoding set in the configuration) is searched.</param>
        /// <param name="offset">
        /// The position where to start searching (is non-negative) or a negative number of characters
        /// prior the end where to stop searching (if negative).
        /// </param>
        /// <returns>Non-negative integer on success, -1 otherwise.</returns>
        /// <exception cref="PhpException">Thrown if <paramref name="offset"/> is out of bounds or <paramref name="needle"/> is empty string.</exception>
        [ImplementsFunction("strrpos"), EditorBrowsable(EditorBrowsableState.Never)]
        [return: CastToFalse]
        public static int Strrpos(string haystack, object needle, int offset)
        {
            return Strrpos(haystack, needle, offset, false);
        }


        /// <summary>
        /// Retrieves the index of the last occurrence of the <paramref name="needle"/> in the <paramref name="haystack"/>
        /// (case insensitive).
        /// </summary>
        /// <remarks>See <see cref="Strrpos(string,object,int)"/> for details.</remarks>
        [ImplementsFunction("strripos"), EditorBrowsable(EditorBrowsableState.Never)]
        [return: CastToFalse]
        public static int Strripos(string haystack, string needle)
        {
            return Strrpos(haystack, needle, 0, true);
        }

        /// <summary>
        /// Retrieves the index of the last occurrence of the <paramref name="needle"/> in the <paramref name="haystack"/>
        /// (case insensitive).
        /// </summary>
        /// <remarks>See <see cref="Strrpos(string,object,int)"/> for details.</remarks>
        /// <exception cref="PhpException">Thrown if <paramref name="offset"/> is out of bounds or <paramref name="needle"/> is empty string.</exception>
        [ImplementsFunction("strripos"), EditorBrowsable(EditorBrowsableState.Never)]
        [return: CastToFalse]
        public static int Strripos(string haystack, object needle, int offset)
        {
            return Strrpos(haystack, needle, offset, true);
        }

        #endregion

        /// <summary>
        /// Implementation of <c>str[i]pos</c> functions.
        /// </summary>
        public static int Strpos(string haystack, object needle, int offset, bool ignoreCase)
        {
            if (String.IsNullOrEmpty(haystack)) return -1;

            if (offset < 0 || offset >= haystack.Length)
            {
                if (offset != haystack.Length)
                    PhpException.InvalidArgument("offset", LibResources.GetString("arg:out_of_bounds"));
                return -1;
            }

            string str_needle = PhpVariable.AsString(needle);
            if (str_needle != null)
            {
                if (str_needle == String.Empty)
                {
                    PhpException.InvalidArgument("needle", LibResources.GetString("arg:empty"));
                    return -1;
                }

                if (ignoreCase)
                    return haystack.IndexOf(str_needle, offset, StringComparison.OrdinalIgnoreCase);
                else
                    return haystack.IndexOf(str_needle, offset, StringComparison.Ordinal);
            }
            else
            {
                if (ignoreCase)
                    return haystack.IndexOf(ChrUnicode(Core.Convert.ObjectToInteger(needle) % 256), offset, StringComparison.OrdinalIgnoreCase);
                else
                    return haystack.IndexOf(ChrUnicode(Core.Convert.ObjectToInteger(needle) % 256), offset, StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// Implementation of <c>strr[i]pos</c> functions.
        /// </summary>
        public static int Strrpos(string haystack, object needle, int offset, bool ignoreCase)
        {
            if (String.IsNullOrEmpty(haystack)) return -1;

            int end = haystack.Length - 1;
            if (offset > end || offset < -end - 1)
            {
                PhpException.InvalidArgument("offset", LibResources.GetString("arg:out_of_bounds"));
                return -1;
            }

            string str_needle = PhpVariable.AsString(needle);
            if (offset < 0)
            {
                end += offset + (str_needle != null ? str_needle.Length : 1);
                offset = 0;
            }

            if (str_needle != null)
            {
                if (str_needle.Length == 0)
                {
                    PhpException.InvalidArgument("needle", LibResources.GetString("arg:empty"));
                    return -1;
                }

                if (ignoreCase)
                    return haystack.LastIndexOf(str_needle, end, end - offset + 1, StringComparison.OrdinalIgnoreCase);
                else
                    return haystack.LastIndexOf(str_needle, end, end - offset + 1, StringComparison.Ordinal);
            }
            else
            {
                if (ignoreCase)
                    return haystack.LastIndexOf(ChrUnicode(Core.Convert.ObjectToInteger(needle) % 256), end, end - offset + 1, StringComparison.OrdinalIgnoreCase);
                else
                    return haystack.LastIndexOf(ChrUnicode(Core.Convert.ObjectToInteger(needle) % 256), end, end - offset + 1, StringComparison.Ordinal);
            }
        }

        #endregion


        #region strstr, stristr, strchr, strrchr

        #region Stubs

        /// <summary>
        /// Finds first occurrence of a string.
        /// </summary>
        /// <param name="haystack">The string to search in.</param>
        /// <param name="needle">The substring to search for.</param>
        /// <returns>Part of <paramref name="haystack"/> string from the first occurrence of <paramref name="needle"/> to the end 
        /// of <paramref name="haystack"/> or null if <paramref name="needle"/> is not found.</returns>
        /// <exception cref="PhpException">Thrown when <paramref name="needle"/> is empty.</exception>
        [ImplementsFunction("strstr"), EditorBrowsable(EditorBrowsableState.Never)]
        [return: CastToFalse]
        public static string Strstr(string haystack, object needle)
        {
            return StrstrImpl(haystack, needle, false, false);
        }

        /// <summary>
        /// Finds first occurrence of a string.
        /// </summary>
        /// <param name="haystack">The string to search in.</param>
        /// <param name="needle">The substring to search for.</param>
        /// <param name="beforeNeedle">If TRUE, strstr() returns the part of the haystack before the first occurrence of the needle. </param>
        /// <returns>Part of <paramref name="haystack"/> string from the first occurrence of <paramref name="needle"/> to the end 
        /// of <paramref name="haystack"/> or null if <paramref name="needle"/> is not found.</returns>
        /// <exception cref="PhpException">Thrown when <paramref name="needle"/> is empty.</exception>
        [ImplementsFunction("strstr"), EditorBrowsable(EditorBrowsableState.Never)]
        [return: CastToFalse]
        public static string Strstr(string haystack, object needle, bool beforeNeedle /*= false*/)
        {
            return StrstrImpl(haystack, needle, false, beforeNeedle);
        }

        /// <summary>
        /// Finds first occurrence of a string. Alias of <see cref="Strstr(string,object)"/>.
        /// </summary>
        /// <remarks>See <see cref="Strstr(string,object)"/> for details.</remarks>
        /// <exception cref="PhpException">Thrown when <paramref name="needle"/> is empty.</exception>
        [ImplementsFunction("strchr"), EditorBrowsable(EditorBrowsableState.Never)]
        public static string Strchr(string haystack, object needle)
        {
            return StrstrImpl(haystack, needle, false, false);
        }

        /// <summary>
        /// Case insensitive version of <see cref="Strstr(string,object)"/>.
        /// </summary>
        /// <exception cref="PhpException">Thrown when <paramref name="needle"/> is empty.</exception>
        [ImplementsFunction("stristr"), EditorBrowsable(EditorBrowsableState.Never)]
        [return: CastToFalse]
        public static string Stristr(string haystack, object needle)
        {
            return StrstrImpl(haystack, needle, true, false);
        }

        /// <summary>
        /// Case insensitive version of <see cref="Strstr(string,object)"/>.
        /// </summary>
        /// <param name="haystack"></param>
        /// <param name="needle"></param>
        /// <param name="beforeNeedle">If TRUE, strstr() returns the part of the haystack before the first occurrence of the needle. </param>
        /// <exception cref="PhpException">Thrown when <paramref name="needle"/> is empty.</exception>
        [ImplementsFunction("stristr"), EditorBrowsable(EditorBrowsableState.Never)]
        [return: CastToFalse]
        public static string Stristr(string haystack, object needle, bool beforeNeedle /*= false*/)
        {
            return StrstrImpl(haystack, needle, true, beforeNeedle);
        }

        #endregion

        /// <summary>
        /// This function returns the portion of haystack  which starts at the last occurrence of needle  and goes until the end of haystack . 
        /// </summary>
        /// <param name="haystack">The string to search in.</param>
        /// <param name="needle">
        /// If needle contains more than one character, only the first is used. This behavior is different from that of strstr().
        /// If needle is not a string, it is converted to an integer and applied as the ordinal value of a character.
        /// </param>
        /// <returns>This function returns the portion of string, or FALSE  if needle  is not found.</returns>
        /// <exception cref="PhpException">Thrown when <paramref name="needle"/> is empty.</exception>
        [ImplementsFunction("strrchr"), EditorBrowsable(EditorBrowsableState.Never)]
        [return: CastToFalse]
        public static string Strrchr(string haystack, object needle)
        {
            if (haystack == null)
                return null;

            char charToFind;
            string str_needle;

            if ((str_needle = PhpVariable.AsString(needle)) != null)
            {
                if (str_needle.Length == 0)
                {
                    PhpException.InvalidArgument("needle", LibResources.GetString("arg:empty"));
                    return null;
                }

                charToFind = str_needle[0];
            }
            else
            {
                charToFind = ChrUnicode(Core.Convert.ObjectToInteger(needle) % 256)[0];
            }

            int index = haystack.LastIndexOf(charToFind);
            if (index < 0)
                return null;

            return haystack.Substring(index);
        }

        /// <summary>
        /// Implementation of <c>str[i]{chr|str}</c> functions.
        /// </summary>
        internal static string StrstrImpl(string haystack, object needle, bool ignoreCase, bool beforeNeedle)
        {
            if (haystack == null) return null;

            int index;
            string str_needle = PhpVariable.AsString(needle);
            if (str_needle != null)
            {
                if (str_needle == String.Empty)
                {
                    PhpException.InvalidArgument("needle", LibResources.GetString("arg:empty"));
                    return null;
                }

                if (ignoreCase)
                    index = haystack.ToLower().IndexOf(str_needle.ToLower());
                else
                    index = haystack.IndexOf(str_needle);
            }
            else
            {
                if (ignoreCase)
                    index = haystack.ToLower().IndexOf(ChrUnicode(Core.Convert.ObjectToInteger(needle) % 256).ToLower());
                else
                    index = haystack.IndexOf(ChrUnicode(Core.Convert.ObjectToInteger(needle) % 256));
            }

            return (index == -1) ? null : (beforeNeedle ? haystack.Substring(0, index) : haystack.Substring(index));
        }

        #endregion


        #region strpbrk

        /// <summary>
        /// Finds first occurence of any of given characters.
        /// </summary>
        /// <param name="haystack">The string to search in.</param>
        /// <param name="charList">The characters to search for given as a string.</param>
        /// <returns>Part of <paramref name="haystack"/> string from the first occurrence of any of characters contained
        /// in <paramref name="charList"/> to the end of <paramref name="haystack"/> or <B>null</B> if no character is
        /// found.</returns>
        /// <exception cref="PhpException">Thrown when <paramref name="charList"/> is empty.</exception>
        [ImplementsFunction("strpbrk")]
        [return: CastToFalse]
        public static string Strpbrk(string haystack, string charList)
        {
            if (charList == null)
            {
                PhpException.InvalidArgument("charList", LibResources.GetString("arg:empty"));
                return null;
            }

            if (haystack == null) return null;

            int index = haystack.IndexOfAny(charList.ToCharArray());
            return (index >= 0 ? haystack.Substring(index) : null);
        }

        #endregion


        #region strtolower, strtoupper, strlen

        /// <summary>
        /// Returns string with all alphabetic characters converted to lowercase. 
        /// Note that 'alphabetic' is determined by the current culture.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <returns>The lowercased string or empty string if <paramref name="str"/> is null.</returns>
        [ImplementsFunction("strtolower")]
        public static string ToLower(string str)
        {
            return (str == null) ? String.Empty : str.ToLower(Locale.GetCulture(Locale.Category.CType));
        }

        /// <summary>
        /// Returns string with all alphabetic characters converted to lowercase. 
        /// Note that 'alphabetic' is determined by the current culture.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <returns>The lowercased string or empty string if <paramref name="str"/> is null.</returns>
        [ImplementsFunction("strtoupper")]
        public static string ToUpper(string str)
        {
            return (str == null) ? String.Empty : str.ToUpper(Locale.GetCulture(Locale.Category.CType));
        }

        /// <summary>
        /// Returns the length of a string.
        /// </summary>
        /// <param name="x">The string (either <see cref="string"/> or <see cref="PhpBytes"/>).</param>
        /// <returns>The length of the string.</returns>
        [ImplementsFunction("strlen"/*, FunctionImplOptions.Special*/)]
        [PureFunction]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static int Length(object x)
        {
            string str = x as string;
            if (str != null) return str.Length;

            PhpBytes bytes = x as PhpBytes;
            if (bytes != null) return bytes.Length;

            PhpString phpstr = x as PhpString;
            if (phpstr != null) return phpstr.Length;

            return Core.Convert.ObjectToString(x).Length;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Converts object <paramref name="obj"/> to <see cref="System.String"/>.
        /// In case if bunary string, the conversion routine respects given <paramref name="charSet"/>.
        /// </summary>
        /// <param name="obj">Object to be converted.</param>
        /// <param name="charSet">Character set used to encode binary string to <see cref="System.String"/>.</param>
        /// <returns>String representation of <paramref name="obj"/>.</returns>
        internal static string ObjectToString(object obj, string charSet)
        {
            if (obj != null && obj.GetType() == typeof(PhpBytes))
            {
                var encoding = Encoding.GetEncoding(charSet);
                if (encoding == null)
                    throw new ArgumentException(string.Format(Strings.arg_invalid_value, "charSet", charSet), "charSet");

                return encoding.GetString(((PhpBytes)obj).ReadonlyData);
            }
            else
            {
                return Core.Convert.ObjectToString(obj);
            }
        }

        #endregion
    }
}
