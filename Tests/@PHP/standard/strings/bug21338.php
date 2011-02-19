[expect php]
[file]
<?
include('Phalanger.inc');
	__var_dump(html_entity_decode(NULL));
	__var_dump(html_entity_decode(""));
?>