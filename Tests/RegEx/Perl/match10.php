[expect exact]
array
(
  [0] => 1231
  [1] => 1
  [2] => 23
  [3] => 1
)

[file]
<?php
if (preg_match("/([0-9])(.*?)(\\1)/", "01231234", $match))
{
   print_r($match);
}
else
   echo "none";
?>

