/*

 Copyright (c) 2004-2006 Tomas Matousek and Ladislav Prosek.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Threading;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;

#if SILVERLIGHT
using PHP.CoreCLR;
using DirectoryEx = PHP.CoreCLR.DirectoryEx;
#else
using System.Collections.Specialized; // case-insensitive hashtable
using System.Runtime.Serialization;
using DirectoryEx = System.IO.Directory;
#endif

namespace PHP.Core
{
    #region Debug

    /// <summary>
    /// Support for debugging (replaces <see cref="System.Diagnostics.Debug"/>).
    /// </summary>
    [DebuggerNonUserCode]
    public static partial class Debug
    {
        /// <summary>
        /// Thrown when an assertion fails.
        /// </summary>
        [Serializable]
        public class AssertException : ApplicationException
        {
            public AssertException(string message) : base(message) { }
#if !SILVERLIGHT
            protected AssertException(SerializationInfo info, StreamingContext context) : base(info, context) { }
#endif
        }

        /// <summary>
        /// An assertion.
        /// </summary>
        /// <param name="condition">The condition.</param>
        [Conditional("DEBUG")]
        public static void Assert(bool condition)
        {
            if (!condition)
                Fail(null);
        }

        /// <summary>
        /// Assertion with a message.
        /// </summary>
        /// <param name="condition">The condition.</param>
        /// <param name="message">The message.</param>
        [Conditional("DEBUG")]
        public static void Assert(bool condition, string message)
        {
            if (!condition)
                Fail(message);
        }

        /// <summary>
        /// Asserts exactly specified number of the references to be non-null.
        /// </summary>
        [Conditional("DEBUG")]
        public static void AssertNonNull(int count, params object[] references)
        {
            Debug.Assert(references != null);

            foreach (object reference in references)
                if (reference != null) count--;

            Debug.Assert(count == 0);
        }

        /// <summary>
        /// Asserts that the array is non-null and doesn't contain null references.
        /// </summary>
        [Conditional("DEBUG")]
        public static void AssertAllNonNull(params object[] array)
        {
            Debug.Assert(array != null);

            for (int i = 0; i < array.Length; i++)
                Debug.Assert(array[i] != null);
        }

        /// <summary>
        /// Failed assertion.
        /// </summary>
        [Conditional("DEBUG")]
        public static void Fail()
        {
            throw new AssertException("Assertion failed!");
        }

        /// <summary>
        /// Failed assertion with a message.
        /// </summary>
        /// <param name="message">The message.</param>
        [Conditional("DEBUG")]
        public static void Fail(string message)
        {
            throw new AssertException("Assertion failed: " + message);
        }

        /// <summary>
        /// Writes a formatted message to the log file.
        /// </summary>
        /// <param name="source">The source Phalanger component.</param>
        /// <param name="format">A formatting string.</param>
        /// <param name="arg1">A formatted argument.</param>
        [Conditional("DEBUG")]
        public static void WriteLine(string source, string format, object arg1)
        {
            WriteLine(source, String.Format(format, arg1));
        }

        /// <summary>
        /// Writes a formatted message to the log file.
        /// </summary>
        /// <param name="source">The source Phalanger component.</param>
        /// <param name="format">A formatting string.</param>
        /// <param name="arg1">A formatted argument.</param>
        /// <param name="arg2">A formatted argument.</param>
        [Conditional("DEBUG")]
        public static void WriteLine(string source, string format, object arg1, object arg2)
        {
            WriteLine(source, String.Format(format, arg1, arg2));
        }

        /// <summary>
        /// Writes a formatted message to the log file.
        /// </summary>
        /// <param name="source">The source Phalanger component.</param>
        /// <param name="format">A formatting string.</param>
        /// <param name="arg1">A formatted argument.</param>
        /// <param name="arg2">A formatted argument.</param>
        /// <param name="arg3">A formatted argument.</param>
        [Conditional("DEBUG")]
        public static void WriteLine(string source, string format, object arg1, object arg2, object arg3)
        {
            WriteLine(source, String.Format(format, arg1, arg2, arg3));
        }

        /// <summary>
        /// Writes a formatted message to the log file.
        /// </summary>
        /// <param name="source">The source Phalanger component.</param>
        /// <param name="format">A formatting string.</param>
        /// <param name="args">Formatted arguments.</param>
        [Conditional("DEBUG")]
        public static void WriteLine(string source, string format, params object[] args)
        {
            WriteLine(source, String.Format(format, args));
        }

        /// <summary>
        /// Writes a message to the log file.
        /// </summary>
        /// <param name="source">The source Phalanger component.</param>
        /// <param name="message">The message.</param>
        [Conditional("DEBUG")]
        public static void WriteLine(string source, string message)
        {
            string msg = String.Format("[{0:8}]\t{1}\t", source, DateTime.Now.ToString("T")) + message;
            System.Diagnostics.Debug.WriteLine(msg);
        }


#if DEBUG

        /// <summary>
        /// Runs unit tests (methods marked with <see cref="TestAttribute"/>) included in the specified assembly.
        /// </summary>
        public static void UnitTest(Assembly/*!*/ assembly, TextWriter/*!*/ output)
        {
            ScriptContext.CurrentContext.DisableErrorReporting();

            foreach (MethodInfo method in GetTestMethods(assembly))
            {
                output.Write("Testing {0}.{1} ... ", method.DeclaringType.Name, method.Name);

                Debug.Assert(method.GetParameters().Length == 0 && method.ReturnType == Emit.Types.Void && method.IsStatic);

                try
                {
                    method.Invoke(null, ArrayUtils.EmptyStrings);
                    output.WriteLine("OK.");
                }
                catch (TargetInvocationException)
                {
                    output.WriteLine("Failed.");
                }
            }
            output.WriteLine("Done.");
        }

        private static IEnumerable<MethodInfo> GetTestMethods(Assembly/*!*/ assembly)
        {
            // scans assembly for test methods:
            foreach (Type type in assembly.GetTypes())
            {
                foreach (MethodInfo method in type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static))
                {
                    object[] attrs = method.GetCustomAttributes(typeof(TestAttribute), false);
                    if (attrs.Length == 1)
                    {
                        if (((TestAttribute)attrs[0]).One)
                        {
                            //result = new ArrayList();
                            //result.Add(method);
                            //return result;
                        }
                        else
                        {
                            yield return method;
                        }
                    }
                }
            }
        }

        public static void UnitTestCore()
        {
            UnitTest(Assembly.GetExecutingAssembly(), Console.Out);
        }

