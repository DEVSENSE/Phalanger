
/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Project;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using PHP.VisualStudio.PhalangerProject.WPFProviders;

namespace PHP.VisualStudio.PhalangerProject
{
	// Generic stuff:
	[DefaultRegistryRoot("Software\\Microsoft\\VisualStudio\\9.0Exp")]
	[PackageRegistration(UseManagedResourcesOnly = true)]
	[ProvideMenuResource(1000, 1)]
	[ProvideLoadKey(PhalangerConstants.PLKMinEdition, PhalangerConstants.PLKProductVersion, PhalangerConstants.PLKProductName, PhalangerConstants.PLKCompanyName, PhalangerConstants.PLKResourceID)]

	//Set the projectsTemplatesDirectory to a non-existant path to prevent VS from including the working directory as a valid template path
	[ProvideProjectFactory(typeof(PhalangerProjectFactory), "Phalanger", "Phalanger Project Files (*.phpproj);*.phpproj", 
		"phpproj", "phpproj", ".\\NullPath", LanguageVsTemplate = "Phalanger")]
	
	//Register the WPF Factory
	//[ProvideProjectFactory(typeof(PythonWPFProjectFactory), null, null, null, null, null, 
	//	LanguageVsTemplate = "Phalanger", TemplateGroupIDsVsTemplate = "WPF", ShowOnlySpecifiedTemplatesVsTemplate = false)]
	
	[SingleFileGeneratorSupportRegistrationAttribute(typeof(PhalangerProjectFactory))]

	// Web site:
	[WebSiteProject("Phalanger", "PHP")]

	[WebSiteProjectRelatedFiles("aspx", "php")]
	[WebSiteProjectRelatedFiles("xaml", "phpx")]
	[WebSiteProjectRelatedFiles("master", "php")]

	// Property pages:
	[ProvideObject(typeof(GeneralPropertyPage))]
	[ProvideObject(typeof(PhalangerBuildPropertyPage))]

	// designer:
	// Following lines also causes setting of registry keys and values under HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\
	// 8.0\Editors\{8a5b13a6-aaa4-465e-9f93-6c76cca28980} as mentioned before EditorFactory.MapLogicalView
	[ProvideEditorExtensionAttribute(typeof(EditorFactory), ".php", 32)]
	[ProvideEditorExtensionAttribute(typeof(EditorFactory), ".phpx", 32)]
	[ProvideEditorLogicalView(typeof(EditorFactory), "{7651a702-06e5-11d1-8ebd-00a0c90f26ea}")]  //LOGVIEWID_Designer
	[ProvideEditorLogicalView(typeof(EditorFactory), "{7651a701-06e5-11d1-8ebd-00a0c90f26ea}")]  //LOGVIEWID_Code

	// Register the targets file used by the IronPython project system.
	[ProvideMSBuildTargets("Phalanger_2.0", @"%ProgramFiles%\MSBuild\Phalanger\2.0\Phalanger.targets")]

	// Web Application:
	[WAProvideProjectFactory(typeof(WAPhalangerProjectFactory), "Phalanger Web Application Project Templates", "Phalanger", false, "Web", null)]
	[WAProvideProjectFactoryTemplateMapping("{" + GuidList.GuidPhpProjectFactoryString + "}", typeof(WAPhalangerProjectFactory))]
	[WAProvideLanguageProperty(typeof(WAPhalangerProjectFactory), "CodeFileExtension", ".php")]
	[WAProvideLanguageProperty(typeof(WAPhalangerProjectFactory), "TemplateFolder", "Phalanger")]
	//[WAProvideLanguageProperty(typeof(WAPhalangerProjectFactory), "RootIcon", "#8001")]
	[WAProvideLanguageProperty(typeof(WAPhalangerProjectFactory), "CodeBehindEventBinding", "349c5856-65df-11da-9384-00065b846f21"/*typeof(CSWACodeBehindEventBinding)*/)]   // class implementing IVsCodeBehindEventBinding
	//[WAProvideLanguageProperty(typeof(WAPhalangerProjectFactory), "CodeBehindCodeGenerator", typeof(PhpCodeBehindGenerator))] // class implementing IVsCodeBehindCodeGenerator
	//[ProvideObject(typeof(PhpCodeBehindGenerator))]
	//The following value would be the guid of the Debug property page for IronPython (if it existed). The reason this guid is needed is so
	//WAP can hide it from the user.
	//[WAProvideLanguageProperty(typeof(WAPythonProjectFactory), "DebugPageGUID", "{00000000-1008-4FB2-A715-3A4E4F27E610}")]

	// TODO?
	// [LanguageIntellisenseProviderRegistration("{9DDC432B-B9F2-43bd-9806-8E30EBE9444D}", "PhalangerCodeProvider", "Phalanger", ".php", "PHP", "Phalanger")]

