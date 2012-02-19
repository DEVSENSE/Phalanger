<?php
#
# The Computer Language Benchmarks Game
# http://shootout.alioth.debian.org/
#
# contributed by Danny Sauer
# modified by Josh Goldfoot
# modified by Sergey Khripunov

# regexp matches

#ini_set("memory_limit","40M");

$variants = array(
    'agggtaaa|tttaccct',
    '[cgt]gggtaaa|tttaccc[acg]',
    'a[act]ggtaaa|tttacc[agt]t',
    'ag[act]gtaaa|tttac[agt]ct',
    'agg[act]taaa|ttta[agt]cct',
    'aggg[acg]aaa|ttt[cgt]ccct',
    'agggt[cgt]aa|tt[acg]accct',
    'agggta[cgt]a|t[acg]taccct',
    'agggtaa[cgt]|[acg]ttaccct',
);

# IUB replacement parallel arrays
$IUB = array(); $IUBnew = array();
$IUB[]='/B/S';     $IUBnew[]='(c|g|t)';
$IUB[]='/D/S';     $IUBnew[]='(a|g|t)';
$IUB[]='/H/S';     $IUBnew[]='(a|c|t)';
$IUB[]='/K/S';     $IUBnew[]='(g|t)';
$IUB[]='/M/S';     $IUBnew[]='(a|c)';
$IUB[]='/N/S';     $IUBnew[]='(a|c|g|t)';
$IUB[]='/R/S';     $IUBnew[]='(a|g)';
$IUB[]='/S/S';     $IUBnew[]='(c|g)';
$IUB[]='/V/S';     $IUBnew[]='(a|c|g)';
$IUB[]='/W/S';     $IUBnew[]='(a|t)';
$IUB[]='/Y/S';     $IUBnew[]='(c|t)';

# sequence descriptions start with > and comments start with ;
#my $stuffToRemove = '^[>;].*$|[\r\n]';
$stuffToRemove = '^>.*$|\n'; # no comments, *nix-format test file...

# read in file
$contents = file_get_contents('php://stdin');
$initialLength = strlen($contents);

# remove things
$contents = preg_replace("/$stuffToRemove/mS", '', $contents);
$codeLength = strlen($contents);

# do regexp counts
foreach ($variants as &$regex){
    print $regex . ' ' . preg_match_all('/'.$regex.'/iS', $contents, $discard). "\n";
}

# do replacements
$contents = preg_replace($IUB, $IUBnew, $contents);

print "\n" .
      $initialLength . "\n" .
      $codeLength . "\n" .
      strlen($contents) . "\n" ;
?>
