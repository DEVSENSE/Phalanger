[expect exact]
array
(
  [0] => array
  (
    [0] => def
    [1] => 0
  )
)

[file]
<?php
$subject = "abcdef";
$pattern = '/^def/';
preg_match($pattern, substr($subject,3), $matches, PREG_OFFSET_CAPTURE);
print_r($matches);
?> 