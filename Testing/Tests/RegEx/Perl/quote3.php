[expect php]

[file]
<?php

$phrase = 'a test'; // note the space
$textbody = 'this is a test';

// Does not match:
if (preg_match('/' . preg_quote($phrase) . '$/x', $textbody))
	echo "YES";
else
	echo "NO";

function preg_quote_white($a) {
     $a = preg_quote($a);
     $a = str_replace(' ', '\ ', $a);
     return $a;
}

// Does match:
if (preg_match('/' . preg_quote_white($phrase) . '$/x', $textbody))
	echo "YES";
else
	echo "NO";


?>