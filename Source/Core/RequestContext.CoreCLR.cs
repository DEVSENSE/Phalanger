/*

 Copyright (c) 2005-2006 Tomas Matousek.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/
using System;

namespace PHP.Core
{
	/// <summary>
	/// Represents a set of data associated with the current web request targeting PHP scripts.
	/// </summary>
	public sealed partial class RequestContext
	{
		#region Initialization & Current

		/// <summary>
		/// Allways 'null' on Silverlight.
		/// </summary>
		public static RequestContext CurrentContext { get { return null; } }

		#endregion

		#region Cleanup

		void TryDisposeBeforeFinalization()
		{
		}

		void TryDisposeAfterFinalization()
		{
		}

		void FinallyDispose()
		{
		}

		#endregion
	}
}
