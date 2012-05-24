<?php
/**
 * CAPTCHA generator script.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: captcha.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls.assets
 */

define('THEME_OPAQUE_BACKGROUND',0x0001);
define('THEME_NOISY_BACKGROUND',0x0002);
define('THEME_HAS_GRID',0x0004);
define('THEME_HAS_SCRIBBLE',0x0008);
define('THEME_MORPH_BACKGROUND',0x0010);
define('THEME_SHADOWED_TEXT',0x0020);

require_once(dirname(__FILE__).'/captcha_key.php');

$token='error';
$theme=0;

if(isset($_GET['options']))
{
	$str=base64_decode($_GET['options']);
	if(strlen($str)>32)
	{
		$hash=substr($str,0,32);
		$str=substr($str,32);
		if(md5($privateKey.$str)===$hash)
		{
			$options=unserialize($str);
			$publicKey=$options['publicKey'];
			$tokenLength=$options['tokenLength'];
			$caseSensitive=$options['caseSensitive'];
			$alphabet=$options['alphabet'];
			$fontSize=$options['fontSize'];
			$theme=$options['theme'];
			if(($randomSeed=$options['randomSeed'])>0)
				srand($randomSeed);
			else
				srand((int)(microtime()*1000000));
			$token=generateToken($publicKey,$privateKey,$alphabet,$tokenLength,$caseSensitive);
		}
	}
}

displayToken($token,$fontSize,$theme);

function generateToken($publicKey,$privateKey,$alphabet,$tokenLength,$caseSensitive)
{
	$token=substr(hash2string(md5($publicKey.$privateKey),$alphabet).hash2string(md5($privateKey.$publicKey),$alphabet),0,$tokenLength);
	return $caseSensitive?$token:strtoupper($token);
}

function hash2string($hex,$alphabet)
{
	if(strlen($alphabet)<2)
		$alphabet='234578adefhijmnrtABDEFGHJLMNRT';
	$hexLength=strlen($hex);
	$base=strlen($alphabet);
	$result='';
	for($i=0;$i<$hexLength;$i+=6)
	{
		$number=hexdec(substr($hex,$i,6));
		while($number)
		{
			$result.=$alphabet[$number%$base];
			$number=floor($number/$base);
		}
	}
	return $result;
}

function displayToken($token,$fontSize,$theme)
{
	if(($fontSize=(int)$fontSize)<22)
		$fontSize=22;
	if($fontSize>100)
		$fontSize=100;
	$length=strlen($token);
	$padding=10;
	$fontWidth=$fontSize;
	$fontHeight=floor($fontWidth*1.5);
	$width=$fontWidth*$length+$padding*2;
	$height=$fontHeight;
	$image=imagecreatetruecolor($width,$height);

	addBackground
	(
		$image, $width, $height,
		$theme&THEME_OPAQUE_BACKGROUND,
		$theme&THEME_NOISY_BACKGROUND,
		$theme&THEME_HAS_GRID,
		$theme&THEME_HAS_SCRIBBLE,
		$theme&THEME_MORPH_BACKGROUND
	);

	$font=dirname(__FILE__).DIRECTORY_SEPARATOR.'verase.ttf';

	if(function_exists('imagefilter'))
    	imagefilter($image,IMG_FILTER_GAUSSIAN_BLUR);

	$hasShadow=($theme&THEME_SHADOWED_TEXT);
    for($i=0;$i<$length;$i++)
	{
        $color=imagecolorallocate($image,rand(150,220),rand(150,220),rand(150,220));
        $size=rand($fontWidth-10,$fontWidth);
        $angle=rand(-30,30);
        $x=$padding+$i*$fontWidth;
        $y=rand($fontHeight-15,$fontHeight-10);
        imagettftext($image,$size,$angle,$x,$y,$color,$font,$token[$i]);
        if($hasShadow)
        	imagettftext($image,$size,$angle,$x+2,$y+2,$color,$font,$token[$i]);
        imagecolordeallocate($image,$color);
    }

	imagepng($image);
	imagedestroy($image);
}

function addBackground($image,$width,$height,$opaque,$noisy,$hasGrid,$hasScribble,$morph)
{
	$background=imagecreatetruecolor($width*2,$height*2);
	$white=imagecolorallocate($background,255,255,255);
	imagefill($background,0,0,$white);

	if($opaque)
		imagefill($background,0,0,imagecolorallocate($background,100,100,100));

	if($noisy)
		addNoise($background,$width*2,$height*2);

	if($hasGrid)
		addGrid($background,$width*2,$height*2);

	if($hasScribble)
		addScribble($background,$width*2,$height*2);

	if($morph)
		morphImage($background,$width*2,$height*2);

	imagecopy($image,$background,0,0,30,30,$width,$height);

	if(!$opaque)
		imagecolortransparent($image,$white);
}

function addNoise($image,$width,$height)
{
	for($x=0;$x<$width;++$x)
	{
		for($y=0;$y<$height;++$y)
		{
			if(rand(0,100)<25)
			{
				$color=imagecolorallocate($image,rand(150,220),rand(150,220),rand(150,220));
				imagesetpixel($image,$x,$y,$color);
	            imagecolordeallocate($image,$color);
	        }
		}
	}
}

function addGrid($image,$width,$height)
{
	for($i=0;$i<$width;$i+=rand(15,25))
	{
		imagesetthickness($image,rand(2,6));
		$color=imagecolorallocate($image,rand(100,180),rand(100,180),rand(100,180));
		imageline($image,$i+rand(-10,20),0,$i+rand(-10,20),$height,$color);
		imagecolordeallocate($image,$color);
	}
	for($i=0;$i<$height;$i+=rand(15,25))
	{
		imagesetthickness($image,rand(2,6));
		$color=imagecolorallocate($image,rand(100,180),rand(100,180),rand(100,180));
		imageline($image,0,$i+rand(-10,20),$width,$i+rand(-10,20),$color);
		imagecolordeallocate($image,$color);
	}
}

function addScribble($image,$width,$height)
{
	for($i=0;$i<8;$i++)
	{
		$color=imagecolorallocate($image,rand(100,180),rand(100,180),rand(100,180));
		$points=array();
		for($j=1;$j<rand(5,10);$j++)
		{
			$points[]=rand(2*(20*($i+1)),2*(50*($i+1)));
			$points[]=rand(30,$height+30);
		}
		imagesetthickness($image,rand(2,6));
		imagepolygon($image,$points,intval(sizeof($points)/2),$color);
		imagecolordeallocate($image,$color);
	}
}

function morphImage($image,$width,$height)
{
	$tempImage=imagecreatetruecolor($width,$height);
	$chunk=rand(1,5);
	for($x=$y=0;$x<$width;$x+=$chunk)
	{
		$chunk=rand(1,5);
		$y+=rand(-1,1);
		if($y>=$height)	$y=$height-5;
		if($y<0) $y=5;
		imagecopy($tempImage,$image,$x,0,$x,$y,$chunk,$height);
	}
	for($x=$y=0;$y<$height;$y+=$chunk)
	{
		$chunk=rand(1,5);
		$x+=rand(-1,1);
		if($x>=$width)	$x=$width-5;
		if($x<0) $x=5;
		imagecopy($image,$tempImage,$x,$y,0,$y,$width,$chunk);
	}
}

