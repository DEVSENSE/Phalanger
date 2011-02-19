[expect php]
[file]

<?php
// $df contains the number of bytes available on "/"
echo disk_total_space("/") . "\n";

// On Windows:
echo disk_total_space("C:") . "\n";
echo disk_total_space("D:") . "\n";
//echo disk_total_space("E:") . "\n";
//echo disk_total_space("F:") . "\n";
?> 