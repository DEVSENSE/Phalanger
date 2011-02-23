[expect php]
[file]
<?php
require('Phalanger.inc');

echo "Test 7: Transform To Uri";
include("prepare.inc");
$proc->importStylesheet($xsl);
print "\n";
$doc = $proc->transformToUri($dom, "file://".dirname(__FILE__)."/out.xml");
print __xml_norm(file_get_contents(dirname(__FILE__)."/out.xml"));
unlink(dirname(__FILE__)."/out.xml");
print "\n";
?>
