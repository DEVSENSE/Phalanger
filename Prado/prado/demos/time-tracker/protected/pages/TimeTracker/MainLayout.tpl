<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN" 
	"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en" lang="en">
<com:THead Title="My Company - Time Tracker - Log" />
<body>

<com:TForm>

<h1 class="heading">
	<a href="index.php">My Company 
		<span class="subheading">Time Tracker</span>
	</a>
</h1>
<div class="minheading">
<h2 class="login">
	<com:TLabel CssClass="name" Text="Welcome <%= h($this->User->Name) %>" />
	<com:THyperLink 
		Text="Login"
		NavigateUrl=<%= $this->Service->constructUrl('TimeTracker.Login') %>
		Visible=<%= $this->User->getIsGuest() %> />
	<com:THyperLink 
		Text="Logout"
		NavigateUrl=<%= $this->Service->constructUrl('TimeTracker.Logout') %>
		Visible=<%= !$this->User->getIsGuest() %> />
</h2>
<h2 class="help"><a href="?page=Welcome">Help</a></h2>
</div>

<com:Application.pages.TimeTracker.SiteMap
	Visible=<%= !$this->User->getIsGuest() %> />

<div class="main">
<com:TContentPlaceHolder ID="Main" />
</div>

</com:TForm>

<div class="copyrights">
Copyright &copy; 2005-2006 <a href="http://www.pradosoft.com">PradoSoft</a>.
<span class="note"><a href="http://www.famfamfam.com/lab/icons/silk/">Silk icons</a> by Mark James.</span>
</div>

</body>
</html>