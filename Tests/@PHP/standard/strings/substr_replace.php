[expect php]
[file]
<?php
  require('Phalanger.inc');
$str = "try this";
$repl = "bala ";
$start = 2;
echo "\n";


echo "substr_replace('$str', '$repl', $start)\n";
__var_dump(substr_replace($str, $repl, $start));
echo "\n";

$len = 3;
echo "substr_replace('$str', '$repl', $start, $len)\n";
__var_dump(substr_replace($str, $repl, $start, $len));
echo "\n";

$len = 0;
echo "substr_replace('$str', '$repl', $start, $len)\n";
__var_dump(substr_replace($str, $repl, $start, $len));
echo "\n";

$len = -2;
echo "substr_replace('$str', '$repl', $start, $len)\n";
__var_dump(substr_replace($str, $repl, $start, $len));
echo "\n";
echo "\n";
echo "\n";


$str = "try this";
$repl = array("bala ");
$start = 4;
$len = 3;
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).", ".__var_dump($start,1)."")."\n";
__var_dump(substr_replace($str, $repl, $start))."\n";
echo "\n";
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).", ".__var_dump($start,1)."")."\n";
__var_dump(substr_replace($str, $repl, $start, $len))."\n";
echo "\n";

echo "\n";
echo "\n";
echo "\n";



$str = array("ala portokala");
$repl = array("bala ");
$start = array(4);
$len = array(3);
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).", ".__var_dump($start,1)."")."\n";
__var_dump(substr_replace($str, $repl, $start))."\n";
echo "\n";

$len = array(3);
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).", ".__var_dump($start,1).", ".__var_dump($len,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start, $len))."\n";
echo "\n";

$len = array(0);
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).", ".__var_dump($start,1).", ".__var_dump($len,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start, $len))."\n";
echo "\n";

$len = array(-2);
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).", ".__var_dump($start,1).", ".__var_dump($len,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start, $len))."\n";
echo "\n";
echo "\n";
echo "\n";




$str = array("ala portokala");
$repl = "bala ";
$start = 4;
$len = 3;
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).",".__var_dump($start,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start))."\n";
echo "\n";
echo "\n";



$str = array("ala portokala", "try this");
$repl = array("bala ");
$start = 4;
$len = 3;
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).",".__var_dump($start,1).", ".__var_dump($len,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start, $len))."\n";
echo "\n";
echo "\n";


$str = array("ala portokala", "try this");
$repl = "bala ";
$start = 4;
$len = 3;
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).",".__var_dump($start,1).", ".__var_dump($len,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start, $len))."\n";
echo "\n";


$str = array("ala portokala", "try this");
$repl = array("bala ");
$start = 4;
$len = 0;
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).",".__var_dump($start,1).", ".__var_dump($len,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start, $len))."\n";
echo "\n";
echo "\n";


$str = array("ala portokala", "try this");
$repl = "bala ";
$start = 4;
$len = 0;
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).",".__var_dump($start,1).", ".__var_dump($len,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start, $len))."\n";
echo "\n";

$str = array("ala portokala", "try this");
$repl = array("bala ");
$start = 4;
$len = -2;
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).",".__var_dump($start,1).", ".__var_dump($len,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start, $len))."\n";
echo "\n";
echo "\n";


$str = array("ala portokala", "try this");
$repl = "bala ";
$start = 4;
$len = -2;
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).",".__var_dump($start,1).", ".__var_dump($len,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start, $len))."\n";
echo "\n";
echo "\n";
echo "\n";
echo "\n";
echo "\n";
echo "\n";




$str = array("ala portokala");
$repl = "bala ";
$start = array(4);
$len = 3;
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).",".__var_dump($start,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start))."\n";
echo "\n";
echo "\n";



$str = array("ala portokala", "try this");
$repl = array("bala ");
$start = array(4);
$len = 3;
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).",".__var_dump($start,1).", ".__var_dump($len,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start, $len))."\n";
echo "\n";
echo "\n";


$str = array("ala portokala", "try this");
$repl = "bala ";
$start = array(4);
$len = 3;
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).",".__var_dump($start,1).", ".__var_dump($len,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start, $len))."\n";
echo "\n";


$str = array("ala portokala", "try this");
$repl = array("bala ");
$start = array(4);
$len = 0;
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).",".__var_dump($start,1).", ".__var_dump($len,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start, $len))."\n";
echo "\n";
echo "\n";


$str = array("ala portokala", "try this");
$repl = "bala ";
$start = array(4);
$len = 0;
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).",".__var_dump($start,1).", ".__var_dump($len,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start, $len))."\n";
echo "\n";

$str = array("ala portokala", "try this");
$repl = array("bala ");
$start = array(4);
$len = -2;
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).",".__var_dump($start,1).", ".__var_dump($len,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start, $len))."\n";
echo "\n";
echo "\n";


$str = array("ala portokala", "try this");
$repl = "bala ";
$start = array(4);
$len = -2;
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).",".__var_dump($start,1).", ".__var_dump($len,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start, $len))."\n";
echo "\n";


echo "\n";
echo "\n";
echo "\n";
echo "\n";
echo "\n";
echo "\n";


