<div class="portlet">

<h2 class="portlet-title">Archives</h2>

<div class="portlet-content">
<ul>
<com:TRepeater ID="MonthList" EnableViewState="false">
	<prop:ItemTemplate>
	<li><a href="<%# $this->Service->constructUrl('Posts.ListPost',array('time'=>date('Ym',$this->DataItem))) %>"><%# date('F Y',$this->DataItem) %></a></li>
	</prop:ItemTemplate>
</com:TRepeater>
</ul>
</div><!-- end of portlet-content -->

</div><!-- end of portlet -->
