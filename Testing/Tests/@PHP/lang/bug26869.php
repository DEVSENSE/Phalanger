[expect php]
[file]
<?
  include('Phalanger.inc');
	define("A", "1");
	static $a=array(A => 1);
	__var_dump($a);
	__var_dump(isset($a[A]));
?>