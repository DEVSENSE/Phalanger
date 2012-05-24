<div class="portlet">

<h2 class="portlet-title">
Categories
<com:THyperLink
	Text="[+]"
	Tooltip="Create a new category"
	NavigateUrl=<%= $this->Service->constructUrl('Posts.NewCategory') %>
	Visible=<%= $this->User->IsAdmin %> />
</h2>

<div class="portlet-content">
<com:TBulletedList
	ID="CategoryList"
	DisplayMode="HyperLink"
	DataTextField="Name"
	DataValueField="ID"
	EnableViewState="false"
	/>
</div><!-- end of portlet-content -->

</div><!-- end of portlet -->
