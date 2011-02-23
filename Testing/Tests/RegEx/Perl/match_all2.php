[expect php]

[file]
<?php
preg_match_all("|<[^>]+?>(.*?)</[^>]+?>|", 
   "<b>example: </b><div align=\"left\">this is a test</div>", 
   $out, PREG_SET_ORDER);
echo $out[0][0] . ", " . $out[0][1] . "\n";
echo $out[1][0] . ", " . $out[1][1] . "\n";
?> 