[expect php]
[file]
<?
include('Phalanger.inc');
	$a = "boo";
	$z = sprintf("%580.58s\n", $a);
	__var_dump($z);
?>