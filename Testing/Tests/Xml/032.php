[expect php]
[file]
<?php 
require('Phalanger.inc');

$xml =<<<EOF
<people>
  <person name="Joe"/>
  <person name="John">
    <children>
      <person name="Joe"/>
    </children>
  </person>
  <person name="Jane"/>
</people>
EOF;

$xml1 =<<<EOF
<people>
  <person name="John">
    <children>
      <person name="Joe"/>
    </children>
  </person>
  <person name="Jane"/>
</people>
EOF;


$people = simplexml_load_string($xml);
$people1 = simplexml_load_string($xml);
$people2 = simplexml_load_string($xml1);

__var_dump($people1 == $people);
__var_dump($people2 == $people);
__var_dump($people2 == $people1);

?>
