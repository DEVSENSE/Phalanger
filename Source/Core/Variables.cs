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
using System.Diagnostics;
using System.Globalization;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;

using PHP.Core;
using PHP.Core.Emit;
using PHP.Core.Reflection;

#if SILVERLIGHT
using PHP.CoreCLR;
#endif

namespace PHP.Core
{
	#region Enumerations

	/// <summary>
	/// Type codes of Phalanger special variables.
	/// </summary>
	public enum PhpTypeCode : byte
	{
		/// <summary>The type code of the <see cref="string"/> type.</summary>
		String,
		/// <summary>The type code of the <see cref="int"/> type.</summary>
		Integer,
		/// <summary>The type code of the <see cref="long"/> type.</summary>
		LongInteger,
		/// <summary>The type code of the <see cref="bool"/> type.</summary>
		Boolean,
		/// <summary>The type code of the <see cref="double"/> type.</summary>
		Double,

		/// <summary>The type code of the <see cref="object"/> type and of a <B>null</B> reference.</summary>
		Object,
		/// <summary>The type code of the <see cref="object"/>&amp; type.</summary>
		ObjectAddress,
		/// <summary>The type code of LINQ source.</summary>
		LinqSource,

		/// <summary>The type code of the <see cref="PHP.Core.PhpReference"/> type.</summary>
		PhpReference,
		/// <summary>The type code of the types assignable to <see cref="PHP.Core.PhpArray"/> type.</summary>
		PhpArray,
		/// <summary>The type code of the types assignable to <see cref="PHP.Core.Reflection.DObject"/> type.</summary>
		DObject,
		/// <summary>The type code of the types assignable to <see cref="PHP.Core.PhpResource"/> type.</summary>
		PhpResource,
		/// <summary>The type code of the <see cref="PHP.Core.PhpBytes"/> type.</summary>
		PhpBytes,
		/// <summary>The type code of the <see cref="PHP.Core.PhpString"/> type.</summary>
		PhpString,
		/// <summary>The type code of the <see cref="PHP.Core.PhpRuntimeChain"/> type.</summary>
		PhpRuntimeChain,

        /// <summary>The type code of a callable PHP object. Used as a type hint only.</summary>
        PhpCallable,

		/// <summary>The type code of the types which are not PHP.NET ones.</summary>
		Invalid,
		/// <summary>The type code of the <see cref="System.Void"/> type.</summary>
		Void,
		/// <summary>An unknown type. Means the type cannot or shouldn't be determined.</summary>
		Unknown
	}

	public partial class PhpTypeCodeEnum
	{
		/// <summary>
		/// Retrieves <see cref="Type"/> from a specified <see cref="PhpTypeCode"/>.
		/// </summary>
		public static Type ToType(PhpTypeCode code)
		{
			switch (code)
			{
				case PhpTypeCode.String: return Types.String[0];
				case PhpTypeCode.Integer: return Types.Int[0];
                case PhpTypeCode.LongInteger: return Types.LongInt[0];
				case PhpTypeCode.Boolean: return Types.Bool[0];
				case PhpTypeCode.Double: return Types.Double[0];
				case PhpTypeCode.Object: return Types.Object[0];
				case PhpTypeCode.PhpReference: return Types.PhpReference[0];
				case PhpTypeCode.PhpArray: return Types.PhpArray[0];
				case PhpTypeCode.DObject: return Types.DObject[0];
				case PhpTypeCode.PhpResource: return typeof(PhpResource);
				case PhpTypeCode.PhpBytes: return typeof(PhpBytes);
				case PhpTypeCode.PhpString: return typeof(PhpString);
				case PhpTypeCode.Void: return Types.Void;
				case PhpTypeCode.LinqSource: return Types.IEnumerableOfObject;
				default: return null;
			}
		}

		/// <summary>
		/// Retrieves <see cref="PhpTypeCode"/> from a specified <see cref="Type"/>.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>The code.</returns>
		internal static PhpTypeCode FromType(Type type)
		{
			if (type == Types.Object[0]) return PhpTypeCode.Object;
			if (type == Types.Double[0]) return PhpTypeCode.Double;
			if (type == Types.Int[0]) return PhpTypeCode.Integer;
            if (type == Types.LongInt[0]) return PhpTypeCode.LongInteger;
			if (type == Types.String[0]) return PhpTypeCode.String;
			if (type == Types.Bool[0]) return PhpTypeCode.Boolean;
			if (type == Types.Void) return PhpTypeCode.Void;
			if (type == typeof(PhpReference)) return PhpTypeCode.PhpReference;
			if (type == Types.PhpArray[0]) return PhpTypeCode.PhpArray;
			if (typeof(Reflection.DObject).IsAssignableFrom(type)) return PhpTypeCode.DObject;
			if (type == typeof(PhpResource)) return PhpTypeCode.PhpResource;
			if (type == typeof(PhpBytes)) return PhpTypeCode.PhpBytes;
			if (type == typeof(PhpString)) return PhpTypeCode.PhpString;

			Debug.Fail("GetCodeFromType used on a method with a return type unknown for Phalanger");
			return PhpTypeCode.Invalid;
		}

