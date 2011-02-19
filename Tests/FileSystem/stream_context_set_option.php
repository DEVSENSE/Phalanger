[expect php]
[file]

<?php
require(dirname(__FILE__).'/fs.inc');

$opts = array(
  'http'=>array(
    'method'=>"GET",
    'header'=>"Accept-language: en\r\n" . 
              "Cookie: foo=bar\r\n"
  ),
  'other'=>array(
    'method'=>"otherGET",
    'header'=>"otherAccept-language: en\r\n" . 
              "otherCookie: foo=bar\r\n"
  ),
  'file'=>array(
    'method'=>"otherGET",
    'header'=>"otherAccept-language: en\r\n" . 
              "otherCookie: foo=bar\r\n"
  )
);


$c = stream_context_create($opts);
//$fr = fopen('fs.inc', 'rb', 0, $c);
$fr = @fopen('http://www.google.com/', 'rb', 0, $c);
if ($fr === false) die('NO NETWORK CONNECTION!');

stream_context_set_option($c, "wrap", "op1", "DIRECT");
stream_context_set_option($fr, "wrap", "op2", "VIA STREAM");
stream_context_set_option($c, "http", "op1", "DIRECT");
stream_context_set_option($fr, "http", "op2", "VIA STREAM");

d(stream_context_get_options($c));
d(stream_context_get_options($fr));

/* Note: in PHP only some wrappers accept the context; no filtering by wrapper name.
$fr = fopen('fs.inc', 'rb', 0, $c);

stream_context_set_option($fr, "wrap", "op3", "VIA STREAM");
stream_context_set_option($fr, "file", "op3", "VIA STREAM");

d(stream_context_get_options($fr));
/**/

// static include to compile fs.inc into assembly
require_once('fs.inc');

?> 
