<%@ Page Language="PHP" MasterPageFile="~/Default.master" Title="Your Name Here | Admin"
	CodeFile="Albums.aspx.php" Inherits="Admin_Albums_aspx" %>

<asp:content id="Content1" contentplaceholderid="Main" runat="server">

	<div class="shim column"></div>

	<div class="page" id="admin-albums">

		<div id="sidebar">
			<h3>Add New Album</h3>
			<p>Before uploading your pictures, create an album to organize your pictures.</p>
			<asp:FormView ID="FormView1" Runat="server"
				DataSourceID="ObjectDataSource1" DefaultMode="Insert"
				BorderWidth="0" CellPadding="0">
				<InsertItemTemplate>
					<asp:RequiredFieldValidator	ID="RequiredFieldValidator1" Runat="server" ErrorMessage="You must choose a	title." ControlToValidate="TextBox1" Display="Dynamic" Enabled="false" />
					<p>
						Title<br />
						<asp:TextBox ID="TextBox1" Runat="server" Width="200" Text='<%# Bind("Caption") %>' CssClass="textfield" />
						<asp:CheckBox ID="CheckBox2" Runat="server" checked='<%# Bind("IsPublic") %>' text="Make this album public" />
					</p>
					<p style="text-align:right;">
						<asp:ImageButton ID="ImageButton1" Runat="server" CommandName="Insert" skinid="add"/>
					</p>
				</InsertItemTemplate>
			</asp:FormView>
		</div>

		<div id="content">
			<h3>Your Albums</h3>
			
			<p>The following are the albums	currently on your site. Click <b>Edit</b> to modify the pictures in each 
			album. Click <b>Delete</b> to permanently remove the album and all of its pictures</p>
			
			<asp:gridview id="GridView1" runat="server"
				datasourceid="ObjectDataSource1" datakeynames="AlbumID" cellpadding="6"
				autogeneratecolumns="False" BorderStyle="None" BorderWidth="0px" width="420px" showheader="false">
				<EmptyDataTemplate>
				You currently have no albums.
				</EmptyDataTemplate>
				<EmptyDataRowStyle CssClass="emptydata"></EmptyDataRowStyle>
				<columns>
					<asp:TemplateField>
						<ItemStyle Width="116" />
						<ItemTemplate>
							<table border="0" cellpadding="0" cellspacing="0" class="photo-frame">
								<tr>
									<td class="topx--"></td>
									<td class="top-x-"></td>
									<td class="top--x"></td>
								</tr>
								<tr>
									<td class="midx--"></td>
									<td><a href='Photos.aspx?AlbumID=<%# $this->Eval("AlbumID") %>'>
										<img src="../Handler.ashx?AlbumID=<%# $this->Eval("AlbumID") %>&Size=S" class="photo_198" style="border:4px solid white" alt="Sample Photo from Album Number <%# $this->Eval("AlbumID") %>" /></a></td>
									<td class="mid--x"></td>
								</tr>
								<tr>
									<td class="botx--"></td>
									<td class="bot-x-"></td>
									<td class="bot--x"></td>
								</tr>
							</table>
						</ItemTemplate>
					</asp:TemplateField>
					<asp:TemplateField>
						<ItemStyle Width="280" />
						<ItemTemplate>
							<div style="padding:8px 0;">
								<b><%# $this->Server->HtmlEncode($this->Eval("Caption")) %></b><br />
								<%# $this->Eval("Count") %> Photo(s)<asp:Label ID="Label1" Runat="server" Text=" Public" Visible='<%# $this->Eval("IsPublic") %>'></asp:Label>
							</div>
							<div style="width:100%;text-align:right;">
								<asp:ImageButton ID="ImageButton2" Runat="server" CommandName="Edit" SkinID="rename" />
								<a href='<%# "Photos.aspx?AlbumID=" . $this->Eval("AlbumID") %>'><asp:image ID="Image1" runat="Server"  skinid="edit" /></a>
								<asp:ImageButton ID="ImageButton3" Runat="server" CommandName="Delete" SkinID="delete" />
							</div>
						</ItemTemplate>
						<EditItemTemplate>
							<div style="padding:8px 0;">
								<asp:TextBox ID="TextBox2" Runat="server" Width="160" Text='<%# Bind("Caption") %>' CssClass="textfield" />
								<asp:CheckBox ID="CheckBox1" Runat="server" checked='<%# Bind("IsPublic") %>' text="Public" />
							</div>
							<div style="width:100%;text-align:right;">
								<asp:ImageButton ID="ImageButton4" Runat="server" CommandName="Update" SkinID="save" />
								<asp:ImageButton ID="ImageButton5" Runat="server" CommandName="Cancel" SkinID="cancel" />
							</div>
						</EditItemTemplate>
					</asp:TemplateField>
				</columns>
			</asp:gridview>
		</div>

	</div>
	
	<asp:ObjectDataSource ID="ObjectDataSource1" Runat="server" TypeName="PhotoManager" 
		SelectMethod="GetAlbums"
		InsertMethod="AddAlbum" 
		DeleteMethod="RemoveAlbum" 
		UpdateMethod="EditAlbum" >
	</asp:ObjectDataSource>

</asp:content>
