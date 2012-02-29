[Expect php]
[file]
<?php
function test() {
    
$pattern = '/((?P<embed>.*))/';

if (preg_match($pattern,"hello", $res))
	echo $res["embed"];

}

test();

	
?>