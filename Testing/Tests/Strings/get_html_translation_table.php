[expect php]
[file]

<?php

//printme(get_html_translation_table(HTML_ENTITIES));

echo "<hr>";
printme(get_html_translation_table(HTML_SPECIALCHARS));
echo "<hr>";

$trans = get_html_translation_table(HTML_ENTITIES);
$str = "Hallo & <Frau> & Krämer";
echo $encoded = strtr($str, $trans);


function printme($a)
{
asort($a);
foreach ($a as $k => $v) echo "[$k] => $v\n";
}

?>