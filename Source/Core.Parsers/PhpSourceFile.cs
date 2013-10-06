/*

 Copyright (c) 2006 Tomas Matousek.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Diagnostics;

namespace PHP.Core
{
	/// <summary>
	/// Represents a PHP source file.
	/// </summary>
	public sealed class PhpSourceFile
	{
		#region Properties

		/// <summary>
		/// Root path used for path relativization.
		/// </summary>
		public FullPath Root { get { return root; } }
		private readonly FullPath root;

		/// <summary>
		/// Full cannonical path to the source file.
		/// </summary>
		public FullPath FullPath
		{
			get
			{
				// converts from relative path if not known:
				if (fullPath.IsEmpty)
					fullPath = relativePath.ToFullPath(root);
				return fullPath;
			}
		}
		private FullPath fullPath;

		/// <summary>
		/// Path to the source file relative to the <see cref="Root"/>.
		/// </summary>
		public RelativePath RelativePath
		{
			get
			{
				// converts from full path if not known:
				if (relativePath.IsEmpty)
					relativePath = new RelativePath(root, fullPath);

				return relativePath;
			}
		}
		private RelativePath relativePath;

		/// <summary>
		/// Full path to directory containing the source file.
		/// </summary>
		public FullPath Directory
		{
			get
			{
				if (directory.IsEmpty)
					directory = new FullPath(System.IO.Path.GetDirectoryName(this.FullPath), false); // TODO: optimize
				return directory;
			}
		}
		private FullPath/*!*/ directory;

		#endregion

		public PhpSourceFile(FullPath root, FullPath fullPath)
		{
			root.EnsureNonEmpty("root");
			fullPath.EnsureNonEmpty("fullPath");

			this.fullPath = fullPath;
			this.relativePath = RelativePath.Empty;
			this.root = root;
		}

		public PhpSourceFile(FullPath root, RelativePath relativePath)
		{
			root.EnsureNonEmpty("root");

			this.root = root;
			this.fullPath = FullPath.Empty;
			this.relativePath = relativePath;
		}

		public override bool Equals(object obj)
		{
			Debug.Assert(obj == null || obj is PhpSourceFile, "Comparing incomparable objects.");
			return Equals(obj as PhpSourceFile);
		}

		public bool Equals(PhpSourceFile other)
		{
			if (ReferenceEquals(other, this)) return true;
			if (other == null) return false;

			// assuming full to relative conversion is faster:
			if (this.fullPath.IsEmpty || other.fullPath.IsEmpty)
				return this.root.Equals(other.root) && this.RelativePath.Equals(other.RelativePath);
			else
				return this.fullPath.Equals(other.fullPath);
		}

		public override int GetHashCode()
		{
			// assuming full to relative conversion is faster:
			return root.GetHashCode() ^ RelativePath.GetHashCode();
		}

		public override string ToString()
		{
			return this.fullPath.IsEmpty ? this.relativePath.ToString() : this.fullPath.ToString();
		}
	}
}