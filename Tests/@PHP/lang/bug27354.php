[expect php]
[file]
<?
  include('Phalanger.inc');
	__var_dump(-2147483647 % -1);
	__var_dump(-2147483649 % -1);
	__var_dump(-2147483648 % -1);
	__var_dump(-2147483648 % -2);
?>
