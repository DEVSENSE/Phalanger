[expect php]
[file]
<?
include('Phalanger.inc');
	__var_dump(number_format(0.0001, 1));
	__var_dump(number_format(0.0001, 0));
?>