        /// <summary>
        /// Retrieves <see cref="PhpTypeCode"/> from a specified <paramref name="value"/> instance.
        /// </summary>
        internal static PhpTypeCode FromObject(object value)
        {
            if (value == null) return PhpTypeCode.Object;

            return FromType(value.GetType());
        }
        
        /// <summary>
        /// <c>True</c> iff given <paramref name="code"/> represents value that can be copied (is IPhpCloneable and implements some logic in Copy method).
        /// </summary>
        /// <param name="code"><see cref="PhpTypeCode"/>.</param>
        /// <returns>Wheter given <paramref name="code"/> represents value that can be copied.</returns>
        internal static bool IsDeeplyCopied(PhpTypeCode code)
        {
            return
                code != PhpTypeCode.Void &&
                code != PhpTypeCode.String &&
                code != PhpTypeCode.Boolean &&
                code != PhpTypeCode.Double &&
                code != PhpTypeCode.Integer &&
                code != PhpTypeCode.LongInteger &&
                code != PhpTypeCode.PhpResource;
        }
	}

	/// <summary>
	/// Reason why a variable should be copied.
	/// </summary>
	public enum CopyReason
	{
		/// <summary>
		/// Used when copied by operator =.
		/// </summary>
		Assigned,

		/// <summary>
		/// If <see cref="PhpDeepCopyAttribute"/> has been used on argument compiler generates deep-copy call
		/// with this copy reason.
		/// </summary>
		PassedByCopy,

		/// <summary>
		/// If <see cref="PhpDeepCopyAttribute"/> has been used on return value compiler generates deep-copy call
		/// with this copy reason.
		/// </summary>
		ReturnedByCopy,

		/// <summary>
		/// The reason is unknown.
		/// </summary>
		Unknown
	}

	#endregion

	#region Interfaces: IPhpVariable, IPhpCloneable, IPhpPrintable, IPhpObjectGraphNode, IPhpEnumerator, IPhpEnumerable

	/// <summary>
	/// The set of interfaces which each type used in PHP language should implement.
	/// </summary>
	public interface IPhpVariable : IPhpConvertible, IPhpPrintable, IPhpCloneable, IPhpComparable
	{
		/// <summary>
		/// Defines emptiness on implementor.
		/// </summary>
		/// <returns>Whether the variable is empty.</returns>
		bool IsEmpty();

		/// <summary>
		/// Defines whether implementor is a scalar.
		/// </summary>
		/// <returns>Whether the variable is a scalar.</returns>
		bool IsScalar();

		/// <summary>
		/// Defines a PHP name of implementor.
		/// </summary>
		/// <returns>The string identification of the type.</returns>
		string GetTypeName();
	}

    /// <summary>
	/// Supports cloning, which creates a deep copy of an existing instance.
	/// </summary>
	public interface IPhpCloneable
	{
		/// <summary>
		/// Creates a deep copy of this instance.
		/// </summary>
		/// <returns>The deep copy of this instance.</returns>
		object DeepCopy();

		/// <summary>
		/// Creates a copy of this instance.
		/// </summary>
		/// <param name="reason">The reason why the copy is being made.</param>
		/// <returns>
		/// The copy which should contain data independent of the image ones. This often means that a deep copy
		/// is made, although an inplace deep copy optimization can also be used as well as other methods of copying.
		/// Whatever copy method takes place any changes to the image data should not cause
		/// a change in the result which may be identified by its user.
		/// </returns>
		object Copy(CopyReason reason);
	}

	/// <summary>
	/// Provides methods for printing structured variable content in a several different formats.
	/// </summary>
	public interface IPhpPrintable
	{
		/// <summary>
		/// Prints values only.
		/// </summary>
		/// <param name="output">The output stream.</param>
		/// <remarks>Implementations should write an eoln after the variable's data.</remarks>
		void Print(TextWriter output);

		/// <summary>
		/// Prints types and values.
		/// </summary>
		/// <param name="output">The output stream.</param>
		/// <remarks>Implementations should write an eoln after the variable's data.</remarks>
		void Dump(TextWriter output);

