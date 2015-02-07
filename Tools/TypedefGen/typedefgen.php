<?php
/*
  REQUIRES: PHP 4.3.0-pre1 or higher

  This program generates XML typedef files from the C source code
  of extensions.  For functions, the code _must_ have protos

  typedefgen : Generate typedef files from proto defs (req PHP 4.3.0-pre1+)
  Usage: php typedefgen.php <path to ext directory of PHP> <extensions version>
     Ex: php typedefgen.php /php4/ext 4.4.9

*/

$funclist=array();
$num_funcs=0;

$constlist=array();
$num_const=0;

$generate_constants=1;
$generate_functions=1;
$source_files=array();
$extension_name="";
$constant_dir="";
$function_dir="";


function new_function()
{
  global $funclist, $num_funcs;
  $funclist[$num_funcs]["args"]=array();
  $funclist[$num_funcs]["num_args"]=0;
  $num_funcs++;
  return($num_funcs-1);
}

function fix_name($name)
{
  $replace = array('_' => '-',
                   '::' => '-',
                   '->' => '-');

  $name = strtr($name, $replace);
  $name = strtr($name, array('---' => '-'));

  return strtolower($name);
}

function function_add_name($num, $name)
{
  global $funclist, $num_funcs;

  $funclist[$num]["function_name"]=$name;
  $funclist[$num]["function_name_fix"]=fix_name($name);
  return(1);
}

function function_add_type($num, $type)
{
  global $funclist, $num_funcs;
  $funclist[$num]["function_type"]=$type;
  return(1);
}

function function_add_casttofalse($num, $castToFalse)
{
  global $funclist, $num_funcs;
  $funclist[$num]["castToFalse"]=$castToFalse;
  return(1);
}

function function_add_purpose($num, $purpose)
{
  global $funclist, $num_funcs;
  $funclist[$num]["purpose"]=$purpose;
  return(1);
}

function function_add_alias($num, $alias)
{
  global $funclist;

  $funclist[$num]["alias"][]=$alias;

  return(1);
}


function function_add_arg($num, $type, $argname, $isopt)
{
  global $funclist, $num_funcs;

  /* Avoid common mistake in parameter return types */
  if ($type == 'long') {
    $type = 'int';
  }
  $num_args=$funclist[$num]["num_args"];
  $funclist[$num]["args"][$num_args]["type"]=$type;
  $funclist[$num]["args"][$num_args]["variable"]=$argname;
  $funclist[$num]["args"][$num_args]["isopt"]=$isopt;
  $funclist[$num]["num_args"]++;

  return(1);
}

function write_functions_xml()
{
  global $funclist, $num_funcs, $extension_name, $phpversion, $warning, $aliases;

  $rename = false;
  $fp=0;

  $filename= /*(is_array($warning) && count($warning) > 0 ? "!!!" : "" ).*/"php_" . $extension_name. ".xml";
  $fp=fopen($filename, "wb");
  if (!$fp) {
      echo "Failed writing: $filename\r\n";
      return;
  }
  
  fwrite($fp, '<?xml version="1.0" encoding="utf-8"?>'."\r\n" .
               '<!DOCTYPE module SYSTEM "module.dtd">'."\r\n" .
			   '<!--PHP VERSION: '.$phpversion.'-->'."\r\n" .
               '<module>' . "\r\n");

	if(is_array($warning))
	{
		for($i = 0;$i < count($warning); ++$i)
		{
			fwrite($fp,"<!-- ". $warning[$i]. " -->\r\n");
		}
	}

    foreach ($aliases as $a_func_name => $a_alias) {
       fwrite($fp,"<!-- ". $a_func_name . " aliased by: ");
       
       if (is_array($a_alias))       
       {
            foreach ($a_alias as $alias) {
                fwrite($fp, $alias . " , ");
            }
        }

        fwrite($fp," wasn't found.-->\r\n");
    }

    //sort by name of the function
    $tmp = Array(); 
    foreach($funclist as &$ma) 
        $tmp[] = &$ma["function_name"]; 
    array_multisort($tmp, $funclist); 
			   
  
  for ($i=0; $i<$num_funcs; $i++) {

    $fixname  = trim($funclist[$i]["function_name_fix"]);
    $funcname = trim($funclist[$i]["function_name"]);
    $purpose  = trim($funclist[$i]["purpose"]);
    $functype = trim($funclist[$i]["function_type"]);
    $isCastToFalse = $funclist[$i]["castToFalse"];

	//  <function returnType="" name="" description="">
    fwrite($fp, "  <function ".($isCastToFalse ? 'CastToFalse="true" ' : '') ."returnType=\"$functype\" name=\"$funcname\" description=\"$purpose\">\r\n");

    $argnames = array();
    for ($j=0; $j<$funclist[$i]["num_args"]; $j++) {
      $argtype = $funclist[$i]["args"][$j]["type"];
      $argname = $funclist[$i]["args"][$j]["variable"];
      $isref = (strpos($argname, '&') === 0);
      if ($isref) {
        $argname = substr($argname, 1);
      }
      $isopt=$funclist[$i]["args"][$j]["isopt"];
	  
	  if ($argname == "" || $argtype== "")
		$rename = true;
	  
	  //    <param optional="true" type="string" name="lib_dir" />
	  // TODO: ($isref ? " role=\"reference\"" : "")
      fwrite($fp, '    <param' . ($isopt ? ' optional="true"' : '') .($isref ? " direction=\"inout\"" : ""). " type=\"$argtype\" name=\"$argname\"/>\r\n");
      $argnames[] = $argname;

    }

    if (isset($funclist[$i]["alias"]))
    {
        foreach ($funclist[$i]["alias"] as $alias) {
            fwrite($fp, "    <alias name=\"$alias\"/>\r\n");
        }
    }

    // if ($funclist[$i]["num_args"] == 0){
      // fwrite($fp, "   <void/>\r\n");
    // }

    fwrite($fp,
        "  </function>\r\n"
    );
  }
  
    fwrite($fp, '</module>' . "\r\n");
	fclose($fp);
	echo "Wrote $num_funcs typdef entries into $filename\r\n";
	
	if (is_array($warning) && count($warning) > 0)
		return 1;
		
		
	//if ($rename)
		//rename($filename,"!!!".$filename);
  //
  return(1);
}

