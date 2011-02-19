[expect php]
[file]
<xmp>
<?php

$fr = fopen(__FILE__, "rb");
fpassthru($fr);
fclose($fr); 

?> 
</xmp>