		/// <summary>
		/// Prints object's definition in PHP language.
		/// </summary>
		/// <param name="output">The output stream.</param>
		/// <remarks>Implementations should write an eoln after the variable's data only on the top level.</remarks>
		void Export(TextWriter output);
	}

	/// <summary>
	/// Called back by <see cref="IPhpObjectGraphNode.Walk"/> when walking an object graph.
	/// </summary>
	/// <remarks>
	/// The parameter represents the node that is being visited. Return value represents the new
	/// node value that the original node will be replaced with in its container.
	/// </remarks>
	public delegate object PhpWalkCallback(IPhpObjectGraphNode node, ScriptContext context);

	/// <summary>
	/// Implemented by variable types that represent notable nodes in object graphs.
	/// </summary>
	public interface IPhpObjectGraphNode
	{
		/// <summary>
		/// Walks the object graph rooted in this node. All subnodes supporting the <see cref="IPhpObjectGraphNode"/>
		/// interface will be visited and <pararef name="callback"/> called.
		/// </summary>
		/// <param name="callback">Designates the method that should be invoked for each graph node.</param>
		/// <param name="context">Current <see cref="ScriptContext"/> (optimization).</param>
		/// <remarks><paramref name="callback"/> will not be called for this very object - that is its container's
		/// responsibility.</remarks>
		void Walk(PhpWalkCallback callback, ScriptContext context);
	}

	/// <summary>
	/// Represents enumerator which 
	/// </summary>
	public interface IPhpEnumerator : IDictionaryEnumerator
	{
		/// <summary>
		/// Moves the enumerator to the last entry of the dictionary.
		/// </summary>
		/// <returns>Whether the enumerator has been sucessfully moved to the last entry.</returns>
		bool MoveLast();

		/// <summary>
		/// Moves the enumerator to the first entry of the dictionary.
		/// </summary>
		/// <returns>Whether the enumerator has been sucessfully moved to the first entry.</returns>
		bool MoveFirst();

		/// <summary>
		/// Moves the enumerator to the previous entry of the dictionary.
		/// </summary>
		/// <returns>Whether the enumerator has been sucessfully moved to the previous entry.</returns>
		bool MovePrevious();

		/// <summary>
		/// Gets whether the enumeration has ended and the enumerator points behind the last element.
		/// </summary>
		bool AtEnd { get; }
	}

	/// <summary>
	/// Provides methods which allows implementor to be used in PHP foreach statement as a source of enumeration.
	/// </summary>
	public interface IPhpEnumerable
	{
		/// <summary>
		/// Implementor's intrinsic enumerator which will be advanced during enumeration.
		/// </summary>
		IPhpEnumerator IntrinsicEnumerator { get; }

		/// <summary>
		/// Creates an enumerator used in foreach statement.
		/// </summary>
		/// <param name="keyed">Whether the foreach statement uses keys.</param>
		/// <param name="aliasedValues">Whether the values returned by enumerator are assigned by reference.</param>
		/// <param name="caller">Type <see cref="Reflection.DTypeDesc"/> of the class in whose context the caller operates.</param>
		/// <returns>The dictionary enumerator.</returns>
		/// <remarks>
		/// <see cref="IDictionaryEnumerator.Value"/> should return <see cref="PhpReference"/> 
		/// iff <paramref name="aliasedValues"/>.
		/// </remarks>
		[Emitted]
		IDictionaryEnumerator GetForeachEnumerator(bool keyed, bool aliasedValues, Reflection.DTypeDesc caller);
	}

	#endregion

	/// <summary>
	/// Methods manipulating PHP variables.
	/// </summary>
	/// <remarks>
	/// <para>Implements some IPhp* interfaces for objects of arbitrary PHP.NET type particularly for CLR types.</para>
	/// </remarks>
	[DebuggerNonUserCode]
	public static class PhpVariable
	{
		public static readonly object LiteralNull = null;
		public static readonly object LiteralTrue = true;
		public static readonly object LiteralFalse = false;
        public static readonly object LiteralIntSize = sizeof(int);

		internal static readonly Type/*!*/ RTVariablesTableType = typeof(Dictionary<string, object>);
		internal static readonly ConstructorInfo/*!*/ RTVariablesTableCtor = RTVariablesTableType.GetConstructor(Types.Int);
		internal static readonly MethodInfo/*!*/ RTVariablesTableAdder = RTVariablesTableType.GetProperty("Item").GetSetMethod();//RTVariablesTableType.GetMethod("Add");
        
		#region IPhpPrintable

