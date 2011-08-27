[expect php]
[file]
<?php

if (preg_match ("/^[a-zA-Z\_0-9\:\/ÄÖÜßäöüßÁÀÉÈÍÌÓÒÚÙÃÂÊÎÕÔÛÇáàéèíìóòúùãâêîõôûç]+([a-zA-Z\_0-9\:\.\,\(\)\[\]\ÄÖÜßäöüßÁÀÉÈÍÌÓÒÚÙÃÂÊÎÕÔÛÇáàéèíìóòúùãâêîõôûç\-\/\s+]*)$/", "C:/inetpub/wwwroot/pic/zoom/animals/"))
	echo "pica";
else
	echo "nasrat";


?>