$str = array("ala portokala");
$repl = "bala ";
$start = array(4,2);
$len = 3;
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).",".__var_dump($start,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start))."\n";
echo "\n";
echo "\n";



$str = array("ala portokala", "try this");
$repl = array("bala ");
$start = array(4,2);
$len = 3;
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).",".__var_dump($start,1).", ".__var_dump($len,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start, $len))."\n";
echo "\n";
echo "\n";


$str = array("ala portokala", "try this");
$repl = "bala ";
$start = array(4,2);
$len = 3;
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).",".__var_dump($start,1).", ".__var_dump($len,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start, $len))."\n";
echo "\n";


$str = array("ala portokala", "try this");
$repl = array("bala ");
$start = array(4,2);
$len = 0;
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).",".__var_dump($start,1).", ".__var_dump($len,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start, $len))."\n";
echo "\n";
echo "\n";


$str = array("ala portokala", "try this");
$repl = "bala ";
$start = array(4,2);
$len = 0;
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).",".__var_dump($start,1).", ".__var_dump($len,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start, $len))."\n";
echo "\n";

$str = array("ala portokala", "try this");
$repl = array("bala ");
$start = array(4,2);
$len = -2;
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).",".__var_dump($start,1).", ".__var_dump($len,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start, $len))."\n";
echo "\n";
echo "\n";


$str = array("ala portokala", "try this");
$repl = "bala ";
$start = array(4,2);
$len = -2;
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).",".__var_dump($start,1).", ".__var_dump($len,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start, $len))."\n";
echo "\n";



echo "\n";
echo "\n";
echo "\n";
echo "\n";
echo "\n";
echo "\n";


$str = array("ala portokala");
$repl = "bala ";
$start = array(4,2);
$len = array(3);
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).",".__var_dump($start,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start))."\n";
echo "\n";
echo "\n";



$str = array("ala portokala", "try this");
$repl = array("bala ");
$start = array(4,2);
$len = array(3);
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).",".__var_dump($start,1).", ".__var_dump($len,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start, $len))."\n";
echo "\n";
echo "\n";


$str = array("ala portokala", "try this");
$repl = "bala ";
$start = array(4,2);
$len = array(3);
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).",".__var_dump($start,1).", ".__var_dump($len,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start, $len))."\n";
echo "\n";


$str = array("ala portokala", "try this");
$repl = array("bala ");
$start = array(4,2);
$len = array(0);
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).",".__var_dump($start,1).", ".__var_dump($len,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start, $len))."\n";
echo "\n";
echo "\n";


$str = array("ala portokala", "try this");
$repl = "bala ";
$start = array(4,2);
$len = array(0);
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).",".__var_dump($start,1).", ".__var_dump($len,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start, $len))."\n";
echo "\n";

$str = array("ala portokala", "try this");
$repl = array("bala ");
$start = array(4,2);
$len = array(-2);
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).",".__var_dump($start,1).", ".__var_dump($len,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start, $len))."\n";
echo "\n";
echo "\n";


$str = array("ala portokala", "try this");
$repl = "bala ";
$start = array(4,2);
$len = array(-2);
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).",".__var_dump($start,1).", ".__var_dump($len,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start, $len))."\n";
echo "\n";


echo "\n";
echo "\n";
echo "\n";
echo "\n";
echo "\n";
echo "\n";


$str = array("ala portokala");
$repl = "bala ";
$start = array(4,2);
$len = array(3,2);
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).",".__var_dump($start,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start))."\n";
echo "\n";
echo "\n";



$str = array("ala portokala", "try this");
$repl = array("bala ");
$start = array(4,2);
$len = array(3,2);
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).",".__var_dump($start,1).", ".__var_dump($len,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start, $len))."\n";
echo "\n";
echo "\n";


$str = array("ala portokala", "try this");
$repl = "bala ";
$start = array(4,2);
$len = array(3,2);
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).",".__var_dump($start,1).", ".__var_dump($len,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start, $len))."\n";
echo "\n";


$str = array("ala portokala", "try this");
$repl = array("bala ");
$start = array(4,2);
$len = array(0,0);
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).",".__var_dump($start,1).", ".__var_dump($len,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start, $len))."\n";
echo "\n";
echo "\n";


$str = array("ala portokala", "try this");
$repl = "bala ";
$start = array(4,2);
$len = array(0,0);
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).",".__var_dump($start,1).", ".__var_dump($len,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start, $len))."\n";
echo "\n";

$str = array("ala portokala", "try this");
$repl = array("bala ");
$start = array(4,2);
$len = array(-2,-3);
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).",".__var_dump($start,1).", ".__var_dump($len,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start, $len))."\n";
echo "\n";
echo "\n";


$str = array("ala portokala", "try this");
$repl = "bala ";
$start = array(4,2);
$len = array(-2,-3);
echo str_replace("\n","","substr_replace(".__var_dump($str,1).", ".__var_dump($repl,1).",".__var_dump($start,1).", ".__var_dump($len,1).")")."\n";
__var_dump(substr_replace($str, $repl, $start, $len))."\n";
echo "\n";

?>

