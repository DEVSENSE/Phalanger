[expect php]
[file]
<?

include "Phalanger.inc";

error_reporting(0);

$a = array("a" => 10);

__var_dump(ereg("([A-Z]*) ([A-Z]*) ([A-Z]*","ADSD  ADASD SD",$a));
__var_dump($a);

__var_dump(ereg("([A-Z]*) ([A-Z]*) ([A-Z]*)","ADSD  ADASD SD",$a));
__var_dump($a);

?>