/*

 Copyright (c) 2005-2006 Ladislav Prosek.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

// Uncomment the following line to enable logging of serialization events into fields of instances
// being (de)serialized.
//#define SERIALIZATION_DEBUG_LOG

using System;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.Serialization;

using PHP.Library;
using PHP.Core.Reflection;

namespace PHP.Core
{
	/// <summary>
	/// Provides services related to serialization.
	/// </summary>
	/// <remarks>
	/// Used by .NET serialization (implemented in Core) as well as by PHP serialization, which is implemented
	/// in ClassLibrary.
	/// </remarks>
	public static class Serialization
	{
		#region ParsePropertyName, FormatPropertyName

		/// <summary>
		/// Parses property name used for serialization. 
		/// </summary>
		/// <param name="name">The name found in serialization stream or returned by <B>__sleep</B>.</param>
		/// <param name="typeName">Will receive the name of the declaring type or <B>null</B> if no
		/// type information is embedded in the property <paramref name="name"/>.</param>
		/// <param name="visibility">Will receive the assumed visibility of the property.</param>
		/// <returns>The bare (unmangled) property name.</returns>
		/// <remarks>
		/// Names of protected properties might be prepended with \0*\0, names of private properties might be
		/// prepended with \0declaring_class_name\0
		/// (see <A href="http://bugs.php.net/bug.php?id=26737">http://bugs.php.net/bug.php?id=26737</A>)
		/// </remarks>
		public static string/*!*/ ParsePropertyName(string/*!*/ name, out string typeName, out PhpMemberAttributes visibility)
		{
			if (name.Length >= 3 && name[0] == '\0')
			{
				if (name[1] == '*' && name[2] == '\0')
				{
					// probably a protected field
					visibility = PhpMemberAttributes.Protected;
					typeName = null;
					return name.Substring(3);
				}
				else
				{
					// probably a private property
					int index = name.IndexOf('\0', 2);
					if (index > 0)
					{
						visibility = PhpMemberAttributes.Private;
						typeName = name.Substring(1, index - 1);  // TODO
						return name.Substring(index + 1);
					}
				}
			}

			visibility = PhpMemberAttributes.Public;
			typeName = null;
			return name;
		}

		/// <summary>
		/// Formats a property name for serialization according to its visibility and declaing type.
		/// </summary>
		/// <param name="property">The property desc.</param>
		/// <param name="propertyName">The property name.</param>
		/// <returns>The property name formatted according to the <paramref name="property"/> as used by PHP serialization.
		/// </returns>
		public static string/*!*/ FormatPropertyName(DPropertyDesc/*!*/ property, string/*!*/ propertyName)
		{
			switch (property.MemberAttributes & PhpMemberAttributes.VisibilityMask)
			{
				case PhpMemberAttributes.Public: return propertyName;
				case PhpMemberAttributes.Protected: return "\0*\0" + propertyName;
				case PhpMemberAttributes.Private: return "\0" + property.DeclaringType.MakeFullName() + "\0" + propertyName;

				default: Debug.Fail(); return null;
			}
		}

		#endregion

		#region EnumerateSerializableProperties

		/// <summary>
		/// Returns names and properties of all instance properties (including runtime fields).
		/// </summary>
		/// <param name="instance">The instance being serialized.</param>
		/// <returns>Name-value pairs. Names are properly formatted for serialization.</returns>
		public static IEnumerable<KeyValuePair<string, object>> EnumerateSerializableProperties(DObject/*!*/ instance)
		{
			return EnumerateSerializableProperties(instance, false);
		}

		/// <summary>
		/// Returns names and properties of all instance properties or only PHP fields (including runtime fields).
		/// </summary>
		/// <param name="instance">The instance being serialized.</param>
		/// <param name="phpFieldsOnly"><B>True</B> to return only PHP fields, <B>false</B> to return all
		/// instance properties.</param>
		/// <returns>Name-value pairs. Names are properly formatted for serialization.</returns>
		public static IEnumerable<KeyValuePair<string, object>> EnumerateSerializableProperties(
			DObject/*!*/ instance,
			bool phpFieldsOnly)
		{
			// enumerate CT properties:
			foreach (KeyValuePair<VariableName, DPropertyDesc> pair in instance.TypeDesc.EnumerateProperties())
			{
				// skip static props
				if (pair.Value.IsStatic) continue;

				// skip CLR fields and props if asked so
				if (phpFieldsOnly && !(pair.Value is DPhpFieldDesc)) continue;

				object property_value = pair.Value.Get(instance);
				PhpReference property_value_ref = property_value as PhpReference;

				if (property_value_ref == null || property_value_ref.IsSet)
				{
					yield return new KeyValuePair<string, object>(
						Serialization.FormatPropertyName(pair.Value, pair.Key.ToString()),
						property_value);
				}
			}

			// enumerate RT fields:
			if (instance.RuntimeFields != null)
			{
                foreach (var pair in instance.RuntimeFields)
                {
                    yield return new KeyValuePair<string, object>(pair.Key.String, pair.Value);
                }
			}
		}

		/// <summary>
		/// Returns names and values of properties whose names have been returned by <c>__sleep</c>.
		/// </summary>
		/// <param name="instance">The instance being serialized.</param>
		/// <param name="sleepResult">The array returned by <c>__sleep</c>.</param>
		/// <param name="context">Current <see cref="ScriptContext"/>.</param>
		/// <returns>Name-value pairs. Names are properly formatted for serialization.</returns>
		/// <exception cref="PhpException">Property of the name returned from <c>__sleep</c> does not exist.</exception>
		/// <remarks>
		/// This method returns exactly <paramref name="sleepResult"/>'s <see cref="PhpHashtable.Count"/> items.
		/// </remarks>
		public static IEnumerable<KeyValuePair<string, object>> EnumerateSerializableProperties(
			DObject/*!*/ instance,
			PhpArray/*!*/ sleepResult,
			ScriptContext/*!*/ context)
		{
			foreach (object item in sleepResult.Values)
			{
				PhpMemberAttributes visibility;
				string name = PHP.Core.Convert.ObjectToString(item);
				string declaring_type_name;
				string property_name = ParsePropertyName(name, out declaring_type_name, out visibility);

				DTypeDesc declarer;
				if (declaring_type_name == null) declarer = instance.TypeDesc;
				else
				{
					declarer = context.ResolveType(declaring_type_name);
					if (declarer == null)
					{
						// property name refers to an unknown class -> value will be null
						yield return new KeyValuePair<string, object>(name, null);
						continue;
					}
				}

				// obtain the property desc and decorate the prop name according to its visibility and declaring class
				DPropertyDesc property;
				if (instance.TypeDesc.GetProperty(new VariableName(property_name), declarer, out property) ==
					GetMemberResult.OK && !property.IsStatic)
				{
					if ((Enums.VisibilityEquals(visibility, property.MemberAttributes) &&
						visibility != PhpMemberAttributes.Public)
						||
						(visibility == PhpMemberAttributes.Private &&
						declarer != property.DeclaringType))
					{
						// if certain conditions are met, serialize the property as null
						// (this is to precisely mimic the PHP behavior)
						yield return new KeyValuePair<string, object>(name, null);
						continue;
					}
					name = FormatPropertyName(property, property_name);
				}
				else property = null;

				// obtain the property value
				object val = null;

				if (property != null)
				{
					val = property.Get(instance);
				}
				else if (instance.RuntimeFields == null || !instance.RuntimeFields.TryGetValue(name, out val))
				{
					// this is new in PHP 5.1
					PhpException.Throw(PhpError.Notice, CoreResources.GetString("sleep_returned_bad_field", name));
				}

				yield return new KeyValuePair<string, object>(name, val);
			}
		}

		#endregion

		#region GetUninitializedInstance, SetProperty

		/// <summary>
		/// Returns an unitialized instance of the specified type or <see cref="__PHP_Incomplete_Class"/>.
		/// </summary>
		/// <param name="typeName">The type name.</param>
		/// <param name="context">Current <see cref="ScriptContext"/>.</param>
		/// <returns>The newly created instance or <B>null</B> if <paramref name="typeName"/> denotes
		/// a primitive type.</returns>
		/// <remarks>
		/// If the <paramref name="typeName"/> denotes a CLR type, no constructor is executed. If the
		/// <paramref name="typeName"/> denotes a PHP type, no user constructor (e.g. <c>__construct</c>)
		/// is executed.
		/// </remarks>
		public static DObject GetUninitializedInstance(string/*!*/ typeName, ScriptContext/*!*/ context)
		{
			// resolve the specified type
			DTypeDesc type = context.ResolveType(typeName);
			if (type == null || type.IsAbstract)
			{
				PhpCallback callback = context.Config.Variables.DeserializationCallback;
				if (callback != null && !callback.IsInvalid)
				{
					callback.Invoke(typeName);
					type = context.ResolveType(typeName);

					if (type == null || type.IsAbstract)
					{
						// unserialize_callback_func failed
						PhpException.Throw(PhpError.Warning, CoreResources.GetString("unserialize_callback_failed",
							((IPhpConvertible)callback).ToString()));
					}
				}
			}

			if (type == null || type.IsAbstract)
			{
				// type not found -> create __PHP_Incomplete_Class
				__PHP_Incomplete_Class pic = new __PHP_Incomplete_Class(context, false);
				pic.__PHP_Incomplete_Class_Name.Value = typeName;
				pic.__PHP_Incomplete_Class_Name.IsSet = true;

				return pic;
			}
			else
			{
				// create the instance
				return type.New(context) as DObject;
			}
		}

		/// <summary>
		/// Sets a property of a <see cref="DObject"/> instance according to deserialized name and value.
		/// </summary>
		/// <param name="instance">The instance being deserialized.</param>
		/// <param name="name">The property name formatted for serialization (see <see cref="FormatPropertyName"/>).</param>
		/// <param name="value">The property value.</param>
		/// <param name="context">Current <see cref="ScriptContext"/>.</param>
		public static void SetProperty(DObject/*!*/ instance, string/*!*/ name, object value, ScriptContext/*!*/ context)
		{
			// the property name might encode its visibility and "classification" -> use these
			// information for suitable property desc lookups
			PhpMemberAttributes visibility;
			string type_name;

			string property_name = ParsePropertyName(name, out type_name, out visibility);

			DTypeDesc declarer;
			if (type_name == null) declarer = instance.TypeDesc;
			else
			{
				declarer = context.ResolveType(type_name);
				if (declarer == null) declarer = instance.TypeDesc;
			}

			// try to find a suitable field handle
			DPropertyDesc property;
			if (instance.TypeDesc.GetProperty(new VariableName(property_name), declarer, out property) ==
				PHP.Core.Reflection.GetMemberResult.OK)
			{
				if ((property.IsPrivate &&
					declarer != property.DeclaringType))
				{
					// if certain conditions are met, don't use the handle even if it was found
					// (this is to precisely mimic the PHP behavior)
					property = null;
				}
			}
			else property = null;

			if (property != null) property.Set(instance, value);
			else
			{
				// suitable CT field not found -> add it to RT fields
				// (note: care must be taken so that the serialize(unserialize($x)) round
				// trip returns $x even if user classes specified in $x are not declared)
                if (instance.RuntimeFields == null) instance.RuntimeFields = new PhpArray();
				instance.RuntimeFields[name] = value;
			}
		}

		#endregion

		#region Debug log
#if !SILVERLIGHT
		[Conditional("SERIALIZATION_DEBUG_LOG")]
		internal static void DebugInstanceSerialized(DObject/*!*/ instance, bool forPersistence)
		{
			instance.SetProperty(
				Guid.NewGuid().ToString(),
				String.Format(
					"Serialized for {0} in process: {1}, app domain: {2}",
					forPersistence ? "persistence" : "remoting",
					Process.GetCurrentProcess().MainModule.FileName,
					AppDomain.CurrentDomain.FriendlyName),
				null);
		}

		[Conditional("SERIALIZATION_DEBUG_LOG")]
		internal static void DebugInstanceDeserialized(DObject/*!*/ instance, bool forPersistence)
		{
			instance.SetProperty(
				Guid.NewGuid().ToString(),
				String.Format(
					"Deserialized for {0} in process: {1}, app domain: {2}",
					forPersistence ? "persistence" : "remoting",
					Process.GetCurrentProcess().MainModule.FileName,
					AppDomain.CurrentDomain.FriendlyName),
				null);
		}
#endif
		#endregion
	}
}