[expect php]
[file]
<?php

$regex = '/(?P<0_notoc>__BEZOBSAHU__)|(?P<1_notoc>__NOTOC__)|(?P<0_nogallery>__BEZGALERIE__)|(?P<1_nogallery>__NOGALLERY__)|(?P<0_forcetoc>__FORCETOC__)|(?P<1_forcetoc>__VZDYOBSAH__)|(?P<0_toc>__OBSAH__)|(?P<1_toc>__TOC__)|(?P<0_noeditsection>__BEZEDITOVATCAST__)|(?P<1_noeditsection>__NOEDITSECTION__)|(?P<0_notitleconvert>__BEZKONVERZENADPISU__)|(?P<1_notitleconvert>__NOTC__)|(?P<2_notitleconvert>__NOTITLECONVERT__)|(?P<0_nocontentconvert>__BEZKONVERZEOBSAHU__)|(?P<1_nocontentconvert>__NOCC__)|(?P<2_nocontentconvert>__NOCONTENTCONVERT__)/iuS';


$text = "Do you want to clear all saved data that you have entered and restart the installation process?";

echo "PREG_PATTERN_ORDER: \n";
$res = preg_match_all( $regex, $text, $matches, PREG_PATTERN_ORDER );
var_dump($matches);

echo "\n\n PREG_SET_ORDER: \n";
$res = preg_match_all( $regex, $text, $matches, PREG_SET_ORDER );
var_dump($matches);


?>