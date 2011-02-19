/*

 Copyright (c) 2006 Ladislav Prosek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Text;
using System.Globalization;
using System.ComponentModel;
using System.Collections.Generic;
using System.CodeDom;

namespace PHP.Core.CodeDom
{
	/// <summary>
	/// Abstract base for <see cref="PhpMemberAttributeConverter"/> and <see cref="PhpTypeAttributeConverter"/>.
	/// </summary>
	/// <remarks>This is almost identical to what C# and VB uses, i.e. conversion between string and an
	/// arbitrary type based on arrays that hold corresponding name-value pairs at the same indices.</remarks>
	internal abstract class PhpModifierAttributeConverter : TypeConverter
	{
		protected abstract object DefaultValue { get; }
		protected abstract string[] Names { get; }
		protected abstract object[] Values { get; }

		protected PhpModifierAttributeConverter()
		{ }

		#region TypeConverter Overrides

		/// <summary>
		/// Returns whether this converter can convert an object of the given type to the type of this converter.
		/// </summary>
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			if (sourceType == typeof(string))
			{
				return true;
			}
			return base.CanConvertFrom(context, sourceType);
		}

		/// <summary>
		/// Converts the given value to the type of this converter.
		/// </summary>
		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			string str_value = value as string;
			if (str_value != null)
			{
				string[] names = Names;
				for (int i = 0; i < names.Length; i++)
				{
					if (names[i].Equals(str_value, StringComparison.OrdinalIgnoreCase))
					{
						return Values[i];
					}
				}
			}
			return this.DefaultValue;
		}

		/// <summary>
		/// Converts the given value object to the specified type, using the arguments.
		/// </summary>
		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
			Type destinationType)
		{
			if (destinationType == null) throw new ArgumentNullException("destinationType");
			if (destinationType != typeof(string)) return base.ConvertTo(context, culture, value, destinationType);

			object[] values = this.Values;
			for (int i = 0; i < values.Length; i++)
			{
				if (values[i].Equals(value))
				{
					return Names[i];
				}
			}
			return "(unknown)";
		}

		/// <summary>
		/// Returns a collection of standard values for the data type this type converter is designed for.
		/// </summary>
		public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
		{
			return new TypeConverter.StandardValuesCollection(Values);
		}

		/// <summary>
		/// Returns whether the collection of standard values returned from GetStandardValues is an exclusive list.
		/// </summary>
		public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
		{
			return true;
		}

		/// <summary>
		/// Returns whether this object supports a standard set of values that can be picked from a list.
		/// </summary>
		public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
		{
			return true;
		}

		#endregion
	}

	/// <summary>
	/// Provides conversion between strings and <see cref="MemberAttributes"/>.
	/// </summary>
	internal class PhpMemberAttributeConverter : PhpModifierAttributeConverter
	{
		#region Fields and Properties

		private static PhpMemberAttributeConverter defaultConverter;
		public static PhpMemberAttributeConverter Default
		{
			get
			{ return defaultConverter; }
		}

		private static string[] names;
		protected override string[] Names
		{
			get
			{ return names; }
		}

		private static object[] values;
		protected override object[] Values
		{
			get
			{ return values; }
		}

		protected override object DefaultValue
		{
			get
			{ return MemberAttributes.Public; }
		}

		#endregion

		private PhpMemberAttributeConverter()
		{ }

		static PhpMemberAttributeConverter()
		{
			defaultConverter = new PhpMemberAttributeConverter();

			names = new string[] { Keywords.Public, Keywords.Protected, Keywords.Private };
			values = new object[] { MemberAttributes.Public, MemberAttributes.Family, MemberAttributes.Private };
		}
	}

	/// <summary>
	/// Provides conversion between strings and <see cref="System.Reflection.TypeAttributes"/>.
	/// </summary>
	internal class PhpTypeAttributeConverter : PhpModifierAttributeConverter
	{
		#region Fields and Properties

		private static PhpTypeAttributeConverter defaultConverter;
		public static PhpTypeAttributeConverter Default
		{
			get
			{ return defaultConverter; }
		}

		private static string[] names;
		protected override string[] Names
		{
			get
			{ return names; }
		}

		private static object[] values;
		protected override object[] Values
		{
			get
			{ return values; }
		}

		protected override object DefaultValue
		{
			get
			{ return System.Reflection.TypeAttributes.Public; }
		}

		#endregion

		private PhpTypeAttributeConverter()
		{ }

		static PhpTypeAttributeConverter()
		{
			defaultConverter = new PhpTypeAttributeConverter();

			names = ArrayUtils.EmptyStrings;
            values = ArrayUtils.EmptyObjects;
		}
	}
}
