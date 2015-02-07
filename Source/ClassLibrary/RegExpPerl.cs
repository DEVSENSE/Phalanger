/*

 Copyright (c) 2004-2006 Pavel Novak and Tomas Matousek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

  TODO: preg_match - unmatched groups should be empty only if they are not followed by matched one (isn't it PHP bug?)
  TODO: preg_last_error - Returns the error code of the last PCRE regex execution

*/

using System;
using System.Text;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

using PHP.Core;
using PHP.Core.Reflection;

namespace PHP.Library
{
	/// <summary>
	/// Perl regular expression specific options that are not captured by .NET <see cref="RegexOptions"/> or by
	/// transformation of the regular expression itself.
	/// </summary>
	[Flags]
	public enum PerlRegexOptions
	{
		None = 0,
		Evaluate = 1,
		Ungreedy = 2,
		Anchored = 4,
		DollarMatchesEndOfStringOnly = 8,
		UTF8 = 16
	}

	/// <summary>
	/// Implements PERL extended regular expressions as they are implemented in PHP.
	/// </summary>
	/// <threadsafety static="true"/>
    [ImplementsExtension(LibraryDescriptor.ExtPcre)]
	public static class PerlRegExp
    {
        #region preg_last_error

        [ImplementsFunction("preg_last_error")]
        public static int LastError()
        {
            return 0;
        }

        #endregion
        #region preg_quote

        /// <summary>
		/// <para>Escapes all characters that have special meaning in regular expressions. These characters are
		/// . \\ + * ? [ ^ ] $ ( ) { } = ! &lt; &gt; | :</para>
		/// </summary>
		/// <param name="str">String with characters to escape.</param>
		/// <returns>String with escaped characters.</returns>
		[ImplementsFunction("preg_quote")]
		[PureFunction]
        public static string Quote(string str)
		{
			return Quote(str, '\0', false);
		}

		/// <summary>
		/// <para>Escapes all characters that have special meaning in regular expressions. These characters are
		/// . \\ + * ? [ ^ ] $ ( ) { } = ! &lt; &gt; | : plus <paramref name="delimiter"/>.</para>
		/// </summary>
		/// <param name="str">String with characters to escape.</param>
		/// <param name="delimiter">Character to escape in additon to general special characters.</param>
		/// <returns>String with escaped characters.</returns>
		[ImplementsFunction("preg_quote")]
        [PureFunction]
        public static string Quote(string str, string delimiter)
		{
			bool delimiter_used = true;
			if (delimiter == null || delimiter.Length == 0)
				delimiter_used = false;

			return Quote(str, delimiter_used ? delimiter[0] : '\0', delimiter_used);
		}

		/// <summary>
		/// Escapes all characters with special meaning in Perl regular expressions and char
		/// <paramref name="delimiter"/>.
		/// </summary>
		/// <param name="str">String to quote.</param>
		/// <param name="delimiter">Additional character to quote.</param>
		/// <param name="delimiterUsed">Whether the delimiter should be quoted.</param>
		/// <returns>String with quoted characters.</returns>
		internal static string Quote(string str, char delimiter, bool delimiterUsed)
		{
			if (str == null)
				return null;

			StringBuilder result = new StringBuilder();

			for (int i = 0; i < str.Length; i++)
			{
				bool escape = false;

				if (delimiterUsed && str[i] == delimiter)
					escape = true;
				else
					// switch only if true is not set already
					switch (str[i])
					{
						case '\\':
						case '+':
						case '*':
						case '?':
						case '[':
						case '^':
						case ']':
						case '$':
						case '(':
						case ')':
						case '{':
						case '}':
						case '=':
						case '!':
						case '<':
						case '>':
						case '|':
						case ':':
						case '.':
							escape = true;
							break;
					}

				if (escape)
					result.Append('\\');

				result.Append(str[i]);
			}

			return result.ToString();
		}

		#endregion

		#region preg_grep

		/// <summary>
		/// Flags for Grep functions.
		/// </summary>
		[Flags]
		public enum GrepFlags
		{
			None = 0,
			[ImplementsConstant("PREG_GREP_INVERT")]
			GrepInvert = 1
		}

		/// <summary>
		/// Returns the array consisting of the elements of the <paramref name="input"/> array that match
		/// the given <paramref name="pattern"/>.
		/// </summary>
		/// <param name="pattern">Pattern to be matched against each array element.</param>
		/// <param name="input">Array of strings to match.</param>
		/// <returns>Array containing only values from <paramref name="input"/> that match <paramref name="pattern"/>
		/// </returns>
		[ImplementsFunction("preg_grep")]
		public static PhpArray Grep(object pattern, PhpArray input)
		{
			return Grep(pattern, input, GrepFlags.None);
		}

		/// <summary>
		/// <para>Returns the array consisting of the elements of the <paramref name="input"/> array that match
		/// the given <paramref name="pattern"/>.</para>
		/// <para>If <see cref="GrepFlags.GrepInvert"/> flag is specified, resulting array will contain
		/// elements that do not match the <paramref name="pattern"/>.</para>
		/// </summary>
		/// <param name="pattern">Pattern to be matched against each array element.</param>
		/// <param name="input">Array of strings to match.</param>
		/// <param name="flags">Flags modifying which elements contains resulting array.</param>
		/// <returns>Array containing only values from <paramref name="input"/> that match <paramref name="pattern"/>.
		/// (Or do not match according to <paramref name="flags"/> specified.)</returns>
		[ImplementsFunction("preg_grep")]
		public static PhpArray Grep(object pattern, PhpArray input, GrepFlags flags)
		{
			if (input == null)
				return null;

			PerlRegExpConverter converter = ConvertPattern(pattern, null);
			if (converter == null) return null;

			PhpArray result = new PhpArray();
			foreach (KeyValuePair<IntStringKey, object> entry in input)
			{
				string str = ConvertData(entry.Value, converter);
				Match m = converter.Regex.Match(str);

				// move a copy to return array if success and not invert or
				// not success and invert
				if (m.Success ^ (flags & GrepFlags.GrepInvert) != 0)
					result.Add(entry.Key, str);
			}

			return result;
		}

		#endregion

		#region preg_match, preg_match_all

		/// <summary>
		/// Flags for Match function family.
		/// </summary>
		/// <remarks>
		/// MatchFlags used by pre_match PHP functions is a hybrid enumeration.
		/// PatternOrder and SetOrder flags are mutually exclusive but OffsetCapture may be added by bitwise | operator.
		/// Moreover, PatternOrder is a default value used by these functions, so it can be equal to 0.
		/// (This confusing declaration is done by PHP authors.)
		///	</remarks>
		[Flags]
		public enum MatchFlags
		{
			[ImplementsConstant("PREG_PATTERN_ORDER")]
			PatternOrder = 1,
			[ImplementsConstant("PREG_SET_ORDER")]
			SetOrder = 2,
			[ImplementsConstant("PREG_OFFSET_CAPTURE")]
			OffsetCapture = 0x100
		}

		/// <summary>
		/// Searches <paramref name="data"/> for a match to the regular expression given in <paramref name="pattern"/>.
		/// The search is stopped after the first match is found.
		/// </summary>
		/// <param name="pattern">Perl regular expression.</param>
		/// <param name="data">String to search.</param>
		/// <returns>0 if there is no match and 1 if the match was found.</returns>
		[ImplementsFunction("preg_match")]
		[return: CastToFalse]
		public static int Match(object pattern, object data)
		{
			PerlRegExpConverter converter = ConvertPattern(pattern, null);
			if (converter == null) return -1;

			string str = ConvertData(data, converter);
			Match match = converter.Regex.Match(str);
			return match.Success ? 1 : 0;
		}

		/// <summary>
		/// <para>Searches <paramref name="data"/> for a match to the regular expression given in
		/// <paramref name="pattern"/>. The search is stopped after the first match is found.</para>
		/// <para><paramref name="matches"/> contains an array with matches. At index 0 is the whole string that
		/// matches the <paramref name="pattern"/>, from index 1 are stored matches for parenthesized subpatterns.</para>
		/// </summary>
		/// <param name="pattern">Perl regular expression.</param>
		/// <param name="data">String or string of bytes to search.</param>
		/// <param name="matches">Array containing matched strings.</param>
		/// <returns>0 if there is no match and 1 if the match was found.</returns>
		[ImplementsFunction("preg_match")]
		[return: CastToFalse]
		public static int Match(object pattern, object data, out PhpArray matches)
		{
			return Match(pattern, data, out matches, MatchFlags.PatternOrder, 0, false);
		}

		/// <summary>
		/// <para>Searches <paramref name="data"/> for a match to the regular expression given in
		/// <paramref name="pattern"/>. The search is stopped after the first match is found.</para>
		/// <para><paramref name="matches"/> contains an array with matches. At index 0 is the whole string that
		/// matches the <paramref name="pattern"/>, from index 1 are stored matches for parenthesized subpatterns.</para>
		/// <para>Flag <see cref="MatchFlags.OffsetCapture"/> can be specified and it means that the
		/// <paramref name="matches"/> array will not contain substrings, but another array where the substring
		/// is stored at index [0] and index [1] is its offset in <paramref name="data"/>.</para>
		/// </summary>
		/// <param name="pattern">Perl regular expression.</param>
		/// <param name="data">String to search.</param>
		/// <param name="matches">Array containing matched strings.</param>
		/// <param name="flags"><see cref="MatchFlags"/>.</param>
		/// <returns>0 if there is no match and 1 if the match was found.</returns>
		[ImplementsFunction("preg_match")]
		[return: CastToFalse]
		public static int Match(object pattern, object data, out PhpArray matches, MatchFlags flags)
		{
			return Match(pattern, data, out matches, flags, 0, false);
		}

		/// <summary>
		/// <para>Searches <paramref name="data"/> for a match to the regular expression given in
		/// <paramref name="pattern"/>. The search is stopped after the first match is found.</para>
		/// <para><paramref name="matches"/> contains an array with matches. At index 0 is the whole string that
		/// matches the <paramref name="pattern"/>, from index 1 are stored matches for parenthesized subpatterns.</para>
		/// <para>Flag <see cref="MatchFlags.OffsetCapture"/> can be specified and it means that the
		/// <paramref name="matches"/> array will not contain substrings, but another array where the substring
		/// is stored at index [0] and index [1] is its offset in <paramref name="data"/>. <paramref name="offset"/>
		/// specifies where the search should start. (Note that it is not the same as passing a substring of
		/// <paramref name="data"/>.)</para>
		/// </summary>
		/// <param name="pattern">Perl regular expression.</param>
		/// <param name="data">String or string of bytes to search.</param>
		/// <param name="matches">Array containing matched strings.</param>
		/// <param name="flags"><see cref="MatchFlags"/>.</param>
		/// <param name="offset">Offset to <paramref name="data"/> where the match should start.</param>
		/// <returns>0 if there is no match and 1 if the match was found.</returns>
		[ImplementsFunction("preg_match")]
		[return: CastToFalse]
		public static int Match(object pattern, object data, out PhpArray matches, MatchFlags flags, int offset)
		{
			return Match(pattern, data, out matches, flags, offset, false);
		}

		/// <summary>
		/// <para>Searches <paramref name="data"/> for all matches to the regular expression given in pattern and puts
		/// them in <paramref name="matches"/> array. The matches are sorted in "Pattern Order" i. e. at zero
		/// index is an array containing whole matches, at first index is an array containing number 1 subpatterns
		/// for all matches etc.</para>
		/// <para>Next match search starts just after the previous match.</para>
		/// </summary>
		/// <param name="pattern">Regular expression.</param>
		/// <param name="data">String or string of bytes to search.</param>
		/// <param name="matches">Output array containing matches found.</param>
		/// <returns>Number of whole matches.</returns>
		[ImplementsFunction("preg_match_all")]
		[return: CastToFalse]
		public static int MatchAll(object pattern, object data, out PhpArray matches)
		{
			return Match(pattern, data, out matches, MatchFlags.PatternOrder, 0, true);
		}

		/// <summary>
		/// <para>Searches <paramref name="data"/> for all matches to the regular expression given in pattern and puts
		/// them in <paramref name="matches"/> array. The matches are sorted in "Pattern Order" i. e. at zero
		/// index is an array containing whole matches, at first index is an array containing number 1 subpatterns
		/// for all matches etc.</para>
		/// <para>Next match search starts just after the previous match.</para>
		/// <para>If <see cref="MatchFlags.PatternOrder"/> flag is specified, <paramref name="matches"/> array
		/// contains an array of full pattern matches at index 0, an array of strings matched to
		/// first parenthesized substring at index 1 etc. If <see cref="MatchFlags.SetOrder"/> is set, at index 0 is the first
		/// set of matches (full match and substrings), at index 1 full set for second match etc.</para>
		/// <para>Flag <see cref="MatchFlags.OffsetCapture"/> indicates that instead the matched substring should
		/// be an array containing the substring at index 0 and position at original string at index 1.</para>
		/// </summary>
		/// <param name="pattern">Regular expression.</param>
		/// <param name="data">String or string of bytes to search.</param>
		/// <param name="matches">Output array containing matches found.</param>
		/// <param name="flags">Flags for specifying order of results in <paramref name="matches"/> array (Set Order,
		/// Pattern Order) and whether positions of matches should be added to results (Offset Capture).</param>
		/// <returns>Number of whole matches.</returns>
		[ImplementsFunction("preg_match_all")]
		[return: CastToFalse]
		public static int MatchAll(object pattern, object data, out PhpArray matches, MatchFlags flags)
		{
			return Match(pattern, data, out matches, flags, 0, true);
		}

