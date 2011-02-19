[expect php]

[file]
<?php
$str = 'foo  o';
$str = preg_replace('/\s\s+/', ' ', $str);

// This will be 'foo o' now
echo $str;
?> 