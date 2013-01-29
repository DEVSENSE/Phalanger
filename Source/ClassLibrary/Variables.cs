/*

 Copyright (c) 2004-2006 Tomas Matousek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.ComponentModel;
using System.Runtime.Serialization;
using PHP.Core;
using System.Collections.Generic;

#if SILVERLIGHT
using PHP.CoreCLR;
#else
using System.Web;
using System.Runtime.Serialization.Formatters.Binary;
#endif

namespace PHP.Library
{
	#region Enumerations

	/// <summary>
	/// Type of extraction <see cref="PhpVariables.Extract(Dictionary{string,object},PhpArray,ExtractType,string)"/>.
	/// </summary>
	[Flags]
	public enum ExtractType
	{
		/// <summary>PHP constant: EXTR_OVERWRITE</summary>
		[ImplementsConstant("EXTR_OVERWRITE")]
		Overwrite,

		/// <summary>PHP constant: EXTR_SKIP</summary>
		[ImplementsConstant("EXTR_SKIP")]
		Skip,

		/// <summary>PHP constant: EXTR_PREFIX_SAME</summary>
		[ImplementsConstant("EXTR_PREFIX_SAME")]
		PrefixSame,

		/// <summary>PHP constant: EXTR_PREFIX_ALL</summary>
		[ImplementsConstant("EXTR_PREFIX_ALL")]
		PrefixAll,

		/// <summary>PHP constant: EXTR_PREFIX_INVALID</summary>
		[ImplementsConstant("EXTR_PREFIX_INVALID")]
		PrefixInvalid,

		/// <summary>PHP constant: EXTR_PREFIX_IF_EXISTS</summary>
		[ImplementsConstant("EXTR_PREFIX_IF_EXISTS")]
		PrefixIfExists,

		/// <summary>PHP constant: EXTR_IF_EXISTS</summary>
		[ImplementsConstant("EXTR_IF_EXISTS")]
		IfExists,

		/// <summary>PHP constant: EXTR_REFS</summary>
		[ImplementsConstant("EXTR_REFS")]
		Refs = 256,

		/// <summary>A value masking all options but <see cref="Refs"/> (0xff).</summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		NonFlags = 0xff
	}

	/// <summary>
	/// File upload errors.
	/// </summary>
    [ImplementsExtension(LibraryDescriptor.ExtCore)]
	public enum FileUploadError
	{
		/// <summary>
		/// No error.
		/// </summary>
		[ImplementsConstant("UPLOAD_ERR_OK")]
		None,

		/// <summary>
		/// The uploaded file exceeds the "upload_max_filesize" configuration option.
		/// </summary>
		[ImplementsConstant("UPLOAD_ERR_INI_SIZE")]
		SizeExceededOnServer,

		/// <summary>
		/// The uploaded file exceeds the "MAX_FILE_SIZE" value specified in the form.
		/// </summary>
		[ImplementsConstant("UPLOAD_ERR_FORM_SIZE")]
		SizeExceededOnClient,

		/// <summary>
		/// The uploaded file was only partially uploaded.
		/// </summary>
		[ImplementsConstant("UPLOAD_ERR_PARTIAL")]
		Partial,

		/// <summary>
		/// No file was uploaded.
		/// </summary>
		[ImplementsConstant("UPLOAD_ERR_NO_FILE")]
		NoFile,

        /// <summary>
        /// Missing a temporary folder
        /// </summary>
        [ImplementsConstant("UPLOAD_ERR_NO_TMP_DIR")]
        NoTempDirectory,

        /// <summary>
        /// Missing a temporary folder
        /// </summary>
        [ImplementsConstant("UPLOAD_ERR_CANT_WRITE")]
        CantWrite,

        /// <summary>
        /// A PHP extension stopped the file upload
        /// </summary>
        [ImplementsConstant("UPLOAD_ERR_EXTENSION")]
        ErrorExtension
	}

	#endregion

	/// <summary>
	/// Provides means for PHP variables handling.
	/// </summary>
	/// <threadsafety static="true"/>
	public static class PhpVariables
	{
		#region Constants

		/// <summary>
		/// Recursive counting.
		/// </summary>
		[ImplementsConstant("COUNT_RECURSIVE")]
		public const int CountRecursive = 1;

		/// <summary>
		/// Non-recursive counting.
		/// </summary>
		[ImplementsConstant("COUNT_NORMAL")]
		public const int CountNormal = 0;

		#endregion

		#region count, sizeof

		/// <summary>
		/// Counts items in a variable.
		/// </summary>
		/// <param name="variable">The variable which items to count.</param>
		/// <returns>The number of items.</returns>
		/// <remarks>The same as <see cref="Count"/>.</remarks>
		[ImplementsFunction("sizeof")]
		public static int SizeOf(object variable)
		{
            return Count(variable, CountNormal);
		}
        
        /// <summary>
        /// Counts items in a variable.
        /// </summary>
        /// <param name="variable">The variable which items to count.</param>
        /// <param name="mode">Whether to count recursively.</param>
        /// <returns>The number of items in all arrays contained recursivelly in <paramref name="variable"/>.</returns>
        /// <remarks>If any item of the <paramref name="variable"/> contains infinite recursion 
        /// skips items that are repeating because of such recursion.
        /// </remarks>
        [ImplementsFunction("sizeof")]
        public static int SizeOf(object variable, int mode)
        {
            return Count(variable, mode);
        }

		/// <summary>
		/// Counts items in a variable.
		/// </summary>
		/// <param name="variable">The variable which items to count.</param>
		/// <returns>The number of items.</returns>
		[ImplementsFunction("count")]
		public static int Count(object variable)
		{
            return Count(variable, CountNormal);
		}

		/// <summary>
		/// Counts items in a variable.
		/// </summary>
		/// <param name="variable">The variable which items to count.</param>
		/// <param name="mode">Whether to count recursively.</param>
		/// <returns>The number of items in all arrays contained recursivelly in <paramref name="variable"/>.</returns>
		/// <remarks>If any item of the <paramref name="variable"/> contains infinite recursion 
		/// skips items that are repeating because of such recursion.
		/// </remarks>
		[ImplementsFunction("count")]
		public static int Count(object variable, int mode)
		{
			// null or uninitialized variable:
			if (variable == null) return 0;

            //
			// hashtable
            //
			PhpHashtable ht;
            if ((ht = variable as PhpHashtable) != null)
            {
                // non recursive count:
                if (mode != CountRecursive)
                    return ht.Count;

                // recursive count:
                int result = 0;
                using (PhpHashtable.RecursiveEnumerator iterator = ht.GetRecursiveEnumerator(true,true))
                {
                    while (iterator.MoveNext())
                        result++;
                }

                return result;
            }

            //
            // SPL.Countable
            // recursive count not supported (not even in PHP)
            //
            SPL.Countable countable;
            if ((countable = variable as SPL.Countable) != null)
            {
                object cnt = countable.count(ScriptContext.CurrentContext);
                return (cnt != null) ? PHP.Core.Convert.ObjectToInteger(cnt) : 0;
            }

            PHP.Core.Reflection.DObject dobj;
            if ((dobj = variable as PHP.Core.Reflection.DObject) != null)
            {
                if (dobj.RealObject is SPL.Countable)
                {
                    object cnt = dobj.InvokeMethod("count", null, ScriptContext.CurrentContext);
                    return (cnt != null) ? PHP.Core.Convert.ObjectToInteger(cnt) : 0;
                }
                else if (dobj.RealObject is ICollection)
                {
                    return ((ICollection)dobj.RealObject).Count;
                }
            }            

            // count not supported
            return 1;
		}

		#endregion

		#region doubleval, floatval, intval, strval, settype, gettype, boolval

		/// <summary>
		/// Converts to double.
		/// </summary>
		/// <param name="variable">The variable.</param>
		/// <returns>The converted value.</returns>
		[ImplementsFunction("doubleval")]
        [PureFunction]
		public static double DoubleVal(object variable)
		{
			return PHP.Core.Convert.ObjectToDouble(variable);
		}

		/// <summary>
		/// Converts to double.
		/// </summary>
		/// <param name="variable">The variable.</param>
		/// <returns>The converted value.</returns>
		[ImplementsFunction("floatval")]
        [PureFunction]
        public static double FloatVal(object variable)
		{
			return PHP.Core.Convert.ObjectToDouble(variable);
		}

		/// <summary>
		/// Converts to integer.
		/// </summary>
		/// <param name="variable">The variable.</param>
		/// <returns>The converted value.</returns>
		[ImplementsFunction("intval")]
        [PureFunction]
        public static int IntVal(object variable)
		{
			return PHP.Core.Convert.ObjectToInteger(variable);
		}

		/// <summary>
		/// Converts to integer using a specified base.
		/// </summary>
		/// <param name="variable">The variable.</param>
		/// <param name="base">The base.</param>
		/// <returns>The converted value.</returns>
		[ImplementsFunction("intval")]
        [PureFunction]
        public static int IntVal(object variable, int @base)
		{
			// TODO: base
			// The integer value of var on success, or 0 on failure. Empty arrays and objects return 0, non-empty arrays and objects return 1. 
			// The maximum value depends on the system. 32 bit systems have a maximum signed integer range of -2147483648 to 2147483647. So for example on such a system, intval('1000000000000') will return 2147483647. The maximum signed integer value for 64 bit systems is 9223372036854775807. 
			return PHP.Core.Convert.ObjectToInteger(variable);
		}

		/// <summary>
		/// Converts to string.
		/// </summary>
		/// <param name="variable">The variable.</param>
		/// <returns>The converted value.</returns>
		[ImplementsFunction("strval")]
        [PureFunction]
        public static string StrVal(object variable)
		{
			return PHP.Core.Convert.ObjectToString(variable);
		}

        /// <summary>
        /// Converts to boolean.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>The converted value.</returns>
        [ImplementsFunction("boolval")]
        [PureFunction]
        public static bool BoolVal(object variable)
        {
            return PHP.Core.Convert.ObjectToBoolean(variable);
        }

		/// <summary>
		/// Sets variable's type.
		/// </summary>
		/// <param name="variable">The variable.</param>
		/// <param name="type">The string identifying a new type. See PHP manual for details.</param>
		/// <returns>Whether <paramref name="type"/> is valid type identifier.</returns>
		/// <exception cref="PhpException"><paramref name="type"/> has invalid value.</exception>
		[ImplementsFunction("settype")]
		public static bool SetType(ref object variable, string type)
		{
            switch (System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToLower(type)) // we don't need Unicode characters to be lowercased properly // CurrentCulture is slow
			{
				case "bool":
				case "boolean":
					variable = PHP.Core.Convert.ObjectToBoolean(variable);
					return true;

				case "int":
				case "integer":
					variable = PHP.Core.Convert.ObjectToInteger(variable);
					return true;

				case "float":
				case "double":
					variable = PHP.Core.Convert.ObjectToDouble(variable);
					return true;

				case "string":
					variable = PHP.Core.Convert.ObjectToString(variable);
					return true;

				case "array":
					variable = PHP.Core.Convert.ObjectToPhpArray(variable);
					return true;

				case "object":
					variable = PHP.Core.Convert.ObjectToDObject(variable, ScriptContext.CurrentContext);
					return true;

				case "null":
					variable = null;
					return true;

				default:
					PhpException.InvalidArgument("type", LibResources.GetString("invalid_type_name"));
					return false;
			}
		}

		/// <summary>
		/// Retrieves name of a variable type.
		/// </summary>
		/// <param name="variable">The variable.</param>
		/// <returns>The string type identifier. See PHP manual for details.</returns>
		[ImplementsFunction("gettype")]
        [PureFunction]
        public static string GetType(object variable)
		{
			// works well on references:
			return PhpVariable.GetTypeName(variable);
		}

		#endregion

		#region is_<type>

		/// <summary>
		/// Checks whether a dereferenced variable is integer.
		/// </summary>
		/// <param name="variable">The variable.</param>
		/// <returns>Whether <paramref name="variable"/> is integer.</returns>
		[ImplementsFunction("is_int")]
        [PureFunction]
        public static bool IsInt(object variable)
		{
            return variable is int;
		}

		/// <summary>
		/// Checks whether a dereferenced variable is integer.
        /// Alias for is_int().
        /// </summary>
		/// <param name="variable">The variable.</param>
		/// <returns>Whether <paramref name="variable"/> is integer.</returns>
		[ImplementsFunction("is_integer")]
        [PureFunction]
        public static bool IsInteger(object variable)
		{
            return variable is int;
		}

		/// <summary>
		/// Checks whether a dereferenced variable is long. 
        /// TODO: Alias for is_int(). But not in Phalanger.
		/// </summary>
		/// <param name="variable">The variable.</param>
		/// <returns>Whether <paramref name="variable"/> is long.</returns>
		[ImplementsFunction("is_long")]
        [PureFunction]
        public static bool IsLong(object variable)
		{
            return variable is long;//IsInt(variable);
		}

		/// <summary>
		/// Checks whether a dereferenced variable is boolean.
		/// </summary>
		/// <param name="variable">The variable.</param>
		/// <returns>Whether <paramref name="variable"/> is boolean.</returns>
		[ImplementsFunction("is_bool")]
        [PureFunction]
        public static bool IsBool(object variable)
		{
			return variable is bool;
		}

        /// <summary>
        /// Checks whether a dereferenced variable is double.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>Whether <paramref name="variable"/> is double.</returns>
        [ImplementsFunction("is_float")]
        [PureFunction]
        public static bool IsFloat(object variable)
        {
            return variable is double;
        }

		/// <summary>
		/// Checks whether a dereferenced variable is double.
        /// Alias for is_float().
        /// </summary>
		/// <param name="variable">The variable.</param>
		/// <returns>Whether <paramref name="variable"/> is double.</returns>
		[ImplementsFunction("is_double")]
        [PureFunction]
        public static bool IsDouble(object variable)
		{
            return variable is double;
		}

		/// <summary>
		/// Checks whether a dereferenced variable is double.
        /// Alias for is_float().
        /// </summary>
		/// <param name="variable">The variable.</param>
		/// <returns>Whether <paramref name="variable"/> is double.</returns>
		[ImplementsFunction("is_real")]
        [PureFunction]
        public static bool IsReal(object variable)
		{
            return variable is double;
		}

		/// <summary>
		/// Checks whether a dereferenced variable is string.
		/// </summary>
		/// <param name="variable">The variable.</param>
		/// <returns>Whether <paramref name="variable"/> is string.</returns>
		[ImplementsFunction("is_string")]
        [PureFunction]
        public static bool IsString(object variable)
		{
			return PhpVariable.IsString(variable);
		}

		/// <summary>
		/// Checks whether a dereferenced variable is an <see cref="PhpArray"/>.
		/// </summary>
		/// <param name="variable">The variable.</param>
		/// <returns>Whether <paramref name="variable"/> is <see cref="PhpArray"/>.</returns>
		[ImplementsFunction("is_array")]
        public static bool IsArray(object variable)
		{
			return variable is PhpArray;
		}

		/// <summary>
		/// Checks whether a dereferenced variable is <see cref="Core.Reflection.DObject"/>.
		/// </summary>
		/// <param name="variable">The variable.</param>
		/// <returns>Whether <paramref name="variable"/> is <see cref="Core.Reflection.DObject"/>.</returns>
		[ImplementsFunction("is_object")]
        public static bool IsObject(object variable)
		{
			return (variable is Core.Reflection.DObject && !(variable is __PHP_Incomplete_Class));
		}

		/// <summary>
		/// Checks whether a dereferenced variable is a valid <see cref="PhpResource"/>.
		/// </summary>
		/// <param name="variable">The variable.</param>
		/// <returns>Whether <paramref name="variable"/> is a valid <see cref="PhpResource"/>.</returns>
		[ImplementsFunction("is_resource")]
		public static bool IsResource(object variable)
		{
			PhpResource res = variable as PhpResource;
			return res != null && res.IsValid;
		}

		/// <summary>
		/// Checks whether a dereferenced variable is a <B>null</B> reference.
		/// </summary>
		/// <param name="variable">The variable.</param>
		/// <returns>Whether <paramref name="variable"/> is a <B>null</B> reference.</returns>
		[ImplementsFunction("is_null")]
        [PureFunction]
        public static bool IsNull(object variable)
		{
			return variable == null;
		}

		#endregion

		#region is_scalar, is_numeric, is_callable, get_resource_type

		/// <summary>
		/// Checks whether a dereferenced variable is a scalar.
		/// </summary>
		/// <param name="variable">The variable.</param>
		/// <returns>Whether <paramref name="variable"/> is an integer, a double, a bool or a string after dereferencing.</returns>
		[ImplementsFunction("is_scalar")]
        [PureFunction]
        public static bool IsScalar(object variable)
		{
			return PhpVariable.IsScalar(variable);
		}

		/// <summary>
		/// Checks whether a dereferenced variable is numeric.
		/// </summary>
		/// <param name="variable">The variable.</param>
		/// <returns>Whether <paramref name="variable"/> is integer, double or numeric string.
		/// <seealso cref="PHP.Core.Convert.StringToNumber"/></returns>
		[ImplementsFunction("is_numeric")]
        [PureFunction]
        public static bool IsNumeric(object variable)
		{
            if (variable == null)
                return false;

            // real numbers
            if (variable.GetType() == typeof(int) ||
                variable.GetType() == typeof(long) ||
                variable.GetType() == typeof(double))
                return true;

            // string            
            if (variable.GetType() == typeof(string))
                return IsNumericString((string)variable);
            
            if (variable.GetType() == typeof(PhpBytes))
                return IsNumericString(variable.ToString());

            // some .NET types:
            if (variable is Core.Reflection.IClrValue)
            {
                // decimal ?
            }

            // anything else:
            return false;
		}

        /// <summary>
        /// Checks whether given string can be converted to a number.
        /// </summary>
        /// <param name="str">A string value.</param>
        /// <returns><c>True</c> if the string represents a number. Otherwise <c>false</c>.</returns>
        private static bool IsNumericString(string str)
        {
            int ival;
            long lval;
            double dval;

            return (Core.Convert.StringToNumber(str, out ival, out lval, out dval) & Core.Convert.NumberInfo.IsNumber) != 0;
        }

		/// <summary>
		/// Verifies that the contents of a variable can be called as a function.
		/// </summary>
        /// <param name="caller">Current class context.</param>
		/// <param name="variable">The variable.</param>
		/// <returns><B>true</B> if <paramref name="variable"/> denotes a function, <B>false</B>
		/// otherwise.</returns>
		[ImplementsFunction("is_callable", FunctionImplOptions.NeedsClassContext)]
		public static bool IsCallable(PHP.Core.Reflection.DTypeDesc caller, object variable)
		{
			return IsCallable(caller, variable, false);
		}

		/// <summary>
		/// Verifies that the contents of a variable can be called as a function.
		/// </summary>
        /// <param name="caller">Current class context.</param>
        /// <param name="variable">The variable.</param>
		/// <param name="syntaxOnly">If <B>true</B>, it is only checked that has <pararef name="variable"/>
		/// a valid structure to be used as a callback. if <B>false</B>, the existence of the function (or
		/// method) is also verified.</param>
		/// <returns><B>true</B> if <paramref name="variable"/> denotes a function, <B>false</B>
		/// otherwise.</returns>
		[ImplementsFunction("is_callable", FunctionImplOptions.NeedsClassContext)]
		public static bool IsCallable(PHP.Core.Reflection.DTypeDesc caller, object variable, bool syntaxOnly)
		{
            return Operators.IsCallable(variable, caller, syntaxOnly);
		}

		/// <summary>
		/// Verifies that the contents of a variable can be called as a function.
		/// </summary>
        /// <param name="caller">Current class context.</param>
        /// <param name="variable">The variable.</param>
		/// <param name="syntaxOnly">If <B>true</B>, it is only checked that has <pararef name="variable"/>
		/// a valid structure to be used as a callback. if <B>false</B>, the existence of the function (or
		/// method) is also verified.</param>
		/// <param name="callableName">Receives the name of the function or method (for example
		/// <c>SomeClass::SomeMethod</c>).</param>
		/// <returns><B>true</B> if <paramref name="variable"/> denotes a function, <B>false</B>
		/// otherwise.</returns>
        [ImplementsFunction("is_callable", FunctionImplOptions.NeedsClassContext)]
		public static bool IsCallable(PHP.Core.Reflection.DTypeDesc caller, object variable, bool syntaxOnly, out string callableName)
		{
			PhpCallback callback = PHP.Core.Convert.ObjectToCallback(variable, true);
			if (callback == null || callback.IsInvalid)
			{
				callableName = PHP.Core.Convert.ObjectToString(variable);
				return false;
			}

			callableName = ((IPhpConvertible)callback).ToString();
			return (syntaxOnly ? true : callback.Bind(true, caller, null));
		}

		/// <summary>
		/// Returns the type of a resource.
		/// </summary>
		/// <param name="resource">The resource.</param>
		/// <returns>The resource type name or <c>null</c> if <paramref name="resource"/> is <c>null</c>.</returns>
		[ImplementsFunction("get_resource_type")]
		[return: CastToFalse]
		public static string GetResourceType(PhpResource resource)
		{
			return (resource != null ? resource.TypeName : null);
		}

		#endregion

		#region serialize, unserialize (CLR only)
#if !SILVERLIGHT

		/// <summary>
		/// Serializes a graph of connected objects to a byte array using the PHP serializer.
		/// </summary>
        /// <param name="caller">DTypeDesc of the caller's class context if it is known or UnknownTypeDesc if it should be determined lazily.</param>
        /// <param name="variable">The variable to serialize.</param>
		/// <returns>The serialized representation of the <paramref name="variable"/>.</returns>
		[ImplementsFunction("serialize", FunctionImplOptions.NeedsClassContext)]
		public static PhpBytes Serialize(PHP.Core.Reflection.DTypeDesc caller, object variable)
		{
            LibraryConfiguration config = LibraryConfiguration.GetLocal(ScriptContext.CurrentContext);

			return config.Serialization.DefaultSerializer.Serialize(variable, caller);
		}

		/// <summary>
		/// Deserializes a graph of connected object from a byte array using the PHP serializer.
		/// </summary>
        /// <param name="caller">DTypeDesc of the caller's class context if it is known or UnknownTypeDesc if it should be determined lazily.</param>
        /// <param name="bytes">The byte array to deserialize the graph from.</param>
		/// <returns>The deserialized object graph.</returns>
        [ImplementsFunction("unserialize", FunctionImplOptions.NeedsClassContext)]
        public static PhpReference Unserialize(PHP.Core.Reflection.DTypeDesc caller, PhpBytes bytes)
		{
            if (bytes == null || bytes.Length == 0)
                return new PhpReference(false);

            LibraryConfiguration config = LibraryConfiguration.GetLocal(ScriptContext.CurrentContext);

            return config.Serialization.DefaultSerializer.Deserialize(bytes, caller);
		}

#endif
		#endregion

        #region compact, extract

        /// <summary>
		/// Creates array containing variables and their values.
		/// </summary>
		/// <param name="localVariables">The table of defined variables.</param>
		/// <param name="names">Names of the variables - each chan be either 
		/// <see cref="string"/> or <see cref="PhpArray"/>. Names are retrived recursively from an array.</param>
		/// <returns>The <see cref="PhpArray"/> which keys are names of variables and values are deep copies of 
		/// their values.</returns>
		/// <remarks>
		/// Items in <paramref name="names"/> which are neither of type <see cref="string"/> nor <see cref="PhpArray"/> 
		/// are ignored.</remarks>
		/// <exception cref="PhpException"><paramref name="names"/> is a <B>null</B> reference.</exception>
		[ImplementsFunction("compact", FunctionImplOptions.NeedsVariables)]
		public static PhpArray Compact(Dictionary<string, object> localVariables, params object[] names)
		{
			if (names == null)
			{
				PhpException.ArgumentNull("names");
				return null;
			}

			PhpArray globals = (localVariables != null) ? null : ScriptContext.CurrentContext.GlobalVariables;
			PhpArray result = new PhpArray();

			for (int i = 0; i < names.Length; i++)
			{
				string name;
				PhpArray array;

				if ((name = PhpVariable.AsString(names[i])) != null)
				{
					// if variable exists adds a copy of its current value to the result:
					object value;

					if (PhpHashtable.TryGetValue(globals, localVariables, name, out value))
						result.Add(name, PhpVariable.DeepCopy(value));
				}
				else if ((array = names[i] as PhpArray) != null)
				{
					// recursively searches for string variable names:
					using (PhpHashtable.RecursiveEnumerator iterator = array.GetRecursiveEnumerator(false,true))
					{
						while (iterator.MoveNext())
						{
							if ((name = PhpVariable.AsString(iterator.Current.Value)) != null)
							{
								// if variable exists adds a copy of its current value to the result:
								object value;
								if (PhpHashtable.TryGetValue(globals, localVariables, name, out value))
									result.Add(name, PhpVariable.DeepCopy(value));
							}
						}
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Import variables into the current variables table from an array.
		/// </summary>
		/// <param name="definedVariables">The table of defined variables.</param>
		/// <param name="vars">The <see cref="PhpArray"/> containing names of variables and values to be assigned to them.</param>
		/// <returns>The number of variables actually affected by the extraction.</returns>
		/// <exception cref="PhpException"><paramref name="vars"/> or <paramref name="definedVariables"/> is a <B>null</B> reference.</exception>
		/// <exception cref="InvalidCastException">Some key of <paramref name="definedVariables"/> is not type of <see cref="string"/>.</exception>
		/// <remarks>The same as <see cref="Extract(Dictionary{string,object},PhpArray,ExtractType,string)"/> with empty prefix and 
		/// <see cref="ExtractType.Overwrite"/> option.
		/// </remarks>
		[ImplementsFunction("extract", FunctionImplOptions.NeedsVariables)]
		public static int Extract(Dictionary<string, object> definedVariables, PhpArray vars)
		{
			// GENERICS: IDictionary<string,object>

			return Extract(definedVariables, vars, ExtractType.Overwrite, null);
		}

		/// <summary>
		/// Import variables into the current variables table from an array.
		/// </summary>
		/// <param name="definedVariables">The table of defined variables.</param>
		/// <param name="vars">The <see cref="PhpArray"/> containing names of variables and values to be assigned to them.</param>
		/// <param name="type">The type of the extraction.</param>
		/// <returns>The number of variables actually affected by the extraction.</returns>
		/// <exception cref="PhpException"><paramref name="type"/> is invalid.</exception>
		/// <exception cref="PhpException"><paramref name="vars"/> or <paramref name="definedVariables"/> is a <B>null</B> reference.</exception>
		/// <exception cref="PhpException"><paramref name="type"/> compels presence of prefix (see <see cref="Extract(Dictionary{string,object},PhpArray,ExtractType,string)"/>).</exception>
		/// <exception cref="InvalidCastException">Some key of <paramref name="definedVariables"/> is not type of <see cref="string"/>.</exception>
		/// <remarks>See <see cref="Extract(Dictionary{string,object},PhpArray,ExtractType,string)"/> for details.</remarks>
		[ImplementsFunction("extract", FunctionImplOptions.NeedsVariables)]
		public static int Extract(Dictionary<string, object> definedVariables, PhpArray vars, ExtractType type)
		{
			// GENERICS: IDictionary<string,object>

			switch (type & ExtractType.NonFlags)
			{
				case ExtractType.PrefixSame:
				case ExtractType.PrefixAll:
				case ExtractType.PrefixInvalid:
				case ExtractType.PrefixIfExists:
					PhpException.InvalidArgument("prefix", LibResources.GetString("should_be_specified", "prefix"));
					return 0;
			}

			return Extract(definedVariables, vars, type, null);
		}

		/// <summary>
		/// Import variables into the current variables table from an array.
		/// </summary>
		/// <param name="localVariables">The table of defined variables.</param>
		/// <param name="vars">The <see cref="PhpArray"/> containing names of variables and values to be assigned to them.</param>
		/// <param name="type">The type of the extraction.</param>
		/// <param name="prefix">The prefix (can be a <B>null</B> reference) of variables names.</param>
		/// <returns>The number of variables actually affected by the extraction.</returns>
		/// <exception cref="PhpException"><paramref name="type"/> is invalid.</exception>
		/// <exception cref="PhpException"><paramref name="vars"/> is a <B>null</B> reference.</exception>
        /// <exception cref="InvalidCastException">Some key of <paramref name="localVariables"/> is not type of <see cref="string"/>.</exception>
		/// <include file='Doc/Variables.xml' path='docs/method[@name="Extract"]/*'/>
		[ImplementsFunction("extract", FunctionImplOptions.NeedsVariables)]
		public static int Extract(Dictionary<string, object> localVariables, PhpArray/*!*/ vars, ExtractType type,
				string prefix)
		{
			if (vars == null)
			{
				PhpException.ArgumentNull("vars");
				return 0;
			}

            if (vars.Count == 0)
                return 0;

            // unfortunately, type contains flags are combined with enumeration: 
            bool refs = (type & ExtractType.Refs) != 0;
            type &= ExtractType.NonFlags;

            //
            // construct the action used to set the variable into the locals/globals
            //
            Action<string/*name*/, object/*value*/> updateVariableFn;    // function that writes the value to locals/globals
            Predicate<string/*name*/> containsFn;       // function that checks if variable exists
            PhpArray globals = (localVariables != null) ? null : ScriptContext.CurrentContext.GlobalVariables;

            #region select function that writes the variable

            if (refs)
            {
                // makes a reference and writes it back (deep copy is not necessary, "no duplicate pointers" rule preserved):

                if (localVariables != null)
                {
                    updateVariableFn = (name, value) =>
                    {
                        localVariables[name] = vars[name] = PhpVariable.MakeReference(value);
                    };
                }
                else
                {
                    updateVariableFn = (name, value) =>
                    {
                        globals[name] = vars[name] = PhpVariable.MakeReference(value);
                    };
                }
            }
            else
            {
                if (localVariables != null)
                {
                    updateVariableFn = (name, value) =>
                    {
                        // deep copy the value
                        value = PhpVariable.DeepCopy(PhpVariable.Dereference(value));

                        // put into locals
                        object item;
                        PhpReference ref_item;
                        if (localVariables.TryGetValue(name, out item) && (ref_item = item as PhpReference) != null)
                            ref_item.Value = value;
                        else
                            localVariables[name] = value;
                    };
                }
                else
                {
                    updateVariableFn = (name, value) =>
                    {
                        // deep copy the value
                        value = PhpVariable.DeepCopy(PhpVariable.Dereference(value));

                        // set the value to globals
                        object item;
                        PhpReference ref_item;
                        if (globals.TryGetValue(name, out item) && (ref_item = item as PhpReference) != null)
                            ref_item.Value = value;
                        else
                            globals[name] = value;
                    };
                }
            }

            #endregion

            Debug.Assert(updateVariableFn != null);

            #region select function that checks if variable exists

            if (localVariables != null)
                containsFn = (name) => localVariables.ContainsKey(name);
            else
                containsFn = (name) => globals.ContainsKey(name);

            #endregion

            Debug.Assert(containsFn != null);
            
            //
            //
            //
            int extracted_count = 0;
            foreach (KeyValuePair<IntStringKey, object> entry in vars)
			{
				string name = entry.Key.ToString();
				if (String.IsNullOrEmpty(name) && type != ExtractType.PrefixInvalid) continue;

				switch (type)
				{
					case ExtractType.Overwrite:

						// anything is overwritten:

						break;

					case ExtractType.Skip:

						// skips existing name:
						if (containsFn(name)) continue;

						break;

					case ExtractType.IfExists:

						// skips nonexistent name:
                        if (!containsFn(name)) continue;

						break;

					case ExtractType.PrefixAll:

						// prefix anything:
						name = String.Concat(prefix, "_", name);

						break;

					case ExtractType.PrefixInvalid:

						// prefixes invalid, others are overwritten:
						if (!PhpVariable.IsValidName(name))
							name = String.Concat(prefix, "_", name);

						break;

					case ExtractType.PrefixSame:

						// prefixes existing, others are overwritten:
                        if (containsFn(name))
							name = String.Concat(prefix, "_", name);

						break;

					case ExtractType.PrefixIfExists:

						// prefixes existing, others are skipped:
                        if (containsFn(name))
							name = String.Concat(prefix, "_", name);
						else
							continue;

						break;

					default:
						PhpException.InvalidArgument("type", LibResources.GetString("arg:invalid_value"));
						return 0;
				}

				// invalid names are skipped:
				if (PhpVariable.IsValidName(name))
				{
                    // write the value to locals or globals:
                    updateVariableFn(name, entry.Value);
					
					extracted_count++;
				}
			}

			return extracted_count;
		}

		#endregion

		#region get_defined_vars, import_request_variables (CLR only)

