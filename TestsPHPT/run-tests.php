<?php
/*
 Copyright 2010 Jakub Misek
 based on slightly modified run-tests.php from PHP 5.3.4 source code
 */

/*
   +----------------------------------------------------------------------+
   | PHP Version 5, 6                                                     |
   +----------------------------------------------------------------------+
   | Copyright (c) 1997-2010 The PHP Group                                |
   +----------------------------------------------------------------------+
   | This source file is subject to version 3.01 of the PHP license,      |
   | that is bundled with this package in the file LICENSE, and is        |
   | available through the world-wide-web at the following url:           |
   | http://www.php.net/license/3_01.txt                                  |
   | If you did not receive a copy of the PHP license and are unable to   |
   | obtain it through the world-wide-web, please send a note to          |
   | license@php.net so we can mail you a copy immediately.               |
   +----------------------------------------------------------------------+
   | Authors: Ilia Alshanetsky <iliaa@php.net>                            |
   |          Preston L. Bannister <pbannister@php.net>                   |
   |          Marcus Boerger <helly@php.net>                              |
   |          Derick Rethans <derick@php.net>                             |
   |          Sander Roobol <sander@php.net>                              |
   | (based on version by: Stig Bakken <ssb@php.net>)                     |
   | (based on the PHP 3 test framework by Rasmus Lerdorf)                |
   +----------------------------------------------------------------------+
 */

/* $Id: run-tests.php 305310 2010-11-13 10:18:35Z jani $ */

/* Sanity check to ensure that pcre extension needed by this script is available.
 * In the event it is not, print a nice error message indicating that this script will
 * not run without it.
 */

if (!extension_loaded('pcre')) {
	echo <<<NO_PCRE_ERROR

+-----------------------------------------------------------+
|                       ! ERROR !                           |
| The test-suite requires that you have pcre extension      |
| enabled. To enable this extension either compile your PHP |
| with --with-pcre-regex or if you've compiled pcre as a    |
| shared module load it via php.ini.                        |
+-----------------------------------------------------------+

NO_PCRE_ERROR;
exit;
}

if (!function_exists('proc_open')) {
	echo <<<NO_PROC_OPEN_ERROR

+-----------------------------------------------------------+
|                       ! ERROR !                           |
| The test-suite requires that proc_open() is available.    |
| Please check if you disabled it in php.ini.               |
+-----------------------------------------------------------+

NO_PROC_OPEN_ERROR;
exit;
}

// Version constants only available as of 5.2.8
if (!defined("PHP_VERSION_ID")) {
	list($major, $minor, $bug) = explode(".", phpversion(), 3);
	$bug = (int)$bug; // Many distros make up their own versions
	if ($bug < 10) {
		$bug = "0$bug";
	}

	define("PHP_VERSION_ID", "{$major}0{$minor}$bug");
	define("PHP_MAJOR_VERSION", $major);
}

// (unicode) is available from 6.0.0
if (PHP_VERSION_ID < 60000) {
	define('STRING_TYPE', 'string');
} else {
	define('STRING_TYPE', 'unicode');
}
define('FILE_BINARY',0);
// If timezone is not set, use UTC.
if (ini_get('date.timezone') == '') {
	date_default_timezone_set('UTC');
}

// store current directory
$CUR_DIR = getcwd();

// Delete some security related environment variables
putenv('SSH_CLIENT=deleted');
putenv('SSH_AUTH_SOCK=deleted');
putenv('SSH_TTY=deleted');
putenv('SSH_CONNECTION=deleted');

$cwd = getcwd();
set_time_limit(0);

ini_set('pcre.backtrack_limit', PHP_INT_MAX);

$valgrind_version = 0;
$valgrind_header = '';

// delete as much output buffers as possible
while(@ob_end_clean());
if (ob_get_level()) echo "Not all buffers were deleted.\n";

error_reporting(E_ALL);
if (PHP_MAJOR_VERSION < 6) {
	ini_set('magic_quotes_runtime',0); // this would break tests by modifying EXPECT sections
	if (ini_get('safe_mode')) {
		echo <<< SAFE_MODE_WARNING

+-----------------------------------------------------------+
|                       ! WARNING !                         |
| You are running the test-suite with "safe_mode" ENABLED ! |
|                                                           |
| Chances are high that no test will work at all,           |
| depending on how you configured "safe_mode" !             |
+-----------------------------------------------------------+


SAFE_MODE_WARNING;
	}
}

$user_tests = array(__DIR__.'\\tests\\zend', __DIR__.'\\tests\\tests');
$exts_to_test = get_loaded_extensions();
$ini_overwrites = array(
		'output_handler=',
		'open_basedir=',
		'safe_mode=0',
		'disable_functions=',
		'output_buffering=Off',
		'error_reporting=' . (E_ALL | E_STRICT),
		'display_errors=1',
		'display_startup_errors=1',
		'log_errors=0',
		'html_errors=0',
		'track_errors=1',
		'report_memleaks=1',
		'report_zend_debug=0',
		'docref_root=',
		'docref_ext=.html',
		'error_prepend_string=',
		'error_append_string=',
		'auto_prepend_file=',
		'auto_append_file=',
		'magic_quotes_runtime=0',
		'ignore_repeated_errors=0',
		'precision=14',
		'unicode.runtime_encoding=ISO-8859-1',
		'unicode.script_encoding=UTF-8',
		'unicode.output_encoding=UTF-8',
		'unicode.from_error_mode=U_INVALID_SUBSTITUTE',
	);

function save_results()
{
	global $sum_results, $failed_test_summary, $compression,
	       $PHP_FAILED_TESTS, $CUR_DIR, $php, $output_dir, $compression;

    $output_file = $output_dir . '\\results.txt';
    if ($compression) $output_file = 'compress.zlib://' . $output_file . '.gz';

	$sep = "\n" . str_repeat('=', 80) . "\n";
	
	$failed_tests_data = '';
	$failed_tests_data .= $failed_test_summary . "\n";
	$failed_tests_data .= get_summary(true, false) . "\n";
    file_put_contents($output_file, $failed_tests_data);
    $failed_tests_data = '';

	if ($sum_results['FAILED']) {
		foreach ($PHP_FAILED_TESTS['FAILED'] as $test_info) {
			$failed_tests_data .= $sep . $test_info['name'] . $test_info['info'];
			$failed_tests_data .= $sep . file_get_contents(realpath($test_info['output']), FILE_BINARY);
			$failed_tests_data .= $sep . file_get_contents(realpath($test_info['diff']), FILE_BINARY);
			$failed_tests_data .= $sep . "\n\n";
			
			file_put_contents($output_file, $failed_tests_data, FILE_APPEND);
            $failed_tests_data = '';
		}
		$status = "failed";
	} else {
		$status = "success";
	}

	$failed_tests_data .= "\n" . $sep . 'BUILD ENVIRONMENT' . $sep;
	$failed_tests_data .= "OS:\n" . PHP_OS . " - " . php_uname() . "\n\n";
	$ldd = $autoconf = $sys_libtool = $libtool = $compiler = 'N/A';

	$failed_tests_data .= "Libraries:\n$ldd\n";
	$failed_tests_data .= "\n";

	file_put_contents($output_file, $failed_tests_data, FILE_APPEND);
    $failed_tests_data = '';

}

// Determine the tests to be run.

$test_files = array();
$redir_tests = array();
$test_results = array();
$PHP_FAILED_TESTS = array('BORKED' => array(), 'FAILED' => array(), 'WARNED' => array(), 'LEAKED' => array(), 'XFAILED' => array());

// If parameters given assume they represent selected tests to run.
$compression = 0;
$output_dir = $CUR_DIR . '\\results\\' . date('Ymd_Hi');