		/// <summary>
		/// Auxiliary variable holding the current level of indentation while printing a variable.
		/// </summary>
#if SILVERLIGHT
        //TODO: Silverlight doesn't have ThreadStatic, it should be done in different way... now output is just a normal static field
        internal static int PrintIndentationLevel = 0;
		
#else
		[ThreadStatic]
		internal static int PrintIndentationLevel = 0;
#endif

		/// <summary>
		/// Writes indentation spaces to <see cref="TextWriter"/> according to <see cref="PrintIndentationLevel"/>.
		/// </summary>
		/// <param name="output">The <see cref="TextWriter"/> where to write spaces.</param>
		internal static void PrintIndentation(TextWriter output)
		{
			for (int i = 0; i < PrintIndentationLevel; i++)
			{
				output.Write(' ');
				output.Write(' ');
			}
		}

		/// <summary>
		/// Prints a content of the given variable.
		/// </summary>
		/// <param name="output">The output text stream.</param>
		/// <param name="obj">The variable to be printed.</param>
		/// <returns>Always returns true.</returns>
		public static void Print(TextWriter output, object obj)
		{
			IPhpPrintable printable = obj as IPhpPrintable;

			if (printable != null)
				printable.Print(output);
			else
				output.Write(Convert.ObjectToString(obj));
		}

		/// <summary>
		/// Prints the variable to the console.
		/// </summary>
		/// <param name="obj">The variable to print.</param>
		public static void Print(object obj)
		{
			Print(Console.Out, obj);
		}


		/// <summary>
		/// Prints a content of the given variable and its type. 
		/// </summary>
		/// <param name="output">The output text stream.</param>
		/// <param name="obj">The variable to be printed.</param>
		public static void Dump(TextWriter output, object obj)
		{
			string s;
			IPhpPrintable printable;

			if ((printable = obj as IPhpPrintable) != null)
			{
				printable.Dump(output);
			}
			else if ((s = obj as string) != null)
			{
				output.WriteLine(TypeNameString + "({0}) \"{1}\"", s.Length, s);
			}
			else if (obj is double)
			{
				output.WriteLine(TypeNameFloat + "({0})", Convert.DoubleToString((double)obj));
			}
			else if (obj is bool)
			{
				output.WriteLine(TypeNameBool + "({0})", (bool)obj ? True : False);
			}
			else if (obj is int)
			{
				output.WriteLine(TypeNameInt + "({0})", (int)obj);
			}
			else if (obj is long)
			{
				output.WriteLine(TypeNameLongInteger + "({0})", (long)obj);
			}
			else if (obj == null)
			{
				output.WriteLine(PhpVariable.TypeNameNull);
			}
			else
			{
				output.WriteLine("{0}({1})", PhpVariable.GetTypeName(obj), obj.ToString());
			}
		}

		/// <summary>
		/// Dumps the variable to the console.
		/// </summary>
		/// <param name="obj">The variable to dump.</param>
		public static void Dump(object obj)
		{
			Dump(Console.Out, obj);
		}

		/// <summary>
		/// Prints a content of the given variable in PHP language. 
		/// </summary>
		/// <param name="output">The output text stream.</param>
		/// <param name="obj">The variable to be printed.</param>
		public static void Export(TextWriter output, object obj)
		{
			IPhpPrintable printable;
			string s;

			if ((printable = obj as IPhpPrintable) != null)
			{
				printable.Export(output);
			}
			else if ((s = obj as string) != null)
			{
				output.Write("'{0}'", StringUtils.AddCSlashes(s, true, false));
			}
			else if (obj is int)
			{
				output.Write((int)obj);
			}
			else if (obj is long)
			{
				output.Write((long)obj);
			}
			else if (obj is double)
			{
				output.Write(Convert.DoubleToString((double)obj));
			}
			else if (obj is bool)
			{
				output.Write((bool)obj ? True : False);
			}
			else
			{
				output.Write(TypeNameNull);
			}

			// prints an eoln if we are on the top level:
			//if (PrintIndentationLevel == 0)
			//	output.WriteLine();
		}

		/// <summary>
		/// Exports the variable to the console.
		/// </summary>
		/// <param name="obj">The variable to export.</param>
		public static void Export(object obj)
		{
			Export(Console.Out, obj);
		}

		#endregion

		#region IPhpCloneable Interface

		/// <summary>
		/// Creates a deep copy of specified PHP variable.
		/// </summary>
		/// <param name="obj">The variable to copy.</param>
		/// <returns>
		/// A deep copy of <paramref name="obj"/> if it is of arbitrary PHP.NET type or 
		/// implements <see cref="IPhpCloneable"/> interface. Otherwise, only a shallow copy can be made.
		/// </returns>
		public static object DeepCopy(object obj)
		{
			// cloneable types:
            IPhpCloneable php_cloneable;
            if ((php_cloneable = obj as IPhpCloneable) != null)
                return php_cloneable.DeepCopy();

