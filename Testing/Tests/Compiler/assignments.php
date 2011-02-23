[expect php]
[file]
<?
error_reporting(0);

function h(&$x) { $x = 10; }
eval('function unknown($x) {  }');

$a = 1;
$b = 3;
$c = 5;
$d = 6;
$e = 7;
echo "---1";
$a -= $b *= $c /= $d %= $e += 1;
echo "---1";

var_dump($a,$b,$c,$d,$e);

echo "---2";
$a = $b = $c += $d = $e;
echo "---2";

var_dump($a,$b,$c,$d,$e);

unset($a,$b,$c,$d,$e);

echo "---3";
$a[$c = ${$e = $f = $g}]->h[$z->l]->u->v = $b[$c = ${$e = $f = $g}]->h[$z->l]->u->v;
echo "---3";

echo "---4";
unknown($a = $b = $c);
echo "---4";

echo "---5";
$a[][][] = $b[][][] = $c[1][2][3] = array(1,2,3);
echo "---5";

var_dump($a,$b,$c);

$z = array(1,2,3);

echo "---6";
$u = $v = $w =& $z;
echo "---6";

$u[] = "u";
$v[] = "v";
$w[] = "w";
$z[] = "z";
var_dump($u,$v,$w,$z);

echo "---7";
h($aa[][][][] =& $bb[][][][]);
echo "---7";

var_dump($aa,$bb);

echo "---8";
unknown($a =& $b,$x);
echo "---8";

$a[$c =& ${$e = $f =& $g}]->h[$z->l]->u->v =& $b[$c =& ${$e = $f = $g}]->h[$z->l]->u->v;

$c =& ${$e =& $g};

$x =& $a[][][];

h($a[][][]);

h($a);
h($a[]);

?>