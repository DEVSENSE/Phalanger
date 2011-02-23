[expect php]
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


?>

<?php
error_reporting(E_ALL & ~E_NOTICE);

$cart = new Cart;
$cart->add_item("10", 1);

$another_cart = new Cart;
$another_cart->add_item("0815", 3);
?>

<?php
// correct, single $
$cart->items = array("10" => 2); 
var_dump($cart->items);

// invalid, because $cart->$items becomes $cart->""
//$cart->$items = array("10" => 1);

// correct, but may or may not be what was intended:
// $cart->$myvar becomes $cart->items
$myvar = 'items';
$cart->$myvar = array("10" => 1);  
var_dump($cart->items);
?>
