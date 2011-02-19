<html xmlns="http://www.w3.org/1999/xhtml">
<head>
  <title>Phalanger Samples - Session Variables</title>
</head>
<body>
<h1>Phalanger Samples - Session Variables via ASP.NET</h1>
<p>
  This sample shows how session variables are handled.
  Phalanger uses ASP.NET session management and doesn't support some specific PHP features.
  However, common functions working with session variables are implemented.
  Note that session management is configured in ASP.NET configuration, section <i>system.web/sessionState</i>.
</p> 
<h3>Current values loaded from session store</h3>

$_SESSION auto-global array content:
<pre>
<?
  session_start();
  var_dump($_SESSION); 
?>
</pre>

<?
class A 
{ 
  public $value;
}
?>

<h3>Storing an instance of a class to the session</h3>
A class <code>A</code> having an instance field <code>$value</code> is declared in this script
and it is added to the session. You can reload this page by submitting of the following form.
The object will then appear into the $_SESSION array dumped above.
Its field will contain an array of declared classes.

<form method="get">
  <input type="submit">
</form>

<?
  // create an object:
  $a = new A;
  
  // set field falue:
  $a->value = get_declared_classes();
  
  // add to session:
  $_SESSION["instance_of_A"] = $a;
?>
</body>
</html>
