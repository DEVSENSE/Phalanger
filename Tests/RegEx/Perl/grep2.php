[expect exact]
Food  array
(
  [0] => apple
  [1] => banana
  [2] => squid
  [3] => pear
)
Fruit array
(
  [0] => apple
  [1] => banana
  [3] => pear
)

[file]
<?

$food = array('apple', 'banana', 'squid', 'pear');
$fruits = preg_grep("/squid/", $food, PREG_GREP_INVERT);
echo "Food  "; print_r($food);
echo "Fruit "; print_r($fruits);

?>