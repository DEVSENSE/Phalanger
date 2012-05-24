<div class="submenu">
<com:XListMenu ActiveCssClass="submenu-active" InactiveCssClass="submenu-inactive">
	<com:XListMenuItem
		Text="Posts"
		PagePath="Admin.PostMan"
		NavigateUrl=<%= $this->Service->constructUrl('Admin.PostMan') %> />
	<com:XListMenuItem
		Text="Users"
		PagePath="Admin.UserMan"
		NavigateUrl=<%= $this->Service->constructUrl('Admin.UserMan') %> />
	<com:XListMenuItem
		Text="Configurations"
		PagePath="Admin.ConfigMan"
		NavigateUrl=<%= $this->Service->constructUrl('Admin.ConfigMan') %> />
</com:XListMenu>
</div>