		/// <summary>
		/// <para>Searches <paramref name="data"/> for all matches to the regular expression given in pattern and puts
		/// them in <paramref name="matches"/> array. The matches are sorted in "Pattern Order" i. e. at zero
		/// index is an array containing whole matches, at first index is an array containing number 1 subpatterns
		/// for all matches etc.</para>
		/// <para>Next match search starts just after the previous match.</para>
		/// <para>If <see cref="MatchFlags.PatternOrder"/> flag is specified, <paramref name="matches"/> array
		/// contains at index 0 an array of full pattern matches, at index 1 is an array of strings matched to
		/// first parenthesized substring etc. If <see cref="MatchFlags.SetOrder"/> is set, at index 0 is the first
		/// set of matches (full match and substrings), at index 1 full set for second match etc.</para>
		/// <para>Flag <see cref="MatchFlags.OffsetCapture"/> indicates that instead the matched substring should
		/// be an array containing the substring at index 0 and position at original string at index 1.</para>
		/// </summary>
		/// <param name="pattern">Regular expression.</param>
		/// <param name="data">String or string of bytes to search.</param>
		/// <param name="matches">Output array containing matches found.</param>
		/// <param name="flags">Flags for specifying order of results in <paramref name="matches"/> array (Set Order,
		/// Pattern Order) and whether positions of matches should be added to results (Offset Capture).</param>
		/// <param name="offset">Offset in <paramref name="data"/> where the search should begin. Note that it is
		/// not equal to passing an substring as this parameter because of ^ (start of the string or line) modifier.
		/// </param>
		/// <returns>Number of whole matches.</returns>
		[ImplementsFunction("preg_match_all")]
		[return: CastToFalse]
		public static int MatchAll(object pattern, object data, out PhpArray matches, MatchFlags flags, int offset)
		{
			return Match(pattern, data, out matches, flags, offset, true);
		}

		/// <summary>
		/// Private method implementing functions from match family.
		/// </summary>
		/// <param name="pattern">Perl regular expression match pattern.</param>
		/// <param name="data">String to search matches.</param>
		/// <param name="matches">An array containing matches found.</param>
		/// <param name="flags">Flags for searching.</param>
		/// <param name="offset">Offset to <paramref name="pattern"/> where the search should start.</param>
		/// <param name="matchAll"><B>True</B> if all matches should be found, <B>false</B> if only the first
		/// is enough.</param>
		/// <returns>Number of times the <paramref name="pattern"/> matches.</returns>
		private static int Match(object pattern, object data, out PhpArray matches, MatchFlags flags,
			int offset, bool matchAll)
		{
			// these two flags together do not make sense
			if ((flags & MatchFlags.PatternOrder) != 0 && (flags & MatchFlags.SetOrder) != 0)
			{
				PhpException.InvalidArgument("flags", LibResources.GetString("preg_match_pattern_set_order"));
				matches = null;
				return -1;
			}

			PerlRegExpConverter converter = ConvertPattern(pattern, null);
			if (converter == null)
			{
				matches = new PhpArray();
				return -1;
			}

			string converted = ConvertData(data, converter);
			Match m = converter.Regex.Match(converted, offset>converted.Length?converted.Length:offset);

			if ((converter.PerlOptions & PerlRegexOptions.Anchored) > 0 && m.Success && m.Index != offset)
			{
				matches = new PhpArray();
				return -1;
			}

			if (m.Success)
			{
				if (!matchAll || (flags & MatchFlags.PatternOrder) != 0)
                {
					matches = new PhpArray(m.Groups.Count);
                }
				else
					matches = new PhpArray();

				if (!matchAll)
				{
                    for (int i = 0; i <= GetLastSuccessfulGroup(m.Groups); i++)
                    {
                        AddGroupNameToResult(converter.Regex, matches, i, (ms,groupName) =>
                        {
                            ms[groupName] = NewArrayItem(m.Groups[i].Value, m.Groups[i].Index, (flags & MatchFlags.OffsetCapture) != 0);
                        });

                        matches[i] = NewArrayItem(m.Groups[i].Value, m.Groups[i].Index, (flags & MatchFlags.OffsetCapture) != 0);
                    }

					return 1;
				}

				// store all other matches in PhpArray matches
				if ((flags & MatchFlags.SetOrder) != 0) // cannot test PatternOrder, it is 0, SetOrder must be tested
					return FillMatchesArrayAllSetOrder(converter.Regex, m, ref matches, (flags & MatchFlags.OffsetCapture) != 0);
				else
					return FillMatchesArrayAllPatternOrder(converter.Regex, m, ref matches, (flags & MatchFlags.OffsetCapture) != 0);
			}

			// no match has been found
			if (matchAll && (flags & MatchFlags.SetOrder) == 0)
			{
				// in that case PHP returns an array filled with empty arrays according to parentheses count
				matches = new PhpArray(m.Groups.Count);
                for (int i = 0; i < converter.Regex.GetGroupNumbers().Length; i++)
                {
                    AddGroupNameToResult(converter.Regex, matches, i, (ms,groupName) =>
                    {
                        ms[groupName] = new PhpArray(0);
                    });

                    matches[i] = new PhpArray(0);
                }
			}
			else
			{
				matches = new PhpArray(0); // empty array
			}

			return 0;
		}

		#endregion

		#region preg_split

		/// <summary>
		/// Flags for split functions family.
		/// </summary>
		[Flags]
		public enum SplitFlags
		{
			None = 0,
			[ImplementsConstant("PREG_SPLIT_NO_EMPTY")]
			NoEmpty = 1,
			[ImplementsConstant("PREG_SPLIT_DELIM_CAPTURE")]
			DelimCapture = 2,
			[ImplementsConstant("PREG_SPLIT_OFFSET_CAPTURE")]
			OffsetCapture = 4
		}

		/// <summary>
		/// Splits <paramref name="data"/> along boundaries matched by <paramref name="pattern"/> and returns
		/// an array containing substrings.
		/// </summary>
		/// <param name="pattern">Regular expression to match to boundaries.</param>
		/// <param name="data">String string of bytes to split.</param>
		/// <returns>An array containing substrings.</returns>
		[ImplementsFunction("preg_split")]
		public static PhpArray Split(object pattern, object data)
		{
			return Split(pattern, data, -1, SplitFlags.None);
		}

		/// <summary>
		/// <para>Splits <paramref name="data"/> along boundaries matched by <paramref name="pattern"/> and returns
		/// an array containing substrings.</para>
		/// <para><paramref name="limit"/> specifies the maximum number of strings returned in the resulting
		/// array. If (limit-1) matches is found and there remain some characters to match whole remaining
		/// string is returned as the last element of the array.</para>
		/// </summary>
		/// <param name="pattern">Regular expression to match to boundaries.</param>
		/// <param name="data">String string of bytes to split.</param>
		/// <param name="limit">Max number of elements in the resulting array.</param>
		/// <returns>An array containing substrings.</returns>
		[ImplementsFunction("preg_split")]
		public static PhpArray Split(object pattern, object data, int limit)
		{
			return Split(pattern, data, limit, SplitFlags.None);
		}

		/// <summary>
		/// <para>Splits <paramref name="data"/> along boundaries matched by <paramref name="pattern"/> and returns
		/// an array containing substrings.</para>
		/// <para><paramref name="limit"/> specifies the maximum number of strings returned in the resulting
		/// array. If (limit-1) matches is found and there remain some characters to match whole remaining
		/// string is returned as the last element of the array.</para>
		/// <para>Some flags may be specified. <see cref="SplitFlags.NoEmpty"/> means no empty strings will be
		/// in the resulting array. <see cref="SplitFlags.DelimCapture"/> adds also substrings matching
		/// the delimiter and <see cref="SplitFlags.OffsetCapture"/> returns instead substrings the arrays
		/// containing appropriate substring at index 0 and the offset of this substring in original
		/// <paramref name="data"/> at index 1.</para>
		/// </summary>
		/// <param name="pattern">Regular expression to match to boundaries.</param>
		/// <param name="data">String or string of bytes to split.</param>
		/// <param name="limit">Max number of elements in the resulting array.</param>
		/// <param name="flags">Flags affecting the returned array.</param>
		/// <returns>An array containing substrings.</returns>
		[ImplementsFunction("preg_split")]
		public static PhpArray Split(object pattern, object data, int limit, SplitFlags flags)
		{
			if (limit == 0) // 0 does not make sense, php's behavior is as it is -1
				limit = -1;
			if (limit < -1) // for all other negative values it seems that is as limit == 1
				limit = 1;

			PerlRegExpConverter converter = ConvertPattern(pattern, null);
			if (converter == null) return null;

			string str = ConvertData(data, converter);
			Match m = converter.Regex.Match(str);

			bool offset_capture = (flags & SplitFlags.OffsetCapture) != 0;
			PhpArray result = new PhpArray();
			int last_index = 0;

			while (m.Success && (limit == -1 || --limit > 0) && last_index < str.Length)
			{
				// add part before match
				int length = m.Index - last_index;
				if (length > 0 || (flags & SplitFlags.NoEmpty) == 0)
					result.Add(NewArrayItem(str.Substring(last_index, length), last_index, offset_capture));

				if (m.Value.Length > 0)
				{
					if ((flags & SplitFlags.DelimCapture) != 0) // add all captures but not whole pattern match (start at 1)
					{
                        List<object> lastUnsucessfulGroups = null;  // value of groups that was not successful since last succesful one
						for (int i = 1; i < m.Groups.Count; i++)
						{
							Group g = m.Groups[i];
                            if (g.Length > 0 || (flags & SplitFlags.NoEmpty) == 0)
                            {
                                // the value to be added into the result:
                                object value = NewArrayItem(g.Value, g.Index, offset_capture);

                                if (g.Success)
                                {
                                    // group {i} was matched:
                                    // if there was some unsuccesfull matches before, add them now:
                                    if (lastUnsucessfulGroups != null && lastUnsucessfulGroups.Count > 0)
                                    {
                                        foreach (var x in lastUnsucessfulGroups)
                                            result.Add(x);
                                        lastUnsucessfulGroups.Clear();
                                    }
                                    // add the matched group:
                                    result.Add(value);
                                }
                                else
                                {
                                    // The match was unsuccesful, remember all the unsuccesful matches
                                    // and add them only if some succesful match will follow.
                                    // In PHP, unsuccessfully matched groups are trimmed by the end
                                    // (regexp processing stops when other groups cannot be matched):
                                    if (lastUnsucessfulGroups == null) lastUnsucessfulGroups = new List<object>();
                                    lastUnsucessfulGroups.Add(value);
                                }
                            }
						}
					}

					last_index = m.Index + m.Length;
				}
				else // regular expression match an empty string => add one character
				{
					// always not empty
					result.Add(NewArrayItem(str.Substring(last_index, 1), last_index, offset_capture));
					last_index++;
				}

				m = m.NextMatch();
			}

			// add remaining string (might be empty)
			if (last_index < str.Length || (flags & SplitFlags.NoEmpty) == 0)
				result.Add(NewArrayItem(str.Substring(last_index), last_index, offset_capture));

			return result;
		}

		#endregion

		#region preg_replace, preg_replace_callback

		/// <summary>
		/// <para>Searches <paramref name="data"/> for matches to <paramref name="pattern"/> and replaces them
		/// with <paramref name="replacement"/>. <paramref name="replacement"/> may contain backreferences
		/// of the form of <I>\\n</I> or <I>$n</I> (second one preferred).</para>
		/// <para>Every parameter may be an unidimensional array of strings. If <paramref name="data"/> is
		/// an array, replacement is done on every element and return value is an array as well. If
		/// <paramref name="pattern"/> and <paramref name="replacement"/> are arrays, the replacements are processed
		/// in the order the keys appear in the array. If only <paramref name="pattern"/> is an array, the
		/// replacement string is used for every key in the <paramref name="pattern"/>.</para>
		/// </summary>
        /// <param name="context">Current <see cref="ScriptContext"/>. Passed by Phalanger runtime, cannot be null.</param>
        /// <param name="self">Instance of object that called the replace method (replace pattern may contain $this).</param>
        /// <param name="definedVariables"></param>
		/// <param name="pattern">Regular expression to match.</param>
		/// <param name="replacement">Replacement string.</param>
		/// <param name="data">String to search for replacements.</param>
		/// <returns>String or array containing strings with replacement performed.</returns>
		[ImplementsFunction("preg_replace", FunctionImplOptions.CaptureEvalInfo | FunctionImplOptions.NeedsVariables | FunctionImplOptions.NeedsThisReference)]
		public static object Replace(ScriptContext/*!*/context, DObject self, Dictionary<string, object> definedVariables, 
			object pattern, object replacement, object data)
		{
			int count = Int32.MinValue; // disables counting
			return Replace(context, self, definedVariables, pattern, replacement, null, data, -1, ref count);
		}

