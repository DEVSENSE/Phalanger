[expect php]
[file]
<?php 
require('Phalanger.inc');

$sxe = simplexml_load_string(<<<EOF
<?xml version='1.0'?>
<sxe id="elem1">
 Plain text.
 <elem1 attr1='first'>
  Bla bla 1.
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
 <elem11 attr2='second'>
  Bla bla 2.
 </elem11>
</sxe>
EOF
);
foreach($sxe->children() as $name=>$val) {
	__var_dump($name);
	__var_dump(get_class($val));
	__var_dump(trim($val));
}
?>
