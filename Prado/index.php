<?php
session_start();
$sqlite = extension_loaded("sqlite") ? "" : "&nbsp;(does not work : missing sqlite)";

require_once("prado/framework/prado.php");
require_once("tbs_class.php");

$tbs = new clsTinyButStrong();
$tbs->LoadTemplate("index.tpl.html");

////////////////////////////////////////////
// PRADO
$tbs->MergeField("prado_version", Prado::getVersion());
$tbs->MergeField("prado_powered", Prado::poweredByPrado());

$dir = scandir("prado/demos");
$arr = array();
function strip_dot_dirs($value)
{
    return $value != "." && $value != "..";
}
$dir = array_filter($dir, "strip_dot_dirs");
foreach($dir as $name)
{
    $item = array("src"=>"prado/demos/$name/index.php", "txt"=>$name);
    $arr[] = $item;
}
$tbs->MergeBlock("prado_demo", "array", $arr);

// TBS
$tbs->MergeField("tbs_version", $tbs->Version);

// Phalanger
ob_start() ;
phpinfo() ;
$pinfo = ob_get_contents();
ob_end_clean();
$tbs->MergeField("phpinfo", $pinfo);

// End
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