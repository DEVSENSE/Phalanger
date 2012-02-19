<?php
/* The Computer Language Benchmarks Game
   http://shootout.alioth.debian.org/

   contributed by Damien Bonvillain
   modified by anon
   fixed by Isaac Gouy
 */

ob_implicit_flush(1);
ob_start(NULL, 4096);

$sequence = read_sequence('THREE');

fclose(STDIN);

// sequence read, let's write some stats
write_freq($sequence, 1);
write_freq($sequence, 2);
write_count($sequence, 'GGT');
write_count($sequence, 'GGTA');
write_count($sequence, 'GGTATT');
write_count($sequence, 'GGTATTTTAATT');
write_count($sequence, 'GGTATTTTAATTTATAGT');

/* functions definitions follow */

function read_sequence($id) {
   $id = '>' . $id;
   $ln_id = strlen($id);
   $fd = STDIN;
   // reach sequence three
   $line = '';
   while($line !== '' || !feof($fd)) {
      $line = stream_get_line($fd, 100, "\n");
      if($line[0] == '>' && strncmp($line, $id, $ln_id) === 0) {
         break;
      }
   }
   if(feof($fd)) {
      // sequence not found
      exit(-1);
   }
   // next, read the content of the sequence
   $sequence = '';
   while($line !== '' || !feof($fd)) {
      $line = stream_get_line($fd, 100, "\n");
      if (!isset($line[0])) continue;
      $c = $line[0];
      if ($c === ';') continue;
      if ($c === '>') break;
      // append the uppercase sequence fragment,
      // must get rid of the CR/LF or whatever if present
      $sequence .= $line;
   }
   return strtoupper($sequence);
}

function write_freq($sequence, $key_length) {
   $map = generate_frequencies($sequence, $key_length);
   uasort($map, 'freq_name_comparator');
   foreach($map as $key => $val) {
      printf ("%s %.3f\n", $key, $val);
   }
   echo "\n";
}

function write_count($sequence, $key) {
   $map = generate_frequencies($sequence, strlen($key), false);
   if (isset($map[$key])) $value = $map[$key];
   else $value = 0;
   printf ("%d\t%s\n", $value, $key);
}

/**
 * Returns a map (key, count or freq(default))
 */
function generate_frequencies($sequence, $key_length, $compute_freq = true) {
   $result = array();
   $total = strlen($sequence) - $key_length;
   $i = $total;
   if ($key_length === 1) { 
      do {
         $key = $sequence[$i--];
         if (isset($result[$key])) ++$result[$key];
         else $result[$key] = 1;
      } while ($i);
   } else {
      do {
         $key = substr($sequence, $i--, $key_length);
         if(isset($result[$key])) ++$result[$key];
         else $result[$key] = 1;
      } while ($i);
   }
   if($compute_freq) {
      foreach($result as $k => $v) {
         $result[$k] = $v * 100 / $total;
      }
   }
   return $result;
}


function freq_name_comparator($a, $b) {
   if ($a == $b) return 0;
   return  ($a < $b) ? 1 : -1;
}


