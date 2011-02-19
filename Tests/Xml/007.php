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

echo "===Property===\n";
__var_dump($sxe->elem1);
echo "===Array===\n";
__var_dump($sxe['id']);
__var_dump($sxe->elem1['attr1']);
echo "===Set===\n";
$sxe['id'] = "Changed1";
__var_dump($sxe['id']);
$sxe->elem1['attr1'] = 12;
__var_dump($sxe->elem1['attr1']);
echo "===Unset===\n";
unset($sxe['id']);
__var_dump($sxe['id']);
unset($sxe->elem1['attr1']);
__var_dump($sxe->elem1['attr1']);
echo "===Misc.===\n";
$a = 4;
__var_dump($a);
$dummy = $sxe->elem1[$a];
__var_dump($a);
?>
