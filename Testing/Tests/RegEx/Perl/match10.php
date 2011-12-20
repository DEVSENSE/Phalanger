[expect php]


[file]
<?php
if (preg_match("/([0-9])(.*?)(\\1)/", "01231234", $match))
{
   print_r($match);
}
else
   echo "none";
?>

