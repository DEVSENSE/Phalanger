<com:TPanel CssClass="sitemap" Visible="true">
<ul class="level1">
	<li class="<com:TPlaceHolder ID="LogMenu" />">
		<a class="menuitem" href="?page=TimeTracker.LogTimeEntry">
		<img src="<%= $this->Page->Theme->BaseUrl %>/time.gif" width="16" height="16" alt="">
		<span>Log</span></a>
	</li>
	<com:TPlaceHolder Visible=<%= $this->User->isInRole('manager') %> >
	<li class="<com:TPlaceHolder ID="ReportMenu" />">
		<a class="menuitem" href="?page=TimeTracker.ReportProject">
		<img src="<%= $this->Page->Theme->BaseUrl %>/report.gif" width="16" height="16" alt="">
		<span>Reports</span></a>
		<ul class="level2">
			<li><a href="?page=TimeTracker.ReportProject">Project Reports</a></li>
			<li><a href="?page=TimeTracker.ReportResource">Resources Report</a></li>
		</ul>
	</li>
	<li class="<com:TPlaceHolder ID="ProjectMenu" />">
		<a class="menuitem" href="?page=TimeTracker.ProjectList">
		<img src="<%= $this->Page->Theme->BaseUrl %>/bell.gif" width="16" height="16" alt="">
		<span>Projects</span></a>
		<ul class="level2">
			<li><a href="?page=TimeTracker.ProjectDetails">Create New Project</a></li>
			<li><a href="?page=TimeTracker.ProjectList">List Projects</a></li>
		</ul>
	</li>
	</com:TPlaceHolder>
	<com:TPlaceHolder Visible=<%= $this->User->isInRole('admin') %> >
	<li class="<com:TPlaceHolder ID="AdminMenu" />">
		<a class="menuitem" href="?page=TimeTracker.UserList">
		<img src="<%= $this->Page->Theme->BaseUrl %>/group.gif" width="16" height="16" alt="">
		<span>Adminstration</span></a>
		<ul class="level2">
			<li><a href="?page=TimeTracker.UserCreate">Create New User</a></li>
			<li><a href="?page=TimeTracker.UserList">List Users</a></li>
		</ul>
	</li>
	</com:TPlaceHolder>
</ul>
<com:TClientScript PradoScripts="prado">
	Event.OnLoad(function()
	{
		menuitems = $$(".menuitem");
		menuitems.each(function(el)
		{
			Event.observe(el, "mouseover", function(ev)
			{	
				menuitems.each(function(item)
				{
					Element.removeClassName(item.parentNode, "active");
				});
				node = Event.element(ev).parentNode;
				if(node.tagName.toLowerCase() != 'li')
					node = node.parentNode;
				Element.addClassName(node, "active");
			});
		});
	});
</com:TClientScript>
</com:TPanel>