[expect] Notice:
[expect] Undefined offset (1)
[expect] 1

[file]
<?php
class Cart {
    var $items;  // Items in our shopping cart
   
    // Add $num articles of $artnr to the cart
 
    function add_item($artnr, $num) {
        $this->items[$artnr] += $num;
    }
   
    // Take $num articles of $artnr out of the cart
 
    function remove_item($artnr, $num) {
        if ($this->items[$artnr] > $num) {
            $this->items[$artnr] -= $num;
            return true;
        } else {
            return false;
        }   
    }
}

$c = new Cart();
$c->add_item(1, 5);
$b = $c->remove_item(1, 6);
echo $b;

$b = $c->remove_item(1,4);
echo $b;
?>
