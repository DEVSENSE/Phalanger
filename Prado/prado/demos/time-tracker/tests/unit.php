<?php

include_once '../../../tests/test_tools/unit_tests.php';

$app_directory = "../protected";
$test_cases = dirname(__FILE__)."/unit";

$tester = new PradoUnitTester($test_cases, $app_directory);
$tester->run(new HtmlReporter());

?>