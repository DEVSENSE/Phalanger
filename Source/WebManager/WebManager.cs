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
using System.Collections;
using System.Management;
using System.DirectoryServices;
using System.Xml;
using System.Web;
using System.Drawing;
using System.Windows.Forms;
//using Microsoft.Win32.Security;

namespace WebManager
{
	/// <summary>
	/// Represents node in the IIS tree view. 
	/// Contains information about the directory.
	/// </summary>
	public class NodeTag
	{
		#region Constants

		const int SiteIconIndex = 0;
		const int WebDirIconIndex = 1;
		const int WebAppIconIndex = 2;
		const int WebVirtualDirIconIndex = 3;

		#endregion
		#region Properties

		public bool LazyPopulate { get { return lazyPopulate; } set { lazyPopulate = value; } }
		private bool lazyPopulate;

		public TreeNode Node { get { return node; } }
		private TreeNode node;

		public DirectoryEntry Entry { get { return entry; } set { entry = value; } }
		private DirectoryEntry entry;

		public string/*!*/ PhysicalPath { get { return physicalPath; } }
		private string/*!*/ physicalPath;

		private bool containsPhpConfig = false;
		public bool ContainsPhpWebConfig
		{
			get { return containsPhpConfig; }
		}

		private string configFilePath;

		public string ConfigFilePath
		{
			get { return configFilePath; }
		}

		public bool IsPhpApp { get { return isPhpApp; } }
		private bool isPhpApp = false;

		public bool IsWebApp { get { return isWebApp; } }
		private bool isWebApp = false;

		public bool IsSiteRoot { get { return isSiteRoot; } }
		private bool isSiteRoot = false;

		public bool IsVirtual { get { return isVirtual; } }
		private bool isVirtual = false;

		public bool IsAccessible { get { return isAccessible; } }
		private bool isAccessible = false;

		#endregion
		#region Constructor/Methods

		/// <summary>
		///	 Looks for a config file in the entry directory
		/// </summary>
		/// <returns>Physical path if config file is found. Null if isnt</returns>
		private string FindConfigFile()
		{
			DirectoryInfo di = new DirectoryInfo(this.physicalPath);
			if (di.Exists)
			foreach (FileInfo fi in di.GetFiles())
				if (fi.Name.ToUpper() == "WEB.CONFIG")
				{
					containsPhpConfig = true;
					return this.physicalPath + "\\" +fi.Name;
				}
			return null;
		}

		public NodeTag(TreeNode/*!*/ node, string/*!*/ physicalPath, DirectoryEntry entry, bool lazyPopulate, bool isSiteRoot)
		{
			// read path from DirectoryServices
			string path = Path.GetFullPath(physicalPath);
			if (entry != null && entry.Properties["Path"].Value != null) path = (string)entry.Properties["Path"].Value;
			this.physicalPath = path;
			this.node = node;
			this.entry = entry;
			this.lazyPopulate = lazyPopulate;
			this.isSiteRoot = isSiteRoot;

			UpdateNode();
		}

		/// <summary>
		///		Determines if 
		/// </summary>
		/// <param name="tn"></param>
		/// <returns></returns>
		public bool HasPhpparent(TreeNode tn)
		{
			TreeNode parentnode = tn.Parent;

			if (parentnode == null)
                return false;

			NodeTag node = (NodeTag)parentnode.Tag;

			if (node.isPhpApp)
                return true;
			else
                return HasPhpparent(parentnode);
		}

		/// <summary>
		///		Updates the nodes parameters
		/// </summary>
		public void UpdateNode()
		{
			isWebApp = !isSiteRoot && entry != null && (WebAppStatus)entry.Invoke("AppGetStatus2") != WebAppStatus.NotDefined;
			isPhpApp = (isWebApp && (HasPhpparent(this.node) || HasPhpMapping(entry) || IsIIS7ConfigFile()));
			isPhpApp = isPhpApp || (IsSiteRoot && (HasPhpparent(this.node) || HasPhpMapping(entry) || IsIIS7ConfigFile()));
			
			
			isVirtual = entry != null && entry.SchemaClassName == WebManager.VirtualWebDirectorySchema;

			if (IsPhpApp)
				configFilePath = FindConfigFile();

			if (isVirtual)
				node.ForeColor = Color.Blue;
			else
				node.ForeColor = Color.Black;

			int icon;
			if (isSiteRoot) icon = SiteIconIndex;
			else if (isPhpApp) icon = WebAppIconIndex;
			else if (isWebApp) icon = WebVirtualDirIconIndex;
			else icon = WebDirIconIndex;

			node.ImageIndex = icon;
			node.SelectedImageIndex = icon;
		}

