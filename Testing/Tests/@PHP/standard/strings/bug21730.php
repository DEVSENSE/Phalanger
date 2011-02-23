[expect php]
[file]
<?
include('Phalanger.inc');
$foo = "ABC = DEF";
$fmt = "%s = %s %n";
$res_a = array();

/* $res_a[2] is supposed to be a integer value that
 * represents the number of characters consumed so far
 */
sscanf($foo, $fmt, $res_a[0], $res_a[1], $res_a[2]);

$res_b = sscanf($foo, $fmt);

__var_dump($res_a);
__var_dump($res_b);
?>