[expect php]
[file]
<?
setlocale(LC_COLLATE,"cs-CZ");
$sorts = array("sort","ksort","asort","rsort");
$types = array(SORT_REGULAR => "regular",SORT_NUMERIC => "numeric",SORT_STRING => "string",SORT_LOCALE_STRING => "locale");
$array = array("x" => 8,"z" => 1,"2b" => 1,"x10","10a" => 0,"10x","20x","x2","0x10","ach0","add1");

for ($i=0;$i<count($sorts);$i++)
{
  foreach($types as $type => $type_name)
  {
    echo "\n{$sorts[$i]} $type_name:\n";
    $x = $array;
    $sorts[$i]($x,$type);
    print_r($x);
  }  
}

echo "\nnatsort:\n";
$x = $array;
natsort($x);
print_r($x);
?>