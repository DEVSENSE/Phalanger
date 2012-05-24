<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en" >

<com:THead Title="PRADO QuickStart Tutorial">
<meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
<meta http-equiv="content-language" content="en"/>
</com:THead>

<body>
<com:TForm>
<div id="header">
<div class="title">Prado QuickStart Tutorial</div>
<div class="image"></div>
</div>

<com:TPanel ID="MainMenu" CssClass="mainmenu">
<div style="float:left; color:black; margin-top:-5px">
	<com:SearchBox />
</div>
<a href="?">Home</a> |
<a href="http://www.pradosoft.com">PradoSoft.com</a> |
<a href="../../docs/quickstart.pdf">PDF Version</a> |
<com:THyperLink ID="PrinterLink" Text="Printer-friendly Version" />
</com:TPanel>

<table width="100%" border="0" cellspacing="0" cellpadding="0">
<tr>
<td valign="top" width="1">
<com:TopicList ID="TopicPanel" />
</td>
<td valign="top">

<com:TRepeater ID="languages" OnItemCreated="languageLinkCreated">
	<prop:HeaderTemplate>
		<div class="languages">Available Languages: <ul>
	</prop:HeaderTemplate>
	<prop:ItemTemplate>
		<li><com:THyperLink ID="link" Text=<%# $this->DataItem %> /></li>
	</prop:ItemTemplate>
	<prop:FooterTemplate>
		</ul></div>
	</prop:FooterTemplate>
</com:TRepeater>
<div id="content">
<p class="block-content" id="top-content" style="border-color: transparent; height:1px; margin: 0; padding: 0; background-color: transparent"></p>
<com:TContentPlaceHolder ID="body" />
</div>
</td>
</tr>
</table>

<div id="footer">
Copyright &copy; 2005-2007 <a href="http://www.pradosoft.com">PradoSoft</a>.
<br/><br/>
<%= Prado::poweredByPrado() %>
<a href="http://validator.w3.org/check?uri=referer"><img border="0" src="http://www.w3.org/Icons/valid-xhtml10" alt="Valid XHTML 1.0 Transitional" height="31" width="88" /></a>
</div>

</com:TForm>
</body>
</html>