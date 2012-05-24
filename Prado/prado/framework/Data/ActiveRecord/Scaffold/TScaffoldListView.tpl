<div class="scaffold_list_view">
<div class="item-header">
<com:TRepeater ID="_header">
	<prop:ItemTemplate>
		<com:TLabel Text=<%# $this->DataItem %> CssClass="field field_<%# $this->ItemIndex %>"/>
	</prop:ItemTemplate>
</com:TRepeater>

<span class="sort-options">
	<com:TDropDownList ID="_sort" AutoPostBack="true"/>
</span>

</div>

<div class="item-list">
<com:TRepeater ID="_list"
	AllowPaging="true"
     AllowCustomPaging="true"
     onItemCommand="bubbleEvent"
     onItemCreated="listItemCreated"
	 PageSize="10">
	<prop:ItemTemplate>
	<div class="item item_<%# $this->ItemIndex % 2 %>">

	<com:TRepeater ID="_properties">
		<prop:ItemTemplate>
		<span class="field field_<%# $this->ItemIndex %>">
			<%# htmlspecialchars($this->DataItem) %>
		</span>
		</prop:ItemTemplate>
	</com:TRepeater>

	<span class="edit-delete-buttons">
		<com:TButton Text="Edit"
			Visible=<%# $this->NamingContainer->Parent->EditViewID !== Null %>
			CommandName="edit"
			CssClass="edit-button"
			CausesValidation="false" />
		<com:TButton Text="Delete"
			CommandName="delete"
			CssClass="delete-button"
			CausesValidation="false"
			Attributes.onclick="if(!confirm('Are you sure?')) return false;"  />
	</span>

	</div>
	</prop:ItemTemplate>
</com:TRepeater>
</div>

<com:TPager ID="_pager"
	CssClass="pager"
	ControlToPaginate="_list"
	PageButtonCount="10"
	Mode="Numeric"
	OnPageIndexChanged="pageChanged" />
</div>