<div class="portlet">

<h2 class="portlet-title">Login</h2>

<com:TPanel CssClass="portlet-content" DefaultButton="LoginButton">
Username
<com:TRequiredFieldValidator
	ControlToValidate="Username"
	ValidationGroup="login"
	Text="...is required"
	Display="Dynamic"/>
<br/>
<com:TTextBox ID="Username" />
<br/>

Password
<com:TCustomValidator
	ControlToValidate="Password"
	ValidationGroup="login"
	Text="...is invalid"
	Display="Dynamic"
	OnServerValidate="validateUser" />
<br/>
<com:TTextBox ID="Password" TextMode="Password" />

<br/><br/>

<com:TLinkButton
	ID="LoginButton"
	Text="Login"
	ValidationGroup="login"
	CssClass="link-button"
	OnClick="loginButtonClicked" />
<com:THyperLink
	Text="Register"
	NavigateUrl=<%= $this->Service->constructUrl('Users.NewUser') %>
	Visible=<%= TPropertyValue::ensureBoolean($this->Application->Parameters['MultipleUser']) %>
	CssClass="link-button" />

</com:TPanel><!-- end of portlet-content -->

</div><!-- end of portlet -->
