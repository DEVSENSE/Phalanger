[expect php]
[file]

<?php
function _basename($cmd)
{
  global $path;

  echo "<p><code>$cmd</code><br />\n";
  eval ("print_r(" . $cmd . ");");
  echo "<br />\n";
}

$path = "/home/httpd/html/index.php";
echo basename($path);         		// "index.php"
echo basename($path, ".php") . "\n";   	// "index"

$path = "http://s.cc/path/html/index.php3";
echo basename($path) . "\n";           	// "index.php3"
echo basename($path, ".php") . "\n";   	// "index.php3"

?> 