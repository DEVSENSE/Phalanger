<?php

/**
 * Combines multiple javascript files and serve up as gzip if possible.
 * Allowable scripts and script dependencies can be specified in a "packages.php" file
 * with the following format. This "packages.php" is optional, if absent the filenames
 * without ".js" extension are used.
 *
 * <code>
 * <?php
 *  $packages = array(
 *     'package1' => array('file1.js', 'file2.js'),
 *     'package2' => array('file3.js', 'file4.js'));
 *
 *  $dependencies = array(
 *     'package1' => array('package1'),
 *     'package2' => array('package1', 'package2')); //package2 requires package1 first.
 * </code>
 *
 * To serve up 'package1', specify the url, a maxium of 25 packages separated with commas is allows.
 *
 * clientscripts.php?js=package1
 *
 * for 'package2' (automatically resolves 'package1') dependency
 *
 * clientscripts.php?js=package2
 *
 * The scripts comments are removed and whitespaces removed appropriately. The
 * scripts may be served as zipped if the browser and php server allows it. Cache
 * headers are also sent to inform the browser and proxies to cache the file.
 * Moreover, the post-processed (comments removed and zipped) are saved in this
 * current directory for the next requests.
 *
 * If the url contains the parameter "mode=debug", the comments are not removed
 * and headers invalidating the cache are sent. In debug mode, the script can still
 * be zipped if the browser and server supports it.
 *
 * E.g. clientscripts.php?js=package2&mode=debug
 *
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2007 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @author Wei Zhuo<weizhuo[at]gmail[dot]com>
 * @version $Id$
 * @package System.Web.Javascripts
 * @since 3.1
 */

//run script for as long as needed
@set_time_limit(0);

//set error_reporting directive
@error_reporting(E_ERROR | E_WARNING | E_PARSE);

function get_client_script_files()
{
	$package_file = dirname(__FILE__).'/packages.php';
	if(is_file($package_file))
		return get_package_files($package_file, get_script_requests());
	else
		return get_javascript_files(get_script_requests());
}

/**
 * @param array list of requested libraries
 */
function get_script_requests($max=25)
{
	$param = isset($_GET['js']) ? $_GET['js'] : '';
	if(preg_match('/([a-zA-z0-9\-_])+(,[a-zA-z0-9\-_]+)*/', $param))
		return array_unique(explode(',', $param, $max));
	return array();
}

/**
 * @param string packages.php filename
 * @param array packages requests
 */
function get_package_files($package_file, $request)
{
	list($packages, $dependencies) = include($package_file);
	if(!(is_array($packages) && is_array($dependencies)))
	{
		error_log('Prado client script: invalid package file '.$package_file);
		return array();
	}
	$result = array();
	$found = array();
	foreach($request as $library)
	{
		if(isset($dependencies[$library]))
		{
			foreach($dependencies[$library] as $dep)
			{
				if(isset($found[$dep]))
					continue;
				$result = array_merge($result, (array)$packages[$dep]);
				$found[$dep]=true;
			}
		}
		else
			error_log('Prado client script: no such javascript library "'.$library.'"');
	}
	return $result;
}

/**
 * @param string requested javascript files
 * @array array list of javascript files.
 */
function get_javascript_files($request)
{
	$result = array();
	foreach($request as $file)
		$result[] = $file.'.js';
	return $result;
}

/**
 * @param array list of javascript files.
 * @return string combined the available javascript files.
 */
function combine_javascript($files)
{
	$content = '';
	$base = dirname(__FILE__);
	foreach($files as $file)
	{
		$filename = $base.'/'.$file;
		if(is_file($filename)) //relies on get_client_script_files() for security
			$content .= "\x0D\x0A".file_get_contents($filename); //add CR+LF
		else
			error_log('Prado client script: missing file '.$filename);
	}
	return $content;
}

/**
 * @param string javascript code
 * @param array files names
 * @return string javascript code without comments.
 */
function minify_javascript($content, $files)
{
	return JSMin::minify($content);
}

/**
 * @param boolean true if in debug mode.
 */
function is_debug_mode()
{
	return isset($_GET['mode']) && $_GET['mode']==='debug';
}

/**
 * @param string javascript code
 * @param string gzip code
 */
function gzip_content($content)
{
	return gzencode($content, 9, FORCE_GZIP);
}

/**
 * @param string javascript code.
 * @param string filename
 */
function save_javascript($content, $filename)
{
	file_put_contents($filename, $content);
	if(supports_gzip_encoding())
		file_put_contents($filename.'.gz', gzip_content($content));
}

/**
 * @param string comprssed javascript file to be read
 * @param string javascript code, null if file is not found.
 */
