[expect exact]
array
(
  [0] => 209.6.145.47 - - [22/Nov/2003:19:02:30 -0500] "GET /dir/doc.htm HTTP/1.0" 200 6776 "http://search.yahoo.com/search?p=key+words=UTF-8" "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; .NET CLR 1.1.4322)"
  [1] => 209.6.145.47
  [2] => -
  [3] => -
  [4] => 22/Nov/2003:19:02:30 -0500
  [5] => GET
  [6] => /dir/doc.htm
  [7] => HTTP
  [8] => 1.0
  [9] => 200
  [10] => 6776
  [11] => "http://search.yahoo.com/search?p=key+words=UTF-8"
  [12] => "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; .NET CLR 1.1.4322)"
)

[file]
<?php

$line_in = '209.6.145.47 - - [22/Nov/2003:19:02:30 -0500] "GET /dir/doc.htm HTTP/1.0" 200 6776 "http://search.yahoo.com/search?p=key+words=UTF-8" "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; .NET CLR 1.1.4322)"';

if (preg_match('!^([^ ]+) ([^ ]+) ([^ ]+) \[([^\]]+)\] "([^ ]+) ([^ ]+) ([^/]+)/([^"]+)" ([^ ]+) ([^ ]+) ([^ ]+) (.+)!',
  $line_in,
  $elements))
{
  print_r($elements);
}

?>