function read_file($filename)
{
  $fp = fopen($filename, "rb");
  if ($fp == 0) return("");
  $buffer=fread($fp, filesize($filename));
  fclose($fp);
  return($buffer);
}

function parse_desc($func_num, $data)
{
 global $warning, $func_name;
  // require at least 5 chars for the description (to skip empty or '*' lines)
  if (!preg_match('/(.{5,})/', $data, $match)) {
    echo "$func_name : Not a proper description definition: $data\r\n";
	$warning[] = "$func_name : Not a proper description definition: $data\r\n";
    return;
  }
  $data = htmlentities(trim($match[1], "* \t\n"),ENT_QUOTES);
  function_add_purpose($func_num, $data);
}

function parse_proto($proto, $source)
{
  global $warning, $func_name, $aliases;

  if (!preg_match('/proto\s+(?:(?:static|final|protected)\s+)?([a-zA-Z]+)\s+([a-zA-Z0-9:_-]+)\s*\((.*)\)\s+([\000-\377]+)/', $proto, $match)) {
    echo "Not a proper proto definition: $proto\r\n";
	$warning[] = "Not a proper proto definition: $proto\r\n";
    return;
  }

  $func_name = $match[2];

  $func_number = new_function();
  function_add_type($func_number, $match[1]);
  function_add_name($func_number, $match[2]);

  function_add_casttofalse($func_number, is_castToFalse($match[1],$source));
  parse_desc($func_number, $match[4]);

  // now parse the arguments
  // original: /(?:(\[),\s*)?([a-zA-Z]+)\s+(&?[a-zA-Z0-9:_-]+)/
  preg_match_all('/(?:(\[)\s*,\s*)?(?:,?\s*(\[)\s*)?([a-zA-Z]+)\s+(&?[a-zA-Z0-9:_-]+)|(?:,\s*(\[)\s*)?([a-zA-Z]+)\s+(&?[a-zA-Z0-9:_-]+)/', $match[3], $match, PREG_SET_ORDER);

  foreach ($match as $arg) {
    function_add_arg($func_number, $arg[3], $arg[4], $arg[1] || $arg[2]);
  }

  if (!isset($aliases[$func_name]))
    return;

  //add functions that aliasing this function
  foreach ($aliases[$func_name] as $alias) {
   function_add_alias($func_number, $alias);
//   echo "alias $alias";
  }
  unset($aliases[$func_name]);

}

function parse_file($buffer)
{
  add_aliases($buffer);

  preg_match_all('@/\*\s*{{{\s*(proto.+)\*/@sU', $buffer, $match);

  $split = preg_split('@/\*\s*{{{\s*(proto.+)\*/@sU', $buffer);
  $i = 1;

  foreach($match[1] as $proto) {
    parse_proto(trim($proto),$split[$i++]);

  }
}

