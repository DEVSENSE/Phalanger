[expect php]
[file]
<?

// tests expression lists in the loop:

for ($i = 0, $j = 10; $i < 2, $j > 2; $i++, $j--)
{
  echo $i,".",$j,"\n";
}
?>