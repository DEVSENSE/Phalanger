
/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Project;
using Microsoft.VisualStudio.Project.Automation;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Windows.Design.Host;
using PHP.VisualStudio.PhalangerProject.WPFProviders;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using VsCommands = Microsoft.VisualStudio.VSConstants.VSStd97CmdID;
using VSConstants = Microsoft.VisualStudio.VSConstants;

namespace PHP.VisualStudio.PhalangerProject
{
	class PhalangerFileNode : FileNode
	{
		#region fields

		private OAVSProjectItem vsProjectItem;
		private SelectionElementValueChangedListener selectionChangedListener;
		//private OAPhpFileItem automationObject;
		private DesignerContext designerContext;

		#endregion

		#region Properties

		/// <summary>
		/// Returns bool indicating whether this node is of subtype "Form"
		/// </summary>
		public bool IsFormSubType
		{
			get { return IsSubType(ProjectFileAttributeValue.Form); }
		}

		/// <summary>
		/// Returns bool indicating whether this node is of subtype "UserControl"
		/// </summary>
		public bool IsUserControlSubType
		{
			get { return IsSubType(ProjectFileAttributeValue.UserControl); }
		}

		/// <summary>
		/// Returns bool indicating whether this node is of subtype "Component"
		/// </summary>
		public bool IsComponentSubType
		{
			get { return IsSubType(ProjectFileAttributeValue.Component); }
		}

		/// <summary>
		/// Returns bool indicating whether this node is of subtype which can be edited by Windows Forms 
		/// Designer (it is "Component", "UserControl" and "Form")
		/// </summary>
		internal bool IsWinFormsDesignerSubType
		{
			get { return IsFormSubType || IsUserControlSubType || IsComponentSubType; }
		}

		/// <summary>
		/// Returns bool indicating whether this node is of given subtype
		/// </summary>
		/// <param name="SubType">Name of subtype</param>
		private bool IsSubType(string SubType)
		{
			string result = this.ItemNode.GetMetadata(ProjectFileConstants.SubType);
			return (!String.IsNullOrEmpty(result) && string.Compare(result, SubType, true, CultureInfo.InvariantCulture) == 0);
		}

		/// <summary>
		/// Returns the SubType of an Iron Python FileNode. It is 
		/// </summary>
		public string SubType
		{
			get
			{
				return this.ItemNode.GetMetadata(ProjectFileConstants.SubType);
			}
			set
			{
				this.ItemNode.SetMetadata(ProjectFileConstants.SubType, value);
			}
		}

		protected internal VSLangProj.VSProjectItem VSProjectItem
		{
			get
			{
				if (null == vsProjectItem)
				{
					vsProjectItem = new OAVSProjectItem(this);
				}
				return vsProjectItem;
			}
		}

		protected internal Microsoft.Windows.Design.Host.DesignerContext DesignerContext
		{
			get
			{
				if (designerContext == null)
				{
					designerContext = new DesignerContext();
					//Set the EventBindingProvider for this XAML file so the designer will call it
					//when event handlers need to be generated
					designerContext.EventBindingProvider = new PythonEventBindingProvider(this.Parent.FindChild(this.Url.Replace(".xaml", ".php")) as PhalangerFileNode);
				}
				return designerContext;
			}
		}

		#endregion

		#region Constructors

		internal PhalangerFileNode(ProjectNode root, ProjectElement e)
			: base(root, e)
		{
			selectionChangedListener = new SelectionElementValueChangedListener(new ServiceProvider((IOleServiceProvider)root.GetService(typeof(IOleServiceProvider))), root);
			selectionChangedListener.Init();

		}
		#endregion

		#region Overridden Properties

		public override int ImageIndex
		{
			get
			{
				if (IsFormSubType)
					return (int)ProjectNode.ImageName.WindowsForm;
				
				if (this.FileName.ToLower().EndsWith(".php") || this.FileName.ToLower().EndsWith(".phpx"))
					return PhalangerProjectNode.ImageOffset + 0; // first image in 'phalangerimagelist.bmp'
				
				return base.ImageIndex;
			}
		}

		internal override object Object
		{
			get
			{
				return this.VSProjectItem;
			}
		}

		#endregion

		#region Overridden Methods

		protected override NodeProperties CreatePropertiesObject()
		{
			PhalangerFileNodeProperties properties = new PhalangerFileNodeProperties(this);
			properties.OnCustomToolChanged += new EventHandler<HierarchyNodeEventArgs>(OnCustomToolChanged);
			properties.OnCustomToolNameSpaceChanged += new EventHandler<HierarchyNodeEventArgs>(OnCustomToolNameSpaceChanged);
			return properties;
		}

