[expect php]

[file]
<?
$a = "b";
$$a = "Stored via indirect variable.";
echo $$a." ".$b;
?>