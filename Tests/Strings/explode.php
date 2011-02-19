[expect php]
[file]

<?php

// Example 1
$pizza  = "piece1 piece2 piece3 piece4 piece5 piece6";
$pieces = explode(" ", $pizza);
echo $pieces[0]; // piece1
echo $pieces[1]; // piece2

// Example 2
$data = "foo:*:1023:1000::/home/foo:/bin/sh";
list($user, $pass, $uid, $gid, $gecos, $home, $shell) = explode(":", $data);
echo $user; // foo
echo $pass; // *

// Example 3
$data = "foo[SEP]*[SEP]1023[SEP]1000[SEP][SEP]/home/foo[SEP]/bin/sh";
list($user, $pass, $uid, $gid, $gecos, $home, $shell) = explode("[SEP]", $data);
echo $user; // foo
echo $pass; // *

?> 