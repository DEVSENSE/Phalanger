[expect php]
[file]
<?php 
$line = '10.0.0.2 - - [17/Mar/2003:18:03:08 +1100] "GET /images/org_background.gif HTTP/1.0" 200 2321 "http://10.0.0.3/login.php" "Mozilla/5.0 Galeon/1.2.7 (X11; Linux i686; U;) Gecko/20021203"'; 

$elements = preg_split('/^(\S+) (\S+) (\S+) \[([^\]]+)\] "([^"]+)" (\S+) (\S+) "([^"]+)" "([^"]+)"/', $line,-1,PREG_SPLIT_DELIM_CAPTURE | PREG_SPLIT_NO_EMPTY); 

print_r($elements); 
?> 
