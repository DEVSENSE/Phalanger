[expect php]
[file]
<?
include('Phalanger.inc');

class test {
	static public $ar = array();
}

__var_dump(test::$ar);

test::$ar[] = 1;

__var_dump(test::$ar);

echo "Done\n";
?>