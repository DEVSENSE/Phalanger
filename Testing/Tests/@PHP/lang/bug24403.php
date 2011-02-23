[expect php]
[file]
<?php
class a
{
	public $a = array();

	function a()
	{
		echo preg_replace(
				'!\{\s*([a-z0-9_]+)\s*\}!sie',
				"(in_array('\\1',\$this->a) ? '\'.\$p[\'\\1\'].\'' :
'\'.\$r[\'\\1\'].\'')",
				"{a} b {c}");
	}
}
new a();
?>