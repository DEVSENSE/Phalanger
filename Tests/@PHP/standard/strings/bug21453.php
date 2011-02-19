[expect php]
[file]
<?
include('Phalanger.inc');
$test = "
<table>
	<tr><td>first cell before < first cell after</td></tr>
	<tr><td>second cell before < second cell after</td></tr>
</table>";

	__var_dump(strip_tags($test));
?>