	[Guid(GuidList.GuidPhpProjectPkgString)]
	public class PhalangerProjectPackage : ProjectPackage, IVsInstalledProduct
	{
		protected override void Initialize()
		{
			IVsActivityLog log = GetService(typeof(SVsActivityLog)) as IVsActivityLog;
			log.LogEntry((UInt32)__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION, this.ToString(),
					string.Format(System.Globalization.CultureInfo.CurrentCulture, "Entering initializer for: {0}", this.ToString()));

			try
			{
				base.Initialize();

				log.LogEntry((UInt32)__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION, this.ToString(),
						string.Format(System.Globalization.CultureInfo.CurrentCulture, "Registering factories", this.ToString()));

				this.RegisterProjectFactory(new PhalangerProjectFactory(this));

				log.LogEntry((UInt32)__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION, this.ToString(),
						string.Format(System.Globalization.CultureInfo.CurrentCulture, "Project... OK", this.ToString()));

				//this.RegisterProjectFactory(new PythonWPFProjectFactory(this as IServiceProvider));
				//log.LogEntry((UInt32)__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION, this.ToString(),
				//		string.Format(System.Globalization.CultureInfo.CurrentCulture, "WPF... OK", this.ToString()));
				
				this.RegisterEditorFactory(new EditorFactory(this));

				log.LogEntry((UInt32)__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION, this.ToString(),
						string.Format(System.Globalization.CultureInfo.CurrentCulture, "Editor... OK", this.ToString()));
			}
			catch (Exception e)
			{
				log.LogEntry((UInt32)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR, this.ToString(), e.ToString());
			}
		}

		#region IVsInstalledProduct Members
		/// <summary>
		/// This method is called during Devenv /Setup to get the bitmap to
		/// display on the splash screen for this package.
		/// </summary>
		/// <param name="pIdBmp">The resource id corresponding to the bitmap to display on the splash screen</param>
		/// <returns>HRESULT, indicating success or failure</returns>
		public int IdBmpSplash(out uint pIdBmp)
		{
			pIdBmp = 300;

			return VSConstants.S_OK;
		}

		/// <summary>
		/// This method is called to get the icon that will be displayed in the
		/// Help About dialog when this package is selected.
		/// </summary>
		/// <param name="pIdIco">The resource id corresponding to the icon to display on the Help About dialog</param>
		/// <returns>HRESULT, indicating success or failure</returns>
		public int IdIcoLogoForAboutbox(out uint pIdIco)
		{
			pIdIco = 400;

			return VSConstants.S_OK;
		}

		/// <summary>
		/// This methods provides the product official name, it will be
		/// displayed in the help about dialog.
		/// </summary>
		/// <param name="pbstrName">Out parameter to which to assign the product name</param>
		/// <returns>HRESULT, indicating success or failure</returns>
		public int OfficialName(out string pbstrName)
		{
			pbstrName = GetResourceString("@ProductName");
			return VSConstants.S_OK;
		}

		/// <summary>
		/// This methods provides the product description, it will be
		/// displayed in the help about dialog.
		/// </summary>
		/// <param name="pbstrProductDetails">Out parameter to which to assign the description of the package</param>
		/// <returns>HRESULT, indicating success or failure</returns>
		public int ProductDetails(out string pbstrProductDetails)
		{
			pbstrProductDetails = GetResourceString("@ProductDetails");
			return VSConstants.S_OK;
		}

		/// <summary>
		/// This methods provides the product version, it will be
		/// displayed in the help about dialog.
		/// </summary>
		/// <param name="pbstrPID">Out parameter to which to assign the version number</param>
		/// <returns>HRESULT, indicating success or failure</returns>
		public int ProductID(out string pbstrPID)
		{
			pbstrPID = GetResourceString("@ProductID");
			return VSConstants.S_OK;
		}

		#endregion

		/// <summary>
		/// This method loads a localized string based on the specified resource.
		/// </summary>
		/// <param name="resourceName">Resource to load</param>
		/// <returns>String loaded for the specified resource</returns>
		public string GetResourceString(string resourceName)
		{
			string resourceValue;
			IVsResourceManager resourceManager = (IVsResourceManager)GetService(typeof(SVsResourceManager));
			if (resourceManager == null)
			{
				throw new InvalidOperationException("Could not get SVsResourceManager service. Make sure the package is Sited before calling this method");
			}
			Guid packageGuid = this.GetType().GUID;
			int hr = resourceManager.LoadResourceString(ref packageGuid, -1, resourceName, out resourceValue);
			Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(hr);
			return resourceValue;
		}
	}
}
