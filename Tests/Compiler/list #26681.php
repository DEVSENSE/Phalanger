[expect php]
[file]
<?php

$p = array();

$x = (list( $p["first"], list( list($a), $b ), ) = array( 1, array(array(2),3),"text" ));

var_dump($x, $p, $a, $b);

?>