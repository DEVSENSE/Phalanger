[expect php]
[file]
<?php
// split the phrase by any number of commas or space characters,
// which include " ", \r, \t, \n and \f
$keywords = preg_split("/[\s,]+/", "hypertext language, programming");

foreach ($keywords as $k)
	echo "$k,";
?> 