			// string, bool, int, double, null:
			return obj;
		}

		/// <summary>
		/// Depending on the copy reason and configuration, makes 
		/// inplace copy, shallow copy, or a deep copy of a specified PHP variable.
		/// </summary>
		[Emitted]
		public static object Copy(object obj, CopyReason reason)
		{
			// cloneable types:
			IPhpCloneable php_cloneable;
			if ((php_cloneable = obj as IPhpCloneable) != null)
				return php_cloneable.Copy(reason);

			// string, bool, int, double, null:
			return obj;
		}

		public static IEnumerable<KeyValuePair<K, object>>/*!*/ EnumerateDeepCopies<K>(
			IEnumerable<KeyValuePair<K, object>>/*!*/ enumerable)
		{
			if (enumerable == null)
				throw new ArgumentNullException("enumerable");

			foreach (KeyValuePair<K, object> entry in enumerable)
				yield return new KeyValuePair<K, object>(entry.Key, DeepCopy(entry.Value));
		}

		public static IEnumerable<object>/*!*/ EnumerateDeepCopies<K>(IEnumerable<object>/*!*/ enumerable)
		{
			if (enumerable == null)
				throw new ArgumentNullException("enumerable");

			foreach (object item in enumerable)
				yield return DeepCopy(item);
		}

		#endregion

		#region IPhpVariable Interface: GetTypeName, IsEmpty, IsScalar

		/// <summary>
		/// Implements empty language construct.
		/// </summary>
		/// <param name="obj">The variable.</param>
		/// <returns>Whether the variable is empty according to PHP rules.</returns>
		/// <remarks>
		/// A variable is considered to be empty if it is undefined, <b>null</b>, 0, 0.0, <b>false</b>, "0", 
		/// empty string or empty string of bytes, object without properties, 
		/// </remarks>
		/// <exception cref="InvalidCastException">If <paramref name="obj"/> is not of PHP.NET type.</exception>
		[Emitted]
		public static bool IsEmpty(object obj)
		{
			if (obj == null) return true;
            if (obj.GetType() == typeof(int)) return (int)obj == 0;
            if (obj.GetType() == typeof(string)) return !Convert.StringToBoolean((string)obj);
            if (obj.GetType() == typeof(bool)) return !(bool)obj;
            if (obj.GetType() == typeof(double)) return (double)obj == 0.0;
            if (obj.GetType() == typeof(long)) return (long)obj == 0;

			Debug.Assert(obj is IPhpVariable, "Object should be wrapped when calling IsEmpty");

			return ((IPhpVariable)obj).IsEmpty();
		}


		/// <summary>
		/// Checks whether a specified object is of scalar type.
		/// </summary>
		/// <param name="obj">The variable.</param>
		/// <returns>Whether <paramref name="obj"/> is either <see cref="int"/>, <see cref="double"/>,
        /// <see cref="bool"/>, <see cref="long"/>, <see cref="string"/> or <see cref="IPhpVariable.IsScalar"/>.</returns>
		public static bool IsScalar(object obj)
		{
            if (obj == null)
                return false;

            if (obj.GetType() == typeof(int) ||
                obj.GetType() == typeof(double) ||
                obj.GetType() == typeof(bool) ||
                obj.GetType() == typeof(long) ||
                obj.GetType() == typeof(string) ||
                obj.GetType() == typeof(PhpString) ||   // handled also in IPhpVariable, but this is faster
                obj.GetType() == typeof(PhpBytes)       // handled also in IPhpVariable, but this is faster
                )
				return true;

			IPhpVariable php_var = obj as IPhpVariable;
			if (php_var != null) return php_var.IsScalar();

			return false;
		}

		#endregion

		#region Types

		/// <summary>
		/// PHP name for <see cref="int"/>.
		/// </summary>
		public const string TypeNameInt = "int";
        public const string TypeNameInteger = "integer";

		/// <summary>
		/// PHP name for <see cref="long"/>.
		/// </summary>
		public const string TypeNameLongInteger = "int64";

		/// <summary>
		/// PHP name for <see cref="double"/>.
		/// </summary>
		public const string TypeNameDouble = "double";
        public const string TypeNameFloat = "float";

		/// <summary>
		/// PHP name for <see cref="bool"/>.
		/// </summary>
		public const string TypeNameBool = "bool";
        public const string TypeNameBoolean = "boolean";

		/// <summary>
		/// PHP name for <see cref="string"/>.
		/// </summary>
		public const string TypeNameString = "string";

