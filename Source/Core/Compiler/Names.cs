/*

 Copyright (c) 2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using PHP.Core.Reflection;
using PHP.Core.Parsers;

#if SILVERLIGHT
using PHP.CoreCLR;
#endif

namespace PHP.Core
{
	// 
	//  Identifier            Representation
	// --------------------------------------------------------------------
	//  variable, field       VariableName     (case-sensitive)
	//  class constant        VariableName     (case-sensitive)
	//  namespace constant    QualifiedName    (case-sensitive)
	//  method                Name             (case-insensitive)
	//  class, function       QualifiedName    (case-insensitive)
	//  namespace component   Name             (case-sensitive?)
	//  label                 VariableName     (case-sensitive?)
	//

	#region Name

	/// <summary>
	/// Case-insensitive culture-sensitive (TODO ???) simple name in Unicode C normal form.
	/// Used for names of methods and namespace components.
	/// </summary>
	[DebuggerNonUserCode]
	[Serializable]
	public struct Name : IEquatable<Name>, IEquatable<string>
	{
		public string/*!*/ Value
		{
			get { return value; }
			set
			{
				this.value = value;
                this.lowerCaseValue = value.ToLower();
			}
		}
		private string/*!*/ value;

		public string/*!*/ LowercaseValue
		{
            get { return lowerCaseValue; }
		}
		private string/*!*/lowerCaseValue;

		public static readonly Name[] EmptyNames = new Name[0];
		public static readonly Name EmptyBaseName = new Name("");
		public static readonly Name SelfClassName = new Name("self");
		public static readonly Name ParentClassName = new Name("parent");
		public static readonly Name AutoloadName = new Name("__autoload");
		public static readonly Name ClrCtorName = new Name(".ctor");
		public static readonly Name ClrInvokeName = new Name("Invoke"); // delegate Invoke method
		public static readonly Name AppStaticName = new Name("AppStatic");
		public static readonly Name AppStaticAttributeName = new Name("AppStaticAttribute");
		public static readonly Name ExportName = new Name("Export");
		public static readonly Name ExportAttributeName = new Name("ExportAttribute");
        public static readonly Name DllImportName = new Name("DllImport");
		public static readonly Name OutAttributeName = new Name("OutAttribute");
		public static readonly Name OutName = new Name("Out");
		public static readonly Name DeclareHelperName = new Name("<Declare>");
		public static readonly Name LambdaFunctionName = new Name("<Lambda>");

		#region Special Names

		public bool IsCloneName
		{
			get { return this.Equals(DObject.SpecialMethodNames.Clone); }
		}

		public bool IsConstructName
		{
			get { return this.Equals(DObject.SpecialMethodNames.Construct); }
		}

		public bool IsDestructName
		{
			get { return this.Equals(DObject.SpecialMethodNames.Destruct); }
		}

        public bool IsCallName
        {
            get { return this.Equals(DObject.SpecialMethodNames.Call); }
        }

        public bool IsCallStaticName
        {
            get { return this.Equals(DObject.SpecialMethodNames.CallStatic); }
        }

		#endregion

		/// <summary>
		/// Creates a name. 
		/// </summary>
		/// <param name="value">The name shouldn't be <B>null</B>.</param>
		public Name(string/*!*/ value)
		{
			Debug.Assert(value != null);
			// TODO (missing from Mono): this.value = value.Normalize();
			this.value = value;
            this.lowerCaseValue = value.ToLower();
		}

		#region Basic Overrides

		public override bool Equals(object obj)
		{
			if (!(obj is Name)) return false;
			return Equals((Name)obj);
		}

		public override int GetHashCode()
		{
			return lowerCaseValue.GetHashCode();
		}

		public override string ToString()
		{
			return this.value;
		}

		#endregion

		#region IEquatable<Name> Members

		public bool Equals(Name other)
		{
            // use lowerCaseValue if initialized, otherwise compare values case-insensitively
            //if (this.lowerCaseValue != null && other.lowerCaseValue != null)
                return this.lowerCaseValue.Equals(other.lowerCaseValue);
            //else
            //    return this.Equals(other.value);
		}

		public static bool operator ==(Name name, Name other)
		{
			return name.Equals(other);
		}

		public static bool operator !=(Name name, Name other)
		{
			return !name.Equals(other);
		}

		#endregion

		#region IEquatable<string> Members

		/*public bool EqualsLowercase(string otherLowercase)
		{
            return this.LowercaseValue.Equals(otherLowercase);
		}*/

		public bool Equals(string other)
		{
            return /*other != null &&*/ string.Compare(value, other, StringComparison.OrdinalIgnoreCase) == 0;// this.lowerCaseValue.Equals(other.ToLower());
		}

		#endregion
	}

	#endregion

	#region VariableName

	/// <summary>
	/// Case-sensitive simple name in Unicode C normal form.
	/// Used for names of variables and constants.
	/// </summary>
	[DebuggerNonUserCode]
	public struct VariableName : IEquatable<VariableName>, IEquatable<string>
	{
		public string/*!*/ Value { get { return value; } set { this.value = value; } }
		private string/*!*/ value;

		#region Special Names

		public static readonly VariableName ThisVariableName = new VariableName("this");

		public bool IsThisVariableName
		{
			get
			{
				return this == ThisVariableName;
			}
		}

		public bool IsAutoGlobal
		{
			get
			{
				return AutoGlobals.IsAutoGlobal(value);
			}
		}

		#endregion

		/// <summary>
		/// Creates a name. 
		/// </summary>
		/// <param name="value">The name, cannot be <B>null</B> nor empty.</param>
		public VariableName(string/*!*/ value)
		{
			Debug.Assert(value != null);
			// TODO (missing from Mono): this.value = value.Normalize();

			this.value = value;
		}

		#region Basic Overrides

		public override bool Equals(object obj)
		{
			if (!(obj is VariableName)) return false;
			return Equals((VariableName)obj);
		}

		public override int GetHashCode()
		{
			return value.GetHashCode();
		}

		public override string ToString()
		{
			return this.value;
		}

		#endregion

		#region IEquatable<VariableName> Members

		public bool Equals(VariableName other)
		{
			return this.value.Equals(other.value);
		}

		public static bool operator ==(VariableName name, VariableName other)
		{
			return name.Equals(other);
		}

		public static bool operator !=(VariableName name, VariableName other)
		{
			return !name.Equals(other);
		}

		#endregion

		#region IEquatable<string> Members

		public bool Equals(string other)
		{
			return value.Equals(other);
		}

		public static bool operator ==(VariableName name, string str)
		{
			return name.Equals(str);
		}

		public static bool operator !=(VariableName name, string str)
		{
			return !name.Equals(str);
		}

		#endregion
	}

	#endregion

	#region QualifiedName

	/// <summary>
	/// Case-insensitive culture-sensitive (TODO ???) qualified name in Unicode C normal form.
	/// </summary>
	[Serializable]
    [DebuggerNonUserCode]
    public struct QualifiedName : IEquatable<QualifiedName>
	{
		internal static readonly QualifiedName Error = new QualifiedName(new Name("<error>"), Name.EmptyNames);
		internal static readonly QualifiedName Global = new QualifiedName(new Name("<Global>"), Name.EmptyNames);
		internal static readonly QualifiedName Lambda = new QualifiedName(new Name("Lambda"), Name.EmptyNames);
		internal static readonly QualifiedName Null = new QualifiedName(new Name("null"), Name.EmptyNames);
		internal static readonly QualifiedName True = new QualifiedName(new Name("true"), Name.EmptyNames);
		internal static readonly QualifiedName False = new QualifiedName(new Name("false"), Name.EmptyNames);
		internal static readonly QualifiedName Array = new QualifiedName(new Name("array"), Name.EmptyNames);
		internal static readonly QualifiedName Object = new QualifiedName(new Name("object"), Name.EmptyNames);
		internal static readonly QualifiedName Integer = new QualifiedName(new Name("int"), Name.EmptyNames);
		internal static readonly QualifiedName LongInteger = new QualifiedName(new Name("int64"), Name.EmptyNames);
		internal static readonly QualifiedName String = new QualifiedName(new Name("string"), Name.EmptyNames);
		internal static readonly QualifiedName Boolean = new QualifiedName(new Name("bool"), Name.EmptyNames);
		internal static readonly QualifiedName Double = new QualifiedName(new Name("double"), Name.EmptyNames);
		internal static readonly QualifiedName Resource = new QualifiedName(new Name("resource"), Name.EmptyNames);
		internal static readonly QualifiedName SystemObject = new QualifiedName(new Name("Object"), new Name[] { new Name("System") });


		public const string Separator = ":::";

		/// <summary>
		/// The outer most namespace is the first in the array.
		/// </summary>
		public Name[]/*!*/ Namespaces { get { return namespaces; } set { namespaces = value; } }
		private Name[]/*!*/ namespaces;

		/// <summary>
		/// Base name. Contains the empty string for namespaces.
		/// </summary>
		public Name Name { get { return name; } set { name = value; } }
		private Name name;

		public bool IsSimpleName
		{
			get
			{
				return Namespaces.Length == 0;
			}
		}

		public bool IsParentClassName
		{
			get { return IsSimpleName && name == Name.ParentClassName; }
		}

		public bool IsSelfClassName
		{
			get { return IsSimpleName && name == Name.SelfClassName; }
		}

		public bool IsReservedClassName
		{
			get { return IsParentClassName || IsSelfClassName; }
		}

		public bool IsAutoloadName
		{
			get { return IsSimpleName && name == Name.AutoloadName; }
		}

		public bool IsAppStaticAttributeName
		{
			get { return IsSimpleName && (name == Name.AppStaticName || name == Name.AppStaticAttributeName); }
		}

		public bool IsExportAttributeName
		{
			get { return IsSimpleName && (name == Name.ExportName || name == Name.ExportAttributeName); }
		}

		public bool IsOutAttributeName
		{
			get { return IsSimpleName && (name == Name.OutName || name == Name.OutAttributeName); }
		}

		/// <summary>
		/// Makes full CLR name from this instance. 
		/// </summary>
		/// <param name="genericParamCount">Number of generic parameters.</param>
		/// <param name="versionIndex">Index of the conditional version or 0 for unconditional.</param>
		/// <returns>Full CLR name.</returns>
		public string ToClrNotation(int genericParamCount, int versionIndex)
		{
			Debug.Assert(versionIndex >= 0, "Version index should be known.");

			StringBuilder result = new StringBuilder();

			for (int i = 0; i < namespaces.Length; i++)
			{
				result.Append(namespaces[i]);
				result.Append('.');
			}

			if (name.Value != "")
				result.Append(name);

			if (versionIndex > 0)
			{
				result.Append('#');
				result.Append(versionIndex);
			}

			if (genericParamCount > 0)
			{
				result.Append('`');
				result.Append(genericParamCount);
			}

			return result.ToString();
		}

		/// <summary>
		/// Parses CLR full name. 
		/// TODO: In some cases, we need to create a PHP full name string (not qualified name), so a similar method
		/// producing a string would be useful.
		/// </summary>
		public static QualifiedName FromClrNotation(string/*!*/ fullName, bool hasBaseName)
		{
			int component_count = 1;
			for (int i = 0; i < fullName.Length; i++)
			{
				if (fullName[i] == '.' || fullName[i] == '+')
					component_count++;
			}

			Name[] namespaces = new Name[hasBaseName ? component_count - 1 : component_count];

			int j = 0;
			int last_separator = -1;
			for (int i = 0; i < fullName.Length; i++)
			{
				if (fullName[i] == '.' || fullName[i] == '+')
				{
					namespaces[j++] = new Name(SubstringWithoutBackquoteAndHash(fullName, last_separator + 1, i - last_separator - 1));
					last_separator = i;
				}
			}

			Name last_component = new Name(SubstringWithoutBackquoteAndHash(fullName, last_separator + 1, fullName.Length - last_separator - 1));

			if (hasBaseName)
			{
				return new QualifiedName(last_component, namespaces);
			}
			else
			{
				namespaces[j] = last_component;
				return new QualifiedName(Name.EmptyBaseName, namespaces);
			}
		}

		private static char[] BackquoteAndHash = new char[] { '`', '#' };

		internal static string/*!*/ SubstringWithoutBackquoteAndHash(string/*!*/ fullName, int start, int length)
		{
			int backquote = fullName.IndexOfAny(BackquoteAndHash, start, length);
			if (backquote != -1)
				length = backquote - start;

			return fullName.Substring(start, length);
		}


		#region Construction

		/// <summary>
		/// Creates a qualified name with or w/o a base name. 
		/// </summary>
		internal QualifiedName(string/*!*/ qualifiedName, bool hasBaseName)
		{
			Debug.Assert(qualifiedName != null);
			QualifiedName qn = Parse(qualifiedName, 0, qualifiedName.Length, hasBaseName);
			this.name = qn.name;
			this.namespaces = qn.namespaces;
		}

		internal QualifiedName(List<string>/*!*/ names, bool hasBaseName)
		{
			Debug.Assert(names != null && names.Count > 0);

			if (hasBaseName)
			{
				name = new Name(names[names.Count - 1]);
				namespaces = new Name[names.Count - 1];
			}
			else
			{
				name = Name.EmptyBaseName;
				namespaces = new Name[names.Count];
			}

			for (int i = 0; i < namespaces.Length; i++)
				namespaces[i] = new Name(names[i]);
		}

		public QualifiedName(Name name)
		{
			this.name = name;
			this.namespaces = Name.EmptyNames;
		}

		public QualifiedName(Name name, Name[]/*!*/ namespaces)
		{
			if (namespaces == null)
				throw new ArgumentNullException("namespaces");

			this.name = name;
			this.namespaces = namespaces;
		}

		internal QualifiedName(Name name, QualifiedName namespaceName)
		{
			Debug.Assert(namespaceName.name.Value == "");

			this.name = name;
			this.namespaces = namespaceName.Namespaces;
		}

		internal QualifiedName(QualifiedName name, QualifiedName namespaceName)
		{
			Debug.Assert(namespaceName.name.Value == "");

			if (name.IsSimpleName)
			{
				this.name = name.name;
				this.namespaces = namespaceName.Namespaces;
			}
			else // used for nested types
			{
				this.name = name.name;
				this.namespaces = ArrayUtils.Concat(namespaceName.namespaces, name.namespaces);
			}
		}

		internal static QualifiedName Parse(string/*!*/ buffer, int startIndex, int length, bool hasBaseName)
		{
			Debug.Assert(buffer != null && startIndex >= 0 && startIndex <= buffer.Length - length);

			QualifiedName result = new QualifiedName();

			int colon_count = 0;
			for (int i = startIndex; i < startIndex + length; i++)
				if (buffer[i] == ':') colon_count++;

			int separator_count = colon_count / Separator.Length;

			Debug.Assert(colon_count % Separator.Length == 0);

			if (separator_count == 0)
			{
				Name entire_name = new Name(buffer.Substring(startIndex, length));

				if (hasBaseName)
				{
					result.namespaces = Name.EmptyNames;
					result.name = entire_name;
				}
				else
				{
					result.namespaces = new Name[] { entire_name };
					result.name = Name.EmptyBaseName;
				}
			}
			else
			{
				result.namespaces = new Name[separator_count + (hasBaseName ? 0 : 1)];

				int current_name = startIndex;
				int next_separator = startIndex;
				int i = 0;
				do
				{
					while (buffer[next_separator] != ':')
						next_separator++;

					result.namespaces[i++] = new Name(buffer.Substring(current_name, next_separator - current_name));
					next_separator += Separator.Length;
					current_name = next_separator;
				}
				while (i < separator_count);

				Name base_name = new Name(buffer.Substring(current_name, length - current_name));

				if (hasBaseName)
				{
					result.name = base_name;
				}
				else
				{
					result.namespaces[separator_count] = base_name;
					result.name = Name.EmptyBaseName;
				}
			}

			return result;
		}

		#endregion

		#region Basic Overrides

		public override bool Equals(object obj)
		{
			if (!(obj is QualifiedName)) return false;
			return !Equals((QualifiedName)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int result = name.GetHashCode();
				for (int i = 0; i < namespaces.Length; i++)
					result ^= namespaces[i].GetHashCode() << (i & 0x0f);

				return result;
			}
		}

		public string ToString(Name? memberName, bool instance)
		{
			StringBuilder result = new StringBuilder();
			for (int i = 0; i < namespaces.Length; i++)
			{
				result.Append(namespaces[i]);
				result.Append(Separator);
			}
			result.Append(Name);
			if (memberName.HasValue)
			{
				result.Append(instance ? "->" : "::");
				result.Append(memberName.Value.ToString());
			}

			return result.ToString();
		}

		public override string ToString()
		{
			return ToString(null, false);
		}

		#endregion

		#region IEquatable<QualifiedName> Members

		public bool Equals(QualifiedName other)
		{
			if (!this.name.Equals(other.name)) return false;

			if (this.namespaces.Length != other.namespaces.Length) return false;

			for (int i = 0; i < namespaces.Length; i++)
			{
				if (!this.namespaces[i].Equals(other.namespaces[i]))
					return false;
			}

			return true;
		}

		public static bool operator ==(QualifiedName name, QualifiedName other)
		{
			return name.Equals(other);
		}

		public static bool operator !=(QualifiedName name, QualifiedName other)
		{
			return !name.Equals(other);
		}

		#endregion
	}

	#endregion

	#region GenericQualifiedName

	/// <summary>
	/// Case-insensitive culture-sensitive (TODO ???) qualified name in Unicode C normal form
	/// with associated list of generic qualified names.
	/// </summary>
	public struct GenericQualifiedName
	{
		public QualifiedName QualifiedName { get { return qualifiedName; } }
		private QualifiedName qualifiedName;

		/// <summary>
		/// Array of GenericQualifiedNames and PrimitiveTypes.
		/// </summary>
		public object[]/*!!*/ GenericParams { get { return genericParams; } }
		private object[]/*!!*/ genericParams;

		public GenericQualifiedName(QualifiedName qualifiedName, object[]/*!!*/ genericParams)
		{
			Debug.Assert(genericParams != null);

			this.qualifiedName = qualifiedName;
			this.genericParams = genericParams;
		}

		public GenericQualifiedName(QualifiedName qualifiedName)
		{
			this.qualifiedName = qualifiedName;
			this.genericParams = ArrayUtils.EmptyObjects;
		}
	}

	#endregion

	#region NamingContext

	public sealed class NamingContext
	{
		public string[] Prefixes { get { return prefixes; } }
		private string[] prefixes;

		public NamingContext(string[] prefixes)
		{
			this.prefixes = prefixes;
		}
	}

	#endregion
}
