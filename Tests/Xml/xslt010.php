[expect php]
[file]
<?php
require('Phalanger.inc');

echo "Test 10: EXSLT Support";

$dom = new domDocument();
  $dom->load(dirname(__FILE__)."/exslt.xsl");
  $proc = new xsltprocessor;
  $xsl = $proc->importStylesheet($dom);
  
  $xml = new DomDocument();
  $xml->load(dirname(__FILE__)."/exslt.xml");
  
  print __xml_norm($proc->transformToXml($xml));
?>
