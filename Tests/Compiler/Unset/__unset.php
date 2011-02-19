[expect php]
[file]
<?php
class magicmethod
{
	function __unset($variablename)
	{
		echo "Variable '".$variablename."' not Set and Cannot be UnSet";
	}
}
$a = new magicmethod();
unset($a->name);
?>