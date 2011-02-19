[expect php]

[file]
<?php
$keywords = "$40 for a g3/400";
$keywords = preg_quote($keywords, "/");
echo $keywords; // returns \$40 for a g3\/400
?> 