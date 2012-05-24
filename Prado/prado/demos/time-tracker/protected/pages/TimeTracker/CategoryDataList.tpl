  	
<table class="categorylist">
  <tr>
	<th>Category Name</th>
	<th>Abbreviation</th>
	<th>Estimate Duration</th>
	<th></th>
  </tr>
<com:TDataList ID="categories"
	RepeatLayout="Raw"
	DataKeyField="ID"
	OnEditCommand="editCategoryItem"
	OnCancelCommand="refreshCategoryList"
	OnUpdateCommand="updateCategoryItem"
	OnDeleteCommand="deleteCategoryItem">

	<prop:ItemTemplate>
	  <tr>
	  	<td class="categoryName"><%# h($this->DataItem->Name) %></td>
	  	<td class="abbrev"><%# h($this->DataItem->Abbreviation) %></td>
	  	<td class="duration"><%# h($this->DataItem->EstimateDuration) %></td>
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
	  	  <com:TTextBox ID="name" Text=<%# $this->DataItem->Name %> />
	  	  <span class="required">*</span>
	  	    	<com:TRequiredFieldValidator
		  		ControlToValidate="name"
		  		Display="None"
		  		CssClass="validator"
		  		ControlCssClass="required-input"
		  		ValidationGroup="category-update"
		  		ErrorMessage="Please enter the category name." />
	  	</td>
	  	<td class="abbrev">
	  		<com:TTextBox ID="abbrev" Text=<%# $this->DataItem->Abbreviation %> />
	  		<span class="required">*</span>
		  	<com:TRequiredFieldValidator
		  		ControlToValidate="abbrev"
		  		Display="None"
		  		CssClass="validator"
		  		ValidationGroup="category-update"
		  		ControlCssClass="required-input"
		  		ErrorMessage="Please enter an abbreviation." />
		  	<com:TRegularExpressionValidator
		  		ControlToValidate="abbrev"
		  		Display="None"
		  		CssClass="validator"
		  		ValidationGroup="category-update"
		  		ControlCssClass="required-input1"
		  		RegularExpression="[a-zA-Z0-9]*"
		  		ErrorMessage="Abbreviation must be alphanumeric." />	  		
	  	</td>
	  	<td class="duration">
	  		<com:TTextBox ID="duration" Text=<%# $this->DataItem->EstimateDuration %> />
	  		<span class="required">*</span>
	  		<com:TRequiredFieldValidator
		  		ControlToValidate="duration"
		  		Display="None"
		  		CssClass="validator"
		  		ValidationGroup="category-update"
		  		ControlCssClass="required-input"
		  		ErrorMessage="Please enter a duration." />
		  	<com:TRangeValidator 
		  		ControlToValidate="duration"
		  		DataType="Float"
		  		MinValue="0"
		  		MaxValue="9999"
		  		CssClass="validator"
		  		Display="None"
		  		ValidationGroup="category-update"
		  		ControlCssClass="required-input1"
		  		ErrorMessage="Duration must be between 0 and 9999." /> 
	  	</td>
	  	<td class="edit">
	  		<com:TButton Text="Save" CommandName="update" ValidationGroup="category-update"/>
	  		<com:TButton Text="Cancel" CommandName="cancel" />
	  	</td>
	  </tr>	
	</prop:EditItemTemplate>
	
</com:TDataList>
  <tr>
  	<td class="categoryName">
  	<com:TTextBox ID="categoryName" />
  	<com:TRequiredFieldValidator
  		ControlToValidate="categoryName"
  		Display="None"
  		CssClass="validator"
  		ValidationGroup="category-add"
  		ControlCssClass="required-input"
  		ErrorMessage="Please enter the category name." />
  	</td>
  	
  	<td class="abbrev">
  	<com:TTextBox ID="abbrev" />
  	<com:TRequiredFieldValidator
  		ControlToValidate="abbrev"
  		Display="None"
  		CssClass="validator"
  		ValidationGroup="category-add"
  		ControlCssClass="required-input"
  		ErrorMessage="Please enter an abbreviation." />
  	<com:TRegularExpressionValidator
  		ControlToValidate="abbrev"
  		Display="None"
  		CssClass="validator"
  		ValidationGroup="category-add"
  		RegularExpression="[a-zA-Z0-9]*"
  		ControlCssClass="required-input1"
  		ErrorMessage="Abbreviation must be alphanumeric." />
  	</td>
  	
    <td class="duration">
  	<com:TTextBox ID="duration" />
  	<com:TRequiredFieldValidator
  		ControlToValidate="duration"
  		Display="None"
  		CssClass="validator"
  		ValidationGroup="category-add"
  		ControlCssClass="required-input"
  		ErrorMessage="Please enter a duration." />
  	<com:TRangeValidator 
  		ControlToValidate="duration"
  		DataType="Float"
  		MinValue="0"
  		MaxValue="9999"
  		CssClass="validator"
  		Display="None"
  		ValidationGroup="category-add"
  		ControlCssClass="required-input1"
  		ErrorMessage="Duration must be between 0 and 9999." />  	
  	</td>
  	<td class="edit">
  		<com:TButton Text="Add Category" OnClick="addCategory_clicked" ValidationGroup="category-add" />
  	</td>	
  </tr>
</table>
<com:TValidationSummary
	AutoUpdate="false"
	ValidationGroup="category-add" />
<com:TValidationSummary
	AutoUpdate="false"
	ValidationGroup="category-update" />
