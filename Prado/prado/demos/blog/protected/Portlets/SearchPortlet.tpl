<div class="portlet">

<h2 class="portlet-title">Search</h2>

<com:TPanel CssClass="portlet-content" DefaultButton="SearchButton">
Keyword
<com:TRequiredFieldValidator
	ControlToValidate="Keyword"
	ValidationGroup="search"
	Text="...is required"
	Display="Dynamic"/>
<br/>
<com:TTextBox ID="Keyword" />
<br/><br/>
<com:TLinkButton
	ID="SearchButton"
	Text="Search"
	ValidationGroup="search"
	CssClass="link-button"
	OnClick="search" />
</com:TPanel><!-- end of portlet-content -->

</div><!-- end of portlet -->
