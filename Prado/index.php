<?php
session_start();
$sqlite = extension_loaded("sqlite") ? "" : "&nbsp;(does not work : missing sqlite)";
?>
<html>
<head>
</head>
<body>
    <table>
        <tr>
            <td>
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
            </td>
            <td>
                <fieldset>
                    <legend>SQLite</legend>
		            <a href="sqlite.php">test</a><br />
                </fieldset>
            </td>
        </tr>
    </table>
    <pre>
    From C#, added in Global.asax.cs at Session_Start event. var_export is :
<?php
echo var_export($_SESSION, true).'<br />';
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