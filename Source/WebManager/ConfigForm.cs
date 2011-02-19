using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Xml;

namespace WebManager
{
	
	/// <summary>
	///		Web config window class
	/// </summary>
	public partial class ConfigForm : Form
	{
		private string ServerName = "localhost";
		private WebManager WebManager = new WebManager();
		private ConfigManager confmgr;

		public ConfigForm()
		{
			InitializeComponent();
			WebManager.Server = ServerName.Trim(); //Will be set later
			WebManager.PopulateNodes(sitesTreeView.Nodes);
			bool a = WebManager.IsRunningIIS7;
			sitesTreeView.Sorted = true;
			panelSideConfig.Visible = false;
			panelSidePhpApp.Visible = false;
			panelSideWebApp.Visible = false;
		}


		private void ReloadSiteNodes()
		{
			WebManager.Server = ServerName.Trim();
			//TODO: WebManager.PopulateNodes(SitesTreeView.Nodes);
			//SitesTreeView.ExpandAll();
		}

		private void ReloadCurrentNodeDump()
		{
			Cursor cursor = this.Cursor;
			this.Cursor = Cursors.WaitCursor;
			NodeTag tag = GetCurrentTag();
			// NodeDump.Text = (tag != null) ? WebManager.DumpProperties(tag) : ""; 
			//Informace o node
			this.Cursor = cursor;
		}

		private NodeTag GetCurrentTag()
		{
			if (sitesTreeView.SelectedNode == null) return null;
			return (NodeTag)sitesTreeView.SelectedNode.Tag;
		}

		private void DeleteCurrentApplication()
		{
			NodeTag tag = GetCurrentTag();
			if (tag == null) return;

			WebManager.DeleteApplication(tag);
		}

		/// <summary>
		///	 Creates a php application from selected project
		/// </summary>
		private void CreateApplication()
		{
			NodeTag tag = GetCurrentTag();
			if (tag == null) return;

			WebManager.CreateApplication(tag);
			//SitesTreeView_AfterSelect(this, null);
			if (tag.ConfigFilePath == null)
			{
				ConfigManager.CreateConfigFile(tag.PhysicalPath);
				confmgr = new ConfigManager(tag.PhysicalPath + "\\web.config");
			}
			else
				confmgr.CreatePHPMapping();
			
			tag.UpdateNode();
		}

		/// <summary>
		///	 Deletes web application
		/// </summary>
		private void RemoveApplication()
		{
			NodeTag tag = GetCurrentTag();
			if (tag == null) return;

			WebManager.DeleteApplication(tag);
		}

		private void GlobalExceptionHandler(object sender, ThreadExceptionEventArgs e)
		{
			// TODO:
			MessageBox.Show("An error occured: " + e.Exception, "Error", MessageBoxButtons.OK);
		}

		private void reloadBtn_Click(object sender, EventArgs e)
		{
			ReloadSiteNodes();
			sitesTreeView.Refresh();
		}

		private void SitesTreeView_MouseDown(object sender, MouseEventArgs e)
		{
			// before context menu popup:
			if (e.Button == MouseButtons.Right)
			{
				sitesTreeView.SelectedNode = (TreeNode)sitesTreeView.GetNodeAt(e.X, e.Y);
			}
		}

		private void SitesTreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
		{
			NodeTag tag = (NodeTag)e.Node.Tag;
			if (tag.LazyPopulate)
			{
				Cursor cursor = this.Cursor;
				this.Cursor = Cursors.WaitCursor;
				WebManager.PopulateNode(tag, false);
				this.Cursor = cursor;
			}
		}

		private void PopulateOptionListView()
		{
			optionList.Items.Clear();
			string[] keyColl = confmgr.GetTabPages();
			foreach (string name in keyColl)
			{
				ListViewGroup lvg = new ListViewGroup(name,name);
				optionList.Groups.Add(lvg);

				foreach (ListViewItem lvi in confmgr.GetTabPageContent(name))
				{
					lvi.Group = lvg;
					optionList.Items.Add(lvi);
				}
			}

			return;
		}

		/// <summary>
		///	 Sets what side panel will be visible. All others will be hidden
		/// </summary>
		/// <param name="sidepanelname">String name of the side panel</param>
		private void SetSidePanelVisible(string sidepanelname)
		{
			panelSideWebApp.Visible = false;
			panelSidePhpApp.Visible = false;
			panelSideConfig.Visible = false;
			switch (sidepanelname)
			{
				case "panelSidePhpApp":
					panelSidePhpApp.Visible = true;
					break;
				case "panelSideWebApp":
					panelSideWebApp.Visible = true;
					break;
				case "panelSideConfig":
					panelSideConfig.Visible = true;
					break;
				default:
					break;
			}
		}

