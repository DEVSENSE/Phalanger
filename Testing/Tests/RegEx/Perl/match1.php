[expect exact]
array [empty]
array
(
  [0] => array
  (
    [0] => def
    [1] => 3
  )
)

[file]
<?php
$subject = "abcdef";
$pattern = '/^def/';
preg_match($pattern, $subject, $matches, PREG_OFFSET_CAPTURE, 3);
print_r($matches);

$pattern = '<def>';
preg_match($pattern, $subject, $matches, PREG_OFFSET_CAPTURE, 3);
print_r($matches);
?> 