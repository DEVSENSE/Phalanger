[expect php]
[file]
<?php

  $text = "<H2 some='stuff'>hej hola</H2>";
  $matches = null;
  $numMatches = preg_match_all('/<H(?P<level>[1-6])(?P<attrib>.*?'.'>)(?P<header>.*?)<\/H[1-6] *>/i', $text, $matches);
	
	echo "FIRST\n";
	echo $matches['header'][0];

  $text = "<H2 some='stuff'>hej hola</H2>";
  $matches = null;
  $numMatches = preg_match_all('/<H(?P<level>[1-6])(?P<attrib>.*?'.'>)(?P<header>.*?)<\/H[1-6] *>/i', $text, $matches, PREG_SET_ORDER);

	echo "\nSECOND\n";
	echo $matches[0]['header'];

?>