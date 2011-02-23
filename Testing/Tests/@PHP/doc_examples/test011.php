[expect php]

[file]
<?php
echo gettype(25/7);         // float(3.5714285714286) 
echo gettype((int) (25/7)); // int(3)
echo gettype(round(25/7));  // float(4) 
?>