		private bool isRootItem(NodeTag nt)
		{
			foreach (TreeNode n in sitesTreeView.Nodes)
			{
				if (n.Tag == nt && n.Parent == null)
					return true;
			}
			return false;
		}

		private void SitesTreeView_AfterSelect(object sender, TreeViewEventArgs e)
		{
			optionList.Clear();
			NodeTag tag = GetCurrentTag();
			if (tag == null) return;

			setsidepanel();
		   
			if (tag.IsPhpApp)
			{
				if (tag.ConfigFilePath == null)
				{
					ConfigManager.CreateConfigFile(tag.PhysicalPath);
					tag.UpdateNode();
				}
				if (tag.ContainsPhpWebConfig)
				{
					confmgr = new ConfigManager(tag.ConfigFilePath);
					SetSidePanelVisible("panelSidePhpApp");
					PopulateOptionListView();
				}
				titlelbl.Text = "Phalanger Web Application";
			}
			else if (tag.IsWebApp)
			{
				titlelbl.Text = "ASP.NET Web Application";
				confmgr = new ConfigManager(tag.ConfigFilePath);
			}
			else if (tag.IsVirtual)
			{
				titlelbl.Text = "Virtual Directory";
			}
			else
			{
				titlelbl.Text = "Directory";
			}

			homePanel.Visible = true;
			settingspanel.Visible = false; 
		}

		private void LoadSettingPanel(XmlNode xn)
		{
			GroupBox g = confmgr.GetGroupInfo(xn);
			grouppanel.Controls.Clear();
			grouppanel.Controls.Add(g);
		}


		private void optionList_DoubleClick(object sender, EventArgs e)
		{
			homePanel.Visible = false;
			settingspanel.Visible = true;
			SetSidePanelVisible("panelSideConfig");
		}

		private void optionList_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (optionList.SelectedItems.Count > 0)
			{
				ListViewItem selected = optionList.SelectedItems[0];
				settinglbl.Text = selected.Text;
				LoadSettingPanel((XmlNode)selected.Tag);
				confmgr.LoadFile();
			}
		}

		private void resetlbl_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			ListViewItem selected = optionList.SelectedItems[0];
			settinglbl.Text = selected.Text;
			confmgr.LoadFile();
			
		}

		private void clearalllnk_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			ListViewItem selected = optionList.SelectedItems[0];
			LoadSettingPanel((XmlNode)selected.Tag);
		}


		private void backlbl_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			homePanel.Visible = true;
			settingspanel.Visible = false;
			setsidepanel();
		}

		/// <summary>
		///		Sets the sidepanel
		/// </summary>
		private void setsidepanel()
		{
			NodeTag tag = GetCurrentTag();
			tag.UpdateNode();
			if (tag == null) return;
			SetSidePanelVisible("panelSideWebApp");
			if (tag.IsPhpApp)
			{
				if (tag.ContainsPhpWebConfig)
				{
					SetSidePanelVisible("panelSidePhpApp");
				}
				titlelbl.Text = "Phalanger Web Application";
			}
		}

		private void optionList_Click(object sender, EventArgs e)
		{
			setsidepanel();
		}

		private void saveFile()
		{
			confmgr.SaveToFile();
			confmgr.LoadFile();
		}

		private void savelnk_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			saveFile();
		}

		/// <summary>
		///	 Ocurse before changing the selection of the tree element. Checks if the current selection is saved
		///	 and promts the user if it isnt. 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void sitesTreeView_BeforeSelect(object sender, TreeViewCancelEventArgs e)
		{
			if (confmgr != null && confmgr.Changed)
			{
				switch (
				MessageBox.Show("You have made unsaved changes. Do you wish to save the changes?", "Unsaved Changes", MessageBoxButtons.YesNoCancel)
					)
				{
					case DialogResult.Cancel:
						e.Cancel = true;
						break;
					case DialogResult.Yes:
						saveFile();
						break;
					case DialogResult.No:
						confmgr.Changed = false;
						break;
					default:
						e.Cancel = true;
						break;
				}
			}
		}

		private void linkLabel4_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			CreateApplication();
			SitesTreeView_AfterSelect(sender, null);
		}

		private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			ClearPhpMapping();
			SitesTreeView_AfterSelect(sender, null);

			
		}

		private void ClearPhpMapping()
		{
			NodeTag tag = GetCurrentTag();
			if (tag == null) return;

			confmgr.RemovePhpMapping();
			WebManager.ClearPhpMapping(tag);
		}

		private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			confmgr.LoadFile(ConfigManager.DefaultFile);
		}

		private void ConfigForm_Load(object sender, EventArgs e)
		{
			panelSidePhpApp.Dock = panelSideConfig.Dock = panelSideWebApp.Dock = DockStyle.Fill;
			panel1.Height = 0;
		}

		private void actionPanel_Paint(object sender, PaintEventArgs e)
		{

		}
	}
}