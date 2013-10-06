/*

 Copyright (c) 2004-2006 Tomas Matousek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/
using System;
using System.Threading;
using System.Globalization;
using System.Collections;
using System.ComponentModel;

using PHP.Core;
using System.Diagnostics;

#if SILVERLIGHT
using PHP.CoreCLR;
#endif

namespace PHP.Library
{
	/// <summary>
	/// This class manages locale information for PHP and interacts .NET Framework.
	/// </summary>
	/// <threadsafety static="true"/>
	public static class Locale
	{
		[ImplementsConstant("CHAR_MAX")]
		public const int CHAR_MAX = 127;

		private static readonly char[] CultureNameSeparators = new char[] { '-', '_' };

		#region Categorized Cultures

		/// <summary>
		/// A locale categories.
		/// </summary>
		/// <exclude/>
		public enum Category
		{
			/// <summary>
			/// Assigning a culture to this category is equivalent to assigning it to all other categories.
			/// </summary>
			[ImplementsConstant("LC_ALL")]
			All,

			/// <summary>
			/// Influences function <c>strcoll</c>.
			/// </summary>
			[ImplementsConstant("LC_COLLATE")]
			Collate,

			/// <summary>
			/// Influences functions <c>strtolower</c>, <c>strtoupper</c>
			/// </summary>
			[ImplementsConstant("LC_CTYPE")]
			CType,

			/// <summary>
			/// Influences functions <c>money_format</c>, <c>localeconv</c>
			/// </summary>
			[ImplementsConstant("LC_MONETARY")]
			Monetary,

			/// <summary>
			/// Influences function <c>localeconv</c> and formatting of all floating-point numbers.
			/// </summary>
			[ImplementsConstant("LC_NUMERIC")]
			Numeric,

			/// <summary>
			/// Influences function <c>strftime</c>.
			/// </summary>
			[ImplementsConstant("LC_TIME")]
			Time
		}

		/// <summary>
		/// Cultures associated with cathegories.
		/// </summary>
		private static CultureInfo[] cultures
		{
			get
			{
				if (_cultures == null)
					_cultures = new CultureInfo[(int)Category.Time + 1];

				return _cultures;
			}
		}

#if !SILVERLIGHT
		[ThreadStatic]
#endif
		private static CultureInfo[] _cultures;

		static Locale()
		{
            RequestContext.RequestEnd += new Action(Clear);
		}

		private static void Clear()
		{
			_cultures = null;
		}

		/// <summary>
		/// Gets a culture specific for the given category.
		/// </summary>
		/// <param name="category">The category.</param>
		/// <returns>Non-null culture info.</returns>
		public static CultureInfo GetCulture(Category category)
		{
			if ((int)category < 0 || (int)category >= cultures.Length)
				throw new ArgumentOutOfRangeException("category");

			return cultures[(int)category] ?? CultureInfo.CurrentCulture;
		}

		/// <summary>
		/// Sets a culture specific for the given category.
		/// </summary>
		/// <param name="category">The category.</param>
		/// <param name="culture">The culture.</param>
		public static void SetCulture(Category category, CultureInfo culture)
		{
			if ((int)category < 0 || (int)category >= cultures.Length)
				throw new ArgumentOutOfRangeException("category");

			// sets specific culture:
			if (category == Category.All)
			{
				for (int i = 0; i < cultures.Length; i++)
					cultures[i] = culture;
			}
			else
			{
				cultures[(int)category] = culture;
			}

			// sets global culture used in many places:
			if (category == Category.All || category == Category.Numeric)
				Thread.CurrentThread.CurrentCulture = culture;
		}

		/// <summary>
		/// Creates a new <see cref="PhpLocaleStringComparer"/> comparing according to the current collate.
		/// </summary>
		/// <param name="ignoreCase">Whether to create a case-insensitive comparer.</param>
		/// <returns>The comparer.</returns>
		public static PhpLocaleStringComparer GetStringComparer(bool ignoreCase)
		{
			return new PhpLocaleStringComparer(GetCulture(Category.Collate), ignoreCase);
		}

		#endregion

		#region localeconv

		/// <summary>
		/// Converts .NET groups information to PHP array.
		/// </summary>
		private static PhpArray GetGroupingArray(int[] groups)
		{
			Debug.Assert(groups != null);

			int length = groups.Length;
			PhpArray result = new PhpArray(length, 0);
			for (int i = 0; i < length; i++)
				if (groups[i] == 0)
					result.Add(i, CHAR_MAX);
				else
					result.Add(i, groups[i]);

			return result;
		}

		/// <summary>
		/// Gets information about the current thread culture.
		/// </summary>
		/// <returns>The associative array of number and currency information.</returns>
		[ImplementsFunction("localeconv")]
		public static PhpArray localeconv()
		{
			PhpArray result = new PhpArray(0, 18);
			NumberFormatInfo number;

			number = GetCulture(Category.Numeric).NumberFormat;

			result.Add("decimal_point", number.NumberDecimalSeparator);
			result.Add("thousands_sep", number.NumberGroupSeparator);
			result.Add("grouping", GetGroupingArray(number.CurrencyGroupSizes));
			result.Add("positive_sign", number.PositiveSign);
			result.Add("negative_sign", number.NegativeSign);
			result.Add("frac_digits", number.CurrencyDecimalDigits);

			number = GetCulture(Category.Monetary).NumberFormat;

			result.Add("currency_symbol", number.CurrencySymbol);
			result.Add("mon_decimal_point", number.CurrencyDecimalSeparator);
			result.Add("mon_thousands_sep", number.CurrencyGroupSeparator);
			result.Add("mon_grouping", GetGroupingArray(number.CurrencyGroupSizes));

			// currency patterns: 0 -> $n, 1 -> n$, 2 -> $ n, 3 -> n $
			result.Add("p_cs_precedes", number.CurrencyPositivePattern == 0 || number.CurrencyPositivePattern == 2);
			result.Add("p_sep_by_space", number.CurrencyPositivePattern == 2 || number.CurrencyPositivePattern == 3);
			result.Add("n_cs_precedes", number.CurrencyNegativePattern == 0 || number.CurrencyNegativePattern == 2);
			result.Add("n_sep_by_space", number.CurrencyNegativePattern == 2 || number.CurrencyNegativePattern == 3);

			result.Add("p_sign_posn", 1);
			result.Add("n_sign_posn", 1);

			return result;
		}

		#endregion

		#region setlocale, strcoll, nl_langinfo (NS)

#if !SILVERLIGHT
		/// <summary>
		/// Sets or gets the current thread culture settings.
		/// </summary>
		/// <param name="category">
		/// A category to be modified. The only supported value in this version is <see cref="Category.All"/>.
		/// </param>
		/// <param name="locale">Either an instance of <see cref="PhpArray"/> containing locales or a locale.</param>
		/// <param name="moreLocales">If <paramref name="locale"/> is not of type <see cref="PhpArray"/> contains locales, ignored otherwise.</param>
		/// <returns>The culture string (e.g. "en-US").</returns>
		/// <remarks>
		/// <para>
		/// Values specified in <paramref name="locale"/> and <paramref name="moreLocales"/> are converted to strings.
		/// Each value should have format "{language}-{region}" or "{language}_{region}" or "{language}" or special values "C" or empty string
		/// which represents the invariant culture or special values <B>null</B> or "0" which means no changes is made 
		/// by the method rather the current culture name is returned. 
		/// The first value containing am existing culture string is used.
		/// </para>
		/// </remarks>
		/// <exception cref="PhpException"><paramref name="category"/> has an invalid or unsupported value. (Warning)</exception>
		[ImplementsFunction("setlocale")]
		[return: CastToFalse]
		public static string SetLocale(Category category, object locale, params object[] moreLocales)
		{
			CultureInfo new_culture;

			if (GetFirstExistingCulture(locale, moreLocales, out new_culture))
			{
				if ((int)category < 0 || (int)category > cultures.Length)
				{
					PhpException.InvalidArgument("category", LibResources.GetString("arg:invalid_value"));
					return null;
				}

				// sets specific culture:
				SetCulture(category, new_culture);
			}
			else
			{
				new_culture = CultureInfo.CurrentCulture;
			}

			if (new_culture == CultureInfo.InvariantCulture)
				return "C";

			return String.Format("{0}.{1}",
			  new_culture.EnglishName.Replace(" (", "_").Replace(")", ""),
			  new_culture.TextInfo.ANSICodePage);
		}

		/// <summary>
		/// Searches in given objects for a locale string describing an existing culture.
		/// </summary>
		/// <param name="locale">Contains either an instance of <see cref="PhpArray"/> containing locales or a locale.</param>
		/// <param name="moreLocales">If <paramref name="locale"/> is not of type <see cref="PhpArray"/> contains locales, ignored otherwise.</param>
		/// <param name="culture">The resulting culture. A <B>null</B> reference means no culture has been found.</param>
		/// <returns>Whether a culture settings should be changed.</returns>
		private static bool GetFirstExistingCulture(object locale, object[] moreLocales, out CultureInfo culture)
		{
			PhpArray array;
			IEnumerator locales;
			culture = null;

			if ((array = locale as PhpArray) != null)
			{
				// locales are stored in the "locale" array:
				locales = array.GetEnumerator();
				locales.MoveNext();
				locale = locales.Current;
			}
			else if (moreLocales != null)
			{
				// locales are stored in the "locale" and "moreLocales":
				locales = moreLocales.GetEnumerator();
			}
			else
			{
				throw new ArgumentNullException("moreLocales");
			}

			// enumerates locales and finds out the first which is valid:
			for (; ; )
			{
				string name = (locale != null) ? Core.Convert.ObjectToString(locale) : null;

				culture = GetCultureByName(name);

				// name is "empty" then the current culture is not changed:
				if (name == null || name == "0") return false;

				// if culture exists and is specific then finish searching:
				if (culture != null) return true;

				// the next locale:
				if (!locales.MoveNext()) return false;

				locale = locales.Current;
			}
		}

		/// <summary>
		/// Gets a culture of a specified name. 
		/// Tries "{language}-{country}", "{country}-{language}".
		/// Recognizes "C", "", "0" and <B>null</B> as invariant culture.
		/// Note, PHP swaps language and country codes.
		/// </summary>
		private static CultureInfo GetCultureByName(string name)
		{
			// invariant culture:
			if (name == null || name == "0" || name == String.Empty || name == "C")
				return CultureInfo.InvariantCulture;

			int separator = name.IndexOfAny(CultureNameSeparators);
			if (separator < 0)
			{
				try
				{
					return CultureInfo.CreateSpecificCulture(name);
				}
				catch (ArgumentException)
				{
				}
			}
			else
			{
				string part1 = name.Substring(0, separator);
				string part2 = name.Substring(separator + 1);
				try
				{
					return CultureInfo.CreateSpecificCulture(String.Concat(part1, "-", part2));
				}
				catch (ArgumentException)
				{
					try
					{
						return CultureInfo.CreateSpecificCulture(String.Concat(part2, "-", part1));
					}
					catch (ArgumentException)
					{
					}
				}
			}

			return null;
		}

        /// <summary>
		/// Compares two specified strings, honoring their case, using culture specific comparison.
		/// </summary>
		/// <param name="str1">A string.</param>
		/// <param name="str2">A string.</param>
		/// <returns>
		/// Returns -1 if <paramref name="str1"/> is less than <paramref name="str2"/>; +1 if <paramref name="str1"/> is greater than <paramref name="str2"/>,
		/// and 0 if they are equal.
		/// </returns>
		[ImplementsFunction("strcoll")]
		public static int StringCollate(string str1, string str2)
		{
			return String.Compare(str1, str2, false, GetCulture(Category.Collate));
		}

		/// <summary>
		/// Not supported.
		/// </summary>
		[ImplementsFunction("nl_langinfo", FunctionImplOptions.NotSupported)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static string nl_langinfo(int item)
		{
			PhpException.FunctionNotSupported();
			return null;
		}

#endif
        #endregion
    }
}
