[expect php]

[file]
<?

$c  = "a";
$$c = 3;
echo $a;

$c  = "a";
$a = 5;
echo " ".$$c;

?>