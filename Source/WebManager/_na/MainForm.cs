/*

Copyright (c) 2005-2006 Tomas Matousek.

The use and distribution terms for this software are contained in the file named License.txt, 
which can be found in the root of the Phalanger distribution. By using this software 
in any fashion, you are agreeing to be bound by the terms of this license.

You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Diagnostics;
using System.DirectoryServices;
using System.Threading;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using WebManager.Controls;
//using Microsoft.Win32.Security;

namespace WebManager
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class MainForm : System.Windows.Forms.Form
	{
		private System.Windows.Forms.MainMenu mainMenu1;
		private System.Windows.Forms.MenuItem menuItem1;
		private System.Windows.Forms.Button Reload;

		private WebManager WebManager = new WebManager();
		private System.Windows.Forms.TextBox ServerName;
		private System.Windows.Forms.Label ServerLabel;
		private System.Windows.Forms.Panel panel3;
		private System.Windows.Forms.ImageList SmallImageList;
		private System.Windows.Forms.Splitter splitter2;
		private System.Windows.Forms.Panel panel4;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.TabControl tabControl;
		private System.Windows.Forms.TabPage tabDump;
		private System.Windows.Forms.RichTextBox NodeDump;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TreeView SitesTreeView;
		private System.Windows.Forms.TabPage tabWebApp;
		private System.Windows.Forms.ContextMenu contextMenu1;
		private System.Windows.Forms.MenuItem miDelete;
		private System.Windows.Forms.MenuItem miCreate;
		private System.Windows.Forms.MenuItem miClearMapping;
		private System.Windows.Forms.MenuItem menuItem4;
		private System.Windows.Forms.MenuItem miRepair;
		private System.Windows.Forms.MenuItem miAddMapping;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox tbPhysicalPath;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TextBox tbAppRoot;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.ListBox lbSecurity;
		private System.ComponentModel.IContainer components;
		private Button SaveBtn;
		private ConfigManager confmgr;

		public MainForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			WebManager.Server = ServerName.Text.Trim();
			WebManager.PopulateNodes(SitesTreeView.Nodes);
			SitesTreeView.Sorted = true;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this.mainMenu1 = new System.Windows.Forms.MainMenu(this.components);
			this.menuItem1 = new System.Windows.Forms.MenuItem();
			this.Reload = new System.Windows.Forms.Button();
			this.ServerName = new System.Windows.Forms.TextBox();
			this.ServerLabel = new System.Windows.Forms.Label();
			this.panel3 = new System.Windows.Forms.Panel();
			this.SaveBtn = new System.Windows.Forms.Button();
			this.SmallImageList = new System.Windows.Forms.ImageList(this.components);
			this.splitter2 = new System.Windows.Forms.Splitter();
			this.panel4 = new System.Windows.Forms.Panel();
			this.SitesTreeView = new System.Windows.Forms.TreeView();
			this.contextMenu1 = new System.Windows.Forms.ContextMenu();
			this.miDelete = new System.Windows.Forms.MenuItem();
			this.miCreate = new System.Windows.Forms.MenuItem();
			this.menuItem4 = new System.Windows.Forms.MenuItem();
			this.miClearMapping = new System.Windows.Forms.MenuItem();
			this.miAddMapping = new System.Windows.Forms.MenuItem();
			this.miRepair = new System.Windows.Forms.MenuItem();
			this.label1 = new System.Windows.Forms.Label();
			this.panel1 = new System.Windows.Forms.Panel();
			this.tabControl = new System.Windows.Forms.TabControl();
			this.tabWebApp = new System.Windows.Forms.TabPage();
			this.lbSecurity = new System.Windows.Forms.ListBox();
			this.label4 = new System.Windows.Forms.Label();
			this.tbPhysicalPath = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.tbAppRoot = new System.Windows.Forms.TextBox();
			this.tabDump = new System.Windows.Forms.TabPage();
			this.NodeDump = new System.Windows.Forms.RichTextBox();
			this.panel3.SuspendLayout();
			this.panel4.SuspendLayout();
			this.panel1.SuspendLayout();
			this.tabControl.SuspendLayout();
			this.tabWebApp.SuspendLayout();
			this.tabDump.SuspendLayout();
			this.SuspendLayout();
			// 
			// mainMenu1
			// 
			this.mainMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
			this.menuItem1});
			// 
			// menuItem1
			// 
			this.menuItem1.Index = 0;
			this.menuItem1.Text = "";
			// 
			// Reload
			// 
			this.Reload.Location = new System.Drawing.Point(160, 8);
			this.Reload.Name = "Reload";
			this.Reload.Size = new System.Drawing.Size(56, 23);
			this.Reload.TabIndex = 1;
			this.Reload.Text = "Reload";
			this.Reload.Click += new System.EventHandler(this.button1_Click);
			// 
			// ServerName
			// 
			this.ServerName.Location = new System.Drawing.Point(56, 8);
			this.ServerName.Name = "ServerName";
			this.ServerName.Size = new System.Drawing.Size(100, 20);
			this.ServerName.TabIndex = 3;
			this.ServerName.Text = "localhost";
			// 
			// ServerLabel
			// 
			this.ServerLabel.Location = new System.Drawing.Point(8, 8);
			this.ServerLabel.Name = "ServerLabel";
			this.ServerLabel.Size = new System.Drawing.Size(48, 16);
			this.ServerLabel.TabIndex = 4;
			this.ServerLabel.Text = "Server:";
			this.ServerLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// panel3
			// 
			this.panel3.Controls.Add(this.SaveBtn);
			this.panel3.Controls.Add(this.ServerLabel);
			this.panel3.Controls.Add(this.Reload);
			this.panel3.Controls.Add(this.ServerName);
			this.panel3.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel3.Location = new System.Drawing.Point(0, 0);
			this.panel3.Name = "panel3";
			this.panel3.Size = new System.Drawing.Size(744, 40);
			this.panel3.TabIndex = 9;
			// 
			// SaveBtn
			// 
			this.SaveBtn.Location = new System.Drawing.Point(657, 11);
			this.SaveBtn.Name = "SaveBtn";
			this.SaveBtn.Size = new System.Drawing.Size(75, 23);
			this.SaveBtn.TabIndex = 5;
			this.SaveBtn.Text = "Save";
			this.SaveBtn.UseVisualStyleBackColor = true;
			this.SaveBtn.Visible = false;
			this.SaveBtn.Click += new System.EventHandler(this.button1_Click_4);
			// 
			// SmallImageList
			// 
			this.SmallImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("SmallImageList.ImageStream")));
			this.SmallImageList.TransparentColor = System.Drawing.Color.Transparent;
			this.SmallImageList.Images.SetKeyName(0, "");
			this.SmallImageList.Images.SetKeyName(1, "");
			this.SmallImageList.Images.SetKeyName(2, "");
			this.SmallImageList.Images.SetKeyName(3, "");
			// 
			// splitter2
			// 
			this.splitter2.Location = new System.Drawing.Point(237, 0);
			this.splitter2.Name = "splitter2";
			this.splitter2.Size = new System.Drawing.Size(3, 609);
			this.splitter2.TabIndex = 1;
			this.splitter2.TabStop = false;
			// 
			// panel4
			// 
			this.panel4.Controls.Add(this.SitesTreeView);
			this.panel4.Controls.Add(this.label1);
			this.panel4.Dock = System.Windows.Forms.DockStyle.Left;
			this.panel4.Location = new System.Drawing.Point(0, 0);
			this.panel4.Name = "panel4";
			this.panel4.Size = new System.Drawing.Size(237, 609);
			this.panel4.TabIndex = 14;
			// 
			// SitesTreeView
			// 
			this.SitesTreeView.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.SitesTreeView.ContextMenu = this.contextMenu1;
			this.SitesTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.SitesTreeView.HideSelection = false;
			this.SitesTreeView.ImageIndex = 0;
			this.SitesTreeView.ImageList = this.SmallImageList;
			this.SitesTreeView.Location = new System.Drawing.Point(0, 22);
			this.SitesTreeView.Name = "SitesTreeView";
			this.SitesTreeView.SelectedImageIndex = 0;
			this.SitesTreeView.Size = new System.Drawing.Size(237, 587);
			this.SitesTreeView.TabIndex = 5;
			this.SitesTreeView.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.SitesTreeView_BeforeExpand);
			this.SitesTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.SitesTreeView_AfterSelect);
			this.SitesTreeView.MouseDown += new System.Windows.Forms.MouseEventHandler(this.SitesTreeView_MouseDown);
			this.SitesTreeView.Click += new System.EventHandler(this.SitesTreeView_Click);
			// 
			// contextMenu1
			// 
			this.contextMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
			this.miDelete,
			this.miCreate,
			this.menuItem4,
			this.miClearMapping,
			this.miAddMapping,
			this.miRepair});
			this.contextMenu1.Popup += new System.EventHandler(this.contextMenu1_Popup);
			// 
			// miDelete
			// 
			this.miDelete.Index = 0;
			this.miDelete.Text = "Delete";
			this.miDelete.Click += new System.EventHandler(this.miDelete_Click);
			// 
			// miCreate
			// 
			this.miCreate.Index = 1;
			this.miCreate.Text = "Create";
			this.miCreate.Click += new System.EventHandler(this.miCreate_Click);
			// 
			// menuItem4
			// 
			this.menuItem4.Index = 2;
			this.menuItem4.Text = "-";
			// 
			// miClearMapping
			// 
			this.miClearMapping.Index = 3;
			this.miClearMapping.Text = "Clear Mapping";
			this.miClearMapping.Click += new System.EventHandler(this.miClearMapping_Click);
			// 
			// miAddMapping
			// 
			this.miAddMapping.Index = 4;
			this.miAddMapping.Text = "Add Mapping";
			this.miAddMapping.Click += new System.EventHandler(this.miSetMapping_Click);
			// 
			// miRepair
			// 
			this.miRepair.Index = 5;
			this.miRepair.Text = "Repair ASP.NET";
			this.miRepair.Click += new System.EventHandler(this.miRepair_Click);
			// 
			// label1
			// 
			this.label1.BackColor = System.Drawing.SystemColors.AppWorkspace;
			this.label1.Dock = System.Windows.Forms.DockStyle.Top;
			this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
			this.label1.ForeColor = System.Drawing.Color.White;
			this.label1.Location = new System.Drawing.Point(0, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(237, 22);
			this.label1.TabIndex = 4;
			this.label1.Text = " Web Sites";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.label1.Click += new System.EventHandler(this.label1_Click);
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.tabControl);
			this.panel1.Controls.Add(this.splitter2);
			this.panel1.Controls.Add(this.panel4);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel1.Location = new System.Drawing.Point(0, 40);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(744, 609);
			this.panel1.TabIndex = 12;
			// 
			// tabControl
			// 
			this.tabControl.Controls.Add(this.tabWebApp);
			this.tabControl.Controls.Add(this.tabDump);
			this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabControl.ItemSize = new System.Drawing.Size(141, 18);
			this.tabControl.Location = new System.Drawing.Point(240, 0);
			this.tabControl.Name = "tabControl";
			this.tabControl.SelectedIndex = 0;
			this.tabControl.Size = new System.Drawing.Size(504, 609);
			this.tabControl.TabIndex = 15;
			this.tabControl.SelectedIndexChanged += new System.EventHandler(this.tabControl_SelectedIndexChanged);
			// 
			// tabWebApp
			// 
			this.tabWebApp.Controls.Add(this.lbSecurity);
			this.tabWebApp.Controls.Add(this.label4);
			this.tabWebApp.Controls.Add(this.tbPhysicalPath);
			this.tabWebApp.Controls.Add(this.label2);
			this.tabWebApp.Controls.Add(this.label3);
			this.tabWebApp.Controls.Add(this.tbAppRoot);
			this.tabWebApp.Location = new System.Drawing.Point(4, 22);
			this.tabWebApp.Name = "tabWebApp";
			this.tabWebApp.Size = new System.Drawing.Size(496, 583);
			this.tabWebApp.TabIndex = 2;
			this.tabWebApp.Text = "Web Application";
			// 
			// lbSecurity
			// 
			this.lbSecurity.Location = new System.Drawing.Point(88, 104);
			this.lbSecurity.Name = "lbSecurity";
			this.lbSecurity.Size = new System.Drawing.Size(232, 238);
			this.lbSecurity.TabIndex = 4;
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(8, 104);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(80, 23);
			this.label4.TabIndex = 3;
			this.label4.Text = "Read privs.:";
			this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// tbPhysicalPath
			// 
			this.tbPhysicalPath.Location = new System.Drawing.Point(88, 56);
			this.tbPhysicalPath.Name = "tbPhysicalPath";
			this.tbPhysicalPath.ReadOnly = true;
			this.tbPhysicalPath.Size = new System.Drawing.Size(384, 20);
			this.tbPhysicalPath.TabIndex = 2;
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(8, 56);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(80, 23);
			this.label2.TabIndex = 1;
			this.label2.Text = "Physical path:";
			this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(8, 16);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(80, 23);
			this.label3.TabIndex = 1;
			this.label3.Text = "App. Root:";
			this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// tbAppRoot
			// 
			this.tbAppRoot.Location = new System.Drawing.Point(88, 16);
			this.tbAppRoot.Name = "tbAppRoot";
			this.tbAppRoot.ReadOnly = true;
			this.tbAppRoot.Size = new System.Drawing.Size(384, 20);
			this.tbAppRoot.TabIndex = 2;
			// 
			// tabDump
			// 
			this.tabDump.Controls.Add(this.NodeDump);
			this.tabDump.Location = new System.Drawing.Point(4, 22);
			this.tabDump.Name = "tabDump";
			this.tabDump.Size = new System.Drawing.Size(496, 583);
			this.tabDump.TabIndex = 1;
			this.tabDump.Text = "Dump";
			this.tabDump.Visible = false;
			// 
			// NodeDump
			// 
			this.NodeDump.Dock = System.Windows.Forms.DockStyle.Fill;
			this.NodeDump.Font = new System.Drawing.Font("Courier New", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
			this.NodeDump.Location = new System.Drawing.Point(0, 0);
			this.NodeDump.Name = "NodeDump";
			this.NodeDump.Size = new System.Drawing.Size(496, 583);
			this.NodeDump.TabIndex = 11;
			this.NodeDump.Text = "";
			// 
			// MainForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(744, 649);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.panel3);
			this.Menu = this.mainMenu1;
			this.Name = "MainForm";
			this.Text = "Phalanger Web Applications Manager";
			this.panel3.ResumeLayout(false);
			this.panel3.PerformLayout();
			this.panel4.ResumeLayout(false);
			this.panel1.ResumeLayout(false);
			this.tabControl.ResumeLayout(false);
			this.tabWebApp.ResumeLayout(false);
			this.tabWebApp.PerformLayout();
			this.tabDump.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		private void ReloadSiteNodes()
		{
			WebManager.Server = ServerName.Text.Trim();
			//TODO: WebManager.PopulateNodes(SitesTreeView.Nodes);
			//SitesTreeView.ExpandAll();
		}

		private void ReloadCurrentNodeDump()
		{
			Cursor cursor = this.Cursor;
			this.Cursor = Cursors.WaitCursor;
			NodeTag tag = GetCurrentTag();
			NodeDump.Text = (tag != null) ? WebManager.DumpProperties(tag) : "";
			this.Cursor = cursor;
		}

		public NodeTag GetCurrentSiteRootTag()
		{
			TreeNode node = SitesTreeView.SelectedNode;

			while (node != null && node.Parent != null)
				node = node.Parent;

			// no node is selected:
			if (node == null)
				node = SitesTreeView.TopNode;

			NodeTag result = (NodeTag)node.Tag;

			Debug.Assert(result.Entry.Name == "ROOT");

			return result;
		}


		private NodeTag GetCurrentTag()
		{
			if (SitesTreeView.SelectedNode == null) return null;
			return (NodeTag)SitesTreeView.SelectedNode.Tag;
		}

		private void DeleteCurrentApplication()
		{
			NodeTag tag = GetCurrentTag();
			if (tag == null) return;

			WebManager.DeleteApplication(tag);
		}

		private void CreateApplication()
		{
			NodeTag tag = GetCurrentTag();
			if (tag == null) return;

			WebManager.CreateApplication(tag);
		}

		private void GlobalExceptionHandler(object sender, ThreadExceptionEventArgs e)
		{
			// TODO:
			MessageBox.Show("An error occured: " + e.Exception, "Error", MessageBoxButtons.OK);
		}

		private void button1_Click(object sender, System.EventArgs e)
		{
			ReloadSiteNodes();
			SitesTreeView.Refresh();
		}

		private void button1_Click_1(object sender, System.EventArgs e)
		{
		}

		private void button2_Click(object sender, System.EventArgs e)
		{
			//WebManager.DumpSites();
			//Console.Text = WebManager.ConsoleOutput;
		}

		private void tabDump_Click(object sender, System.EventArgs e)
		{

		}

		private void tabControl_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if (SitesTreeView.SelectedNode != null)
			{
				if (tabControl.SelectedTab == tabDump)
				{
					if (NodeDump.Text == "")
					{
						ReloadCurrentNodeDump();
					}
				}
			}
			else
			{
				// empty tab:
				NodeDump.Text = "";
			}
		}

		private void RemovePhpTabs()
		{
			for (int i = this.tabControl.Controls.Count - 1; i >= 0; i--)
			{
				Control c = this.tabControl.Controls[i];
				if (c is TabPage)
				{
					TabPage tp = (TabPage)c;
					if (tp.Name != "tabWebApp" && tp.Name != "tabDump")
						this.tabControl.Controls.Remove(c);
				}
			}

			return;
		}

		private void populateTab(TabPage tp, List<GroupBox> groups)
		{

			FlowLayoutPanel flp = new FlowLayoutPanel();
			tp.Controls.Add(flp);
			flp.AutoSize = true;
			//flp.Height = tabControl.Height;
			flp.Width = 400;
			flp.FlowDirection = FlowDirection.TopDown;
			foreach (GroupBox gb in groups)
				flp.Controls.Add(gb);
		}

		private void AddPhpTabs()
		{
			string[] keyColl = confmgr.GetTabPages();
			foreach (string s in keyColl)
			{
				TabPage tp = new TabPage();
				tp.Name = "tab" + s;
				tp.Text = s;
				tp.AutoScroll = true;
				//populateTab(tp,confmgr.GetTabPageContent(s));
				//this.tabControl.Controls.Add(tp);
			}
			
			// this.tabControl.Controls.Add(this.tabExts);
			// this.tabControl.Controls.Add(this.tabCompiler);
			return;
		}


		private void SitesTreeView_AfterSelect(object sender, System.Windows.Forms.TreeViewEventArgs e)
		{
			NodeTag tag = GetCurrentTag();
			if (tag == null) return;

			// resets data on tabs:
			NodeDump.Text = "";
			SaveBtn.Visible = false;

			if (tag.IsPhpApp)
			{
				if (tag.ContainsPhpWebConfig)
				{
					confmgr = new ConfigManager(tag.ConfigFilePath);
					SaveBtn.Visible = true;
					RemovePhpTabs();
					AddPhpTabs();

				}
				tabWebApp.Text = "Phalanger Web Application";
				
			}
			else if (tag.IsWebApp)
			{
				tabWebApp.Text = "ASP.NET Web Application";
			}
			else if (tag.IsVirtual)
			{
				tabWebApp.Text = "Virtual Directory";
			}
			else
			{
				tabWebApp.Text = "Directory";
			}

			if (!tag.ContainsPhpWebConfig)
				RemovePhpTabs();

			tbPhysicalPath.Text = tag.PhysicalPath;
			tbAppRoot.Text = (tag.Entry != null) ? WebManager.GetPropertyString(tag.Entry, "AppRoot") : "";

			//SecurityDescriptor desc;
			//lbSecurity.Items.Clear();
			//try
			//{
			//	if (Directory.Exists(tag.PhysicalPath))
			//	{	
			//		desc = SecurityDescriptor.GetFileSecurity(tag.PhysicalPath);

			//		foreach (Ace ace in desc.Dacl)
			//		{
			//			if ((ace.AccessType & (AccessType.GENERIC_READ | AccessType.GENERIC_ALL | AccessType.STANDARD_RIGHTS_ALL
			//			 | AccessType.STANDARD_RIGHTS_READ))!=0)
			//				lbSecurity.Items.Add(ace.Sid.AccountName);
			//		}
			//	}
			//}
			//catch(Exception ex)
			//{
			//	lbSecurity.Items.Add(ex.ToString());
			//}	

			//tbUrl.Text = String.Concat("http://",WebManager.Server,"/",tag.GetRelativeUrl()); 

			if (tabControl.SelectedTab == tabDump)
				ReloadCurrentNodeDump();
		}

		private void SitesTreeView_Click(object sender, System.EventArgs e)
		{

		}

		private void contextMenu1_Popup(object sender, System.EventArgs e)
		{
			NodeTag tag = GetCurrentTag();
			miDelete.Enabled = tag.IsPhpApp;
			miCreate.Enabled = !tag.IsPhpApp;
			miRepair.Enabled = miAddMapping.Enabled = miClearMapping.Enabled = tag.Entry != null;
		}

		private void miDelete_Click(object sender, System.EventArgs e)
		{
			DeleteCurrentApplication();
		}

		private void SitesTreeView_BeforeExpand(object sender, System.Windows.Forms.TreeViewCancelEventArgs e)
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

		private void miCreate_Click(object sender, System.EventArgs e)
		{
			CreateApplication();
		}

		private void SitesTreeView_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
		{

		}

		private void miClearMapping_Click(object sender, System.EventArgs e)
		{
			WebManager.ClearPhpMapping(GetCurrentTag());
		}

		private void miSetMapping_Click(object sender, System.EventArgs e)
		{
			WebManager.AddPhpMapping(GetCurrentTag());
		}

		private void miRepair_Click(object sender, System.EventArgs e)
		{
			WebManager.RepairMapping(GetCurrentTag());
		}

		private void button1_Click_4(object sender, EventArgs e)
		{

			confmgr.SaveToFile();
		}

		private void label1_Click(object sender, EventArgs e)
		{

		}
	}
}
