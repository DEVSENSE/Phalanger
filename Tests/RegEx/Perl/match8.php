[expect php]

[file]
<?php
$html_in = <<<table

<table>
	<tr>
		<td>1</td>
		<td>2</td>
	</tr>
	<tr>
		<td>3</td>
		<td>4</td>
	</tr>
	<tr>
		<td>5</td>
		<td>6</td>
	</tr>
</table>

table;

error_reporting(E_ALL & ~E_NOTICE);
echo transpose($html_in);
/****************************

HTML Table Transpose Function
Converts rows to columns and columns to rows in an HTML Table
By Darren Gates & Gary Beith, www.TUFaT.com

Example of usage:

$html_in = file_get_contents('some_table.html');
echo transpose($html_in);

****************************/

function transpose($html_in)
{
  $html_out = '';
  $element = '';
  $table_level = -1;
  $row_index = -1;
  $column_index = -1;
  $row_max = -1;
  $column_max = -1;
  $row_attrs = array();
  $cell_attrs = array();
  $elements = array();

  $tokens = preg_split(
  '/(?is)(<\/?(?:table|tr|th|td)(?:|\s.*?)>)/', 
  $html_in, -1, PREG_SPLIT_NO_EMPTY | PREG_SPLIT_DELIM_CAPTURE);
  foreach ($tokens as $token)
  {
    if (preg_match('/(?i)\A<table[>\s]/', $token))
    {
      $element .= $token;
      $table_level++;
    }
    elseif (preg_match('/(?i)\A<\/table>/', $token)) 
    {
      $element .= $token;
      $table_level--;
    }
    elseif ($table_level > 0)  // token is within nested table
    {
      $element .= $token;  
    }
    elseif (preg_match('/(?i)\A<tr[>\s]/', $token)) 
    {
      if (++$row_index == 0)
      {
        $html_out = $element; 
      }

      preg_match_all('/(?i)\s(align|bgcolor|char|charoff|valign)=([^>\s]*)/', $token, $row_attrs, PREG_SET_ORDER);
    }
    elseif (preg_match('/(?i)\A<\/tr>/', $token)) 
    {
      if ($column_index < 0)
      {
        $element = '';
      }
      else
      {
        $i_max = $row_index + $rowspan - 1;
        if ($row_max < $i_max)
        {
          $row_max = $i_max;
        }
        
        $j_max = $column_index + $colspan - 1;
        if ($column_max < $j_max)
        {
          $column_max = $j_max;
        }

        for ($i = $row_index; $i <= $i_max; $i++)
        {
          for ($j = $column_index; $j <= $j_max; $j++)
          {
            $elements[$i][$j] = $element;
            $element = ''; 
          }
        }
      }

      $column_index = -1;
    }
    elseif (preg_match('/(?i)\A<t[hd][>\s]/', $token)) 
    {
      if ($column_index >= 0)
      {
        $i_max = $row_index + $rowspan - 1;
        if ($row_max < $i_max)
        {
          $row_max = $i_max;
        }

        $j_max = $column_index + $colspan - 1;
        if ($column_max < $j_max)
        {
          $column_max = $j_max;
        }

        for ($i = $row_index; $i <= $i_max; $i++)
        {
          for ($j = $column_index; $j <= $j_max; $j++)
          {
            $elements[$i][$j] = $element;
            $element = '';
          }
        }
        
        $column_index = $j_max;
      }

      while (isset($elements[$row_index][++$column_index]))
      {
        continue;
      }

      $colspan = 1;
      $rowspan = 1;
      $tag = substr($token, 0, -1);
      preg_match_all('/(?is)\s(colspan|rowspan)=(?:"(.*?)"|([^>\s]*))/',$token, $cell_attrs, PREG_SET_ORDER);
      
      foreach ($cell_attrs as $cell_attr)
      {

        if (stristr($cell_attr[1], 'colspan') !== FALSE)
        {
          $colspan = (int)$cell_attr[2];
          $tag = preg_replace('/(?i)(\s)colspan=/','$1rrrspan=',$tag);
        }
        elseif (stristr($cell_attr[1], 'rowspan') !== FALSE)
        {
          $rowspan = (int)$cell_attr[2];
          $tag = preg_replace('/(?i)(\s)rowspan=/','$1cccspan=',$tag);
        }
      }

      $tag = preg_replace('/(?i)(\s)rrrspan=/','$1rowspan=',$tag);
      $tag = preg_replace('/(?i)(\s)cccspan=/','$1colspan=',$tag);

      preg_match_all('/(?i)\s(align|bgcolor|char|charoff|valign)=([^>\s]*)/',$token, $cell_attrs, PREG_SET_ORDER); 
      foreach ($row_attrs as $row_attr)
      {
        foreach ($cell_attrs as $cell_attr)
        {
          if (stristr($cell_attr[1], $row_attr[1]) !== FALSE)
          {
            continue 2;
          }
        }

        $tag .= $row_attr[0];
      }

      $tag .= '>';

      $element = $tag;  // initialize th/td element to tag
    }
    else
    {
      $element .= $token;  // add token to current element
    }
  }

  for ($i = 0; $i <= $column_max; $i++)
  {
    $html_out .= "<tr>\n";

    for ($j = 0; $j <= $row_max; $j++)
    {
      $html_out .= $elements[$j][$i];
    }

    $html_out .= "</tr>\n";
  }

  $html_out .= $element; 

  return $html_out;
}

?> 
