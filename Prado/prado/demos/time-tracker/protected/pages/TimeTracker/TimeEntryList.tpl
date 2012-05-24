<com:TDataList ID="entries"
	RepeatLayout="Raw"
	DataKeyField="ID"
	OnEditCommand="editEntryItem"
	OnCancelCommand="refreshEntryList"
	OnUpdateCommand="updateEntryItem"
	OnDeleteCommand="deleteEntryItem"
	OnItemCreated="EntryItemCreated" >
	<prop:EmptyTemplate>
	There are no time entries for this user.
	</prop:EmptyTemplate>
	<prop:HeaderTemplate>
	  <table class="time-entries">
	  	<tr>
	  		<th>Category Name</th>
	  		<th>Description</th>
	  		<th>Duration</th>
	  		<th>Reported Date</th>
	  		<th>Edit/Delete</th>
	  	</tr>
	</prop:HeaderTemplate>

	<prop:FooterTemplate>
	  </table>
	</prop:FooterTemplate>
	<prop:ItemTemplate>
	  <tr>
	  	<td class="categoryName"><%# h($this->DataItem->Category->Name) %></td>
	  	<td class="description"><%# h($this->DataItem->Description) %></td>
	  	<td class="duration"><%# h($this->DataItem->Duration) %></td>
	  	<td class="date">
	  		<com:System.I18N.TDateFormat 
	  			Pattern="dd/MM/yyyy"
	  			Value=<%# $this->DataItem->ReportDate %> />
	  	</td>
	  	<td class="edit">
	  		<com:TButton Text="Edit" CommandName="edit"/>	  			
	  		<com:TButton Text="Delete" CommandName="delete"
	  			Attributes.onclick="if(!confirm('Are you sure?')) return false;" />
	  	</td>
	  </tr>
	</prop:ItemTemplate>
	
	<prop:EditItemTemplate>
	  <tr>
	  	<td class="categoryName">
	  		<com:TDropDownList ID="category" />
	  	</td>
	  	<td class="description">
	  		<com:TTextBox ID="description" Text=<%# $this->DataItem->Description %> />
	  	</td>
	  	<td class="duration">
	  		<com:TTextBox ID="hours" Text=<%# $this->DataItem->Duration %> />
	  	</td>
	  	<td class="date">
	  		<com:TDatePicker InputMode="DropDownList" ID="day" 
	  			TimeStamp=<%# $this->DataItem->ReportDate %> />
	  	</td>
	  	<td class="edit">
	  		<com:TButton Text="Save" CommandName="update" ValidationGroup="entry-update"/>
	  		<com:TButton Text="Cancel" CommandName="cancel" />
	  	</td>
	  </tr>
	</prop:EditItemTemplate>	
		
</com:TDataList>