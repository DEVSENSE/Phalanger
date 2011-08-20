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
        document.title = "PHPT: Done";
		$('#list').after("<hr/><center><b>DONE</b></center>");
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

    var jqxhr = $.get(document.location + '?location=' + escape(document.location) + '&test=' + escape(files[files_requested++]))
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

    document.title ="PHPT: " + files_requested + "/" + files.length;

    return true;
}

// time to time decrease pending requests, since they can timeout and not complete ?
decreasePending();