[expect php]

[file]
<?php
$a = "hello";
?>


<?php
$$a = "world";
?>


<?php
echo "$a ${$a}";
?>


<?php
echo "$a $hello";
?>
 
