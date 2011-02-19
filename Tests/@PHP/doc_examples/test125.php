[expect] kris
[expect] Notice
[expect] Undefined offset (10)

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
class Named_Cart extends Cart {
    var $owner;
  
    function set_owner ($name) {
        $this->owner = $name;
    }
}
?>
<?php
$ncart = new Named_Cart;    // Create a named cart
$ncart->set_owner("kris");  // Name that cart
print $ncart->owner;        // print the cart owners name
$ncart->add_item("10", 1);  // (inherited functionality from cart)
?>