		private bool IsIIS7ConfigFile()
		{
			configFilePath = FindConfigFile();
			if (configFilePath != null && configFilePath != "")
			{
				XmlDocument xdoc = new XmlDocument();
				xdoc.Load(configFilePath);
				XmlNode xnode = xdoc.SelectSingleNode("configuration/system.webServer/handlers/add[@path='.php']");
				if (xnode != null) return true;
				xnode = xdoc.SelectSingleNode("configuration/system.webServer/handlers/add[@path='*.php']");
				if (xnode != null) return true;
			}
			return false;
		}

		private bool HasPhpMapping(DirectoryEntry/*!*/ entry)
		{


			foreach (string mapping in entry.Properties["ScriptMaps"])
			{
				int first_comma = mapping.IndexOf(',');
				if (first_comma == -1) continue;

				string ext = mapping.Substring(1, first_comma - 1);

				foreach (string extension in WebManager.Extensions)
				{
					if (String.Compare(extension, ext, true) == 0)
					{
						int second_comma = mapping.IndexOf(',', first_comma + 1);
						if (second_comma == -1) return false;

						string path = mapping.Substring(first_comma + 1, second_comma - first_comma - 1);
						return String.Compare(path, WebManager.IsapiPath, true) == 0;
					}
				}
			}
			return false;
		}

		#endregion
	}


	/// <summary>
	/// Status of web application in IIS
	/// </summary>
	public enum WebAppStatus
	{
		#region Values

		Stopped,
		Running,
		NotDefined

		#endregion
	}

	/// <summary>
	/// Summary description for WebManager.
	/// </summary>
	public class WebManager
	{
		#region Static Properties

		/// <summary>
		/// Determines if code is running on Windows NT v6+
		/// </summary>
		public static bool IsRunningIIS7
		{
			get { return (System.Environment.OSVersion.Version.Major >= 6); }
		}

		#endregion 

		#region Properties
		public string Server { get { return server; } set { server = value; } }
		public static string[] Extensions { get { return extensions; } set { extensions = value; } }
		public static readonly string IsapiPath = Path.Combine(HttpRuntime.ClrInstallDirectory, "aspnet_isapi.dll");
		#endregion 

		#region Variables
		private StringWriter console;
		private string server;
		private static string[] extensions = { "php" };

		public const string VirtualWebDirectorySchema = "IIsWebVirtualDir";
		public const string WebDirectorySchema = "IIsWebDirectory";
		public const string SiteSchema = "IIsWebServer";
		#endregion 

		#region Methods
		public WebManager()
		{
			console = new StringWriter();
		}

		/// <summary>
		///		Writes all properties od the directory entry into the console
		/// </summary>
		/// <param name="indent"></param>
		/// <param name="entry"></param>
		private void DumpAllProperties(string indent, DirectoryEntry entry)
		{
			entry.RefreshCache();

			try
			{
				foreach (string name in entry.Properties.PropertyNames)
				{
					console.Write("{0}	Name={1} Values=", indent, name);
					foreach (object value in entry.Properties[name])
					{
						console.Write(value);
						console.WriteLine(", ");
					}
					console.WriteLine();
				}
			}
			catch (Exception e)
			{
				console.WriteLine("Exception: {0}", e.Message);
			}
		}

		private void DumpProperties(string indent, DirectoryEntry entry)
		{
			entry.RefreshCache();

			console.WriteLine("{0}Schema='{1}'", indent, entry.SchemaClassName);
			console.WriteLine("{0}Name='{1}'", indent, entry.Name);
			console.WriteLine("{0}Path='{1}'", indent, entry.Path);
			console.WriteLine("{0}FriendlyName='{1}'", indent, GetPropertyString(entry, "AppFriendlyName"));
			console.WriteLine("{0}AppRoot='{1}'", indent, GetPropertyString(entry, "AppRoot"));
			console.WriteLine("{0}AnonymousUserName='{1}'", indent, GetPropertyString(entry, "AnonymousUserName"));

			console.WriteLine("{0}ScriptMaps:", indent);
			foreach (object value in entry.Properties["ScriptMaps"])
			{
				string[] mapping = ((string)value).Split(',');
				if (mapping.Length > 3)
				{
					console.WriteLine("{0}	{1} => {2} (check exists: {3})", indent, mapping[0], mapping[1],
						mapping[2] != "" && (Int32.Parse(mapping[2]) & 4) != 0 ? "yes" : "no");
				}
			}

			console.WriteLine("------------------------------");

			DumpAllProperties(indent, entry);
		}

