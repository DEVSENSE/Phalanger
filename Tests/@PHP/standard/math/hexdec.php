[expect php]
[file]
<?

echo(hexdec("012345")), "\n";
echo(hexdec("12345")), "\n";
echo(hexdec("q12345")), "\n";
echo(hexdec("12345+?!")), "\n";
echo(hexdec("12345q")), "\n";
echo((float)hexdec("1234500001")), "\n";
echo((float)hexdec("17fffffff")), "\n";

?>