		/// <summary>
		/// <para>Searches <paramref name="data"/> for matches to <paramref name="pattern"/> and replaces them
		/// with <paramref name="replacement"/>. <paramref name="replacement"/> may contain backreferences
		/// of the form of <I>\\n</I> or <I>$n</I> (second one preferred).</para>
		/// <para>Every parameter may be an unidimensional array of strings. If <paramref name="data"/> is
		/// an array, replacement is done on every element and return value is an array as well. If
		/// <paramref name="pattern"/> and <paramref name="replacement"/> are arrays, the replacements are processed
		/// in the order the keys appear in the array. If only <paramref name="pattern"/> is an array, the
		/// replacement string is used for every key in the <paramref name="pattern"/>.</para>
		/// </summary>
        /// <param name="context">Current <see cref="ScriptContext"/>. Passed by Phalanger runtime, cannot be null.</param>
        /// <param name="self">Instance of object that called the replace method (replace pattern may contain $this)</param>
        /// <param name="definedVariables"></param>
		/// <param name="pattern">Regular expression to match.</param>
		/// <param name="replacement">Replacement string.</param>
		/// <param name="data">String to search for replacements.</param>
		/// <param name="limit">Maximum number of matches replaced. (-1 for no limit)</param>
		/// <returns>String or array containing strings with replacement performed.</returns>
		[ImplementsFunction("preg_replace", FunctionImplOptions.CaptureEvalInfo | FunctionImplOptions.NeedsVariables | FunctionImplOptions.NeedsThisReference)]
        public static object Replace(ScriptContext/*!*/context, DObject self, Dictionary<string, object> definedVariables, 
			object pattern, object replacement, object data, int limit)
		{
			int count = Int32.MinValue; // disables counting
			return Replace(context, self, definedVariables, pattern, replacement, null, data, limit, ref count);
		}

		/// <summary>
		/// <para>Searches <paramref name="data"/> for matches to <paramref name="pattern"/> and replaces them
		/// with <paramref name="replacement"/>. <paramref name="replacement"/> may contain backreferences
		/// of the form of <I>\\n</I> or <I>$n</I> (second one preferred).</para>
		/// <para>Every parameter may be an unidimensional array of strings. If <paramref name="data"/> is
		/// an array, replacement is done on every element and return value is an array as well. If
		/// <paramref name="pattern"/> and <paramref name="replacement"/> are arrays, the replacements are processed
		/// in the order the keys appear in the array. If only <paramref name="pattern"/> is an array, the
		/// replacement string is used for every key in the <paramref name="pattern"/>.</para>
		/// </summary>
        /// <param name="context">Current <see cref="ScriptContext"/>. Passed by Phalanger runtime, cannot be null.</param>
        /// <param name="self">Instance of object that called the replace method (replace pattern may contain $this)</param>
        /// <param name="definedVariables"></param>
		/// <param name="pattern">Regular expression to match.</param>
		/// <param name="replacement">Replacement string.</param>
		/// <param name="data">String to search for replacements.</param>
		/// <param name="limit">Maximum number of matches replaced. (-1 for no limit)</param>
		/// <param name="count">Number of replacements.</param>
		/// <returns>String or array containing strings with replacement performed.</returns>
		[ImplementsFunction("preg_replace", FunctionImplOptions.CaptureEvalInfo | FunctionImplOptions.NeedsVariables | FunctionImplOptions.NeedsThisReference)]
        public static object Replace(ScriptContext/*!*/context, DObject self, Dictionary<string, object> definedVariables, 
			object pattern, object replacement, object data, int limit, out int count)
		{
			count = 0;
			return Replace(context, self, definedVariables, pattern, replacement, null, data, limit, ref count);
		}

		/// <summary>
		/// <para>Searches <paramref name="data"/> for matches to <paramref name="pattern"/> and the array of matched
		/// strings (full pattern match + parenthesized substrings) is passed to <paramref name="callback"/> which
		/// returns replacement string.</para>
		/// <para><paramref name="pattern"/> and <paramref name="data"/> parameters may be also unidimensional
		/// arrays of strings. For the explanation <see cref="Replace"/>.</para>
		/// </summary>
        /// <param name="context">Current <see cref="ScriptContext"/>. Passed by Phalanger runtime, cannot be null.</param>
        /// <param name="pattern">Regular expression to match.</param>
		/// <param name="callback">Function called to find out the replacement string.</param>
		/// <param name="data">String to search for replacements.</param>
		/// <returns>String or array containing strings with replacement performed.</returns>
		[ImplementsFunction("preg_replace_callback")]
        public static object Replace(ScriptContext/*!*/context, object pattern, PhpCallback callback, object data)
		{
			int count = Int32.MinValue; // disables counting;
			return Replace(context, null, null, pattern, null, callback, data, -1, ref count);
		}

		/// <summary>
		/// <para>Searches <paramref name="data"/> for matches to <paramref name="pattern"/> and the array of matched
		/// strings (full pattern match + parenthesized substrings) is passed to <paramref name="callback"/> which
		/// returns replacement string.</para>
		/// <para><paramref name="pattern"/> and <paramref name="data"/> parameters may be also unidimensional
		/// arrays of strings. For the explanation <see cref="Replace"/>.</para>
		/// </summary>
        /// <param name="context">Current <see cref="ScriptContext"/>. Passed by Phalanger runtime, cannot be null.</param>
        /// <param name="pattern">Regular expression to match.</param>
		/// <param name="callback">Function called to find out the replacement string.</param>
		/// <param name="data">String to search for replacements.</param>
		/// <param name="limit">Maximum number of matches replaced. (-1 for no limit)</param>
		/// <returns>String or array containing strings with replacement performed.</returns>
		[ImplementsFunction("preg_replace_callback")]
        public static object Replace(ScriptContext/*!*/context, object pattern, PhpCallback callback, object data, int limit)
		{
			int count = Int32.MinValue; // disables counting
			return Replace(context, null, null, pattern, null, callback, data, limit, ref count);
		}

		/// <summary>
		/// <para>Searches <paramref name="data"/> for matches to <paramref name="pattern"/> and the array of matched
		/// strings (full pattern match + parenthesized substrings) is passed to <paramref name="callback"/> which
		/// returns replacement string.</para>
		/// <para><paramref name="pattern"/> and <paramref name="data"/> parameters may be also unidimensional
		/// arrays of strings. For the explanation <see cref="Replace"/>.</para>
		/// </summary>
        /// <param name="context">Current <see cref="ScriptContext"/>. Passed by Phalanger runtime, cannot be null.</param>
        /// <param name="pattern">Regular expression to match.</param>
		/// <param name="callback">Function called to find out the replacement string.</param>
		/// <param name="data">String to search for replacements.</param>
		/// <param name="limit">Maximum number of matches replaced. (-1 for no limit)</param>
		/// <param name="count">Number of replacements.</param>
		/// <returns>String or array containing strings with replacement performed.</returns>
		[ImplementsFunction("preg_replace_callback")]
        public static object Replace(ScriptContext/*!*/context, object pattern, PhpCallback callback, object data, int limit, out int count)
		{
			count = 0;
			return Replace(context, null, null, pattern, null, callback, data, limit, ref count);
		}

		/// <summary>
		/// Private mehtod implementing all replace methods. Just one of <paramref name="replacement"/> or <paramref name="callback" /> should be used.
		/// </summary>
        /// <param name="context">Current <see cref="ScriptContext"/>. Must not be null.</param>
        /// <param name="self">Instance of object that called the replace method (replace pattern may contain $this)</param>
        /// <param name="definedVariables"></param>
        /// <param name="pattern"></param>
        /// <param name="replacement"></param>
        /// <param name="callback"></param>
        /// <param name="data"></param>
        /// <param name="limit"></param>
        /// <param name="count"></param>
		/// <returns>String or an array.</returns>
		private static object Replace(ScriptContext/*!*/context, DObject self, Dictionary<string, object> definedVariables, object pattern, object replacement, PhpCallback callback,
			object data, int limit, ref int count)
		{
			// if we have no replacement and no callback, matches are deleted (replaced by an empty string)
			if (replacement == null && callback == null)
				replacement = String.Empty;

			// exactly one of replacement or callback is valid now
			Debug.Assert(replacement != null ^ callback != null);

			// get eval info if it has been captured - is needed even if we do not need them later
            SourceCodeDescriptor descriptor = context.GetCapturedSourceCodeDescriptor();

			// PHP's behaviour for undocumented limit range
			if (limit < -1)
				limit = 0;
            
            PhpArray replacement_array = replacement as PhpArray;

			string replacement_string = null;
			if (replacement_array == null && replacement != null)
				replacement_string = Core.Convert.ObjectToString(replacement);

			// we should return new array, if there is an array passed as subject, it should remain unchanged:
			object data_copy = PhpVariable.DeepCopy(data);

            PhpArray pattern_array = pattern as PhpArray;
            if (pattern_array == null)
            {
                // string pattern
                // string replacement

                if (replacement_array != null)
                {
                    // string pattern and array replacement not allowed:
                    PhpException.InvalidArgument("replacement", LibResources.GetString("replacement_array_pattern_not"));
                    return null;
                }

                // pattern should be treated as string and therefore replacement too:
                return SimpleReplace(self, definedVariables, pattern, replacement_string, callback, data_copy, limit, descriptor, ref count);
            }
            else if (replacement_array == null)
            {
                // array  pattern
                // string replacement

                using (var pattern_enumerator = pattern_array.GetFastEnumerator())
                    while (pattern_enumerator.MoveNext())
                    {
                        data_copy = SimpleReplace(self, definedVariables, pattern_enumerator.CurrentValue, replacement_string,
                                    callback, data_copy, limit, descriptor, ref count);
                    }
            }
            else //if (replacement_array != null)
            {
                // array pattern
                // array replacement

                var replacement_enumerator = replacement_array.GetFastEnumerator();
                bool replacement_valid = true;

                using (var pattern_enumerator = pattern_array.GetFastEnumerator())
                    while (pattern_enumerator.MoveNext())
                    {
                        // replacements are in array, move to next item and take it if possible, in other case take empty string:
                        if (replacement_valid && replacement_enumerator.MoveNext())
                        {
                            replacement_string = Core.Convert.ObjectToString(replacement_enumerator.CurrentValue);
                        }
                        else
                        {
                            replacement_string = string.Empty;
                            replacement_valid = false;  // end of replacement_enumerator, do not call MoveNext again!
                        }

                        data_copy = SimpleReplace(self, definedVariables, pattern_enumerator.CurrentValue, replacement_string,
                                    callback, data_copy, limit, descriptor, ref count);
                    }
            }
            
			// return resulting array or string assigned to data
			return data_copy;
		}

		/// <summary>
		/// Takes a regular expression <paramref name="pattern"/> and one of <paramref name="replacement"/> or 
		/// <paramref name="callback"/>. Performs replacing on <paramref name="data"/>, which can be
		/// <see cref="PhpArray"/>, in other cases it is converted to string.
		/// If <paramref name="data"/> is <see cref="PhpArray"/>, every value is converted to string and
		/// replacement is performed in place in this array.
		/// Either <paramref name="replacement"/> or <paramref name="callback"/> should be null.
		/// </summary>
		/// <param name="self">Instance of object that called the replace method (replace pattern may contain $this)</param>
		/// <param name="definedVariables">Array with local variables - can be used by replace pattern</param>
		/// <param name="pattern">Regular expression to search.</param>
		/// <param name="replacement">Regular replacement expression. Should be null if callback is specified.</param>
		/// <param name="callback">Callback function that should be called to make replacements. Should be null
		/// if replacement is specified.</param>
		/// <param name="data">Array or string where pattern is searched.</param>
		/// <param name="limit">Max count of replacements for each item in subject.</param>
		/// <param name="descriptor"><see cref="SourceCodeDescriptor"/> for possible lambda function creation.</param>
		/// <param name="count">Cumulated number of replacements.</param>
		/// <returns></returns>
		private static object SimpleReplace(DObject self, Dictionary<string, object> definedVariables, object pattern, 
			string replacement, PhpCallback callback, object data, int limit, SourceCodeDescriptor descriptor, ref int count)
		{
			Debug.Assert(limit >= -1);

			// exactly one of replacement or callback is valid:
			Debug.Assert(replacement != null ^ callback != null);

			PerlRegExpConverter converter = ConvertPattern(pattern, replacement);
			if (converter == null) return null;

			// get types of data we need:
			PhpArray data_array = data as PhpArray;
			string data_string = (data_array == null) ? ConvertData(data, converter) : null;

			// data comprising of a single string:
            if (data_array == null)
            {
                return ReplaceInternal(self, definedVariables, converter, callback, data_string, limit, descriptor, ref count);
            }
            else
            {
                // data is array, process each item:
                var enumerator = data_array.GetFastEnumerator();
                while (enumerator.MoveNext())
                {
                    enumerator.CurrentValue = ReplaceInternal(self, definedVariables, converter, callback,
                        ConvertData(enumerator.CurrentValue, converter), limit, descriptor, ref count);
                }
                enumerator.Dispose();

                // return array with items replaced:
                return data;
            }
		}