#endif

    }

    #endregion

    #region Strings

    /// <summary>
    /// Unicode category group.
    /// </summary>
    public enum UnicodeCategoryGroup
    {
        None,
        Separators,
        Symbols,
        Punctuations,
        Numbers,
        Marks,
        Letters,
        OtherCharacters
    }

    /// <summary>
    /// Utilities manipulating strings.
    /// </summary>
    public static class StringUtils
    {
        /// <summary>
        /// Increments a string in a manner of Perl (and also PHP). 
        /// </summary>
        /// <param name="str">The string to be incremented.</param>
        /// <returns>The incremented string. </returns>
        /// <remarks>
        /// <para>Single characters are incremented such that '0' changes to '1', ..., '8' to '9' and '9' to '0' 
        /// with a carry, 'a' changes to 'b', ..., 'z' to 'a' (with a carry) and finally 'A' to 'B', ..., 'Z' to 'A' 
        /// (with a carry). Other characters remain unchanged and generate no carry.</para>
        /// <para>Characters of the <paramref name="str"/> string are incremented starting from the last one
        /// advancing to the beginning of the string and ending when there is no carry generated or no character 
        /// available (i.e. we proceeded the first character of the <paramref name="str"/> string). In latter 
        /// case appropriate character ('1', 'a' or 'A') is prepended before the result.</para>
        /// </remarks>
        public static string Increment(string str)
        {
            if (str == null) return "0";

            int length = str.Length;

            // make a copy of a string and allocate one more character to handler "overflow":
            StringBuilder result = new StringBuilder(str, 0, length, length + 1);

            // if length of the string is zero, then '1' will be returned:
            char c = '9';

            // while there is a carry flag and not all characters of the string 
            // are processed do increment the current character:
            for (int i = length - 1; i >= 0; i--)
            {
                c = str[i];
                if (c >= '0' && c <= '8' || c >= 'a' && c <= 'y' || c >= 'A' && c <= 'Y')
                {
                    result[i] = (char)((int)c + 1);
                    break;
                }
                switch (c)
                {
                    case '9': result[i] = '0'; continue;
                    case 'z': result[i] = 'a'; continue;
                    case 'Z': result[i] = 'A'; continue;
                }
                break;
            }

            // if the last incremented character is '9', 'z' or 'Z' then we must be at the beginning of the string;
            // the string is shifted to the right by one and the first charater is set:
            switch (c)
            {
                case '9': result.Insert(0, new char[] { '1' }); break;
                case 'z': result.Insert(0, new char[] { 'a' }); break;
                case 'Z': result.Insert(0, new char[] { 'A' }); break;
            }

            return result.ToString();
        }

#if DEBUG
        /// <summary>
        /// Unit test.
        /// </summary>
        [Test]
        static void TestIncrement()
        {
            string[] cases = new string[] { null, "", "z", "ZZ[Z9ZzZ", "ZZz" };
            string[] results = new string[] { "0", "1", "aa", "ZZ[A0AaA", "AAAa" };

            for (int i = 0; i < cases.Length; i++)
                Debug.Assert(StringUtils.Increment(cases[i]) == results[i]);
        }
#endif

        public static string/*!*/ AddCSlashes(string/*!*/ str)
        {
            return AddCSlashes(str, true, true, true);
        }

        public static string/*!*/ AddCSlashes(string/*!*/ str, bool singleQuotes, bool doubleQuotes)
        {
            return AddCSlashes(str, singleQuotes, doubleQuotes, true);
        }

        /// <summary>
        /// Adds slashes before characters '\\', '\0', '\'' and '"'.
        /// </summary>
        /// <param name="str">The string to add slashes in.</param>
        /// <param name="doubleQuotes">Whether to slash double quotes.</param>
        /// <param name="singleQuotes">Whether to slash single quotes.</param>
        /// <param name="nul">Whether to slash '\0' character.</param>
        /// <returns>The slashed string.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="str"/> is a <B>null</B> reference.</exception>
        public static string/*!*/ AddCSlashes(string/*!*/ str, bool singleQuotes, bool doubleQuotes, bool nul)
        {
            if (str == null) throw new ArgumentNullException("str");

            StringBuilder result = new StringBuilder(str.Length);

            string double_quotes = doubleQuotes ? "\\\"" : "\"";
            string single_quotes = singleQuotes ? @"\'" : "'";
            string slashed_nul = nul ? "\\0" : "\0";

            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                switch (c)
                {
                    case '\\': result.Append(@"\\"); break;
                    case '\0': result.Append(slashed_nul); break;
                    case '\'': result.Append(single_quotes); break;
                    case '"': result.Append(double_quotes); break;
                    default: result.Append(c); break;
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Escape characters in toExcape in given string with given escape character.
        /// </summary>
        /// <param name="str">String to escape</param>
        /// <param name="toEscape">Characters to be escaped</param>
        /// <param name="escape">Escape character</param>
        /// <returns>Escaped string.</returns>
        public static string/*!*/ EscapeStringCustom(string/*!*/str, char[]/*!*/toEscape, char escape)
        {
            if (str == null) throw new ArgumentNullException("str");
            if (toEscape == null) throw new ArgumentNullException("toEscape");

            StringBuilder result = new StringBuilder(str.Length);

            Dictionary<char, bool> charsToEscape = new Dictionary<char, bool>(toEscape.Length);
            foreach (char c in toEscape) charsToEscape.Add(c, true);

            foreach (char c in str)
            {
                if (charsToEscape.ContainsKey(c)) result.Append(escape);

                result.Append(c);
            }

            return result.ToString();
        }

        /// <summary>
        /// Strips slashes from a string.
        /// </summary>
        /// <param name="str">String.</param>
        /// <returns>
        /// String where slashes are striped away.
        /// Slashed characters with special meaning ("\0") are replaced with their special value.
        /// </returns>
        public static string/*!*/ StripCSlashes(string/*!*/ str)
        {
            if (str == null) throw new ArgumentNullException("str");
            if (str == "") return "";

            StringBuilder result = new StringBuilder(str.Length);

            int i = 0;
            while (i < str.Length - 1)
            {
                if (str[i] == '\\')
                {
                    if (str[i + 1] == '0')
                        result.Append('\0');
                    else
                        result.Append(str[i + 1]); // PHP strips all slashes, not only quotes and slash

                    i += 2;
                }
                else
                {
                    result.Append(str[i]);
                    i++;
                }
            }
            if (i < str.Length && str[i] != '\\')
                result.Append(str[i]);

            return result.ToString();
        }

        /// <summary>
        /// Adds slash before '\0' character and duplicates apostrophes.
        /// </summary>
        /// <param name="str">The string to add slashes in.</param>
        /// <returns>The slashed string.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="str"/> is a <B>null</B> reference.</exception>
        public static string/*!*/ AddDbSlashes(string/*!*/ str)
        {
            if (str == null) throw new ArgumentNullException("str");

            StringBuilder result = new StringBuilder(str.Length);

            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                switch (c)
                {
                    case '\0': result.Append('\\'); result.Append('0'); break;
                    case '\'': result.Append('\''); result.Append('\''); break;
                    default: result.Append(c); break;
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Replaces slashed 0 with null character ('\0') and double apostrophe with single apostrophe. 
        /// </summary>
        /// <param name="str">String.</param>
        /// <returns>String with replaced characters.</returns>
        public static string/*!*/ StripDbSlashes(string/*!*/ str)
        {
            if (str == null) throw new ArgumentNullException("str");

            StringBuilder result = new StringBuilder(str.Length);

            int i = 0;
            while (i < str.Length - 1)
            {
                if (str[i] == '\\' && str[i + 1] == '0')
                {
                    result.Append('\0');
                    i += 2;
                }
                else if (str[i] == '\'' && str[i + 1] == '\'')
                {
                    result.Append('\'');
                    i += 2;
                }
                else
                {
                    result.Append(str[i]);
                    i++;
                }
            }
            if (i < str.Length)
                result.Append(str[i]);

            return result.ToString();
        }

        /// <summary>
        /// Converts a string of bytes into hexadecimal representation.
        /// </summary>
        /// <param name="bytes">The string of bytes.</param>
        /// <param name="separator">The separator.</param>
        /// <returns>Concatenation of hexadecimal values of bytes of <paramref name="bytes"/> separated by <paramref name="separator"/>.</returns>
        public static string BinToHex(byte[] bytes, string separator)
        {
            if (bytes == null) return null;
            if (bytes.Length == 0) return String.Empty;
            if (separator == null) separator = String.Empty;

            int c;
            int length = bytes.Length;
            int sep_length = separator.Length;
            int res_length = length * (2 + sep_length);

            const string hex_digs = "0123456789abcdef";

            // prepares characters which will be appended to the result for each byte:
            char[] chars = new char[2 + sep_length];
            separator.CopyTo(0, chars, 2, sep_length);

            // prepares the result:
            StringBuilder result = new StringBuilder(res_length, res_length);

            // appends characters to the result for each byte:
            for (int i = 0; i < length - 1; i++)
            {
                c = (int)bytes[i];
                chars[0] = hex_digs[(c & 0xf0) >> 4];
                chars[1] = hex_digs[(c & 0x0f)];
                result.Append(chars);
            }

            // the last byte:
            c = (int)bytes[length - 1];
            result.Append(hex_digs[(c & 0xf0) >> 4]);
            result.Append(hex_digs[(c & 0x0f)]);

            return result.ToString();
        }

        /// <summary>
        /// Replaces specified characters in a string with another ones.
        /// </summary>
        /// <param name="str">A string where to do the replacement.</param>
        /// <param name="from">Characters to be replaced.</param>
        /// <param name="to">Characters to replace those in <paramref name="from"/> with.</param>
        /// <remarks>Replaces characters one by one.</remarks>
        internal static string Replace(string str, string from, string to)
        {
            Debug.Assert(from != null && to != null && from.Length == to.Length);

            StringBuilder result = new StringBuilder(str);

            for (int i = 0; i < from.Length; i++)
                result.Replace(from[i], to[i]);

            return result.ToString();
        }

        /// <summary>
        /// Finds an index of the first character in which two specified strings differs.
        /// </summary>
        /// <param name="str1">The first string.</param>
        /// <param name="str2">The second string.</param>
        /// <param name="ignoreCase">Whether to ignore case.</param>
        /// <returns>The index of the character or the length of the shortest string one is substring of the other.</returns>
        public static int FirstDifferent(string str1, string str2, bool ignoreCase)
        {
            // GENERICS: replace where used for StartsWith
            return ignoreCase ? FirstDifferentIgnoreCase(str1, str2) : FirstDifferentCaseSensitive(str1, str2);
        }

        private static int FirstDifferentIgnoreCase(string str1, string str2)
        {
            CultureInfo currentCulture = null;

            int length = Math.Min(str1.Length, str2.Length);
            char c1, c2;
            for (int i = 0; i < length; i++)
            {
                // check the characters case insensitively first, ToLower() is expensive
                // initialize the currentCulture lazily, CultureInfo.CurrentCulture is expensive too

                if ((c1 = str1[i]) != (c2 = str2[i]) &&
                    (Char.ToLower(c1, currentCulture ?? (currentCulture = CultureInfo.CurrentCulture)) != Char.ToLower(c2, currentCulture)))
                {
                    return i;
                }
            }
            return length;
        }
        private static int FirstDifferentCaseSensitive(string str1, string str2)
        {
            int length = Math.Min(str1.Length, str2.Length);
            for (int i = 0; i < length; i++)
            {
                if (str1[i] != str2[i])
                {
                    return i;
                }
            }
            return length;
        }

        public static int FirstDifferent(char[] str1, int startIndex1, string str2, int startIndex2, bool ignoreCase)
        {
            int i = startIndex1;
            int j = startIndex2;
            int result = 0;
            int limit = Math.Min(str1.Length - startIndex1, str2.Length - startIndex2);
            if (ignoreCase)
            {
                while (result < limit)
                {
                    if (Char.ToLower(str1[i++]) != Char.ToLower(str2[j++])) return result;
                    result++;
                }
            }
            else
            {
                while (result < limit)
                {
                    if (str1[i++] != str2[j++]) return result;
                    result++;
                }
            }
            return result;
        }

        internal static void StringBuilderAppend(PHP.Core.Parsers.PhpStringBuilder/*!*/ dst, StringBuilder/*!*/ src, int startIndex, int length)
        {
            dst.Append(src.ToString(startIndex, length));
        }

        public static bool IsAsciiString(string/*!*/ str)
        {
            return IsAsciiString(str, 0, str.Length);
        }

        public static bool IsAsciiString(string/*!*/ str, int start, int length)
        {
            if (str == null)
                throw new ArgumentNullException("str");
            if (start < 0)
                throw new ArgumentOutOfRangeException("start");
            if (length < 0 || length > str.Length - start)
                throw new ArgumentOutOfRangeException("length");

            for (int i = start; i < start + length; i++)
            {
                if (str[i] > (char)127)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Adds characters of a Unicode designation parsed from the specified string.
        /// </summary>
        /// <param name="str">String containing the property.</param>
        /// <param name="pos">Position where the property code starts.</param>
        /// <param name="group">Group.</param>
        /// <param name="category">Category.</param>
        /// <returns>Length of the parsed property code (0 to 2).</returns>
        public static int ParseUnicodeDesignation(string/*!*/ str, int pos, out UnicodeCategoryGroup group,
          out UnicodeCategory category)
        {
            if (str == null)
                throw new ArgumentNullException("str");
            if (pos < 0)
                throw new ArgumentOutOfRangeException("pos");

            category = 0;
            group = (UnicodeCategoryGroup)'\0';

            if (pos == str.Length) return 0;

            switch (str[pos])
            {
                case 'C': // Other 
                    if (pos + 1 < str.Length)
                    {
                        switch (str[pos + 1])
                        {
                            case 'c': category = UnicodeCategory.Control; return 2;
                            case 'f': category = UnicodeCategory.Format; return 2;
                            case 'n': category = UnicodeCategory.OtherNotAssigned; return 2;
                            case 'o': category = UnicodeCategory.PrivateUse; return 2;
                            case 's': category = UnicodeCategory.Surrogate; return 2;
                        }
                    }
                    group = UnicodeCategoryGroup.OtherCharacters;
                    return 1;

                case 'L': // Letter 
                    if (pos + 1 < str.Length)
                    {
                        switch (str[pos + 1])
                        {
                            case 'l': category = UnicodeCategory.LowercaseLetter; return 2;
                            case 'm': category = UnicodeCategory.ModifierLetter; return 2;
                            case 'o': category = UnicodeCategory.OtherLetter; return 2;
                            case 't': category = UnicodeCategory.TitlecaseLetter; return 2;
                            case 'u': category = UnicodeCategory.UppercaseLetter; return 2;
                        }
                    }
                    group = UnicodeCategoryGroup.Letters;
                    return 1;

                case 'M': // Mark 
                    if (pos + 1 < str.Length)
                    {
                        switch (str[pos + 1])
                        {
                            case 'c': category = UnicodeCategory.SpacingCombiningMark; return 2;
                            case 'e': category = UnicodeCategory.EnclosingMark; return 2;
                            case 'n': category = UnicodeCategory.NonSpacingMark; return 2;
                        }
                    }
                    group = UnicodeCategoryGroup.Marks;
                    return 1;

                case 'N': // Number 
                    if (pos + 1 < str.Length)
                    {
                        switch (str[pos + 1])
                        {
                            case 'd': category = UnicodeCategory.DecimalDigitNumber; return 2;
                            case 'l': category = UnicodeCategory.LetterNumber; return 2;
                            case 'o': category = UnicodeCategory.OtherNumber; return 2;
                        }
                    }
                    group = UnicodeCategoryGroup.Numbers;
                    return 1;

                case 'P': // Punctuation 
                    if (pos + 1 < str.Length)
                    {
                        switch (str[pos + 1])
                        {
                            case 'c': category = UnicodeCategory.ConnectorPunctuation; return 2;
                            case 'd': category = UnicodeCategory.DashPunctuation; return 2;
                            case 'e': category = UnicodeCategory.ClosePunctuation; return 2;
                            case 'f': category = UnicodeCategory.FinalQuotePunctuation; return 2;
                            case 'i': category = UnicodeCategory.InitialQuotePunctuation; return 2;
                            case 'o': category = UnicodeCategory.OtherPunctuation; return 2;
                            case 's': category = UnicodeCategory.OpenPunctuation; return 2;
                        }
                    }
                    group = UnicodeCategoryGroup.Punctuations;
                    return 1;

                case 'S': // Symbol 
                    if (pos + 1 < str.Length)
                    {
                        switch (str[pos + 1])
                        {
                            case 'c': category = UnicodeCategory.CurrencySymbol; return 2;
                            case 'k': category = UnicodeCategory.ModifierSymbol; return 2;
                            case 'm': category = UnicodeCategory.MathSymbol; return 2;
                            case 'o': category = UnicodeCategory.OtherSymbol; return 2;
                        }
                    }
                    group = UnicodeCategoryGroup.Symbols;
                    return 1;

                case 'Z': // Separator 
                    if (pos + 1 < str.Length)
                    {
                        switch (str[pos + 1])
                        {
                            case 'l': category = UnicodeCategory.LineSeparator; return 2;
                            case 'p': category = UnicodeCategory.ParagraphSeparator; return 2;
                            case 's': category = UnicodeCategory.SpaceSeparator; return 2;
                        }
                    }
                    group = UnicodeCategoryGroup.Separators;
                    return 1;
            }
            return 0;
        }

        internal static bool IsWhitespace(string/*!*/ str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                if (!Char.IsWhiteSpace(str[i]))
                    return false;
            }
            return true;
        }


        internal static bool IsWhiteSpace(byte b)
        {
            return b == ' ' || (b >= '\t' && b <= '\r') || b == '\u00a0' || b == '\u0085';
        }

        /// <summary>
        /// Checks if binary data can be converted a number
        /// </summary>
        /// <param name="bytes">The bytes to checked.</param>
        /// <returns>Returns true if bytes can be converted to a number.</returns>
        internal static bool IsConvertableToNumber(byte[] bytes)
        {
            int state = 0;
            byte b;

            for (int i = 0; i < bytes.Length; i++)
            {
                b = bytes[i];

                switch (state)
                {
                    case 0: // expecting whitespaces to be skipped
                        {
                            if (!IsWhiteSpace(b))
                            {
                                state = 1;
                                goto case 1;
                            }
                            break;
                        }

                    case 1:
                        {

                            if (b >= '0' && b <= '9')
                            {
                                //it's a number
                                return true;
                            }

                            if (b == '-')//sign
                            {
                                state = 2;
                                break;
                            }

                            if (b == '+')//sign
                            {
                                state = 2;
                                break;
                            }

                            // switch to decimals in next turn:
                            if (b == '.')
                            {
                                state = 2;
                                break;
                            }

                            //it's not a valid number
                            return false;

                        }

                    case 2:
                        {
                            if (b >= '0' && b <= '9')
                            {
                                //it's a number
                                return true;
                            }

                            //it's not a valid number
                            return false;
                        }
                }
            }

            return false;
        }

        /// <summary>
        /// <see cref="Char.ConvertFromUtf32"/> is missing from Mono so we must implement it by ourselves.
        /// </summary>
        internal static string Utf32ToString(int codePoint)
        {
            // MONO BUG:
            // replace by Char.ConvertFromUtf32 when implemented in Mono

            if (codePoint < 0x10000)
                return char.ToString((char)((ushort)codePoint));

            codePoint -= 0x10000;
            return new string(new char[] { (char)((codePoint / 0x400) + 0xd800), (char)((codePoint % 0x400) + 0xdc00) });
        }

        #region Uniform Wrappers

        // NOTE: It is not multibyte safe to work with these wrappers.

        public abstract class UniformWrapper
        {
            public abstract char this[int index] { get; }
            public abstract int Length { get; }
            public abstract object/*!*/ Value { get; }

            public virtual bool HasBuilder { get { return false; } }

            public virtual UniformBuilder GetBuilder()
            {
                throw new NotSupportedException();
            }

            public virtual UniformBuilder GetBuilder(int capacity)
            {
                throw new NotSupportedException();
            }

            #region Implicit Casts

            public static implicit operator UniformWrapper(string str)
            {
                return (str != null) ? new StringWrapper(str) : null;
            }

            public static implicit operator UniformWrapper(byte[] bytes)
            {
                return (bytes != null) ? new BytesWrapper(bytes) : null;
            }

            public static implicit operator UniformWrapper(char[] chars)
            {
                return (chars != null) ? new CharsWrapper(chars) : null;
            }

            public static implicit operator UniformWrapper(StringBuilder builder)
            {
                return (builder != null) ? new StringBuilderWrapper(builder) : null;
            }

            public static implicit operator string(UniformWrapper wrapper)
            {
                return (wrapper != null) ? (string)wrapper.Value : null;
            }

            public static implicit operator byte[](UniformWrapper wrapper)
            {
                return (wrapper != null) ? (byte[])wrapper.Value : null;
            }

            public static implicit operator char[](UniformWrapper wrapper)
            {
                return (wrapper != null) ? (char[])wrapper.Value : null;
            }

            public static implicit operator StringBuilder(UniformWrapper wrapper)
            {
                return (wrapper != null) ? (StringBuilder)wrapper.Value : null;
            }

            #endregion
        }

        public abstract class UniformBuilder
        {
            public abstract UniformWrapper ToWrapper();
            public abstract void Append(char c);

            public void Append(string s)
            {
                foreach (char c in s)
                    Append(c);
            }
        }

        public sealed class BytesWrapper : UniformWrapper
        {
            private byte[]/*!*/ bytes;

            public override object Value
            {
                get { return bytes; }
            }

            public override char this[int index]
            {
                get { return (char)bytes[index]; }
            }

            public override int Length
            {
                get { return bytes.Length; }
            }

            public override bool HasBuilder { get { return true; } }

            public BytesWrapper(byte[]/*!*/ bytes)
            {
                Debug.Assert(bytes != null);
                this.bytes = bytes;
            }

            public override UniformBuilder GetBuilder()
            {
                return new Builder(new MemoryStream());
            }

            public override UniformBuilder GetBuilder(int capacity)
            {
                return new Builder(new MemoryStream(capacity));
            }

            #region Nested Class: Builder

            public sealed class Builder : UniformBuilder
            {
                private MemoryStream stream;

                public Builder(MemoryStream stream)
                {
                    this.stream = stream;
                }

                public override void Append(char c)
                {
                    stream.WriteByte((byte)c);
                }

                public override UniformWrapper ToWrapper()
                {
                    return new BytesWrapper(stream.ToArray());
                }
            }

            #endregion
        }

        public sealed class CharsWrapper : UniformWrapper
        {
            private char[]/*!*/ chars;

            public override object Value
            {
                get { return chars; }
            }

            public override char this[int index] { get { return chars[index]; } }

            public override int Length { get { return chars.Length; } }

            public CharsWrapper(char[]/*!*/ chars)
            {
                Debug.Assert(chars != null);
                this.chars = chars;
            }
        }

        public sealed class StringWrapper : UniformWrapper
        {
            private string/*!*/ str;

            public override char this[int index] { get { return str[index]; } }

            public override int Length { get { return str.Length; } }

            public override bool HasBuilder { get { return true; } }

            public override object Value
            {
                get { return str; }
            }

            public StringWrapper(string str)
            {
                Debug.Assert(str != null);
                this.str = str;
            }

            public override UniformBuilder GetBuilder()
            {
                return new Builder(new StringBuilder());
            }

            public override UniformBuilder GetBuilder(int capacity)
            {
                return new Builder(new StringBuilder(capacity));
            }

            #region Nested Class: Builder

            public sealed class Builder : UniformBuilder
            {
                private System.Text.StringBuilder builder;

                public Builder(System.Text.StringBuilder builder)
                {
                    this.builder = builder;
                }

                public override void Append(char c)
                {
                    builder.Append(c);
                }

                public override UniformWrapper ToWrapper()
                {
                    return new StringWrapper(builder.ToString());
                }
            }

            #endregion
        }

        public sealed class StringBuilderWrapper : UniformWrapper
        {
            private StringBuilder/*!*/ builder;

            public override char this[int index] { get { return builder[index]; } }

            public override int Length { get { return builder.Length; } }

            public StringBuilderWrapper(StringBuilder builder)
            {
                Debug.Assert(builder != null);
                this.builder = builder;
            }

            public override object Value
            {
                get { return builder; }
            }
        }

        #endregion

        internal static string ToClsCompliantIdentifier(string/*!*/ name)
        {
            if (String.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            StringBuilder result = new StringBuilder(name.Length);

            if (name[0] >= 'a' && name[0] <= 'z' || name[0] >= 'A' && name[0] <= 'Z' || name[0] == '_')
                result.Append(name[0]);

            for (int i = 1; i < name.Length; i++)
            {
                if (name[i] >= '0' && name[i] <= '9' || name[i] >= 'a' && name[i] <= 'z' || name[i] >= 'A' && name[i] <= 'Z'
                    || name[i] == '_')
                {
                    result.Append(name[i]);
                }
            }

            return (result.Length > 0) ? result.ToString() : "_";
        }

        /// <summary>
        /// Compare two strings ordinally (which is ok for ascii strings) case insensitively.
        /// </summary>
        /// <param name="self">First string.</param>
        /// <param name="str">Second string.</param>
        /// <returns>True iff two given strings are equals when using <see cref="StringComparison.OrdinalIgnoreCase"/>.</returns>
        public static bool EqualsOrdinalIgnoreCase(this string self, string str)
        {
            return string.Equals(self, str, StringComparison.OrdinalIgnoreCase);
        }


        /// <summary>
        /// Returns last character of string or -1 if empty
        /// </summary>
        /// <param name="str">String</param>
        /// <returns>Last character of string or -1 if empty</returns>
        public static int LastCharacter(this string/*!*/ str)
        {
            return str.Length == 0 ? -1 : str[str.Length - 1];
        }
    }

    #endregion

    #region Collections

    /// <summary>
    /// Type of an item in dictionary collection.
    /// </summary>
    public enum DictionaryItemType { Keys, Values, Entries };

    /// <summary>
    /// Auxiliary class which represents invalid key or value.
    /// </summary>
    [Serializable]
    public class InvalidItem : ISerializable
    {
        /// <summary>Prevents instantiation.</summary>
        private InvalidItem() { }

        /// <summary>Invalid item singleton.</summary>
        internal static readonly InvalidItem Default = new InvalidItem();

        #region Serialization
#if !SILVERLIGHT

        /// <include file='Doc/Common.xml' path='/docs/method[@name="GetObjectData"]/*'/>
        [System.Security.SecurityCritical]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.SetType(typeof(InvalidItemDeserializer));
        }

        [Serializable]
        private class InvalidItemDeserializer : IObjectReference
        {
            public Object GetRealObject(StreamingContext context)
            {
                return InvalidItem.Default;
            }
        }

#endif
        #endregion
    }

    #region WeakCache

    /// <summary>
    /// Maps real objects to their associates (of type <typeparamref name="T"/>).
    /// </summary>
    /// <typeparam name="T">The type of objects associated with real objects.</typeparam>
    /// <remarks>
    /// The cache should store only the real objects that are alive, i.e. reachable from GC roots.
    /// It is assumed that there exists a (strong) reference from instances of <typeparamref name="T"/>
    /// to their associated real objects. Therefore holding the associates in this cache using strong
    /// references only would not work and a more sophisticated pattern is employed.
    /// </remarks>
    internal class WeakCache<T> : Dictionary<object, object>
    {
        #region WeakCacheKey
#if !SILVERLIGHT
        /// <summary>
        /// Weak reference with overriden <see cref="GetHashCode"/> and <see cref="Equals"/>.
        /// </summary>
        /// <remarks>
        /// Delegating <see cref="GetHashCode"/> to the target and confirming equality with the target
        /// makes it possible to use the target (real object) as key when performing dictionary lookups.
        /// There's not need to create a new <see cref="WeakCacheKey"/> just to call something like
        /// <see cref="TryGetValue"/>.
        /// </remarks>
        private class WeakCacheKey : WeakReference
        {
            private int hashCode;

            internal WeakCacheKey(object obj)
                : base(obj, false)
            {
                this.hashCode = obj.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj == null) return false;
                if (obj != this && (!IsAlive || !Object.ReferenceEquals(obj, Target))) return false;

                return true;
            }

            public override int GetHashCode()
            {
                return hashCode;
            }
        }

#else

        private class WeakCacheKey
        {
            private WeakReference _ref;

            private int hashCode;

            internal WeakCacheKey(object obj)
            {
                _ref = new WeakReference(obj,false);

                this.hashCode = obj.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj == null) return false;
                if (obj != this && (!_ref.IsAlive || !Object.ReferenceEquals(obj, _ref.Target))) return false;

                return true;
            }

            public override int GetHashCode()
            {
                return hashCode;
            }

            public bool IsAlive
            {
                get
                { return _ref.IsAlive; }
            }


            public object Target
            {
                get
                { return _ref.Target; }
            }

        }

#endif

        #endregion

        private int allocCheckCounter;
        private int lastSweepCount;
        private long lastSweepMemory;

        /// <summary>
        /// Adds a new real object - associate mapping.
        /// </summary>
        public void Add(object key, T value)
        {
            CheckAllocation();

            base.Add(new WeakCacheKey(key), new WeakReference(value, true));
        }

        /// <summary>
        /// Retrieves the associate for a given real object.
        /// </summary>
        public bool TryGetValue(object key, out T value)
        {
            object obj;

            bool success = base.TryGetValue(key, out obj);
            if (!success)
            {
                value = default(T);
                return false;
            }

            WeakReference wr = obj as WeakReference;
            if (wr != null)
            {
                value = (T)wr.Target;
            }
            else
            {
                // turn the strong ref to weak ref now
                this[key] = new WeakReference(obj, true);

                value = (T)obj;
            }

            return true;
        }

        /// <summary>
        /// Check whether it is reasonable to perform a weak reference sweep and delegates to <see cref="WeakReferenceSweep"/>.
        /// </summary>
        /// <remarks>
        /// Inspired by <c>System.ComponentModel.WeakHashTable.ScavengeKeys</c> BCL internal class.
        /// </remarks>
        private void CheckAllocation()
        {
            int count = Count;

            if (count != 0)
            {
                if (lastSweepCount == 0) lastSweepCount = count;
                else
                {
                    long mem = GC.GetTotalMemory(false);
                    if (lastSweepMemory == 0) lastSweepMemory = mem;
                    else
                    {
                        float mem_delta = ((float)(mem - lastSweepMemory)) / ((float)lastSweepMemory);
                        float count_delta = ((float)(count - lastSweepCount)) / ((float)lastSweepCount);

                        if ((mem_delta < 0 && count_delta >= 0) || ++allocCheckCounter > 4096)
                        {
                            WeakReferenceSweep();

                            lastSweepMemory = mem;
                            lastSweepCount = count;
                            allocCheckCounter = 0;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Removes items representing real objects that are already dead.
        /// </summary>
        private void WeakReferenceSweep()
        {
            List<WeakCache<T>.WeakCacheKey> dead_refs = new List<WeakCache<T>.WeakCacheKey>();
            List<KeyValuePair<object, T>> strong_keys = new List<KeyValuePair<object, T>>();

            foreach (KeyValuePair<object, object> pair in this)
            {
                WeakCache<T>.WeakCacheKey key = (WeakCache<T>.WeakCacheKey)pair.Key;

                if (!key.IsAlive) dead_refs.Add(key);
                else
                {
                    if (!(pair.Value is WeakReference))
                    {
                        strong_keys.Add(new KeyValuePair<object, T>(key.Target, (T)pair.Value));
                    }
                }
            }

            // remove dead keys
            foreach (WeakCache<T>.WeakCacheKey key in dead_refs)
            {
                Remove(key);
            }

            // weaken strong references to living associates
            foreach (KeyValuePair<object, T> pair in strong_keys)
            {
                if (pair.Key != null) this[pair.Key] = new WeakReference(pair.Value, true);
            }
        }

        /// <summary>
        /// Ensures that the associate of the given real object is held strongly.
        /// </summary>
        /// <remarks>
        /// Should be called from within the associate's finalizer when the real object is
        /// figured out to be still alive.
        /// </remarks>
        public void Resurrect(object key, T value)
        {
            Debug.Assert(ContainsKey(key));

            // turn the weak ref into strong ref
            this[key] = value;
        }
    }

    #endregion

    #region GenericEnumeratorAdapter, GenericDictionaryAdapter

    /// <summary>
    /// Makes it possible to use C# 2.0 iterators to implement the <see cref="IDictionaryEnumerator"/>
    /// interface.
    /// </summary>
    /// <remarks>
    /// Optionally performs CLR to PHP wrapping on returned values.
    /// </remarks>
    [Serializable]
    public class GenericEnumerableAdapter<TValue> : IDictionaryEnumerator
    {
        #region Fields

        private IEnumerator<TValue>/*!*/ baseEnumerator;
        private bool wrapValues;

        #endregion

        #region Construction

        public GenericEnumerableAdapter(IEnumerator<TValue>/*!*/ baseEnumerator, bool wrapValues)
        {
            Debug.Assert(baseEnumerator != null);

            this.baseEnumerator = baseEnumerator;
            this.wrapValues = wrapValues;
        }

        #endregion

        #region IDictionaryEnumerator Members

        public DictionaryEntry Entry
        {
            get { return new DictionaryEntry(Key, Value); }
        }

        public object Key
        {
            get
            { return null; }
        }

        public object Value
        {
            get
            {
                TValue value = baseEnumerator.Current;
                if (wrapValues) return PHP.Core.Reflection.ClrObject.WrapDynamic(value);
                return value;
            }
        }

        #endregion

        #region IEnumerator Members

        public object Current
        {
            get { return new DictionaryEntry(Key, Value); }
        }

        public bool MoveNext()
        {
            return baseEnumerator.MoveNext();
        }

        public void Reset()
        {
            baseEnumerator.Reset();
        }

        #endregion
    }

    /// <summary>
    /// Makes it possible to use C# 2.0 iterators to implement the <see cref="IDictionaryEnumerator"/>
    /// interface.
    /// </summary>
    /// <remarks>
    /// Optionally performs CLR to PHP wrapping on returned keys and values.
    /// </remarks>
    [Serializable]
    public class GenericDictionaryAdapter<TKey, TValue> : IDictionaryEnumerator
    {
        #region Fields

        private IEnumerator<KeyValuePair<TKey, TValue>>/*!*/ baseEnumerator;
        private bool wrapKeysAndValues;

        #endregion

        #region Construction

        public GenericDictionaryAdapter(IEnumerator<KeyValuePair<TKey, TValue>>/*!*/ baseEnumerator, bool wrapKeysAndValues)
        {
            Debug.Assert(baseEnumerator != null);

            this.baseEnumerator = baseEnumerator;
            this.wrapKeysAndValues = wrapKeysAndValues;
        }

        #endregion

        #region IDictionaryEnumerator Members

        public DictionaryEntry Entry
        {
            get { return new DictionaryEntry(Key, Value); }
        }

        public object Key
        {
            get
            {
                TKey key = baseEnumerator.Current.Key;
                if (wrapKeysAndValues) return PHP.Core.Reflection.ClrObject.WrapDynamic(key);
                return key;
            }
        }

        public object Value
        {
            get
            {
                TValue value = baseEnumerator.Current.Value;
                if (wrapKeysAndValues) return PHP.Core.Reflection.ClrObject.WrapDynamic(value);
                return value;
            }
        }

        #endregion

        #region IEnumerator Members

        public object Current
        {
            get { return this.Entry; }
        }

        public bool MoveNext()
        {
            return baseEnumerator.MoveNext();
        }

        public void Reset()
        {
            baseEnumerator.Reset();
        }

        #endregion
    }

    #endregion

    #region HashQueue

    internal sealed class HashQueue       // GENERICS: <K,V>
    {
        public delegate object KeyProvider(object value); // GENERICS: K(V)

        private readonly Queue queue;     // GENERICS: <V>
        private readonly Hashtable index; // GENERICS: <K,int>
        private readonly KeyProvider keyProvider;

        public HashQueue(ICollection collection, KeyProvider keyProvider)
        {
            queue = new Queue(collection);
            index = new Hashtable(StringComparer.CurrentCultureIgnoreCase);

            this.keyProvider = keyProvider;

            foreach (object item in collection)
                index[keyProvider(item)] = 1;
        }

        public int Count { get { return queue.Count; } }

        public object Dequeue()
        {
            object result = queue.Dequeue();
            object key = keyProvider(result);
            int count = (int)index[key];

            if (count == 0)
                index.Remove(key);
            else
                index[key] = count - 1;

            return result;
        }

        public void Enqueue(object item)
        {
            queue.Enqueue(item);
            object key = keyProvider(item);
            object count = index[key];

            index[key] = (count != null) ? (int)count + 1 : 1;
        }

        public bool Contains(object item)
        {
            return index.ContainsKey(keyProvider(item));
        }
    }

    #endregion

    #region Utils

    /// <summary>
    /// A few useful methods working with collections.
    /// </summary>
    public static class CollectionUtils
    {
        /// <summary>
        /// Determines whether a specified collection of strings contains a string.
        /// </summary>
        /// <param name="collection">The enumerable collection of strings.</param>
        /// <param name="str">The string to search for.</param>
        /// <param name="ignoreCase">Whether to compare case-insensitively.</param>
        /// <returns>Whether the collection contains <paramref name="str"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> is a <B>null</B> reference.</exception>
        /// <exception cref="InvalidCastException"><paramref name="collection"/> contains a non-string.</exception>
        public static bool ContainsString(IEnumerable/*!*/ collection, string str, bool ignoreCase)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            foreach (string item in collection)
            {
                if (String.Compare(item, str, StringComparison.CurrentCultureIgnoreCase) == 0)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns an index of the last set bit in a bitmap.
        /// </summary>
        /// <param name="bitmap">The bitmap to search in.</param>
        /// <returns>The index of the last bit which is set.</returns>
        public static int GetLastSet(BitArray bitmap)
        {
            int result = bitmap.Count - 1;
            while (result >= 0 && !bitmap[result]) result--;
            return result;
        }

        public static int IncrementValue<TKey>(Dictionary<TKey, int>/*!*/ dictionary, TKey key, int amount)
        {
            int value = 0;
            dictionary.TryGetValue(key, out value);
            dictionary[key] = value + 1;
            return value;
        }


        /// <summary>
        /// Creates dictionary from two enumerators.
        /// </summary>
        public static IDictionary<T, K> JoinDictionary<T, K>(IEnumerable<T> keys, IEnumerable<K> values)
        {
            Dictionary<T, K> ret = new Dictionary<T, K>();
            IEnumerator<T> ek = keys.GetEnumerator();
            IEnumerator<K> ev = values.GetEnumerator();

            bool en, vn;
            while ((en = ek.MoveNext()) == (vn = ev.MoveNext()))
            {
                if (!en) return ret;
                ret.Add(ek.Current, ev.Current);
            }
            throw new InvalidOperationException("Joining collections of incompatible size!");
        }


        /// <summary>
        /// Creates dictionary with all items from the <paramref name="values"/> collection. 
        /// The key of returned dictionary is list of values of type <typeparamref name="K"/>.
        /// </summary>
        public static IDictionary<T, IList<K>> BuildListDictionary<T, K>(IEnumerable<T> keys, IEnumerable<K> values)
        {
            Dictionary<T, IList<K>> ret = new Dictionary<T, IList<K>>();
            IEnumerator<T> ek = keys.GetEnumerator();
            IEnumerator<K> ev = values.GetEnumerator();

            bool en, vn;
            while ((en = ek.MoveNext()) == (vn = ev.MoveNext()))
            {
                if (!en) return ret;

                IList<K> tmp;
                if (!ret.TryGetValue(ek.Current, out tmp))
                    ret.Add(ek.Current, tmp = new List<K>());
                tmp.Add(ev.Current);
            }
            throw new InvalidOperationException("Joining collections of incompatible size!");
        }

        /// <summary>
        /// Applies function <paramref name="f"/> to every element in <paramref name="en"/>
        /// </summary>
        public static IEnumerable<K> Map<T, K>(IEnumerable<T> en, Func<T, K> f)
        {
            foreach (T el in en) yield return f(el);
        }

        /// <summary>
        /// Filters a collection <paramref name="en"/> using a function <paramref name="f"/>
        /// </summary>
        public static IEnumerable<T> Filter<T>(IEnumerable<T> en, Func<T, bool> f)
        {
            foreach (T el in en) if (f(el)) yield return el;
        }
    }

    #endregion

    #endregion

    #region Arrays

    /// <summary>
    /// Utilities manipulating arrays.
    /// </summary>
    [DebuggerNonUserCode]
    public static class ArrayUtils
    {
        /// <summary>
        /// Empty int array.
        /// </summary>
        public static readonly int[] EmptyIntegers = new int[0];

        /// <summary>
        /// Empty object array.
        /// </summary>
        public static readonly object[] EmptyObjects = new object[0];

        /// <summary>
        /// Empty object array.
        /// </summary>
        public static readonly char[] EmptyChars = new char[0];

        /// <summary>
        /// Empty byte array.
        /// </summary>
        public static readonly byte[] EmptyBytes = new byte[0];

        /// <summary>
        /// Empty <see cref="MethodInfo"/> array.
        /// </summary>
        public static readonly MethodInfo[] EmptyMethodInfos = new MethodInfo[0];

        /// <summary>
        /// Empty <see cref="string"/> array.
        /// </summary>
        public static readonly string[] EmptyStrings = new string[0];

        /// <summary>
        /// Converts a <see cref="IList"/> to an array of strings.
        /// </summary>
        /// <param name="list">The list of strings.</param>
        /// <returns>The array of strings.</returns>
        /// <exception cref="InvalidCastException">An item of <paramref name="list"/> is not a string.</exception>
        public static string[] ToStringArray(IList list)
        {
            if (list == null || list.Count == 0) return ArrayUtils.EmptyStrings;

            string[] result = new string[list.Count];
            for (int i = 0; i < list.Count; i++)
                result[i] = (string)list[i];

            return result;
        }

        /// <summary>
        /// Converts an array of bytes to a string.
        /// </summary>
        /// <param name="bytes">The array of bytes.</param>
        /// <returns>The string which characters contains items of bytes array. 
        /// The higher bytes of characters are zeroes the lower ones are copied from the bytes array.</returns>
        public unsafe static string ToString(byte[] bytes)
        {
            if (bytes == null) return null;
            int length = bytes.Length;
            if (length == 0) return String.Empty;

            fixed (byte* ptr = bytes)
            {
                return new String((char*)ptr, 0, length);
            }
        }

        /// <summary>
        /// Searches for specified character in sorted array of characters.
        /// </summary>
        /// <param name="array">The array to search in.</param>
        /// <param name="c">The character to search for.</param>
        /// <returns>The position of the <paramref name="c"/> in <paramref name="array"/> or -1 if not found.</returns>
        public static int BinarySearch(char[] array, char c)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            int i = 0;
            int j = array.Length - 1;
            while (i < j)
            {
                int m = (i + j) >> 1;
                char cm = array[m];
                if (c == cm) return m;

                if (c > cm)
                {
                    i = m + 1;
                }
                else
                {
                    j = m - 1;
                }
            }
            return (array[i] == c) ? i : -1;
        }

        /// <summary>
        /// Fills a portion of an array of bytes by specified byte.
        /// </summary>
        /// <param name="array">The array to fill.</param>
        /// <param name="value">The value to fill the array with.</param>
        /// <param name="offset">The index of the first byte to be set.</param>
        /// <param name="count">The number of bytes to be set.</param>
        /// <remarks>This method uses fast unsafe filling of memory with bytes.</remarks>
        public unsafe static void Fill(byte[] array, byte value, int offset, int count)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (offset < 0 || offset + count > array.Length)
                throw new ArgumentOutOfRangeException("offset");
            if (count < 0)
                throw new ArgumentOutOfRangeException("length");
            if (array.Length == 0)
                return;

#if SILVERLIGHT
            for (int i = offset; i < count + offset; i++)
                array[i] = value;
#else
            fixed (byte* ptr = &array[offset])
            {
                for (int i = 0; i < count; i++)
                    ptr[i] = value;
                //Utils.MemFill(ptr, value, count);
            }
#endif
        }

        /// <summary>
        /// Create copy of given array without the last item.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <returns></returns>
        internal static T[] RemoveLast<T>(T[] array)
        {
            T[] array2 = new T[array.Length - 1];
            Array.Copy(array, 0, array2, 0, array2.Length);
            return array2;
        }

        /// <summary>
        /// Compare arrays lexicographically.
        /// </summary>
        /// <include file='Doc/Common.xml' path='docs/method[@name="Compare(x,y)"]/*'/>
        /// <exception cref="ArgumentNullException">Either <paramref name="x"/> or <paramref name="y"/> are <B>null</B>.</exception>
        unsafe public static int Compare(byte[] x, byte[] y)
        {
            if (x == null) throw new ArgumentNullException("x");
            if (y == null) throw new ArgumentNullException("y");

            int length = Math.Min(x.Length, y.Length);

            fixed (byte* pinned_x = x, pinned_y = y)
            {
                byte* ptr_x = pinned_x, ptr_y = pinned_y;

                for (int i = 0; i < length; i++, ptr_x++, ptr_y++)
                {
                    if (*ptr_x != *ptr_y)
                        return (int)*ptr_y - *ptr_x;
                }
            }
            return x.Length - y.Length;
        }

        unsafe public static int Compare(byte[]/*!*/x_bytes, byte[]/*!*/y_bytes, int length)
        {
            Debug.Assert(x_bytes != null && y_bytes != null);
            Debug.Assert(length <= x_bytes.Length && length <= y_bytes.Length);

            fixed (byte* x = x_bytes, y = y_bytes)
            {
                return ArrayUtils.Compare(x, y, length);
            }
        }

        unsafe public static int Compare(byte* x, byte* y, int length)
        {
            for (int i = 0; i < length; i++, x++, y++)
            {
                if (*x != *y)
                    return (int)*y - *x;
            }
            return 0;
        }

        /// <summary>
        /// Compares two IEquatable objects. They can be null, the method will safely checks the references first.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private static bool Equals<T>(T x, T y) where T : IEquatable<T>
        {
            if (object.ReferenceEquals(x, y))
                return true;

            if (object.ReferenceEquals(x, null) ^ object.ReferenceEquals(y, null))
                return false;

            return x.Equals(y);
        }

        /// <summary>
        /// Compares two arrays of objects of type T. The method returns true if array are the same reference or they have the same length and single values matches.
        /// </summary>
        /// <typeparam name="T">Type of elements of arrays.</typeparam>
        /// <param name="x">First array</param>
        /// <param name="y">Second array.</param>
        /// <returns>True if arrays contains same objects, compared using <c>IEquatable.Equals</c>.</returns>
        public static bool Equals<T>(T[] x, T[] y) where T : IEquatable<T>
        {
            if (object.ReferenceEquals(x, y))
                return true;

            if (object.ReferenceEquals(x, null) ^ object.ReferenceEquals(y, null))
                return false;

            if (x.Length != y.Length)
                return false;

            for (int i = 0; i < x.Length; ++i)
                if (!Equals<T>(x[i], y[i]))
                    return false;

            return true;
        }

        /// <summary>
        /// Compare arrays of <see cref="Type"/> reference.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>True if <paramref name="x"/> and <paramref name="y"/> references are equal or single elements matches.</returns>
        public static bool Equals(Type[] x, Type[] y)
        {
            if (object.ReferenceEquals(x, y))
                return true;

            if (object.ReferenceEquals(x, null) ^ object.ReferenceEquals(y, null))
                return false;

            if (x.Length != y.Length)
                return false;

            for (int i = 0; i < x.Length; ++i)
                if (x[i] != y[i])
                    return false;

            return true;
        }

        /// <summary>
        /// Returns an array of indices of items in an array which are equal to or not equal to the specified value.
        /// </summary>
        /// <param name="bytes">The array of values. Assumes that length of the array is less or equal to 256.</param>
        /// <param name="value">The filtered value.</param>
        /// <param name="inequality">Determines whether to compare for inequality or equality.</param>
        /// <returns>The array of indices.</returns>
        internal static byte[] GetValueIndices(int[] bytes, int value, bool inequality)
        {
            Debug.Assert(bytes.Length <= 256);
            Debug.Assert(bytes != null);

            int length = bytes.Length;

            // computes new array's length:
            int count = 0;
            for (int i = 0; i < length; i++)
                if (bytes[i] == value) count++;
            if (inequality) count = length - count;

            // creates new array:
            byte[] result = new byte[count];

            // fills new array:
            if (!inequality)
            {
                for (int i = 0, j = 0; i < length; i++)
                    if (bytes[i] == value)
                        result[j++] = (byte)i;

            }
            else
            {
                for (int i = 0, j = 0; i < length; i++)
                    if (bytes[i] != value)
                        result[j++] = (byte)i;
            }

            return result;
        }

        /// <summary>
        /// Concats two arrays of <typeparamref name="T"/>.
        /// </summary>
        /// <param name="x">The first array of <typeparamref name="T"/> to be concatenated.</param>
        /// <param name="y">The second array of <typeparamref name="T"/> to be concatenated.</param>
        /// <returns>The concatenation of <paramref name="x"/> and <paramref name="y"/>.</returns>
        public static T[]/*!*/ Concat<T>(T[]/*!*/ x, T[]/*!*/ y)
        {
            if (x == null) throw new ArgumentNullException("x");
            if (y == null) throw new ArgumentNullException("y");

            T[] result = new T[x.Length + y.Length];

            Array.Copy(x, 0, result, 0, x.Length);
            Array.Copy(y, 0, result, x.Length, y.Length);

            return result;
        }

        /// <summary>
        /// Concats array of <typeparamref name="T"/> with single <typeparamref name="T"/> element.
        /// </summary>
        /// <param name="x">The array of <typeparamref name="T"/> to be concatenated.</param>
        /// <param name="y">The element of <typeparamref name="T"/> to be appended.</param>
        /// <returns>The concatenation of <paramref name="x"/> and <paramref name="y"/>.</returns>
        public static T[]/*!*/ Concat<T>(T[] x, T y)
        {
            if (x == null || x.Length == 0)
                return new T[] { y };

            if (x.Length == 1)
                return new T[] { x[0], y };

            //
            T[] result = new T[x.Length + 1];

            Array.Copy(x, 0, result, 0, x.Length);
            result[x.Length] = y;

            return result;
        }

        /// <summary>
        /// Concats two arrays of bytes.
        /// </summary>
        /// <param name="x">The first array of bytes to be concatenated.</param>
        /// <param name="y">The second array of bytes to be concatenated.</param>
        /// <returns>The concatenation of <paramref name="x"/> and <paramref name="y"/>.</returns>
        public static byte[]/*!*/ Concat(byte[]/*!*/ x, byte[]/*!*/ y)
        {
            if (x == null) throw new ArgumentNullException("x");
            if (y == null) throw new ArgumentNullException("y");

            byte[] result = new byte[x.Length + y.Length];

            Buffer.BlockCopy(x, 0, result, 0, x.Length);
            Buffer.BlockCopy(y, 0, result, x.Length, y.Length);

            return result;
        }

#if SILVERLIGHT
		/// <summary>
		/// Finds the first occurence of a specified byte in an array.
		/// </summary>
		/// <param name="haystack">The array to search.</param>
		/// <param name="needle">The data to look for.</param>
		/// <param name="from">First offset to look at for the <paramref name="needle"/>.</param>
		/// <returns>The index of the first occurence of <paramref name="needle"/> or <c>-1</c> if not found.</returns>
		public static int IndexOf(byte[] haystack, byte needle, int from)
		{
			for(int i = from; i<haystack.Length; i++)
				if (haystack[i] == needle) return i;
			return -1;
		}

#else
        /// <summary>
        /// Finds the first occurrence of a specified byte in an array.
        /// </summary>
        /// <param name="haystack">The array to search.</param>
        /// <param name="needle">The data to look for.</param>
        /// <param name="from">First offset to look at for the <paramref name="needle"/>.</param>
        /// <returns>The index of the first occurence of <paramref name="needle"/> or <c>-1</c> if not found.</returns>
        public unsafe static int IndexOf(byte[] haystack, byte needle, int from)
        {
            fixed (byte* h = haystack)
            {
                byte* p = h + from;
                for (int i = haystack.Length - from; i > 0; i--, p++)
                {
                    if (*p == needle) return (int)(p - h);
                }
            }
            return -1;
        }
#endif

        internal static T[]/*!*/ Filter<T>(T[]/*!*/ srcArray, T[]/*!*/ dstArray, T removedValue)
            where T : class
        {
            int j = 0;
            for (int i = 0; i < srcArray.Length; i++)
            {
                if (!ReferenceEquals(srcArray[i], removedValue))
                    dstArray[j++] = srcArray[i];
            }

            return dstArray;
        }

        internal static int IndexOfNull<T>(ref T[]/*!*/ array, int start)
            where T : class
        {
            while (start < array.Length && array[start] != null) start++;

            if (start == array.Length)
                Array.Resize(ref array, (array.Length + 1) * 2);

            return start;
        }

        internal static void CheckCopyTo(Array/*!*/ array, int index, int count)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (array.Rank != 1)
                throw new ArgumentException(CoreResources.GetString("invalid_array_rank"), "array");
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            if (array.Length - index < count)
                throw new ArgumentException(CoreResources.GetString("not_enought_space_in_target_array"));
        }

        public static string/*!*/ ToList<T>(IEnumerable<T> enumerable, Action<StringBuilder, T>/*!*/ appendItem)
        {
            if (appendItem == null)
                throw new ArgumentNullException("appendItem");

            if (enumerable == null) return "";

            StringBuilder result = new StringBuilder();

            bool first = true;
            foreach (T item in enumerable)
            {
                if (!first) result.Append(',');
                first = false;

                appendItem(result, item);
            }

            return result.ToString();
        }

        /// <summary>
        /// Returns number of starting elements satisfying given predicate.
        /// </summary>
        /// <param name="args">Array of objects.</param>
        /// <param name="predicate">Condition.</param>
        /// <returns>Amount of elements.</returns>
        public static int TakeWhileCount(object[]/*!*/args, Predicate<object> predicate)
        {
            Debug.Assert(args != null);

            int i = 0;

            for (; i < args.Length; ++i)
                if (!predicate(args[i]))
                    return i;

            return i;
        }

        /// <summary>
        /// Creates list without duplicities from given <c>items</c>.
        /// </summary>
        /// <typeparam name="T">Type of single items in the list.</typeparam>
        /// <param name="items">Items to check for duplicities.</param>
        /// <returns>New list of unique items. Cannot return null.</returns>
        public static ICollection<T>/*!*/Unique<T>(IList<T> items)
        {
            if (items == null || items.Count == 0) return (ICollection<T>)new T[0];

            Dictionary<T, bool> list = new Dictionary<T, bool>(items.Count);

            foreach (var x in items)
                if (!list.ContainsKey(x))
                    list.Add(x, true);

            return list.Keys;
        }

        /// <summary>
        /// Group given <c>items</c> by their key obtained through given <c>makeKey</c> converter.
        /// </summary>
        /// <typeparam name="TKey">Type of key of <c>items</c>.</typeparam>
        /// <typeparam name="TItem">Type of single items in the list.</typeparam>
        /// <param name="items">Items to check for duplicities.</param>
        /// <param name="makeKey">Function converting the <c>TItem</c> into <c>TKey</c>.</param>
        /// <returns>Dictionary of (key, list) of items grouped by their key. Cannot return null.</returns>
        public static IDictionary<TKey, List<TItem>>/*!*/Group<TKey, TItem>(IList<TItem> items, Converter<TItem, TKey>/*!*/makeKey)
        {
            if (items == null || items.Count == 0) return new Dictionary<TKey, List<TItem>>();

            var list = new Dictionary<TKey, List<TItem>>(items.Count);

            foreach (var x in items)
            {
                List<TItem> group;

                var key = makeKey(x);
                if (!list.TryGetValue(key, out group))
                    list.Add(key, (group = new List<TItem>(1)));

                group.Add(x);
            }

            return list;
        }

        /// <summary>
        /// Perform logical AND operation onto the array of logical values.
        /// </summary>
        /// <param name="items">Array of bools. Cannot be null.</param>
        /// <param name="boolGetter">Function converting items to logical value.</param>
        /// <returns>True if all the values in <c>items</c> are true or if given array is empty. False if it contains at least one <c>false</c> value.</returns>
        public static bool LogicalAnd<T>(IEnumerable<T>/*!*/items, Converter<T, bool>/*!*/boolGetter)
        {
            if (items == null)
                throw new ArgumentNullException("items");
            if (boolGetter == null)
                throw new ArgumentNullException("boolGetter");

            foreach (var x in items)
                if (!boolGetter(x))
                    return false;

            return true;
        }

        /// <summary>
        /// Read all the bytes from input stream to byte array.
        /// </summary>
        /// <param name="input">Input stream.</param>
        /// <returns>Array of bytes read from the input stream.</returns>
        public static byte[] ReadAllBytes(Stream input)
        {
            if (input == null)
                return null;

            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

    }

    public struct SubArray<T> : IEnumerable<T>
    {
        private readonly T[] array;
        private int start;

        public int Length { get { return length; } }
        private int length;

        public bool IsNull { get { return array == null; } }

        public bool IsNullOrEmpty { get { return array == null || length == 0; } }

        public T this[int index] { get { return array[start + index]; } set { array[start + index] = value; } }

        public SubArray(T[] array, int start, int length)
        {
            this.array = array;
            this.start = start;
            this.length = length;
        }

        public T[] ToArray()
        {
            if (array == null) return null;

            T[] result = new T[length];

            for (int i = 0, j = start; i < length; i++, j++)
                result[i] = array[j];

            return result;
        }

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            if (array != null)
            {
                for (int i = 0, j = start; i < length; i++, j++)
                    yield return array[j];
            }
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }

    #endregion

    #region Reflection Utils

    /// <summary>
    /// Utilities manipulating metadata via reflection.
    /// </summary>
    public static class ReflectionUtils
    {
        /// <summary>
        /// Sets user entry point if this feature is supported.
        /// </summary>
        internal static void SetUserEntryPoint(ModuleBuilder/*!*/ builder, MethodInfo/*!*/ method)
        {
            try
            {
                if (setUserEntryPointSupported ?? true) //TODO: UserEntryPoint shouldn't be set if there isn't any user method that is called first or it should be generated trivial method with emptystatement and one sequencepoint
                    builder.SetUserEntryPoint(method);
                setUserEntryPointSupported = true;
            }
            catch (NotImplementedException)
            {
                setUserEntryPointSupported = false;
            }
            catch (NotSupportedException)
            {
                setUserEntryPointSupported = false;
            }
        }
        private static bool? setUserEntryPointSupported;

        #region Global Fields

        private const string GlobalFieldsType = "<Global Fields>";

        internal static List<FieldInfo>/*!!*/ GetGlobalFields(Assembly/*!*/ assembly, BindingFlags bindingFlags)
        {
            List<FieldInfo> result = new List<FieldInfo>();

#if SILVERLIGHT
			Module[] modules = assembly.GetModules();
#else
            Module[] modules = assembly.GetModules(false); // false - include resource modules (?)
#endif
            foreach (Module module in modules)
            {
                result.AddRange(module.GetFields(bindingFlags));

                Type global_type = module.GetType(GlobalFieldsType);
                if (global_type != null)
                    result.AddRange(global_type.GetFields(bindingFlags));
            }

            return result;
        }

        internal static FieldBuilder/*!*/ DefineGlobalField(ModuleBuilder/*!*/ moduleBuilder, string/*!*/ name, Type/*!*/ type, FieldAttributes attributes)
        {
            //FieldBuilder result = TryDefineRealGlobalField(moduleBuilder, name, type, attributes);
            //if (result != null)
            //    return result;

            TypeBuilder global_type = (TypeBuilder)moduleBuilder.GetType(GlobalFieldsType);
            if (global_type == null)
            {
                global_type = moduleBuilder.DefineType(GlobalFieldsType, TypeAttributes.Class | TypeAttributes.Public |
                    TypeAttributes.Sealed | TypeAttributes.SpecialName);

                global_type.DefineDefaultConstructor(MethodAttributes.PrivateScope);
            }

            return global_type.DefineField(name, type, attributes);
        }

        //private static FieldBuilder TryDefineRealGlobalField(ModuleBuilder/*!*/ moduleBuilder, string/*!*/ name, Type/*!*/ type, FieldAttributes attributes)
        //{
        //    try
        //    {
        //        if (EnvironmentUtils.IsDotNetFramework)
        //        {
        //            // .NET Framework:

        //            FieldInfo fm_ModuleData = typeof(Module).GetField("m_moduleData", BindingFlags.Instance | BindingFlags.NonPublic);
        //            FieldInfo fm_globalTypeBuilder = fm_ModuleData.FieldType.GetField("m_globalTypeBuilder", BindingFlags.Instance | BindingFlags.NonPublic);

        //            object m_ModuleData = fm_ModuleData.GetValue(moduleBuilder);
        //            TypeBuilder m_globalTypeBuilder = (TypeBuilder)fm_globalTypeBuilder.GetValue(m_ModuleData);

        //            return m_globalTypeBuilder.DefineField(name, type, attributes);
        //        }
        //        else
        //        {
        //            // Mono:

        //            FieldInfo f_global_fields = typeof(ModuleBuilder).GetField("global_fields", BindingFlags.Instance | BindingFlags.NonPublic);
        //            FieldBuilder[] global_fields = (FieldBuilder[])f_global_fields.GetValue(moduleBuilder);

        //            FieldInfo f_global_type = typeof(ModuleBuilder).GetField("global_type", BindingFlags.Instance | BindingFlags.NonPublic);
        //            TypeBuilder global_type = (TypeBuilder)f_global_type.GetValue(moduleBuilder);

        //            FieldBuilder result = global_type.DefineField(name, type, attributes);

        //            if (global_fields != null)
        //            {
        //                FieldBuilder[] new_global_fields = new FieldBuilder[global_fields.Length + 1];
        //                System.Array.Copy(global_fields, new_global_fields, global_fields.Length);
        //                new_global_fields[global_fields.Length] = result;

        //                f_global_fields.SetValue(moduleBuilder, new_global_fields);
        //            }
        //            else
        //            {
        //                global_fields = new FieldBuilder[1];
        //                global_fields[0] = result;

        //                f_global_fields.SetValue(moduleBuilder, global_fields);
        //            }

        //            return result;
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        return null;
        //    }
        //}

        internal static void CreateGlobalType(ModuleBuilder/*!*/ moduleBuilder)
        {
            moduleBuilder.CreateGlobalFunctions();

            TypeBuilder global_type = (TypeBuilder)moduleBuilder.GetType(GlobalFieldsType);
            if (global_type != null)
                global_type.CreateType();
        }

        #endregion

        #region Utils

        internal static ParameterBuilder/*!*/ DefineParameter(MethodInfo/*!*/ method, int index, ParameterAttributes attributes,
            string/*!*/ name)
        {
            Debug.Assert(method is MethodBuilder || method is DynamicMethod);

            MethodBuilder builder = method as MethodBuilder;
            if (builder != null)
                return builder.DefineParameter(index, attributes, name);
            else
                return ((DynamicMethod)method).DefineParameter(index, attributes, name);
        }

        #endregion

        internal static void SetCustomAttribute(MethodInfo/*!*/ method, CustomAttributeBuilder/*!*/ customAttributeBuilder)
        {
            MethodBuilder builder = method as MethodBuilder;
            if (builder != null)
                builder.SetCustomAttribute(customAttributeBuilder);
        }

        internal static Type[]/*!!*/ GetParameterTypes(ParameterInfo[]/*!!*/ parameters)
        {
            Type[] types = new Type[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
                types[i] = parameters[i].ParameterType;
            return types;
        }

        public static object GetDefault(Type type)
        {
            return (type.IsValueType) ? Activator.CreateInstance(type) : null;
        }

        /// <summary>
        /// Parses the <paramref name="realType"/> into <paramref name="transientId"/>, <paramref name="sourceFile"/> and <paramref name="typeName"/>.
        /// </summary>
        /// <param name="realType">Type from within <see cref="PHP.Core.Reflection.TransientModule"/>, <see cref="PHP.Core.Reflection.ScriptModule"/> or <see cref="PHP.Core.Reflection.PureModule"/>.</param>
        /// <param name="transientId"><c>-1</c> or Id of transiend module.</param>
        /// <param name="sourceFile"><c>null</c> or relative file name of the contained type.</param>
        /// <param name="typeName">Cannot be null. PHP type name without the prefixed <c>&lt;</c>~<c>&gt;</c> information. CLR notation of namespaces.</param>
        /// <remarks>Handles special cases of types from ClassLibrary and Core.</remarks>
        public static void ParseTypeId(Type/*!*/realType, out int transientId, out string sourceFile, out string typeName)
        {
            Debug.Assert(realType != null);

            // parse the type name (ScriptModule, PureModule, TransientModule):
            ParseTypeId(realType.FullName, out transientId, out sourceFile, out typeName);

            //
            // handle special cases:
            //

            // [ImplementsTypeAttribute] with PHPTypeName specified => take the PHPTypeName only
            var attr = ImplementsTypeAttribute.Reflect(realType);
            if (attr != null && attr.PHPTypeName != null)
            {
                typeName = attr.PHPTypeName;
                return;
            }
            
            // PHP.Library. => cut of the namespace, keep realType.Name only
            // J: HACK because of PHP types in ClassLibrary and Core
            if (realType.Namespace.StartsWith(Namespaces.Library, StringComparison.Ordinal))
            {
                typeName = realType.Name;
                return;
            }            
        }

        /// <summary>
        /// Parses the <paramref name="realTypeFullName"/> into <paramref name="transientId"/>, <paramref name="sourceFile"/> and <paramref name="typeName"/>.
        /// </summary>
        /// <param name="realTypeFullName">Expecting <see cref="Type.FullName"/> (type CLR full name, including <c>.</c>, <c>+</c>) of a type from within <see cref="PHP.Core.Reflection.TransientModule"/>, <see cref="PHP.Core.Reflection.ScriptModule"/> or <see cref="PHP.Core.Reflection.PureModule"/>.</param>
        /// <param name="transientId"><c>-1</c> or Id of transiend module.</param>
        /// <param name="sourceFile"><c>null</c> or relative file name of the contained type.</param>
        /// <param name="typeName">Cannot be null. PHP type name without the prefixed <c>&lt;</c>~<c>&gt;</c> information. CLR notation of namespaces.</param>
        internal static void ParseTypeId(string/*!*/realTypeFullName, out int transientId, out string sourceFile, out string typeName)
        {
            Debug.Assert(!string.IsNullOrEmpty(realTypeFullName));

            //
            transientId = PHP.Core.Reflection.TransientAssembly.InvalidEvalId;

            // <srcFile{[^?]id}>.typeName
            if (realTypeFullName[0] == '<')
            {
                // naming policy of TransientModule, ScriptModule
                int closing;
                if ((closing = realTypeFullName.IndexOf('>', 1)) >= 0)
                {
                    // find ^ or ?
                    int idDelim;
                    if ((idDelim = realTypeFullName.IndexOfAny(PHP.Core.Reflection.TransientModule.IdDelimiters, 1, closing)) >= 0)
                        transientId = int.Parse(realTypeFullName.Substring(idDelim + 1, closing - idDelim - 1));
                    else
                        idDelim = closing;
                    
                    // parse sourceFile out:
                    sourceFile = realTypeFullName.Substring(1, idDelim - 1);
                }
                else
                {
                    Debug.Fail("Unexpected Type.FullName! Missing closing '>' in '" + realTypeFullName + "'.");
                    sourceFile = null;
                    closing = 1;
                }

                // parse typeName out:
                Debug.Assert(
                    realTypeFullName.Length > closing + 2 && realTypeFullName[closing + 1] == '.',
                    "Unexpected Type.FullName! Missing '.' after '>' in '" + realTypeFullName + "'.");

                // get the type name (without version id and generic params):
                typeName = QualifiedName.SubstringWithoutBackquoteAndHash(
                    realTypeFullName,
                    closing + 2,
                    realTypeFullName.Length - closing - 2);
            }
            else
            {
                // naming policy of PureModule:
                sourceFile = null;  // we are not able to determine the file name here
                typeName = realTypeFullName;
            }
        }
    }

    #endregion

    #region File System

    #region HTTP Paths (Silverlight)
#if SILVERLIGHT

	/// <summary>
	/// Utilities for working with HTTP paths
	/// </summary>
	public static class HttpPathUtils
	{
		/// <summary>
		/// Combines absolute and relative HTTP path - for example
		/// 'http://foo/bar' + '../t.html' = 'http://foo/t.html'
		/// </summary>
		/// <param name="root">Root path starting with HTTP</param>
		/// <param name="relative">Relative path to be appended at the end.</param>
		/// <returns>Combined URL</returns>
		public static string Combine(string root, string relative)
		{
			// counts number of level-ups "../"
			int level = 0;
			int i = 0;
			while (i + 1 < relative.Length && relative[i] == '.' && relative[i + 1] == '.')
			{
				Debug.Assert(i + 2 == relative.Length || relative[i + 2] == '/');
				i += 3;
				level++;
			}

			// remove 'level' numbers of directories from the root
			int j = root.Length - 1;
			while (level-- > 0)
				while (j > 0 && root[j--] != '/') /*nop*/;

			return root.Substring(0, j + 1) + "/" + 
				relative.Substring(i, relative.Length - i);			
		}
	}

#endif
    #endregion

    #region Paths

    /// <summary>
    /// Represents a full canonical path.
    /// </summary>
    [Serializable]
    [DebuggerNonUserCode]
    public struct FullPath : IEquatable<FullPath>
    {
        #region Equality Comparer

        private class PathEqualityComparer : IEqualityComparer<FullPath>
        {
            /// <summary>
            /// Underlaying <see cref="StringComparer"/> selected for current environment (win/linux).
            /// </summary>
            public StringComparer/*!*/StringComparer { get { return stringComparer; } }
            private readonly StringComparer/*!*/stringComparer;

            public int GetHashCode(FullPath path)
            {
                return stringComparer.GetHashCode(path.path);
            }

            public bool Equals(FullPath x, FullPath y)
            {
                return stringComparer.Equals(x.path, y.path);
            }

            public PathEqualityComparer()
            {
                stringComparer = EnvironmentUtils.IsDotNetFramework ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            }
        }

        #endregion

        #region Static members

        /// <summary>
        /// Implementation of IEqualityComparer&lt;FullPath&gt; interface.
        /// </summary>
        private static readonly PathEqualityComparer/*!*/EqualityComparer = new PathEqualityComparer();

        /// <summary>
        /// Underlaying <see cref="StringComparer"/> selected for current environment (win/linux).
        /// </summary>
        public static StringComparer/*!*/StringComparer { get { return EqualityComparer.StringComparer; } }

        public static readonly FullPath[]/*!*/ EmptyArray = new FullPath[0];

        /// <summary>
        /// Empty path.
        /// </summary>
        public static FullPath Empty = new FullPath(null);

        /// <summary>
        /// Boxed <see cref="Path.DirectorySeparatorChar"/>.
        /// </summary>
        public static string/*!*/DirectorySeparatorString = Path.DirectorySeparatorChar.ToString();

        #endregion

        #region Fields and properties

        /// <summary>
        /// Full canonical path. Can be a <B>null</B> reference.
        /// </summary>
        private string path;

        /// <summary>
        /// Gets whether the path is empty.
        /// </summary>
        public bool IsEmpty { get { return path == null; } }

        /// <summary>
        /// Gets whether the path represents an existing directory.
        /// </summary>
        public bool DirectoryExists { get { return Directory.Exists(path); } }

        /// <summary>
        /// Gets whether the path represents an existing file.
        /// </summary>
        public bool FileExists { get { return File.Exists(path); } }

        /// <summary>Gets last segment path</summary>
        /// <seealso cref="System.IO.Path.GetFileName"/>
        public string FileName { get { return Path.GetFileName(path); } }

        /// <summary>
        /// Full file name. Can be <c>null</c>.
        /// </summary>
        public string FullFileName { get { return path; } }

        /// <summary>Gets last segment of path without extension</summary>
        /// <seealso cref="System.IO.Path.GetFileNameWithoutExtension"/>
        public string FileNameWithoutExtension { get { return Path.GetFileNameWithoutExtension(path); } }
        /// <summary>Gets extension of filename</summary>
        /// <seealso cref="Path.GetExtension"/>
        public string Extension { get { return Path.GetExtension(path); } }

        #endregion

        #region Construction

        /// <summary>
        /// Creates a full path from arbitrary path using <see cref="System.IO.Path.GetFullPath"/>.
        /// </summary>
        /// <param name="arbitraryPath">Arbitrary path.</param>
        /// <exception cref="ArgumentNullException"><paramref name="arbitraryPath"/> is a <B>null</B> reference.</exception>
        /// <exception cref="ArgumentException">Invalid path. Inner exception specifies details (see <see cref="System.IO.Path.GetFullPath"/>).</exception>
        public FullPath(string arbitraryPath)
            : this(arbitraryPath, true)
        {
        }

        /// <summary>
        /// Creates a full path from relative path using <see cref="System.IO.Path.GetFullPath"/>.
        /// </summary>
        /// <param name="relativePath">Arbitrary path.</param>
        /// <param name="root">Root for the path.</param>
        /// <exception cref="ArgumentNullException"><paramref name="relativePath"/> is a <B>null</B> reference.</exception>
        /// <exception cref="ArgumentException">Invalid path. Inner exception specifies details (see <see cref="System.IO.Path.GetFullPath"/>).</exception>
        public FullPath(string/*!*/ relativePath, FullPath root)
        {
            if (relativePath == null)
                throw new ArgumentNullException("relativePath");

            root.EnsureNonEmpty("root");

            try
            {
                path = Path.GetFullPath(Path.Combine(root, relativePath));
            }
            catch (Exception e)
            {
                throw new ArgumentException(CoreResources.GetString("invalid_path"), e);
            }
        }

        internal FullPath(string/*!*/ path, bool isArbitrary)
        {
            if (path == null)
            {
                this.path = null;
            }
            else if (isArbitrary)
            {
                try
                {
                    this.path = System.IO.Path.GetFullPath(path);
                }
                catch (Exception e)
                {
                    throw new ArgumentException(CoreResources.GetString("invalid_path"), e);
                }
            }
            else
            {
                // TODO: better linux/windows compatibility!!
#if !SILVERLIGHT
                Debug.Assert(System.IO.Path.GetFullPath(path).Replace('/', '\\') == path.Replace('/', '\\'));
#endif
                this.path = path;
            }
        }

        /// <summary>
        /// Initialize the <see cref="path"/> using existing <see cref="FullPath"/> and valid <see cref="RelativePath"/>.
        /// </summary>
        /// <param name="root">The root path. Must not be empty.</param>
        /// <param name="relativePath">Canonized relative path to be added to the <paramref name="root"/>.
        /// The path must be normalized already. If its level is non-negative, the path must not start with \ or drive letter.</param>
        /// <exception cref="ArgumentException">The exception is thrown when <paramref name="root"/> is empty or
        /// <paramref name="relativePath"/> level is out of the root level.</exception>
        internal FullPath(FullPath root, RelativePath relativePath)
        {
            if (relativePath.Level < 0)
            {
                // relative path is absolute
                path = relativePath.Path;
                return;
            }

            // empty root
            if (string.IsNullOrEmpty(root.path))
            {
                throw new ArgumentException("Root cannot be empty.", "root");
            }

            //
            // relativePath does not start with / or a drive letter
            //

            Debug.Assert(relativePath.Path == null || relativePath.Path.Length == 0 || (relativePath.Path[0] != Path.DirectorySeparatorChar));

            // root and last root character to be used
            string rootpath = root.path;
            int rootend = rootpath.Length;

            //Debug.Assert(rootend > 0);  // rootpath is not empty string

            // cut ending \ from the root
            if (rootpath[rootend - 1] == Path.DirectorySeparatorChar)
                rootend--;

            // go <level>s up // typically 0
            for (int level = relativePath.Level; level > 0; --level)
            {
                rootend = (rootend > 0) ?
                    rootpath.LastIndexOf(Path.DirectorySeparatorChar, rootend - 1, rootend) :  // start search from <rootend-1>, search for <rootend> chars to the left
                    -1;
                if (rootend < 0)
                    throw new ArgumentException("Too many up-directories.", "relativePath");
            }

            // build the absolute path string
            path = String.Concat(
                    ((rootend == rootpath.Length) ? rootpath : rootpath.Substring(0, rootend)),
                    DirectorySeparatorString,
                    relativePath.Path);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Throws an exception is the path is empty. Used for argument check.
        /// </summary>
        /// <param name="argumentName">Argument name.</param>
        /// <exception cref="ArgumentException">Path is empty.</exception>
        public void EnsureNonEmpty(string argumentName)
        {
            if (IsEmpty)
                throw new ArgumentException(argumentName, CoreResources.GetString("path_is_empty"));
        }

        private void EnsureNonEmpty()
        {
            if (IsEmpty)
                throw new InvalidOperationException(CoreResources.GetString("path_is_empty"));
        }

        public bool Equals(FullPath other)
        {
            return EqualityComparer.Equals(this, other);
        }

        public override bool Equals(object other)
        {
            Debug.Assert(other == null || other is FullPath, "Comparing incomparable objects.");
            if (!(other is FullPath)) return false;
            return Equals(this, (FullPath)other);
        }

        public override int GetHashCode()
        {
            return EqualityComparer.GetHashCode(this);
        }

        public override string ToString()
        {
            return path;
        }

        public static implicit operator string(FullPath fullPath)
        {
            return fullPath.path;
        }

        public FullPath[]/*!*/ GetFiles()
        {
            EnsureNonEmpty();
            return GetFullPaths(DirectoryEx.GetFiles(this.path), false);
        }

        public FullPath[]/*!*/ GetDirectories()
        {
            EnsureNonEmpty();
            return GetFullPaths(DirectoryEx.GetDirectories(this.path), false);
        }

        public static FullPath[]/*!*/ GetFullPaths(string[]/*!*/ paths)
        {
            return GetFullPaths(paths, true);
        }

        internal static FullPath[]/*!*/ GetFullPaths(string[]/*!*/ paths, bool isArbitrary)
        {
            if (paths == null)
                throw new ArgumentNullException("paths");

            FullPath[] result = new FullPath[paths.Length];

            for (int i = 0; i < paths.Length; i++)
                result[i] = new FullPath(paths[i], isArbitrary);

            return result;
        }

        public static FullPath[]/*!*/ GetFullPaths(string[]/*!*/ paths, FullPath root)
        {
            if (paths == null)
                throw new ArgumentNullException("paths");

            root.EnsureNonEmpty("root");

            FullPath[] result = new FullPath[paths.Length];

            for (int i = 0; i < paths.Length; i++)
                result[i] = new FullPath(paths[i], root);

            return result;
        }

        public static FullPath GetCurrentDirectory()
        {
            return new FullPath(Directory.GetCurrentDirectory(), false);
        }

        /// <summary>
        /// Checks whether an extension of the path is contained in a list of extensions.
        /// </summary>
        public bool HasAnyExtension(IEnumerable<string>/*!*/ fileExtensions)
        {
            if (fileExtensions == null)
                throw new ArgumentNullException("fileExtensions");

            EnsureNonEmpty();

            // extension is either "" or contains ".":
            string path_ext = Path.GetExtension(this.path);

            foreach (string ext in fileExtensions)
            {
                if (ext == path_ext || path_ext.Length > 0 && String.Compare(ext, 0, path_ext, 1, path_ext.Length, StringComparison.CurrentCultureIgnoreCase) == 0)
                    return true;
            }
            return false;
        }

        #endregion
    }

    /// <summary>
    /// Represents a relative canonical path without a root binding.
    /// </summary>
    [Serializable]
    [DebuggerNonUserCode]
    public struct RelativePath : IComparable
    {
        /// <summary>
        /// The minimal number of directories we must go up or -1 if it is not possible to relativize the path.
        /// </summary>
        public int Level { get { return level; } }       // TODO: byte
        private readonly sbyte level;

        /// <summary>
        /// Gets path relative with respect to the directory <see cref="level"/> levels up from the 
        /// root specified in the constructor <see cref="RelativePath(FullPath,FullPath)"/>, 
        /// full drive-rooted path if relativization failed,
        /// or a <B>null</B> reference for empty path.
        /// </summary>
        /// <remarks>
        /// Canonical path without leading backslash.
        /// </remarks>
        public string Path { get { return path; } }
        private readonly string path;

        /// <summary>
        /// Gets whether the path is empty. 
        /// </summary>
        public bool IsEmpty
        {
            get { return path == null; }
        }

        public static RelativePath Empty = new RelativePath();

        /// <summary>
        /// Creates relative path using the number of up levels and canonical relative path.
        /// </summary>
        internal RelativePath(sbyte level, string/*!*/ path)
        {
            Debug.Assert(path != null);
            this.level = level;
            this.path = path;
        }

        /// <summary>
        /// Creates new RelativePath using combination of two relative paths.
        /// </summary>
        /// <param name="first">Path to be prepended.</param>
        /// <param name="second">Path to be appended.</param>
        public RelativePath(RelativePath/*!*/ first, RelativePath/*!*/ second)
        {
            char separator = System.IO.Path.DirectorySeparatorChar;

            Debug.AssertAllNonNull(first, second);
            Debug.Assert(first.level >= -1 && second.level >= -1);
            Debug.Assert(first.path == null || first.path[0] != separator);
            Debug.Assert(second.path == null || second.path[0] != separator);

            // non-relativizable second path => error
            if (second.level == -1)
            {
                throw new ArgumentException("Cannot combine non-relativizable path.", "second");
            }

            // no first path fast-track
            if (first.level == 0 && (first.path == null || first.path == "" || first.path == "."))
            {
                this.level = second.level;
                this.path = second.path;
                return;
            }

            if (second.level == 0)
            {
                // no second path fast-track
                if (second.path == null || second.path == "" || second.path == ".")
                {
                    this.level = first.level;
                    this.path = first.path;
                }
                // concat of first and second path
                else
                {
                    this.level = first.level;

                    if (first.path[first.path.Length - 1] == separator)
                        this.path = String.Concat(first.path, second.path);
                    else
                        this.path = String.Concat(first.path, separator.ToString(), second.path);
                }

                return;
            }

            // general case
            // following conditions are always true in this point:
            // first.level is not zero and first.path is not trivial (empty or .)
            // second is relative, level is larger than 0 and path not trivial (empty or .)
            sbyte remainingUps = second.level;
            int firstPosition = first.path.Length - 1;

            while (remainingUps > 0)
            {
                firstPosition--;

                //find the next directory separator
                while (firstPosition > -1)
                {
                    if (first.path[firstPosition] == separator) break;
                    firstPosition--;
                }

                //ran out of first path - use second path and add remaning ups to first path level
                if (firstPosition == -1)
                {
                    this.level = (sbyte)(remainingUps + first.level);
                    this.path = second.path;
                    return;
                }

                remainingUps--;
            }

            // truncate the first path (include the separator) and add the second path 
            this.level = first.level;
            this.path = String.Concat(first.path.Substring(0, firstPosition + 1), second.path);
        }

        /// <summary>
        /// Parses arbitrary path string to RelativePath. If the path is absolute, non-relativizable path is created.
        /// </summary>
        /// <param name="path">Path in generic format.</param>
        public RelativePath(string/*!*/ path)
        {
            Debug.AssertAllNonNull(path);

            char separator = System.IO.Path.DirectorySeparatorChar;

            // replace all alt separators by the correct one
            path = path.Replace(System.IO.Path.AltDirectorySeparatorChar, separator);

            // TODO: if path is absolute, we need to do some additional checking
            if (path.Length > 3 && path[1] == ':' && path[2] == separator)
            {
                //Windows-style absolute path - we keep the drive letter
                this.level = -1;
                this.path = path;
                return;
            }

            if (path.Length > 0 && path[0] == separator)
            {
                //Unix-style absolute path - we remove separator in the beginning (absolute path information is kept in the level -1)
                this.level = -1;
                this.path = path.Substring(1);
                return;
            }

            sbyte level = 0;
            List<string> realElements = new List<string>();

            string[] elements = path.Split(separator);

            // go through path elements, assembling the real path
            foreach (string elem in elements)
            {
                switch (elem)
                {
                    case "":
                    case ".":
                        // does not change the directory
                        continue;
                    case "..":
                        // jump one directory up
                        if (realElements.Count > 0)
                            realElements.RemoveAt(realElements.Count - 1);
                        else
                            level++;
                        continue;
                    default:
                        // add the directory
                        realElements.Add(elem);
                        continue;
                }
            }

            StringBuilder sb = new StringBuilder();

            //make path string
            for (int i = 0; i < realElements.Count; i++)
            {
                sb.Append(realElements[i]);

                if (i != realElements.Count - 1)
                    sb.Append(separator);
            }

            // if there was trailing separator in the path, append it explicitly
            if (elements.Length > 1 && elements[elements.Length - 1] == "" && sb.Length > 0)
            {
                sb.Append(separator);
            }

            this.level = level;
            this.path = sb.ToString();
        }

        /// <summary>
        /// Creates a relative path to the file/directory specified by <paramref name="path"/> 
        /// with respect to the directory <paramref name="root"/>.
        /// </summary>
        /// <param name="root">The root directory where to start.</param>
        /// <param name="path">The path where to end.</param>
        public RelativePath(FullPath root, FullPath path)
        {
            root.EnsureNonEmpty("root");
            path.EnsureNonEmpty("path");

            string srcDir = ((string)root).Replace(System.IO.Path.AltDirectorySeparatorChar, System.IO.Path.DirectorySeparatorChar);
            string dstPath = ((string)path).Replace(System.IO.Path.AltDirectorySeparatorChar, System.IO.Path.DirectorySeparatorChar);
            char separator = System.IO.Path.DirectorySeparatorChar;

            Debug.Assert(srcDir != "" && dstPath != "");

            // removes separator from the end of the directory path:
            if (srcDir[srcDir.Length - 1] == separator)
                srcDir = srcDir.Substring(0, srcDir.Length - 1);

            int first_different = StringUtils.FirstDifferent(srcDir, dstPath, true);
            if (first_different == 0)
            {
                // different volumes:
                this.path = dstPath;
                this.level = -1;
                return;
            }

            // dst is subdirectory of src (substring):
            if (first_different == srcDir.Length && (first_different == dstPath.Length ||
                  dstPath[first_different] == separator))
            {
                this.path = "";
                if (first_different < dstPath.Length)
                    this.path = dstPath.Substring(first_different + 1);

                this.level = 0;
                return;
            }

            // src is subdirectory of dst (substring):
            if (first_different == dstPath.Length && first_different < srcDir.Length && srcDir[first_different] == separator)
            {
                this.level = 0;
                for (int i = srcDir.Length - 1; i >= first_different; i--)
                {
                    if (srcDir[i] == separator)
                        this.level++;
                }

                this.path = "";
                return;
            }

            int last_common_separator = srcDir.LastIndexOf(separator, first_different - 1);
            Debug.Assert(last_common_separator != -1);

            this.level = 0;
            for (int i = srcDir.Length - 1; i >= last_common_separator; i--)
            {
                if (srcDir[i] == separator)
                    this.level++;
            }

            this.path = dstPath.Substring(last_common_separator + 1);
        }

        /// <summary>
        /// Absolutizes this relative path with respect to a specified root.
        /// </summary>
        /// <param name="root">Full root path.</param>
        /// <returns>Absolute path.</returns>
        public FullPath ToFullPath(FullPath root)
        {
            root.EnsureNonEmpty("root");

            if (level <= 0) return new FullPath(CombinePath(root, path), false);

            string root_str = root;
            char separator = System.IO.Path.DirectorySeparatorChar;

            // starts at the end of the root skipping the separator:
            int separator_pos = (root_str[root_str.Length - 1] == separator) ? root_str.Length - 1 : root_str.Length;

            for (int i = 0; i < level; i++)
            {
                separator_pos = root_str.LastIndexOf(separator, separator_pos - 1);
                if (separator_pos == -1)
                    throw new ArgumentException("root"); // TODO // not a valid root for this path
            }

            return new FullPath(String.Concat(root_str.Substring(0, separator_pos + 1), this.path), false);
        }

        private static string CombinePath(FullPath root, string path)
        {
            if (CultureInfo.InvariantCulture.TextInfo.ToLower((string)root).StartsWith("http://")) // we don't need Unicode characters to be lowercased properly // CurrentCulture is slow
                return System.IO.Path.Combine(root, path).Replace('\\', '/');
            else
                return System.IO.Path.Combine(root, path);
        }

        /// <summary>
        /// Returns canonical string representation of the relative path.
        /// </summary>
        /// <returns>A relative path, e.g. "../../dir/file.extension".</returns>
        public override string ToString()
        {
            if (level <= 0) return path;
            Debug.Assert(path != null);

            StringBuilder result = new StringBuilder(level * 3 + path.Length);

            string level_up = ".." + System.IO.Path.DirectorySeparatorChar;

            for (int i = 0; i < level; i++)
                result.Append(level_up);

            if (path != "")
                result.Append(path);
            else
                result.Length--;

            return result.ToString();
        }

        /// <summary>
        /// Parses canonical relative path. 
        /// </summary>
        /// <param name="relativePath">
        /// Canonical path. Assumes path separators to be <see cref="System.IO.Path.DirectorySeparatorChar"/> and
        /// level-ups ".." to be only at the start of the string.
        /// </param>
        /// <exception cref="PathTooLongException">Number of level-ups is greater than <see cref="SByte.MaxValue"/>.</exception>
        internal static RelativePath ParseCanonical(string/*!*/ relativePath)
        {
            Debug.Assert(relativePath != null);

            // counts number of level-ups "..\"
            int level = 0;
            int i = 0;
            while (i + 1 < relativePath.Length && relativePath[i] == '.' && relativePath[i + 1] == '.')
            {
                Debug.Assert(i + 2 == relativePath.Length || relativePath[i + 2] == System.IO.Path.DirectorySeparatorChar);
                i += 3;
                level++;
            }

            if (level > SByte.MaxValue)
                throw new PathTooLongException();

            // remaining path:
            string path = (i < relativePath.Length) ? relativePath.Substring(i) : "";

            return new RelativePath((sbyte)level, path);
        }

        public override int GetHashCode()
        {
            return unchecked(StringComparer.InvariantCultureIgnoreCase.GetHashCode(path) ^ (level << 30));
        }

        public override bool Equals(object other)
        {
            if (!(other is RelativePath)) return false;
            RelativePath rp = (RelativePath)other;
            return level == rp.level && String.Compare(path, rp.path, StringComparison.CurrentCultureIgnoreCase) == 0;
        }

        public bool Equals(RelativePath other)
        {
            return this.level == other.level && String.Compare(this.path, other.path, StringComparison.CurrentCultureIgnoreCase) == 0;
        }

        #region IComparable Members

        public int CompareTo(object obj)
        {
            RelativePath other = (RelativePath)obj;
            if (this.level == other.level || this.level < 0)
                return String.Compare(this.path, other.path, StringComparison.CurrentCultureIgnoreCase);
            return other.level - this.level;                     // TODO:
        }

        #endregion

        #region Unit Tests
#if DEBUG

        [Test]
        static void TestPaths()
        {
            FullPath root;
            FullPath full;
            RelativePath rel;
            string str1, str2;

            string[,] cases = 
      {
          // root:        // full:          // full canonical:    // relative canonical:
        { @"C:\a/b/c/",   @"D:\a/b/",       @"D:\a\b\",           @"D:\a\b\" },
                                            
        { @"C:\a\b\c",    @"C:\a\b\c",      @"C:\a\b\c",          @"" },
        { @"C:\a\b\c",    @"C:\a\b\c\",     @"C:\a\b\c\",         @"" },
        { @"C:\a\b\c\",   @"C:\a\b\c",      @"C:\a\b\c",          @"" },
        { @"C:\a\b\c\",   @"C:\a\b\c\",     @"C:\a\b\c\",         @"" },
                                            
        { @"C:\a\b\c",    @"C:\a\b",        @"C:\a\b",            @".." },
        { @"C:\a\b\c\",   @"C:\a\b",        @"C:\a\b",            @".." },
                                            
        { @"C:\a\b\c",    @"C:\",           @"C:\",                @"..\..\.." },
        { @"C:\a\b\c\",   @"C:\",           @"C:\",                @"..\..\.." },
                                            
        { @"C:\a\b\c\",   @"C:\a\b\x\y\z",  @"C:\a\b\x\y\z",      @"..\x\y\z" },
        { @"C:\a\b\cd\",  @"C:\a\b\c",      @"C:\a\b\c",          @"..\c" },
        { @"C:\a\b\cd",   @"C:\a\b\c",      @"C:\a\b\c",          @"..\c" },
        { @"C:\a\b\cd\",  @"C:\a\b\c\d",    @"C:\a\b\c\d",          @"..\c\d" },
        { @"C:\a\b\cd",   @"C:\a\b\c\d",    @"C:\a\b\c\d",          @"..\c\d" },
      };

            for (int i = 0; i < cases.GetLength(0); i++)
            {
                root = new FullPath(cases[i, 0]);
                full = new FullPath(cases[i, 1]);
                rel = new RelativePath(root, full);

                if (full.ToString() != cases[i, 2])
                    Debug.Fail();

                if (rel.ToString() != cases[i, 3])
                    Debug.Fail();

                str1 = full;
                if (str1[str1.Length - 1] == '\\') str1 = str1.Substring(0, str1.Length - 1);

                str2 = rel.ToFullPath(root);
                if (str2[str2.Length - 1] == '\\') str2 = str2.Substring(0, str2.Length - 1);

                if (str1 != str2)
                    Debug.Fail();

                if (RelativePath.ParseCanonical(cases[i, 3]).ToString() != cases[i, 3])
                    Debug.Fail();
            }
        }

#endif
        #endregion

    }

    #endregion

    /// <summary>
    /// File system utilities.
    /// </summary>
    [DebuggerNonUserCode]
    public static partial class FileSystemUtils
    {
        /// <summary>
        /// Returns the given URL without the username/password information.
        /// </summary>
        /// <remarks>
        /// Removes the text between the last <c>"://"</c> and the following <c>'@'</c>.
        /// Does not check the URL for validity. Works for php://filter paths too.
        /// </remarks>
        /// <param name="url">The URL to modify.</param>
        /// <returns>The given URL with the username:password section replaced by <c>"..."</c>.</returns>
        public static string StripPassword(string url)
        {
            if (url == null) return null;

            int url_start = url.LastIndexOf("://");
            if (url_start > 0)
            {
                url_start += "://".Length;
                int pass_end = url.IndexOf('@', url_start);
                if (pass_end > url_start)
                {
                    StringBuilder sb = new StringBuilder(url.Length);
                    sb.Append(url.Substring(0, url_start));
                    sb.Append("...");
                    sb.Append(url.Substring(pass_end));  // results in: scheme://...@host
                    return sb.ToString();
                }
            }
            return url;
        }


        /// <summary>
        /// Converts path to standardized form using '/' as only directory separator
        /// </summary>
        /// <param name="path">Path to be canonicalized</param>
        /// <returns>Canonicalized path</returns>
        public static String CanonicalizePath(string path)
        {
            return path.Replace('\\', '/');
        }

        public static int FileSize(FileInfo fi)//TODO: Move this to PlatformAdaptationLayer
        {
            if (EnvironmentUtils.IsDotNetFramework)
            {
                // we are not calling full stat(), it is slow
                return (int)fi.Length;
            }
            else
            {
                //bypass Mono bug in FileInfo.Length
                using (FileStream stream = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    return unchecked((int)stream.Length);
                }
            }
        }

    }

    #endregion

    #region Environment Utils

    /// <summary>
    /// Utilities related to environment.
    /// </summary>
    public static class EnvironmentUtils
    {
        /// <summary>
        /// Gets whether the CLR runtime is genuine Microsoft .NET Framework.
        /// </summary>
        /// <remarks>
        /// There should be as least decisions based on this value as possible.
        /// However, some features are not yet available under Mono.
        /// </remarks>
        public static bool IsDotNetFramework
        {
            get
            {
                if (!isDotNetFramework.HasValue)
                {
                    object[] attrs = typeof(int).Assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                    isDotNetFramework = (attrs.Length > 0 && ((AssemblyProductAttribute)attrs[0]).Product == "Microsoft\x00ae .NET Framework");
                }
                return (bool)isDotNetFramework;
            }
        }
        private static bool? isDotNetFramework;

        /// <summary>
        /// Determine if the current OS is Windows.
        /// </summary>
        public static bool IsWindows
        {
            get
            {
                var os = Environment.OSVersion;
                return os.Platform != PlatformID.MacOSX && os.Platform != PlatformID.Unix;
            }
        }
    }

    #endregion

    #region Date and Time

    ///// <summary>
    ///// Fixes the strange behavior of <see cref="System.TimeZone"/> which translates between local and
    ///// UTC using current time zone, completely disregarding the current zone's UTC offset.
    ///// </summary>
    //public abstract class CustomTimeZoneBase : TimeZone
    //{
    //    public override DateTime ToLocalTime(DateTime time)
    //    {
    //        TimeSpan offset = GetUtcOffset(time);
    //        DateTime local = new DateTime((time + offset).Ticks, DateTimeKind.Local);

    //        // was the offset correct?
    //        offset = GetUtcOffset(local);
    //        if (local - offset != time)
    //        {
    //            return new DateTime((time + offset).Ticks, DateTimeKind.Local);
    //        }
    //        else return local;
    //    }

    //    public override DateTime ToUniversalTime(DateTime time)
    //    {
    //        return new DateTime((time - GetUtcOffset(time)).Ticks, DateTimeKind.Utc);
    //    }
    //}

    /// <summary>
    /// Unix TimeStamp to DateTime conversion and vice versa
    /// </summary>
    public static class DateTimeUtils
    {
        #region Nested Class: UtcTimeZone, GmtTimeZone

        //private sealed class _UtcTimeZone : CustomTimeZoneBase
        //{
        //    public override string DaylightName { get { return "UTC"; } }
        //    public override string StandardName { get { return "UTC"; } }

        //    public override TimeSpan GetUtcOffset(DateTime time)
        //    {
        //        return new TimeSpan(0);
        //    }

        //    public override DaylightTime GetDaylightChanges(int year)
        //    {
        //        return new DaylightTime(new DateTime(0), new DateTime(0), new TimeSpan(0));
        //    }


        //}

        //private sealed class _GmtTimeZone : CustomTimeZoneBase
        //{
        //    public override string DaylightName { get { return "GMT Daylight Time"; } }
        //    public override string StandardName { get { return "GMT Standard Time"; } }

        //    public override TimeSpan GetUtcOffset(DateTime time)
        //    {
        //        return IsDaylightSavingTime(time) ? new TimeSpan(0, +1, 0, 0, 0) : new TimeSpan(0);
        //    }
        //    public override DaylightTime GetDaylightChanges(int year)
        //    {
        //        return new DaylightTime
        //        (
        //          new DateTime(year, 3, 27, 1, 0, 0),
        //          new DateTime(year, 10, 30, 2, 0, 0),
        //          new TimeSpan(0, +1, 0, 0, 0)
        //        );
        //    }
        //}

        #endregion

        /// <summary>
        /// Time 0 in terms of Unix TimeStamp.
        /// </summary>
        public static readonly DateTime/*!*/UtcStartOfUnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0);

        /// <summary>
        /// UTC time zone.
        /// </summary>
        public static TimeZoneInfo/*!*/UtcTimeZone { get { return TimeZoneInfo.Utc; } }

        /// <summary>
        /// Converts <see cref="DateTime"/> representing UTC time to UNIX timestamp.
        /// </summary>
        /// <param name="dt">Time.</param>
        /// <returns>Unix timestamp.</returns>
        public static int UtcToUnixTimeStamp(DateTime dt)
        {
            double seconds = (dt - UtcStartOfUnixEpoch).TotalSeconds;

            if (seconds < Int32.MinValue)
                return Int32.MinValue;
            if (seconds > Int32.MaxValue)
                return Int32.MaxValue;

            return (int)seconds;
        }

        /// <summary>
        /// Converts UNIX timestamp (number of seconds from 1.1.1970) to <see cref="DateTime"/>.
        /// </summary>
        /// <param name="timestamp">UNIX timestamp</param>
        /// <returns><see cref="DateTime"/> structure representing UTC time.</returns>
        public static DateTime UnixTimeStampToUtc(int timestamp)
        {
            return UtcStartOfUnixEpoch + TimeSpan.FromSeconds(timestamp);
        }
#if !SILVERLIGHT
        /// <summary>
        /// Gets the daylight saving time difference between two dates.
        /// </summary>
        /// <param name="src">Source date.</param>
        /// <param name="dst">Destination date.</param>
        /// <returns>
        /// The time span that has to be added to the source date's Daylight Saving Time Delta to get 
        /// destination date's one.
        /// </returns>
        public static TimeSpan GetDaylightTimeDifference(DateTime src, DateTime dst)
        {
            TimeZone zone = TimeZone.CurrentTimeZone;

            if (src.Kind != DateTimeKind.Local) src = zone.ToLocalTime(src);
            if (dst.Kind != DateTimeKind.Local) dst = zone.ToLocalTime(dst);

            DaylightTime src_dt = zone.GetDaylightChanges(src.Year);
            DaylightTime dst_dt = zone.GetDaylightChanges(dst.Year);

            // difference between DST of src and dst:
            return
              (TimeZone.IsDaylightSavingTime(dst, dst_dt) ? dst_dt.Delta : TimeSpan.Zero) -
              (TimeZone.IsDaylightSavingTime(src, src_dt) ? src_dt.Delta : TimeSpan.Zero);
        }
#endif

        /// <summary>
        /// Determine maximum of three given <see cref="DateTime"/> values.
        /// </summary>
        public static DateTime Max(DateTime d1, DateTime d2, DateTime d3)
        {
            return (d1 < d2) ? ((d2 < d3) ? d3 : d2) : ((d1 < d3) ? d3 : d1);
        }
#if DEBUG

        static FieldInfo TimeZone_CurrentTimeZone = null;

        /// <summary>
        /// Sets system current time zone (for debugging purposes only).
        /// </summary>
        public static void SetCurrentTimeZone(TimeZone/*!*/ zone)
        {
            Debug.Assert(zone != null);

            if (TimeZone_CurrentTimeZone == null)
                TimeZone_CurrentTimeZone = typeof(TimeZone).GetField("currentTimeZone", BindingFlags.NonPublic | BindingFlags.Static);
            
            Debug.Assert(TimeZone_CurrentTimeZone != null, "Missing private field of TimeZone class.");
            TimeZone_CurrentTimeZone.SetValue(null, zone);
        }

#endif


        //		private static TimeZone GetTimeZoneFromRegistry(TimeZone/*!*/ zone)
        //		{
        //		  try
        //		  {
        //		    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(
        //		      @"Software\Microsoft\Windows NT\CurrentVersion\Time Zones\" + zone.StandardName,false))
        //		    {
        //  		    if (key == null) return null;
        //		      
        //		      byte[] tzi = key.GetValue("TZI") as byte[];
        //		      if (tzi == null) continue;
        //    		    
        //    		  int bias = BitConverter.ToInt32(tzi,0);
        //    		  
        //  		  }  
        //		  }
        //		  catch (Exception)
        //		  {
        //		  }
        //
        //		  return null;
        //		}		
    }

    #endregion

    #region Threading
    //TODO: do something with this #if
#if !SILVERLIGHT

    /// <summary>
    /// Implements cache mechanism to be used in multi-threaded environment.
    /// </summary>
    /// <typeparam name="K">The cache key type.</typeparam>
    /// <typeparam name="T">The cache value type.</typeparam>
    public class SynchronizedCache<K, T>
    {
        /// <summary>
        /// The lock used to access the cache synchronously. Cannot be null.
        /// </summary>
        private readonly ReaderWriterLockSlim/*!*/cacheLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        /// <summary>
        /// Cached values. Cannot be null.
        /// </summary>
        private readonly Dictionary<K, T>/*!*/innerCache = new Dictionary<K, T>();

        /// <summary>
        /// Amount of items in the cache dictionary.
        /// </summary>
        public int Count
        {
            get
            {
                return innerCache.Count;
            }
        }

        /// <summary>
        /// The update function used when cache miss. Cannot be null.
        /// </summary>
        private readonly Func<K, T>/*!*/updateFunction;

        /// <summary>
        /// Initialize the new instance of SynchronizedCache object.
        /// </summary>
        /// <param name="updateFunction">The update function used when cache miss.
        /// Note the function is called within the write lock exclusively.</param>
        public SynchronizedCache(Func<K, T>/*!*/updateFunction)
        {
            if (updateFunction == null)
                throw new ArgumentNullException("updateFunction");

            //
            this.updateFunction = updateFunction;
        }

        /// <summary>
        /// Try to get an item from the cache. If the given <paramref name="key"/> is not found,
        /// the <see cref="updateFunction"/> is used to create new item.
        /// </summary>
        /// <param name="key">The key of the item.</param>
        /// the cache does not contain given <paramref name="key"/> yet.
        /// <returns>The item according to the given <paramref name="key"/>.</returns>
        [DebuggerNonUserCode]
        public T Get(K key)
        {
            T result;

            // TODO (J): 2-gen cache, persistent readonly cache without locks

            // try to find the value in the cache first
            cacheLock.EnterUpgradeableReadLock();
            try
            {
                if (innerCache.TryGetValue(key, out result))
                    return result;

                // upgrade to write lock and add new value into the cache
                cacheLock.EnterWriteLock();
                try
                {
                    // double check the lock, the item could be added while the thread was waiting for the writer lock
                    if (innerCache.TryGetValue(key, out result))
                        return result;

                    // only here, the Get method can be called recursively

                    // add the value into the cache
                    // create new value synchronously here
                    innerCache.Add(key, (result = updateFunction(key)));

                    //
                    return result;
                }
                finally
                {
                    cacheLock.ExitWriteLock();
                }
            }
            finally
            {
                cacheLock.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        /// Update the value with the given <paramref name="key"/> using specified <paramref name="updateFunction"/>.
        /// </summary>
        /// <param name="key">Key of the value to be updated or added.</param>
        /// <param name="updateFunction">The update function used to get the value of the item. The parameter cannot be null.</param>
        /// <returns>The value of the item with given <paramref name="key"/>.</returns>
        [DebuggerNonUserCode]
        public T Update(K key, Func<K, T>/*!*/updateFunction)
        {
            Debug.Assert(updateFunction != null);

            cacheLock.EnterWriteLock();
            try
            {
                // update the value in the cache
                // create new value synchronously here
                return innerCache[key] = updateFunction(key);
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Update the value with the given <paramref name="key"/> using default <see cref="updateFunction"/>.
        /// </summary>
        /// <param name="key">Key of the value to be updated or added.</param>
        /// <returns>The value of the item with given <paramref name="key"/>.</returns>
        [DebuggerNonUserCode]
        public T Update(K key)
        {
            return Update(key, updateFunction);
        }
    }

#else

    /// <summary>
    /// Implements cache mechanism to be used in multi-threaded environment.
    /// </summary>
    /// <typeparam name="K">The cache key type.</typeparam>
    /// <typeparam name="T">The cache value type.</typeparam>
    public class SynchronizedCache<K, T>
    {
        /// <summary>
        /// The lock used to access the cache synchronously. Cannot be null.
        /// </summary>
        private readonly object/*!*/cacheLock = new object();

        /// <summary>
        /// Cached values. Cannot be null.
        /// </summary>
        private readonly Dictionary<K, T>/*!*/innerCache = new Dictionary<K, T>();

        /// <summary>
        /// The update function used when cache miss. Cannot be null.
        /// </summary>
        private readonly Func<K, T>/*!*/updateFunction;

        /// <summary>
        /// Initialize the new instance of SynchronizedCache object.
        /// </summary>
        /// <param name="updateFunction">The update function used when cache miss.
        /// Note the function is called within the lock.</param>
        public SynchronizedCache(Func<K, T>/*!*/updateFunction)
        {
            if (updateFunction == null)
                throw new ArgumentNullException("updateFunction");

            //
            this.updateFunction = updateFunction;
        }

        /// <summary>
        /// Try to get an item from the cache. If the given <paramref name="key"/> is not found,
        /// the <see cref="updateFunction"/> is used to create new item.
        /// </summary>
        /// <param name="key">The key of the item.</param>
        /// the cache does not contain given <paramref name="key"/> yet.
        /// <returns>The item according to the given <paramref name="key"/>.</returns>
        public T Get(K key)
        {
            T result;

            // try to find the value in the cache first
            lock(cacheLock)
            {
                if (innerCache.TryGetValue(key, out result))
                    return result;

                // add the value into the cache
                // create new value synchronously here
                innerCache.Add(key, (result = updateFunction(key)));
                return result;
            }
        }

        /// <summary>
        /// Update the value with the given <paramref name="key"/> using specified <paramref name="updateFunction"/>.
        /// </summary>
        /// <param name="key">Key of the value to be updated or added.</param>
        /// <param name="updateFunction">The update function used to get the value of the item. The parameter cannot be null.</param>
        /// <returns>The value of the item with given <paramref name="key"/>.</returns>
        public T Update(K key, Func<K, T>/*!*/updateFunction)
        {
            Debug.Assert(updateFunction != null);

            lock(cacheLock)
            {
                // update the value in the cache
                // create new value synchronously here
                return innerCache[key] = updateFunction(key);
            }
        }

        /// <summary>
        /// Update the value with the given <paramref name="key"/> using default <see cref="updateFunction"/>.
        /// </summary>
        /// <param name="key">Key of the value to be updated or added.</param>
        /// <returns>The value of the item with given <paramref name="key"/>.</returns>
        public T Update(K key)
        {
            return Update(key, updateFunction);
        }
    }


#endif

    #endregion

    #region Delegates

    public static class DelegateExtensions
    {
        /// <summary>
        /// Combine with another predicate function. Both functions must return true.
        /// </summary>
        /// <typeparam name="T">The type of predicate functions argument.</typeparam>
        /// <param name="predicate1">This predicate. Can be null.</param>
        /// <param name="predicate2">Another predicate. Can be null.</param>
        /// <returns>Combination of two given predicates or null if both arguments are null.</returns>
        public static Predicate<T> AndAlso<T>(this Predicate<T> predicate1, Predicate<T> predicate2)
        {
            if (predicate1 == null && predicate2 == null)
                return null;

            if (predicate1 == null)
                return predicate2;

            if (predicate2 == null)
                return predicate1;

            // else combine
            return (arg) => predicate1(arg) && predicate2(arg);
        }

        /// <summary>
        /// Combine with another predicate function. Predicates will be processed sequentially until one pass.
        /// </summary>
        /// <typeparam name="T">The type of predicate functions argument.</typeparam>
        /// <param name="predicate1">This predicate. Can be null.</param>
        /// <param name="predicate2">Another predicate. Can be null.</param>
        /// <returns>Combination of two given predicates or null if both arguments are null.</returns>
        public static Predicate<T> OrElse<T>(this Predicate<T> predicate1, Predicate<T> predicate2)
        {
            if (predicate1 == null && predicate2 == null)
                return null;

            if (predicate1 == null)
                return predicate2;

            if (predicate2 == null)
                return predicate1;

            // else combine
            return (arg) => predicate1(arg) || predicate2(arg);
        }
    }

    #endregion

    public enum DfsStates
    {
        Initial,
        Entered,
        Done
    }
}
