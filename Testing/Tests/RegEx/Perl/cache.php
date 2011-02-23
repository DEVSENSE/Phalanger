[expect php]

[file]
<?php
$subject = "abcdef";

$pattern = '<def>';
for ($i = 0; $i < 5; $i++)
{
	preg_match($pattern, $subject, $matches, PREG_OFFSET_CAPTURE, 3);
	if ($matches[0][0] == "def" && $matches[0][1] == 3)
		echo "OK\n";
	else
		echo "Fail.\n";
}
?> 