		/// <summary>
		/// Replaces <paramref name="limit"/> occurences of substrings.
		/// </summary>
		/// <param name="converter">
		/// Converter used for replacement if <paramref name="callback"/> is <B>null</B>.
		/// </param>
		/// <param name="self">Instance of object that called the replace method (replace pattern may contain $this)</param>
		/// <param name="definedVariables">Array with local variables - can be used by replace pattern</param>
		/// <param name="callback">Callback to call for replacement strings.</param>
		/// <param name="str">String to search for matches.</param>
		/// <param name="limit">Max number of replacements performed.</param>
		/// <param name="sourceCodeDesc"><see cref="SourceCodeDescriptor"/> for possible lambda function creation.</param>
		/// <param name="count">Cumulated number of replacements.</param>
		/// <returns></returns>
		private static string ReplaceInternal(DObject self, Dictionary<string, object> definedVariables, PerlRegExpConverter converter, PhpCallback callback,
			string str, int limit, SourceCodeDescriptor sourceCodeDesc, ref int count)
		{
			Debug.Assert(limit >= -1);

			if (callback == null)
			{
				// replace without executing code or counting the number of replacements:
				if ((converter.PerlOptions & PerlRegexOptions.Evaluate) == 0 && count < 0)
					return converter.Regex.Replace(str, converter.DotNetReplaceExpression, limit);

				Evaluator evaluator = new Evaluator(converter.Regex, converter.DotNetReplaceExpression, sourceCodeDesc, self, definedVariables);
				MatchEvaluator match_evaluator;

				if ((converter.PerlOptions & PerlRegexOptions.Evaluate) != 0)
					match_evaluator = new MatchEvaluator(evaluator.ReplaceCodeExecute);
				else
					match_evaluator = new MatchEvaluator(evaluator.ReplaceCount);

				string result = converter.Regex.Replace(str, match_evaluator, limit);
				count += evaluator.Count;
				return result;
			}
			else
			{
                StringBuilder result = new StringBuilder((str != null) ? str.Length : 0);
				int last_index = 0;

				Match m = converter.Regex.Match(str);
				while (m.Success && (limit == -1 || limit-- > 0))
				{
					// append everything from input string to current match
					result.Append(str, last_index, m.Index - last_index);

					// move index after current match
					last_index = m.Index + m.Length;

					PhpArray arr = new PhpArray(m.Groups.Count, 0);
					for (int i = 0; i < m.Groups.Count; i++)
						arr[i] = m.Groups[i].Value;

					// append user callback function result
					string replacement = Core.Convert.ObjectToString(callback.Invoke(arr));
					result.Append(replacement);

					m = m.NextMatch();

					count++;
				}

				// remaining string
				result.Append(str, last_index, str.Length - last_index);
				return result.ToString();
			}
		}

		/// <summary>
		/// Class implementing <see cref="MatchEvaluator"/> delegate evaluating php code if 'e' modifier
		/// in preg_replace is specified.
		/// </summary>
		private sealed class Evaluator
		{
			private Regex reg;
			private string replacement;
			private SourceCodeDescriptor sourceCodeDesc;
			private Dictionary<string, object> definedVariables;
			private DObject self;

			public int Count { get { return count; } }
			private int count;

			public Evaluator(Regex reg, string replacement, SourceCodeDescriptor sourceCodeDesc, DObject self, Dictionary<string, object> definedVariables)
			{
				this.reg = reg;
				this.definedVariables = definedVariables;
				this.replacement = replacement;
				this.sourceCodeDesc = sourceCodeDesc;
				this.count = 0;
				this.self = self;
			}

			public string ReplaceCodeExecute(Match m)
			{
				count++;

				if (m.Value.Trim().Length == 0)
					return String.Empty; // nothing to do

				ScriptContext context = ScriptContext.CurrentContext;

				// generate code that will be executed
				string code = String.Concat("return ", Substitute(replacement, m.Groups), ";");

				// Execute..
				return Core.Convert.ObjectToString(DynamicCode.Eval(code, true, context, definedVariables, self, null, 
					context.EvalRelativeSourcePath, context.EvalLine, context.EvalColumn, context.EvalId, null));
			}

			public string ReplaceCount(Match m)
			{
				count++;
				return replacement;
			}

			/// <summary>
            /// Expects replacement string produced by <see cref="PerlRegExpReplacement.ConvertReplacement"/>, 
			/// i.e. only ${n} refer to valid groups.
			/// </summary>
			private string Substitute(string replacement, GroupCollection groups)
			{
				StringBuilder result = new StringBuilder(replacement.Length);

				int i = 0;
				while (i < replacement.Length)
				{
					if (IsParenthesizedGroupReference(replacement, i))
					{
						// ${
						i += 2;

						// [0-9]{1,2}
						int group_no = replacement[i++] - '0';
						if (replacement[i] != '}')
						{
							group_no = group_no * 10 + (replacement[i] - '0');
							i++;
						}

						// }
						Debug.Assert(replacement[i] == '}');
						i++;

						Debug.Assert(group_no < groups.Count);

						// append slashed group value:
						result.Append(StringUtils.AddCSlashes(groups[group_no].Value, true, true, false));
					}
					else if (replacement[i] == '$')
					{
						Debug.Assert(i + 1 < replacement.Length && replacement[i + 1] == '$');
						result.Append('$');
						i += 2;
					}
					else
					{
						result.Append(replacement[i++]);
					}
				}

				return result.ToString();
			}
		}

		#endregion

		#region Helper methods


        private static void AddGroupNameToResult(Regex regex,PhpArray matches, int i, Action<PhpArray, string> action)
        {
            // named group?
            var groupName = regex.GroupNameFromNumber(i);
            //remove sign from the beginning of the groupName
            groupName = groupName[0] == PerlRegExpConverter.GroupPrefix ? groupName.Remove(0, 1) : groupName;

            if (!String.IsNullOrEmpty(groupName) && groupName != i.ToString())
            {
                action(matches, groupName);
            }
        }

		private static PerlRegExpConverter ConvertPattern(object pattern, string replacement)
		{
            var converter = PerlRegExpCache.Get(pattern, replacement, true);

            // converter can contain a warning message,
            // it means it is invalid and we cannot use it:
            if (converter.ArgumentException != null)
            {
                // Exception message might contain substrings like "{2}" so it cannot be passed to any
                // method that formats the string and replaces these numbers with parameters.
                PhpException.Throw(PhpError.Warning, LibResources.GetString("invalid_argument", "pattern") + ": " + converter.ArgumentException);
                return null;
            }

            //
            return converter;
		}

        private static string ConvertData(object data, PerlRegExpConverter/*!*/ converter)
		{
            if (data == null)
            {
                return string.Empty;
            }
            else if (data.GetType() == typeof(PhpBytes))
            {
                return converter.ConvertBytes(((PhpBytes)data).ReadonlyData);
            }
            else
			{
				string str = Core.Convert.ObjectToString(data);
				return converter.ConvertString(str, 0, str.Length);
			}
		}
        
		/// <summary>
		/// Used for handling Offset Capture flags. Returns just <paramref name="item"/> if
		/// <paramref name="offsetCapture"/> is <B>false</B> or an <see cref="PhpArray"/> containing
		/// <paramref name="item"/> at index 0 and <paramref name="index"/> at index 1.
		/// </summary>
		/// <param name="item">Item to add to return value.</param>
		/// <param name="index">Index to specify in return value if <paramref name="offsetCapture"/> is
		/// <B>true</B>.</param>
		/// <param name="offsetCapture">Whether or not to make <see cref="PhpArray"/> with item and index.</param>
		/// <returns></returns>
		private static object NewArrayItem(object item, int index, bool offsetCapture)
		{
			if (!offsetCapture)
				return item;

			PhpArray arr = new PhpArray(2, 0);
			arr[0] = item;
			arr[1] = index;
			return arr;
		}

		/// <summary>
		/// Goes through <paramref name="m"/> matches and fill <paramref name="matches"/> array with results
		/// according to Pattern Order.
		/// </summary>
		/// <param name="r"><see cref="Regex"/> that produced the match</param>
		/// <param name="m"><see cref="Match"/> to iterate through all matches by NextMatch() call.</param>
		/// <param name="matches">Array for storing results.</param>
		/// <param name="addOffsets">Whether or not add arrays with offsets instead of strings.</param>
		/// <returns>Number of full pattern matches.</returns>
		private static int FillMatchesArrayAllPatternOrder(Regex r, Match m, ref PhpArray matches, bool addOffsets)
		{
			// second index, increases at each match in pattern order
			int j = 0;
			while (m.Success)
			{
				// add all groups
				for (int i = 0; i < m.Groups.Count; i++)
				{
					object arr = NewArrayItem(m.Groups[i].Value, m.Groups[i].Index, addOffsets);

                    AddGroupNameToResult(r, matches, i, (ms, groupName) =>
                    {
                        if (j == 0) ms[groupName] = new PhpArray();
                        ((PhpArray)ms[groupName])[j] = arr;
                    });

					if (j == 0) matches[i] = new PhpArray();
					((PhpArray)matches[i])[j] = arr;
				}

				j++;
				m = m.NextMatch();
			}

			return j;
		}

		/// <summary>
		/// Goes through <paramref name="m"/> matches and fill <paramref name="matches"/> array with results
		/// according to Set Order.
		/// </summary>
		/// <param name="r"><see cref="Regex"/> that produced the match</param>
		/// <param name="m"><see cref="Match"/> to iterate through all matches by NextMatch() call.</param>
		/// <param name="matches">Array for storing results.</param>
		/// <param name="addOffsets">Whether or not add arrays with offsets instead of strings.</param>
		/// <returns>Number of full pattern matches.</returns>
		private static int FillMatchesArrayAllSetOrder(Regex r, Match m, ref PhpArray matches, bool addOffsets)
		{
			// first index, increases at each match in set order
			int i = 0;

			while (m.Success)
			{
				PhpArray pa = new PhpArray(m.Groups.Count, 0);

				// add all groups
				for (int j = 0; j < m.Groups.Count; j++)
				{
					object arr = NewArrayItem(m.Groups[j].Value, m.Groups[j].Index, addOffsets);
					
                    AddGroupNameToResult(r, pa, j, (p, groupName) =>
                    {
                        p[groupName] = arr;
                    });


					pa[j] = arr;
				}

				matches[i] = pa;
				i++;
				m = m.NextMatch();
			}

			return i;
		}

		private static int GetLastSuccessfulGroup(GroupCollection/*!*/ groups)
		{
			Debug.Assert(groups != null);

			for (int i = groups.Count - 1; i >= 0; i--)
			{
				if (groups[i].Success)
					return i;
			}

			return -1;
		}

		internal static bool IsDigitGroupReference(string replacement, int i)
		{
			return (replacement[i] == '$' || replacement[i] == '\\') &&
			  (i + 1 < replacement.Length && Char.IsDigit(replacement, i + 1));
		}

		internal static bool IsParenthesizedGroupReference(string replacement, int i)
		{
			return replacement[i] == '$' && i + 3 < replacement.Length && replacement[i + 1] == '{' &&
			  Char.IsDigit(replacement, i + 2) &&
				(
					replacement[i + 3] == '}' ||
					i + 4 < replacement.Length && replacement[i + 4] == '}' && Char.IsDigit(replacement, i + 3)
			  );
		}

		#endregion

		#region Unit Testing
#if DEBUG
		
		[Test]
		static void TestUnicodeMatch()
		{
			int m;

			m = Match
			(
			  new PhpBytes(Encoding.UTF8.GetBytes("/[ø]/u")),
			  new PhpBytes(Encoding.UTF8.GetBytes("12šèø45"))
			);
			Debug.Assert(m == 1);

			Encoding enc = Configuration.Application.Globalization.PageEncoding;

			m = Match
			(
			  new PhpBytes(enc.GetBytes("/[ø]/")),
			  new PhpBytes("12šèø45")
			);
			Debug.Assert(m == 1);

			// binary cache test:
			m = Match
			(
			  new PhpBytes(enc.GetBytes("/[ø]/")),
			  new PhpBytes("12šèø45")
			);
			Debug.Assert(m == 1);

			int count;
			object r = Replace
			(
            ScriptContext.CurrentContext,
				null,
				null,
			  new PhpBytes(Encoding.UTF8.GetBytes("/[øš]+/u")),
			  "|žýø|",
			  new PhpBytes(Encoding.UTF8.GetBytes("Hešováøøøøíèkoøš hxx")),
			  1000,
			  out count
			);

			Debug.Assert(r as string == "He|žýø|ová|žýø|íèko|žýø| hxx");
			Debug.Assert(count == 3);
		}

#endif
		#endregion
	}

    #region PerlRegExpReplacement

