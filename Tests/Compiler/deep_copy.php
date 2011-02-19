[expect php]
[file]
<?

/*
  Checks whether self-referencing arrays are correctly deeply-copied.
*/

$a = array(&$a);
$b = $a;

var_dump($a,$b);

$a[1] = 1;

var_dump($a,$b);
?>