//$leak_check = false;
$www_target = $CUR_DIR . '\\www';
$web_url = null;
$ext_dir = $CUR_DIR . '\\tests\\ext';
$no_clean = false;

$log_format = "LD";
$environment = array();

$cfgtypes = array('show', 'keep');
$cfgfiles = array('skip', 'php', 'clean', 'out', 'diff', 'exp');
$cfg = array();

foreach($cfgtypes as $type) {
	$cfg[$type] = array();

	foreach($cfgfiles as $file) {
		$cfg[$type][$file] = false;
	}
}

if (isset($argc) && $argc > 1) {

	for ($i=1; $i<$argc; $i++) {
		$is_switch = false;
		$switch = substr($argv[$i],1,1);
		$repeat = substr($argv[$i],0,1) == '-';

		while ($repeat) {

			if (!$is_switch) {
				$switch = substr($argv[$i],1,1);
			}

			$is_switch = true;

			if ($repeat) {
				foreach($cfgtypes as $type) {
					if (strpos($switch, '--' . $type) === 0) {
						foreach($cfgfiles as $file) {
							if ($switch == '--' . $type . '-' . $file) {
								$cfg[$type][$file] = true;
								$is_switch = false;
								break;
							}
						}
					}
				}
			}

			if (!$is_switch) {
				$is_switch = true;
				break;
			}

			$repeat = false;

			switch($switch) {
				case 'd':
					$ini_overwrites[] = $argv[++$i];
					break;
				case '--no-clean':
					$no_clean = true;
					break;
				case '--show-all':
					foreach($cfgfiles as $file) {
						$cfg['show'][$file] = true;
					}
					break;
				case 'v':
				case '--verbose':
					$DETAILED = true;
					break;
				case 'x':
					$environment['SKIP_SLOW_TESTS'] = 1;
					break;
				//case 'w'
				case '-':
					// repeat check with full switch
					$switch = $argv[$i];
					if ($switch != '-') {
						$repeat = true;
					}
					break;
			    case "w":
				case "--www":
				    $www_target = $argv[++$i];
				    break;
			    case "--web":
				    $web_url = $argv[++$i];
				    break;
				case "o":
				case "--output":
				    $output_dir = $argv[++$i];
				    break;
			    case "-exts":
				case "--extensions":
				    $exts_to_test = explode(",",$argv[++$i]);
				    break;
				case "--extroot":
				    $ext_dir = $argv[++$i];
				    break;
			    default:
					echo "Illegal switch '$switch' specified!\n";
				case 'h':
				case '-help':
				case '--help':
					echo <<<HELP
Synopsis:
    php run-tests.php [options] [files] [directories]

Options:
    --verbose
    -v          Verbose mode.
    
    --www
    -w          Directory mapped onto IIS WWW application.
    
    --web       URL of IIS WWW application.
    
    --output
    -o          Directory for result files.
    
    --extensions
    -exts       List of extensions to test.
    
    --extroot   Root of extension tests.
    
    --help
    -h          This Help.
    
    -x          Skip slow tests.
    
    -d          INI overwrites.
    
    --show-all  

    --no-clean  Do not execute clean section if any.

HELP;
					exit(1);
			}
		}

		if (!$is_switch) {
			$testfile = realpath($argv[$i]);

			if (!$testfile && strpos($argv[$i], '*') !== false && function_exists('glob')) {

				if (preg_match("/\.phpt$/", $argv[$i])) {
					$pattern_match = glob($argv[$i]);
				} else if (preg_match("/\*$/", $argv[$i])) {
					$pattern_match = glob($argv[$i] . '.phpt');
				} else {
					die("bogus test name " . $argv[$i] . "\n");
				}

				if (is_array($pattern_match)) {
					$test_files = array_merge($test_files, $pattern_match);
				}

			} else if (is_dir($testfile)) {
				find_files($testfile);
			} else if (preg_match("/\.phpt$/", $testfile)) {
				$test_files[] = $testfile;
			} else {
				die("bogus test name " . $argv[$i] . "\n");
			}
		}
	}

	$test_files = array_unique($test_files);
	$test_files = array_merge($test_files, $redir_tests);

	// Run selected tests.
	$test_cnt = count($test_files);

	if ($test_cnt) {
		usort($test_files, "test_sort");
		$start_time = time();

		echo "Running selected tests.\n";
		
		$test_idx = 0;
		run_all_tests($test_files, $environment);
		$end_time = time();

		if (getenv('REPORT_EXIT_STATUS') == 1 and preg_match('/FAILED(?: |$)/', implode(' ', $test_results))) {
			exit(1);
		}

		exit(0);
	}
}

if (!$web_url)
    error("No URL specified.");

// Compile a list of all test files (*.phpt).
$test_files = array();
$exts_tested = count($exts_to_test);
$exts_skipped = 0;
$ignored_by_ext = 0;
sort($exts_to_test);

// Convert extension names to lowercase
foreach ($exts_to_test as $key => $val) {
	$exts_to_test[$key] = strtolower($val);
	find_files( $ext_dir . "\\" . $exts_to_test[$key] );
}

foreach ($user_tests as $dir) {
	find_files($dir);
}

function find_files($dir)
{
	global $test_files, $exts_to_test, $ignored_by_ext, $exts_skipped, $exts_tested, $cwd, $www_target;

    if (!is_dir($dir))
        return;

	$o = opendir($dir) or error("cannot open directory: $dir");

	while (($name = readdir($o)) !== false) {

        if ( in_array($name, array('.', '..', 'CVS')) )
            continue;

		if (is_dir("{$dir}/{$name}")) {
			find_files("{$dir}/{$name}");
		}

		// Cleanup any left-over tmp files from last run.
		if (substr($name, -4) == '.tmp') {
			@unlink("$dir/$name");
			continue;
		}

        if (substr($name, -4) == '.inc') {
			$incfile = realpath("{$dir}/{$name}");
			$wwwincfile = str_replace($cwd,$www_target,$incfile);
			mkdir(dirname($wwwincfile),0777,true);
			copy($incfile,$wwwincfile);
		}

		// Otherwise we're only interested in *.phpt files.
		if (substr($name, -5) == '.phpt') {
			$testfile = realpath("{$dir}/{$name}");
			$test_files[] = $testfile;
		}
	}

	closedir($o);
}

function test_name($name)
{
	if (is_array($name)) {
		return $name[0] . ':' . $name[1];
	} else {
		return $name;
	}
}

function test_sort($a, $b)
{
	global $cwd;

	$a = test_name($a);
	$b = test_name($b);

	$ta = strpos($a, "{$cwd}/tests") === 0 ? 1 + (strpos($a, "{$cwd}/tests/run-test") === 0 ? 1 : 0) : 0;
	$tb = strpos($b, "{$cwd}/tests") === 0 ? 1 + (strpos($b, "{$cwd}/tests/run-test") === 0 ? 1 : 0) : 0;

	if ($ta == $tb) {
		return strcmp($a, $b);
	} else {
		return $tb - $ta;
	}
}

$test_files = array_unique($test_files);
usort($test_files, "test_sort");

$start_time = time();

$test_cnt = count($test_files);
$test_idx = 0;
run_all_tests($test_files, $environment);
$end_time = time();

// Summarize results

if (0 == count($test_results)) {
	echo "No tests were run.\n";
	return;
}

compute_summary();

show_summary();

save_results();
 
if (getenv('REPORT_EXIT_STATUS') == 1 and $sum_results['FAILED']) {
	exit(1);
}
exit(0);

//
//  Write the given text to a temporary file, and return the filename.
//

