[expect php]
[file]

<?php
include('fs.inc');

// Get a file into an array.  In this example we'll go through HTTP to get 
// the HTML source of a URL.
d( file('oneline.txt') );
d( file('twoline.txt', FILE_IGNORE_NEW_LINES) );
d( file('twoline.txt', FILE_SKIP_EMPTY_LINES) );
d( file('twoline.txt', FILE_SKIP_EMPTY_LINES | FILE_IGNORE_NEW_LINES) );
d( file('noline.txt') );

// Get a file into an array.  In this example we'll go through HTTP to get 
// the HTML source of a URL.
$lines = file('test.txt');

// Loop through our array, show HTML source as HTML source; and line numbers too.
//foreach ($lines as $line_num => $line) {
//    echo "Line #<b>{$line_num}</b> : " . htmlspecialchars($line) . "<br />\n";
//}
$line_num = count($lines) - 1;
$line = $lines[$line_num];
    echo "Line #<b>{$line_num}</b> : " . htmlspecialchars($line) . "<br />\n";

// Another example, let's get a web page into a string.  See also file_get_contents().
$html = implode('', file('oneline.txt'));

echo '['.str_replace("\n", "{\\n}\n", htmlspecialchars($html)).']';

?> 