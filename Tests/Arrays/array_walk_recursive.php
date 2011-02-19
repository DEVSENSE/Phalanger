[expect php]
[file]
<?php
  $sweet = array('a' => 'apple', 'b' => 'banana');
  $fruits = array('sweet' => $sweet, 'x' =>& $sweet, 'sour' => 'lemon');

  function test_print($item, $key) 
  {
    echo "$key holds $item\n";
  }

  array_walk_recursive($fruits, 'test_print');
?>