[expect] 1

[file]
<?
if ($a == $b)
{
class X
{
static function f($a)
{ echo 1; }
}
}

X::f(1);

?>
