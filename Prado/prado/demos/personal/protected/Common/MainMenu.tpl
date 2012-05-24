<a href="<%=$this->Service->constructUrl('Home') %>" >HOME</a> |
<a href="<%=$this->Service->constructUrl('Resume') %>" >RESUME</a> |
<a href="<%=$this->Service->constructUrl('Links') %>" >LINKS</a> |
<a href="<%=$this->Service->constructUrl('Albums') %>" >ALBUMS</a> |
<a href="<%=$this->Service->constructUrl('Register') %>" >REGISTER</a> |
<com:THyperLink
	NavigateUrl="<%=$this->Service->constructUrl('UserLogin') %>"
	Text="LOGIN"
	Visible="<%= $this->User->IsGuest %>"
	/>
<com:TLinkButton
	Text="LOGOUT"
	Visible="<%= !$this->User->IsGuest %>"
	OnClick="logout"
	/>
