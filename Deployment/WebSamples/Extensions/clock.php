<?php

/*
  
  This sample was borrowed from the PHP manual. It's author is Uwe Steinmann.
  It was slightly modified by Jan Benda.
  
*/

if (!extension_loaded('pdf'))
{
	die("PDF not installed!");
}
?>
<html xmlns="http://www.w3.org/1999/xhtml">
  <head>
    <title>Phalanger Samples - PDF Clock</title>
    <meta http-equiv="Content-Type" content="text/html; charset=iso-8859-2" />
    <meta name="Generator" content="Microsoft Visual Studio .NET 7.1">
    <meta name="vs_targetSchema" content="http://schemas.microsoft.com/intellisense/ie5">
    <meta name="Description" content="Phalanger Sample - Analog Clock Generated to PDF">
    <meta name="Keywords" content="">
    <meta name="Authors" content="Uwe Steinman, Jan Benda">
    <meta name="Status" content="Sample">
  </head>
  <body>
    <h1>Phalanger Samples - PDF Clock</h1>
<?    
echo "<DIV id='loading'>Generating PDF file, please wait a moment ...</div><br/>";
flush(); 

error_reporting(E_ALL);

$time = time();
$ltime = getdate($time);
$stime = Date("H:i:s", $time);
$texttime = ($ltime['hours']+0) . " hour" . ($ltime['hours'] ? "s":"");
if ($ltime['minutes']) $texttime .= " and " . ($ltime['minutes']+0) . " minute" . ($ltime['minutes'] ? "s":"");
  else $texttime = "exactly $texttime";

// A4 page: 595 x 842 points

$radius = 177;
$margin = 120;
$ring = 40;
$header = 20;
$footer = 228;
$file = tempnam("", "tests") . '.pdf';

$width = 2 * ($radius + $margin);
$height = 2 * ($radius + $margin) + $header + $footer;

$pdf = pdf_new();
if (!pdf_open_file($pdf, $file)) {
    echo "error";
    exit;
};

pdf_set_parameter($pdf, "warning", "true");

pdf_set_info($pdf, "Creator", "Phalanger");
pdf_set_info($pdf, "Author", "Uwe Steinmann");
pdf_set_info($pdf, "Title", "Analog Clock");

pdf_begin_page($pdf, $width, $height);

function center($s, $y, $size, $fontname = "Times-Roman", $outline = 0)
{
  global $pdf, $font, $width;
  pdf_set_value($pdf, "textrendering", $outline);
  $font = pdf_findfont($pdf, $fontname, "iso8859-2");
  pdf_setfont($pdf, $font, $size);
  $w = pdf_stringwidth($pdf, $s);
  pdf_show_xy($pdf, $s, ($width - $w) / 2, $y); 
}

/* outlined */
center("It is $texttime.", $height - 60, 42, "Times-Roman", 1);
center("It is time for", 200, 100, "Times-Roman", 1);

/* filled */
center("Phalanger!", 70, 110, "Times-Bold", 0);

pdf_translate($pdf, $radius + $margin, $radius + $margin + $footer);
pdf_save($pdf);
pdf_setrgbcolor($pdf, 0.0, 0.0, 1.0);

/* minute strokes */
pdf_setlinewidth($pdf, 2.0);
for ($alpha = 0; $alpha < 360; $alpha += 6) {
    pdf_rotate($pdf, 6.0);
    pdf_moveto($pdf, $radius, 0.0);
    pdf_lineto($pdf, $radius-$ring/3, 0.0);
    pdf_stroke($pdf);
}

pdf_restore($pdf);
pdf_save($pdf);

/* 5 minute strokes */
pdf_setlinewidth($pdf, 3.0);
for ($alpha = 0; $alpha < 360; $alpha += 30) { 
    pdf_rotate($pdf, 30.0);
    pdf_moveto($pdf, $radius, 0.0);
    pdf_lineto($pdf, $radius-$ring, 0.0);
    pdf_stroke($pdf);
}

/* draw hour hand */
pdf_save($pdf);
pdf_rotate($pdf,-(($ltime['minutes']/60.0)+$ltime['hours']-3.0)*30.0);
pdf_moveto($pdf, -$radius/10, -$radius/20);
pdf_lineto($pdf, $radius/2, 0.0);
pdf_lineto($pdf, -$radius/10, $radius/20);
pdf_closepath($pdf);
pdf_fill($pdf);
pdf_restore($pdf);

/* draw minute hand */
pdf_save($pdf);
pdf_rotate($pdf,-(($ltime['seconds']/60.0)+$ltime['minutes']-15.0)*6.0);
pdf_moveto($pdf, -$radius/10, -$radius/20);
pdf_lineto($pdf, $radius * 0.8, 0.0);
pdf_lineto($pdf, -$radius/10, $radius/20);
pdf_closepath($pdf);
pdf_fill($pdf);
pdf_restore($pdf);

/* draw second hand */
pdf_setrgbcolor($pdf, 1.0, 0.0, 0.0);
pdf_setlinewidth($pdf, 2);
pdf_save($pdf);
pdf_rotate($pdf, -(($ltime['seconds'] - 15.0) * 6.0));
pdf_moveto($pdf, -$radius/5, 0.0);
pdf_lineto($pdf, $radius, 0.0);
pdf_stroke($pdf);
pdf_restore($pdf);

/* draw little circle at center */
pdf_circle($pdf, 0, 0, $radius/30);
pdf_fill($pdf);

pdf_restore($pdf);

/* DONE */
pdf_end_page($pdf);
pdf_close($pdf);
pdf_delete($pdf);

// hides "Generating ..." message:
echo "<script language='JavaScript'>document.getElementById('loading').style.display = 'none';</script>";
?>
<?if (!file_exists($file)):?>
    <h2 style="color: red">Error occured: file <?=$file?> does not exist!</h2>
<?endif;?>
    It is exactly <?=$stime?>.
    A PDF file containing an image of clocks with the current time has been generated.</br>
    <p>
    <form method="get" action="getpdf.php">
       <input style="font-weight: bold" type="submit" value="Get it in PDF!">
       <input type="hidden" name="filename" value="<?=$file?>">
    </form>
    <form method="get" action="">
       <input type="submit" value="Refresh">
    </form>
    </p>

  </body>
</html>