#if !SILVERLIGHT
		/// <summary>
		/// Retrieves an array containing copies of currently defined variables.
		/// </summary>
		/// <param name="localVariables">The table of defined variables.</param>
		/// <returns>The <see cref="PhpArray"/> which keys are the name of variables and values are 
		/// deep copies of their values.</returns>
		[ImplementsFunction("get_defined_vars", FunctionImplOptions.NeedsVariables)]
		public static PhpArray GetDefinedVariables(Dictionary<string, object> localVariables)
		{
			PhpArray globals = (localVariables != null) ? null : ScriptContext.CurrentContext.GlobalVariables;

            PhpArray result = new PhpArray((localVariables != null) ? localVariables.Count : globals.Count);
			foreach (KeyValuePair<string, object> entry in PhpArray.GetEnumerator(globals, localVariables))
				result.Add(entry.Key, PhpVariable.DeepCopy(entry.Value));

			return result;
		}

		/// <summary>
		/// Imports request variables to the $GLOBALS array.
		/// </summary>
		[ImplementsFunction("import_request_variables")]
		public static bool ImportRequestVariables(string gpcOrder)
		{
			return ImportRequestVariables(gpcOrder, null);
		}

		/// <summary>
		/// Imports request variables to the $GLOBALS array.
		/// </summary>
		/// <param name="gpcOrder">
		/// Order of addition. A string containing 'G', 'P', 'C' letters. 
		/// GET, POST, COOKIE variables are set to the $GLOBALS array for each such letter, respectively, 
		/// overwriting variables of the same name that are already there.
		/// </param>
		/// <param name="prefix">
		/// String to prefix variables names with. 
		/// It's recommanded to specify some to prevent overriding existing global variables.
		/// </param>
		/// <returns>
		/// Whether any variables has been added.
		/// </returns>
		/// <exception cref="PhpException">No prefix specified - security hazard (Notice).</exception>
		/// <exception cref="PhpException">Attempt to override $GLOBALS variable (Warning).</exception>
		[ImplementsFunction("import_request_variables")]
		public static bool ImportRequestVariables(string gpcOrder, string prefix)
		{
			if (String.IsNullOrEmpty(gpcOrder)) return false;

			if (prefix == null || prefix == String.Empty)
				PhpException.Throw(PhpError.Notice, LibResources.GetString("no_prefix_security_hazard"));

			HttpContext http_context = HttpContext.Current;
			if (http_context == null)
				return false;

			ScriptContext context = ScriptContext.CurrentContext;

			PhpArray globals_array = context.GlobalVariables;
			PhpArray get_array, post_array, cookie_array;
            string httprawpost_array;

            AutoGlobals.InitializeGetPostVariables(context.Config, http_context.Request, out get_array, out post_array, out httprawpost_array);
			AutoGlobals.InitializeCookieVariables(context.Config, http_context.Request, out cookie_array);

			for (int i = 0; i < gpcOrder.Length; i++)
			{
				switch (Char.ToUpper(gpcOrder[i]))
				{
					case 'G': AddGpcVariables(globals_array, get_array, prefix); break;
					case 'P': AddGpcVariables(globals_array, post_array, prefix); break;
					case 'C': AddGpcVariables(globals_array, cookie_array, prefix); break;
				}
			}

			return true;
		}

		private static void AddGpcVariables(PhpArray/*!*/ globals, PhpArray/*!*/ gpcArray, string prefix)
		{
			foreach (KeyValuePair<IntStringKey, object> entry in gpcArray)
			{
				string name = prefix + entry.Key.ToString();
				if (name == AutoGlobals.GlobalsName)
				{
					PhpException.Throw(PhpError.Warning, LibResources.GetString("attempted_variable_override",
							AutoGlobals.GlobalsName));
				}
				else
				{
					globals[name] = PhpVariable.DeepCopy(entry.Value);
				}
			}
		}
