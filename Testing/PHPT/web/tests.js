var files = [];
var files_requested = 0;

var pending_requests = 0;
var max_requests = 1;

var timer = null;

function start_test() {
    while (do_next_tests());

    // try more tests after some time
    if (files_requested < files.length)
        setTimeout("start_test()", 100);
}

function do_next_tests() {
    if (pending_requests >= max_requests)
        return false;

    var jqxhr = $.get(document.location + '?location=' + escape(document.location) + '&test=' + escape(files[files_requested]))
      .success(function (data) {
          $('#list').append(data);
      })
      .error(function () {
          $('#list').append('<li> Tester error </br>'+data+'</li>');
      })
      .complete(function () {
          pending_requests--;
      });

    files_requested++;
    pending_requests++;

    return true;
}