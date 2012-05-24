<?php
session_start();
$sqlite = extension_loaded("sqlite") ? "" : "&nbsp;(does not work : missing sqlite)";
?>
<html>
<head>
</head>
<body>
    <fieldset>
        <legend>Prado 3.1.10.r3017</legend>
		<a href="prado/index.html">index</a><br />
		<a href="prado/requirements/index.php">Requirements</a><br />
        <ul>
            <li><a href="prado/demos/helloworld/index.php">Hello world !</a></li>
            <li><a href="prado/demos/composer/index.php">Composer</a>&nbsp;(code generation does not work : System.OutOfMemoryException)</li>
            <li><a href="prado/demos/chat/index.php">Chat</a><?php echo $sqlite; ?></li>
            <li><a href="prado/demos/quickstart/index.php?page=Controls.Standard">QuickStart, Standard controls</a></li>
        </ul>
    </fieldset>
    <pre>
    From C#, added in Global.asax.cs at Session_Start event. var_export is :
<?php
    echo var_export($_SESSION, true).'<br />';

include_once("prado/framework/prado.php");
Prado::using('System.I18N.core.HTTPNegotiator');
echo "class_exists('HTTPNegotiator',false)=".(class_exists('HTTPNegotiator',false) ? "true":"false")."<br />";
flush();
$c = new HTTPNegotiator();
?>
	</pre>
	<pre>
	</pre>
	<hr />
<?php
phpinfo();
?>
	<hr />
	By MaitreDede :)
</body>
</html>