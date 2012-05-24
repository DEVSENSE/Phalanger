<?php

$frameworkPath='../../framework/prado.php';

/** The directory checks may be removed if performance is required **/
$basePath=dirname(__FILE__);
$assetsPath=$basePath."/assets";
$runtimePath=$basePath."/protected/runtime";

$sqliteDbDir = $basePath."/protected/App_Code";
$sqliteDb = $sqliteDbDir."/chat.db";

if(!is_file($frameworkPath))
	die("Unable to find prado framework path $frameworkPath.");
if(!is_writable($assetsPath))
	die("Please make sure that the directory $assetsPath is writable by Web server process.");
if(!is_writable($runtimePath))
	die("Please make sure that the directory $runtimePath is writable by Web server process.");
if(!is_writable($sqliteDbDir))
	die("Please make sure that the directory $sqliteDbDir is writable by Web server process.");
if(!is_writable($sqliteDb))
	die("Please make sure that the sqlite file $sqliteDb is writable by Web server process.");

require_once($frameworkPath);

$application=new TApplication;
$application->run();

?>