var currentCommentID;

function show_comment_list()
{
	$('comment-list').show();
	$('add-comment').hide();
	$('show-comment-link').addClassName("active");
	$('add-comment-link').removeClassName("active");
	$('all-comments-link').removeClassName("active");
	show_comments_in_list(currentCommentID);
}

function show_all_comments()
{
	$('comment-list').show();
	$('add-comment').hide();
	$('show-comment-link').removeClassName("active");
	$('add-comment-link').removeClassName("active");
	$('all-comments-link').addClassName("active");
	show_comments_in_list();
}


function show_add_comment()
{
	$('comment-list').hide();
	$('add-comment').show();
	$('show-comment-link').removeClassName("active");
	$('add-comment-link').addClassName("active");
	$('all-comments-link').removeClassName("active");
}

function hide_add_comment()
{
	$('user-comments').hide();
	$('modal-background').hide();
	$(content_textare_id).value = '';
	$(currentCommentID).style.zIndex = 0;
}

function show_comments_in_list(id)
{
	var list = $('comment-list');
	var count=0;
	for(var i=0, k=list.childNodes.length; i < k; i++)
	{
		var node = list.childNodes[i];
		if(node.nodeType == 1) //an element node
		{
			if(typeof(id) == "undefined" || node.className.indexOf(id) >= 0)
			{
				node.style.display="block"
				count++;
			}
			else
				node.style.display="none";
		}
	}
	return count;
}

function show_comments(block)
{
	var id = block.id
	currentCommentID = id;
	$(hidden_block_id).value = id;
	var commentBlock = $('user-comments');
	var pos = Position.cumulativeOffset(block);
	var top = pos[1] + block.offsetHeight;
	commentBlock.style.top = top+"px";
	commentBlock.style.width = (block.offsetWidth-22)+"px";

	commentBlock.show();

	var count = show_comments_in_list(id);

	$('comment-list').show();
	if(count > 0)
		show_comment_list();
	else
		show_add_comment();

	var background = $('modal-background');
	background.style.top="0px";
	background.style.left="0px";
	background.style.opacity="0.5";
	background.style.width = document.body.offsetWidth+"px";
	background.style.height = document.body.offsetHeight+"px";
	background.show();
	block.style.zIndex = 100;
	block.style.paddingRight="9px";
	block.style.marginRight="-9px";

}

function show_block(block)
{
	while(block && (!block.className || block.className.indexOf("block-content") < 0))
		block = block.parentNode;
	if(block && block.className.indexOf("block-content") >= 0)
	{
		block.addClassName('block-hover');
		var tag = $('tag-'+block.id);
//		if(tag.className.indexOf("tag-shown")<=0)
			tag.firstChild.style.visibility="visible"
	}
}

function hide_block(block)
{
	while(block && (!block.className || block.className.indexOf("block-content") < 0))
		block = block.parentNode;
	if(block && block.className.indexOf("block-hover") >= 0)
	{
		block.removeClassName('block-hover');
		var tag = $('tag-'+block.id);
		if(tag.className.indexOf("tag-shown")<=0)
			tag.firstChild.style.visibility="hidden"
	}
}

function add_comment_tag(el)
{
	var dim = Element.getDimensions(el);
	var comments = get_comment_count(el.id);
	var style = "height:"+(dim.height > 35 ? dim.height : 35)+"px;";
	var cssClass = dim.height ? "block-comment-tag" : "block-comment-tag-ie";
	var title = "View "+comments+" comments"
	var innerStyle="";
	if(comments <= 0)
	{
		innerStyle = " visibility:hidden;";
		comments = "add";
		title = "Add new comment";
	}
	else
		cssClass += " tag-shown";
	var id = "tag-"+el.id;
	var tag = "<div id='"+id+"' class='"+cssClass+"' style='"+style+"'><div style='"+innerStyle+"' title='"+title+"'>"+comments+"</div>&nbsp;</div>";
	new Insertion.Before(el, tag);
	var tag_div = $(id);
	Event.observe(tag_div, "mouseover", function(e){ if(typeof(show_block)!="undefined") show_block(el); });
	Event.observe(tag_div, "mouseout", function(e){ if(typeof(hide_block) !="undefined") hide_block(el); });
	Event.observe(tag_div, "click", function(e) { if(typeof(show_comments) !="undefined") show_comments(el); Event.stop(e); });
}

function increment_count_tag(id)
{
	var tag = $('tag-'+id);
	if(tag && tag.firstChild)
	{
		if(tag.className.indexOf("tag-shown") >= 0)
		{
			var count = Number(tag.firstChild.innerHTML);
			tag.firstChild.innerHTML = (++count)+"";
			tag.firstChild.style.visibility="visible";
		}
		else
		{
			tag.firstChild.innerHTML = "1";
			tag.addClassName("tag-shown");
			tag.firstChild.style.visibility="visible";
		}
	}
}

function get_comment_count(id)
{
	return comment_count[id] ? comment_count[id] : 0;
}

//initialize the comment js

if(!Prado.Browser.ie) //not IE 4,5,6
{
	(function()
	{
		var userComments = $('user-comments');
		userComments.style.position="absolute";
		userComments.style.marginRight="80px";
		var commentList = $('comment-list');
		commentList.style.height="320px";
		$('add-comment').style.height="320px";
		commentList.style.overflow="auto";
		$('show-comment-link').style.display="";
		$('to-top').hide();
		$('close-comments').show();
		$('all-comments-link').show();
		userComments.hide();
		$('comments-header').hide();

		$$('#comment-list .source-link').each(function(el){ el.hide(); });

		$$('#content .block-content').each(function(el)
		{
			Event.observe(el, 'mouseover', function(e){ if(typeof(show_block)!="undefined") show_block(Event.element(e)); });
			Event.observe(el, 'mouseout', function(e){ if(typeof(hide_block)!="undefined") hide_block(Event.element(e)); });
			add_comment_tag(el);
		});

		Event.observe($('show-comment-link'), "click", function(e) { show_comment_list(); Event.stop(e); });
		Event.observe($('add-comment-link'), "click", function(e) { show_add_comment();	Event.stop(e); });
		Event.observe($('all-comments-link'), "click", function(e) { show_all_comments();	Event.stop(e); });
		Event.observe($('close-comments'), "click", function(e) { hide_add_comment(); Event.stop(e); });

	})();
}