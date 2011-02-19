[expect php]
[file]
<?

echo "Testing ord() & chr()...";
for($i=0; $i<256; $i++) echo !ord(chr($i)) == $i;
echo " done";
?>
