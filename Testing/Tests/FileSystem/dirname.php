[expect php]
[file]
<?php
require('Phalanger.inc');     
__var_dump(
  dirname("C:\\"),
  dirname("C:/"),
  dirname("somedir/somefile"),
  dirname("/"),
  dirname("/dir"),
  dirname("/dir/"),
  dirname("/etc/passwd"),
  dirname("\\etc\\passwd"),
  dirname("c:\\temp\\file.tmp"));
?> 