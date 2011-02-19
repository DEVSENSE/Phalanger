[expect php]
[file]
<?php 
require('Phalanger.inc');

$xml =<<<EOF
<?xml version="1.0" encoding="ISO-8859-1" ?>
<foo>bar<baz/>bar</foo>
EOF;

$sxe = simplexml_load_string($xml);

__var_dump((string)$sxe);

?>
