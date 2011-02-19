[expect php]
[file]
<?php

require_once ('Phalanger.inc');

preg_match("/(?P<jmeno>hovno)/","praseci hovno",$matches, PREG_OFFSET_CAPTURE, 0);
__var_dump($matches);

preg_match("/(?'jmeno1'hovno)/","praseci hovno",$matches, PREG_OFFSET_CAPTURE, 0);
__var_dump($matches);

preg_match("/(?<jmeno2>hovno)/","praseci hovno",$matches, PREG_OFFSET_CAPTURE, 0);
__var_dump($matches);
?>