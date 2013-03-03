/*

 Copyright (c) 2006 Tomas Matousek and Ladislav Prosek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using System.Reflection.Emit;
using PHP.Core.Emit;

namespace PHP.Core.Reflection
{
	/// <summary>
	/// Return type of <see cref="DTypeDesc.GetMember"/>.
	/// </summary>
	public enum GetMemberResult
	{
		/// <summary>The member was found and is visible for the caller.</summary>
		OK,

		/// <summary>The member was not found.</summary>
		NotFound,

		/// <summary>The member was found but is invisible for the caller.</summary>
		BadVisibility,
	}

	#region PhpMemberAttributes

	[Flags]
	public enum PhpMemberAttributes : short
	{
		None = 0,

		Public = 0,
		Private = 1,
		Protected = 2,
		NamespacePrivate = Private,

		Static = 4,
		AppStatic = Static | 8,
		Abstract = 16,
		Final = 32,

        /// <summary>
        /// The type is an interface.
        /// </summary>
		Interface = 64,

        /// <summary>
        /// The type is a trait.
        /// </summary>
        Trait = 128,

		/// <summary>
		/// The member is a constructor.
		/// </summary>
		Constructor = 256,

		/// <summary>
		/// The member is imported type, function or global constant with ambiguous fully qualified name.
		/// </summary>
		Ambiguous = 1024,

		/// <summary>
		/// The member needs to be activated before it can be resolved.
		/// TODO: useful when analysis checks whether there are any imported conditional types/functions.
		/// TODO: add the first conditional to the AC, ignore the others. Add the flag handling to Resolve* and to analyzer.
		/// </summary>
		InactiveConditional = 2048,

        StaticMask = Static | AppStatic,
		VisibilityMask = Public | Private | Protected | NamespacePrivate,
		SpecialMembersMask = Constructor,
		PartialMerged = Abstract | Final
	}

	[DebuggerNonUserCode]
	public static partial class Enums
	{
		#region GetUserEntryPoint Attributes

		public static PhpMemberAttributes GetMemberAttributes(MethodInfo/*!*/ info)
		{
			PhpMemberAttributes value = PhpMemberAttributes.None;

			if (info.IsPublic) value |= PhpMemberAttributes.Public;
			else if (info.IsFamily) value |= PhpMemberAttributes.Protected;
			else if (info.IsPrivate) value |= PhpMemberAttributes.Private;

			if (info.IsStatic)
			{
				value |= (PhpMemberAttributes.Static | PhpMemberAttributes.AppStatic);
			}

			// "finalness" and "abstractness" is stored as attributes, if the method is static, not in MethodInfo
			// (static+abstract, static+final are not allowed in CLR)
			//Debug.Assert(!(info.IsStatic && (info.IsFinal || info.IsAbstract)), "User static method cannot have CLR final or abstract modifier.");

			if (info.IsAbstract || info.IsDefined(typeof(PhpAbstractAttribute), false))
				value |= PhpMemberAttributes.Abstract;

			if (info.IsFinal || info.IsDefined(typeof(PhpFinalAttribute), false))
				value |= PhpMemberAttributes.Final;

			return value;
		}

		/// <summary>
		/// <para>Returns <see cref="MethodAttributes"/> that are used while emitting the method.</para>
		/// <para>NOTE: Combinations static/final and static/abstract can be returned. Such methods are
		/// not allowed in CLI, so final or abstract flag must be removed and PhpFinalAttribute or
		/// PhpAbstractAttribute added instead.</para>
		/// </summary>
		/// <returns></returns>
		public static MethodAttributes ToMethodAttributes(PhpMemberAttributes value)
		{
			MethodAttributes result = 0;
			PhpMemberAttributes visibility = value & PhpMemberAttributes.VisibilityMask;

			if (visibility == PhpMemberAttributes.Public) result |= MethodAttributes.Public;
			else if (visibility == PhpMemberAttributes.Private) result |= MethodAttributes.Private;
			else if (visibility == PhpMemberAttributes.Protected) result |= MethodAttributes.Family;

			if ((value & PhpMemberAttributes.Final) != 0)
				result |= MethodAttributes.Final;

			if ((value & PhpMemberAttributes.Static) != 0)
			{
				result |= MethodAttributes.Static;

				// abstract statics are not allowed in CLR => custom attribute is added
				// final statics as well
			}
			else
			{
				result |= MethodAttributes.Virtual;

				if ((value & PhpMemberAttributes.Abstract) != 0)
					result |= MethodAttributes.Abstract;
			}

			Debug.Assert((value & PhpMemberAttributes.Interface) == 0);

			return result;
		}

		internal static void DefineCustomAttributes(PhpMemberAttributes attrs, MethodInfo/*!*/ method)
		{
			Debug.Assert(method != null);

			if ((attrs & PhpMemberAttributes.Static) != 0)
			{
				if ((attrs & PhpMemberAttributes.Abstract) != 0)
					ReflectionUtils.SetCustomAttribute(method, AttributeBuilders.PhpAbstract);

				if ((attrs & PhpMemberAttributes.Final) != 0)
					ReflectionUtils.SetCustomAttribute(method, AttributeBuilders.PhpFinal);
			}
		}

		#endregion

		#region Property Attributes

		public static PropertyAttributes ToPropertyAttributes(PhpMemberAttributes value)
		{
			return PropertyAttributes.None;
		}

		#endregion

		#region Field Attributes

		public static PhpMemberAttributes GetMemberAttributes(FieldInfo/*!*/ info)
		{
			PhpMemberAttributes value = PhpMemberAttributes.None;

			if (info.IsPublic) value |= PhpMemberAttributes.Public;
			else if (info.IsFamily) value |= PhpMemberAttributes.Protected;
			else if (info.IsPrivate) value |= PhpMemberAttributes.Private;

			if (info.IsStatic)
			{
				value |= PhpMemberAttributes.Static;

				// AppStatic == Static on Silverlight
#if !SILVERLIGHT
				if (!info.IsDefined(typeof(ThreadStaticAttribute), false))
					value |= PhpMemberAttributes.AppStatic;
#else
				value |= PhpMemberAttributes.AppStatic;
#endif
			}

			return value;
		}

        /// <summary>
        /// Determines lowest accessibility of all property accessors and other member attributes.
        /// </summary>
        public static PhpMemberAttributes GetPropertyAttributes(PropertyInfo/*!*/info)
        {
            var accessors = info.GetAccessors(true);
            PhpMemberAttributes attributes = PhpMemberAttributes.None;
            
            // find lowest visibility in all property accessors:
            for (int i = 0; i < accessors.Length; i++)
            {
                if (accessors[i].IsStatic)
                    attributes |= PhpMemberAttributes.Static;

                if (accessors[i].IsPrivate)
                    attributes |= PhpMemberAttributes.Private;
                else if (accessors[i].IsFamily)
                    attributes |= PhpMemberAttributes.Protected;
                //else if (accessors[i].IsPublic)
                //    visibility |= PhpMemberAttributes.Public;
            }

            return attributes;
        }

		public static FieldAttributes ToFieldAttributes(PhpMemberAttributes value)
		{
			FieldAttributes result = 0;
			PhpMemberAttributes visibility = value & PhpMemberAttributes.VisibilityMask;

			if (visibility == PhpMemberAttributes.Public) result |= FieldAttributes.Public;
			else if (visibility == PhpMemberAttributes.Private) result |= FieldAttributes.Private;
			else if (visibility == PhpMemberAttributes.Protected) result |= FieldAttributes.Family;

			if ((value & PhpMemberAttributes.Static) != 0)
				result |= FieldAttributes.Static;

			Debug.Assert((value & PhpMemberAttributes.Interface) == 0);
			Debug.Assert((value & PhpMemberAttributes.Abstract) == 0);
			Debug.Assert((value & PhpMemberAttributes.Final) == 0);

			return result;
		}

		internal static void DefineCustomAttributes(PhpMemberAttributes attrs, FieldBuilder/*!*/ fieldBuilder)
		{
			Debug.Assert(fieldBuilder != null);

			// AppStatic == Static on Silverlight
#if !SILVERLIGHT
			// static but not app-static => thread static
			if ((attrs & PhpMemberAttributes.AppStatic) == PhpMemberAttributes.Static)
				fieldBuilder.SetCustomAttribute(AttributeBuilders.ThreadStatic);
#endif
		}

		#endregion

		#region Type Attributes

		public static PhpMemberAttributes GetMemberAttributes(Type/*!*/ type)
		{
			PhpMemberAttributes value = PhpMemberAttributes.None;

			if (type.IsDefined(typeof(PhpNamespacePrivateAttribute), false))
				value |= PhpMemberAttributes.NamespacePrivate;
			else
				value |= PhpMemberAttributes.Public;

			if (type.IsSealed)
			{
				value |= PhpMemberAttributes.Final;
			}
			else
			{
				if (type.IsInterface) value |= PhpMemberAttributes.Interface | PhpMemberAttributes.Abstract;
				else if (type.IsAbstract) value |= PhpMemberAttributes.Abstract;

                if (type.IsDefined(typeof(PhpTraitAttribute), false))
                    value |= PhpMemberAttributes.Trait;
			}

			return value;
		}

		public static TypeAttributes ToTypeAttributes(PhpMemberAttributes attrs)
		{
			TypeAttributes result = TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit;

			result = TypeAttributes.Public;

			Debug.Assert((attrs & PhpMemberAttributes.VisibilityMask) != PhpMemberAttributes.Protected);

			if ((attrs & PhpMemberAttributes.Abstract) != 0)
				result |= TypeAttributes.Abstract;

			if ((attrs & PhpMemberAttributes.Interface) != 0)
				result |= TypeAttributes.Interface;
			else
				result |= TypeAttributes.Class | TypeAttributes.Serializable;

			if ((attrs & PhpMemberAttributes.Final) != 0)
				result |= TypeAttributes.Sealed;

			return result;
		}

		internal static void DefineCustomAttributes(PhpMemberAttributes attrs, TypeBuilder/*!*/ typeBuilder)
		{
			Debug.Assert(typeBuilder != null);

            if ((attrs & PhpMemberAttributes.Trait) != 0)
                typeBuilder.SetCustomAttribute(AttributeBuilders.ImplementsTrait);
			// no attributes so far
		}

		#endregion

		#region Visibility

		public static string VisibilityToString(PhpMemberAttributes value)
		{
			PhpMemberAttributes visibility = value & PhpMemberAttributes.VisibilityMask;

			if (visibility == PhpMemberAttributes.Public) return "public";
			else if (visibility == PhpMemberAttributes.Protected) return "protected";
			else if (visibility == PhpMemberAttributes.Private) return "private";

			Debug.Fail();
			return null;
		}

		public static bool VisibilityEquals(PhpMemberAttributes attr1, PhpMemberAttributes attr2)
		{
			return ((attr1 & PhpMemberAttributes.VisibilityMask) == (attr2 & PhpMemberAttributes.VisibilityMask));
		}

		public static bool GenericParameterAttrTest(GenericParameterAttributes parameterAttrs,
			GenericParameterAttributes testAttrs)
		{
			return ((parameterAttrs & testAttrs) == testAttrs);
		}

		#endregion

		#region Debug

