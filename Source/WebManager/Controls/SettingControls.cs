using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;
using WebManager;

namespace WebManager.Controls
{
	#region Interfaces 
	public interface ISettingControl
	{
		string XPath { get; }
		void SaveValue(XmlNode node);
		void LoadValue(XmlNode node);
		void ResetValue();
	}
	#endregion
	
	#region Controls
	class SettingComboBox: ComboBox, ISettingControl
	{
		#region ISettingControl Members

		private string xPath;
		public string XPath
		{
			get { return xPath; }
		}


		public void ResetValue()
		{
			this.SelectedIndex = -1;
		}

		public void SaveValue(XmlNode node)
		{
			if (SelectedIndex == -1) return;
			ConfigManager.ComboBoxOption cbo = (ConfigManager.ComboBoxOption)this.SelectedItem;
			if (cbo == null) return;
			if (node is XmlAttribute)
			{
				node.Value = cbo.Value.InnerText.Trim();
			}
			else if (node is XmlElement)
			{
				node.AppendChild(node.OwnerDocument.ImportNode(cbo.Value.FirstChild.CloneNode(true),true));
			}
			else Debug.Fail("Couldn't save dropdown value");
		}

		public void LoadValue(XmlNode node)
		{
			foreach (ConfigManager.ComboBoxOption cbo in this.Items)
			{
				if (node is XmlAttribute)
				{
					if (cbo.Value.InnerText.Trim().CompareTo(node.Value) == 0)
					{ this.SelectedItem = cbo; break; }
				}
				else if (node is XmlElement)
				{
					if (ConfigManager.ContainsNode(node, cbo.Value))
					{ this.SelectedItem = cbo; break; }
				}
				else Debug.Fail("Couldn't load dropdown value");
			}
		}

		public SettingComboBox(string xpath)
		{
			xPath = xpath;
		}

		#endregion

	}

	class SettingTextBox : TextBox, ISettingControl
	{
		#region ISettingControl Members

		private string xPath;
		public string XPath
		{
			get { return xPath; }
		}

		public void SaveValue(XmlNode node)
		{
			if (node is XmlAttribute)
			{
				node.Value = this.Text;
			}
			else if (node is XmlElement)
			{
				node.InnerText = this.Text;
			}
			else Debug.Fail("Couldn't save text value");
		}

		public void LoadValue(XmlNode node)
		{
			if (node is XmlAttribute)
			{
				this.Text = node.Value;
			}
			else if (node is XmlElement)
			{
				this.Text = node.InnerText;
			}
			else Debug.Fail("Couldn't load text value");
		}

		public SettingTextBox(string xpath)
		{
			xPath = xpath;
		}

		#endregion

		#region ISettingControl Members


		public void ResetValue()
		{
			this.Text = "";
		}

		#endregion

	}

	class SettingCheckBox : CheckBox, ISettingControl
	{
		#region ISettingControl Members

		private string xPath;
		public string XPath
		{
			get { return xPath; }
		}

		public void SaveValue(XmlNode node)
		{
			if (this.Checked)
			{
				if (node is XmlAttribute)
				{
					node.Value = ((string)Tag).Trim();
				}
				else if (node is XmlElement)
				{
					node.AppendChild(node.OwnerDocument.ImportNode(((XmlNode)Tag).CloneNode(true), true));
				}
				else Debug.Fail("Couldn't save checkbox value");
			}
		}

		public void LoadValue(XmlNode node)
		{
			if (node is XmlElement)
			{
				if (ConfigManager.ContainsNode(node,((XmlNode)this.Tag)))
					this.Checked= true;
			}
			else if (node is XmlAttribute)
			{
				if (((string)Tag).CompareTo(node.Value.Trim()) == 0)
					this.Checked = true;
			}
			else Debug.Fail("Couldn't load checkbox value");
		}

		public SettingCheckBox(string xpath)
		{
			xPath = xpath;
		}
		

		#endregion

		#region ISettingControl Members


		public void ResetValue()
		{
			this.Checked = false;
		}

		#endregion
	}

	#endregion
}
