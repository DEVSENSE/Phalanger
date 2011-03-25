/*

 Copyright (c) 2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

using PHP.Core;
using PHP.Core.Parsers;
using PHP.Core.Emit;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace PHP.Core.Reflection
{
	/// <summary>
	/// Represents declaration in the compilation unit. 
	/// Declaration can be either known (when compiling) or 
	/// reflected (when reflecting)
	/// </summary>
	public abstract class ScopedDeclaration<T> where T : DMember
	{
		#region Members

		protected ScopedDeclaration(Scope scope)
		{
			this.scope = scope;
		}

		public Scope Scope { get { return scope; } }
		private Scope scope;

		public abstract T Member { get; }
		public abstract ScopedDeclaration<T> CloneWithScope(Scope scope);

		#endregion
	}

	/// <summary>
	/// Known declaration - used by ScriptCompilationUnit
	/// </summary>
	[DebuggerDisplay("{Scope}: {Declaration.Declaree.FullName} (known)")]
	public class KnownScopedDeclaration<T> : ScopedDeclaration<T> 
		where T : DMember
	{
		#region Members

		public Declaration/*!*/ Declaration { get { return declaration; } }
		private Declaration/*!*/ declaration;
		public override T Member { get { return (T)declaration.Declaree; } }

		public KnownScopedDeclaration(Scope scope, Declaration/*!*/ declaration) : base(scope)
		{
			this.declaration = declaration;
		}

		public override ScopedDeclaration<T> CloneWithScope(Scope scope)
		{
			return new KnownScopedDeclaration<T>(scope, declaration);
		}

		#endregion
	}

	/// <summary>
	/// Reflected declaration - used by ReflectedCompilationUnit
	/// </summary>
	[DebuggerDisplay("{Scope}: {Declaration.Member.Name} (reflected)")]
	public class ReflectedScopedDeclaration<T> : ScopedDeclaration<T>
		where T : DMember
	{
		#region Members

		public override T/*!*/ Member { get { return member; } }
		private T/*!*/ member;

		public ReflectedScopedDeclaration(Scope scope, T/*!*/ member)
			: base(scope)
		{
			this.member = member;
		}

		public override ScopedDeclaration<T> CloneWithScope(Scope scope)
		{
			return new ReflectedScopedDeclaration<T>(scope, member);
		}

		#endregion
	}

	#region InclusionTypes

	/// <summary>
	/// Type of inclusion.
	/// </summary>
	/// <remarks>
	/// The properties of inclusion types are defined by IsXxxInclusion methods.
	/// </remarks>
	public enum InclusionTypes
	{
		Include, IncludeOnce, Require, RequireOnce, Prepended, Appended, RunSilverlight
	}

    [DebuggerNonUserCode]
    public static partial class InclusionTypesEnum
	{
		/// <summary>
		/// Returns whether a specified inclusion is "once-inclusion".
		/// </summary>
		public static bool IsOnceInclusion(InclusionTypes inclusionType)
		{
			return inclusionType == InclusionTypes.IncludeOnce || inclusionType == InclusionTypes.RequireOnce;
		}

		/// <summary>
		/// Returns whether a specified inclusion is auto-inclusion (auto-prepended/appended file).
		/// </summary>
		public static bool IsAutoInclusion(InclusionTypes inclusionType)
		{
			return inclusionType == InclusionTypes.Prepended || inclusionType == InclusionTypes.Appended;
		}

		/// <summary>
		/// Returns whether a specified inclusion must succeed.
		/// </summary>
		public static bool IsMustInclusion(InclusionTypes inclusionType)
		{
			return inclusionType != InclusionTypes.IncludeOnce && inclusionType != InclusionTypes.Include;
		}

	}

	#endregion
}