    internal static class PerlRegExpReplacement
    {
        /// <summary>
        /// Get the converted replacement from the cache or perform conversion and cache.
        /// </summary>
        /// <param name="regex"></param>
        /// <param name="replacement"></param>
        /// <returns></returns>
        internal static string ConvertReplacement(Regex/*!*/regex, string/*!*/replacement)
        {
            int[] group_numbers = regex.GetGroupNumbers();
            int max_number = (group_numbers.Length > 0) ? group_numbers[group_numbers.Length - 1] : 0;

            return ConvertReplacement(max_number, replacement);
        }

        /// <summary>
        /// Converts substitutions of the form \\xx to $xx (perl to .NET format).
        /// </summary>
        /// <param name="max_number">Maximum group number for the current regullar expression.
        /// <code>
        ///     int[] group_numbers = regex.GetGroupNumbers();
        ///     int max_number = (group_numbers.Length > 0) ? group_numbers[group_numbers.Length - 1] : 0;
        /// </code>
        /// </param>
        /// <param name="replacement">String possibly containing \\xx substitutions.</param>
        /// <returns>String with converted $xx substitution format.</returns>
        private static string ConvertReplacement(int max_number, string replacement)
        {
            int length = replacement.Length;
            StringBuilder result = new StringBuilder(length);

            //int[] group_numbers = regex.GetGroupNumbers();
            //int max_number = (group_numbers.Length > 0) ? group_numbers[group_numbers.Length - 1] : 0;

            int i = 0;
            while (i < length)
            {
                if (PerlRegExp.IsDigitGroupReference(replacement, i) ||
                  PerlRegExp.IsParenthesizedGroupReference(replacement, i))
                {
                    int add = 0;
                    i++;

                    if (replacement[i] == '{') { i++; add = 1; }

                    // parse number
                    int number = replacement[i++] - '0';
                    if (i < length && Char.IsDigit(replacement, i))
                    {
                        number = number * 10 + (replacement[i++] - '0');
                    }

                    // insert only existing group references (others replaced with empty string):
                    if (number <= max_number)
                    {
                        result.Append('$');
                        result.Append('{');
                        result.Append(number.ToString());
                        result.Append('}');
                    }

                    i += add;
                }
                else if (replacement[i] == '$')
                {
                    // there is $ and it is not a substitution - duplicate it:
                    result.Append("$$");
                    i++;
                }
                else if (replacement[i] == '\\' && i + 1 < length)
                {
                    if (replacement[i + 1] == '\\')
                    {
                        // two backslashes, replace with one:
                        result.Append('\\');
                        i += 2;
                    }
                    else if (replacement[i + 1] == '$')
                    {
                        // "/$" -> '$$' because /$ doesn't escape $ in .NET
                        result.Append("$$");
                        i += 2;
                    }
                    else
                    {
                        // backslash + some character, skip two characters
                        result.Append(replacement, i, 2);
                        i += 2;
                    }
                }
                else
                {
                    // no substitution, no backslash (or backslash at the end of string)
                    result.Append(replacement, i++, 1);
                }
            }

            return result.ToString();
        }
    }

    #endregion

    #region PerlRegExpCache

    internal static class PerlRegExpCache
    {
        private const uint BucketsLength = 64;
        private static readonly PerlRegExpConverter[]/*!*/buckets = new PerlRegExpConverter[BucketsLength];
        private static readonly object[] locks = new object[8];
        static PerlRegExpCache()
        {
            var locks = PerlRegExpCache.locks;
            for (int i = 0; i < locks.Length; i++)
                locks[i] = new object();

            Debug.Assert(BucketsLength == 64); // must be 2^x
            RequestContext.RequestEnd += CleanupBuckets;
        }

        private static int generation = 0;

        public static PerlRegExpConverter Get(object pattern, string replacement, bool add)
        {
            uint hash = unchecked(
                ((pattern != null)
                    ? (uint)pattern.GetHashCode()   // little slow, some virtual method call
                    : 0)
                & (BucketsLength - 1));

            for (var item = buckets[hash]; item != null; item = item.nextcache)
            {
                if (item.CacheEquals(item, pattern, replacement))
                {
                    item.Cachehit();
                    item.generation = PerlRegExpCache.generation;   // move item to the current generation
                    return item;
                }
            }

            return add ? EnsureGet(pattern, replacement, hash) : null;
        }

        private static PerlRegExpConverter/*!*/EnsureGet(object pattern, string replacement, uint hash)
        {
            PerlRegExpConverter item;

            lock (locks[hash % locks.Length])
            {
                // double checked lock
                if ((item = Get(pattern, replacement, false)) == null)
                {
                    // avoid growing of the table in non-web applications (console etc.)
                    CleanupBuckets();

                    // new item
                    item = new PerlRegExpConverter(pattern, replacement, Configuration.Application.Globalization.PageEncoding)
                    {
                        nextcache = PerlRegExpCache.buckets[hash],
                        generation = PerlRegExpCache.generation
                    };
                    buckets[hash] = item;   // enlist the item
                }
            }

            return item;
        }

        private static int requestsCounter = 0;
        private static uint cleanupBucket = 0;
        private static void CleanupBuckets()
        {
            var requestsCounter = PerlRegExpCache.requestsCounter;
            if (requestsCounter < 32)
            {
                PerlRegExpCache.requestsCounter = requestsCounter + 1;
            }
            else if (requestsCounter < 64)
            {
                if (Interlocked.Increment(ref PerlRegExpCache.requestsCounter) == 64)
                {
                    // do some cleanup
                    var generation = PerlRegExpCache.generation;
                    var hash = PerlRegExpCache.cleanupBucket;
                    PerlRegExpCache.cleanupBucket = (uint)(hash + 1) & (BucketsLength - 1);

                    //
                    PerlRegExpConverter prev = null;
                    for (var p = buckets[hash]; p != null; p = p.nextcache)
                    {
                        if (p.generation != generation && unchecked(p.generation + 1) != generation)
                        {
                            if (prev != null) prev.nextcache = p.nextcache;
                            else buckets[hash] = p.nextcache;
                        }
                        else
                            prev = p;
                    }

                    // 
                    if ((hash & 1) == 1)    // every 2nd
                        PerlRegExpCache.generation = unchecked(generation + 1);
                }
            }
            else
                PerlRegExpCache.requestsCounter = 0;
        }
    }

    #endregion

    #region PerlRegExpConverter

    /// <summary>
	/// Used for converting PHP Perl like regular expressions to .NET regular expressions.
	/// </summary>
	internal sealed class PerlRegExpConverter
    {
        #region Static & Constants

        /// <summary>
        /// All named groups from Perl regexp are renamed to start with this character. 
        /// In order to enable group names starting with number
        /// </summary>
        internal const char GroupPrefix = 'a';

        /// <summary>
		/// Regular expression used for matching quantifiers, they are changed ungreedy to greedy and vice versa if
		/// needed.
		/// </summary>
		private static Regex quantifiers
		{
			get
			{
				if (_quantifiers == null)
					_quantifiers = new Regex(@"\G(?:\?|\*|\+|\{[0-9]+,[0-9]*\})");
				return _quantifiers;
			}
		}
		private static Regex _quantifiers;

		/// <summary>
		/// Regular expression for POSIX regular expression classes matching.
		/// </summary>
		private static Regex posixCharClasses
		{
			get
			{
				if (_posixCharClasses == null)
					_posixCharClasses = new Regex("^\\[:(^)?(alpha|alnum|ascii|cntrl|digit|graph|lower|print|punct|space|upper|word|xdigit):]", RegexOptions.Singleline | RegexOptions.Compiled);
				return _posixCharClasses;
			}
		}
		private static Regex _posixCharClasses = null;

        #endregion

        #region Fields & Properties

        /// <summary>
		/// Returns <see cref="Regex"/> class that can be used for matching.
		/// </summary>
		public Regex/*!*/ Regex { get { return regex; } }
		private Regex/*!*/ regex;

		/// <summary>
		/// Returns .NET replacement string.
		/// </summary>
        public readonly string DotNetReplaceExpression;

		/// <summary>
		/// <see cref="RegexOptions"/> which should be set while matching the expression. May be <B>null</B>
		/// if <see cref="regex"/> is already set.
		/// </summary>
		public RegexOptions DotNetOptions { get { return dotNetOptions; } }
		private RegexOptions dotNetOptions;

		public PerlRegexOptions PerlOptions { get { return perlOptions; } }
		private PerlRegexOptions perlOptions = PerlRegexOptions.None;

		private readonly Encoding/*!*/ encoding;

        /// <summary>
        /// An error message. Is <c>null</c> if all the conversions are ok.
        /// </summary>
        public string ArgumentException { get; private set; }

		#endregion

        #region Cache helper

        /// <summary>
        /// Internal pointer to the next <see cref="PerlRegExpConverter"/> in the list of cached <see cref="PerlRegExpConverter"/> instances.
        /// </summary>
        internal PerlRegExpConverter nextcache;
        
        /// <summary>
        /// Internal hits counter. Once it gets to specified constant number, <see cref="regex"/> gets compiled.
        /// </summary>
        private int hitsCount = 0;
        
        /// <summary>
        /// Current generation. Old generations can be removed from cache.
        /// </summary>
        internal long generation;
        
        internal readonly object _pattern;
        internal readonly string _replacement, _strpattern;
        
        internal void Cachehit()
        {
            int hitsCount = this.hitsCount;

            if (hitsCount < 3 && (Interlocked.Increment(ref this.hitsCount) == 3))
            {
                if (this.regex != null) // && (this.regex.Options & RegexOptions.Compiled) == 0)
                    this.regex = new Regex(this.regex.ToString(), this.dotNetOptions | RegexOptions.Compiled);
            }
        }
        
        /// <summary>
        /// Function that efficiently compares <c>this</c> instance of <see cref="PerlRegExpConverter"/> with another <see cref="PerlRegExpConverter"/>.
        /// 1st argument is reference to <c>this</c>.
        /// 2nd argument is the other's <see cref="PerlRegExpConverter._pattern"/>.
        /// 3nd argument is the other's <see cref="PerlRegExpConverter._replacement"/>.
        /// Function returns <c>true</c> if pattern and replacement match.
        /// </summary>
        internal readonly Func<PerlRegExpConverter, object, string, bool>/*!*/CacheEquals;

        /// <summary>
        /// Functions for efficient equality check.
        /// </summary>
        private struct CacheEqualsFunctions
        {
            static bool eq_null(PerlRegExpConverter self, object otherpattern, string otherreplacement) { return otherpattern == null && otherreplacement == self._replacement; }
            static bool eq_string_null(PerlRegExpConverter self, object otherpattern, string otherreplacement) { return otherpattern != null && otherreplacement == null && otherpattern.GetType() == typeof(string) && self._strpattern.Equals((string)otherpattern); }
            static bool eq_string(PerlRegExpConverter self, object otherpattern, string otherreplacement) { return otherpattern != null && otherreplacement != null && otherpattern.GetType() == typeof(string) && self._strpattern.Equals((string)otherpattern) && self._replacement.Equals(otherreplacement); }
            static bool eq_phpbytes(PerlRegExpConverter self, object otherpattern, string otherreplacement) { return otherpattern != null && otherpattern.GetType() == typeof(PhpBytes) && ((PhpBytes)otherpattern).Equals((PhpBytes)self._pattern) && otherreplacement == self._replacement; }
            static bool eq_phpstring(PerlRegExpConverter self, object otherpattern, string otherreplacement) { return otherpattern != null && otherpattern.GetType() == typeof(PhpString) && ((PhpString)otherpattern).Equals((PhpString)self._pattern) && otherreplacement == self._replacement; }
            static bool eq_default(PerlRegExpConverter self, object otherpattern, string otherreplacement) { return otherpattern != null && otherpattern.GetType() == self._pattern.GetType() && otherpattern.Equals(self._pattern) && otherreplacement == self._replacement; }

            // cached delegates
            static Func<PerlRegExpConverter, object, string, bool>/*!*/
                cacheeq_null = eq_null,
                cacheeq_string_null = eq_string_null,
                cacheeq_string = eq_string,
                cacheeq_phpbytes = eq_phpbytes,
                cacheeq_phpstring = eq_phpstring,
                cacheeq_default = eq_default;

            /// <summary>
            /// Select appropriate equality function delegate for given <see cref="PerlRegExpConverter"/>'s pattern and replacement.
            /// </summary>
            public static Func<PerlRegExpConverter, object, string, bool>/*!*/SelectEqualsFunction(object pattern, string replacement)
            {
                if (pattern == null) return CacheEqualsFunctions.cacheeq_null;
                else if (pattern.GetType() == typeof(string) && replacement == null) return CacheEqualsFunctions.cacheeq_string_null;
                else if (pattern.GetType() == typeof(string)) return CacheEqualsFunctions.cacheeq_string;
                else if (pattern.GetType() == typeof(PhpBytes)) return CacheEqualsFunctions.cacheeq_phpbytes;
                else if (pattern.GetType() == typeof(PhpString)) return CacheEqualsFunctions.cacheeq_phpstring;
                else return CacheEqualsFunctions.cacheeq_default;
            }
        }

