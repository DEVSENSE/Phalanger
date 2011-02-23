[expect php]
[file]
<?php
include('Phalanger.inc');

$ar = array(
    'element 1',
    array('subelement1')
    );

func($ar);
__var_dump($ar);

function func($a) {
  array_walk_recursive($a, 'apply');
  __var_dump($a);
}

function apply(&$input, $key) {
  $input = 'changed';
}
?>