function save_text($filename, $text, $filename_copy = null)
{
	global $DETAILED;

	if ($filename_copy && $filename_copy != $filename) {
		if (file_put_contents($filename_copy, (binary) $text, FILE_BINARY) === false) {
			error("Cannot open file '" . $filename_copy . "' (save_text)");
		}
	}

	if (file_put_contents($filename, (binary) $text, FILE_BINARY) === false) {
		error("Cannot open file '" . $filename . "' (save_text)");
	}

	if (1 < $DETAILED) echo "
FILE $filename {{{
$text
}}} 
";
}

//
//  Write an error in a format recognizable to Emacs or MSVC.
//

function error_report($testname, $logname, $tested) 
{
	$testname = realpath($testname);
	$logname  = realpath($logname);

	switch (strtoupper(getenv('TEST_PHP_ERROR_STYLE'))) {
		case 'MSVC':
			echo $testname . "(1) : $tested\n";
			echo $logname . "(1) :  $tested\n";
			break;
		case 'EMACS':
			echo $testname . ":1: $tested\n";
			echo $logname . ":1:  $tested\n";
			break;
	}
}

function run_all_tests($test_files, $env, $redir_tested = null)
{
	global $test_results, /*$php,*//*(modified) not needed*/ $test_cnt, $test_idx;

	foreach($test_files as $name) {

		if (is_array($name)) {
			$index = "# $name[1]: $name[0]";

			if ($redir_tested) {
				$name = $name[0];
			}
		} else if ($redir_tested) {
			$index = "# $redir_tested: $name";
		} else {
			$index = $name;
		}
		$test_idx++;
		$result = run_test($name, $env);

		if (!is_array($name) && $result != 'REDIR') {
			$test_results[$index] = $result;
		}
	}
}

//
//  Show file or result block
//
function show_file_block($file, $block, $section = null)
{
	global $cfg;

	if ($cfg['show'][$file]) {

		if (is_null($section)) {
			$section = strtoupper($file);
		}

		echo "\n========" . $section . "========\n";
		echo rtrim($block);
		echo "\n========DONE========\n";
	}
}

/**
 * StartsWith
 * Tests if a text starts with an given string.
 *
 * @param     string
 * @param     string
 * @return    bool
 */
function StartsWith($Haystack, $Needle){
    // Recommended version, using strpos
    return strpos($Haystack, $Needle) === 0;
}
function EndsWith($Haystack,$Needle) {
    $end = strlen($Haystack) - strlen($Needle);
    return strpos($Haystack, $Needle, $end) == $end;
}

function binary_section($section)
{
	return PHP_MAJOR_VERSION < 6 || 
		(
			$section == 'FILE'			||
	        $section == 'FILEEOF'		||
			$section == 'EXPECT'		||
			$section == 'EXPECTF'		||
			$section == 'EXPECTREGEX'	||
			$section == 'EXPECTHEADERS'	||
			$section == 'SKIPIF'		||
			$section == 'CLEAN'
		);
}

