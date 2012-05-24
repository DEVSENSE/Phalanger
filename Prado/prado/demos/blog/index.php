<?php

$basePath=dirname(__FILE__);
$frameworkPath=$basePath.'/../../framework/prado.php';
$assetsPath=$basePath.'/assets';
$runtimePath=$basePath.'/protected/runtime';
$dataPath=$basePath.'/protected/Data';

if(!is_writable($assetsPath))
	die("Please make sure that the directory $assetsPath is writable by Web server process.");
if(!is_writable($runtimePath))
	die("Please make sure that the directory $runtimePath is writable by Web server process.");
if(!is_writable($dataPath))
	die("Please make sure that the directory $dataPath is writable by Web server process.");
if(!extension_loaded("sqlite"))
	die("SQLite PHP extension is required.");

require_once($frameworkPath);

$application=new TApplication;
$application->run();

?>