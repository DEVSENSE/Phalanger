[expect php]

[file]
<?php
$string = "The quick brown fox jumped over the lazy dog.";

$patterns[0] = "/quick/";
$patterns[1] = "/brown/";
$patterns[2] = "/fox/";

$replacements[2] = "bear";
$replacements[1] = "black";
$replacements[0] = "slow";

echo preg_replace($patterns, $replacements, $string);


ksort($patterns);
ksort($replacements);

echo preg_replace($patterns, $replacements, $string);

?> 