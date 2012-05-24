<div class="portlet">

<h2 class="portlet-title">Account</h2>

<div class="portlet-content">
Welcome, <b><%= $this->User->Name %></b>!
<ul>
<li><a href="<%= $this->Service->constructUrl('Posts.NewPost') %>">New post</a></li>
<li><a href="<%= $this->Service->constructUrl('Posts.MyPost') %>">My post</a></li>
<li><a href="<%= $this->Service->constructUrl('Users.ViewUser',array('id'=>$this->User->ID)) %>">Profile</a></li>
<%%
if($this->User->IsAdmin)
    echo '<li><a href="'.$this->Service->constructUrl('Admin.PostMan').'">Admin</a></li>';
%>
<li><com:TLinkButton Text="Logout" OnClick="logout" /></li>
</ul>

</div><!-- end of portlet-content -->

</div><!-- end of portlet -->
