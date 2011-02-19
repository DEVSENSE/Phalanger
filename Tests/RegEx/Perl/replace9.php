[expect php]

[file]
<?
$string1 = "<b>text</b>";
$string2 = "<b>te\nxt</b>";

// without the s
#output: something new
echo preg_replace("/<b>.*<\/b>/", "something new", $string1);
#output: <b>te\nxt</b>
echo preg_replace("/<b>.*<\/b>/", "something more new", $string2);

// with the s
#output: something new
echo preg_replace("/<b>.*<\/b>/s", "something new", $string1);
#output: something new
echo preg_replace("/<b>.*<\/b>/s", "something more new", $string2);
?> 
