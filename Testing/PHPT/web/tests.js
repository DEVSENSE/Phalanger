var files = [];
var files_requested = 0;

var pending_requests = 0;
var max_requests = 1;

var paused = true;	// if true, no new tests are started

function start_test() {
    while (do_next_tests());

    // try more tests after some time
    if (files_requested < files.length)
        setTimeout("start_test()", 5000);
    else {
        $('#infotxt').text("PHPT: Done! " + files.length + ' files tested.');
        $('#list').after("<hr/><center><b>DONE</b></center>");
        $('#startbtn').unbind('click').css('color','#333');
    }
}

function decreasePending() {
    if (pending_requests > 0)
        pending_requests--;

    setTimeout("decreasePending()", 30000);
}

function do_next_tests() {
    if (pending_requests >= max_requests || files_requested >= files.length || paused)
        return false;

    pending_requests++;

    var url = document.location.origin + document.location.pathname;

    var jqxhr = $.get(url + '?location=' + escape(url) + '&test=' + escape(files[files_requested++]))
      .success(function (data, textStatus) {
          $('#list').append("<li>" + data + "</li>");
      })
      .error(function (data) {
          $('#list').append("<li style='color:#f00;'>Request Error</li>");
      })
      .complete(function () {
          pending_requests--;
          do_next_tests();
      });    

    $('#infotxt').text("Progress: " + files_requested + "/" + files.length);

    return true;
}

// get query param value
function gup( name )
{
  name = name.replace(/[\[]/,"\\\[").replace(/[\]]/,"\\\]");
  var regexS = "[\\?&]"+name+"=([^&#]*)";
  var regex = new RegExp( regexS );
  var results = regex.exec( window.location.href );
  if( results == null )
    return "";
  else
    return results[1];
}

$().ready( function(){

	// time to time decrease pending requests, since they can timeout and not complete ?
	decreasePending();

	// top bar with filters and buttons
	var body = $('body');
	body.css('margin-top',"34px");
	body.prepend(
	'<div style="padding:4px;position:fixed;top:0;left:0;display:block;width:100%;background:#ccc;border-bottom:2px solid #888;">' +
		'<a href="#" id="startbtn" style="text-decoration:none;padding:0 8px 0 8px;">Start testing!</a> | ' +
		'Filter tests: <input id="filterval" name="filter" value="" style="background:#eee;border:1px solid;" /> | ' +
		'<span id="infotxt"></a> | ' +
	'</div>');
	
	//
	$('#infotxt').text(files.length + " tests to go");
	$('#startbtn').click(function(){ 
		paused = !paused;
		if (!files_requested)
			start_test();
		
		$(this).text(paused ? "Continue" : "Pause");		
		return false;
	});
	
	var filter = $('#filterval');
	filter.val(gup('filter'));
	filter.keyup(function(e) {
		if(e.keyCode == 13) {
			pause = true;
			document.location = document.location.origin + document.location.pathname + '?filter=' + filter.val();
		}
	});
} );