        /// <summary>
        /// Initializes cache-specific fields of <see cref="PerlRegExpConverter"/> new instance.
        /// </summary>
        private PerlRegExpConverter(object pattern, string replacement)
        {
            // used for caching:
            this._pattern = PhpVariable.Copy(pattern, CopyReason.Assigned);
            this._strpattern = pattern as string;
            this._replacement = replacement;

            // initialize function that effectively checks given pattern whether it is equal to this pattern
            this.CacheEquals = CacheEqualsFunctions.SelectEqualsFunction(pattern, replacement);
        }
        
        #endregion

        /// <summary>
		/// Creates new <see cref="PerlRegExpConverter"/> and converts Perl regular expression to .NET.
		/// </summary>
		/// <param name="pattern">Perl regular expression to convert.</param>
		/// <param name="replacement">Perl replacement string to convert or a <B>null</B> reference for match only.</param>
		/// <param name="encoding">Encoding used in the case the pattern is a binary string.</param>
		public PerlRegExpConverter(object pattern, string replacement, Encoding/*!*/ encoding)
            :this(pattern, replacement)
		{
			if (encoding == null)
				throw new ArgumentNullException("encoding");

			this.encoding = encoding;

            ConvertPattern(pattern);

            if (replacement != null && this.regex != null)
                this.DotNetReplaceExpression = (replacement.Length == 0) ? string.Empty : PerlRegExpReplacement.ConvertReplacement(regex, replacement);
		}

		private void ConvertPattern(object pattern)
		{
            string perlRegEx;
            string dotNetMatchExpression = null;

            try
            {
                // convert pattern into string, parse options:
                if (pattern != null && pattern.GetType() == typeof(PhpBytes))
                    perlRegEx = LoadPerlRegex(((PhpBytes)pattern).ReadonlyData);
			    else
                    perlRegEx = LoadPerlRegex(PHP.Core.Convert.ObjectToString(pattern));
			
                // convert pattern into regex:
                dotNetMatchExpression = ConvertRegex(perlRegEx, perlOptions);

                // process the regex:
                this.regex = new Regex(dotNetMatchExpression, dotNetOptions);
			}
			catch (ArgumentException e)
			{
                this.ArgumentException = ExtractExceptionalMessage(e.Message, dotNetMatchExpression);
			}
		}

		/// <summary>
		/// Extracts the .NET exceptional message from the message stored in an exception.
		/// The message has format 'parsing "{pattern}" - {message}\r\nParameter name {pattern}' in .NET 1.1.
		/// </summary>
        private static string ExtractExceptionalMessage(string message, string dotNetMatchExpression)
		{
            if (message != null)
            {
                if (dotNetMatchExpression != null)
                    message = message.Replace(dotNetMatchExpression, "<pattern>");

                int i = message.IndexOf("\r\n");
                if (i >= 0)
                    message = message.Substring(0, i);

                i = message.IndexOf("-");
                if (i >= 0)
                    message = message.Substring(i + 2);

                return message;
            }
            else
            {
                return string.Empty;
            }
		}

		internal string ConvertString(string str, int start, int length)
		{
			if ((perlOptions & PerlRegexOptions.UTF8) != 0 && !StringUtils.IsAsciiString(str, start, length))
#if SILVERLIGHT
			{
		    byte[] bytes = new byte[encoding.GetByteCount(str)];
				encoding.GetBytes(str, 0, str.Length, bytes, 0);
		    return System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length);
			}
#else
				return Encoding.UTF8.GetString(encoding.GetBytes(str.Substring(start, length)));
#endif
			else
				return str.Substring(start, length);
		}

        internal string ConvertBytes(byte[] bytes)
        {
            return ConvertBytes(bytes, 0, bytes.Length);
        }

		internal string ConvertBytes(byte[] bytes, int start, int length)
		{
			if ((perlOptions & PerlRegexOptions.UTF8) != 0)
				return Encoding.UTF8.GetString(bytes, start, length);
			else
				return encoding.GetString(bytes, start, length);
		}

		private string LoadPerlRegex(byte[] pattern)
		{
			if (pattern == null) pattern = ArrayUtils.EmptyBytes;
			int regex_start, regex_end;

			StringUtils.UniformWrapper upattern = new StringUtils.BytesWrapper(pattern);

			FindRegexDelimiters(upattern, out regex_start, out regex_end);
			ParseRegexOptions(upattern, regex_end + 2, out dotNetOptions, out perlOptions);

			return ConvertBytes(pattern, regex_start, regex_end - regex_start + 1);
		}

		private string LoadPerlRegex(string pattern)
		{
			if (pattern == null) pattern = "";
			int regex_start, regex_end;

			StringUtils.UniformWrapper upattern = new StringUtils.StringWrapper(pattern);

			FindRegexDelimiters(upattern, out regex_start, out regex_end);
			ParseRegexOptions(upattern, regex_end + 2, out dotNetOptions, out perlOptions);

			return ConvertString(pattern, regex_start, regex_end - regex_start + 1);
		}

		private void FindRegexDelimiters(StringUtils.UniformWrapper pattern, out int start, out int end)
		{
			int i = 0;
			while (i < pattern.Length && Char.IsWhiteSpace(pattern[i])) i++;

			if (i == pattern.Length)
				throw new ArgumentException(LibResources.GetString("regular_expression_empty"));

			char start_delimiter = pattern[i++];
			if (Char.IsLetterOrDigit(start_delimiter) || start_delimiter == '\\')
				throw new ArgumentException(LibResources.GetString("delimiter_alnum_backslash"));

			start = i;
			char end_delimiter;
			if (start_delimiter == '[') end_delimiter = ']';
			else if (start_delimiter == '(') end_delimiter = ')';
			else if (start_delimiter == '{') end_delimiter = '}';
			else if (start_delimiter == '<') end_delimiter = '>';
			else end_delimiter = start_delimiter;

			int depth = 1;
			while (i < pattern.Length)
			{
				if (pattern[i] == '\\' && i + 1 < pattern.Length)
				{
					i += 2;
					continue;
				}
				else if (pattern[i] == end_delimiter)   // (1) should precede (2) to handle end_delim == start_delim case
				{
					depth--;
					if (depth == 0) break;
				}
				else if (pattern[i] == start_delimiter) // (2)
				{
					depth++;
				}
				i++;
			}

			if (i == pattern.Length)
				throw new ArgumentException(LibResources.GetString("preg_no_end_delimiter", end_delimiter));

			end = i - 1;
		}

		private static void ParseRegexOptions(StringUtils.UniformWrapper pattern, int start,
		  out RegexOptions dotNetOptions, out PerlRegexOptions extraOptions)
		{
			dotNetOptions = RegexOptions.None;
			extraOptions = PerlRegexOptions.None;

			for (int i = start; i < pattern.Length; i++)
			{
				char option = pattern[i];

				switch (option)
				{
					case 'i': // PCRE_CASELESS
						dotNetOptions |= RegexOptions.IgnoreCase;
						break;

					case 'm': // PCRE_MULTILINE
						dotNetOptions |= RegexOptions.Multiline;
						break;

					case 's': // PCRE_DOTALL
						dotNetOptions |= RegexOptions.Singleline;
						break;

					case 'x': // PCRE_EXTENDED
						dotNetOptions |= RegexOptions.IgnorePatternWhitespace;
						break;

					case 'e': // evaluate as PHP code
						extraOptions |= PerlRegexOptions.Evaluate;
						break;

					case 'A': // PCRE_ANCHORED
						extraOptions |= PerlRegexOptions.Anchored;
						break;

					case 'D': // PCRE_DOLLAR_ENDONLY
						extraOptions |= PerlRegexOptions.DollarMatchesEndOfStringOnly;
						break;

					case 'S': // spend more time studying the pattern - ignore
						break;

					case 'U': // PCRE_UNGREEDY
						extraOptions |= PerlRegexOptions.Ungreedy;
						break;

					case 'u': // PCRE_UTF8
						extraOptions |= PerlRegexOptions.UTF8;
						break;

					case 'X': // PCRE_EXTRA
						PhpException.Throw(PhpError.Warning, LibResources.GetString("modifier_not_supported", option));
						break;

					default:
						PhpException.Throw(PhpError.Notice, LibResources.GetString("modifier_unknown", option));
						break;
				}
			}

			// inconsistent options check:
			if
			(
			  (dotNetOptions & RegexOptions.Multiline) != 0 &&
			  (extraOptions & PerlRegexOptions.DollarMatchesEndOfStringOnly) != 0
			)
			{
				PhpException.Throw(PhpError.Notice, LibResources.GetString("modifiers_inconsistent", 'D', 'm'));
			}
		}

		/// <summary>
		/// Parses escaped sequences: "\[xX][0-9A-Fa-f]{2}", "\[xX]\{[0-9A-Fa-f]{0,4}\}", "\[0-7]{3}", 
		/// "\[pP]{Unicode Category}"
		/// </summary>
		private static bool ParseEscapeCode(
            //Encoding/*!*/ encoding,
            string/*!*/ str, ref int pos, ref int ch, ref bool escaped)
		{
			Debug.Assert(/*encoding != null &&*/ str != null && pos >= 0 && pos < str.Length && str[pos] == '\\');

			if (pos + 3 >= str.Length) return false;

			int number = 0;

			if (str[pos + 1] == 'x')
			{
				if (str[pos + 2] == '{')
				{
					// hexadecimal number encoding a Unicode character:
					int i = pos + 3;
					while (i < str.Length && str[i] != '}' && number < Char.MaxValue)
					{
						int digit = Core.Convert.AlphaNumericToDigit(str[i]);
						if (digit > 16) return false;
						number = (number << 4) + digit;
						i++;
					}
					if (/*number > Char.MaxValue || */i >= str.Length) return false;
					pos = i;
					ch = number;
					escaped = ch < Char.MaxValue ? IsCharRegexSpecial((char)ch) : false;
				}
				else
				{

					// hexadecimal number encoding single-byte character:
					for (int i = pos + 2; i < pos + 4; i++)
					{
						Debug.Assert(i < str.Length);
						int digit = Core.Convert.AlphaNumericToDigit(str[i]);
						if (digit > 16) return false;
						number = (number << 4) + digit;
					}
					pos += 3;
                    ch = number;
					//char[] chars = encoding.GetChars(new byte[] { (byte)number });
                    //if (chars.Length == 1)
                    //    ch = chars[0];
                    //else
                    //    ch = number;
                    escaped = ch < Char.MaxValue ? IsCharRegexSpecial((char)ch) : false;
				}
				return true;
			}
			else if (str[pos + 1] >= '0' && str[pos + 1] <= '7')
			{
				// octal number:
				for (int i = pos + 1; i < pos + 4; i++)
				{
					Debug.Assert(i < str.Length);
					int digit = Core.Convert.AlphaNumericToDigit(str[i]);
					if (digit > 8) return false;
					number = (number << 3) + digit;
				}
				pos += 3;
                ch = number;//encoding.GetChars(new byte[] { (byte)number })[0];
                escaped = ch < Char.MaxValue ? IsCharRegexSpecial((char)ch) : false;
				return true;
			}
			else if (str[pos + 1] == 'p' || str[pos + 1] == 'P')
			{
				bool complement = str[pos + 1] == 'P';
				int cat_start;

				if (str[pos + 2] == '{')
				{
					if (!complement && str[pos + 3] == '^')
					{
						complement = true;
						cat_start = pos + 4;
					}
					else
						cat_start = pos + 3;
				}
				else
				{
					cat_start = pos + 2;
				}

				UnicodeCategoryGroup group;
				UnicodeCategory category;

				int cat_length = StringUtils.ParseUnicodeDesignation(str, cat_start, out group, out category);
				int cat_end = cat_start + cat_length - 1;

				// unknown category:
				if (cat_length == 0) return false;

				// check closing brace:
				if (str[pos + 2] == '{' && (cat_end + 1 >= str.Length || str[cat_end + 1] != '}'))
					return false;

				// TODO: custom categories on .NET 2?
				// Unicode category:
				PhpException.Throw(PhpError.Warning, "Unicode categories not supported.");
				// ?? if (complement) pos = pos;
				return false;
			}
			else if (str[pos + 1] == 'X')
			{
				PhpException.Throw(PhpError.Warning, "Unicode categories not supported.");
				return false;
			}


			return false;
		}

		/// <summary>
		/// Characters that must be encoded in .NET regexp
		/// </summary>
		static char[] encodeChars = new char[] { '.', '$', '(', ')', '*', '+', '?', '[', ']', '{', '}', '\\', '^', '|' };

		/// <summary>
		/// Returns true if character needs to be escaped in .NET regex
		/// </summary>
		private static bool IsCharRegexSpecial(char ch)
		{
			return Array.IndexOf(encodeChars, ch) != -1;
		}

