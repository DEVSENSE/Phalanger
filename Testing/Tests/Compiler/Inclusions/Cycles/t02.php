[expect php]
[file]

<?
error_reporting(0);

include "b02.inc";
include "c02.inc";
b();
c();

echo "Done."; // never gets here

?>
