[expect php]
[file]
<?php
$pizzas   = new ArrayIterator(array('Margarita', 'Siciliana', 'Hawaii'));
$toppings = new ArrayIterator(array('Cheese', 'Anchovies', 'Olives', 'Pineapple', 'Ham'));

$appendIterator = new AppendIterator;
$appendIterator->append($pizzas);
$appendIterator->append($toppings);

foreach ($appendIterator as $key => $item) {
    echo "$key => $item", PHP_EOL;
}

$appendIterator->append($toppings);
while($appendIterator->valid())
{
    echo $appendIterator->key() . " => " . $appendIterator->current() . "\n";
    $appendIterator->next();
}

?>