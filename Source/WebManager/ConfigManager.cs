using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.IO;
using WebManager.Controls;

namespace WebManager
{
	/// <summary>
	///	 Class that manages web application configuration
	/// </summary>
	class ConfigManager
	{
		#region Variables
		private Dictionary<string, List<ListViewItem>> tabinfo = new Dictionary<string, List<ListViewItem>>();
		private static string compilerfile = Application.StartupPath + "\\" + "Config\\ConfigTemplate.xml";
		private static string defaultfile = Application.StartupPath + "\\" + "Config\\Default.xml";
		private List<ISettingControl> controls = new List<ISettingControl>();
		private string configfile;
		#endregion

		#region Properties
		private bool changed = false;

		/// <summary>
		///	 Indicator if config was changed
		/// </summary>
		public bool Changed
		{
			get { return changed; }
			set { changed = value; }
		}
	
		/// <summary>
		///	 Path to the config file
		/// </summary>
		public string ConfigFile
		{
			get { return configfile; }
		}

		/// <summary>
		///  Path to default file config
		/// </summary>
		public static string DefaultFile
		{
			get { return defaultfile; }
		}
		#endregion 

		#region Methods
		#region Public Methods
		/// <summary>
		///	 Gets the pages from the config file
		/// </summary>
		/// <returns>String array of page names</returns>
		public string[] GetTabPages()
		{
			List<string> tablist= new List<string>();
			foreach ( string s in tabinfo.Keys)
				tablist.Add(s);
			return tablist.ToArray();
		}

		/// <summary>
		///	 Gets the content of a page
		/// </summary>
		/// <param name="tabpage">Page name</param>
		/// <returns>List of ListViewItems</returns>
		public List<ListViewItem> GetTabPageContent(string tabpage)
		{
			if (tabinfo.ContainsKey(tabpage))
				return tabinfo[tabpage];
			else
				return null;
		}

		/// <summary>
		///	 Checks for attribute in XmlNode
		/// </summary>
		/// <param name="container">Node</param>
		/// <param name="what">Atribute name</param>
		/// <returns></returns>
		public static bool ContainsAttribute(XmlNode container, XmlNode what)
		{
			if (container.Name.ToLower() == what.Name.ToLower() &&
				container.Value.ToLower() == what.Value.ToLower())
				return true;
			return false;
		}

		/// <summary>
		///	 Sets the attribute to true if a user has clicked on a edit control
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void edited(object sender, EventArgs e)
		{
			changed = true;
		}

		public static bool ContainsNode(XmlNode container, XmlNode what)
		{
			foreach (XmlNode child in container.ChildNodes)
			{
				if (child.Name != what.Name) continue;
				if (child.Attributes.Count != what.Attributes.Count) continue;
				bool match = true;
				foreach (XmlAttribute a in child.Attributes)
				{
					XmlAttribute wa = what.Attributes[a.Name];
					if (wa == null || wa.Value != a.Value || wa.NamespaceURI != a.NamespaceURI)
					{ match = false; break; }
				}
				if (!match) continue;

				return true;
			}
			return false;
		}

		public ConfigManager(string conffile)
		{
 			configfile = conffile;
			ReadConfigFile();
			LoadFile(ConfigManager.DefaultFile);
			LoadFile();
		}

		/// <summary>
		///		Creates the config file 
		/// </summary>
		public static string CreateConfigFile(string path)
		{
			if (!File.Exists(path + "\\web.config"))
				File.Copy(defaultfile, path + "\\web.config");
			return path + "\\web.config";
		}

