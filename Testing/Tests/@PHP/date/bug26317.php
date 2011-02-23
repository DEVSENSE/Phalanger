[expect php]
[file]
<?
if (!@putenv("TZ=GMT0") || getenv("TZ") != 'GMT0') {
	die("skip unable to change TZ enviroment variable\n");
}
    putenv("TZ=GMT0");
	echo date("Y-m-d H:i:s\n", strtotime("2003-11-19 16:20:42 Z"));
	echo date("Y-m-d H:i:s\n", strtotime("2003-11-19 09:20:42 T"));
	echo date("Y-m-d H:i:s\n", strtotime("2003-11-19 19:20:42 C"));
?>
