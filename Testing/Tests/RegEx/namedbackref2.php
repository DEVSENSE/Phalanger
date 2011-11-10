[expect php]
[file]
<?php

require_once ('Phalanger.inc');

echo "1</br>\n";

preg_match('#(?<2name>(?i)rah)\s+(?P=2name)#',"rah rah",$matches);
__var_dump($matches);

echo"</br>\n</br>\n";

echo "2</br>\n";

preg_match('#(?<2name>(?i)rah)\s+\k<2name>#',"rah rah",$matches);
__var_dump($matches);

echo"</br>\n</br>\n";

echo "3</br>\n";

preg_match('#(?<2name>(?i)rah)\s+\k\'2name\'#',"rah rah",$matches);
__var_dump($matches);

echo"</br>\n</br>\n";

echo "4</br>\n";

preg_match('#(?<2name>(?i)rah)\s+\k{2name}#',"rah rah",$matches);
__var_dump($matches);

echo"</br>\n</br>\n";

echo "5</br>\n";

preg_match('#(?<2name>(?i)rah)\s+\g{2name}#',"rah rah",$matches);
__var_dump($matches);

?>