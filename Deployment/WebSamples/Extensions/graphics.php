<html xmlns="http://www.w3.org/1999/xhtml">
<head>
  <title>Phalanger Samples - GD Extension</title>
</head>
<body>
<h1>Phalanger Samples - GD Extension</h1>
<p>
  The sample shows how to load a .png image and to draw to it.
</p>
<? 
$title = empty($_GET["Title"]) ? "Class diagram" : addcslashes($_GET["Title"],'"');
?>
<p align="center">
  <img src="image.php?Title=<? echo $title; ?>" alt="<? echo $title; ?>"/> 
</p>
<form method="get">
  A title:
  <input type="text" name="Title" value="<? echo $title; ?>" />
  <input type="submit" value="Set the title of the image">
</form>  
</body>
</html>