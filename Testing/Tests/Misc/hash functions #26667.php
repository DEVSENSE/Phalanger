[expect php]
[file]
<?php

function test_hash($algo, $init)
{
	echo "\n$algo, incremental: ";
	$h = hash_init($algo);
	for($i=0;$i<10;++$i) hash_update($h, '' . $init*2 + $i*17);
	echo '(copying state) ';
	$h2 = hash_copy($h);
	for($i=0;$i<10;++$i) hash_update($h, '' . $init*2 + $i*19);
	var_dump(hash_final($h));
	
	echo "\n$algo, from copied state: ";
	var_dump(hash_final($h2));
	
	echo "\n$algo, HMAC, incremental: ";
	$h = hash_init($algo, HASH_HMAC, 'HMAC key. It can be very long, but in this case it will be rehashed to fit the block size of the hashing algorithm...'.$init*147);
	for($i=0;$i<10;++$i) hash_update($h, '' . $init*4 + $i*7);
	//echo '(copying state) ';
	//$h2 = hash_copy($h);// causes PHP crashes sometimes, reported as PHP Bug #52240
	for($i=0;$i<10;++$i) hash_update($h, '' . $init*3 + $i*11);
	var_dump(hash_final($h));
	
	//echo "\n$algo, HMAC, from copied state: ";
	//var_dump(hash_final($h2));// BUG IN PHP, HMAC key is not copied, but only referenced ... hash_final on $h clears the HMAC key in $h2 too...  reported as PHP Bug #52240
		
	echo "\n$algo, at once, short data: ";
	var_dump(hash($algo, 'some string to be hashed ... ' . $init * 123 . ' ...'));
	
	echo "\n$algo, at once, HMAC: ";
	var_dump(hash_hmac($algo, 'some string to be hashed ... ' . $init * 123 . ' ...', 'HMAC key. It can be very long, but in this case it will be rehashed to fit the block size of the hashing algorithm.'));
}

// fixed // http://bugs.php.net/bug.php?id=52240 // PHP Bug, see for future updates of this test

test_hash('adler32', 12345678);
test_hash('crc32', 2345678);
test_hash('crc32b', 345678);
test_hash('md2', 45678);
test_hash('md4', 5678);
test_hash('md5', 678);
test_hash('sha1', 111222);
test_hash('sha256', 64983042165);
// add more tests as other hashing algorithms will be implemented

?>