		/// <summary>
		/// PHP name for <see cref="System.Void"/>.
		/// </summary>
		public const string TypeNameVoid = "void";

		/// <summary>
		/// PHP name for <see cref="System.Object"/>.
		/// </summary>
		public const string TypeNameObject = "mixed";

		/// <summary>
		/// PHP name for <B>null</B>.
		/// </summary>
		public const string TypeNameNull = "NULL";

		/// <summary>
		/// PHP name for <B>true</B> constant.
		/// </summary>
		public const string True = "true";

		/// <summary>
		/// PHP name for <B>true</B> constant.
		/// </summary>
		public const string False = "false";

		/// <summary>
		/// Gets the PHP name of a type of a specified object.
		/// </summary>
		/// <param name="obj">The object which type name to get.</param>
		/// <returns>The PHP name of the type of <paramref name="obj"/>.</returns>
		/// <remarks>Returns CLR type name for variables of unknown type.</remarks>
		public static string GetTypeName(object obj)
		{
			IPhpVariable php_var;

			if (obj is int) return TypeNameInteger;
			else if (obj is double) return TypeNameDouble;
			else if (obj is bool) return TypeNameBoolean;
			else if (obj is string) return TypeNameString;
			else if (obj is long) return TypeNameLongInteger;
			else if ((php_var = obj as IPhpVariable) != null) return php_var.GetTypeName();
			else if (obj == null) return TypeNameNull;
			else return obj.GetType().Name;
		}

		/// <summary>
		/// Gets the PHP name of a specified PHP non-primitive type.
		/// </summary>
		/// <param name="type">The PHP non-primitive type.</param>
		/// <returns>The PHP name of the <paramref name="type"/>.</returns>
		/// <remarks>Returns CLR type name for primitive types.</remarks>
		/// <exception cref="ArgumentNullException"><paramref name="type"/> is a <B>null</B>.</exception>
		public static string/*!*/ GetTypeName(Type/*!*/ type)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			string result = PrimitiveTypeDesc.GetPrimitiveName(type);
			if (result != null) return result;

			if (type == typeof(void))
				return TypeNameVoid;

			if (type == typeof(PhpReference))
				return PhpReference.PhpTypeName;

			return type.Name;
		}

		/// <summary>
		/// Determines whether a type of a specified variable is PHP/CLR primitive type.
		/// Doesn't check for <c>object</c> primitive type as is is only a compiler construction.
		/// </summary>
		public static bool HasPrimitiveType(object variable)
		{
			return HasLiteralPrimitiveType(variable) || variable is PhpArray || variable is PhpResource;
		}

		/// <summary>
		/// Determines whether a type of a specified variable is PHP/CLR primitive literal type.
		/// </summary>
		public static bool HasLiteralPrimitiveType(object variable)
		{
            return
                variable == null ||
                variable.GetType() == typeof(bool) ||
                variable.GetType() == typeof(int) ||
                variable.GetType() == typeof(double) ||
                variable.GetType() == typeof(long) ||
                IsString(variable);
		}

		public static bool IsPrimitiveType(Type/*!*/ type)
		{
			return IsLiteralPrimitiveType(type) || type == typeof(PhpArray) || type.IsSubclassOf(typeof(PhpResource));
		}

		/// <summary>
		/// Checks whether the type is among Phalanger primitive ones.
		/// </summary>
		/// <param name="type">The type to be checked.</param>
		/// <exception cref="NullReferenceException"><paramref name="type"/> is a <B>null</B> reference.</exception>
		public static bool IsLiteralPrimitiveType(Type/*!*/ type)
		{
			return type == typeof(bool) || type == typeof(int) || type == typeof(double) || type == typeof(long)
				|| IsStringType(type);
		}

		/// <summary>
		/// Gets the PHP name of a specified PHP non-primitive type.
		/// </summary>
		/// <param name="type">The PHP non-primitive type.</param>
		/// <returns>The PHP name of the <paramref name="type"/> or the type which can be assigned from its.</returns>
		/// <remarks>Returns CLR type name for primitive types.</remarks>
		/// <exception cref="NullReferenceException"><paramref name="type"/> is a <B>null</B> exception.</exception>
		public static string GetAssignableTypeName(Type type)
		{
			if (type.IsAssignableFrom(typeof(PhpArray))) return PhpArray.PhpTypeName;
			if (type.IsAssignableFrom(typeof(PhpObject))) return PhpObject.PhpTypeName;
			if (type.IsAssignableFrom(typeof(PhpResource))) return PhpResource.PhpTypeName;
			if (type.IsAssignableFrom(typeof(PhpReference))) return PhpReference.PhpTypeName;
			if (type.IsAssignableFrom(typeof(PhpBytes))) return PhpBytes.PhpTypeName;
			if (type.IsAssignableFrom(typeof(PhpString))) return PhpString.PhpTypeName;
			return type.Name;
		}

