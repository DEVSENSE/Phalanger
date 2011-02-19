/*

 Copyright (c) 2006 Tomas Matousek. Based on Visual Studio 2005 SDK IronPython sample.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace PHP.VisualStudio.PhalangerProject
{
	/// <summary>
	/// Creates Phalanger Projects
	/// </summary>
	[Guid(GuidList.GuidPhpProjectFactoryString)]
    public class PhalangerProjectFactory : Microsoft.VisualStudio.Project.ProjectFactory
	{
		#region Constructor

		/// <summary>
		/// Constructor for PythonProjectFactory
		/// </summary>
		/// <param name="package">the package who created this object</param>
		public PhalangerProjectFactory(PhalangerProjectPackage package)
				: base(package)
		{
		}
		#endregion

		#region Overridden Methods

		/// <summary>
		/// Creates the Phalanger Project node
		/// </summary>
		/// <returns>the new instance of the Python Project node</returns>
        protected override Microsoft.VisualStudio.Project.ProjectNode CreateProject()
		{
				PhalangerProjectNode project = new PhalangerProjectNode(this.Package as PhalangerProjectPackage);
				project.SetSite((IOleServiceProvider)((IServiceProvider)this.Package).GetService(typeof(IOleServiceProvider)));
				return project;
		}

		#endregion
	}

	/// <summary>
	/// This class is a 'fake' project factory that is used by WAP to register WAP specific information about
	/// Phalanger projects.
	/// </summary>
	[GuidAttribute("3542AB5E-FBF9-4f06-A2EA-97F22A6E350F")]
	public class WAPhalangerProjectFactory { }
}
