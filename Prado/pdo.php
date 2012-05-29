<?php
session_start();

require_once("tbs_class.php");

$tbs = new clsTinyButStrong();
$tbs->LoadTemplate("pdo.tpl.html");

$drv = PDO::getAvailableDrivers();

$tbs->MergeBlock("drv", "array", $drv);

$tbs->Show(TBS_NOTHING);
$html = $tbs->Source;

$bom = chr(0xEF).chr(0xBB).chr(0xBF);
if(strlen($html) >= strlen($bom))
{
    if(substr($html, 0, strlen($bom)) == $bom)
    {
        $html = substr($html, strlen($bom));
    }
}

echo $html;
?>