		/// <summary>
		/// Returns <see cref="PhpTypeCode"/> of specified object of arbitrary PHP.NET type.
		/// </summary>
		/// <param name="obj">The object of one of the PHP.NET Framework type.</param>
		/// <returns>The <see cref="PhpTypeCode"/> of the <paramref name="obj"/></returns>
		public static PhpTypeCode GetTypeCode(object obj)
		{
			IPhpConvertible conv;

            if (obj == null) return PhpTypeCode.Object;
            else if (obj.GetType() == typeof(int)) return PhpTypeCode.Integer;
            else if (obj.GetType() == typeof(bool)) return PhpTypeCode.Boolean;
            else if (obj.GetType() == typeof(double)) return PhpTypeCode.Double;
            else if (obj.GetType() == typeof(string)) return PhpTypeCode.String;
            else if (obj.GetType() == typeof(long)) return PhpTypeCode.LongInteger;
			else if ((conv = obj as IPhpConvertible) != null) return conv.GetTypeCode();
			else return PhpTypeCode.Invalid;
		}

		#endregion

		#region AsString, IsString, AsBytes, Dereference, MakeReference, AsArray, Unwrap

		/// <summary>
		/// Casts or converts a specified variable representing a string in PHP into a string. 
		/// </summary>
		/// <param name="variable">The variable.</param>
		/// <returns>
		/// The string representation of <paramref name="variable"/> or 
		/// a <B>null</B> reference if the variable doesn't represent PHP string.
		/// </returns>
		/// <remarks>
		/// <para>
		/// The method should be used by the Class Library functions instead of 
		/// <c>variable <B>as</B> <see cref="string"/></c>.
		/// </para>
		/// <para>
		/// Converts only types which represents a string in PHP. 
		/// These are <see cref="string"/>, <see cref="PhpString"/>, and <see cref="PhpBytes"/>.
		/// Types like <see cref="int"/>, <see cref="bool"/> are not converted.
		/// </para>
		/// </remarks>
        [Emitted]
		public static string AsString(object variable)
		{
            if (object.ReferenceEquals(variable, null))
                return null;

			if (variable.GetType() == typeof(string))
				return (string)variable;

            if (variable.GetType() == typeof(PhpString))
                return ((PhpString)variable).ToString();

            if (variable.GetType() == typeof(PhpBytes))
                return ((PhpBytes)variable).ToString();
			
			return null;
		}

		/// <summary>
		/// Checks whether a variable represents a string in PHP.
		/// </summary>
		/// <param name="variable">The variable.</param>
		/// <returns>Whether a variable represents PHP string.</returns>
		/// <remarks>
		/// The method should be used by the Class Library functions instead of 
		/// <c>variable <B>is</B> <see cref="string"/></c>.
		/// </remarks>
		[Emitted]
		public static bool IsString(object variable)
		{
            if (variable == null)
                return false;

            return
                variable.GetType() == typeof(string) ||
                variable.GetType() == typeof(PhpBytes) ||
                variable.GetType() == typeof(PhpString);
		}

		public static bool IsStringType(Type/*!*/ type)
		{
			return type == typeof(string) || type == typeof(PhpBytes) || type == typeof(PhpString);
		}

		/// <summary>
		/// Casts or converts a specified variable into binary string. 
		/// The method should be used by the Class Library functions instead of 
		/// <c>variable <B>as</B> <see cref="PhpBytes"/></c>.
		/// </summary>
		/// <param name="variable">The variable.</param>
		/// <returns>The binary representation of <paramref name="variable"/> or a <B>null</B> reference.</returns>
		public static PhpBytes AsBytes(object variable)
		{
            if (variable == null)
                return null;

			if (variable.GetType() == typeof(PhpBytes))
                return (PhpBytes)variable;

            if (variable.GetType() == typeof(string))
                return new PhpBytes((string)variable);

            if (variable.GetType() == typeof(PhpString))
                return ((PhpString)variable).ToPhpBytes();
            
			return null;
		}

		/// <summary>
		/// Dereferences a reference (if applicable).
		/// </summary>
		/// <returns></returns>
		public static object Dereference(object variable)
		{
            if (variable != null)
            {
                if (variable.GetType() == typeof(PhpReference))
                    return ((PhpReference)variable).Value;
                else if (variable.GetType() == typeof(PhpSmartReference))
                    return ((PhpSmartReference)variable).Value;
            }

            return variable;
		}