function get_saved_javascript($filename)
{
	$fn=$filename;
	if(supports_gzip_encoding())
		$fn .= '.gz';
	if(!is_file($fn))
		save_javascript(get_javascript_code(true), $filename);
	return file_get_contents($fn);
}

/**
 * @return string compressed javascript file name.
 */
function compressed_js_filename()
{
	$files = get_client_script_files();
	if(count($files) > 0)
	{
		$filename = sprintf('%x',crc32(implode(',',($files))));
		return dirname(__FILE__).'/clientscript_'.$filename.'.js';
	}
}

/**
 * @param boolean true to strip comments from javascript code
 * @return string javascript code
 */
function get_javascript_code($minify=false)
{
	$files = get_client_script_files();
	if(count($files) > 0)
	{
		$content = combine_javascript($files);
		if($minify)
			return minify_javascript($content, $files);
		else
			return $content;
	}
}

/**
 * Prints headers to serve javascript
 *
 * @param string $filePath (default: NULL)
 */
function print_headers($filePath = null)
{
	$expiresOffset = is_debug_mode() ? -10000 : 3600 * 24 * 10; //no cache
	header("Content-type: text/javascript");
	header("Vary: Accept-Encoding");  // Handle proxies
	header("Expires: " . @gmdate("D, d M Y H:i:s", @time() + $expiresOffset) . " GMT");
	if(null !== $filePath && is_file($filePath)) {
		header("Last-Modified: " . @gmdate("D, d M Y H:i:s", filectime($filePath)) . " GMT");
	}
	if(($enc = supports_gzip_encoding()) !== null)
		header("Content-Encoding: " . $enc);
}

/**
 * @return string 'x-gzip' or 'gzip' if php supports gzip and browser supports gzip response, null otherwise.
 */
function supports_gzip_encoding()
{
	if(isset($_GET['gzip']) && $_GET['gzip']==='false')
		return false;

	if (isset($_SERVER['HTTP_ACCEPT_ENCODING']))
	{
		$encodings = explode(',', strtolower(preg_replace("/\s+/", "", $_SERVER['HTTP_ACCEPT_ENCODING'])));
		$allowsZipEncoding = in_array('gzip', $encodings) || in_array('x-gzip', $encodings) || isset($_SERVER['---------------']);
		$hasGzip = function_exists('ob_gzhandler');
		$noZipBuffer = !ini_get('zlib.output_compression');
		$noZipBufferHandler = ini_get('output_handler') != 'ob_gzhandler';

		if ( $allowsZipEncoding && $hasGzip && $noZipBuffer && $noZipBufferHandler)
			$enc = in_array('x-gzip', $encodings) ? "x-gzip" : "gzip";
		return $enc;
	}
}

/**
 * jsmin.php - PHP implementation of Douglas Crockford's JSMin.
 *
 * This is pretty much a direct port of jsmin.c to PHP with just a few
 * PHP-specific performance tweaks. Also, whereas jsmin.c reads from stdin and
 * outputs to stdout, this library accepts a string as input and returns another
 * string as output.
 *
 * PHP 5 or higher is required.
 *
 * Permission is hereby granted to use this version of the library under the
 * same terms as jsmin.c, which has the following license:
 *
 * --
 * Copyright (c) 2002 Douglas Crockford  (www.crockford.com)
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of
 * this software and associated documentation files (the "Software"), to deal in
 * the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
 * of the Software, and to permit persons to whom the Software is furnished to do
 * so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * The Software shall be used for Good, not Evil.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 * --
 *
 * @package JSMin
 * @author Ryan Grove <ryan@wonko.com>
 * @copyright 2002 Douglas Crockford <douglas@crockford.com> (jsmin.c)
 * @copyright 2008 Ryan Grove <ryan@wonko.com> (PHP port)
 * @license http://opensource.org/licenses/mit-license.php MIT License
 * @version 1.1.1 (2008-03-02)
 * @link http://code.google.com/p/jsmin-php/
 */

class JSMin {
  const ORD_LF    = 10;
  const ORD_SPACE = 32;

  protected $a           = '';
  protected $b           = '';
  protected $input       = '';
  protected $inputIndex  = 0;
  protected $inputLength = 0;
  protected $lookAhead   = null;
  protected $output      = '';

  // -- Public Static Methods --------------------------------------------------

  public static function minify($js) {
    $jsmin = new JSMin($js);
    return $jsmin->min();
  }

  // -- Public Instance Methods ------------------------------------------------

  public function __construct($input) {
    $this->input       = str_replace("\r\n", "\n", $input);
    $this->inputLength = strlen($this->input);
  }

