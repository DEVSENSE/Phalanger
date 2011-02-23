[expect php]
[file]
<?php
require('Phalanger.inc');
__var_dump(html_entity_decode("&amp;lt;", ENT_COMPAT, 'ISO-8859-1'));
__var_dump(html_entity_decode("&amp;#38;", ENT_COMPAT, 'ISO-8859-1'));
__var_dump(html_entity_decode("&amp;#38;lt;", ENT_COMPAT, 'ISO-8859-1'));
?>
