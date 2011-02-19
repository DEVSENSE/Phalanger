/*

 Copyright (c) 2006 Tomas Matousek. Based on Visual Studio 2005 SDK IronPython sample.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/
using System;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Project;
using Microsoft.Win32;
using EnvDTE;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace PHP.VisualStudio.PhalangerProject
{
	internal enum BuildPropertyPageTag
	{
		DisabledWarnings
	}

	[ComVisible(true), Guid("4B156B7E-6C1C-4eb4-B27E-791E35C9CDCF")]
	public class PhalangerBuildPropertyPage : BuildPropertyPage
	{
		#region Fields & Construction

		private string disabledWarnings;

		/// <include file='doc\PropertyPages.uex' path='docs/doc[@for="GeneralPropertyPage.GeneralPropertyPage"]/*' />
		public PhalangerBuildPropertyPage()
		{
		}

		#endregion

		#region Overriden Methods

		/// <include file='doc\PropertyPages.uex' path='docs/doc[@for="GeneralPropertyPage.GetClassName"]/*' />
		public override string GetClassName()
		{
			return this.GetType().FullName;
		}

		/// <include file='doc\PropertyPages.uex' path='docs/doc[@for="GeneralPropertyPage.BindProperties"]/*' />
		protected override void BindProperties()
		{
			if (this.ProjectMgr == null)
			{
				Debug.Assert(false);
				return;
			}

			base.BindProperties();

			this.disabledWarnings = this.GetConfigProperty(BuildPropertyPageTag.DisabledWarnings.ToString());
		}

		/// <include file='doc\PropertyPages.uex' path='docs/doc[@for="GeneralPropertyPage.ApplyChanges"]/*' />
		protected override int ApplyChanges()
		{
			this.SetConfigProperty(BuildPropertyPageTag.DisabledWarnings.ToString(), this.disabledWarnings);

			return base.ApplyChanges();
		}

		#endregion

		#region Exposed Properties

		/// <include file='doc\PropertyPages.uex' path='docs/doc[@for="GeneralPropertyPage.AssemblyName"]/*' />
		[SRCategoryAttribute(SR.Build)]
		[LocDisplayName(SR.DisabledWarnings)]
		[SRDescriptionAttribute(SR.DisabledWarningsDescription)]
		public string DisabledWarnings
		{
			get { return this.disabledWarnings; }
			set { this.disabledWarnings = value; this.IsDirty = true; }
		}

		#endregion
	}
}
