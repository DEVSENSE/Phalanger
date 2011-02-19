
/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;
using System.ComponentModel;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Project;
using Microsoft.VisualStudio.Project.Automation;
using Microsoft.Win32;
using EnvDTE;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace PHP.VisualStudio.PhalangerProject
{
	[ComVisible(true), CLSCompliant(false), System.Runtime.InteropServices.ClassInterface(ClassInterfaceType.AutoDual)]
	[Guid("4A383C3B-4734-4d21-B5DF-B4B2CE07AEF4")]
	public class PhalangerProjectNodeProperties : ProjectNodeProperties
	{
		#region ctors
		public PhalangerProjectNodeProperties(ProjectNode node)
			: base(node)
		{
		}
		#endregion

		#region properties
		[Browsable(false)]
		public string OutputFileName
		{
			get
			{
				return ((PhalangerProjectNode)(this.Node.ProjectMgr)).OutputFileName;
			}
		}
		/// <summary>
		/// Returns/Sets the MainFile project property
		/// </summary>
		[Browsable(false)]
		public string MainFile
		{
			get
			{
				return this.Node.ProjectMgr.GetProjectProperty(PhalangerProjectFileConstants.MainFile, true);
			}
			//set
			//{
			//	// Set relative path to file as mainfile property				
			//	this.Node.ProjectMgr.SetProjectProperty(PythonProjectFileConstants.MainFile, value);
			//}
		}

		[Browsable(false)]
		public string AssemblyName
		{
			get
			{
				return this.Node.ProjectMgr.GetProjectProperty(ProjectFileConstants.AssemblyName);
			}
			set
			{
				this.Node.ProjectMgr.SetProjectProperty(ProjectFileConstants.AssemblyName, value);
			}
		}

		[Browsable(false)]
		public string DefaultNamespace
		{
			get
			{
				return this.Node.ProjectMgr.GetProjectProperty(ProjectFileConstants.RootNamespace);
			}
			set
			{
				this.Node.ProjectMgr.SetProjectProperty(ProjectFileConstants.RootNamespace, value);
			}
		}

		[Browsable(false)]
		public string RootNamespace
		{
			get
			{
				return this.Node.ProjectMgr.GetProjectProperty(ProjectFileConstants.RootNamespace);
			}
			set
			{
				this.Node.ProjectMgr.SetProjectProperty(ProjectFileConstants.RootNamespace, value);
			}
		}

		[Browsable(false)]
		public string OutputType
		{
			get
			{
				return this.Node.ProjectMgr.GetProjectProperty(ProjectFileConstants.OutputType);
			}
			set
			{
				this.Node.ProjectMgr.SetProjectProperty(ProjectFileConstants.OutputType, value);
			}
		}

		#endregion
	}
}
