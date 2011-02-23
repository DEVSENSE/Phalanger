[exact php]
[file]
<?php

require_once ('Phalanger.inc');

echo "1</br>\n";

preg_match('#(?<name>(?i)rah)\s+(?P=name)#',"rah rah",$matches);
__var_dump($matches);

echo"</br>\n</br>\n";

echo "2</br>\n";

preg_match('#(?<name>(?i)rah)\s+\k<name>#',"rah rah",$matches);
__var_dump($matches);

echo"</br>\n</br>\n";

echo "3</br>\n";

preg_match('#(?<name>(?i)rah)\s+\k\'name\'#',"rah rah",$matches);
__var_dump($matches);

echo"</br>\n</br>\n";

echo "4</br>\n";

preg_match('#(?<name>(?i)rah)\s+\k{name}#',"rah rah",$matches);
__var_dump($matches);

echo"</br>\n</br>\n";

echo "5</br>\n";

preg_match('#(?<name>(?i)rah)\s+\g{name}#',"rah rah",$matches);
__var_dump($matches);

?>