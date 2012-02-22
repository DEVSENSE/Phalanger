[clr]
[Expect exact]
hello worldSystem\Object Object
(
)

[file]
<?php
function test() {
    
    $x = new System\Collections\Hashtable();
	$y = new System\object();
	
    $x->Add("obj", $y);
    $x->Add("message", "hello world");
	
	foreach ($x as $item)
	{
		print_r($item);
	}
}

test();

	
?>