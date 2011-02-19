[expect] prefix1
[expect] prefix2

[file]


<?php

// no prefix
echo uniqid("") . ";";
//echo uniqid() . ";";
echo uniqid("prefix1") . ";";
echo uniqid("prefix2", true) . ";";
echo lcg_value();
?> 