		/// <summary>
		/// Dereferences a reference and returns the <see cref="PhpReference"/>.
		/// </summary>
		/// <param name="variable">The variable to dereference, receives the dereferenced value.</param>
		/// <returns>The <paramref name="variable"/> as <see cref="PhpReference"/>.</returns>
		public static PhpReference Dereference(ref object variable)
		{
            if (variable != null)
            {
                if (variable.GetType() == typeof(PhpReference))
                {
                    var reference = (PhpReference)variable;
                    variable = reference.Value;
                    return reference;
                }
                else if (variable.GetType() == typeof(PhpSmartReference))
                {
                    var reference = (PhpSmartReference)variable;
                    variable = reference.Value;
                    return reference;
                }
            }

            return null;
		}

		/// <summary>
		/// Boxes variable into a reference if it is not yet a reference.
		/// </summary>
		/// <param name="variable">The instance to box.</param>
		/// <returns>The reference.</returns>
		/// <remarks>
		/// Note that there has to be no other CLR reference pointing to the <paramref name="variable"/> 
		/// if it is reachable from PHP. In a case there is such a reference a deep copy has to take place.
		/// </remarks>
        public static PhpReference MakeReference(object variable)
        {
            if (variable != null)
            {
                if (variable.GetType() == typeof(PhpReference))
                    return (PhpReference)variable;
                else if (variable.GetType() == typeof(PhpSmartReference))
                    return (PhpSmartReference)variable;
            }
            
            return new PhpReference(variable);
        }

		/// <summary>
		/// Unwraps a <see cref="Reflection.DObject"/>, <see cref="PhpBytes"/>, and <see cref="PhpString"/>
		/// returning their real object.
		/// </summary>
		/// <param name="var">The object of a PHP type.</param>
		/// <returns>The real <paramref name="var"/>'s value (free of PHP-specific types).</returns>
		public static object Unwrap(object var)
		{
            if (object.ReferenceEquals(var, null))
                return null;

			Reflection.DObject dobj = var as Reflection.DObject;
			if (dobj != null) return dobj.RealObject;

            if (var.GetType() == typeof(PhpBytes))
                return ((PhpBytes)var).Data;

			if (var.GetType() == typeof(PhpString))
                return ((PhpString)var).ToString();

			return var;
		}

		#endregion

		#region IsValidName

		/// <summary>
		/// Checks whether a string is "valid" PHP variable identifier.
		/// </summary>
		/// <param name="name">The variable name.</param>
		/// <returns>
		/// Whether <paramref name="name"/> is "valid" name of variable, i.e. [_[:alpha:]][_0-9[:alpha:]]*.
		/// This doesn't say anything about whether a variable of such name can be used in PHP, e.g. <c>${0}</c> is ok.
		/// </returns>
		public static bool IsValidName(string name)
		{
			if (string.IsNullOrEmpty(name)) return false;

			// first char:
			if (!Char.IsLetter(name[0]) && name[0] != '_') return false;

			// next chars:
			for (int i = 1; i < name.Length; i++)
			{
				if (!Char.IsLetterOrDigit(name[i]) && name[i] != '_') return false;
			}

			return true;
		}

		#endregion
	}

	#region PhpArgument

	/// <summary>
	/// Methods used for checking arguments of Class Library functions.
	/// </summary>
	public sealed class PhpArgument
	{
		/// <summary>
		/// Checks whether a target of callback argument can be invoked.
		/// </summary>
		/// <param name="callback">The callback to check.</param>
        /// <param name="caller">The class context used to bind the callback function.</param>
		/// <param name="argIndex">The index of argument starting from 1 or 0 if not applicable.</param>
		/// <param name="argName">A name of the argument or <B>null</B> reference if not applicable.</param>
		/// <param name="emptyAllowed">Whether an empty callback is allowed.</param>
		/// <returns>Whether the callback can be bound to its target or it is empty and that is allowed.</returns>
		/// <remarks>The callback is bound if it can be.</remarks>
		public static bool CheckCallback(PhpCallback callback, DTypeDesc caller, string argName, int argIndex, bool emptyAllowed)
		{
			// error has already been reported by Convert.ObjectToCallback:
			if (callback == PhpCallback.Invalid)
				return false;

			// callback is empty:
			if (callback == null)
				return emptyAllowed;

			// callback is bindable:
            if (callback.Bind(true, caller, null))
                return true;

			if (argName != null)
				argName = String.Concat('\'', argName, '\'');
			else
				argName = "#" + argIndex;

			PhpException.Throw(PhpError.Warning, CoreResources.GetString("noncallable_callback",
				((IPhpConvertible)callback).ToString(), argName));

			return false;
		}
	}

	#endregion
}
