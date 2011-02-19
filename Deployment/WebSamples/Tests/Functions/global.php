[expect]Set in global code

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