function add_aliases($buffer)
{
   global $aliases;

   preg_match_all('/PHP_FALIAS\(([^,]*),([^,]*),/',$buffer,$match, PREG_SET_ORDER );

   if($match ==null)
    return;

   foreach($match as $alias) {
        $aliases[trim($alias[2])][] = trim($alias[1]);
  }

 var_dump($aliases);

}

function is_castToFalse($returnType,$source,$debug=false)
{
    if($returnType == "bool" || $returnType == "mixed")
        return false;

    $split = preg_split('/\*\s*}}}/m',$source);
    if ($debug) var_dump($split);

    if (preg_match("/RETURN_FALSE/",$split[0]))
        return true;

    return false;
}


function create_xml_docs()
{
  global $source_files, $generate_constants, $generate_functions;
  global $funclist, $num_funcs;
  $num=count($source_files);

  for ($i=0; $i<$num; $i++) {
    echo "READING " . $source_files[$i] . "\r\n";
    $contents=read_file($source_files[$i]);
    if ($contents == false || $contents == "") {
      echo "Could not read {$source_files[$i]}\r\n";
    }
    parse_file($contents);
  }


    echo "Writing function XML files\r\n";
    write_functions_xml();


  return(1);
}

function minimum_version($vercheck) {
  if(version_compare(phpversion(), $vercheck) == -1) {
    return false;
  } else {
    return true;
  }
}

function process_extension($name,$path)
{
  global $extension_name, $source_files, $funclist, $num_funcs, $warning, $aliases;
  
     echo "Processing ".$name. " extension \r\n".$path. "\r\n";
  
     $source_files= array();
	 $funclist=array();
	 $num_funcs=0;
	 $warning = array();
	 $aliases = array();
	 
	 $extension_name = $name;
     $temp_source_files=glob($path);
     $num=count($source_files);
     $new_num=count($temp_source_files);
     for ($j=0; $j<$new_num; $j++) {
       $source_files[$num+$j]=$temp_source_files[$j];
     }
	 $total=count($source_files);
	 
	 // if (is_dir("./typedefs/" . $extension_name)) {
      // echo "Warning: ./$extension_name already exists, skipping...\r\n";
	// } else {
      // mkdir("./typedefs/" . $extension_name);
	// }
	create_xml_docs();
	echo "\r\n";
}

function iterate_extensions($path) {
   $parsefiles = array();
   $srcdir = dir($path);
   while (false !== ($file = $srcdir->read())) {
	   $filepath = $path."".$file;
	   if (is_dir($filepath) && $file !== "." && $file !== "..") {
		  process_extension($file,$filepath."\*.c");
	   }
   }
   $srcdir->close();
}

function usage($progname)
{
  echo "typedefgen : Generate typedef files from proto defs (req PHP 4.3.0-pre1+)\r\n";
  echo "Usage: " . $progname . " <path to ext directory of PHP>\r\n";
  echo "   Ex: " . $progname . " /php4/ext\r\n\r\n";
}

if (minimum_version('5.0')) {
$myargc=$argc;
$myargv=$argv;
} else {
$myargc=$_SERVER["argc"];
$myargv=$_SERVER["argv"];
}

if (!minimum_version("4.3.0")) {
  echo "You need PHP 4.3.0-pre1 or higher!\r\n";
  $ver=phpversion();
  echo "YOU HAVE: $ver\r\n";
  exit();
}

if ($myargc != 3) {
  usage($myargv[0]);
  exit();
}
echo $myargv[1];
$phpversion = $myargv[2];
	 
$typedefdir = "./typedefs";

if (is_dir($typedefdir))
{
	$d = dir($typedefdir);
	chdir($typedefdir );
	
	while($entry = $d->read())
	{  
		if ($entry!= "." && $entry!= "..")
		   unlink($entry); 
	} 
	$d->close();
}
else
{

	mkdir($typedefdir );
	chdir($typedefdir );
}

iterate_extensions($myargv[1]);

//create_xml_docs();


echo <<<NOTES

Note: Also be sure to double check the documentation before commit as this
      script is still being tested.  Things to check:
      
      a) The parameter names in the prototype must be alphanumeric (no spaces 
         or other characters).  Sometimes this isn't the case in the PHP sources.
      b) Be sure optional parameters are listed as such, and vice versa.
      c) The script defaults to --with-{ext} but it could be different, like
         maybe --enable-{ext} OR a directory path is required or optional
      d) If you're writing over files in CVS, be 100% sure to check unified
         diffs before commit!
      e) Run script check-references.php and add role="reference" where required.
      f) Fill-in the Purpose and Membership comments in reference.xml and run
         extensions.xml.php.


NOTES;
?>
