<tr>
<com:TTableCell ID="Cell"
	ForeColor="white"
	BackColor="<%# $this->ItemIndex%2 ? '#6078BF' : '#809FFF' %>"
	Text="<%#$this->Data['name'] %>"
	/>
<td>

<com:TRepeater ID="Repeater" OnItemCreated="itemCreated">
<prop:HeaderTemplate>
<table cellspacing="1">
</prop:HeaderTemplate>

<prop:ItemTemplate>
<com:TTableRow ID="Row">
  <com:TTableCell Width="70px">
    <%#$this->Data['name'] %>
  </com:TTableCell>
  <com:TTableCell Width="20">
    <%#$this->Data['age'] %>
  </com:TTableCell>
  <com:TTableCell Width="150px">
    <%#$this->Data['position'] %>
  </com:TTableCell>
</com:TTableRow>
</prop:ItemTemplate>

<prop:FooterTemplate>
</table>
</prop:FooterTemplate>
</com:TRepeater>

</td>
</tr>
