/*

 Copyright (c) 2006 Tomas Matousek. Based on Visual Studio 2005 SDK IronPython sample.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/
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
using Microsoft.Win32;
using EnvDTE;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace PHP.VisualStudio.PhalangerProject
{
	[ComVisible(true), CLSCompliant(false)]
	[Guid("BF389FD8-F382-41b1-B502-63CB11254137")]
	public class PhalangerFileNodeProperties : SingleFileGeneratorNodeProperties
	{
		#region ctors
		public PhalangerFileNodeProperties(HierarchyNode node)
			: base(node)
		{
		}
		#endregion

		#region properties
		[Browsable(false)]
		public string Url
		{
			get
			{
				return "file:///" + this.Node.Url;
			}
		}
		[Browsable(false)]
		public string SubType
		{
			get
			{
				return ((PhalangerFileNode)this.Node).SubType;
			}
			set
			{
				((PhalangerFileNode)this.Node).SubType = value;
			}
		}

        [Microsoft.VisualStudio.Project.SRCategoryAttribute(Microsoft.VisualStudio.Project.SR.Advanced)]
        [Microsoft.VisualStudio.Project.LocDisplayName(Microsoft.VisualStudio.Project.SR.BuildAction)]
        [Microsoft.VisualStudio.Project.SRDescriptionAttribute(Microsoft.VisualStudio.Project.SR.BuildActionDescription)]
		public virtual PhalangerBuildAction PhalangerBuildAction
		{
			get
			{
				string value = this.Node.ItemNode.ItemName;
				if (value == null || value.Length == 0)
				{
					return PhalangerBuildAction.None;
				}
				return (PhalangerBuildAction)Enum.Parse(typeof(PhalangerBuildAction), value);
			}
			set
			{
				this.Node.ItemNode.ItemName = value.ToString();
			}
		}

		[Browsable(false)]
		public override BuildAction BuildAction
		{
			get
			{
				switch(this.PhalangerBuildAction)
				{
					case PhalangerBuildAction.ApplicationDefinition:
					case PhalangerBuildAction.Page:
					case PhalangerBuildAction.Resource:
						return BuildAction.Compile;
					default:
						return (BuildAction)Enum.Parse(typeof(BuildAction), this.PhalangerBuildAction.ToString());
				}
			}
			set
			{
				this.PhalangerBuildAction = (PhalangerBuildAction)Enum.Parse(typeof(PhalangerBuildAction), value.ToString());
			}
		}
		#endregion
	}

	public enum PhalangerBuildAction { None, Compile, Content, EmbeddedResource, ApplicationDefinition, Page, Resource };
}
