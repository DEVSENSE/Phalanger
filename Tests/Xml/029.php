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

$people = simplexml_load_string($xml);

foreach($people as $person)
{
	__var_dump((string)$person['name']);
	__var_dump(count($people));
	__var_dump(count($person));
}

?>
