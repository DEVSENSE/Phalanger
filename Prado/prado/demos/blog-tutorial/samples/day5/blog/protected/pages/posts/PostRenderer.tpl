<div class="post-box">
<h3>
<com:THyperLink Text="<%# $this->Data->title %>"
	NavigateUrl="<%# $this->Service->constructUrl('posts.ReadPost',array('id'=>$this->Data->post_id)) %>" />
</h3>

<p>
Author:
<com:TLiteral Text="<%# $this->Data->author->username %>" /><br/>
Time:
<com:TLiteral Text="<%# date('m/d/Y h:m:sa', $this->Data->create_time) %>" />
</p>

<p>
<com:TLiteral Text="<%# $this->Data->content %>" />
</p>
</div>