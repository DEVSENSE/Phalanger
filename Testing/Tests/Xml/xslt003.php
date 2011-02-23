[expect php]
[file]
<?php
require('Phalanger.inc');

echo "Test 3: Using Parameters";
include("prepare.inc");
$proc->importStylesheet($xsl);
$proc->setParameter( "", "foo","hello world");
print "\n";
print __xml_norm($proc->transformToXml($dom));
print "\n";
?>