		/// <summary>
		/// Converts Perl match expression (only, without delimiters, options etc.) to .NET regular expression.
		/// </summary>
		/// <param name="perlExpr">Perl regular expression to convert.</param>
		/// <param name="opt">Regexp options - some of them must be processed by changes in match string.</param>
		/// <returns>Resulting .NET regular expression.</returns>
		private static string ConvertRegex(string perlExpr, PerlRegexOptions opt)
		{
			// Ranges in bracket expressions should be replaced with appropriate characters

			// assume no conversion will be performed, create string builder with exact length. Only in
			// case there is a range StringBuilder would be prolonged, +1 for Anchored
			StringBuilder result = new StringBuilder(perlExpr.Length + 1);

			// Anchored means that the string should match only at the start of the string, add '^'
			// at the beginning if there is no one
			if ((opt & PerlRegexOptions.Anchored) != 0 && (perlExpr.Length == 0 || perlExpr[0] != '^'))
				result.Append('^');

			// set to true after a quantifier is matched, if there is second quantifier just behind the
			// first it is an error
			bool last_quantifier = false;

			// 4 means we're switching from 3 back to 2 - ie. "a-b-c" 
			// (we need to make a difference here because second "-" shouldn't be expanded)
			bool leaving_range = false;

            // remember the last character added in the character class, so in state 3 we can expand the range as properly as possible
            int range_from_character = -1; 

			bool escaped = false;
			int state = 0;
			int inner_state = 0;
            HashSet<uint> addedSurrogate2Ranges = null; // cache of already added character pairs valid within character class [], dropped when switching to 0

			int i = 0;
			while (i < perlExpr.Length)
			{
				int ch = perlExpr[i];

				escaped = false;
				if (ch == '\\' && !ParseEscapeCode(/*encoding,*/ perlExpr, ref i, ref ch, ref escaped))
				{
					i++;
					Debug.Assert(i < perlExpr.Length, "Regex cannot end with backslash.");
					ch = perlExpr[i];

                    if (ch == 'g')
                    {
                        ++i;
                        inner_state = 5; // skip 'g' from resulting pattern
                        escaped = false; 
                        continue;
                    }
                    else if (ch == 'k')
                    {
                        inner_state = 11;
                        escaped = true;
                    }

					// some characters (like '_') don't need to be escaped in .net
                    // and ignore escaping of unicode sequence of characters
					if (ch == '_' || (int)ch > 0x7F) escaped = false; else escaped = true;
				}

				switch (state)
				{
					case 0: // outside of character class
						if (escaped)
						{
							result.Append('\\');
							Append(result, ch);
							last_quantifier = false; 
							break;
						}

						// In perl regexps, named groups are written like this: "(?P<name> ... )"
                        //  (\k<name>...)
                        //  (\k'name'...)
                        //  (\k{name}...)
                        //  (\g{name}...)
                        //  (?'name'...)
                        //  (?<name>...)
                        //  (?P=name)
  
						// If the group is starting here, we need to skip the 'P' character (see state 4)
						switch (inner_state)
						{
							case 0:
                                if (ch == '(')
                                    inner_state = 1;
                                else if (ch == '\\')
                                    inner_state = 4;
                                else
                                    inner_state = 0;
                                
                                break;

                            //groups
							case 1:
                                if (ch == '?')
                                    inner_state = 2;
                                else if (ch != '(')// stay in inner_state == 1, because this can happen: ((?<blah>...))
                                    inner_state = 0;
                                break;
							case 2:
                                if (ch == 'P')
                                {
                                    i++;
                                    inner_state = 3;
                                    continue; //skip 'P' from resulting pattern
                                }
                                else if (ch == '<')
                                {
                                    inner_state = 15;
                                    break;
                                }
                                else if (ch == '\'')
                                {
                                    i++;
                                    result.Append('\'');
                                    result.Append(GroupPrefix);

                                    inner_state = 0;
                                    continue;
                                }
                                
                                inner_state = 0;
                                break;
                            case 3: // '(?P'
                                if (ch == '=') 
                                {
                                    ++i;
                                    inner_state = 12;
                                    continue; //skip '=' from resulting pattern
                                }
                                else if (ch != '<')// if P wasn't part of "(?P<name> ... )" neither '(?P=name)' back reference, so put it back to the pattern
                                {
                                    result.Append('P');
                                }
                                else if (ch == '<')
                                {
                                    i++;
                                    result.Append('<');
                                    result.Append(GroupPrefix);

                                    inner_state = 0;
                                    continue;
                                }

                                inner_state = 0;
                                break;

                            // /g[0-9]{1,2} back references
                            case 5: // '\g'

                                result.Append('\\');

                                if (ch == '{')
                                {
                                    i++;
                                    inner_state = 6;
                                    continue; // skip '{' from resulting pattern
                                }
                                else if (ch >= '0' && ch <= '9')
                                {
                                    inner_state = 0; // just copy the rest of the pattern
                                }
                                else
                                {
                                    result.Append('g'); // unexpected character after '/g', so put g back to pattern
                                    inner_state = 0;
                                }
                                break;
                            case 6: // '\g{'

                                if (ch >= '0' && ch <= '9')
                                {
                                    inner_state = 7;
                                }
                                else
                                {
                                    // it can be named group
                                    result.Append("k<");
                                    result.Append(GroupPrefix);
                                    inner_state = 10;

                                    //result.Append("g{"); // unexpected character after '/g{', so put it back to pattern
                                    //group_state = 0;
                                }

                                break;

                            case 7:// '\g{[0-9]'

                                if (ch == '}')
                                {
                                    i++;
                                    inner_state = 9;
                                    continue; // skip '}' from resulting pattern
                                }
                                else if (ch >= '0' && ch <= '9')
                                {
                                    inner_state = 8;
                                }
                                else
                                {
                                    //name of the group starts with a number
                                    //put behind PreGroupNameSign
                                    result.Insert(result.Length - 1,"k<");
                                    result.Insert(result.Length - 1, GroupPrefix);
                                    inner_state = 14;
                                }



                                break;

                            case 8: // '\g{[0-9][0-9]'

                                if (ch == '}')
                                {
                                    i++;
                                    inner_state = 9;
                                    continue; // skip '}' from resulting pattern
                                }
                                else
                                {
                                    //name of the group starts with a number
                                    //put behind PreGroupNameSign
                                    result.Insert(result.Length - 1, "k<");
                                    result.Insert(result.Length - 2, GroupPrefix);
                                    inner_state = 14;
                                }

                                // there is just 99 back references possible

                                inner_state = 0;

                                break;

                            case 9:// '\g{[0-9][0-9]?}'

                                if (ch >= '0' && ch <= '9')
                                {
                                    result.Append("(?#)"); // put this to the resulting pattern to separate number of the reference from number that follows
                                }

                                inner_state = 0;

                                break;

                            // named back references
                            case 10:// '\g{.*?}' | '\k{.*?}'

                                if (ch == '}')
                                {
                                    ++i;
                                    result.Append('>');
                                    inner_state = 0;
                                    continue; // skip '}' from resulting pattern
                                }

                                break;

                            case 11:// '\k'

                                if (ch == '{')
                                {
                                    i++;
                                    inner_state = 10;
                                    result.Append('<');
                                    result.Append(GroupPrefix);
                                    continue; // skip '{' from resulting pattern
                                }
                                else if (ch == '<')
                                {
                                    i++;
                                    result.Append('<');
                                    result.Append(GroupPrefix);

                                    inner_state = 0;
                                    continue;
                                }
                                else if (ch == '\'')
                                {
                                    i++;
                                    result.Append('\'');
                                    result.Append(GroupPrefix);

                                    inner_state = 0;
                                    continue;
                                }

                                inner_state = 0;

                                break;

                            // transforming '(?P=name)' to '\k<name>'
                            case 12: // '(?P='

                                // (? was already put in the pattern, so replace it with '\k'
                                result[result.Length - 2] = '\\';
                                result[result.Length - 1] = 'k';
                                
                                // add '<' so it is '\k<'
                                result.Append('<');
                                result.Append(GroupPrefix);

                                inner_state = 13;

                                break;

                            case 13: // '(?P=.*?'

                                if (ch == ')')
                                {
                                    ++i;
                                    result.Append('>');
                                    inner_state = 0;
                                    continue; // skip ')' from resulting pattern
                                }

                                break;

                            case 14:// '\g{[0-9].*?'

                                if (ch == '}')
                                {
                                    i++;
                                    inner_state = 9;
                                    result.Append(">");
                                    continue; // skip '}' from resulting pattern
                                }

                                break;

                            case 15:// (?<

                                //Add group prefix only if it's not lookbehind assertions
                                //(?<! negative
                                //(?<= positive
                                if (ch != '!' && ch != '=')
                                {
                                    result.Append(GroupPrefix);

                                }

                                inner_state = 0;

                                break;


                            
                            default: inner_state = 0; break;
						}

						if ((opt & PerlRegexOptions.Ungreedy) != 0)
						{
							// match quantifier ?,*,+,{n,m} at the position i:
							Match m = quantifiers.Match(perlExpr, i);

							// quantifier matched; quentifier '?' hasn't to be preceded by '(' - a grouping construct '(?'
							if (m.Success && (m.Value != "?" || i == 0 || perlExpr[i - 1] != '('))
							{
								// two quantifiers: 
								if (last_quantifier)
									throw new ArgumentException(LibResources.GetString("regexp_duplicate_quantifier", i));

								// append quantifier:
								result.Append(perlExpr, i, m.Length);
								i += m.Length;

								if (i < perlExpr.Length && perlExpr[i] == '?')
								{
									// skip question mark to make the quantifier greedy:
									i++;
								}
                                else if (i < perlExpr.Length && perlExpr[i] == '+')
                                {
                                    // TODO: we do not yet support possesive quantifiers
                                    //       so we just skip the attribute it and pray
                                    //       nobody will ever realize :-)
                                    i++;
                                }
                                else
								{
									// add question mark to make the quantifier lazy:
									if (result.Length != 0 && result[result.Length - 1] == '?')
									{
										// HACK: Due to the issue in .NET regex we can't use "??" because it isn't interpreted correctly!!
										// (for example "^(ab)??$" matches with "abab", but it shouldn't!!)
									}
									else
										result.Append('?');
								}

								last_quantifier = true;
								continue;
							}
						}

						last_quantifier = false;

						if (ch == '$' && (opt & PerlRegexOptions.DollarMatchesEndOfStringOnly) != 0)
						{
							// replaces '$' with '\z': 
							result.Append(@"\z");
							break;
						}

						if (ch == '[')
							state = 1;

                        Append(result, ch);
                        break;

					case 1: // first character of character class
						if (escaped)
						{
							result.Append('\\');
                            Append(result, ch);
                            range_from_character = ch;
							state = 2;
							break;
						}

						// special characters:
						if (ch == '^' || ch == ']' || ch == '-')
						{
							Append(result, ch);
						}
						else
						{
							// other characters are not consumed here, for example [[:space:]abc] will not match if the first
							// [ is appended here.
							state = 2;
							goto case 2;
						}
						break;

					case 2: // inside of character class
                        if (escaped)
						{
                            result.Append('\\');
                            Append(result, ch);
                            range_from_character = ch;
							leaving_range = false;
							break;
						}

						if (ch == '-' && !leaving_range)
						{
							state = 3;
							break;
						}
						leaving_range = false;

						// posix character classes
						Match match = posixCharClasses.Match(perlExpr.Substring(i), 0);
						if (match.Success)
						{
							string chars = PosixRegExp.BracketExpression.CountCharacterClass(match.Groups[2].Value);
							if (chars == null)
								throw new ArgumentException(/*TODO*/ String.Format("Unknown character class '{0}'", match.Groups[2].Value));

							if (match.Groups[1].Value.Length > 0)
								throw new ArgumentException(/*TODO*/ "POSIX character classes negation not supported.");

							result.Append(chars);
                            range_from_character = -1;  // -1 means, it is not rangable :)
							i += match.Length - 1; // +1 is added just behind the switch
							break;
						}

                        if (ch == ']')
                        {
                            addedSurrogate2Ranges = null;   // drop the cache of ranges
                            state = 0;
                        }
                        
                        // append <ch>
                        range_from_character = ch;

						if (ch == '-')
                            result.Append("\\x2d");
                        else
                            AppendEscaped(result, ch);

                        break;

					case 3: // range previous character was '-'
						if (!escaped && ch == ']')
						{
                            if (range_from_character > char.MaxValue)
                                throw new ArgumentException("Cannot range from an UTF-32 character to unknown.");
                            
							result.Append("-]");
                            addedSurrogate2Ranges = null;   // drop the cache of ranges
                            state = 0;
                            break;
						}

                        //string range;
                        //int error;
                        //if (!PosixRegExp.BracketExpression.CountRange(result[result.Length - 1], ch, out range, out error))
                        //{
                        //    if ((error != 1) || (!CountUnicodeRange(result[result.Length - 1], ch, out range)))
                        //    {
                        //        Debug.Assert(error == 2);
                        //        throw new ArgumentException(LibResources.GetString("range_first_character_greater"));
                        //    }
                        //}
                        //PosixRegExp.BracketExpression.EscapeBracketExpressionSpecialChars(result, range); // left boundary is duplicated, but doesn't matter...

                        if (addedSurrogate2Ranges == null)
                            addedSurrogate2Ranges = new HashSet<uint>();    // initialize the cache of already added character ranges, invalidated at the end of character class

                        if (ch != range_from_character)
                        {
                            // <from>-<ch>:

                            // 1. <utf16>-<utf16>
                            // 2. <utf16>-<utf32>
                            // 3. <utf32>-<utf32>

                            if (range_from_character <= char.MaxValue)
                            {
                                if (ch <= char.MaxValue)
                                {
                                    // 1.
                                    result.Append('-');
                                    AppendEscaped(result, ch);
                                }
                                else
                                {
                                    // 2.
                                    result.Append('-');
                                    AppendEscaped(result, char.MaxValue);
                                    
                                    // count <char.MaxValue+1>-<ch>
                                    CountUTF32Range(result, char.MaxValue + 1, ch, addedSurrogate2Ranges);
                                }
                            }
                            else
                            {
                                // 3. utf32 range
                                result.Length -= 2;
                                CountUTF32Range(result, range_from_character, ch, addedSurrogate2Ranges);
                            }
                        }

						state = 2;
						leaving_range = true;
                        range_from_character = -1;
						break;
				}

				i++;
			}

            return ConvertPossesiveToAtomicGroup(result);
		}