		public string DumpProperties(NodeTag tag)
		{
			console = new StringWriter();

			console.WriteLine("Text='{0}'", tag.Node.Text);
			console.WriteLine("IsPhpApp='{0}'", tag.IsPhpApp);
			console.WriteLine("LazyPopulate='{0}'", tag.LazyPopulate);
			console.WriteLine("PhysicalPath='{0}'", tag.PhysicalPath);
			console.WriteLine();

			if (tag.Entry != null)
				DumpProperties("", tag.Entry);

			return console.ToString();
		}

		private void Dump(string indent, DirectoryEntry entry)
		{
			foreach (DirectoryEntry child in entry.Children)
			{
				console.WriteLine("{0}{2}:{1}",
					indent, child.Name, child.SchemaClassName);

				//DumpProperties(indent,child);
				DumpAllProperties(indent, child);
				//Dump(indent + "	",child);
			}
		}

		public static string GetPropertyString(DirectoryEntry/*!*/ entry, string/*!*/ name)
		{
			PropertyValueCollection values = entry.Properties[name];
			return (values.Count > 0) ? values[0] as string : null;
		}

		/// <summary>
		///		Adds the php mapping to the IIS project
		/// </summary>
		/// <param name="entry">entry in the active directory structure</param>
		private void AddPhpMapping(DirectoryEntry/*!*/ entry)
		{
			// add:
			if (!IsRunningIIS7)
			{
				foreach (string extension in extensions)
					entry.Properties["ScriptMaps"].Add(String.Format(".{0},{1},5,GET,HEAD,POST,DEBUG", extension, IsapiPath));
			}
		}

		private void ClearPhpMapping(DirectoryEntry/*!*/ entry)
		{
			// remove:
			if (!IsRunningIIS7)
			{
				for (int i = entry.Properties["ScriptMaps"].Count - 1; i >= 0; i--)
				{
					string mapping = (string)entry.Properties["ScriptMaps"][i];

					int first_comma = mapping.IndexOf(',');
					if (first_comma == -1) continue;

					string ext = mapping.Substring(1, first_comma - 1);

					foreach (string extension in extensions)
					{
						if (String.Compare(extension, ext, true) == 0)
							entry.Properties["ScriptMaps"].RemoveAt(i);
					}
				}
			}
		}

		private void RepairMapping(DirectoryEntry/*!*/ entry)
		{
			char[] slashes = { '\\', '/' };

			for (int i = 0; i < entry.Properties["ScriptMaps"].Count; i++)
			{
				string mapping = (string)entry.Properties["ScriptMaps"][i];
				string[] parts = mapping.Split(',');
				if (parts.Length < 2 || parts[1] == "") continue;

				int last_slash = parts[1].LastIndexOfAny(slashes);
				if (last_slash == -1) continue;

				string file_name = parts[1].Substring(last_slash + 1);

				if (String.Compare(file_name, "aspnet_isapi.dll", true) == 0)
				{
					parts[1] = IsapiPath;
					entry.Properties["ScriptMaps"][i] = String.Join(",", parts);
				}
			}
		}

		private TreeNode GetChildNodeByName(TreeNode/*!*/ node, string/*!*/ text)
		{
			foreach (TreeNode child_node in node.Nodes)
			{
				if (String.Compare(text, child_node.Text, true) == 0)
					return child_node;
			}
			return null;
		}

		public void DumpSites()
		{
			console = new StringWriter();

			Dump("", new DirectoryEntry("IIS://localhost/W3svc"));
		}



		private DirectoryEntry GetServiceEntry()
		{
			return new DirectoryEntry(String.Concat("IIS://", server, "/W3svc"));
		}

		public DirectoryEntry GetSiteRootEntry(string sitePath)
		{
			return new DirectoryEntry(String.Concat(sitePath, "/ROOT"));
		}

		public void PopulateNodes(TreeNodeCollection/*!*/ nodes)
		{
			nodes.Clear();

			DirectoryEntry de = GetServiceEntry();

			// enumerates web sites:
			foreach (DirectoryEntry site in GetServiceEntry().Children)
			{
				if (site.SchemaClassName == SiteSchema)
				{
					DirectoryEntry root = GetSiteRootEntry(site.Path);
					TreeNode root_node = new TreeNode(GetPropertyString(site, "ServerComment"));
					string path = GetPropertyString(root, "Path");
					NodeTag root_tag = new NodeTag(root_node, path, root, true, true);
					root_node.Tag = root_tag;
					root_node.Nodes.Add(new TreeNode());
					nodes.Add(root_node);
				}
			}
		}

