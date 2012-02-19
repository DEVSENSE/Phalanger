<?php
#
# TheComputer Language Benchmarks Game
# http://shootout.alioth.debian.org/
#
# reverse complement in PHP
# contributed by Danny Sauer
# modified by anon

ob_implicit_flush(1);
ob_start(NULL, 4096);

$str = '';
$seq = '';

# read in the file, a line at a time
$stdin = STDIN;
while( $str !== '' || !feof($stdin) ) {
    $str = stream_get_line($stdin, 100, "\n");
    if( isset($str[0]) && $str[0] === '>' ){
        # if we're on a comment line, print the previous seq and move on
        print_seq($seq);
        echo $str, "\n";
    }else{
        # otherwise, just append to the sequence
        $seq .= $str;
    }
}
print_seq($seq);

exit;

# print the sequence out, if it exists
function print_seq(&$seq){
    if ( $seq !== '' ) {
        echo chunk_split( strrev( strtr($seq, 'ACBDGHKMNSRUTWVYacbdghkmnsrutwvy', 'TGVHCDMKNSYAAWBRTGVHCDMKNSYAAWBR') ),
		60, "\n");
    }
    $seq = '';
}
?>
