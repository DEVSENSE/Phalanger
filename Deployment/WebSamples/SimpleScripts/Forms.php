<html xmlns="http://www.w3.org/1999/xhtml">
<head>
  <title>Phalanger Samples - Request Variables, Uploaded Files</title>
</head>
<body>
<h1>Phalanger Samples - Request Variables, Uploaded Files</h1>
<p>
  This sample shows how request variables are passed to PHP scripts and how files can be uploaded.
</p> 
<h2>Current values of request variables and uploaded files information</h2>

$_GET auto-global array content:
<pre>
<? print_r($_GET); ?>
</pre>

$_POST auto-global array content:
<pre>
<? print_r($_POST); ?>
</pre>

$_FILES auto-global array content:
<pre>
<? print_r($_FILES); ?>
</pre>

<h2>Forms sending data</h2>
<p>
A form that sends post data back to this script.
Variable called "post_var" will appear in $_POST array.</br>
<form method="post">
  <input name="post_var" value="<? echo isset($_POST["post_var"]) ? addslashes($_POST["post_var"]) : "This is post datum" ?>"/>
  <input type="submit"/>
</form>
</hr>
A form that sends get data back to this script.
An array "get_var" containing two values will appear in $_GET array.</br> 
<form method="get">
  <input name="get_var[]" value="<? echo isset($_GET["get_var"][0]) ? addslashes($_GET["get_var"][0]) : "get #1" ?>"/>
  <input name="get_var[]" value="<? echo isset($_GET["get_var"][1]) ? addslashes($_GET["get_var"][1]) : "get #2" ?>"/>
  <input type="submit"/>
</form>
</hr>
<h2>A form uploading files</h2>
Information about uploaded file will appear in the $_FILES array.</br>
<form enctype="multipart/form-data" method="POST"> 
  <p>
  Choose a file to sent (note, its size should be less than a limit set in ASP.NET configuration option
  <i>maxRequestLength</i> in section <i>system.web/httpRuntime</i>):
  </p>
  <input name="userfile" type="file"><br> 
  <input type="submit" value="Send File"> 
</form>
</body>
</html>