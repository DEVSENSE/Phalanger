[expect php]
[file]
<?php
require('Phalanger.inc');
echo "Test 1: Transform To XML String";
include("prepare.inc");
$proc->importStylesheet($xsl);
print "\n";
print __xml_norm($proc->transformToXml($dom));
print "\n";
?>
