[expect php]

[file]
<?php
$string = "Don't split words";
echo substr($string, 0, 10); // Returns "Don't spli"

$pattern = "/(^.{0,10})(\W+.*$)/"; 
$replacement = "\${1}";
echo preg_replace($pattern, $replacement, $string); // Returns "Don't"
?> 