		public string CreatePHPMapping()
		{
			//CreateXmlNode
			FileInfo fi = new FileInfo(configfile);
			if (fi.Exists)
			{
				XmlDocument config = new XmlDocument();
				config.Load(fi.FullName);

				GetNode("configuration/system.webServer/defaultDocument/files/add[@value=index.php]", config);
				XmlNode xn = GetNode("configuration/system.webServer/handlers/add[@name=Phalanger]", config);
				xn.Attributes.Append(config.CreateAttribute("path"));
				xn.Attributes.Append(config.CreateAttribute("verb"));
				xn.Attributes.Append(config.CreateAttribute("modules"));
				xn.Attributes.Append(config.CreateAttribute("scriptProcessor"));
				xn.Attributes.Append(config.CreateAttribute("resourceType"));
				xn.Attributes["path"].Value = ".php";
				xn.Attributes["verb"].Value = "*";
				xn.Attributes["modules"].Value = "IsapiModule";
				xn.Attributes["scriptProcessor"].Value = "C:\\Windows\\Microsoft.NET\\Framework\\v2.0.50727\\aspnet_isapi.dll";
				xn.Attributes["resourceType"].Value = "File";
				
		
				config.Save(fi.FullName);
			}
			else //!Exist
			{
				File.Copy(defaultfile, configfile);
			}
			return fi.FullName;
		}

		/// <summary>
		///		Deletes the php mapping info from the php file in windows vista
		/// </summary>
		public void RemovePhpMapping()
		{
			if (WebManager.IsRunningIIS7)
			{
				XmlDocument xdoc = new XmlDocument();
				xdoc.Load(configfile);
				
				//Php extension mapping
				XmlNode xn = xdoc.SelectSingleNode("configuration/system.webServer/defaultDocument/files/add[@value=\"index.php\"]");
				if (xn != null)
					xn.ParentNode.RemoveChild(xn);

				//Http handler removal
				xn = xdoc.SelectSingleNode("configuration/system.webServer/handlers/add[@name=\"Phalanger\"]");
				if (xn != null)
					xn.ParentNode.RemoveChild(xn);

				//Php compiler setting
				xn = xdoc.SelectSingleNode("configuration/phpNet");
				if (xn != null)
					xn.ParentNode.RemoveChild(xn);

				try
				{
					xdoc.Save(configfile);
				}
				catch (Exception)
				{
					MessageBox.Show("Couldn't save to config file");
				}
			}
		}

		/// <summary>
		///  Creates a XML document and records the settings of all set controls. Saves the document to a file
		/// </summary>
		public void SaveToFile()
		{
			XmlDocument xdef = new XmlDocument();
			xdef.Load(defaultfile);
			{
				foreach (ISettingControl isc in controls)
				{
					bool found = false;
					string xp = isc.XPath;

					foreach (XmlNode defxn in xdef.SelectNodes(xp))
					{
						XmlNode xn2save = defxn.CloneNode(false);
						isc.SaveValue(xn2save);
						
						if (defxn is XmlElement && xn2save.FirstChild != null)
							if (ContainsNode(defxn, xn2save.FirstChild))
								found = true;
						if (defxn is XmlAttribute)
							if (ContainsAttribute(xn2save, defxn))
								found = true;
					}
					if (!found)
					{
						XmlNode xn = GetNode(xp, xdef);
						if (xn != null)
							isc.SaveValue(xn);
					}
				}
			}
			xdef.Save(configfile);
			changed = false;
		}
		#endregion

