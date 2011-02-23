[expect php]

[file]
<?
function f()
{
	global $z;

	echo $z;
}

$z = "Set in global code";
f();

?>