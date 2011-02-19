[expect php]
[file]
<?php
  require('Phalanger.inc');
__var_dump(sprintf("%3.2f", 1.2));
__var_dump(sprintf("%-3.2f", 1.2));
__var_dump(sprintf("%03.2f", 1.2));
__var_dump(sprintf("%-03.2f", 1.2));
echo "\n";
__var_dump(sprintf("%5.2f", 3.4));
__var_dump(sprintf("%-5.2f", 3.4));
__var_dump(sprintf("%05.2f", 3.4));
__var_dump(sprintf("%-05.2f", 3.4));
echo "\n";
__var_dump(sprintf("%7.2f", -5.6));
__var_dump(sprintf("%-7.2f", -5.6));
__var_dump(sprintf("%07.2f", -5.6));
__var_dump(sprintf("%-07.2f", -5.6));
echo "\n";
__var_dump(sprintf("%3.4f", 1.2345678));

?>