/*

 Copyright (c) 2012 Jakub Misek

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/
using System;
using PHP.Core;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.ComponentModel;

#if SILVERLIGHT
using PHP.CoreCLR;
using System.Windows.Browser;
#else
using System.Web;
#endif

namespace PHP.Library
{
    #region Constants

    public enum FilterInput : int
    {
        [ImplementsConstant("INPUT_POST")]
        Post = 0,
        [ImplementsConstant("INPUT_GET")]
        Get = 1,
        [ImplementsConstant("INPUT_COOKIE")]
        Cookie = 2,
        [ImplementsConstant("INPUT_ENV")]
        Env = 4,
        [ImplementsConstant("INPUT_SERVER")]
        Server = 5,
        [ImplementsConstant("INPUT_SESSION")]
        Session = 6,
        [ImplementsConstant("INPUT_REQUEST")]
        Request = 99
    }

    /// <summary>
    /// Other filter ids.
    /// </summary>
    public enum FilterIds : int
    {
        /// <summary>
        /// Flag used to require scalar as input
        /// </summary>
        [ImplementsConstant("FILTER_REQUIRE_SCALAR")]
        FILTER_REQUIRE_SCALAR = 33554432,

        /// <summary>
        /// Require an array as input.
        /// </summary>
        [ImplementsConstant("FILTER_REQUIRE_ARRAY")]
        FILTER_REQUIRE_ARRAY = 16777216,

        /// <summary>
        /// Always returns an array.
        /// </summary>
        [ImplementsConstant("FILTER_FORCE_ARRAY")]
        FILTER_FORCE_ARRAY = 67108864,

        /// <summary>
        /// Use NULL instead of FALSE on failure.
        /// </summary>
        [ImplementsConstant("FILTER_NULL_ON_FAILURE")]
        FILTER_NULL_ON_FAILURE = 134217728,

        /// <summary>
        /// ID of "callback" filter.
        /// </summary>
        [ImplementsConstant("FILTER_CALLBACK")]
        FILTER_CALLBACK = 1024,
    }

    /// <summary>
    /// Validation filters.
    /// </summary>
    public enum FilterValidate : int
    {
        /// <summary>
        /// ID of "int" filter.
        /// </summary>
        [ImplementsConstant("FILTER_VALIDATE_INT")]
        INT = 257,

        /// <summary>
        /// ID of "boolean" filter.
        /// </summary>
        [ImplementsConstant("FILTER_VALIDATE_BOOLEAN")]
        BOOLEAN = 258,

        /// <summary>
        /// ID of "float" filter.
        /// </summary>
        [ImplementsConstant("FILTER_VALIDATE_FLOAT")]
        FLOAT = 259,

        /// <summary>
        /// ID of "validate_regexp" filter.
        /// </summary>
        [ImplementsConstant("FILTER_VALIDATE_REGEXP")]
        REGEXP = 272,

        /// <summary>
        /// ID of "validate_url" filter.
        /// </summary>
        [ImplementsConstant("FILTER_VALIDATE_URL")]
        URL = 273,

        /// <summary>
        /// ID of "validate_email" filter.
        /// </summary>
        [ImplementsConstant("FILTER_VALIDATE_EMAIL")]
        EMAIL = 274,

        /// <summary>
        /// ID of "validate_ip" filter.
        /// </summary>
        [ImplementsConstant("FILTER_VALIDATE_IP")]
        IP = 275,
    }

    /// <summary>
    /// Sanitize filters.
    /// </summary>
    public enum FilterSanitize : int
    {
        /// <summary>
        /// ID of "string" filter.
        /// </summary>
        [ImplementsConstant("FILTER_SANITIZE_STRING")]
        STRING = 513,

        /// <summary>
        /// ID of "stripped" filter.
        /// </summary>
        [ImplementsConstant("FILTER_SANITIZE_STRIPPED")]
        STRIPPED = STRING,   // alias of FILTER_SANITIZE_STRING

        /// <summary>
        /// ID of "encoded" filter.
        /// </summary>
        [ImplementsConstant("FILTER_SANITIZE_ENCODED")]
        ENCODED = 514,

        /// <summary>
        /// ID of "special_chars" filter.
        /// </summary>
        [ImplementsConstant("FILTER_SANITIZE_SPECIAL_CHARS")]
        SPECIAL_CHARS = 515,

        /// <summary>
        /// ID of "unsafe_raw" filter.
        /// </summary>
        [ImplementsConstant("FILTER_UNSAFE_RAW")]
        FILTER_UNSAFE_RAW = 516,

        /// <summary>
        /// ID of default ("string") filter.
        /// </summary>
        [ImplementsConstant("FILTER_DEFAULT")]
        FILTER_DEFAULT = FILTER_UNSAFE_RAW, // alias of FILTER_UNSAFE_RAW
        
        /// <summary>
        /// ID of "email" filter.
        /// Remove all characters except letters, digits and !#$%&amp;'*+-/=?^_`{|}~@.[].
        /// </summary>
        [ImplementsConstant("FILTER_SANITIZE_EMAIL")]
        EMAIL = 517,

        /// <summary>
        /// ID of "url" filter.
        /// </summary>
        [ImplementsConstant("FILTER_SANITIZE_URL")]
        URL = 518,

        /// <summary>
        /// ID of "number_int" filter.
        /// </summary>
        [ImplementsConstant("FILTER_SANITIZE_NUMBER_INT")]
        NUMBER_INT = 519,

        /// <summary>
        /// ID of "number_float" filter.
        /// </summary>
        [ImplementsConstant("FILTER_SANITIZE_NUMBER_FLOAT")]
        NUMBER_FLOAT = 520,

        /// <summary>
        /// ID of "magic_quotes" filter.
        /// </summary>
        [ImplementsConstant("FILTER_SANITIZE_MAGIC_QUOTES")]
        MAGIC_QUOTES = 521,
    }

    [Flags]
    public enum FilterFlag : int
    {
        /// <summary>
        /// No flags.
        /// </summary>
        [ImplementsConstant("FILTER_FLAG_NONE")]
        NONE = 0,

        /// <summary>
        /// Allow octal notation (0[0-7]+) in "int" filter.
        /// </summary>
        [ImplementsConstant("FILTER_FLAG_ALLOW_OCTAL")]
        ALLOW_OCTAL = 1,

        /// <summary>
        /// Allow hex notation (0x[0-9a-fA-F]+) in "int" filter.
        /// </summary>
        [ImplementsConstant("FILTER_FLAG_ALLOW_HEX")]
        ALLOW_HEX = 2,

        /// <summary>
        /// Strip characters with ASCII value less than 32.
        /// </summary>
        [ImplementsConstant("FILTER_FLAG_STRIP_LOW")]
        STRIP_LOW = 4,

        /// <summary>
        /// Strip characters with ASCII value greater than 127.
        /// </summary>
        [ImplementsConstant("FILTER_FLAG_STRIP_HIGH")]
        STRIP_HIGH = 8,

        /// <summary>
        /// Encode characters with ASCII value less than 32.
        /// </summary>
        [ImplementsConstant("FILTER_FLAG_ENCODE_LOW")]
        ENCODE_LOW = 16,

        /// <summary>
        /// Encode characters with ASCII value greater than 127.
        /// </summary>
        [ImplementsConstant("FILTER_FLAG_ENCODE_HIGH")]
        ENCODE_HIGH = 32,

        /// <summary>
        /// Encode &amp;.
        /// </summary>
        [ImplementsConstant("FILTER_FLAG_ENCODE_AMP")]
        ENCODE_AMP = 64,

        /// <summary>
        /// Don't encode ' and ".
        /// </summary>
        [ImplementsConstant("FILTER_FLAG_NO_ENCODE_QUOTES")]
        NO_ENCODE_QUOTES = 128,

        /// <summary>
        /// ?
        /// </summary>
        [ImplementsConstant("FILTER_FLAG_EMPTY_STRING_NULL")]
        EMPTY_STRING_NULL = 256,

        /// <summary>
        /// Allow fractional part in "number_float" filter.
        /// </summary>
        [ImplementsConstant("FILTER_FLAG_ALLOW_FRACTION")]
        ALLOW_FRACTION = 4096,

        /// <summary>
        /// Allow thousand separator (,) in "number_float" filter.
        /// </summary>
        [ImplementsConstant("FILTER_FLAG_ALLOW_THOUSAND")]
        ALLOW_THOUSAND = 8192,

        /// <summary>
        /// Allow scientific notation (e, E) in "number_float" filter.
        /// </summary>
        [ImplementsConstant("FILTER_FLAG_ALLOW_SCIENTIFIC")]
        ALLOW_SCIENTIFIC = 16384,

        /// <summary>
        /// Require path in "validate_url" filter.
        /// </summary>
        [ImplementsConstant("FILTER_FLAG_PATH_REQUIRED")]
        PATH_REQUIRED = 262144,

        /// <summary>
        /// Require query in "validate_url" filter.
        /// </summary>
        [ImplementsConstant("FILTER_FLAG_QUERY_REQUIRED")]
        QUERY_REQUIRED = 524288,

        /// <summary>
        /// Allow only IPv4 address in "validate_ip" filter.
        /// </summary>
        [ImplementsConstant("FILTER_FLAG_IPV4")]
        IPV4 = 1048576,

        /// <summary>
        /// Allow only IPv6 address in "validate_ip" filter.
        /// </summary>
        [ImplementsConstant("FILTER_FLAG_IPV6")]
        IPV6 = 2097152,

        /// <summary>
        /// Deny reserved addresses in "validate_ip" filter.
        /// </summary>
        [ImplementsConstant("FILTER_FLAG_NO_RES_RANGE")]
        NO_RES_RANGE = 4194304,

        /// <summary>
        /// Deny private addresses in "validate_ip" filter.
        /// </summary>
        [ImplementsConstant("FILTER_FLAG_NO_PRIV_RANGE")]
        NO_PRIV_RANGE = 8388608
    }

    #endregion

    [ImplementsExtension("filter")]
    public static class PhpFiltering
    {
        #region (NS) filter_input_array, filter_var_array, filter_id, filter_list
        
        [ImplementsFunction("filter_input_array", FunctionImplOptions.NotSupported)]
        public static object filter_input_array(int type)
        {
            return filter_input_array(type, null);
        }

        /// <summary>
        /// Gets external variables and optionally filters them.
        /// </summary>
        [ImplementsFunction("filter_input_array", FunctionImplOptions.NotSupported)]
        public static object filter_input_array(int type, object definition)
        {
            return false;
        }

        /// <summary>
        /// Returns the filter ID belonging to a named filter.
        /// </summary>
        [ImplementsFunction("filter_id", FunctionImplOptions.NotSupported)]
        [return: CastToFalse]
        public static int filter_id(string filtername)
        {
            return -1;
        }

        /// <summary>
        /// Returns a list of all supported filters.
        /// </summary>
        [ImplementsFunction("filter_list", FunctionImplOptions.NotSupported)]
        public static PhpArray/*!*/filter_list()
        {
            return new PhpArray();
        }

        [ImplementsFunction("filter_var_array", FunctionImplOptions.NotSupported)]
        public static object filter_var_array(PhpArray data)
        {
            return filter_var_array(data, null);
        }

        /// <summary>
        /// Gets multiple variables and optionally filters them.
        /// </summary>
        /// <returns></returns>
        [ImplementsFunction("filter_var_array", FunctionImplOptions.NotSupported)]
        public static object filter_var_array(PhpArray data, object definition)
        {
            return null;
        }

        #endregion

        #region filter_input

        [ImplementsFunction("filter_input")]
        public static object filter_input(ScriptContext/*!*/context, FilterInput type, string variable_name)
        {
            return filter_input(context, type, variable_name, (int)FilterSanitize.FILTER_DEFAULT, null);
        }

        [ImplementsFunction("filter_input")]
        public static object filter_input(ScriptContext/*!*/context, FilterInput type, string variable_name, int filter)
        {
            return filter_input(context, type, variable_name, filter, null);
        }

        /// <summary>
        /// Gets a specific external variable by name and optionally filters it.
        /// </summary>
        [ImplementsFunction("filter_input")]
        public static object filter_input(ScriptContext/*!*/context, FilterInput type, string variable_name, int filter /*= FILTER_DEFAULT*/ , object options)
        {
            var arrayobj = GetArrayByInput(context, type);
            object value;
            if (arrayobj == null || !arrayobj.TryGetValue(variable_name, out value))
                return null;

            return filter_var(value, filter, options);
        }

        #endregion

        #region filter_var, filter_has_var

        /// <summary>
        /// Checks if variable of specified type exists
        /// </summary>
        [ImplementsFunction("filter_has_var")]
        public static bool filter_has_var(ScriptContext/*!*/context, FilterInput type, string variable_name)
        {
            var arrayobj = GetArrayByInput(context, type);
            if (arrayobj != null)
                return arrayobj.ContainsKey(variable_name);
            else
                return false;
        }

        /// <summary>
        /// Returns <see cref="PhpArray"/> containing required input.
        /// </summary>
        /// <param name="context">CUrrent <see cref="ScriptContext"/>.</param>
        /// <param name="type"><see cref="FilterInput"/> value.</param>
        /// <returns>An instance of <see cref="PhpArray"/> or <c>null</c> if there is no such input.</returns>
        private static PhpArray GetArrayByInput(ScriptContext/*!*/context, FilterInput type)
        {
            object arrayobj = null;

            switch (type)
            {
                case FilterInput.Get:
                    arrayobj = context.AutoGlobals.Get.Value; break;
                case FilterInput.Post:
                    arrayobj = context.AutoGlobals.Post.Value; break;
                case FilterInput.Server:
                    arrayobj = context.AutoGlobals.Server.Value; break;
                case FilterInput.Request:
                    arrayobj = context.AutoGlobals.Request.Value; break;
                case FilterInput.Env:
                    arrayobj = context.AutoGlobals.Env.Value; break;
                case FilterInput.Cookie:
                    arrayobj = context.AutoGlobals.Cookie.Value; break;
                case FilterInput.Session:
                    arrayobj = context.AutoGlobals.Session.Value; break;
                default:
                    return null;
            }

            // cast arrayobj to PhpArray if possible:
            return PhpArray.AsPhpArray(arrayobj);
        }

        [ImplementsFunction("filter_var")]
        public static object filter_var(object variable)
        {
            return filter_var(variable, (int)FilterSanitize.FILTER_DEFAULT, null);
        }

        [ImplementsFunction("filter_var")]
        public static object filter_var(object variable, int filter)
        {
            return filter_var(variable, filter, null);
        }        
        
        /// <summary>
        /// Filters a variable with a specified filter.
        /// </summary>
        /// <param name="variable">Value to filter.</param>
        /// <param name="filter">The ID of the filter to apply.</param>
        /// <param name="options">Associative array of options or bitwise disjunction of flags. If filter accepts options, flags can be provided in "flags" field of array. For the "callback" filter, callback type should be passed. The callback must accept one argument, the value to be filtered, and return the value after filtering/sanitizing it.</param>
        /// <returns>Returns the filtered data, or <c>false</c> if the filter fails.</returns>
        [ImplementsFunction("filter_var")]
        public static object filter_var(object variable, int filter /*= FILTER_DEFAULT*/ , object options)
        {
            switch (filter)
            {
                //
                // SANITIZE
                //

                case (int)FilterSanitize.FILTER_DEFAULT:
                    return Core.Convert.ObjectToString(variable);

                case (int)FilterSanitize.EMAIL:
                    // Remove all characters except letters, digits and !#$%&'*+-/=?^_`{|}~@.[].
                    return FilterSanitizeString(PHP.Core.Convert.ObjectToString(variable), (c) =>
                            (int)c <= 0x7f && (Char.IsLetterOrDigit(c) ||
                            c == '!' || c == '#' || c == '$' || c == '%' || c == '&' || c == '\'' ||
                            c == '*' || c == '+' || c == '-' || c == '/' || c == '=' || c == '!' ||
                            c == '?' || c == '^' || c == '_' || c == '`' || c == '{' || c == '|' ||
                            c == '}' || c == '~' || c == '@' || c == '.' || c == '[' || c == ']'));

                //
                // VALIDATE
                //

                case (int)FilterValidate.EMAIL:
                    {
                        var str = PHP.Core.Convert.ObjectToString(variable);
                        return RegexUtilities.IsValidEmail(str) ? str : (object)false;
                    }

                case (int)FilterValidate.INT:
                    {
                        int result;
                        if (int.TryParse((PhpVariable.AsString(variable) ?? string.Empty).Trim(), out result))
                        {
                            if (options != null) PhpException.ArgumentValueNotSupported("options", "!null");
                            return result;  // TODO: options: min_range, max_range
                        }
                        else
                            return false;
                    }
                case (int)FilterValidate.REGEXP:
                    {
                        PhpArray optarray;
                        // options = options['options']['regexp']
                        if ((optarray = PhpArray.AsPhpArray(options)) != null &&
                            optarray.TryGetValue("options", out options) && (optarray = PhpArray.AsPhpArray(options)) != null &&
                            optarray.TryGetValue("regexp", out options))
                        {
                            if (PerlRegExp.Match(options, variable) > 0)
                                return variable;
                        }
                        else
                            PhpException.InvalidArgument("options", LibResources.GetString("option_missing", "regexp"));
                        return false;
                    }

                default:
                    PhpException.ArgumentValueNotSupported("filter", filter);
                    break;
            }

            return false;
        }

        #endregion

        #region Helper filter methods

        private static class RegexUtilities
        {
            private static readonly Regex ValidEmailRegex = new Regex(
                    @"^(?("")(""[^""]+?""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                    @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9]{2,17}))$",
                    RegexOptions.IgnoreCase | RegexOptions.Compiled);

            public static bool IsValidEmail(string strIn)
            {
                if (String.IsNullOrEmpty(strIn) || strIn.Length > 320)
                    return false;

                // Use IdnMapping class to convert Unicode domain names.
                try
                {
                    strIn = Regex.Replace(strIn, @"(@)(.+)$", DomainMapper);
                }
                catch (ArgumentException)
                {
                    return false;
                }

                // Return true if strIn is in valid e-mail format.
                return ValidEmailRegex.IsMatch(strIn);
            }

            private static string DomainMapper(Match match)
            {
                // IdnMapping class with default property values.
                var idn = new System.Globalization.IdnMapping();

                string domainName = match.Groups[2].Value;
                //try
                //{
                    domainName = idn.GetAscii(domainName);
                //}
                //catch (ArgumentException)
                //{
                //    invalid = true;
                //}

                return match.Groups[1].Value + domainName;
            }
        }


        /// <summary>
        /// Remove all characters not valid by given <paramref name="predicate"/>.
        /// </summary>
        private static string FilterSanitizeString(string str, Predicate<char>/*!*/predicate)
        {
            Debug.Assert(predicate != null);

            // nothing to sanitize:
            if (string.IsNullOrEmpty(str)) return string.Empty;

            // check if all the characters are valid first:
            bool allvalid = true;
            foreach (var c in str)
                if (!predicate(c))
                {
                    allvalid = false;
                    break;
                }

            if (allvalid)
            {
                return str;
            }
            else
            {
                // remove not allowed characters:
                StringBuilder newstr = new StringBuilder(str.Length);

                foreach (char c in str)
                    if (predicate(c))
                        newstr.Append(c);

                return newstr.ToString();
            }
        }

        #endregion
    }
}
