[expect php]
[file]
<xmp>
<?php
chdir(dirname(__FILE__));
define('f', 'readfile.php');

readfile(f);

echo "<hr>";

readfile(f);

echo "<hr>";

$fr = fopen(f, "rb");
fpassthru($fr);
fclose($fr); 

?> 
</xmp>