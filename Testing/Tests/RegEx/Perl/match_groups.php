[expect exact]
Array
(
    [0] => alt=A smooth, rocket-shaped shark swims quickly through the water.
    [0_img_left] =>
    [1] =>
    [0_img_right] =>
    [2] =>
    [0_img_center] =>
    [3] =>
    [1_img_center] =>
    [4] =>
    [0_img_none] =>
    [5] =>
    [0_img_baseline] =>
    [6] =>
    [0_img_sub] =>
    [7] =>
    [0_img_super] =>
    [8] =>
    [1_img_super] =>
    [9] =>
    [0_img_top] =>
    [10] =>
    [0_img_text_top] =>
    [11] =>
    [0_img_middle] =>
    [12] =>
    [0_img_bottom] =>
    [13] =>
    [0_img_text_bottom] =>
    [14] =>
    [0_img_thumbnail] =>
    [15] =>
    [1_img_thumbnail] =>
    [16] =>
    [0_img_manualthumb] =>
    [17] =>
    [18] =>
    [1_img_manualthumb] =>
    [19] =>
    [20] =>
    [0_img_framed] =>
    [21] =>
    [1_img_framed] =>
    [22] =>
    [2_img_framed] =>
    [23] =>
    [0_img_frameless] =>
    [24] =>
    [0_img_upright] =>
    [25] =>
    [1_img_upright] =>
    [26] =>
    [27] =>
    [2_img_upright] =>
    [28] =>
    [29] =>
    [0_img_border] =>
    [30] =>
    [0_img_link] =>
    [31] =>
    [32] =>
    [0_img_alt] => alt=A smooth, rocket-shaped shark swims quickly through the water.
    [33] => alt=A smooth, rocket-shaped shark swims quickly through the water.
    [34] => A smooth, rocket-shaped shark swims quickly through the water.
)
[file]
<?php

$line_in = 'alt=A smooth, rocket-shaped shark swims quickly through the water.';

if (preg_match('/^(?:(?P<0_img_left>left)|(?P<0_img_right>right)|(?P<0_img_center>center)|(?P<1_img_center>centre)|(?P<0_img_none>none)|(?P<0_img_baseline>baseline)|(?P<0_img_sub>sub)|(?P<0_img_super>super)|(?P<1_img_super>sup)|(?P<0_img_top>top)|(?P<0_img_text_top>text-top)|(?P<0_img_middle>middle)|(?P<0_img_bottom>bottom)|(?P<0_img_text_bottom>text-bottom)|(?P<0_img_thumbnail>thumbnail)|(?P<1_img_thumbnail>thumb)|(?P<0_img_manualthumb>thumbnail\=(.*?))|(?P<1_img_manualthumb>thumb\=(.*?))|(?P<0_img_framed>framed)|(?P<1_img_framed>enframed)|(?P<2_img_framed>frame)|(?P<0_img_frameless>frameless)|(?P<0_img_upright>upright)|(?P<1_img_upright>upright\=(.*?))|(?P<2_img_upright>upright (.*?))|(?P<0_img_border>border)|(?P<0_img_link>link\=(.*?))|(?P<0_img_alt>alt\=(.*?)))$/S',
  $line_in,
  $elements))
{
  print_r($elements);
}

?>