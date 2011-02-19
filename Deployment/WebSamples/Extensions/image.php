<?
   header("Content-type: image/png"); 
   $string = $_GET["Title"]; 
   $im     = imagecreatefrompng(getcwd() . "\\diagram.png"); 
   $orange = imagecolorallocate($im, 0, 0, 255); 
   imagestring($im, 3, 25, 9, $string, $orange);
   imagepng($im); 
   imagedestroy($im);
?>