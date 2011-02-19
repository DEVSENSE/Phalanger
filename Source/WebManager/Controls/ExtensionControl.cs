using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace WebManager.Controls
{
	public partial class ExtensionControl : UserControl, ISettingControl
	{
		public event EventHandler ListChanged;

		#region Methods
		public ExtensionControl(string xp, XmlNodeList groups)
		{
			InitializeComponent();
			xpath = xp;
			foreach (XmlNode gb in groups)
			{
				string assemblyname = gb.Attributes["assembly"].Value;
				ListViewItem assemblylvi = new ListViewItem(assemblyname);
				if (gb.Attributes["section"] != null)
					assemblylvi.SubItems.Add(gb.Attributes["section"].Value);
				else
					assemblylvi.SubItems.Add("");
				extensionslist.Add(assemblylvi);
			}
		}

		private List<ListViewItem> extensionslist = new List<ListViewItem>();

		private void editbtn_Click(object sender, EventArgs e)
		{
			AddExtensionDialog asd = new AddExtensionDialog(extensionslist,extensionlist.Items);
			if (asd.ShowDialog() == DialogResult.OK)
			{
				extensionlist.Items.Clear();
				foreach (ListViewItem lvi in asd.GetSelectedExstensions())
					extensionlist.Items.Add((ListViewItem)lvi.Clone());
				if (ListChanged != null)
					ListChanged(this, EventArgs.Empty);
			}

			
		}
		#endregion

		#region	ISettingControl	Members

		private	string xpath;
		public string XPath
		{
			get	{ return xpath;	}
		}

		public void	SaveValue(XmlNode node)
		{
			foreach	(ListViewItem lvi in extensionlist.Items)
			{
				XmlNode	assemblyn =	node.OwnerDocument.CreateNode(XmlNodeType.Element,"add",null);
				for	(int i = 0;	i <	extensionlist.Columns.Count; i++)
				{
					XmlAttribute xa	= assemblyn.OwnerDocument.CreateAttribute(extensionlist.Columns[i].Text.ToLower());
					xa.Value = lvi.SubItems[i].Text;
					assemblyn.Attributes.Append(xa);
				}
				node.AppendChild(assemblyn);
			}
		}

		public void	LoadValue(XmlNode node)
		{
			extensionlist.Items.Clear();
			foreach	(XmlNode xn	in node.ChildNodes)
			{
				ListViewItem lwi = new ListViewItem(xn.Attributes["assembly"].Value);
				if (xn.Attributes["section"] !=	null)
					lwi.SubItems.Add(xn.Attributes["section"].Value);
				extensionlist.Items.Add(lwi);
			}
			return;
		}

		public void	ResetValue()
		{
			this.extensionlist.Items.Clear();
		}

		#endregion		 
	}
}
