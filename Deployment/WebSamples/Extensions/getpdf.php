<?php
$len = filesize($_GET['filename']);
header("Content-Type: application/pdf");
header("Content-Length: $len");
header("Accept-Ranges: bytes");
header("Content-Disposition: inline; filename=Samples.Clock.pdf");
readfile($_GET['filename']);
?> 