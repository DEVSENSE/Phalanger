[expect php]
[file]
<?php
require('Phalanger.inc');

$xml =<<<EOF
<people>
test
  <person name="Joe"/>
  <person name="John">
    <children>
      <person name="Joe"/>
    </children>
  </person>
  <person name="Jane"/>
</people>
EOF;

$foo = simplexml_load_string( "<foo />" );
$people = simplexml_load_string($xml);

__var_dump((bool)$foo);
__var_dump((bool)$people);
__var_dump((int)$foo);
__var_dump((int)$people);
__var_dump((double)$foo);
__var_dump((double)$people);
__var_dump(trim((string)$foo));
__var_dump(trim((string)$people));
//__var_dump((array)$foo);
//__var_dump((array)$people);
//__var_dump((object)$foo);
//__var_dump((object)$people);

?>
