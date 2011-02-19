namespace WebManager.Controls
{
	partial class AddExtensionDialog
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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.Extensionlist = new System.Windows.Forms.ListView();
			this.assemblycolumn = new System.Windows.Forms.ColumnHeader();
			this.sectioncolumn = new System.Windows.Forms.ColumnHeader();
			this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
			this.selectedlist = new System.Windows.Forms.ListView();
			this.browsebtn = new System.Windows.Forms.Button();
			this.selectbtn = new System.Windows.Forms.Button();
			this.removebtn = new System.Windows.Forms.Button();
			this.okbtn = new System.Windows.Forms.Button();
			this.cancelbtn = new System.Windows.Forms.Button();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add(this.Extensionlist);
			this.groupBox1.Location = new System.Drawing.Point(12, 12);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(360, 185);
			this.groupBox1.TabIndex = 0;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Extension list";
			// 
			// Extensionlist
			// 
			this.Extensionlist.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.Extensionlist.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.assemblycolumn,
			this.sectioncolumn});
			this.Extensionlist.Location = new System.Drawing.Point(9, 19);
			this.Extensionlist.Name = "Extensionlist";
			this.Extensionlist.Size = new System.Drawing.Size(339, 152);
			this.Extensionlist.TabIndex = 3;
			this.Extensionlist.UseCompatibleStateImageBehavior = false;
			this.Extensionlist.View = System.Windows.Forms.View.Details;
			// 
			// assemblycolumn
			// 
			this.assemblycolumn.Text = "Assembly";
			this.assemblycolumn.Width = 240;
			// 
			// sectioncolumn
			// 
			this.sectioncolumn.Text = "Section";
			// 
			// openFileDialog1
			// 
			this.openFileDialog1.FileName = "openFileDialog1";
			// 
			// selectedlist
			// 
			this.selectedlist.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.selectedlist.Location = new System.Drawing.Point(21, 211);
			this.selectedlist.Name = "selectedlist";
			this.selectedlist.Size = new System.Drawing.Size(339, 116);
			this.selectedlist.TabIndex = 3;
			this.selectedlist.UseCompatibleStateImageBehavior = false;
			this.selectedlist.View = System.Windows.Forms.View.Details;
			// 
			// browsebtn
			// 
			this.browsebtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.browsebtn.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.browsebtn.Location = new System.Drawing.Point(378, 31);
			this.browsebtn.Name = "browsebtn";
			this.browsebtn.Size = new System.Drawing.Size(75, 23);
			this.browsebtn.TabIndex = 4;
			this.browsebtn.Text = "Browse";
			this.browsebtn.UseVisualStyleBackColor = true;
			this.browsebtn.Click += new System.EventHandler(this.browsebtn_Click);
			// 
			// selectbtn
			// 
			this.selectbtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.selectbtn.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.selectbtn.Location = new System.Drawing.Point(378, 60);
			this.selectbtn.Name = "selectbtn";
			this.selectbtn.Size = new System.Drawing.Size(75, 23);
			this.selectbtn.TabIndex = 5;
			this.selectbtn.Text = "Select";
			this.selectbtn.UseVisualStyleBackColor = true;
			this.selectbtn.Click += new System.EventHandler(this.selectbtn_Click);
			// 
			// removebtn
			// 
			this.removebtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.removebtn.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.removebtn.Location = new System.Drawing.Point(378, 211);
			this.removebtn.Name = "removebtn";
			this.removebtn.Size = new System.Drawing.Size(75, 23);
			this.removebtn.TabIndex = 6;
			this.removebtn.Text = "Remove";
			this.removebtn.UseVisualStyleBackColor = true;
			this.removebtn.Click += new System.EventHandler(this.removebtn_Click);
			// 
			// okbtn
			// 
			this.okbtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.okbtn.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.okbtn.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.okbtn.Location = new System.Drawing.Point(285, 333);
			this.okbtn.Name = "okbtn";
			this.okbtn.Size = new System.Drawing.Size(75, 23);
			this.okbtn.TabIndex = 4;
			this.okbtn.Text = "Ok";
			this.okbtn.UseVisualStyleBackColor = true;
			this.okbtn.Click += new System.EventHandler(this.okbtn_Click);
			// 
			// cancelbtn
			// 
			this.cancelbtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.cancelbtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancelbtn.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.cancelbtn.Location = new System.Drawing.Point(366, 333);
			this.cancelbtn.Name = "cancelbtn";
			this.cancelbtn.Size = new System.Drawing.Size(75, 23);
			this.cancelbtn.TabIndex = 4;
			this.cancelbtn.Text = "Cancel";
			this.cancelbtn.UseVisualStyleBackColor = true;
			this.cancelbtn.Click += new System.EventHandler(this.cancelbtn_Click);
			// 
			// AddExtensionDialog
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(462, 366);
			this.Controls.Add(this.removebtn);
			this.Controls.Add(this.selectbtn);
			this.Controls.Add(this.cancelbtn);
			this.Controls.Add(this.okbtn);
			this.Controls.Add(this.browsebtn);
			this.Controls.Add(this.selectedlist);
			this.Controls.Add(this.groupBox1);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "AddExtensionDialog";
			this.ShowIcon = false;
			this.Text = "AddExtensionDialog";
			this.groupBox1.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.OpenFileDialog openFileDialog1;
		private System.Windows.Forms.ListView Extensionlist;
		private System.Windows.Forms.ListView selectedlist;
		private System.Windows.Forms.ColumnHeader assemblycolumn;
		private System.Windows.Forms.ColumnHeader sectioncolumn;
		private System.Windows.Forms.Button browsebtn;
		private System.Windows.Forms.Button selectbtn;
		private System.Windows.Forms.Button removebtn;
		private System.Windows.Forms.Button okbtn;
		private System.Windows.Forms.Button cancelbtn;

	}
}