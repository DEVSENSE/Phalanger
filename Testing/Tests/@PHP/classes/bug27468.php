[expect php]
[file]
<?
  error_reporting(0);

class foo {
	function __destruct() {
		foreach ($this->x as $x);
	}
}
new foo();
echo 'OK';
?>