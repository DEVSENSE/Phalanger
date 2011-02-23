[expect php]
[file]
<?php

// Precision depends on your precision directive
echo round(sin(deg2rad(60)), 6);  //  0.866025403 ...
echo "\n";
echo round(sin(60), 6);           // -0.304810621 ...

?> 