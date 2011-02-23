[expect] 1
[expect] 0
[expect] 1
[expect] 1

[file]
<?php

define( "REGEXP_PHONE", "/^(\(|){1}[2-9][0-9]{2}(\)|){1}([\. -]|)[2-9][0-9]{2}([\. -]|)[0-9]{4}$/" ); 

$phone = "(225) 548 8541";
$result = preg_match(REGEXP_PHONE, $phone);
print_r($result);

$phone = "(225) 5f8 8541";
$result = preg_match(REGEXP_PHONE, $phone);
print_r($result);

$phone = "225-548-8541";
$result = preg_match(REGEXP_PHONE, $phone);
print_r($result);

$phone = "(225-548 8541";
$result = preg_match(REGEXP_PHONE, $phone);
print_r($result);

?>