#endif

		#endregion

		#region print_r, var_export, var_dump

		/// <summary>
		/// Outputs human-readable information about a variable. 
		/// </summary>
		/// <param name="expression">The variable.</param>
		[ImplementsFunction("print_r")]
        public static object Print(object expression)
		{
            return Print(expression, false);
		}

		/// <summary>
		/// Outputs or returns human-readable information about a variable. 
		/// </summary>
        /// <param name="expression">The variable.</param>
		/// <param name="returnString">Whether to return a string representation.</param>
		/// <returns>A string representation or <B>TRUE</B> if <paramref name="returnString"/> is <B>false</B>.</returns>
		[ImplementsFunction("print_r")]
        public static object Print(object expression, bool returnString)
		{
			if (returnString)
            {
                // output to a string:
                StringWriter output = new StringWriter();
                PhpVariable.Print(output, expression);
                return output.ToString();
            }
			else
			{
                // output to script context:
				PhpVariable.Print(ScriptContext.CurrentContext.Output, expression);
				return true;
			}
		}

		/// <summary>
		/// Dumps variables.
		/// </summary>
		/// <param name="variables">Variables to be dumped.</param>
		[ImplementsFunction("var_dump")]
		public static void Dump(params object[] variables)
		{
			TextWriter output = ScriptContext.CurrentContext.Output;
			foreach (object variable in variables)
				PhpVariable.Dump(output, variable);
		}

		/// <summary>
		/// Outputs a pars-able string representation of a variable.
		/// </summary>
		/// <param name="variable">The variable.</param>
		[ImplementsFunction("var_export")]
		public static string Export(object variable)
		{
            return Export(variable, false);
		}

		/// <summary>
		/// Outputs or returns a pars-able string representation of a variable.
		/// </summary>
		/// <param name="variable">The variable.</param>
		/// <param name="returnString">Whether to return a string representation.</param>
		/// <returns>A string representation or a <B>null</B> reference if <paramref name="returnString"/> is <B>false</B>.</returns>
		[ImplementsFunction("var_export")]
		public static string Export(object variable, bool returnString)
		{
            if (returnString)
            {
                // output to a string:
                StringWriter output = new StringWriter();
                PhpVariable.Export(output, variable);
                return output.ToString();
            }
            else
            {
                // output to script context:
                PhpVariable.Export(ScriptContext.CurrentContext.Output, variable);
                return null;
            }
			
		}
		#endregion
	}

}
