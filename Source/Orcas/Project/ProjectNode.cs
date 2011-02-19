/*

 Copyright (c) 2006 Tomas Matousek. Based on Visual Studio 2005 SDK IronPython sample.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/
using EnvDTE;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Project;
using Microsoft.Win32;
using PHP.VisualStudio.PhalangerLanguageService;
using Microsoft.Windows.Design.Host;
using PHP.VisualStudio.PhalangerProject.WPFProviders;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using VSConstants = Microsoft.VisualStudio.VSConstants;

namespace PHP.VisualStudio.PhalangerProject
{
	[Guid("B2EC32B1-5063-4840-832C-9E9F03E617CE")]
    public class PhalangerProjectNode : ProjectNode // TODO: designer, 
				, IVsProjectSpecificEditorMap2
	{
		#region fields

		private PhalangerProjectPackage package;
		private Guid GUID_MruPage = new Guid("CEB67810-FCF6-4429-A7E0-9E1943C60EF2");
		private VSLangProj.VSProject vsProject = null;
		private Microsoft.VisualStudio.Designer.Interfaces.IVSMDCodeDomProvider codeDomProvider;
		private static ImageList phalangerImageList;
		internal static int ImageOffset;

		#endregion

		#region Properties

		/// <summary>
		/// Returns the outputfilename based on the output type
		/// </summary>
		public string OutputFileName
		{
			get
			{
				string assemblyName = this.ProjectMgr.GetProjectProperty(GeneralPropertyPageTag.AssemblyName.ToString(), true);

				string outputTypeAsString = this.ProjectMgr.GetProjectProperty(GeneralPropertyPageTag.OutputType.ToString(), false);
				OutputType outputType = (OutputType)Enum.Parse(typeof(OutputType), outputTypeAsString);

				return assemblyName + GetOuputExtension(outputType);
			}
		}

		/// <summary>
		/// Retreive the CodeDOM provider
		/// </summary>
		protected internal Microsoft.VisualStudio.Designer.Interfaces.IVSMDCodeDomProvider CodeDomProvider
		{
			get
			{
				if (codeDomProvider == null)
					codeDomProvider = new VSMDPhalangerProvider(this.VSProject);
				return codeDomProvider;
			}
		}

		/// <summary>
		/// Get the VSProject corresponding to this project
		/// </summary>
		protected internal VSLangProj.VSProject VSProject
		{
			get
			{
				if (vsProject == null)
                    vsProject = new Microsoft.VisualStudio.Project.Automation.OAVSProject(this);
				return vsProject;
			}
		}

		private IVsHierarchy InteropSafeHierarchy
		{
			get
			{
				IntPtr unknownPtr = Utilities.QueryInterfaceIUnknown(this);
				if (IntPtr.Zero == unknownPtr)
				{
					return null;
				}
				IVsHierarchy hier = Marshal.GetObjectForIUnknown(unknownPtr) as IVsHierarchy;
				return hier;
			}
		}

		/// <summary>
		/// Python specific project images
		/// </summary>
		public static ImageList PhalangerImageList
		{
			get
			{
				return phalangerImageList;
			}
			set
			{
				phalangerImageList = value;
			}
		}

		#endregion

		#region ctor

		static PhalangerProjectNode()
		{
			string resourceKey = "PHP.VisualStudio.PhalangerProject.Resources.PhalangerImageList.bmp";
			PhalangerImageList = Utilities.GetImageList
				(typeof(PhalangerProjectNode).Assembly.GetManifestResourceStream(resourceKey));
		}


		public PhalangerProjectNode(PhalangerProjectPackage pkg)
		{
			this.package = pkg;
			//this.NodeProperties = new PhalangerProjectNodeProperties(this);

			this.CanFileNodesHaveChilds = true;
			this.OleServiceProvider.AddService(typeof(VSLangProj.VSProject), this.VSProject, false);
			// TODO: designer this.SupportsProjectDesigner = true;

			//Store the number of images in ProjectNode so we know the offset of the python icons.
			ImageOffset = this.ImageHandler.ImageList.Images.Count;
			foreach (Image img in PhalangerImageList.Images)
			{
				this.ImageHandler.AddImage(img);
			}

			InitializeCATIDs();
		}

		/// <summary>
		/// Provide mapping from our browse objects and automation objects to our CATIDs
		/// </summary>
		private void InitializeCATIDs()
		{
			// The following properties classes are specific to python so we can use their GUIDs directly
			this.AddCATIDMapping(typeof(PhalangerProjectNodeProperties), typeof(PhalangerProjectNodeProperties).GUID);
			this.AddCATIDMapping(typeof(PhalangerFileNodeProperties), typeof(PhalangerFileNodeProperties).GUID);
			this.AddCATIDMapping(typeof(OAPhpFileItem), typeof(OAPhpFileItem).GUID);
			// The following are not specific to python and as such we need a separate GUID (we simply used guidgen.exe to create new guids)
			this.AddCATIDMapping(typeof(FolderNodeProperties), new Guid("A3273B8E-FDF8-4ea8-901B-0D66889F645F"));
			// This one we use the same as python file nodes since both refer to files
			this.AddCATIDMapping(typeof(FileNodeProperties), typeof(PhalangerFileNodeProperties).GUID);
			// Because our property page pass itself as the object to display in its grid, we need to make it have the same CATID
			// as the browse object of the project node so that filtering is possible.
			this.AddCATIDMapping(typeof(GeneralPropertyPage), typeof(PhalangerProjectNodeProperties).GUID);

			// We could also provide CATIDs for references and the references container node, if we wanted to.
		}

		#endregion

		#region overridden properties

		public override Guid ProjectGuid
		{
			get
			{
				return typeof(PhalangerProjectFactory).GUID;
			}
		}

		public override string ProjectType
		{
			get
			{
				return "PhalangerProject";
			}
		}

		internal override object Object
		{
			get
			{
				return this.VSProject;
			}
		}

		/// <summary>
		/// Return -1 from the ImageIndex so that VS will use the result from 
		/// GetIconHandle() instead
		/// </summary>
		public override int ImageIndex
		{
			get
			{
				return PhalangerProjectNode.ImageOffset + 1; // second image in 'phalangerimagelist.bmp'
			}
		}

		#endregion

		#region overridden methods

		protected override NodeProperties CreatePropertiesObject()
		{
			return new PhalangerProjectNodeProperties(this);
		}


		public override int Close()
		{
			// TODO:
			//if (null != Site)
			//{
			//  IPhalangerLibraryManager libraryManager = Site.GetService(typeof(IPhalangerLibraryManager)) as IPhalangerLibraryManager;
			//  if (null != libraryManager)
			//  {
			//    libraryManager.UnregisterHierarchy(this.InteropSafeHierarchy);
			//  }
			//}

			return base.Close();
		}

		public override void Load(string filename, string location, string name, uint flags, ref Guid iidProject, out int canceled)
		{
			base.Load(filename, location, name, flags, ref iidProject, out canceled);

			// WAP ask the designer service for the CodeDomProvider corresponding to the project node.
			this.OleServiceProvider.AddService(typeof(SVSMDCodeDomProvider), this.CodeDomProvider, false);
			this.OleServiceProvider.AddService(typeof(System.CodeDom.Compiler.CodeDomProvider), this.CodeDomProvider.CodeDomProvider, false);

			// TODO:
			//IPythonLibraryManager libraryManager = Site.GetService(typeof(IPhalangerLibraryManager)) as IPhalangerLibraryManager;
			//if (null != libraryManager)
			//{
			//  libraryManager.RegisterHierarchy(this.InteropSafeHierarchy);
			//}
		}

		/// <summary>
		/// Overriding to provide project general property page
		/// </summary>
		/// <returns></returns>
		protected override Guid[] GetConfigurationIndependentPropertyPages()
		{
			Guid[] result = new Guid[1];
			result[0] = typeof(GeneralPropertyPage).GUID;
			return result;
		}

		/// <summary>
		/// Returns the configuration dependent property pages.
		/// Specify here a property page. By returning no property page the configuartion dependent properties will be neglected.
		/// Overriding, but current implementation does nothing
		/// To provide configuration specific page project property page, this should return an array bigger then 0
		/// (you can make it do the same as GetPropertyPageGuids() to see its impact)
		/// </summary>
		/// <param name="config"></param>
		/// <returns></returns>
		protected override Guid[] GetConfigurationDependentPropertyPages()
		{
			Guid[] result = new Guid[1];
			result[0] = typeof(PhalangerBuildPropertyPage).GUID;
			return result;
		}

        /// <summary>
		/// Overriding to provide customization of files on add files.
		/// This will replace tokens in the file with actual value (namespace, class name,...)
		/// </summary>
		/// <param name="source">Full path to template file</param>
		/// <param name="target">Full path to destination file</param>
		public override void AddFileFromTemplate(string source, string target)
		{
			if (!System.IO.File.Exists(source))
				throw new FileNotFoundException(String.Format("Template file not found: {0}", source));

			// The class name is based on the new file name
			string className = Path.GetFileNameWithoutExtension(target);
			string namespce = this.FileTemplateProcessor.GetFileNamespace(target, this);

			this.FileTemplateProcessor.AddReplace("%className%", className);
			this.FileTemplateProcessor.AddReplace("%namespace%", namespce);
			try
			{
				this.FileTemplateProcessor.UntokenFile(source, target);
			}
			catch (Exception e)
			{
				throw new FileLoadException("Failed to add template file to project", target, e);
			}
		}
		/// <summary>
		/// Evaluates if a file is an Phalanger code file based on is extension
		/// </summary>
		/// <param name="strFileName">The filename to be evaluated</param>
		/// <returns>true if is a code file</returns>
		public override bool IsCodeFile(string strFileName)
		{
			// We do not want to assert here, just return silently.
			if (String.IsNullOrEmpty(strFileName))
			{
				return false;
			}
			string ext = Path.GetExtension(strFileName);
			return (String.Compare(ext, ".php", StringComparison.OrdinalIgnoreCase) == 0) ||
						 (String.Compare(ext, ".phpx", StringComparison.OrdinalIgnoreCase) == 0);
		}

		/// <summary>
		/// Create a file node based on an msbuild item.
		/// </summary>
		/// <param name="item">The msbuild item to be analyzed</param>
		/// <returns>PhalangerFileNode or FileNode</returns>
		public override FileNode CreateFileNode(ProjectElement item)
		{
			if (item == null)
			{
				throw new ArgumentNullException("item");
			}

			string include = item.GetMetadata(ProjectFileConstants.Include);

			PhalangerFileNode newNode = new PhalangerFileNode(this, item);
			newNode.OleServiceProvider.AddService(typeof(EnvDTE.Project), this.ProjectMgr.GetAutomationObject(), false);
			newNode.OleServiceProvider.AddService(typeof(EnvDTE.ProjectItem), newNode.GetAutomationObject(), false);
			newNode.OleServiceProvider.AddService(typeof(VSLangProj.VSProject), this.VSProject, false);

			if (IsCodeFile(include))
			{
				newNode.OleServiceProvider.AddService(typeof(SVSMDCodeDomProvider), this.CodeDomProvider, false);
			}

			return newNode;
		}

		/// <summary>
		/// Creates the format list for the open file dialog
		/// </summary>
		/// <param name="formatlist">The formatlist to return</param>
		/// <returns>Success</returns>
		public override int GetFormatList(out string formatlist)
		{
			formatlist = String.Format(CultureInfo.CurrentCulture, SR.GetString(SR.ProjectFileExtensionFilter), "\0", "\0");
			return VSConstants.S_OK;
		}

		/// <summary>
		/// This overrides the base class method to show the VS 2005 style Add reference dialog. The ProjectNode implementation
		/// shows the VS 2003 style Add Reference dialog.
		/// </summary>
		/// <returns>S_OK if succeeded. Failure other wise</returns>
		public override int AddProjectReference()
		{

			CCITracing.TraceCall();

			IVsComponentSelectorDlg2 componentDialog;
			Guid guidEmpty = Guid.Empty;
			VSCOMPONENTSELECTORTABINIT[] tabInit = new VSCOMPONENTSELECTORTABINIT[5];
			string strBrowseLocations = Path.GetDirectoryName(this.BaseURI.Uri.LocalPath);

			//Add the .NET page
			tabInit[0].dwSize = (uint)Marshal.SizeOf(typeof(VSCOMPONENTSELECTORTABINIT));
			tabInit[0].varTabInitInfo = 0;
			tabInit[0].guidTab = VSConstants.GUID_COMPlusPage;

			//Add the COM page
			tabInit[1].dwSize = (uint)Marshal.SizeOf(typeof(VSCOMPONENTSELECTORTABINIT));
			tabInit[1].varTabInitInfo = 0;
			tabInit[1].guidTab = VSConstants.GUID_COMClassicPage;

			//Add the Project page
			tabInit[2].dwSize = (uint)Marshal.SizeOf(typeof(VSCOMPONENTSELECTORTABINIT));
			// Tell the Add Reference dialog to call hierarchies GetProperty with the following
			// propID to enablefiltering out ourself from the Project to Project reference
			tabInit[2].varTabInitInfo = (int)__VSHPROPID.VSHPROPID_ShowProjInSolutionPage;
			tabInit[2].guidTab = VSConstants.GUID_SolutionPage;

			// Add the Browse page			
			tabInit[3].dwSize = (uint)Marshal.SizeOf(typeof(VSCOMPONENTSELECTORTABINIT));
			tabInit[3].guidTab = VSConstants.GUID_BrowseFilePage;
			tabInit[3].varTabInitInfo = 0;

			//// Add the Recent page			
			tabInit[4].dwSize = (uint)Marshal.SizeOf(typeof(VSCOMPONENTSELECTORTABINIT));
			tabInit[4].guidTab = GUID_MruPage;
			tabInit[4].varTabInitInfo = 0;

			uint pX = 0, pY = 0;


			componentDialog = this.GetService(typeof(SVsComponentSelectorDlg)) as IVsComponentSelectorDlg2;
			try
			{
				// call the container to open the add reference dialog.
				if (componentDialog != null)
				{
					// Let the project know not to show itself in the Add Project Reference Dialog page
					this.ShowProjectInSolutionPage = false;

					// call the container to open the add reference dialog.
					ErrorHandler.ThrowOnFailure(componentDialog.ComponentSelectorDlg2(
							(System.UInt32)(__VSCOMPSELFLAGS.VSCOMSEL_MultiSelectMode | __VSCOMPSELFLAGS.VSCOMSEL_IgnoreMachineName),
							(IVsComponentUser)this,
							0,
							null,
                            SR.GetString(Microsoft.VisualStudio.Project.SR.AddReferenceDialogTitle),   // Title
							"VS.AddReference",                          // Help topic
							ref pX,
							ref pY,
							(uint)tabInit.Length,
							tabInit,
							ref guidEmpty,
							"*.dll",
							ref strBrowseLocations));
				}
			}
			catch (COMException e)
			{
				Trace.WriteLine("Exception : " + e.Message);
				return e.ErrorCode;
			}
			finally
			{
				// Let the project know it can show itself in the Add Project Reference Dialog page
				this.ShowProjectInSolutionPage = true;
			}
			return VSConstants.S_OK;
		}

		protected override ConfigProvider CreateConfigProvider()
		{
			return new PhalangerConfigProvider(this);
		}

		public override MSBuildResult Build(uint vsopts, string config, IVsOutputWindowPane output, string target)
		{
			MSBuildResult result = base.Build(vsopts, config, output, target);
			//QueryDebugLaunch
			return result;
		}

		#endregion

		#region IVsProjectSpecificEditorMap2 Members

		public int GetSpecificEditorProperty(string mkDocument, int propid, out object result)
		{
			// initialize output params
			result = null;

			//Validate input
			if (string.IsNullOrEmpty(mkDocument))
				throw new ArgumentException("Was null or empty", "mkDocument");

			// Make sure that the document moniker passed to us is part of this project
			// We also don't care if it is not a Phalanger file node
			uint itemid;
			ErrorHandler.ThrowOnFailure(ParseCanonicalName(mkDocument, out itemid));
			HierarchyNode hierNode = NodeFromItemId(itemid);
			if (hierNode == null || ((hierNode as PhalangerFileNode) == null))
				return VSConstants.E_NOTIMPL;

			switch (propid)
			{
				case (int)__VSPSEPROPID.VSPSEPROPID_UseGlobalEditorByDefault:
					// we do not want to use global editor for form files
					result = true;
					break;
				case (int)__VSPSEPROPID.VSPSEPROPID_ProjectDefaultEditorName:
					result = "Phalanger Form Editor";
					break;
			}

			return VSConstants.S_OK;
		}

		public int GetSpecificEditorType(string mkDocument, out Guid guidEditorType)
		{
			// Ideally we should at this point initalize a File extension to EditorFactory guid Map e.g.
			// in the registry hive so that more editors can be added without changing this part of the
			// code. Iron Python only makes usage of one Editor Factory and therefore we will return 
			// that guid
			guidEditorType = EditorFactory.guidEditorFactory;
			return VSConstants.S_OK;
		}

		public int GetSpecificLanguageService(string mkDocument, out Guid guidLanguageService)
		{
			guidLanguageService = Guid.Empty;
			return VSConstants.E_NOTIMPL;
		}

		public int SetSpecificEditorProperty(string mkDocument, int propid, object value)
		{
			return VSConstants.E_NOTIMPL;
		}

		#endregion

		#region static methods

		internal static string GetOuputExtension(OutputType outputType)
		{
			if (outputType == OutputType.Library)
			{
				return "." + OutputFileExtension.dll.ToString();
			}
			else
			{
				return "." + OutputFileExtension.exe.ToString();
			}
		}

		#endregion
	}
}