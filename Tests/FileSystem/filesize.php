[expect php]
[file]

<?php

$filename = __FILE__;
echo $filename . ': ' . filesize($filename) . ' bytes';

?> 