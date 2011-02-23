[expect php]

[file]
<?php
function translate($x)
{
$x = reset($x);
return "($x)";
}
echo preg_replace_callback(
'/(?:\s|^);(?:-\)|\))|(?:\s|^)\:(?:\||x|wink\:|twisted\:|smile\:|shock\:|sad\:|roll\:|razz\:|oops\:|o|neutral\:|mrgreen\:|mad\:|lol\:|idea\:|grin\:|evil\:|eek\:|cry\:|cool\:|arrow\:|P|D|\?\?\?\:|\?\:|\?|-\||-x|-o|-P|-D|-\?|-\)|-\(|\)|\(|\!\:)|(?:\s|^)8(?:O|-O|-\)|\))(?:\s|$)/m',
'translate',
'smilies :-) :-( :) :( :lol:');
?>

