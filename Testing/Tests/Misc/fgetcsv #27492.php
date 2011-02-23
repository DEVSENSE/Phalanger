[expect php]
[file]
<?php

function test($fname, $enclosure = '"')
{
	echo "\n>> {$fname}, enclosure {$enclosure}\n\n";
	$csvFile = fopen(dirname(__FILE__)."/".$fname, 'r');
	if ($csvFile !== false) {
		while (($csvLine = fgetcsv($csvFile, 0, ",",$enclosure)) !== false) {
			if ($csvLine == null)	return;	// invalid stream handle
			echo "\n";
			foreach ($csvLine as $index => $value)
				echo "[{$index}] = \"{$value}\"\n";
			
		}
	}
	fclose($csvFile);
}

test('test1.csv');
test('test2.csv');
test('test3.csv');
test('test4.csv');
test('test4.csv',"'");

?>