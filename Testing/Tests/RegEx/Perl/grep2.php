[expect php]

[file]
<?

$food = array('apple', 'banana', 'squid', 'pear');
$fruits = preg_grep("/squid/", $food, PREG_GREP_INVERT);
echo "Food  "; print_r($food);
echo "Fruit "; print_r($fruits);

?>