		/// <summary>
		///		Populates the current node, gets all the information for its subnodes
		/// </summary>
		/// <param name="tag">Node</param>
		/// <param name="recurse">recursive call for all subnodes</param>
		public void PopulateNode(NodeTag tag, bool recurse)
		{
			tag.LazyPopulate = false;
			tag.Node.Nodes.Clear();
			Hashtable names = new Hashtable();

			if (tag.Entry != null)
			{
				tag.Entry.UsePropertyCache = false;

				foreach (DirectoryEntry child in tag.Entry.Children)
				{
					TreeNode child_node = new TreeNode(child.Name);
					//string child_path = String.Concat(tag.PhysicalPath, "/", child.Name);
					string child_path = GetPropertyString(child, "Path");
					NodeTag child_tag = new NodeTag(child_node, child_path, child, !recurse, false);
					child_node.Tag = child_tag;

					if (recurse)
						PopulateNode(child_tag, recurse);
					else
						child_node.Nodes.Add(new TreeNode());

					tag.Node.Nodes.Add(child_node);
					names.Add(child.Name, null);
				}
			}

			if (Directory.Exists(tag.PhysicalPath))
			{
				foreach (string dir_path in Directory.GetDirectories(tag.PhysicalPath))
				{
					string dir = Path.GetFileName(dir_path);

					if (!names.ContainsKey(dir))
					{
						TreeNode child_node = new TreeNode(dir);
						NodeTag child_tag = new NodeTag(child_node, dir_path, null, !recurse, false);
						child_node.Tag = child_tag;

						if (recurse)
							PopulateNode(child_tag, recurse);
						else
							child_node.Nodes.Add(new TreeNode());

						tag.Node.Nodes.Add(child_node);
						names.Add(dir, null);
					}
				}
			}
		}

		private void CreateEntriesRecursive(NodeTag/*!*/ leaf)
		{
			Debug.Assert(leaf.Entry == null);

			NodeTag parent = (NodeTag)leaf.Node.Parent.Tag;

			// create parent entries if not exist:
			if (parent.Entry == null)
				CreateEntriesRecursive(parent);

			leaf.Entry = (DirectoryEntry)parent.Entry.Invoke("Create", WebDirectorySchema, leaf.Node.Text);
			leaf.Entry.Invoke("Put", "EnableDefaultDoc", true);
			leaf.Entry.Invoke("Put", "DirBrowseFlags", 0xC000003E);
			leaf.Entry.Invoke("Put", "AccessRead", true);
			leaf.Entry.Invoke("Put", "AccessScript", true);
			leaf.Entry.Invoke("SetInfo");
		}

		/// <summary>
		///		Creates a php application
		/// </summary>
		/// <param name="tag">Selected project in the treeview</param>
		public void CreateApplication(NodeTag/*!*/ tag)
		{
			// metadata entry not exists:
			if (tag.Entry == null)
				CreateEntriesRecursive(tag);

			Debug.Assert(tag.Entry != null);

			// application not defined:
			if (!tag.IsWebApp)
			{
				tag.Entry.Invoke("AppCreate2", 2);
				tag.Entry.Invoke("Put", "AppFriendlyName", tag.Entry.Name);
				tag.Entry.Invoke("SetInfo");
			}

			// mapping:
			ClearPhpMapping(tag.Entry);
			AddPhpMapping(tag.Entry);
			tag.Entry.Invoke("SetInfo");

			tag.UpdateNode();
		}

		public void DeleteApplication(NodeTag tag)
		{
			if (tag == null || tag.Entry == null) return;

			if (tag.IsWebApp)
			{
				tag.Entry.Invoke("AppDelete");
				tag.Entry.Invoke("SetInfo");
			}

			tag.Entry.DeleteTree();
			tag.Entry.CommitChanges();
			tag.Entry = null;

			tag.UpdateNode();
		}

		public void ClearPhpMapping(NodeTag tag)
		{	
			if (tag == null || tag.Entry == null) return;

			ClearPhpMapping(tag.Entry);
			tag.Entry.Invoke("SetInfo");

			tag.UpdateNode();
		}

		public void AddPhpMapping(NodeTag tag)
		{
			if (tag == null || tag.Entry == null) return;

			AddPhpMapping(tag.Entry);
			tag.Entry.Invoke("SetInfo");

			tag.UpdateNode();
		}

		public void RepairMapping(NodeTag tag)
		{
			if (tag == null || tag.Entry == null) return;

			RepairMapping(tag.Entry);
			tag.Entry.Invoke("SetInfo");
		}
		#endregion 
	}
}
