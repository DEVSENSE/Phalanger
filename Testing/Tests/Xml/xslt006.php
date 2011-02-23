[expect php]
[file]
<?php
require('Phalanger.inc');

echo "Test 6: Transform To Doc";
include("prepare.inc");
$proc->importStylesheet($xsl);
print "\n";
$doc = $proc->transformToDoc($dom);
print __xml_norm($doc->saveXML());
print "\n";
?>
