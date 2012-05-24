<com:TStyleSheet StyleSheetUrl=<%~ comments.css %> />
<!-- 
	<%~ right_back.png %>
	<%~ right_tag.png %>  
	<%~ tag.gif %>  
-->
<h2 style="margin-bottom: 0.4em; margin-top: 2em; border-bottom: 0 none" id="comments-header">User Comments</h2>
<div id="user-comments">
	<ul id="comment-nav">
		<li><a href="#comment-list" style="display:none" id="show-comment-link" >View Comments</a></li>
		<li><a href="#add-comment" class="active" id="add-comment-link">Add New Comment</a></li>
		<li><a href="#comment-list" style="display:none" id="all-comments-link">View All Comments</a></li>
	</ul>
	<a href="javascript:;" class="right-tab" style="display:none" id="close-comments">Close</a>
	<a href="#" class="right-tab" id="to-top">Top</a>
<div id="comment-list">
	<com:TRepeater ID="comments">
		<prop:ItemTemplate>
		<%# $this->parent->parent->format_message($this->DataItem) %>
		</prop:ItemTemplate>
	</com:TRepeater>
</div>
	<div id="add-comment">

		<div class="username">
			<div>
			<com:TLabel ForControl="username" Text="Username/Password:" />
			<span class="hint">(must have 5 or more posts in <a href="http://www.pradosoft.com/forum/" title="Prado Forums">forum</a>)</span>
			<com:TRequiredFieldValidator
				Style="font-weight: bold"
				ValidationGroup="comment-group"
				ControlToValidate="username"
				ErrorMessage="*" />
			</div>
			<com:TTextBox ID="username" /> /
			<com:TTextBox ID="password" TextMode="Password" />
			<com:TActiveCustomValidator
				ID="credential_validator"
				ControlToValidate="password"
				ValidationGroup="comment-group"
				OnServerValidate="validate_credential"
				EnableClientScript="false"
				ErrorMessage="Incorrect username/password" />
		</div>
		<div class="content">
			<div>
			<com:TLabel ForControl="content" Text="Comment:" />
			<com:TRequiredFieldValidator
				Style="font-weight: bold"
				ValidationGroup="comment-group"
				ControlToValidate="content"
				ErrorMessage="*" />
			</div>
			<com:TActiveTextBox TextMode="MultiLine" ID="content" />
			<com:THiddenField ID="block_id" Value="top-content"/>
		</div>

		<div class="submit">
			<com:TActiveButton ID="addComment" ValidationGroup="comment-group" Text="Add Comment" OnClick="add_comment">
				<prop:ClientSide OnLoading="show_posting_comment()" OnSuccess="on_posting_success()" />
			</com:TActiveButton>
			<img id="comment-loading" src=<%~ ajax-loader.gif %> style="border:0 none;display:none"/>
		</div>
	</div>
</div>

<div id="modal-background"></div>
<com:TClientScript PradoScripts="prado" ScriptUrl=<%~ comments.js %> >
	var hidden_block_id = '<%= $this->block_id->ClientID %>';
	var content_textare_id = '<%= $this->content->ClientID %>';

function show_posting_comment()
{
	var button = $('<%= $this->addComment->ClientID %>')
	button.value="Posting ...";
	button.disabled = true;
	$('comment-loading').show();
}

function on_posting_success()
{
	var button = $('<%= $this->addComment->ClientID %>')
	button.value="Add Comment";
	button.disabled = false;
	$('comment-loading').hide();	
}
</com:TClientScript>