  // -- Protected Instance Methods ---------------------------------------------

  protected function action($d) {
    switch($d) {
      case 1:
        $this->output .= $this->a;

      case 2:
        $this->a = $this->b;

        if ($this->a === "'" || $this->a === '"') {
          for (;;) {
            $this->output .= $this->a;
            $this->a       = $this->get();

            if ($this->a === $this->b) {
              break;
            }

            if (ord($this->a) <= self::ORD_LF) {
              throw new JSMinException('Unterminated string literal.');
            }

            if ($this->a === '\\') {
              $this->output .= $this->a;
              $this->a       = $this->get();
            }
          }
        }

      case 3:
        $this->b = $this->next();

        if ($this->b === '/' && (
            $this->a === '(' || $this->a === ',' || $this->a === '=' ||
            $this->a === ':' || $this->a === '[' || $this->a === '!' ||
            $this->a === '&' || $this->a === '|' || $this->a === '?')) {

          $this->output .= $this->a . $this->b;

          for (;;) {
            $this->a = $this->get();

            if ($this->a === '/') {
              break;
            } elseif ($this->a === '\\') {
              $this->output .= $this->a;
              $this->a       = $this->get();
            } elseif (ord($this->a) <= self::ORD_LF) {
              throw new JSMinException('Unterminated regular expression '.
                  'literal.');
            }

            $this->output .= $this->a;
          }

          $this->b = $this->next();
        }
    }
  }

  protected function get() {
    $c = $this->lookAhead;
    $this->lookAhead = null;

    if ($c === null) {
      if ($this->inputIndex < $this->inputLength) {
        $c = $this->input[$this->inputIndex];
        $this->inputIndex += 1;
      } else {
        $c = null;
      }
    }

    if ($c === "\r") {
      return "\n";
    }

    if ($c === null || $c === "\n" || ord($c) >= self::ORD_SPACE) {
      return $c;
    }

    return ' ';
  }

  protected function isAlphaNum($c) {
    return ord($c) > 126 || $c === '\\' || preg_match('/^[\w\$]$/', $c) === 1;
  }

  protected function min() {
    $this->a = "\n";
    $this->action(3);

    while ($this->a !== null) {
      switch ($this->a) {
        case ' ':
          if ($this->isAlphaNum($this->b)) {
            $this->action(1);
          } else {
            $this->action(2);
          }
          break;

        case "\n":
          switch ($this->b) {
            case '{':
            case '[':
            case '(':
            case '+':
            case '-':
              $this->action(1);
              break;

            case ' ':
              $this->action(3);
              break;

            default:
              if ($this->isAlphaNum($this->b)) {
                $this->action(1);
              }
              else {
                $this->action(2);
              }
          }
          break;

        default:
          switch ($this->b) {
            case ' ':
              if ($this->isAlphaNum($this->a)) {
                $this->action(1);
                break;
              }

              $this->action(3);
              break;

            case "\n":
              switch ($this->a) {
                case '}':
                case ']':
                case ')':
                case '+':
                case '-':
                case '"':
                case "'":
                  $this->action(1);
                  break;

                default:
                  if ($this->isAlphaNum($this->a)) {
                    $this->action(1);
                  }
                  else {
                    $this->action(3);
                  }
              }
              break;

            default:
              $this->action(1);
              break;
          }
      }
    }

    return $this->output;
  }

  protected function next() {
    $c = $this->get();

    if ($c === '/') {
      switch($this->peek()) {
        case '/':
          for (;;) {
            $c = $this->get();

            if (ord($c) <= self::ORD_LF) {
              return $c;
            }
          }

        case '*':
          $this->get();

          for (;;) {
            switch($this->get()) {
              case '*':
                if ($this->peek() === '/') {
                  $this->get();
                  return ' ';
                }
                break;

              case null:
                throw new JSMinException('Unterminated comment.');
            }
          }

        default:
          return $c;
      }
    }

    return $c;
  }

  protected function peek() {
    $this->lookAhead = $this->get();
    return $this->lookAhead;
  }
}

// -- Exceptions ---------------------------------------------------------------
class JSMinException extends Exception {}


/************** OUTPUT *****************/

if(count(get_script_requests()) > 0)
{
	if(is_debug_mode())
	{
		if(($code = get_javascript_code()) !== null)
		{
			print_headers();
			echo supports_gzip_encoding() ? gzip_content($code) : $code;
		}
	}
	else
	{
		if(($filename = compressed_js_filename()) !== null)
		{
			if(!is_file($filename))
				save_javascript(get_javascript_code(true), $filename);
			print_headers($filename);
			echo get_saved_javascript($filename);
		}
	}
}
