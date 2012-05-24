<html>
<com:THead />
<body>
<com:TForm>
<div id="page">

<div id="header">
<h1>My PRADO Blog</h1>
</div>

<div id="main">
<com:TContentPlaceHolder ID="Main" />
</div>

<div id="footer">

<com:THyperLink Text="Home" SkinID="MainMenu"
	NavigateUrl="<%= $this->Service->DefaultPageUrl %>" />

<com:THyperLink Text="New Post" SkinID="MainMenu"
	NavigateUrl="<%= $this->Service->constructUrl('posts.NewPost') %>"
	Visible="<%= !$this->User->IsGuest %>" />

<com:THyperLink Text="New User" SkinID="MainMenu"
	NavigateUrl="<%= $this->Service->constructUrl('users.NewUser') %>"
	Visible="<%= $this->User->IsAdmin %>" />

<com:THyperLink Text="Login" SkinID="MainMenu"
	NavigateUrl="<%= $this->Service->constructUrl('users.LoginUser') %>"
	Visible="<%= $this->User->IsGuest %>" />

<com:TLinkButton Text="Logout" SkinID="MainMenu"
	OnClick="logoutButtonClicked"
	Visible="<%= !$this->User->IsGuest %>"
	CausesValidation="false" />

<br/>
<%= PRADO::poweredByPrado() %>
</div>

</div>
</com:TForm>
</body>
</html>