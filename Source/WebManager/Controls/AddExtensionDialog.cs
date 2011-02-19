using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Reflection;

namespace WebManager.Controls
{
	public partial class AddExtensionDialog : Form
	{
		#region Public Methods
		public AddExtensionDialog()
		{
			InitializeComponent();
		}

		public AddExtensionDialog(List<ListViewItem> groups, ListView.ListViewItemCollection lvic)
		{
			InitializeComponent();

			foreach (ColumnHeader ch in Extensionlist.Columns)
			{
				selectedlist.Columns.Add(ch.Text);
				selectedlist.Columns[0].Width = 240;
			}

			foreach (ListViewItem lvi in groups)
				Extensionlist.Items.Add((ListViewItem)lvi.Clone());
			
			foreach (ListViewItem lvi in lvic)
				selectedlist.Items.Add((ListViewItem)lvi.Clone());
		}

		public ListView.ListViewItemCollection GetSelectedExstensions()
		{
			return selectedlist.Items;
		}
		#endregion

		#region Private Methods
		private void browsebtn_Click(object sender, EventArgs e)
		{
			openFileDialog1.Filter = "Assemblies (*.dll)|*.dll|Managed Extensiones (*.mng)|*.mng";
			openFileDialog1.FileName = "";
			if (openFileDialog1.ShowDialog() == DialogResult.OK)
			{
				ListViewItem lvi = new ListViewItem();
				Assembly a = Assembly.LoadFile(openFileDialog1.FileName);
				MessageBox.Show(a.FullName);
			}
		}

		private void selectbtn_Click(object sender, EventArgs e)
		{
			foreach (ListViewItem lvi in Extensionlist.Items)
				if (lvi.Selected)
				{
					bool found = false;
					foreach (ListViewItem lvi2 in selectedlist.Items)
						if (lvi2.Text == lvi.Text)
							found = true;
					if (found) continue;
					selectedlist.Items.Add((ListViewItem)lvi.Clone());
				}
		}

		private void removebtn_Click(object sender, EventArgs e)
		{
			
			for (int i = selectedlist.Items.Count - 1; i >= 0; i--)
			{
				if (selectedlist.Items[i].Selected)
					selectedlist.Items.Remove(selectedlist.Items[i]);
			}
			
			
		}

		private void cancelbtn_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void okbtn_Click(object sender, EventArgs e)
		{
			this.Close();
		}
		#endregion

	}
}