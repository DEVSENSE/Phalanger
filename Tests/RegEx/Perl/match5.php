[expect php]

[file]
<?php
// get host name from URL
preg_match("/^(http:\/\/)?([^\/]+)/i",
   "http://www.php.net/index.html", $matches);
$host = $matches[2];

// get last two segments of host name
preg_match("/[^\.\/]+\.[^\.\/]+$/", $host, $matches);
echo "domain name is: {$matches[0]}\n";
?>