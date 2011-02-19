[expect php]
[file]
<?php
  include('Phalanger.inc');

echo "==Mixed==\n";
$a = array(-1=>'a', '-2'=>'b', 3=>'c', '4'=>'d', 5=>'e', '6001'=>'f', '07'=>'g');

foreach($a as $k => $v) {
	__var_dump($k);
	__var_dump($v);
}

echo "==Normal==\n";
$b = array();
$b[] = 'a';

foreach($b as $k => $v) {
	__var_dump($k);
	__var_dump($v);
}

echo "==Negative==\n";
$c = array('-2' => 'a');

foreach($c as $k => $v) {
	__var_dump($k);
	__var_dump($v);
}

echo "==Done==\n";
?>