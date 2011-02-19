[expect php]
[file]
<?php 
require('Phalanger.inc');

$xml =<<<EOF
<?xml version='1.0'?>
<sxe id="elem1">
 <elem1 attr1='first'>
  <elem2>
   <elem3>
    <elem4>
     <?test processing instruction ?>
    </elem4>
   </elem3>
  </elem2>
 </elem1>
</sxe>
EOF;

$sxe = simplexml_load_string($xml);

__var_dump($sxe->xpath("elem1/elem2/elem3/elem4"));
__var_dump(@$sxe->xpath("***"));
?>
