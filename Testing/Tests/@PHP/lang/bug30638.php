[expect php]
[file]
<?
 # try to activate a german locale
if (setlocale(LC_NUMERIC, "de_DE", "de", "german", "ge") === FALSE) {
	die("skip");
}
# activate the german locale
setlocale(LC_NUMERIC, "de_DE", "de", "german", "ge");

$lc = localeconv();
printf("decimal_point: %s\n", $lc['decimal_point']);
printf("thousands_sep: %s\n", $lc['thousands_sep']);
?>