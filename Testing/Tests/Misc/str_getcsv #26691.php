[expect php]
[file]
<?php

function test($fname, $enclosure = '"')
{
	echo "\n>> {$fname}, enclosure {$enclosure}\n\n";
	$csvData = file_get_contents(dirname(__FILE__)."/".$fname);
	$csvLine = str_getcsv($csvData, ",",$enclosure);
		foreach ($csvLine as $index => $value)
			echo "[{$index}] = \"{$value}\"\n";
			
}

test('test1.csv');
test('test2.csv');
test('test3.csv');
test('test4.csv');
test('test4.csv',"'");

var_dump(str_getcsv(null));
var_dump(str_getcsv(""));
var_dump(str_getcsv(","));

?>