[expect php]
[file]
<?php
require('Phalanger.inc');

$dom = new DOMDocument();
$dom->loadXML("<root><foo>foobar</foo><foo>foobar#2</foo></root>");

$nodelist = $dom->getElementsByTagName("foo");

__var_dump($nodelist->length);
__var_dump(isset($nodelist->length));
__var_dump(isset($nodelist->foo));
?>
