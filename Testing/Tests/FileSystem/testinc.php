[expect php]
[file]

<?php
require('fs.inc');

d(false);
d(null);
d(M_PI);
d(array(1,3,pi(),false,"4" => "YES", 10 => "ten", "10" => 'TEN', 333 => array(3,3,3)));
d("044"+0);

?> 
