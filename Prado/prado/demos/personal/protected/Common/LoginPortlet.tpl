<com:TPanel CssClass="login" DefaultButton="LoginButton">
	<h4>Login to Site</h4>
	<com:TLabel
		ForControl="Username" 
		Text="User Name" 
		CssClass="label"/>
	<com:TTextBox ID="Username" 
		AccessKey="u" 
		ValidationGroup="login"
		CssClass="textbox"/>
	<com:TRequiredFieldValidator 
		ControlToValidate="Username" 
		ValidationGroup="login"
		Display="Dynamic"
		ErrorMessage="*"/>

	<com:TLabel
		ForControl="Password" 
		Text="Password" 
		CssClass="label"/>
	<com:TTextBox ID="Password" 
		AccessKey="p" 
		CssClass="textbox" 
		ValidationGroup="login"
		TextMode="Password"/>
	<com:TCustomValidator
		ControlToValidate="Password"
		ValidationGroup="login"
		Text="...invalid"
		Display="Dynamic"
		OnServerValidate="validateUser" />

	<div>
	<com:TCheckBox ID="RememberMe" Text="Remember me next time"/>
	</div>

	<com:TImageButton ID="LoginButton"
		OnClick="loginButtonClicked"
		ImageUrl="<%=$this->Page->Theme->BaseUrl.'/images/button-login.gif'%>"
		ValidationGroup="login"
		CssClass="button"/>
	or
	<a href="<%=$this->Service->constructUrl('Register')%>" class="button"><img src="<%=$this->Page->Theme->BaseUrl.'/images/button-create.gif'%>" alt="Create a new account"/></a>
</com:TPanel>