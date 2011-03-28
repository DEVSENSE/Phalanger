[expect php]
[file]
<?php
echo gettype((bool) "");        // bool(false)
var_dump((bool) "");
echo "\n";

echo gettype((bool) 1);         // bool(true)
var_dump((bool) 1);
echo "\n";

echo gettype((bool) -2);        // bool(true)
var_dump((bool) -2);
echo "\n";

echo gettype((bool) "foo");     // bool(true)
var_dump((bool) "foo");
echo "\n";

echo gettype ((bool) 2.3e5);     // bool(true)
var_dump((bool) 2.3e5);
echo "\n";

echo gettype((bool) array(12)); // bool(true)
var_dump((bool) array(12));
echo "\n";

echo gettype((bool) array());   // bool(false)
var_dump((bool) array());
echo "\n";

echo gettype((bool) "false");   // bool(true)
var_dump((bool) "false");
echo "\n";

?>
