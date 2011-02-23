
[expect php]
[file]
<?
include('Phalanger.inc');

$f=-INF;
__var_dump($f);
__var_dump(serialize($f));
__var_dump(unserialize(serialize($f)));

$f=INF;
__var_dump($f);
__var_dump(serialize($f));
__var_dump(unserialize(serialize($f)));

$f=NAN;
__var_dump($f);
__var_dump(serialize($f));
__var_dump(unserialize(serialize($f)));

?>