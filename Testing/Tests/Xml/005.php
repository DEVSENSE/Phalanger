[expect php]
[file]
<?php 
require('Phalanger.inc');

$sxe = simplexml_load_string(<<<EOF
<?xml version='1.0'?>
<sxe id="elem1">
 Plain text.
 <elem1 attr1='first'>
  <!-- comment -->
  <elem2>
   Here we have some text data.
   <elem3>
    And here some more.
    <elem4>
     Wow once again.
    </elem4>
   </elem3>
  </elem2>
 </elem1>
</sxe>
EOF
);

__var_dump(trim($sxe->elem1->elem2));
__var_dump(trim($sxe->elem1->elem2->elem3));
__var_dump(trim($sxe->elem1->elem2->elem3->elem4));
?>