		#region PrivateMethods
		#region SaveFile
		private Attr GetAttributeFromXpath(string xpath)
		{
			Attr myattr;
			myattr.name = "";
			myattr.value = "";
			string[] s = xpath.Split('=');

			if (s.Length == 1)
				return myattr;


			myattr.name = s[0].Split('[')[1].Replace("@", "");
			myattr.value = s[1].Replace("]", "").Replace("'", "");

			return myattr;
		}

		
		/// <summary>
		///		Adds a xmlnode according to the xpath string in the parameter
		/// </summary>
		/// <param name="where">Node, where the new node is added</param>
		/// <param name="what"></param>
		/// <returns></returns>
		private XmlNode CreateXmlNode(XmlNode where, string what)
		{
			//Get information
			if (what.StartsWith("@"))
			{
				XmlAttribute xa = where.OwnerDocument.CreateAttribute(what.Replace("@",""));
				where.Attributes.Append(xa);
				return xa;
			}
			string name = what;
			bool cond = false;
			Attr myattr = GetAttributeFromXpath(what); 
			if (what.Contains("[") && what.Contains("]"))
			{
				name = what.Split('[')[0];
				cond = true;
			}

			//Create new node
			XmlNode xn = null;
			if (where is XmlDocument)
				xn = ((XmlDocument)where).CreateElement(name);
			else 
				xn = where.OwnerDocument.CreateElement(name);

			
			if (cond)
			{
				xn.Attributes.Append(where.OwnerDocument.CreateAttribute(myattr.name));
				xn.Attributes[myattr.name].Value = myattr.value;
			}

			//Add to document structure
			where.AppendChild(xn);

			return xn;			
		}

		/// <summary>
		///	 Creates an Xml Node in a document and returns it 
		/// </summary>
		/// <param name="xp">Xpath to the element</param>
		/// <param name="xdoc">Xdocument where the xpath will be created</param>
		/// <returns></returns>
		private XmlNode GetNode(string xp, XmlDocument xdoc)
		{
			string[] s = xp.Split('/');
			StringBuilder sb = new StringBuilder();
			XmlNode destnode = null;
			sb.Append(s[0]);
			
			destnode =xdoc.SelectSingleNode(sb.ToString());
			if (destnode == null)
			{
				destnode = xdoc.SelectSingleNode(sb.ToString());
				if (destnode == null)
					destnode = CreateXmlNode(xdoc, sb.ToString());
			}

			if (s.Length > 1)
			{
				
				for (int i = 1; i < s.Length; i++)
				{
					sb.Append("/");
					sb.Append(s[i]);
					XmlNode newnode = xdoc.SelectSingleNode(sb.ToString());
					if (newnode == null)
						newnode = CreateXmlNode(destnode, s[i]);
					destnode = newnode;
				}
			}

			return destnode;
		}

		#endregion

		#region Structures
		public class ComboBoxOption
		{
			private string text;

			public string Text
			{
				get { return text; }
				set { text = value; }
			}

			private XmlNode val;

			public XmlNode Value
			{
				get { return val; }
				set { val = value; }
			}

			public override string ToString()
			{
				return text;
			}


			public ComboBoxOption(string text, XmlNode value)
			{
				this.text=text;
				this.val= value;
			}
		}

		private struct Attr
		{
			public string name;
			public string value;
		}
		#endregion

