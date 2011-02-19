<html xmlns="http://www.w3.org/1999/xhtml">
<head>
  <title>Phalanger Samples - Configuration</title>
</head>
<body>
<h1>Phalanger Samples - Configuration</h1>
<p>
  This sample shows the current Phalanger configuration and content of the prototypic Web.config configuration 
  file located in the current directory (<? getcwd(); ?>). There are all supported configuration options
  stated in this file along with default values and their respective PHP names if applicable.
  If you are configuring your own application, you don't need to define all options but only those
  that differ from the defaults.
</p> 
<?
  phpinfo(INFO_CONFIGURATION);
?>
<h2>Prototypic Web.config File</h2>
<pre>
<?
// reads a content of file Web.config in the current working directory and applies filter function htmlspecialchars
// which converts HTML special charaters like <, &, etc. to entities:
ob_start("htmlspecialchars");
readfile("Web.config");
ob_end_flush();  
?>
</pre>
</body>
</html>