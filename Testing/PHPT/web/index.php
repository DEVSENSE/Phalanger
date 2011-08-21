<?
    
    define('FILE_BINARY',0);

    function &list_test_dirs()
    {
        // build the array with directories (can contain subdirectories)
        $tests_dir = array('tests/tests', 'tests/zend');    // default test directories

        // add tests of loaded extensions into the list
        foreach (get_loaded_extensions() as $ext)
            $tests_dir[] = "tests/ext/$ext";

        return $tests_dir;
    }

    function &list_test_files()
    {
        $test_files = array();
        $filter = @$_GET['filter'];

        // build list of (relative) directories containing .phpt files
        $tests_dir = list_test_dirs();

        for($i = 0; $i < count($tests_dir); ++$i)
        {
            $dir = $tests_dir[$i];
            if (!is_dir($dir)) continue;

	        $o = opendir($dir) or die("cannot open directory: $dir");

	        while (($name = readdir($o)) !== false)
            {
                // We're only interested in *.phpt files.
		        if (substr($name, -5) == '.phpt')
                {
                    $fullpath = "{$dir}/{$name}";
                    if (!$filter || strpos($fullpath, $filter) !== FALSE)
			            $test_files[] = "{$dir}/{$name}";
                }

                // otherwise recursivelly process valid subdirs
                else if ( !in_array($name, array('.', '..', 'CVS', '.svn')) && is_dir("{$dir}/{$name}"))
                    $tests_dir[] = "{$dir}/{$name}";
	        }

	        closedir($o);
        }

        return $test_files;
    }

    function emit_js_array(&$arr)
    {
        $first = true;
        echo '[';
        foreach ($arr as $item)
        {
            if ($first)$first = false;
            else echo ',';

            echo "'$item'";
        }
        echo ']';
    }

    // list all tests, and run them using AJAX
    function run_all_tests()
    {
        // list .phpt files in specified directories
        $test_files = &list_test_files();

        // generate HTML that requests .phpt tests asynchronously and in parallel
        ?>
        <ul id='list'>
        </ul>
        <script type="text/javascript">
            files = <? emit_js_array( &$test_files ); ?>;
        </script>		
        <?
    }

    if (isset($_GET['test']))
    {   //
        // run specified test
        //
        run_test($_GET['test'], $_GET['location']);
    }
    else
    {
    ?>
    <html><head>
        <title>PHPT Phalanger Tester</title>
        <script type="text/javascript" src="jquery-1.6.2.min.js"></script>
        <script type="text/javascript" src="tests.js"></script>
        <style>
            .state{display:none;}
            .error{color:#f0f;}
            .pass{color:#0f0;}
            .fail{color:#f00;}
            .skip{color:#888;}
        </style>
    </head><body>
    <?
        run_all_tests();
    ?>
    </body></html><?
    }
    

//
//  Run an individual test case.
//

// parse test file, return array of sections
function &parse_file($file)
{
    if (!is_file($file))
        error("$file does not exist");

    // parse the test $file

    // Load the sections of the test file.
	$section_text = array('TEST' => '');
	$fp = fopen($file, "rb") or error("Cannot open test file: $file");

	if (!feof($fp)) {
		$line = fgets($fp);

		if ($line === false) {
			error("cannot read test '$file'");
		}
	} else {
		error("empty test '$file'");
	}
	if (strncmp('--TEST--', $line, 8)) {
		error("tests '$file' must start with --TEST--");
	}

	$section = 'TEST';
	$secfile = false;
	$secdone = false;

	while (!feof($fp)) {
		$line = fgets($fp);

		if ($line === false)
			break;

		// Match the beginning of a section.
		if (preg_match(b'/^--([_A-Z]+)--/', $line, $r)) {
			$section = $r[1];
			settype($section, 'string');

			if (isset($section_text[$section]))
				error("duplicated $section section");
			
			$section_text[$section] = binary_section($section) ? b'' : '';
			$secfile = $section == 'FILE' || $section == 'FILEEOF' || $section == 'FILE_EXTERNAL';
			$secdone = false;
			continue;
		}

		if (!binary_section($section)) {
			$line = (binary)$line;
			if ($line == false) {
				error("cannot read test '$file'");
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
	if (@count($section_text['REDIRECTTEST']) == 1) {

		//if ($IN_REDIRECT) {
			//$borked = true;
			//$bork_info = "Can't redirect a test from within a redirected test";
		//} else {
			//$borked = false;
		//}
	} else {

		if (@count($section_text['FILE']) + @count($section_text['FILEEOF']) + @count($section_text['FILE_EXTERNAL']) != 1) {
			error("missing section --FILE-- in '$file'");
		}

		if (@count($section_text['FILEEOF']) == 1) {
			$section_text['FILE'] = preg_replace(b"/[\r\n]+$/", b'', $section_text['FILEEOF']);
			unset($section_text['FILEEOF']);
		}

		if (@count($section_text['FILE_EXTERNAL']) == 1) {
			// don't allow tests to retrieve files from anywhere but this subdirectory
			$section_text['FILE_EXTERNAL'] = dirname($file) . '/' . trim(str_replace('..', '', $section_text['FILE_EXTERNAL']));

			if (file_exists($section_text['FILE_EXTERNAL'])) {
				$section_text['FILE'] = _file_get_contents($section_text['FILE_EXTERNAL'], FILE_BINARY, null, &$dummyheaders);
				unset($section_text['FILE_EXTERNAL']);
			} else {
				error("could not load --FILE_EXTERNAL-- " . dirname($file) . '/' . trim($section_text['FILE_EXTERNAL']) . " in test '$file'");
			}
		}

		if ((@count($section_text['EXPECT']) + @count($section_text['EXPECTF']) + @count($section_text['EXPECTREGEX'])) != 1) {
			error("missing section --EXPECT--, --EXPECTF-- or --EXPECTREGEX-- in '$file'");
		}
	}
	fclose($fp);

    // return sections
    return $section_text;
}

function _file_get_contents($path, $flags, $context, &$headers)
{
	$result = file_get_contents($path, $flags, $context);
	$headers = @$http_response_header;
	
	return $result;
}

function try_skip($file, $www, &$section_text)
{
    if (array_key_exists('SKIPIF', $section_text)) {

        $skiphtml = "<span class='skip'>SKIP</span>";

        $info = '';
	    $warn = false;

		if (trim($section_text['SKIPIF'])) {
            $temp_skipif = replace_extension($file,'skipif.php');
			save_text($temp_skipif, $section_text['SKIPIF']);
			
            // Create a stream
            $output = _file_get_contents( $www . $temp_skipif, false, null, &$dummyheaders );
			
            if (!strncasecmp('skip', ltrim($output), 4)) {

				if (preg_match('/^\s*skip\s*(.+)\s*/i', $output, $m)) {
					show_result($skiphtml, $file, ", reason: $m[1]");
				} else {
					show_result($skiphtml, $file, '');
				}
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
}

function try_redirect($file, $www, &$section_text)
{
    if (@count($section_text['REDIRECTTEST']) == 1) {
		error("Redirect not supported yet in '$file'");
	}
}

function try_clean($file, $www, &$section_text)
{
    if (array_key_exists('CLEAN', $section_text)) {

		if (trim($section_text['CLEAN'])) {
            $cleanfile = replace_extension($file,'clean.php');
			save_text($cleanfile, trim($section_text['CLEAN']));

			$clean_params = array();
			_file_get_contents($www . $cleanfile, false, null, &$dummyheaders);
			
            @unlink($cleanfile);
		}
	}
}

function get_ini_code($ini_settings)
{
    if (count($ini_settings) == 0) return '';

    $code = '';
    foreach ($ini_settings as $key => $value)
    {
        $code .= "@ini_set('$key', '$value');";
    }

    return "<?php $code ?>";
}

function replace_extension($filename, $new_extension) {
    return preg_replace('/\..+$/', '.' . $new_extension, $filename);
}

function run_test($file,$www)
{
    // parse the test '$file'
    $section_text = parse_file($file);

    // setup environment
    if (EndsWith( $www, '.php')) $www = dirname($www);
    if (!EndsWith( $www, '/')) $www .= '/';
    $phpfile = replace_extension($file,'php');
    $tested = trim($section_text['TEST']);

    $env = array('HTTP_CONTENT_ENCODING'=>'');
	$opts = array(
	    'http'=>array(
            'method'=> "GET",
            'header'=> ''
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
	$ini_settings = array(); // put additional INI settings here
    // Any special ini settings, these may overwrite the test defaults...
	if (array_key_exists('INI', $section_text)) {
		if (strpos($section_text['INI'], '{PWD}') !== false) {
			$section_text['INI'] = str_replace('{PWD}', dirname(realpath($file)), $section_text['INI']);
		}
		settings2array(preg_split( "/[\n\r]+/", $section_text['INI']), $ini_settings);
	}

    // prepend custom ini settings
    if (count($ini_settings) > 0)  $section_text['FILE'] = get_ini_code($ini_settings) .  $section_text['FILE'];

    // skip this test ?
    try_skip($file, $www, $section_text);
    
    // redirect test ?
    try_redirect($file, $www, $section_text);

    // request .php script
    save_text($phpfile, $section_text['FILE']);

	if (array_key_exists('GET', $section_text))
		$query_string = trim($section_text['GET']);
	else
		$query_string = '';
	
	$env['QUERY_STRING']    = $query_string;
	
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

		if (empty($request))
			error("POST empty in '$file'");

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
	} else {

		$env['REQUEST_METHOD'] = 'GET';
		$env['CONTENT_TYPE']   = '';
		$env['CONTENT_LENGTH'] = '';
	}

    $context = stream_context_create($opts);
	$out = _file_get_contents($www . $phpfile, false, $context, &$headers);
	
	if ($out === FALSE) {
		echo '<br/>';
	    error("See <a target='_blank' href='$phpfile'>$phpfile</a>, exception ");
	}	
	
    if ((StartsWith($out,"\r\nCompileError") || StartsWith($out,"\r\nCompileWarning")))
        show_result("<span class='skip'>SKIP</span>", $file, ", Script generates <b>CompileError</b> or <b>CompileWarning</b>, so it cannot be compared with PHP. <a href='$phpfile' target='_blank'>Try the script</a><pre>$out</pre>");
    
    // perform clean
    try_clean($file, $www, $section_text);

    // compare .php response with expected output

	// Does the output match what is expected?
	$output = preg_replace(b"/\r\n/", b"\n", trim($out));
	$output = str_replace("string[binary](","string(",$output);
    
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
    
	if (isset($section_text['EXPECTF']) || isset($section_text['EXPECTREGEX'])) {

		if (isset($section_text['EXPECTF'])) {
			$wanted = trim($section_text['EXPECTF']);
		} else {
			$wanted = trim($section_text['EXPECTREGEX']);
		}

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
			$wanted_re = str_replace(b'%e', b'\\' . '\\', $wanted_re);
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
			@unlink($phpfile);

            show_result("<span class='pass'>PASS</span>", "$file", '');
		}

	} else {

		$wanted = (binary) trim($section_text['EXPECT']);
		$wanted = preg_replace(b'/\r\n/',b"\n", $wanted);
		
		// compare and leave on success
		if (!strcmp($output, $wanted)) {
			@unlink($phpfile);
			
            show_result("<span class='pass'>PASS</span>", $file, '');
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

	/*if ($leaked) {
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
	}*/

	// // if (!$passed)
    {

		// write .exp
		if (file_put_contents($file . '.exp', (binary) $wanted, FILE_BINARY) === false) {
			error("Cannot create expected test output '$file.exp'");
		}

		// write .out
		if (file_put_contents($file . '.out', (binary) $output, FILE_BINARY) === false) {
			error("Cannot create test output - '$file.out'");
		}

		// write .diff
		$diff = generate_diff($wanted, $wanted_re, $output);
		if (file_put_contents($file.'.diff',
         (binary) $diff, FILE_BINARY) === false) {
			error("Cannot create test diff - '$file.diff'");
		}

		$resultid = "result_" . strlen($phpfile) . '_' . crc32($phpfile);
		$sourceid = "source_" . strlen($phpfile) . '_' . crc32($phpfile);
		
        show_result(
			"<span class='fail'>FAIL</span>",
			$file,
			", <a href='$phpfile' target='_blank'>Try the script</a>" .
			", <a href='#' onclick='$(\"#$sourceid\").slideToggle();return false;'>source</a>" .
			", <a href='#' onclick='$(\"#$resultid\").slideToggle();return false;'>details</a>" .
			"<div id='$sourceid' style='display:none;background:#eee;border:1px dashed #888;'><pre>".htmlspecialchars(trim(_file_get_contents($file,false,null,&$dummyheaders)))."</pre></div>" .
			"<div id='$resultid' style='display:none;'><table border='1'><tr><td><b>Output</b><br/><pre style='background:#fee;font-size:8px;'>".htmlspecialchars($output)."</pre></td><td><b>Expected</b><br/><pre  style='background:#efe;font-size:8px;'>".htmlspecialchars($wanted)."</pre></td></tr></table></div>"
			);
		// write .sh
		//if (strpos($log_format, 'S') !== false && file_put_contents($sh_filename, b"#!/bin/sh{$cmd}", FILE_BINARY) === false) {
			//error("Cannot create test shell script - $sh_filename");
		//}
		//chmod($sh_filename, 0755);
	}

	/*foreach ($restype as $type) {
		$PHP_FAILED_TESTS[$type.'ED'][] = array (
			'name'      => $file,
			'test_name' => (is_array($IN_REDIRECT) ? $IN_REDIRECT['via'] : '') . $tested . " [$tested_file]",
			'output'    => $output_filename,
			'diff'      => $diff_filename,
			'info'      => $info,
		);
	}*/

	
}

function save_text($filename, $text, $filename_copy = null)
{
	if (file_put_contents($filename, (binary) $text, FILE_BINARY) === false) {
		error("Cannot open file '" . $filename . "' (save_text)");
	}

    if ($filename_copy && $filename_copy != $filename)
		save_text($filename_copy, $text);
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
	die("<b class='error'>ERROR:</b> {$message} in <a href='?test=".$_GET['test']."&location=".$_GET['location']."' target='_blank'>".$_GET['test']."</a>");
}

function show_result($state, $file, $text)
{
    $testurl = "?test=".$_GET['test']."&location=".$_GET['location']."&displaystandalone=1";

	if (isset($_GET['displaystandalone']))
    	echo '<script type="text/javascript" src="jquery-1.6.2.min.js"></script>';
    	
    echo "<div class='state'>$state</div><b>$state:</b> <a href='$testurl' target='_blank'>".$_GET['test']."</a>$text";
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

function binary_section($section)
{
	return /*PHP_MAJOR_VERSION < 6 || */
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


?>