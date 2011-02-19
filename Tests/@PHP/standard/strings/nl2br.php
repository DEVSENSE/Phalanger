[expect php]
[file]
<?
include('Phalanger.inc');
	__var_dump(nl2br("test"));
	__var_dump(nl2br(""));
	__var_dump(nl2br(NULL));
	__var_dump(nl2br("\r\n"));
	__var_dump(nl2br("\n"));
	__var_dump(nl2br("\r"));
	__var_dump(nl2br("\n\r"));
	
	__var_dump(nl2br("\n\r\r\n\r\r\r\r"));
	__var_dump(nl2br("\n\r\n\n\r\n\r\r\n\r\n"));
	__var_dump(nl2br("\n\r\n\n\n\n\r\r\r\r\n\r"));
	
?>