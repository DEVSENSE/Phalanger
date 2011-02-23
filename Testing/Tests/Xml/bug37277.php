[expect php]
[file]
<?php
function __xml_norm($str)
{
	$str = str_replace(" /", "/", $str);
	if ($str[strlen($str) - 1] != "\n") return $str . "\n";
	else return $str;
}

$dom1 = new DomDocument('1.0', 'UTF-8');

$xml = '<foo />';
$dom1->loadXml($xml);

$node = clone $dom1->documentElement;

$dom2 = new DomDocument('1.0', 'UTF-8');
$dom2->appendChild($dom2->importNode($node->cloneNode(true), TRUE));

$dom2->formatOutput = true;
echo __xml_norm($dom2->saveXML());

?>