		#region LoadFile
		/// <summary>
		///	 Reads the Template file and creates the data structures for the config categories
		/// </summary>
		private void ReadConfigFile()
		{
			XmlDocument xdoc = new XmlDocument();
			FileInfo fi = new FileInfo(compilerfile);
			if (!fi.Exists) return;
			xdoc.Load(compilerfile);

			//Section list
			XmlNodeList xnl = xdoc.SelectNodes("phpNet/tab");
			foreach (XmlNode xn in xnl)
			{
				string tabname = xn.Attributes["name"].Value;
				List<ListViewItem> list = new List<ListViewItem>();

				//Item list
				XmlNodeList groups = xn.SelectNodes("group");
				foreach (XmlNode group in groups)
				{
					ListViewItem lvi = new ListViewItem();
					try
					{
						lvi.Text = group.Attributes["name"].Value;
						int index; 
						int.TryParse(group.Attributes["image"].Value, out index);
						lvi.ImageIndex = index;
					}
					catch (Exception e)
					{
						MessageBox.Show("Error in file " + compilerfile + ": \n" + e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
					lvi.Tag = group;
					list.Add(lvi);
				}
				tabinfo[tabname] = list;
			}
		}

		public void LoadFile()
		{
			LoadFile(this.configfile);
		}

		public void LoadFile(string path)
		{
			FileInfo fi = new FileInfo(path);
			FileInfo dfi = new FileInfo(defaultfile);
			XmlDocument xdoc = new XmlDocument();
			//XmlDocument xdef = new XmlDocument();
			if (fi.Exists)
			{
				xdoc.Load(fi.FullName);
				//xdef.Load(defaultfile);
				
				foreach (ISettingControl isc in controls)
				{
					XmlNode xn = xdoc.SelectSingleNode(isc.XPath);
					//XmlNode xndef = xdef.SelectSingleNode(isc.XPath);

					isc.ResetValue();
					if (xn != null)
						isc.LoadValue(xn);
				}
			}
		}
		#endregion 

		#region TabGeneration
		public GroupBox GetGroupInfo(XmlNode xn)
		{
			GroupBox groupb = new GroupBox();
			TableLayoutPanel tlp = new TableLayoutPanel();

			groupb.Text = xn.Attributes["name"].Value;
			groupb.Controls.Add(tlp);
			groupb.Dock = DockStyle.Fill;
			tlp.Anchor = AnchorStyles.Bottom | AnchorStyles.Top;
			tlp.AutoSize= true;
			tlp.Dock = DockStyle.Fill;
			tlp.Left = 20;
			tlp.Top = 10;
		   
			foreach (XmlNode configitem in xn.ChildNodes)
			{
				string xp = configitem.Attributes["node"].Value;
				if (xp == null) xp = "";
				FlowLayoutPanel myflp = new FlowLayoutPanel();
				
				myflp.WrapContents = false;
				myflp.AutoSize = true;
				myflp.Anchor = AnchorStyles.Left & AnchorStyles.Right;
				myflp.Dock = DockStyle.Left;
				switch (configitem.Name)
				{
					case "check":
						SettingCheckBox cb = new SettingCheckBox(xp);
						cb.AutoSize = true;
						cb.Tag = configitem.FirstChild;
						cb.Text = configitem.Attributes["name"].Value;
						cb.Click += new System.EventHandler(this.edited);
						//private void edited(object sender, EventArgs e)
						myflp.Controls.Add(cb);
						controls.Add(cb);
						break;
					case "textbox":
						SettingTextBox tb = new SettingTextBox(xp);
						Label ttext = new Label();
						ttext.Dock = DockStyle.Bottom;
						ttext.Text = configitem.Attributes["name"].Value;
						ttext.AutoSize = true;
						ttext.TextChanged += new System.EventHandler(this.edited);
						tb.Tag = configitem.InnerXml; ;
						myflp.Controls.Add(ttext);
						myflp.Controls.Add(tb);
						controls.Add(tb);
						break;
					case "dropdown":
						Label ctext = new Label();
						ctext.Dock = DockStyle.Bottom;
						ctext.AutoSize = true;
						ctext.Text = configitem.Attributes["name"].Value;
						SettingComboBox comb = new SettingComboBox(xp);
						comb.DropDownStyle = ComboBoxStyle.DropDownList;
						comb.Click += new System.EventHandler(this.edited);
						foreach (XmlNode option in configitem.ChildNodes)
						{
							ComboBoxOption cbo = new ComboBoxOption(option.Attributes["name"].Value, option);
							comb.Items.Add(cbo);
						}
						myflp.Controls.Add(ctext);
						myflp.Controls.Add(comb);
						controls.Add(comb);
						break;
					case "extensionlist":
						ExtensionControl ec = new ExtensionControl(xp, configitem.SelectNodes("add"));
						ec.ListChanged += new System.EventHandler(this.edited);
						myflp.Controls.Add(ec);
						controls.Add(ec);
						break;
					default: break;
				}

				tlp.Controls.Add(myflp);
				
			};


			foreach (Control c in tlp.Controls)
			{
				c.Left = 0;
			}
			return groupb;


		}
		#endregion
		#endregion
		#endregion
	}
}
