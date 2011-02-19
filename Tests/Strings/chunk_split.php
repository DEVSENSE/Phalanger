[comment]
[expect php]
[file]

<?php
$data = @file_get_contents("http://www.google.com/");
if ($data === false) die('NO NETWORK CONNECTION!');

// format $data using RFC 2045 semantics
$new_string = chunk_split(base64_encode($data));
?> 