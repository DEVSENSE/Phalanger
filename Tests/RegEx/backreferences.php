[exact php]
[file]
<?php

require_once ('Phalanger.inc');

preg_match('#((?i)rah)\s+\g{1}3#',"rah rah3",$matches);
__var_dump($matches);

preg_match('#((?i)rah)\s+\g{1}#',"rah rah3",$matches);
__var_dump($matches);

preg_match('#((?i)rah)\s+\g1#',"rah rah",$matches);
__var_dump($matches);

preg_match('#((?i)rah)\s+\1#',"rah rah",$matches);
__var_dump($matches);

?>