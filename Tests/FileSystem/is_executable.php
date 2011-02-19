[expect php]
[file]

<?php

$file = '/home/vincent/somefile.sh';

// REM if (is_executable($file))  // on Windows from PHP 5.0
if (false)
{
    echo $file.' is executable';
} else {
    echo $file.' is not executable';
}

?> 