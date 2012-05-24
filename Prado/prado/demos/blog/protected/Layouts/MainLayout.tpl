<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en" >

<com:THead Title=<%$ SiteTitle %> >
<meta http-equiv="Expires" content="Fri, Jan 01 1900 00:00:00 GMT"/>
<meta http-equiv="Pragma" content="no-cache"/>
<meta http-equiv="Cache-Control" content="no-cache"/>
<meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
<meta http-equiv="content-language" content="en"/>
</com:THead>

<body>
<div id="page">
<com:TForm>

<div id="header">
<h1 id="header-title"><a href="<%=$this->Request->ApplicationUrl %>"><%$ SiteTitle %></a></h1>
<h2 id="header-subtitle"><%$ SiteSubtitle %></h2>
</div><!-- end of header -->

<div id="main">
<com:TContentPlaceHolder ID="Main" />
</div><!-- end of main -->

<div id="sidebar">

<com:Application.Portlets.LoginPortlet Visible=<%= $this->User->IsGuest %>/>

<com:Application.Portlets.AccountPortlet Visible=<%= !$this->User->IsGuest %>/>

<com:Application.Portlets.SearchPortlet />

<com:Application.Portlets.CategoryPortlet />

<com:Application.Portlets.ArchivePortlet />

<com:Application.Portlets.CommentPortlet />

</div><!-- end of sidebar -->

<div id="footer">
Copyright &copy; 2006 <%$ SiteOwner %>.<br/>
<%= Prado::poweredByPrado() %>
</div><!-- end of footer -->

</com:TForm>
</div><!-- end of page -->
</body>
</html>