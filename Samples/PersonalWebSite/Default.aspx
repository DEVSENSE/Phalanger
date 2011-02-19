<%@	Page Language="PHP" MasterPageFile="~/Default.master" Title="Your Name Here | Home"
	CodeFile="Default.aspx.php" Inherits="Default_aspx" %>

<asp:content id="Content1" contentplaceholderid="Main" runat="server">

	<div class="shim column"></div>
	
	<div class="page" id="home">
		<div id="sidebar">
			<asp:loginview id="LoginArea" runat="server">
				<AnonymousTemplate>
					<asp:login id="Login1" runat="server">
						<layouttemplate>
							<div class="login">
								<h4>Login to Site</h4>
								<asp:label runat="server" id="UserNameLabel" CssClass="label" associatedcontrolid="UserName">User Name</asp:label>
								<asp:textbox runat="server"	id="UserName" cssclass="textbox" accesskey="u" />
								<asp:requiredfieldvalidator	runat="server" id="UserNameRequired" controltovalidate="UserName" validationgroup="Login1" errormessage="User Name is required." tooltip="User Name	is required." >*</asp:requiredfieldvalidator>
								<asp:label runat="server" id="PasswordLabel" CssClass="label" associatedcontrolid="Password">Password</asp:label>
								<asp:textbox runat="server"	id="Password" textmode="Password" cssclass="textbox" accesskey="p" />
								<asp:requiredfieldvalidator	runat="server" id="PasswordRequired" controltovalidate="Password" validationgroup="Login1" tooltip="Password is	required." >*</asp:requiredfieldvalidator>
								<div><asp:checkbox runat="server" id="RememberMe" text="Remember me	next time"/></div>
								<asp:imagebutton runat="server"	id="LoginButton" CommandName="Login" AlternateText="login" skinid="login" CssClass="button"/>
								or
								<a href="register.aspx"	class="button"><asp:image id="Image1" runat="server"  AlternateText="create	a new account" skinid="create"/></a>
								<p><asp:literal	runat="server" id="FailureText"	enableviewstate="False"></asp:literal></p>
							</div>
						</layouttemplate>
					</asp:login>
				</anonymoustemplate>
				<LoggedInTemplate>
					<h4><asp:loginname id="LoginName1" runat="server" formatstring="Welcome	{0}!" /></h4>
				</LoggedInTemplate>
			</asp:loginview>
			<hr />
			<asp:formview id="FormView1" runat="server" datasourceid="ObjectDataSource1" ondatabound="Randomize" cellpadding="0" borderwidth="0" enableviewstate="false">
				<ItemTemplate>
					<h4>Photo of the Day</h4>
					<table border="0" cellpadding="0" cellspacing="0" class="photo-frame">
						<tr>
							<td class="topx--"></td>
							<td class="top-x-"></td>
							<td class="top--x"></td>
						</tr>
						<tr>
							<td class="midx--"></td>
							<td><a href='Details.aspx?AlbumID=<%# $this->Eval("AlbumID") %>&Page=<%# $Container->DataItemIndex %>'>
								<img src="Handler.ashx?PhotoID=<%# $this->Eval("PhotoID") %>&Size=M" class="photo_198" style="border:4px solid white" alt='Photo Number <%# $this->Eval("PhotoID") %>' /></a></td>
							<td class="mid--x"></td>
						</tr>
						<tr>
							<td class="botx--"></td>
							<td class="bot-x-"></td>
							<td class="bot--x"></td>
						</tr>
					</table>
					<p>Lorem ipsum dolor sit amet, consectetuer adipiscing elit, sed diam nonummy nibh euismod </p>
					<p><a href='Download.aspx?AlbumID=<%# $this->Eval("AlbumID") %>&Page=<%# $Container->DataItemIndex %>'>
						<asp:image runat="Server" id="DownloadButton" AlternateText="download photo" skinid="download"/></a></p>
					<p>See <a href="Albums.aspx">more photos </a></p>
					<hr />
				</ItemTemplate>
			</asp:formview>
			<h4>My Latest Piece of Work</h4>
			<p>Lorem ipsum dolor sit amet, consectetuer adipiscing elit, sed diam nonummy nibh euismod tincidunt ut	laoreet	dolore magna aliquam erat volutpat.</p>
		</div>
		<div id="content">
			<h3>Welcome	to My Site</h3>
			<p>This	is my personal site. In	it you will find lorem ipsum dolor sit amet, consectetuer adipiscing elit, sed diam	nonummy	nibh euismod tincidunt ut laoreet dolore magna aliquam erat	volutpat. Ut wisi enim ad minim	veniam.	</p>
			<hr	/>
			<div id="whatsnew">
				<h4>What's New</h4>
				<p>Lorem <a href="#">ipsum</a> dolor sit amet, consectetuer	adipiscing elit, sed diam nonummy nibh euismod.</p>
				<p>Lorem <a href="#">ipsum</a> dolor sit amet, consectetuer	adipiscing elit, sed diam nonummy nibh euismod.</p>
			</div>
			<div id="coollinks">
				<h4>Cool Links</h4>
				<ul	class="link">
					<li><a href="#">Lorem ipsum dolositionr</a></li>
					<li><a href="#">Lorem ipsum dolositionr</a></li>
					<li><a href="#">Lorem ipsum dolositionr</a></li>
					<li><a href="#">Lorem ipsum dolositionr</a></li>
					<li><a href="#">Lorem ipsum dolositionr</a></li>
				</ul>
			</div>
			<hr	/>
			<h4>What's Up Lately </h4>
			<p>Lorem ipsum dolor sit amet, <a href="#">consectetuer</a>	adipiscing elit, sed diam nonummy nibh euismod tincidunt ut	laoreet	dolore magna aliquam erat volutpat.	Ut wisi	enim ad	minim veniam, quis nostrud exercitation	consequat. Duis	autem veleum iriure	dolor in hendrerit in vulputate	velit esse molestie	consequat, vel willum.</p>
			<p>Lorem ipsum dolor sit amet, consectetuer	adipiscing elit, sed diam nonummy nibh <a href="#">euismod tincidunt ut</a>	laoreet	dolore magna aliquam erat volutpat.	Ut wisi	enim ad	minim veniam, quis nostrud exercitation	consequat. Duis	autem veleum iriure	dolor in hendrerit in vulputate	velit esse molestie	consequat, vel willum.</p>
			<p>Lorem<a href="#"> ipsum dolor sit amet</a>, consectetuer	adipiscing elit, sed diam nonummy nibh euismod tincidunt ut	laoreet	dolore magna aliquam erat volutpat.	Ut wisi	enim ad	minim veniam, quis nostrud exercitation	consequat. Duis	autem veleum iriure	dolor in hendrerit in vulputate	velit esse molestie	consequat, vel willum.</p>
			<p>Lorem ipsum dolor sit amet, consectetuer	adipiscing elit, sed diam nonummy nibh euismod tincidunt ut	laoreet	dolore magna aliquam erat volutpat.	Ut wisi	enim ad	minim veniam, quis nostrud exercitation	consequat. <a href="#">Duis	autem veleum</a> iriure	dolor in hendrerit in vulputate	velit esse molestie	consequat, vel willum.</p>
			<p>Lorem ipsum dolor sit amet, consectetuer	adipiscing elit, sed diam nonummy nibh euismod tincidunt ut	laoreet	dolore magna aliquam erat volutpat.	Ut wisi	enim ad	minim veniam, quis nostrud exercitation	consequat. <a href="#">Duis	autem veleum</a> iriure	dolor in hendrerit in vulputate	velit esse molestie	consequat, vel willum.</p>
		</div>
	</div>

	<asp:ObjectDataSource ID="ObjectDataSource1" Runat="server" TypeName="PhotoManager" 
		SelectMethod="GetPhotos">
	</asp:ObjectDataSource>

</asp:content>
