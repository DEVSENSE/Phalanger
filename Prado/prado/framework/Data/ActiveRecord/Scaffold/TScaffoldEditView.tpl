<div class="scaffold_edit_view">
<div class="edit-inputs">
<com:TRepeater ID="_repeater" onItemCreated="createRepeaterEditItem">
	<prop:ItemTemplate>
	<div class="edit-item item_<%# $this->ItemIndex % 2 %>
		input_<%# $this->ItemIndex %> property_<%# $this->DataItem->ColumnId %>">
		<com:TLabel ID="_label" CssClass="item-label"/>
		<span class="item-input">
			<com:TPlaceHolder ID="_input" />
		</span>
	</div>
	</prop:ItemTemplate>
</com:TRepeater>
</div>

<div class="edit-page-buttons">
<com:TButton ID="_save" Text="Save" CommandName="save" ValidationGroup=<%= $this->ValidationGroup %>/>
<com:TButton ID="_clear" Text="Clear" CommandName="clear" CausesValidation="false"/>
<com:TButton ID="_cancel" Text="Cancel" CommandName="cancel" CausesValidation="false" Visible="false"/>
</div>
</div>