/*

 Copyright (c) 2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Reflection;
using System.Xml;
using System.Configuration;

using PHP.Core.Reflection;
using PHP.Core.Emit;
using System.Diagnostics;
using System.Web;

namespace PHP.Core
{
	public sealed partial class ApplicationContext
	{
		#region Properties

		/// <summary>
        /// Singleton instance of <see cref="WebServerCompilerManager"/> manager. Created lazily in HTTP context. 
		/// </summary>
		private volatile WebServerCompilerManager webServerCompilerManager;
		private readonly object/*!*/ webServerCompilerManagerMutex = new object();

		#endregion

		#region Initialization

		internal WebServerCompilerManager/*!*/ GetWebServerCompilerManager(RequestContext/*!*/ requestContext)
		{
			Debug.Assert(requestContext != null && HttpContext.Current != null);

			if (webServerCompilerManager == null)
			{
				lock (webServerCompilerManagerMutex)
				{
					if (webServerCompilerManager == null)
						webServerCompilerManager = new WebServerCompilerManager(this);
				}
			}

			return webServerCompilerManager;
		}

		#endregion
	}
}