[expect php]
[file]
<?php

$dom = new domdocument;
$dom->load(dirname(__FILE__)."/book.xml");
$rootNode = $dom->documentElement;
print "--- Catch exception with try/catch\n";
try {
    $rootNode->appendChild($rootNode);
} catch (domexception $e) {
    echo $e->getCode();
}
//print "--- Don't catch exception with try/catch\n";
//$rootNode->appendChild($rootNode);
?>
