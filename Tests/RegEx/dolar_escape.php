[expect php]
[file]
<?php
$vars=array("a"=>"A","EUR" => "$", "0"=>"O");
$data = "It will cost 1,000,000 EUR";
$patterns = array();
$replacements = array();
if(is_array($vars)){
foreach ($vars as $key => $val) {
$patterns[] = "/(" . strtoupper($key) . ")/i";
$replacements[] = str_replace('$', '\$', $val);
}
}
$result = preg_replace($patterns,$replacements,$data);
var_dump($result);
?>