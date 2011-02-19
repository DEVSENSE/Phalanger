/*

 Copyright (c) 2004-2006 Tomas Matousek.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

namespace PHP.Core
{
	/// <summary>
	/// Namespaces used by Phalanger.
	/// </summary>
	public class Namespaces
	{
		/// <summary>
		/// Library functions, classes, and interfaces (including extensions).
		/// </summary>
		public const string Library = "PHP.Library";

		/// <summary>
		/// Arg-less stubs of library functions.
		/// </summary>
		public const string LibraryStubs = "PHP.Dynamic";

		/// <summary>
		/// Core.
		/// </summary>
		public const string Core = "PHP.Core";

		/// <summary>
		/// Core subnamespace containing AST nodes.
		/// </summary>
		public const string CoreAst = "PHP.Core.AST";

		/// <summary>
		/// Core subnamespace containing code emitting stuff.
		/// </summary>
		public const string CoreEmit = "PHP.Core.Emit";

		/// <summary>
		/// Extensions manager.
		/// </summary>
		public const string ExtManager = "PHP.ExtManager";
	}
}