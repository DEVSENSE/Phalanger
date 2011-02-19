[expect php]
[file]
<?

$handle = popen("dir C:\\ /s", 'r');
echo fgets($handle);
echo fgets($handle);
echo "exit=",pclose($handle);

?>