namespace WebManager.Controls
{
	partial class ExtensionControl
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.extensiongroup = new System.Windows.Forms.GroupBox();
			this.editbtn = new System.Windows.Forms.Button();
			this.extensionlist = new System.Windows.Forms.ListView();
			this.assemblycolumn = new System.Windows.Forms.ColumnHeader();
			this.sectioncolumn = new System.Windows.Forms.ColumnHeader();
			this.extensiongroup.SuspendLayout();
			this.SuspendLayout();
			// 
			// extensiongroup
			// 
			this.extensiongroup.Controls.Add(this.editbtn);
			this.extensiongroup.Controls.Add(this.extensionlist);
			this.extensiongroup.Dock = System.Windows.Forms.DockStyle.Fill;
			this.extensiongroup.Location = new System.Drawing.Point(0, 0);
			this.extensiongroup.Name = "extensiongroup";
			this.extensiongroup.Size = new System.Drawing.Size(365, 220);
			this.extensiongroup.TabIndex = 0;
			this.extensiongroup.TabStop = false;
			this.extensiongroup.Text = "Extension list";
			// 
			// editbtn
			// 
			this.editbtn.Location = new System.Drawing.Point(278, 181);
			this.editbtn.Name = "editbtn";
			this.editbtn.Size = new System.Drawing.Size(75, 23);
			this.editbtn.TabIndex = 5;
			this.editbtn.Text = "Edit";
			this.editbtn.UseVisualStyleBackColor = true;
			this.editbtn.Click += new System.EventHandler(this.editbtn_Click);
			// 
			// extensionlist
			// 
			this.extensionlist.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.assemblycolumn,
			this.sectioncolumn});
			this.extensionlist.Location = new System.Drawing.Point(6, 19);
			this.extensionlist.Name = "extensionlist";
			this.extensionlist.Size = new System.Drawing.Size(347, 156);
			this.extensionlist.TabIndex = 4;
			this.extensionlist.UseCompatibleStateImageBehavior = false;
			this.extensionlist.View = System.Windows.Forms.View.Details;
			// 
			// assemblycolumn
			// 
			this.assemblycolumn.Text = "Assembly";
			this.assemblycolumn.Width = 250;
			// 
			// sectioncolumn
			// 
			this.sectioncolumn.Text = "Section";
			// 
			// ExtensionControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.extensiongroup);
			this.Name = "ExtensionControl";
			this.Size = new System.Drawing.Size(365, 220);
			this.extensiongroup.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox extensiongroup;
		private System.Windows.Forms.ListView extensionlist;
		private System.Windows.Forms.ColumnHeader assemblycolumn;
		private System.Windows.Forms.ColumnHeader sectioncolumn;
		private System.Windows.Forms.Button editbtn;
	}
}
