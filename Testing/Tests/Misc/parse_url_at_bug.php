[expect php]
[file]
<?php

$uri = "http://deki.example.org/@api/deki/site/settings";

$parsed_uri = parse_url($uri);

var_dump($uri);
var_dump($parsed_uri);

?>