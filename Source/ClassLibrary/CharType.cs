/*

 Copyright (c) 2005-2006 Tomas Matousek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Text;
using System.Globalization;
using PHP.Core;

namespace PHP.Library
{
	/// <summary>
	/// Implements character type functions known as <c>ctype</c>.
	/// </summary>
	[ImplementsExtension(LibraryDescriptor.ExtCType)]
	public static class CharType
	{
		private static string ArgToString(object var)
		{
			string s = var as string;
			if (s == null)
			{
				if (var is int)
					s = PhpStrings.ChrUnicode((int)var);
			}
			return s;
		}

        private static bool IsValid(object var, Predicate<char>/*!*/isValid)
        {
            string s = ArgToString(var);
            if (s == null) return false;

            foreach (char c in s)
                if (!isValid(c))
                    return false;
            
            return true;
        }

		/// <summary>
		/// Check for alphanumeric character(s).
		/// </summary>
		[ImplementsFunction("ctype_alnum")]
        [PureFunction]
        public static bool IsAlphanumeric(object var)
		{
            return IsValid(var, Char.IsLetterOrDigit);
		}

		/// <summary>
		/// Check for alphabetic character(s). 
		/// </summary>
		[ImplementsFunction("ctype_alpha")]
        [PureFunction]
        public static bool IsAlpha(object var)
		{
            return IsValid(var, Char.IsLetter);
		}

		/// <summary>
		/// Check for control character(s). 
		/// </summary>
		[ImplementsFunction("ctype_cntrl")]
        [PureFunction]
        public static bool IsControl(object var)
		{
            return IsValid(var, Char.IsControl);
		}

		/// <summary>
		/// Check for numeric character(s).
		/// </summary>
		[ImplementsFunction("ctype_digit")]
        [PureFunction]
        public static bool IsDigit(object var)
		{
            return IsValid(var, Char.IsDigit);
		}

		/// <summary>
		/// Check for lowercase character(s). 
		/// </summary>
		[ImplementsFunction("ctype_lower")]
        [PureFunction]
        public static bool IsLower(object var)
		{
            return IsValid(var, Char.IsLower);
		}

		/// <summary>
		/// Check for any printable character which is not whitespace or an alphanumeric character.
		/// </summary>
		[ImplementsFunction("ctype_punct")]
        [PureFunction]
        public static bool IsPunctuation(object var)
		{
            return IsValid(var, Char.IsPunctuation);
		}

		/// <summary>
		/// Check for whitespace character(s).
		/// </summary>
		[ImplementsFunction("ctype_space")]
        [PureFunction]
        public static bool IsWhiteSpace(object var)
		{
            return IsValid(var, Char.IsWhiteSpace);
		}

		/// <summary>
		/// Check for uppercase character(s).
		/// </summary>
		[ImplementsFunction("ctype_upper")]
        [PureFunction]
        public static bool IsUpper(object var)
		{
            return IsValid(var, Char.IsUpper);
		}

		/// <summary>
		/// Check for character(s) representing a hexadecimal digit. 
		/// </summary>
		[ImplementsFunction("ctype_xdigit")]
        [PureFunction]
        public static bool IsHexadigit(object var)
		{
            return IsValid(var, c => (c >= '0' && c <= '9' || c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F'));
		}

		/// <summary>
		/// Check for any printable character(s) except space. Those are alpha-numeric characters and punctuations.
		/// </summary>
		[ImplementsFunction("ctype_graph")]
        [PureFunction]
        public static bool IsGraph(object var)
		{
            return IsValid(var, c => (Char.IsPunctuation(c) || Char.IsLetterOrDigit(c)));
		}

		/// <summary>
		/// Check for printable character(s). Those are alpha-numeric characters, punctuations, and space character.
		/// </summary>
		[ImplementsFunction("ctype_print")]
        [PureFunction]
        public static bool IsPrintable(object var)
		{
            return IsValid(var, c => (c == ' ' || Char.IsPunctuation(c) || Char.IsLetterOrDigit(c)));
		}
	}
}