		public override int Close()
		{
			if (selectionChangedListener != null)
				selectionChangedListener.Dispose();
			return base.Close();
		}

		/// <summary>
		/// Returs an FileNode specific object implmenting DTE.ProjectItem
		/// </summary>
		/// <returns></returns>
		public override object GetAutomationObject()
		{
			return new OAPhpFileItem(this.ProjectMgr.GetAutomationObject() as OAProject, this);
		}

		/// <summary>
		/// Open a file depending on the SubType property associated with the file item in the project file
		/// </summary>
		protected override void DoDefaultAction()
		{
			FileDocumentManager manager = this.GetDocumentManager() as FileDocumentManager;
			Debug.Assert(manager != null, "Could not get the FileDocumentManager");

			Guid viewGuid = (IsWinFormsDesignerSubType ? VSConstants.LOGVIEWID_Designer : VSConstants.LOGVIEWID_Code);
			IVsWindowFrame frame;
			manager.Open(false, false, viewGuid, out frame, WindowFrameShowAction.Show);
		}

		protected override int ExecCommandOnNode(Guid guidCmdGroup, uint cmd, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
		{
			Debug.Assert(this.ProjectMgr != null, "The FileNode has no project manager");

            if (this.ProjectMgr == null)
			{
				throw new InvalidOperationException();
			}

			if (guidCmdGroup == PhalangerMenus.guidPhalangerProjectCmdSet)
			{
				if (cmd == (uint)PhalangerMenus.SetAsMain.ID)
				{
					// Set the MainFile project property to the Filename of this Node
					((PhalangerProjectNode)this.ProjectMgr).SetProjectProperty(PhalangerProjectFileConstants.MainFile, this.GetRelativePath());
					return VSConstants.S_OK;
				}
			}
            /*if (guidCmdGroup == Microsoft.VisualStudio.Shell.VsMenus.guidStandardCommandSet97)
            {
                switch ((VsCommands)cmd)
                {
                    case VsCommands.Delete:

                        break;
                }
            }*/
            
			return base.ExecCommandOnNode(guidCmdGroup, cmd, nCmdexecopt, pvaIn, pvaOut);
		}

		/// <summary>
		/// Handles the menuitems
		/// </summary>		
		protected override int QueryStatusOnNode(Guid guidCmdGroup, uint cmd, IntPtr pCmdText, ref QueryStatusResult result)
		{
			if (guidCmdGroup == Microsoft.VisualStudio.Shell.VsMenus.guidStandardCommandSet97)
			{
				switch ((VsCommands)cmd)
				{
					case VsCommands.AddNewItem:
					case VsCommands.AddExistingItem:
					case VsCommands.ViewCode:
                    //case VsCommands.Delete: // enable Delete command
						result |= QueryStatusResult.SUPPORTED | QueryStatusResult.ENABLED;
						return VSConstants.S_OK;
					case VsCommands.ViewForm:
						if (IsWinFormsDesignerSubType)
							result |= QueryStatusResult.SUPPORTED | QueryStatusResult.ENABLED;
						return VSConstants.S_OK;
				}
			}

			else if (guidCmdGroup == PhalangerMenus.guidPhalangerProjectCmdSet)
			{
				if (cmd == (uint)PhalangerMenus.SetAsMain.ID)
				{
					result |= QueryStatusResult.SUPPORTED | QueryStatusResult.ENABLED;
					return VSConstants.S_OK;
				}
			}
			return base.QueryStatusOnNode(guidCmdGroup, cmd, pCmdText, ref result);
		}

        /// <summary>Returns a specific Document manager to handle files</summary>
        /// <returns>Document manager object</returns>
        /*protected internal override DocumentManager GetDocumentManager() {
            return new PhalangerFileDocumentManager(this);
        }*/
		#endregion
		
		#region Helper Methods

		internal string GetRelativePath()
		{
			string relativePath = Path.GetFileName(this.ItemNode.GetMetadata(ProjectFileConstants.Include));
			HierarchyNode parent = this.Parent;
			while (parent != null && !(parent is ProjectNode))
			{
				relativePath = Path.Combine(parent.Caption, relativePath);
				parent = parent.Parent;
			}
			return relativePath;
		}

		internal OleServiceProvider.ServiceCreatorCallback ServiceCreator
		{
			get { return new OleServiceProvider.ServiceCreatorCallback(this.CreateServices); }
		}

		private object CreateServices(Type serviceType)
		{
			object service = null;
			if (typeof(EnvDTE.ProjectItem) == serviceType)
			{
				service = GetAutomationObject();
			}
			else if (typeof(DesignerContext) == serviceType)
			{
				service = this.DesignerContext;
			}
			return service;
		}
		#endregion
	}
}