#if DEBUG

		internal static void Print(PhpMemberAttributes value, TextWriter output)
		{
			if ((value & PhpMemberAttributes.Abstract) != 0) output.Write("abstract ");
			if ((value & PhpMemberAttributes.Final) != 0) output.Write("final ");

			output.Write(VisibilityToString(value));

			if ((value & PhpMemberAttributes.AppStatic) != 0) output.Write("appstatic ");
			else if ((value & PhpMemberAttributes.Static) != 0) output.Write("static ");
		}

#endif
		#endregion
	}

	#endregion

	#region IPhpMember

	public interface IPhpMember
	{

	}

	#endregion

	#region MemberDesc

	[DebuggerNonUserCode]
	public abstract class DMemberDesc
	{
		public PhpMemberAttributes MemberAttributes { get { return memberAttributes; } set { memberAttributes = value; } }
		protected PhpMemberAttributes memberAttributes;

		/// <summary>
		/// <B>null</B> for run-time created (or fast-reflected) descriptors.
		/// Doesn't trigger the reflection (only <see cref="DTypeDesc.Type"/> trigger the reflection).
		/// </summary>
		public DMember Member { get { return member; } internal /* friend DMember, PhpFunction, DTypeDesc */ set { member = value; } }
		private DMember member;

		/// <summary>
		/// Declaring type - a global type in the case of types, functions and global constants.
		/// </summary>
		/// <remarks>
		/// Run-time type descriptors points to a common singleton (global type of the <see cref="UnknownModule"/>).
		/// </remarks>
		public DTypeDesc/*!*/ DeclaringType { get { return declaringType; } }
		protected readonly DTypeDesc/*!*/ declaringType;

		/// <summary>
		/// Gets the <see cref="DModule"/> declaring the type. Forwarded to <see cref="GlobalTypeDesc"/>.
		/// Descriptors created at run-time refers to the singleton of the <see cref="UnknownModule"/> class.
		/// </summary>
		public virtual DModule/*!*/ DeclaringModule { get { return declaringType.DeclaringModule; } }

		public bool IsPublic { get { return (memberAttributes & PhpMemberAttributes.VisibilityMask) == PhpMemberAttributes.Public; } }
		public bool IsPrivate { get { return (memberAttributes & PhpMemberAttributes.VisibilityMask) == PhpMemberAttributes.Private; } }
		public bool IsProtected { get { return (memberAttributes & PhpMemberAttributes.VisibilityMask) == PhpMemberAttributes.Protected; } }
		public PhpMemberAttributes Visibility { get { return memberAttributes & PhpMemberAttributes.VisibilityMask; } }

		public bool IsStatic { get { return (memberAttributes & PhpMemberAttributes.Static) != 0; } }
		public bool IsAppStatic { get { return (memberAttributes & PhpMemberAttributes.AppStatic) == PhpMemberAttributes.AppStatic; } }
		public bool IsThreadStatic { get { return (memberAttributes & PhpMemberAttributes.StaticMask) == PhpMemberAttributes.Static; } }
		public bool IsAbstract { get { return (memberAttributes & PhpMemberAttributes.Abstract) != 0; } }
		public bool IsFinal { get { return (memberAttributes & PhpMemberAttributes.Final) != 0; } }

		public abstract string MakeFullName();
		public abstract string MakeFullGenericName();

		#region Construction

		/// <summary>
		/// Used by all subclasses except for <see cref="GlobalTypeDesc"/>.
		/// </summary>
		protected DMemberDesc(DTypeDesc/*!*/ declaringType, PhpMemberAttributes memberAttributes)
		{
			Debug.Assert(declaringType != null);

			this.declaringType = declaringType;
			this.memberAttributes = memberAttributes;
			this.member = null; // to be filled by DMember or at run-time (if applicable)
		}

		protected DMemberDesc()
		{
			this.declaringType = (DTypeDesc)this;
			this.memberAttributes = PhpMemberAttributes.None;
			this.member = null; // to be filled by DMember (if applicable)
		}

		#endregion

		#region Debug

#if DEBUG

		public override string ToString()
		{
			return (member != null) ? member.ToString() : base.ToString();
		}

#endif

		#endregion
	}

	#endregion

	#region DMember

	[DebuggerNonUserCode]
	public abstract class DMember
	{
		public DMemberDesc/*!*/ MemberDesc { get { return memberDesc; } }
		protected DMemberDesc/*!*/ memberDesc;

		/// <summary>
		/// Declaring type of the member.
		/// </summary>
		public virtual DType DeclaringType { get { return memberDesc.DeclaringType.Type; } }
		public PhpType DeclaringPhpType { get { return (PhpType)DeclaringType; } }

		public DModule DeclaringModule { get { return ((GlobalTypeDesc)memberDesc.DeclaringType).DeclaringModule; } }

		public abstract bool IsUnknown { get; }

		/// <summary>
		/// Whether the analyzer can be sure about the identity of the member.
		/// That is for types, whether the analyzer can refer to the modifiers, generic parameters
		/// and the members declared directly by the type (inherited members needn't to be definite).
		/// 
		/// For functions, whether the analyzer can refer to the signature of the function.
		/// 
		/// Class constants, fields, and methods are identity definite iff they are known (identity definiteness
		/// of the type's member is relative to the declaring type). The analyzer can refer to the modifiers and 
		/// signatures of the identity definite methods.
		/// 
		/// Constructed types are identity definite iff the generic type is identity definite as the members 
		/// of the constructed types are same as ones of the generic types and only the type parameter substitution
		/// needn't to be known.
		/// 
		/// Generic parameters are not identity definite as their substitute is unknown.
		/// </summary>
		public abstract bool IsIdentityDefinite { get; }

		/// <summary>
		/// Whether the analyzer and code generator can be sure about the entire structure of the member, i.e.
		/// whether the member is identity definite and all members that influences its structure (e.g. all base types) 
		/// are definite.
		/// </summary>
		public abstract bool IsDefinite { get; }

		public bool IsPublic { get { Debug.Assert(!IsUnknown, "Undefined"); return memberDesc.IsPublic; } }
		public bool IsPrivate { get { Debug.Assert(!IsUnknown, "Undefined"); return memberDesc.IsPrivate; } }
		public bool IsProtected { get { Debug.Assert(!IsUnknown, "Undefined"); return memberDesc.IsProtected; } }
		public PhpMemberAttributes Visibility { get { Debug.Assert(!IsUnknown, "Undefined"); return memberDesc.Visibility; } }


		public bool IsStatic { get { Debug.Assert(!IsUnknown, "Undefined"); return memberDesc.IsStatic; } }
		public bool IsAppStatic { get { Debug.Assert(!IsUnknown, "Undefined"); return memberDesc.IsAppStatic; } }
		public bool IsThreadStatic { get { Debug.Assert(!IsUnknown, "Undefined"); return memberDesc.IsThreadStatic; } }
		public bool IsAbstract { get { Debug.Assert(!IsUnknown, "Undefined"); return memberDesc.IsAbstract; } }
		public bool IsFinal { get { Debug.Assert(!IsUnknown, "Undefined"); return memberDesc.IsFinal; } }

		public string FullName
		{
			get
			{
				if (_fullName == null) _fullName = GetFullName();
				return _fullName;
			}
		}
		private string _fullName = null;

		#region Construction

		/// <summary>
		/// Used by compiler for unknown members without descriptors (methods, functions), for generic parameters, and 
		/// for unknown members with descriptor.
		/// </summary>
		protected DMember(DMemberDesc memberDesc, string/*!*/ fullName)
		{
			Debug.Assert(fullName != null);

			this.memberDesc = memberDesc;
			if (memberDesc != null)
				memberDesc.Member = this;

			this._fullName = fullName;
		}

		/// <summary>
		/// Used by compiler for known members and unknown members with descriptor (types).
		/// </summary>
		protected DMember(DMemberDesc/*!*/ memberDesc)
		{
			Debug.Assert(memberDesc != null);

			this.memberDesc = memberDesc;
			memberDesc.Member = this;
		}

		#endregion

		public abstract string GetFullName();

		internal virtual void AddAbstractOverride(DMemberRef/*!*/ abstractMember)
		{
			Debug.Fail("N/A");
			throw null;
		}

		internal virtual DMemberRef GetImplementationInSuperTypes(DType/*!*/ type, bool searchSupertypes, ref bool inSupertype)
		{
			Debug.Fail("N/A");
			throw null;
		}

		internal virtual void ReportError(ErrorSink/*!*/ sink, ErrorInfo error)
		{
			// to be implemented when source file and position are known
		}

		internal virtual void ReportAbstractNotImplemented(ErrorSink/*!*/ errors, DType/*!*/ declaringType, PhpType/*!*/ referringType)
		{
			// to be implemented by methods and properties
			Debug.Fail();
			throw null;
		}

        internal virtual void ReportMethodNotCompatible(ErrorSink/*!*/ errors, DType/*!*/ declaringType, PhpType/*!*/ referringType)
        {
            // to be implemented by methods and properties
            Debug.Fail();
            throw null;
        }

		#region Debug

#if DEBUG

		public override string ToString()
		{
			return ((DeclaringType != null) ? DeclaringType.FullName + Name.ClassMemberSeparator : null) + FullName;
		}

#endif

		#endregion
	}

	#endregion

	#region DMemberRef

	/// <summary>
	/// Represents a member-type pair.
	/// </summary>
	internal sealed class DMemberRef
	{
		public static DMemberRef[]/*!*/ EmptyArray = new DMemberRef[0];

		public DMember/*!*/ Member { get { return member; } }
		private readonly DMember/*!*/ member;

		public DType/*!*/ Type { get { return type; } }
		private readonly DType/*!*/ type;

		public DMemberRef(DMember/*!*/ member, DType/*!*/ type)
		{
			Debug.Assert(member != null && type != null);

			this.member = member;
			this.type = type;
		}

		internal void ReportAbstractNotImplemented(ErrorSink/*!*/ errors, PhpType/*!*/ referringType)
		{
			member.ReportAbstractNotImplemented(errors, type, referringType);
		}
	}

	#endregion

}