        private static void AppendEscaped(StringBuilder/*!*/sb, int ch)
        {
            Debug.Assert(sb != null);

            if (ch < 0x80 && ch != '\\' && ch != '-')
                sb.Append((char)ch);
            else
                AppendUnicode(sb, ch);
        }

        private static void Append(StringBuilder/*!*/sb, int ch)
        {
            Debug.Assert(sb != null);
            Debug.Assert(ch >= 0);

            if (ch < Char.MaxValue && ch > 0)
                sb.Append((char)ch);
            else
                AppendUnicode(sb, ch);
        } 

        private static void AppendUnicode(StringBuilder/*!*/sb, int ch)
        {
            Debug.Assert(sb != null);

            if (ch <= Char.MaxValue)
                sb.Append(@"\u" + ((int)ch).ToString("X4"));
            else
                sb.Append(Char.ConvertFromUtf32(ch));
        }

        #region Conversion of possesive quantifiers

        internal struct brace
        {
            public brace(int position, char braceType)
            {
                this.position = position;
                this.braceType = braceType;
            }

            public int position;
            public char braceType;
        }

        /// <summary>
        /// Convert possesive quantifiers to atomic group, which .NET support.
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        /// <remarks>Works on these cases *+, ++, ?+, {}+
        /// </remarks>
        private static string ConvertPossesiveToAtomicGroup(StringBuilder pattern)
        {
            const int preallocatedAttomicGroups = 10;
            const string atomicGroupStart = "(?>";
            const string atomicGroupEnd = ")";
            Stack<brace> braceStack = new Stack<brace>(16);
            int state = 0;
            int escape_state = 0;
            int escapeSequenceStart = 0;
            int offset = 0;

            StringBuilder sb = new StringBuilder(pattern.Length + atomicGroupStart.Length * preallocatedAttomicGroups);//, 0, pattern.Length, pattern.Length + 4 * 10); // (?>)

            Action<int> addAtomicGroup =
                (start) =>
                {
                    sb.Insert(start, atomicGroupStart);
                    sb.Append(atomicGroupEnd);
                    offset += atomicGroupStart.Length;
                };

            Action<int, char> pushToStack = (pos, ch) =>
            {
                braceStack.Push(new brace(pos + offset, ch));
            };

            brace LastBrace = new brace();

            bool escaped = false;

            int i = 0;
            while (i < pattern.Length)
            {
                char ch = pattern[i];

                //TODO: handle comments

                if (!escaped)
                {
                    switch (state)
                    {
                        case 0:
                            if (ch == '(')
                            {
                                pushToStack(i, ch);
                                state = 2;
                            }
                            else if (ch == '[')
                            {
                                pushToStack(i, ch);
                                state = 12;
                            }

                            break;


                        case 2: // (. 
                            if (ch == ')')
                                state = 3;
                            else if (ch == '(') //nested (
                                pushToStack(i, ch);
                            else if (ch == '[')
                            {
                                state = 12;
                                pushToStack(i, ch);
                            }

                            break;

                        case 3: // (...)

                            LastBrace = braceStack.Pop();

                            if (ch == '*' || ch == '?' || ch =='+')
                                state = 4;
                            else if (ch == '{')
                                state = 5;
                            else
                            {
                                state = DecideState(pattern, braceStack);
                                continue;
                            }

                            break;

                        case 4: // (...)*+ | (...)++ | (...)?+

                            if (ch == '+')
                            {
                                addAtomicGroup(LastBrace.position);

                                state = DecideState(pattern, braceStack);
                                ++i;
                                continue;
                            }
                            else
                                state = DecideState(pattern, braceStack);

                            break;

                        case 5: // (...){

                            if (ch == '}')
                                state = 4;
                            //if (!char.IsDigit(ch))
                            //{
                            //    state = DecideState(pattern, braceStack);
                            //}

                            break;

                        case 12: // [. 
                            if (ch == ']')
                                state = 13;
                            //else 
                            //if (ch == '(')
                            //{
                            //    state = 2;
                            //    pushToStack(i, ch);
                            //}
                            else if (ch == '[')
                            {
                                pushToStack(i, ch);
                            }

                            break;

                        case 13: // [...]

                            LastBrace = braceStack.Pop();

                            if (ch == '*' || ch == '?' || ch == '+')
                                state = 14;
                            else if (ch == '{')
                                state = 15;
                            else
                                state = DecideState(pattern, braceStack);


                            break;

                        case 14: // [...]*+

                            if (ch == '+')
                            {
                                addAtomicGroup(LastBrace.position);

                                state = DecideState(pattern, braceStack);
                                ++i;
                                continue;
                            }
                            else
                            {
                                state = DecideState(pattern, braceStack);
                                continue;
                            }

                        case 15: // [...]{

                            if (ch == '}')
                                state = 4;

                            break;
                    }
                }
                else
                {
                    //escaped

                    switch (escape_state)
                    {
                        case 0:

                            if (ch == '\\')
                            {
                                escape_state = 0;
                            }
                            else
                            {
                                escape_state = 1;
                            }

                            break;

                        case 1:// \.

                            if (ch == '*' || ch == '?' || ch == '+')
                                escape_state = 2;
                            else
                                escape_state = 0;


                            break;

                        case 2:

                            if (ch == '+')
                            {
                                escape_state = 0;
                                addAtomicGroup(escapeSequenceStart);
                                ++i;
                            }
                            else
                            {
                                escape_state = 0;
                            }

                            break;

                    }

                    if (escape_state == 0)
                    {
                        escaped = false;
                        continue;
                    }
                }


                if (ch == '\\' && escaped == false)
                {
                    escaped = true;
                    escapeSequenceStart = i + offset;
                }

                ++i;
                sb.Append(ch);

            }

            return sb.ToString();

        }
        private static int DecideState(StringBuilder pattern, Stack<brace> braceStack)
        {
            int state;
            if (braceStack.Count > 0)
            {
                brace SecondToLastBrace = braceStack.Pop();
                braceStack.Push(SecondToLastBrace);

                if (SecondToLastBrace.braceType == '(')
                    state = 2;
                else
                    state = 12;
            }
            else
                state = 0;

            return state;
        }

        #endregion

        ///// <summary>
        ///// Simple version of 'PosixRegExp.BracketExpression.CountRange' function. Generates string
        ///// with all characters in specified range, but uses unicode encoding.
        ///// </summary>
        ///// <param name="f">Lower bound</param>
        ///// <param name="t">Upper bound</param>
        ///// <param name="range">Returned string</param>
        ///// <returns>Returns false if lower bound is larger than upper bound</returns>
        //private static bool CountUnicodeRange(char f, char t, out string range)
        //{
        //    range = "";
        //    if (f > t) return false;
        //    StringBuilder sb = new StringBuilder(t - f);
        //    for (char c = f; c <= t; c++) sb.Append(c);
        //    range = sb.ToString();
        //    return true;
        //}

        /// <summary>
        /// Ranges characters from <paramref name="chFrom"/> up to <paramref name="chTo"/> inclusive, where characters are UTF32.
        /// We will only list every from surrogate pair once (same result as writing all the characters one by one).
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="chFrom"></param>
        /// <param name="chTo"></param>
        /// <param name="addedSurrogate2Ranges">Cache of already added character pairs to avoid duplicitous character ranges.</param>
        private static void CountUTF32Range(StringBuilder/*!*/sb, int chFrom, int chTo, HashSet<uint>/*!*/addedSurrogate2Ranges)
        {
            Debug.Assert(addedSurrogate2Ranges != null);
            Debug.Assert(chFrom > Char.MaxValue);
            Debug.Assert(chTo > Char.MaxValue);
            
            //
            chFrom -= char.MaxValue + 1;
            chTo -= char.MaxValue + 1;

            // whether the range of the same surrogate1 starts
            bool start = true;

            // range UTF32 characters
            for (; chFrom <= chTo; chFrom++)
            {
                // current UTF32 character "<a1><a2>":
                char a1 = (char)(chFrom / 1024 + 55296);    // surrogate pair [1]
                char a2 = (char)(chFrom % 1024 + 56320);    // surrogate pair [2]

                //var str = new string(new char[] { a1, a2 });  // single UTF32 character

                // output:
                if (start)  // first character from the range of the same surrogate1
                {
                    start = false;
                    // "<a1><a2>"
                    
                    // try to compress <a1>:

                    // convert "-ab" to "-b", where a+1 == b
                    if (sb.Length >= 2 &&
                        sb[sb.Length - 1] + 1 == a1 &&
                        sb[sb.Length - 2] == '-' &&
                        (sb.Length < 3 || sb[sb.Length - 3] != '\\'))    // '-' is not escaped
                    {
                        sb[sb.Length - 1] = a1; // extend the range
                    }
                    // convert "abc" to "a-c", where a+2 == b+1 == c
                    else if (sb.Length >= 2 &&
                            sb[sb.Length - 1] + 1 == a1 &&
                            sb[sb.Length - 2] + 2 == a1)
                    {
                        // a,b,c are all UTF32 surrogate1
                        sb[sb.Length - 1] = '-';
                        sb.Append(a1);
                    }
                    else
                    {
                        sb.Append(a1);
                    }

                    //
                    sb.Append(a2);
                }
                else if ((chFrom + 1) > chTo || a1 != (char)((chFrom + 1) / 1024 + 55296)) // finish the range (end of the range || different next surrogate1)
                {
                    AddCharacterRangeChecked(sb, a2, addedSurrogate2Ranges);
                    start = true;
                }
                else
                {
                    // in range ...
                }
            }
        }

        /// <summary>
        /// Adds "-<paramref name="chTo"/>" iff there is not the same character range in the result already. Otherwise the last character form <paramref name="sb"/> is removed.
        /// </summary>
        /// <param name="sb"><see cref="StringBuilder"/> with lower bound of the range already added.</param>
        /// <param name="chTo">Upper bound of the range.</param>
        /// <param name="addedSurrogate2Ranges">Cache of already added character pairs.</param>
        /// <remarks>Assumes there is starting character at the end of <paramref name="sb"/>.</remarks>
        private static void AddCharacterRangeChecked(StringBuilder/*!*/sb, char chTo, HashSet<uint>/*!*/addedSurrogate2Ranges)
        {
            Debug.Assert(addedSurrogate2Ranges != null);
            Debug.Assert(sb.Length > 0);

            char previous = sb[sb.Length - 1];  // the lower bound already in the result
            uint print = (uint)previous | ((uint)chTo << 16); // the "hash" of the character range to be inserted

            if (addedSurrogate2Ranges.Add(print))   // is the <print> range not in the result yet?
            {
                // and of the range with the same surrogate1
                // "<previous>-<chTo>" wil be in the <sb>
                sb.Append('-');
                sb.Append(chTo);
            }
            else
            {
                // "<previous>-<chTo>" already in the result, just "remove" the last character from the <sb>
                sb.Length--;
            }
        }

        /// <summary>
        /// Modifies regular expression so it matches only at the beginning of the string.
        /// </summary>
        /// <param name="expr">Regular expression to modify.</param>
        private static void ModifyRegExpAnchored(ref string expr)
		{
			// anchored means regular expression should match only at the beginning of the string
			// => add ^ at the beginning if there is no one.
			if (expr.Length == 0 || expr[0] != '^')
				expr.Insert(0, "^");
		}

		#region Unit Test
#if !SILVERLIGHT
#if DEBUG
		[Test]
		private static void TestConvertRegex()
		{
			string s;
			s = ConvertRegex(@"?a+sa?s (?:{1,2})", PerlRegexOptions.Ungreedy);
			Debug.Assert(s == "??a+?sa??s (?:{1,2}?)");

			s = ConvertRegex(@"(X+)(?:\|(.+?))?]](.*)$", PerlRegexOptions.Ungreedy);
			Debug.Assert(s == @"(X+?)(?:\|(.+))??]](.*?)$");

			s = ConvertRegex(@"([X$]+)$", PerlRegexOptions.DollarMatchesEndOfStringOnly);
			Debug.Assert(s == @"([X$]+)\z");
		}
#endif
#endif
		#endregion
	}

	#endregion
}
