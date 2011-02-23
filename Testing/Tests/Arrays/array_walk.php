[expect]
Before ...:
d. lemon
a. orange
b. banana
c. apple
... and after:
d. fruit: lemon
a. fruit: orange
b. fruit: banana
c. fruit: apple
[file]
<?
  $fruits = array("d" => "lemon", "a" => "orange", "b" => "banana", "c" => "apple"); 

  function test_alter(&$item1, $key, $prefix) 
  { 
    $item1 = "$prefix: $item1"; 
  } 

  function test_print($item2, $key) 
  { 
    echo "$key. $item2\n"; 
  } 

  echo "Before ...:\n"; 
  array_walk($fruits, 'test_print'); 

  array_walk($fruits, 'test_alter', 'fruit'); 
  echo "... and after:\n"; 

  array_walk($fruits, 'test_print'); 
?>