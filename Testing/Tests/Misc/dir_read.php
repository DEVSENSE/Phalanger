[expect php]
[file]
<?

function foo()
{
	$dir = dir(".");
	while ( $file = $dir->read() !== FALSE) {	// test for [CastToFalse] in Directory::read() method
		echo "$file\n";
	}
	$dir->close();
}

foo();

?>