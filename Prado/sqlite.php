<?php
session_start();
?>
<html>
<head></head>
<body>
<?php

/* a sqlite file */
$database=":memory:";
$error = "";

//Static function apporach
$db = sqlite_open($database);
sqlite_exec($db, "CREATE TABLE tA (a INT NOT NULL PRIMARY KEY)");
sqlite_exec($db, "CREATE TABLE tB (b INT NOT NULL PRIMARY KEY)");

$tables = array();
$q = sqlite_query($db, "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name");
$result = sqlite_fetch_all($q);
foreach($result as $tot_table)
{
    $tables[] = $tot_table['name'];
}
?>
<a href="index.php">index</a>
<br />
SQLite test :
<table>
    <tr>
        <th>Expected</th>
        <th>Result (static)</th>
        <th>Result (class)</th>
    </tr>
    <tr>
        <td>
            <pre>Array
(
    [0] => tA
    [1] => tB
)
</pre>
        </td>
        <td>
            <pre>
<?php print_r($tables); ?>
            </pre>
        </td>
        <td>
            <pre>
<?php

$db = new SQLiteDatabase(":memory:");
$db->exec("CREATE TABLE tA (a INT NOT NULL PRIMARY KEY)");
$db->exec("CREATE TABLE tB (b INT NOT NULL PRIMARY KEY)");
$q = $db->query("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name");
$result = $q->fetch_all();
foreach($result as $tot_table)
{
    $tables[] = $tot_table['name'];
}
print_r($tables);
?>
            </pre>
        </td>
    </tr>
</table>
<?php

 
 ?>
 </body>
 </html>