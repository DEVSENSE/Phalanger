--TEST--
abusing preg_match()
--FILE--
<?php

{
	var_dump(preg_match('~
		(?P<date> 
		(?P<year>(\d{2})?\d\d) -
		(?P<month>(?:\d\d|[a-zA-Z]{2,3})) -
		(?P<day>[0-3]?\d))
	~x', '2006-05-13', $m));

	var_dump($m);
}

?>
--EXPECT--
int(1)
array(10) {
  [0]=>
  string(10) "2006-05-13"
  ["date"]=>
  string(10) "2006-05-13"
  [1]=>
  string(10) "2006-05-13"
  ["year"]=>
  string(4) "2006"
  [2]=>
  string(4) "2006"
  [3]=>
  string(2) "20"
  ["month"]=>
  string(2) "05"
  [4]=>
  string(2) "05"
  ["day"]=>
  string(2) "13"
  [5]=>
  string(2) "13"
}