//
//  Run an individual test case.
//
function run_test($file, $env)
{
	global $log_format, $info_params, $ini_overwrites, $cwd, $web_url, $www_target, $output_dir, $PHP_FAILED_TESTS;
	global $DETAILED, $IN_REDIRECT, $test_cnt, $test_idx;
	global /*$leak_check,*/ $temp_source, $temp_target, $cfg, $environment;
	global $no_clean;
	global $valgrind_version;
	$temp_filenames = null;
	$org_file = $file;

	if (is_array($file)) {
		$file = $file[0];
	}

	if ($DETAILED) echo "
=================
TEST $file
";

	// Load the sections of the test file.
	$section_text = array('TEST' => '');

	$fp = fopen($file, "rb") or error("Cannot open test file: $file");

	$borked = false;
	$bork_info = '';

	if (!feof($fp)) {
		$line = fgets($fp);

		if ($line === false) {
			$bork_info = "cannot read test";
			$borked = true;
		}
	} else {
		$bork_info = "empty test [$file]";
		$borked = true;
	}
	if (!$borked && strncmp('--TEST--', $line, 8)) {
		$bork_info = "tests must start with --TEST-- [$file]";
		$borked = true;
	}

	$section = 'TEST';
	$secfile = false;
	$secdone = false;

	while (!feof($fp)) {
		$line = fgets($fp);

		if ($line === false) {
			break;
		}

		// Match the beginning of a section.
		if (preg_match(b'/^--([_A-Z]+)--/', $line, $r)) {
			$section = $r[1];
			settype($section, STRING_TYPE);

			if (isset($section_text[$section])) {
				$bork_info = "duplicated $section section";
				$borked    = true;
			}

			$section_text[$section] = binary_section($section) ? b'' : '';
			$secfile = $section == 'FILE' || $section == 'FILEEOF' || $section == 'FILE_EXTERNAL';
			$secdone = false;
			continue;
		}

		if (!binary_section($section)) {
			$line = (binary)$line;
			if ($line == false) {
				$bork_info = "cannot read test";
				$borked = true;
				break;
			}
		}

		// Add to the section text.
		if (!$secdone) {
		    if ($section == 'EXPECTF')
		    {
		        // modify Phalangers different error message format, ", column %d" at the end.
		        if (StartsWith($line, 'Warning:') || StartsWith($line,'Notice:') || StartsWith($line,'Error:'))
		            $line = substr($line,0,7) . "%s\n"; //', column %d.';
		    }
			$section_text[$section] .= $line;
		}

		// End of actual test?
		if ($secfile && preg_match(b'/^===DONE===\s*$/', $line)) {
			$secdone = true;
		}
	}

	// the redirect section allows a set of tests to be reused outside of
	// a given test dir
	if (!$borked) {
		if (@count($section_text['REDIRECTTEST']) == 1) {

			if ($IN_REDIRECT) {
				$borked = true;
				$bork_info = "Can't redirect a test from within a redirected test";
			} else {
				$borked = false;
			}

		} else {

			if (@count($section_text['FILE']) + @count($section_text['FILEEOF']) + @count($section_text['FILE_EXTERNAL']) != 1) {
				$bork_info = "missing section --FILE--";
				$borked = true;
			}

			if (@count($section_text['FILEEOF']) == 1) {
				$section_text['FILE'] = preg_replace(b"/[\r\n]+$/", b'', $section_text['FILEEOF']);
				unset($section_text['FILEEOF']);
			}

			if (@count($section_text['FILE_EXTERNAL']) == 1) {
				// don't allow tests to retrieve files from anywhere but this subdirectory
				$section_text['FILE_EXTERNAL'] = dirname($file) . '/' . trim(str_replace('..', '', $section_text['FILE_EXTERNAL']));

				if (file_exists($section_text['FILE_EXTERNAL'])) {
					$section_text['FILE'] = file_get_contents($section_text['FILE_EXTERNAL'], FILE_BINARY);
					unset($section_text['FILE_EXTERNAL']);
				} else {
					$bork_info = "could not load --FILE_EXTERNAL-- " . dirname($file) . '/' . trim($section_text['FILE_EXTERNAL']);
					$borked = true;
				}
			}

			if ((@count($section_text['EXPECT']) + @count($section_text['EXPECTF']) + @count($section_text['EXPECTREGEX'])) != 1) {
				$bork_info = "missing section --EXPECT--, --EXPECTF-- or --EXPECTREGEX--";
				$borked = true;
			}
		}
	}
	fclose($fp);

	$shortname = str_replace($cwd . '/', '', $file);
	$tested_file = $shortname;

	if ($borked) {
		show_result("BORK", $bork_info, $tested_file);
		$PHP_FAILED_TESTS['BORKED'][] = array (
								'name'      => $file,
								'test_name' => '',
								'output'    => '',
								'diff'      => '',
								'info'      => "$bork_info [$file]",
		);
		return 'BORKED';
	}

	$tested = trim($section_text['TEST']);

	show_test($test_idx, $shortname);

	if (is_array($IN_REDIRECT)) {
		$temp_dir = $test_dir = $IN_REDIRECT['dir'];
	} else {
		$temp_dir = $test_dir = realpath(dirname($file));
	}
	
	if (!StartsWith($temp_dir,$cwd))
	    return die("SKIPPED: Test cannot be outside the current directory.");
	
	$temp_dir = str_replace($cwd, $www_target, $temp_dir);
	$out_dir = str_replace($www_target, $output_dir, $temp_dir);

	$main_file_name = basename($file,'phpt');

	$diff_filename     = $out_dir . DIRECTORY_SEPARATOR . $main_file_name . 'diff';
	$log_filename      = $out_dir . DIRECTORY_SEPARATOR . $main_file_name . 'log';
	$exp_filename      = $temp_dir . DIRECTORY_SEPARATOR . $main_file_name . 'exp';
	$output_filename   = $temp_dir . DIRECTORY_SEPARATOR . $main_file_name . 'out';
	//$memcheck_filename = $temp_dir . DIRECTORY_SEPARATOR . $main_file_name . 'mem';
	//$sh_filename       = $temp_dir . DIRECTORY_SEPARATOR . $main_file_name . 'sh';
	$temp_file         = $temp_dir . DIRECTORY_SEPARATOR . $main_file_name . 'php';
	$test_file         = $test_dir . DIRECTORY_SEPARATOR . $main_file_name . 'php';
	$temp_skipif       = $temp_dir . DIRECTORY_SEPARATOR . $main_file_name . 'skip.php';
	$test_skipif       = $test_dir . DIRECTORY_SEPARATOR . $main_file_name . 'skip.php';
	$temp_clean        = $temp_dir . DIRECTORY_SEPARATOR . $main_file_name . 'clean.php';
	$test_clean        = $test_dir . DIRECTORY_SEPARATOR . $main_file_name . 'clean.php';
	$tmp_post          = $temp_dir . DIRECTORY_SEPARATOR . uniqid('/phpt.');
	$tmp_relative_file = str_replace(__DIR__ . DIRECTORY_SEPARATOR, '', $test_file) . 't';

    mkdir( $temp_dir,0777,true ) or error("Cannot create output directory - " . $temp_dir);
    mkdir( $out_dir,0777,true ) or error("Cannot create output directory - " . $out_dir);

	if (is_array($IN_REDIRECT)) {
		$tested = $IN_REDIRECT['prefix'] . ' ' . trim($section_text['TEST']);
		$tested_file = $tmp_relative_file;
	}

	// unlink old test results
	@unlink($diff_filename);
	@unlink($log_filename);
	@unlink($exp_filename);
	@unlink($output_filename);
	//@unlink($memcheck_filename);
	//@unlink($sh_filename);
	@unlink($temp_file);
	@unlink($test_file);
	@unlink($temp_skipif);
	@unlink($test_skipif);
	@unlink($tmp_post);
	@unlink($temp_clean);
	@unlink($test_clean);

	// Reset environment from any previous test.
	$env['REDIRECT_STATUS'] = '';
	$env['QUERY_STRING']    = '';
	$env['PATH_TRANSLATED'] = '';
	$env['SCRIPT_FILENAME'] = '';
	$env['REQUEST_METHOD']  = '';
	$env['CONTENT_TYPE']    = '';
	$env['CONTENT_LENGTH']  = '';
	$env['TZ']              = '';
	
	$opts = array(
	    'http'=>array(
            'method'=>  "GET"
        ));

	if (!empty($section_text['ENV'])) {

		foreach(explode("\n", trim($section_text['ENV'])) as $e) {
			$e = explode('=', trim($e), 2);

			if (!empty($e[0]) && isset($e[1])) {
				$env[$e[0]] = $e[1];
			}
		}
	}

	// Default ini settings
	$ini_settings = array();
	// additional ini overwrites
	//$ini_overwrites[] = 'setting=value';
	settings2array($ini_overwrites, $ini_settings);

	// Any special ini settings
	// these may overwrite the test defaults...
	if (array_key_exists('INI', $section_text)) {
		if (strpos($section_text['INI'], '{PWD}') !== false) {
			$section_text['INI'] = str_replace('{PWD}', dirname($file), $section_text['INI']);
		}
		settings2array(preg_split( "/[\n\r]+/", $section_text['INI']), $ini_settings);
	}

	//settings2params($ini_settings);

	// Check if test should be skipped.
	$info = '';
	$warn = false;

	if (array_key_exists('SKIPIF', $section_text)) {

		if (trim($section_text['SKIPIF'])) {
			show_file_block('skip', $section_text['SKIPIF']);
			save_text($temp_skipif, $section_text['SKIPIF']);
			$extra = substr(PHP_OS, 0, 3) !== "WIN" ?
				"unset REQUEST_METHOD; unset QUERY_STRING; unset PATH_TRANSLATED; unset SCRIPT_FILENAME; unset REQUEST_METHOD;": "";

			//if ($leak_check) {
				//$env['USE_ZEND_ALLOC'] = '0';
			//} else {
				//$env['USE_ZEND_ALLOC'] = '1';
			//}

			//$output = system_with_timeout("$extra $php $pass_options -q $ini_settings -d display_errors=0 $test_skipif", $env);
			$output = file_get_contents( str_replace($www_target,$web_url,$temp_skipif) );
			/*if (!$cfg['keep']['skip']) {
				@unlink($test_skipif);
			}*/

			if (!strncasecmp('skip', ltrim($output), 4)) {

				if (preg_match('/^\s*skip\s*(.+)\s*/i', $output, $m)) {
					show_result('SKIP', $tested, $tested_file, "reason: $m[1]", $temp_filenames);
				} else {
					show_result('SKIP', $tested, $tested_file, '', $temp_filenames);
				}

				if (isset($old_php)) {
					$php = $old_php;
				}

				if (!$cfg['keep']['skip']) {
					@unlink($test_skipif);
				}

				return 'SKIPPED';
			}

			if (!strncasecmp('info', ltrim($output), 4)) {
				if (preg_match('/^\s*info\s*(.+)\s*/i', $output, $m)) {
					$info = " (info: $m[1])";
				}
			}

			if (!strncasecmp('warn', ltrim($output), 4)) {
				if (preg_match('/^\s*warn\s*(.+)\s*/i', $output, $m)) {
					$warn = true; /* only if there is a reason */
					$info = " (warn: $m[1])";
				}
			}
		}
	}

	if (@count($section_text['REDIRECTTEST']) == 1) {
		$test_files = array();

		$IN_REDIRECT = eval($section_text['REDIRECTTEST']);
		$IN_REDIRECT['via'] = "via [$shortname]\n\t";
		$IN_REDIRECT['dir'] = realpath(dirname($file));
		$IN_REDIRECT['prefix'] = trim($section_text['TEST']);

		if (count($IN_REDIRECT['TESTS']) == 1) {

			if (is_array($org_file)) {
				$test_files[] = $org_file[1];
			} else {
				$GLOBALS['test_files'] = $test_files;
				find_files($IN_REDIRECT['TESTS']);

				foreach($GLOBALS['test_files'] as $f) {
					$test_files[] = array($f, $file);
				}
			}
			$test_cnt += @count($test_files) - 1;
			$test_idx--;

			show_redirect_start($IN_REDIRECT['TESTS'], $tested, $tested_file);

			// set up environment
			$redirenv = array_merge($environment, $IN_REDIRECT['ENV']);
			$redirenv['REDIR_TEST_DIR'] = realpath($IN_REDIRECT['TESTS']) . DIRECTORY_SEPARATOR;

			usort($test_files, "test_sort");
			run_all_tests($test_files, $redirenv, $tested);

			show_redirect_ends($IN_REDIRECT['TESTS'], $tested, $tested_file);

			// a redirected test never fails
			$IN_REDIRECT = false;
			return 'REDIR';

		} else {

			$bork_info = "Redirect info must contain exactly one TEST string to be used as redirect directory.";
			show_result("BORK", $bork_info, '', $temp_filenames);
			$PHP_FAILED_TESTS['BORKED'][] = array (
									'name' => $file,
									'test_name' => '',
									'output' => '',
									'diff'   => '',
									'info'   => "$bork_info [$file]",
			);
		}
	}

	if (is_array($org_file) || @count($section_text['REDIRECTTEST']) == 1) {

		if (is_array($org_file)) {
			$file = $org_file[0];
		}

		$bork_info = "Redirected test did not contain redirection info";
		show_result("BORK", $bork_info, '', $temp_filenames);
		$PHP_FAILED_TESTS['BORKED'][] = array (
								'name' => $file,
								'test_name' => '',
								'output' => '',
								'diff'   => '',
								'info'   => "$bork_info [$file]",
		);
		return 'BORKED';
	}

	// We've satisfied the preconditions - run the test!
	show_file_block('php', $section_text['FILE'], 'TEST');
	save_text($temp_file, $section_text['FILE']);

	if (array_key_exists('GET', $section_text)) {
		$query_string = trim($section_text['GET']);
	} else {
		$query_string = '';
	}

	$env['REDIRECT_STATUS'] = '1';
	$env['QUERY_STRING']    = $query_string;
	$env['PATH_TRANSLATED'] = $test_file;
	$env['SCRIPT_FILENAME'] = $test_file;

	if (array_key_exists('COOKIE', $section_text)) {
		$env['HTTP_COOKIE'] = trim($section_text['COOKIE']);
		$opts["http"]["header"] .= "Cookie: " . $env['HTTP_COOKIE'] . "\r\n";
	} else {
		$env['HTTP_COOKIE'] = '';
	}

	$args = isset($section_text['ARGS']) ? ' -- ' . $section_text['ARGS'] : '';

	if (array_key_exists('POST_RAW', $section_text) && !empty($section_text['POST_RAW'])) {

		$post = trim($section_text['POST_RAW']);
		$raw_lines = explode("\n", $post);

		$request = '';
		$started = false;

		foreach ($raw_lines as $line) {

			if (empty($env['CONTENT_TYPE']) && preg_match('/^Content-Type:(.*)/i', $line, $res)) {
				$env['CONTENT_TYPE'] = trim(str_replace("\r", '', $res[1]));
				continue;
			}

			if ($started) {
				$request .= "\n";
			}

			$started = true;
			$request .= $line;
		}

		$env['CONTENT_LENGTH'] = strlen($request);
		$env['REQUEST_METHOD'] = 'POST';
		
		$opts["http"]["method"] = "POST";
		$opts["http"]["header"] .= "Content-type: " . $env['CONTENT_TYPE'] . "\r\n";
		$opts["http"]["content"] = $request;

		if (empty($request)) {
			return 'BORKED';
		}

		//save_text($tmp_post, $request);
		//$cmd = "$php $pass_options $ini_settings -f \"$test_file\" 2>&1 < $tmp_post";

	} else if (array_key_exists('POST', $section_text) && !empty($section_text['POST'])) {

		$post = trim($section_text['POST']);

		if (array_key_exists('GZIP_POST', $section_text) && function_exists('gzencode')) {
			$post = gzencode($post, 9, FORCE_GZIP);
			$env['HTTP_CONTENT_ENCODING'] = 'gzip';
		} else if (array_key_exists('DEFLATE_POST', $section_text) && function_exists('gzcompress')) {
			$post = gzcompress($post, 9);
			$env['HTTP_CONTENT_ENCODING'] = 'deflate';
		}
		

		//save_text($tmp_post, $post);
		$content_length = strlen($post);

		$env['REQUEST_METHOD'] = 'POST';
		$env['CONTENT_TYPE']   = 'application/x-www-form-urlencoded';
		$env['CONTENT_LENGTH'] = $content_length;

        $opts["http"]["method"] = "POST";
		$opts["http"]["header"] .= "Content-type: " . $env['CONTENT_TYPE'] . "\r\n";
		$opts["http"]["header"] .= "Content-encoding: ". $env['HTTP_CONTENT_ENCODING'] . "\r\n";
		$opts["http"]["header"] .= "Content-length: ". $content_length . "\r\n";
		$opts["http"]["content"] = $post;
		
		//$cmd = "$php $pass_options $ini_settings -f \"$test_file\" 2>&1 < $tmp_post";

	} else {

		$env['REQUEST_METHOD'] = 'GET';
		$env['CONTENT_TYPE']   = '';
		$env['CONTENT_LENGTH'] = '';

		//$cmd = "$php $pass_options $ini_settings -f \"$test_file\" $args 2>&1";
	}

	//if ($leak_check) {
	//	$env['USE_ZEND_ALLOC'] = '0';

		//if ($valgrind_version >= 330) {
		//	/* valgrind 3.3.0+ doesn't have --log-file-exactly option */
		//	$cmd = "valgrind -q --tool=memcheck --trace-children=yes --log-file=$memcheck_filename $cmd";
		//} else {
		//	$cmd = "valgrind -q --tool=memcheck --trace-children=yes --log-file-exactly=$memcheck_filename $cmd";
		//}

	//} else {
	//	$env['USE_ZEND_ALLOC'] = '1';
	//}

	if ($DETAILED) /*echo "
CONTENT_LENGTH  = " . $env['CONTENT_LENGTH'] . "
CONTENT_TYPE    = " . $env['CONTENT_TYPE'] . "
PATH_TRANSLATED = " . $env['PATH_TRANSLATED'] . "
QUERY_STRING    = " . $env['QUERY_STRING'] . "
REDIRECT_STATUS = " . $env['REDIRECT_STATUS'] . "
REQUEST_METHOD  = " . $env['REQUEST_METHOD'] . "
SCRIPT_FILENAME = " . $env['SCRIPT_FILENAME'] . "
HTTP_COOKIE     = " . $env['HTTP_COOKIE'] . "
COMMAND $cmd
";*/ var_dump($opts);

    $context = stream_context_create($opts);
	$out = file_get_contents(str_replace($www_target,$web_url,$temp_file),false,$context); //(binary) system_with_timeout($cmd, $env, isset($section_text['STDIN']) ? $section_text['STDIN'] : null);
	
	if ($out === FALSE)
	{
	    $out = $out;
	    echo "\nProbably IIS crash occured while processing $file\n";
	}

    if ((StartsWith($out,"\r\nCompileError") || StartsWith($out,"\r\nCompileWarning")))
    {
        show_result('SKIP', $tested, $tested_file, "CompileError/Warning ...", $temp_filenames);
        return 'SKIPPED';
    }
    
	if (array_key_exists('CLEAN', $section_text) && (!$no_clean || $cfg['keep']['clean'])) {

		if (trim($section_text['CLEAN'])) {
			show_file_block('clean', $section_text['CLEAN']);
			save_text($test_clean, trim($section_text['CLEAN']), $temp_clean);

			if (!$no_clean) {
				$clean_params = array();
				settings2array($ini_overwrites, $clean_params);
				//settings2params($clean_params);
				//$extra = substr(PHP_OS, 0, 3) !== "WIN" ?
				//    "unset REQUEST_METHOD; unset QUERY_STRING; unset PATH_TRANSLATED; unset SCRIPT_FILENAME; unset REQUEST_METHOD;": "";
				//system_with_timeout("$extra $php $pass_options -q $clean_params $test_clean", $env);
				file_get_contents(str_replace($temp_target, $web_url, $temp_clean));
			}

			if (!$cfg['keep']['clean']) {
				@unlink($test_clean);
			}
		}
	}

	@unlink($tmp_post);

	$leaked = false;
	$passed = false;

	//if ($leak_check) { // leak check
	//	$leaked = filesize($memcheck_filename) > 0;
    //
    //		if (!$leaked) {
    //			@unlink($memcheck_filename);
    //		}
    //	}

	// Does the output match what is expected?
	$output = preg_replace(b"/\r\n/", b"\n", trim($out));

	/* when using CGI, strip the headers from the output */
	$headers = b"";

	if (isset($old_php) && preg_match(b"/^(.*?)\r?\n\r?\n(.*)/s", $out, $match)) {
		$output = trim($match[2]);
		$rh = preg_split(b"/[\n\r]+/", $match[1]);
		$headers = array();

		foreach ($rh as $line) {
			if (strpos($line, b':') !== false) {
				$line = explode(b':', $line, 2);
				$headers[trim($line[0])] = trim($line[1]);
			}
		}
	}

	$failed_headers = false;

	if (isset($section_text['EXPECTHEADERS'])) {
		$want = array();
		$wanted_headers = array();
		$lines = preg_split(b"/[\n\r]+/", (binary) $section_text['EXPECTHEADERS']);

		foreach($lines as $line) {
			if (strpos($line, b':') !== false) {
				$line = explode(b':', $line, 2);
				$want[trim($line[0])] = trim($line[1]);
				$wanted_headers[] = trim($line[0]) . b': ' . trim($line[1]);
			}
		}

		$org_headers = $headers;
		$headers = array();
		$output_headers = array();

		foreach($want as $k => $v) {

			if (isset($org_headers[$k])) {
				$headers = $org_headers[$k];
				$output_headers[] = $k . b': ' . $org_headers[$k];
			}

			if (!isset($org_headers[$k]) || $org_headers[$k] != $v) {
				$failed_headers = true;
			}
		}

		ksort($wanted_headers);
		$wanted_headers = join(b"\n", $wanted_headers);
		ksort($output_headers);
		$output_headers = join(b"\n", $output_headers);
	}

	show_file_block('out', $output);

	if (isset($section_text['EXPECTF']) || isset($section_text['EXPECTREGEX'])) {

		if (isset($section_text['EXPECTF'])) {
			$wanted = trim($section_text['EXPECTF']);
		} else {
			$wanted = trim($section_text['EXPECTREGEX']);
		}

		show_file_block('exp', $wanted);
		$wanted_re = preg_replace(b'/\r\n/', b"\n", $wanted);

		if (isset($section_text['EXPECTF'])) {

			// do preg_quote, but miss out any %r delimited sections
			$temp = b"";
			$r = b"%r";
			$startOffset = 0;
			$length = strlen($wanted_re);
			while($startOffset < $length) {
				$start = strpos($wanted_re, $r, $startOffset);
				if ($start !== false) {
					// we have found a start tag
					$end = strpos($wanted_re, $r, $start+2);
					if ($end === false) {
						// unbalanced tag, ignore it.
						$end = $start = $length;
					}
				} else {
					// no more %r sections
					$start = $end = $length;
				}
				// quote a non re portion of the string
				$temp = $temp . preg_quote(substr($wanted_re, $startOffset, ($start - $startOffset)),  b'/');
				// add the re unquoted.
				if ($end > $start) {
					$temp = $temp . b'(' . substr($wanted_re, $start+2, ($end - $start-2)). b')';
				}
				$startOffset = $end + 2;
			}
			$wanted_re = $temp;
		
			$wanted_re = str_replace(
				array(b'%binary_string_optional%'),
				version_compare(PHP_VERSION, '6.0.0-dev') == -1 ? b'string' : b'binary string',
				$wanted_re
			);
			$wanted_re = str_replace(
				array(b'%unicode_string_optional%'),
				version_compare(PHP_VERSION, '6.0.0-dev') == -1 ? b'string' : b'Unicode string',
				$wanted_re
			);
			$wanted_re = str_replace(
				array(b'%unicode\|string%', b'%string\|unicode%'),
				version_compare(PHP_VERSION, '6.0.0-dev') == -1 ? b'string' : b'unicode',
				$wanted_re
			);
			$wanted_re = str_replace(
				array(b'%u\|b%', b'%b\|u%'),
				version_compare(PHP_VERSION, '6.0.0-dev') == -1 ? b'' : b'u',
				$wanted_re
			);
			// Stick to basics
			$wanted_re = str_replace(b'%e', b'\\' . DIRECTORY_SEPARATOR, $wanted_re);
			$wanted_re = str_replace(b'%s', b'[^\r\n]+', $wanted_re);
			$wanted_re = str_replace(b'%S', b'[^\r\n]*', $wanted_re);
			$wanted_re = str_replace(b'%a', b'.+', $wanted_re);
			$wanted_re = str_replace(b'%A', b'.*', $wanted_re);
			$wanted_re = str_replace(b'%w', b'\s*', $wanted_re);
			$wanted_re = str_replace(b'%i', b'[+-]?\d+', $wanted_re);
			$wanted_re = str_replace(b'%d', b'\d+', $wanted_re);
			$wanted_re = str_replace(b'%x', b'[0-9a-fA-F]+', $wanted_re);
			$wanted_re = str_replace(b'%f', b'[+-]?\.?\d+\.?\d*(?:[Ee][+-]?\d+)?', $wanted_re);
			$wanted_re = str_replace(b'%c', b'.', $wanted_re);
			// %f allows two points "-.0.0" but that is the best *simple* expression
		}
/* DEBUG YOUR REGEX HERE
		var_dump($wanted_re);
		print(str_repeat('=', 80) . "\n");
		var_dump($output);
*/
		if (preg_match(b"/^$wanted_re\$/s", $output)) {
			$passed = true;
			if (!$cfg['keep']['php']) {
				@unlink($test_file);
			}
			if (isset($old_php)) {
				$php = $old_php;
			}

			if (!$leaked && !$failed_headers) {
				if (isset($section_text['XFAIL'] )) {
					$warn = true;
					$info = " (warn: XFAIL section but test passes)";
				}else {
					show_result("PASS", $tested, $tested_file, '', $temp_filenames);
					return 'PASSED';
				}
			}
		}

	} else {

		$wanted = (binary) trim($section_text['EXPECT']);
		$wanted = preg_replace(b'/\r\n/',b"\n", $wanted);
		show_file_block('exp', $wanted);

		// compare and leave on success
		if (!strcmp($output, $wanted)) {
			$passed = true;

			if (!$cfg['keep']['php']) {
				@unlink($test_file);
			}

			if (isset($old_php)) {
				$php = $old_php;
			}

			if (!$leaked && !$failed_headers) {
				if (isset($section_text['XFAIL'] )) {
					$warn = true;
					$info = " (warn: XFAIL section but test passes)";
				}else {
					show_result("PASS", $tested, $tested_file, '', $temp_filenames);
					return 'PASSED';
				}
			}
		}

		$wanted_re = null;
	}

	// Test failed so we need to report details.
	if ($failed_headers) {
		$passed = false;
		$wanted = (binary) $wanted_headers . b"\n--HEADERS--\n" . (binary) $wanted;
		$output = (binary) $output_headers . b"\n--HEADERS--\n" . (binary) $output;

		if (isset($wanted_re)) {
			$wanted_re = preg_quote($wanted_headers . "\n--HEADERS--\n", '/') . $wanted_re;
		}
	}

	if ($leaked) {
		$restype[] = 'LEAK';
	}

	if ($warn) {
		$restype[] = 'WARN';
	}

	if (!$passed) {
	    if (isset($section_text['XFAIL']))
			$restype[] = 'XFAIL';
		else
			$restype[] = 'FAIL';
	}

	if (!$passed) {

		// write .exp
		if (strpos($log_format, 'E') !== false && file_put_contents($exp_filename, (binary) $wanted, FILE_BINARY) === false) {
			error("Cannot create expected test output - $exp_filename");
		}

		// write .out
		if (strpos($log_format, 'O') !== false && file_put_contents($output_filename, (binary) $output, FILE_BINARY) === false) {
			error("Cannot create test output - $output_filename");
		}

		// write .diff
		$diff = generate_diff($wanted, $wanted_re, $output);
		if (is_array($IN_REDIRECT)) {
			$diff = "# original source file: $shortname\n" . $diff;
		}
		show_file_block('diff', $diff);
		if (strpos($log_format, 'D') !== false && file_put_contents($diff_filename, (binary) $diff, FILE_BINARY) === false) {
			error("Cannot create test diff - $diff_filename");
		}

		// write .sh
		//if (strpos($log_format, 'S') !== false && file_put_contents($sh_filename, b"#!/bin/sh{$cmd}", FILE_BINARY) === false) {
			//error("Cannot create test shell script - $sh_filename");
		//}
		//chmod($sh_filename, 0755);

		// write .log
		if (strpos($log_format, 'L') !== false && file_put_contents($log_filename, b"
---- EXPECTED OUTPUT
$wanted
---- ACTUAL OUTPUT
$output
---- FAILED
", FILE_BINARY) === false) {
			error("Cannot create test log - $log_filename");
			error_report($file, $log_filename, $tested);
		}
	}

	show_result(implode('&', $restype), $tested, $tested_file, $info, $temp_filenames);

	foreach ($restype as $type) {
		$PHP_FAILED_TESTS[$type.'ED'][] = array (
			'name'      => $file,
			'test_name' => (is_array($IN_REDIRECT) ? $IN_REDIRECT['via'] : '') . $tested . " [$tested_file]",
			'output'    => $output_filename,
			'diff'      => $diff_filename,
			'info'      => $info,
		);
	}

	if (isset($old_php)) {
		$php = $old_php;
	}

	return $restype[0] . 'ED';
}

function comp_line($l1, $l2, $is_reg)
{
	if ($is_reg) {
		return preg_match(b'/^'. (binary) $l1 . b'$/s', (binary) $l2);
	} else {
		return !strcmp((binary) $l1, (binary) $l2);
	}
}

function count_array_diff(&$ar1, &$ar2, $is_reg, &$w, $idx1, $idx2, $cnt1, $cnt2, $steps)
{
	$equal = 0;

	while ($idx1 < $cnt1 && $idx2 < $cnt2 && comp_line($ar1[$idx1], $ar2[$idx2], $is_reg)) {
		$idx1++;
		$idx2++;
		$equal++;
		$steps--;
	}
	if (--$steps > 0) {
		$eq1 = 0;
		$st = $steps / 2;

		for ($ofs1 = $idx1 + 1; $ofs1 < $cnt1 && $st-- > 0; $ofs1++) {
			$eq = @count_array_diff($ar1, $ar2, $is_reg, $w, $ofs1, $idx2, $cnt1, $cnt2, $st);

			if ($eq > $eq1) {
				$eq1 = $eq;
			}
		}

		$eq2 = 0;
		$st = $steps;

		for ($ofs2 = $idx2 + 1; $ofs2 < $cnt2 && $st-- > 0; $ofs2++) {
			$eq = @count_array_diff($ar1, $ar2, $is_reg, $w, $idx1, $ofs2, $cnt1, $cnt2, $st);
			if ($eq > $eq2) {
				$eq2 = $eq;
			}
		}

		if ($eq1 > $eq2) {
			$equal += $eq1;
		} else if ($eq2 > 0) {
			$equal += $eq2;
		}
	}

	return $equal;
}

function generate_array_diff(&$ar1, &$ar2, $is_reg, &$w)
{
	$idx1 = 0; $ofs1 = 0; $cnt1 = @count($ar1);
	$idx2 = 0; $ofs2 = 0; $cnt2 = @count($ar2);
	$diff = array();
	$old1 = array();
	$old2 = array();

	while ($idx1 < $cnt1 && $idx2 < $cnt2) {

		if (comp_line($ar1[$idx1], $ar2[$idx2], $is_reg)) {
			$idx1++;
			$idx2++;
			continue;
		} else {

			$c1 = @count_array_diff($ar1, $ar2, $is_reg, $w, $idx1+1, $idx2, $cnt1, $cnt2, 10);
			$c2 = @count_array_diff($ar1, $ar2, $is_reg, $w, $idx1, $idx2+1, $cnt1,  $cnt2, 10);

			if ($c1 > $c2) {
				$old1[$idx1] = (binary) sprintf("%03d- ", $idx1+1) . $w[$idx1++];
				$last = 1;
			} else if ($c2 > 0) {
				$old2[$idx2] = (binary) sprintf("%03d+ ", $idx2+1) . $ar2[$idx2++];
				$last = 2;
			} else {
				$old1[$idx1] = (binary) sprintf("%03d- ", $idx1+1) . $w[$idx1++];
				$old2[$idx2] = (binary) sprintf("%03d+ ", $idx2+1) . $ar2[$idx2++];
			}
		}
	}

	reset($old1); $k1 = key($old1); $l1 = -2;
	reset($old2); $k2 = key($old2); $l2 = -2;

	while ($k1 !== null || $k2 !== null) {

		if ($k1 == $l1 + 1 || $k2 === null) {
			$l1 = $k1;
			$diff[] = current($old1);
			$k1 = next($old1) ? key($old1) : null;
		} else if ($k2 == $l2 + 1 || $k1 === null) {
			$l2 = $k2;
			$diff[] = current($old2);
			$k2 = next($old2) ? key($old2) : null;
		} else if ($k1 < $k2) {
			$l1 = $k1;
			$diff[] = current($old1);
			$k1 = next($old1) ? key($old1) : null;
		} else {
			$l2 = $k2;
			$diff[] = current($old2);
			$k2 = next($old2) ? key($old2) : null;
		}
	}

	while ($idx1 < $cnt1) {
		$diff[] = (binary) sprintf("%03d- ", $idx1 + 1) . $w[$idx1++];
	}

	while ($idx2 < $cnt2) {
		$diff[] = (binary) sprintf("%03d+ ", $idx2 + 1) . $ar2[$idx2++];
	}

	return $diff;
}

function generate_diff($wanted, $wanted_re, $output)
{
	$w = explode(b"\n", $wanted);
	$o = explode(b"\n", $output);
	$r = is_null($wanted_re) ? $w : explode(b"\n", $wanted_re);
	$diff = generate_array_diff($r, $o, !is_null($wanted_re), $w);

	return implode(b"\r\n", $diff);
}

function error($message)
{
	echo "ERROR: {$message}\n";
	exit(1);
}

function settings2array($settings, &$ini_settings)
{
	foreach($settings as $setting) {

		if (strpos($setting, '=') !== false) {
			$setting = explode("=", $setting, 2);
			$name = trim(strtolower($setting[0]));
			$value = trim($setting[1]);

			if ($name == 'extension') {

				if (!isset($ini_settings[$name])) {
					$ini_settings[$name] = array();
				}

				$ini_settings[$name][] = $value;

			} else {
				$ini_settings[$name] = $value;
			}
		}
	}
}

function settings2params(&$ini_settings)
{
	$settings = '';

	foreach($ini_settings as $name => $value) {

		if (is_array($value)) {
			foreach($value as $val) {
				$val = addslashes($val);
				$settings .= " -d \"$name=$val\"";
			}
		} else {
			if (substr(PHP_OS, 0, 3) == "WIN" && !empty($value) && $value{0} == '"') {
				$len = strlen($value);

				if ($value{$len - 1} == '"') {
					$value{0} = "'";
					$value{$len - 1} = "'";
				}
			} else {
				$value = addslashes($value);
			}

			$settings .= " -d \"$name=$value\"";
		}
	}

	$ini_settings = $settings;
}

function compute_summary()
{
	global $n_total, $test_results, $ignored_by_ext, $sum_results, $percent_results;

	$n_total = count($test_results);
	$n_total += $ignored_by_ext;
	$sum_results = array(
		'PASSED'  => 0,
		'WARNED'  => 0,
		'SKIPPED' => 0,
		'FAILED'  => 0,
		'BORKED'  => 0,
		'LEAKED'  => 0,
		'XFAILED' => 0
	);

	foreach ($test_results as $v) {
		$sum_results[$v]++;
	}

	$sum_results['SKIPPED'] += $ignored_by_ext;
	$percent_results = array();

	while (list($v, $n) = each($sum_results)) {
		$percent_results[$v] = (100.0 * $n) / $n_total;
	}
}

function get_summary($show_ext_summary, $show_html)
{
	global $exts_skipped, $exts_tested, $n_total, $sum_results, $percent_results, $end_time, $start_time, $failed_test_summary, $PHP_FAILED_TESTS/*, $leak_check*/;

	$x_total = $n_total - $sum_results['SKIPPED'] - $sum_results['BORKED'];

	if ($x_total) {
		$x_warned = (100.0 * $sum_results['WARNED']) / $x_total;
		$x_failed = (100.0 * $sum_results['FAILED']) / $x_total;
		$x_xfailed = (100.0 * $sum_results['XFAILED']) / $x_total;
		$x_leaked = (100.0 * $sum_results['LEAKED']) / $x_total;
		$x_passed = (100.0 * $sum_results['PASSED']) / $x_total;
	} else {
		$x_warned = $x_failed = $x_passed = $x_leaked = $x_xfailed = 0;
	}

	$summary = '';

	if ($show_html) {
		$summary .= "<pre>\n";
	}
	
	if ($show_ext_summary) {
		$summary .= '
=====================================================================
TEST RESULT SUMMARY
---------------------------------------------------------------------
Exts skipped    : ' . sprintf('%4d', $exts_skipped) . '
Exts tested     : ' . sprintf('%4d', $exts_tested) . '
---------------------------------------------------------------------
';
	}

	$summary .= '
Number of tests : ' . sprintf('%4d', $n_total) . '          ' . sprintf('%8d', $x_total);

	if ($sum_results['BORKED']) {
		$summary .= '
Tests borked    : ' . sprintf('%4d (%5.1f%%)', $sum_results['BORKED'], $percent_results['BORKED']) . ' --------';
	}

	$summary .= '
Tests skipped   : ' . sprintf('%4d (%5.1f%%)', $sum_results['SKIPPED'], $percent_results['SKIPPED']) . ' --------
Tests warned    : ' . sprintf('%4d (%5.1f%%)', $sum_results['WARNED'], $percent_results['WARNED']) . ' ' . sprintf('(%5.1f%%)', $x_warned) . '
Tests failed    : ' . sprintf('%4d (%5.1f%%)', $sum_results['FAILED'], $percent_results['FAILED']) . ' ' . sprintf('(%5.1f%%)', $x_failed) . '
Expected fail   : ' . sprintf('%4d (%5.1f%%)', $sum_results['XFAILED'], $percent_results['XFAILED']) . ' ' . sprintf('(%5.1f%%)', $x_xfailed);

	//if ($leak_check) {
		//$summary .= '
//Tests leaked    : ' . sprintf('%4d (%5.1f%%)', $sum_results['LEAKED'], $percent_results['LEAKED']) . ' ' . sprintf('(%5.1f%%)', $x_leaked);
	//}

	$summary .= '
Tests passed    : ' . sprintf('%4d (%5.1f%%)', $sum_results['PASSED'], $percent_results['PASSED']) . ' ' . sprintf('(%5.1f%%)', $x_passed) . '
---------------------------------------------------------------------
Time taken      : ' . sprintf('%4d seconds', $end_time - $start_time) . '
=====================================================================
';
	$failed_test_summary = '';

	if (count($PHP_FAILED_TESTS['BORKED'])) {
		$failed_test_summary .= '
=====================================================================
BORKED TEST SUMMARY
---------------------------------------------------------------------
';
		foreach ($PHP_FAILED_TESTS['BORKED'] as $failed_test_data) {
			$failed_test_summary .= $failed_test_data['info'] . "\n";
		}

		$failed_test_summary .=  "=====================================================================\n";
	}

	if (count($PHP_FAILED_TESTS['FAILED'])) {
		$failed_test_summary .= '
=====================================================================
FAILED TEST SUMMARY
---------------------------------------------------------------------
';
		foreach ($PHP_FAILED_TESTS['FAILED'] as $failed_test_data) {
			$failed_test_summary .= $failed_test_data['test_name'] . $failed_test_data['info'] . "\n";
		}
		$failed_test_summary .=  "=====================================================================\n";
	}
	if (count($PHP_FAILED_TESTS['XFAILED'])) {
		$failed_test_summary .= '
=====================================================================
EXPECTED FAILED TEST SUMMARY
---------------------------------------------------------------------
';
		foreach ($PHP_FAILED_TESTS['XFAILED'] as $failed_test_data) {
			$failed_test_summary .= $failed_test_data['test_name'] . $failed_test_data['info'] . "\n";
		}
		$failed_test_summary .=  "=====================================================================\n";
	}

	if (count($PHP_FAILED_TESTS['WARNED'])) {
		$failed_test_summary .= '
=====================================================================
WARNED TEST SUMMARY
---------------------------------------------------------------------
';
		foreach ($PHP_FAILED_TESTS['WARNED'] as $failed_test_data) {
			$failed_test_summary .= $failed_test_data['test_name'] . $failed_test_data['info'] . "\n";
		}

		$failed_test_summary .=  "=====================================================================\n";
	}

	if (count($PHP_FAILED_TESTS['LEAKED'])) {
		$failed_test_summary .= '
=====================================================================
LEAKED TEST SUMMARY
---------------------------------------------------------------------
';
		foreach ($PHP_FAILED_TESTS['LEAKED'] as $failed_test_data) {
			$failed_test_summary .= $failed_test_data['test_name'] . $failed_test_data['info'] . "\n";
		}

		$failed_test_summary .=  "=====================================================================\n";
	}

	if ($failed_test_summary && !getenv('NO_PHPTEST_SUMMARY')) {
		$summary .= $failed_test_summary;
	}

	if ($show_html) {
		$summary .= "</pre>";
	}

	return $summary;
}

function show_summary()
{
	echo get_summary(true, false);
}

function show_redirect_start($tests, $tested, $tested_file)
{
	echo "---> $tests ($tested [$tested_file]) begin\n";
}

function show_redirect_ends($tests, $tested, $tested_file)
{
	echo "---> $tests ($tested [$tested_file]) done\n";
}

function show_test($test_idx, $shortname)
{
	global $test_cnt;

	echo "TEST $test_idx/$test_cnt [$shortname]\r";
	flush();
}

function show_result($result, $tested, $tested_file, $extra = '', $temp_filenames = null)
{
	global $temp_target, $temp_urlbase;

	echo "$result $tested [$tested_file] $extra\n";
}

/*
 * Local variables:
 * tab-width: 4
 * c-basic-offset: 4
 * End:
 * vim: noet sw=4 ts=4
 */
?>
