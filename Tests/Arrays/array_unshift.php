[expect php]
[file]
<?
  $queue = array("orange", "banana"); 
  array_unshift($queue, "apple", "raspberry"); 
  var_dump($queue);
  
  $queue = array("a" => 1, 4 => 2, "b" => 4, 3 => 1); 
  array_unshift($queue, "apple", 1, "raspberry"); 
  var_dump($queue);
?>