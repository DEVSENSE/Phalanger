[expect php]
[file]

<?php

echo round(tan(M_PI_2-1),5); // 1
echo "\n";
echo substr(round(tan(M_PI_2),5), 0, 4) . "...E..."; // 1

?> 