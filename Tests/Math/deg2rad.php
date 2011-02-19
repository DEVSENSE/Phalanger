[expect php]
[file]

<?php

echo round(deg2rad(45), 12); // 0.785398163397
echo "\n";
if(deg2rad(45) === M_PI_4) echo "bool(true)"; // bool(true)

?> 