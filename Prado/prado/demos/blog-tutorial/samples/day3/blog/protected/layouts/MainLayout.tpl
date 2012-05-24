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
<com:THyperLink Text="Login"
	NavigateUrl="<%= $this->Service->constructUrl('users.LoginUser') %>"
	Visible="<%= $this->User->IsGuest %>" />

<com:TLinkButton Text="Logout"
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