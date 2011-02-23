[expect php]

[file]
<?
$text = "Hello World. Today I visited http://www.google.com/ for the first time";

$text = preg_replace("/(http:\/\/(.*)\/)[\S]*/", "<a href=\\1>\\1</a> ", $text);

echo $text;
?> 
