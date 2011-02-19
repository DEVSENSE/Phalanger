[expect php]
[file]

<?php

echo realpath("C:") . "\n";
echo realpath("C:\\") . "\n";
echo realpath("C:/") . "\n";
echo strtoupper(realpath('C:\windows\explorer.exe')) . "\n";
echo realpath('someotherfile') . "\n";
echo realpath(".") . "\n";
echo realpath(".\\") . "\n";
echo realpath("./") . "\n";
echo realpath("http://www.google.com/") . "\n";

?>
